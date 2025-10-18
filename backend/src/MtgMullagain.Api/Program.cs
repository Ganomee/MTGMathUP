using Microsoft.EntityFrameworkCore;
using MtgMullagain.Api;
using MtgMullagain.Infrastructure;
using Npgsql;
using NSwag;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Use snake_case for JSON serialization to match PostgreSQL naming conventions
        options.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
        options.JsonSerializerOptions.DictionaryKeyPolicy = new SnakeCaseNamingPolicy();
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Register NSwag document generator
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "MTG Mullagain API";
    config.Version = "v1";
});

// Add Entity Framework DbContext with PostgreSQL support
builder.Services.AddDbContext<MtgMullagainDbContext>(options =>
{
    var resolvedConnection = builder.Configuration.GetConnectionString("Default")
        ?? builder.Configuration["DATASOURCE_URL"]
        ?? builder.Configuration["CONNECTION_STRING"];

    if (string.IsNullOrWhiteSpace(resolvedConnection))
    {
        throw new InvalidOperationException("No PostgreSQL connection string provided. Set ConnectionStrings:Default, DATASOURCE_URL, or CONNECTION_STRING.");
    }

    options.UseNpgsql(resolvedConnection, npgsqlOptions =>
    {
        npgsqlOptions.UseVector();
    });
});

// Register HttpClient factory for API calls
builder.Services.AddHttpClient();

// Register services
builder.Services.AddScoped<MtgMullagain.Core.Services.IHandService, MtgMullagain.Infrastructure.Services.HandService>();
builder.Services.AddScoped<MtgMullagain.Infrastructure.Services.ScryfallImportService>();

// Register background services
builder.Services.AddHostedService<MtgMullagain.Api.Services.CardImportBackgroundService>();

// Add SignalR for ElectricSQL sync
builder.Services.AddSignalR();

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:8080", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

var app = builder.Build();

// Auto-migrate the database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MtgMullagainDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Expose NSwag-generated OpenAPI downstream (JSON will be available at /swagger/v1/swagger.json)
    app.UseOpenApi();
}

// Enable CORS
app.UseCors("AllowFrontend");

// Add controllers
app.MapControllers();

// Map SignalR hub for ElectricSQL sync
app.MapHub<MtgMullagain.Api.Hubs.SyncHub>("/sync");

// Basic health check endpoint
app.MapGet("/health", () => "Healthy")
    .WithName("HealthCheck")
    .WithOpenApi();

// Hello endpoint for initial testing
app.MapGet("/", () => "Hello from MTG Mullagain")
    .WithName("Hello")
    .WithOpenApi();

// TODO: Add API endpoints
// app.MapGet("/api/cards", () => { /* TODO: Implement card query endpoint */ });
// app.MapPost("/api/import/scryfall", () => { /* TODO: Implement Scryfall import */ });
// app.MapGet("/api/decks", () => { /* TODO: Implement deck listing */ });
// app.MapPost("/api/decks", () => { /* TODO: Implement deck creation */ });
// app.MapPost("/api/hands/generate", () => { /* TODO: Implement hand generation */ });
// app.MapPost("/api/hands/evaluate", () => { /* TODO: Implement hand evaluation */ });
// app.MapGet("/api/hands/similar/{id}", () => { /* TODO: Implement similar hands search */ });

app.Run();
