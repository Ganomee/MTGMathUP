using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MtgMullagain.Core.Entities;
using MtgMullagain.Infrastructure;

namespace MtgMullagain.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HandsController : ControllerBase
{
    private readonly MtgMullagainDbContext _context;
    private readonly ILogger<HandsController> _logger;

    public HandsController(MtgMullagainDbContext context, ILogger<HandsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Hand>>> GetHands()
    {
        try
        {
            var hands = await _context.Hands
                .OrderByDescending(h => h.CreatedAt)
                .Take(100) // Limit to recent 100 hands
                .ToListAsync();
            
            return Ok(hands);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching hands");
            return StatusCode(500, new { error = "Failed to fetch hands" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Hand>> GetHand(long id)
    {
        try
        {
            var hand = await _context.Hands.FindAsync(id);
            if (hand == null)
                return NotFound(new { error = $"Hand with ID {id} not found" });

            return Ok(hand);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching hand {HandId}", id);
            return StatusCode(500, new { error = "Failed to fetch hand" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Hand>> CreateHand([FromBody] HandCreateDto handDto)
    {
        try
        {
            var hand = new Hand
            {
                CardIntIds = handDto.CardIntIds,
                Size = handDto.Size,
                Hash64 = ComputeHash(handDto.CardIntIds),
                CanonicalKey = string.Join(",", handDto.CardIntIds.OrderBy(x => x)),
                CreatedAt = DateTime.UtcNow
            };

            _context.Hands.Add(hand);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created hand {HandId}", hand.Id);
            return CreatedAtAction(nameof(GetHand), new { id = hand.Id }, hand);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating hand");
            return StatusCode(500, new { error = "Failed to create hand" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHand(long id)
    {
        try
        {
            var hand = await _context.Hands.FindAsync(id);
            if (hand == null)
                return NotFound(new { error = $"Hand with ID {id} not found" });

            _context.Hands.Remove(hand);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted hand {HandId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hand {HandId}", id);
            return StatusCode(500, new { error = "Failed to delete hand" });
        }
    }

    private static long ComputeHash(int[] cardIntIds)
    {
        // Simple hash computation - in production use a proper hash function
        var sorted = cardIntIds.OrderBy(x => x).ToArray();
        long hash = 0;
        foreach (var id in sorted)
        {
            hash = hash * 31 + id;
        }
        return hash;
    }
}

public record HandCreateDto(int[] CardIntIds, short Size);

