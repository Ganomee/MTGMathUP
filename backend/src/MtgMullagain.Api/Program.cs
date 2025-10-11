using Microsoft.EntityFrameworkCore;
using MtgMullagain.Infrastructure;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework DbContext with pgvector support
builder.Services.AddDbContext<MtgMullagainDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.UseVector();
    });
});

// Register services
builder.Services.AddScoped<MtgMullagain.Core.Services.IHandService, MtgMullagain.Core.Services.HandService>();

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:8080", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS
app.UseCors("AllowFrontend");

// Add controllers
app.MapControllers();

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
