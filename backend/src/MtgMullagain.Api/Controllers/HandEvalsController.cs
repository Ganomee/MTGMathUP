using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MtgMullagain.Core.Entities;
using MtgMullagain.Infrastructure;

namespace MtgMullagain.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HandEvalsController : ControllerBase
{
    private readonly MtgMullagainDbContext _context;
    private readonly ILogger<HandEvalsController> _logger;

    public HandEvalsController(MtgMullagainDbContext context, ILogger<HandEvalsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HandEval>>> GetHandEvals()
    {
        try
        {
            var handEvals = await _context.HandEvals
                .OrderByDescending(he => he.CreatedAt)
                .Take(100)
                .ToListAsync();
            
            return Ok(handEvals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching hand evaluations");
            return StatusCode(500, new { error = "Failed to fetch hand evaluations" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<HandEval>> GetHandEval(long id)
    {
        try
        {
            var handEval = await _context.HandEvals.FindAsync(id);
            if (handEval == null)
                return NotFound(new { error = $"Hand evaluation with ID {id} not found" });

            return Ok(handEval);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching hand evaluation {HandEvalId}", id);
            return StatusCode(500, new { error = "Failed to fetch hand evaluation" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<HandEval>> CreateHandEval([FromBody] HandEvalCreateDto handEvalDto)
    {
        try
        {
            var handEval = new HandEval
            {
                Hand1Id = handEvalDto.Hand1Id,
                Hand2Id = handEvalDto.Hand2Id,
                PreferredHand = handEvalDto.PreferredHand,
                ContextJson = handEvalDto.ContextJson,
                CreatedAt = DateTime.UtcNow
            };

            _context.HandEvals.Add(handEval);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created hand evaluation {HandEvalId}", handEval.Id);
            return CreatedAtAction(nameof(GetHandEval), new { id = handEval.Id }, handEval);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating hand evaluation");
            return StatusCode(500, new { error = "Failed to create hand evaluation" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHandEval(long id)
    {
        try
        {
            var handEval = await _context.HandEvals.FindAsync(id);
            if (handEval == null)
                return NotFound(new { error = $"Hand evaluation with ID {id} not found" });

            _context.HandEvals.Remove(handEval);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted hand evaluation {HandEvalId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hand evaluation {HandEvalId}", id);
            return StatusCode(500, new { error = "Failed to delete hand evaluation" });
        }
    }
}

public record HandEvalCreateDto(
    long Hand1Id,
    long Hand2Id,
    short PreferredHand,
    string? ContextJson
);

