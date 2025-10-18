using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace MtgMullagain.Api.Hubs;

/// <summary>
/// SignalR hub for ElectricSQL sync coordination
/// </summary>
public class SyncHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> ConnectedClients = new();
    private readonly ILogger<SyncHub> _logger;

    public SyncHub(ILogger<SyncHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var clientId = Context.GetHttpContext()?.Request.Query["clientId"].FirstOrDefault() ?? connectionId;
        
        ConnectedClients.TryAdd(connectionId, clientId);
        
        _logger.LogInformation("Client {ClientId} connected with connection {ConnectionId}", clientId, connectionId);
        
        // Join the client to their specific group
        await Groups.AddToGroupAsync(connectionId, $"client_{clientId}");
        
        // Notify about sync status
        await Clients.Group($"client_{clientId}").SendAsync("SyncStatus", new
        {
            IsOnline = true,
            IsSyncing = false,
            LastSyncTime = DateTime.UtcNow,
            PendingChanges = 0
        });

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        
        if (ConnectedClients.TryRemove(connectionId, out var clientId))
        {
            _logger.LogInformation("Client {ClientId} disconnected from connection {ConnectionId}", clientId, connectionId);
            
            // Notify about offline status
            await Clients.Group($"client_{clientId}").SendAsync("SyncStatus", new
            {
                IsOnline = false,
                IsSyncing = false,
                LastSyncTime = (DateTime?)null,
                PendingChanges = 0
            });
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Handle sync request from client
    /// </summary>
    public async Task RequestSync(string clientId, object syncData)
    {
        _logger.LogInformation("Sync request from client {ClientId}", clientId);
        
        // Process sync data and send response
        await Clients.Group($"client_{clientId}").SendAsync("SyncResponse", new
        {
            Success = true,
            Data = syncData,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handle conflict resolution request
    /// </summary>
    public async Task ResolveConflict(string clientId, object conflictData)
    {
        _logger.LogInformation("Conflict resolution request from client {ClientId}", clientId);
        
        // Implement conflict resolution logic here
        // For now, use last-write-wins strategy
        await Clients.Group($"client_{clientId}").SendAsync("ConflictResolved", new
        {
            Resolution = "last-write-wins",
            Data = conflictData,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get connected clients count
    /// </summary>
    public async Task<int> GetConnectedClientsCount()
    {
        return ConnectedClients.Count;
    }

    /// <summary>
    /// Broadcast to all connected clients
    /// </summary>
    public async Task BroadcastMessage(string message, object data)
    {
        await Clients.All.SendAsync("Broadcast", new
        {
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        });
    }
}
