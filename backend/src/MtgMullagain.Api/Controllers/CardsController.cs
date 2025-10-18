using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MtgMullagain.Infrastructure;
using MtgMullagain.Core.Entities;

namespace MtgMullagain.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CardsController : ControllerBase
{
    private readonly MtgMullagainDbContext _context;
    private readonly ILogger<CardsController> _logger;

    public CardsController(MtgMullagainDbContext context, ILogger<CardsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Card>>> GetCards([FromQuery] int limit = 100)
    {
        var cards = await _context.Cards
            .OrderBy(c => c.Name)
            .Take(limit)
            .ToListAsync();
        
        return Ok(cards);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Card>> GetCard(Guid id)
    {
        var card = await _context.Cards.FindAsync(id);
        
        if (card == null)
        {
            return NotFound();
        }
        
        return Ok(card);
    }

    [HttpPost]
    public async Task<ActionResult<Card>> CreateCard([FromBody] CardCreateDto cardDto)
    {
        try
        {
            var card = new Card
            {
                Id = Guid.NewGuid(),
                Name = cardDto.Name,
                ManaCost = cardDto.ManaCost ?? string.Empty,
                Type = cardDto.Type ?? string.Empty,
                OracleText = cardDto.OracleText ?? string.Empty,
                Cmc = cardDto.Cmc ?? 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Cards.Add(card);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created card {CardId}: {CardName}", card.Id, card.Name);
            return CreatedAtAction(nameof(GetCard), new { id = card.Id }, card);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating card");
            return StatusCode(500, new { error = "Failed to create card", details = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Card>> UpdateCard(Guid id, [FromBody] CardUpdateDto cardDto)
    {
        try
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
                return NotFound(new { error = $"Card with ID {id} not found" });

            if (!string.IsNullOrEmpty(cardDto.Name))
                card.Name = cardDto.Name;
            if (cardDto.ManaCost != null)
                card.ManaCost = cardDto.ManaCost;
            if (cardDto.Type != null)
                card.Type = cardDto.Type;
            if (cardDto.OracleText != null)
                card.OracleText = cardDto.OracleText;
            if (cardDto.Cmc.HasValue)
                card.Cmc = cardDto.Cmc.Value;
            
            card.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated card {CardId}", id);
            return Ok(card);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating card {CardId}", id);
            return StatusCode(500, new { error = "Failed to update card", details = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCard(Guid id)
    {
        try
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
                return NotFound(new { error = $"Card with ID {id} not found" });

            _context.Cards.Remove(card);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted card {CardId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting card {CardId}", id);
            return StatusCode(500, new { error = "Failed to delete card", details = ex.Message });
        }
    }
}

public record CardCreateDto(
    string Name,
    string? ManaCost,
    string? Type,
    string? OracleText,
    int? Cmc
);

public record CardUpdateDto(
    string? Name,
    string? ManaCost,
    string? Type,
    string? OracleText,
    int? Cmc
);

