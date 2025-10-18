using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MtgMullagain.Core.Entities;
using MtgMullagain.Infrastructure;

namespace MtgMullagain.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DecksController : ControllerBase
{
    private readonly MtgMullagainDbContext _context;
    private readonly ILogger<DecksController> _logger;

    public DecksController(MtgMullagainDbContext context, ILogger<DecksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all decks
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Deck>>> GetDecks()
    {
        try
        {
            var decks = await _context.Decks
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            
            return Ok(decks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching decks");
            return StatusCode(500, new { error = "Failed to fetch decks" });
        }
    }

    /// <summary>
    /// Get a specific deck by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Deck>> GetDeck(long id)
    {
        try
        {
            var deck = await _context.Decks
                .Include(d => d.DeckCards)
                .ThenInclude(dc => dc.Card)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deck == null)
            {
                return NotFound(new { error = $"Deck with ID {id} not found" });
            }

            return Ok(deck);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching deck {DeckId}", id);
            return StatusCode(500, new { error = "Failed to fetch deck" });
        }
    }

    /// <summary>
    /// Create a new deck
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Deck>> CreateDeck([FromBody] DeckCreateDto deckDto)
    {
        try
        {
            var deck = new Deck
            {
                Name = deckDto.Name,
                Format = deckDto.Format,
                Owner = deckDto.Owner ?? "Anonymous",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Decks.Add(deck);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created deck {DeckId}: {DeckName}", deck.Id, deck.Name);

            return CreatedAtAction(nameof(GetDeck), new { id = deck.Id }, deck);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deck");
            return StatusCode(500, new { error = "Failed to create deck" });
        }
    }

    /// <summary>
    /// Update an existing deck
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<Deck>> UpdateDeck(long id, [FromBody] DeckUpdateDto deckDto)
    {
        try
        {
            var deck = await _context.Decks.FindAsync(id);

            if (deck == null)
            {
                return NotFound(new { error = $"Deck with ID {id} not found" });
            }

            if (!string.IsNullOrEmpty(deckDto.Name))
                deck.Name = deckDto.Name;
            
            if (!string.IsNullOrEmpty(deckDto.Format))
                deck.Format = deckDto.Format;
            
            if (!string.IsNullOrEmpty(deckDto.Owner))
                deck.Owner = deckDto.Owner;

            deck.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated deck {DeckId}: {DeckName}", deck.Id, deck.Name);

            return Ok(deck);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating deck {DeckId}", id);
            return StatusCode(500, new { error = "Failed to update deck" });
        }
    }

    /// <summary>
    /// Delete a deck
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDeck(long id)
    {
        try
        {
            var deck = await _context.Decks.FindAsync(id);

            if (deck == null)
            {
                return NotFound(new { error = $"Deck with ID {id} not found" });
            }

            _context.Decks.Remove(deck);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted deck {DeckId}: {DeckName}", id, deck.Name);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting deck {DeckId}", id);
            return StatusCode(500, new { error = "Failed to delete deck" });
        }
    }
}

// DTOs
public record DeckCreateDto(string Name, string Format, string? Owner);
public record DeckUpdateDto(string? Name, string? Format, string? Owner);

