using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MtgMullagain.Core.DTOs;
using MtgMullagain.Core.Services;
using MtgMullagain.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace MtgMullagain.Api.Controllers;

/// <summary>
/// Controller for MTG hand operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HandController : ControllerBase
{
    private readonly IHandService _handService;
    private readonly MtgMullagainDbContext _context;

    public HandController(IHandService handService, MtgMullagainDbContext context)
    {
        _handService = handService;
        _context = context;
    }

    /// <summary>
    /// Find identical hands by hash
    /// </summary>
    /// <param name="cardIntIds">Array of card integer IDs</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="offset">Offset for pagination</param>
    /// <returns>List of identical hands</returns>
    [HttpPost("equal")]
    public async Task<ActionResult<PaginatedHandResponseDto>> FindEqualHands(
        [FromBody] int[] cardIntIds,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        if (cardIntIds == null || cardIntIds.Length == 0)
        {
            return BadRequest("Card IDs array cannot be null or empty");
        }

        try
        {
            // Canonicalize the input hand
            var canonical = Core.Utilities.HandCanonicalizer.Canonicalize(cardIntIds);

            // Find hands with identical hash, size, and card sequence
            var query = _context.Hands
                .Where(h => h.Hash64 == canonical.Hash64 && 
                           h.Size == canonical.Size &&
                           h.CardIntIds.SequenceEqual(canonical.CardIntIds));

            var totalCount = await query.CountAsync();
            var hands = await query
                .OrderByDescending(h => h.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .Select(h => new HandResponseDto
                {
                    Id = h.Id,
                    CardIntIds = h.CardIntIds,
                    Size = h.Size,
                    Hash64 = h.Hash64,
                    CanonicalKey = h.CanonicalKey,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync();

            return Ok(new PaginatedHandResponseDto
            {
                Hands = hands,
                TotalCount = totalCount,
                Offset = offset,
                Limit = limit,
                HasMore = offset + limit < totalCount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error finding equal hands: {ex.Message}");
        }
    }

    /// <summary>
    /// Find hands that contain one or more specified cards
    /// </summary>
    /// <param name="request">Query parameters</param>
    /// <returns>List of hands containing the specified cards</returns>
    [HttpPost("contains")]
    public async Task<ActionResult<PaginatedHandResponseDto>> FindHandsContaining(
        [FromBody] HandQueryDto request)
    {
        if (request.CardIntIds == null || request.CardIntIds.Length == 0)
        {
            return BadRequest("Card IDs array cannot be null or empty");
        }

        try
        {
            // Use PostgreSQL array containment operator (@>)
            var query = _context.Hands
                .Where(h => h.CardIntIds.Contains(request.CardIntIds));

            var totalCount = await query.CountAsync();
            var hands = await query
                .OrderByDescending(h => h.CreatedAt)
                .Skip(request.Offset)
                .Take(request.Limit)
                .Select(h => new HandResponseDto
                {
                    Id = h.Id,
                    CardIntIds = h.CardIntIds,
                    Size = h.Size,
                    Hash64 = h.Hash64,
                    CanonicalKey = h.CanonicalKey,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync();

            return Ok(new PaginatedHandResponseDto
            {
                Hands = hands,
                TotalCount = totalCount,
                Offset = request.Offset,
                Limit = request.Limit,
                HasMore = request.Offset + request.Limit < totalCount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error finding containing hands: {ex.Message}");
        }
    }

    /// <summary>
    /// Find hands sharing ≥ N cards using SQL count_intersection function
    /// </summary>
    /// <param name="request">Query parameters</param>
    /// <returns>List of hands sharing at least N cards</returns>
    [HttpPost("overlap")]
    public async Task<ActionResult<PaginatedHandResponseDto>> FindOverlappingHands(
        [FromBody] HandQueryDto request)
    {
        if (request.CardIntIds == null || request.CardIntIds.Length == 0)
        {
            return BadRequest("Card IDs array cannot be null or empty");
        }

        try
        {
            // Use raw SQL with count_intersection function
            var sql = @"
                SELECT h.id, h.card_int_ids, h.size, h.hash64, h.canonical_key, h.created_at,
                       count_intersection(h.card_int_ids, @cardIntIds) as shared_count
                FROM ""Hands"" h
                WHERE count_intersection(h.card_int_ids, @cardIntIds) >= @minSharedCards
                ORDER BY h.created_at DESC
                LIMIT @limit OFFSET @offset";

            var hands = await _context.Database
                .SqlQueryRaw<HandResponseDto>(sql,
                    new Npgsql.NpgsqlParameter("@cardIntIds", request.CardIntIds),
                    new Npgsql.NpgsqlParameter("@minSharedCards", request.MinSharedCards),
                    new Npgsql.NpgsqlParameter("@limit", request.Limit),
                    new Npgsql.NpgsqlParameter("@offset", request.Offset))
                .ToListAsync();

            // Get total count for pagination
            var countSql = @"
                SELECT COUNT(*)
                FROM ""Hands"" h
                WHERE count_intersection(h.card_int_ids, @cardIntIds) >= @minSharedCards";

            var totalCount = await _context.Database
                .SqlQueryRaw<int>(countSql,
                    new Npgsql.NpgsqlParameter("@cardIntIds", request.CardIntIds),
                    new Npgsql.NpgsqlParameter("@minSharedCards", request.MinSharedCards))
                .FirstAsync();

            return Ok(new PaginatedHandResponseDto
            {
                Hands = hands,
                TotalCount = totalCount,
                Offset = request.Offset,
                Limit = request.Limit,
                HasMore = request.Offset + request.Limit < totalCount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error finding overlapping hands: {ex.Message}");
        }
    }

    /// <summary>
    /// Find similar hands using vector similarity search
    /// </summary>
    /// <param name="request">Similarity search parameters</param>
    /// <returns>List of similar hands with similarity scores</returns>
    [HttpPost("similar")]
    public async Task<ActionResult<PaginatedHandResponseDto>> FindSimilarHands(
        [FromBody] HandSimilarityDto request)
    {
        if (request.HandId <= 0)
        {
            return BadRequest("Valid hand ID is required");
        }

        try
        {
            // Check if the reference hand exists and has an embedding
            var referenceHand = await _context.Hands
                .Include(h => h.HandEmbedding)
                .FirstOrDefaultAsync(h => h.Id == request.HandId);

            if (referenceHand == null)
            {
                return NotFound($"Hand {request.HandId} not found");
            }

            if (referenceHand.HandEmbedding == null)
            {
                return BadRequest($"Hand {request.HandId} has no embedding. Please generate embeddings first.");
            }

            // Use pgvector similarity search with cosine distance
            var sql = @"
                SELECT h.id, h.card_int_ids, h.size, h.hash64, h.canonical_key, h.created_at,
                       1 - (he.embedding <=> @referenceEmbedding) as similarity_score
                FROM ""Hands"" h
                INNER JOIN ""HandEmbeddings"" he ON h.id = he.hand_id
                WHERE h.id != @handId
                  AND 1 - (he.embedding <=> @referenceEmbedding) >= @threshold
                ORDER BY he.embedding <=> @referenceEmbedding
                LIMIT @limit";

            var hands = await _context.Database
                .SqlQueryRaw<HandResponseDto>(sql,
                    new Npgsql.NpgsqlParameter("@handId", request.HandId),
                    new Npgsql.NpgsqlParameter("@referenceEmbedding", referenceHand.HandEmbedding.Embedding),
                    new Npgsql.NpgsqlParameter("@threshold", request.Threshold),
                    new Npgsql.NpgsqlParameter("@limit", request.Limit))
                .ToListAsync();

            // Get total count for pagination
            var countSql = @"
                SELECT COUNT(*)
                FROM ""Hands"" h
                INNER JOIN ""HandEmbeddings"" he ON h.id = he.hand_id
                WHERE h.id != @handId
                  AND 1 - (he.embedding <=> @referenceEmbedding) >= @threshold";

            var totalCount = await _context.Database
                .SqlQueryRaw<int>(countSql,
                    new Npgsql.NpgsqlParameter("@handId", request.HandId),
                    new Npgsql.NpgsqlParameter("@referenceEmbedding", referenceHand.HandEmbedding.Embedding),
                    new Npgsql.NpgsqlParameter("@threshold", request.Threshold))
                .FirstAsync();

            return Ok(new PaginatedHandResponseDto
            {
                Hands = hands,
                TotalCount = totalCount,
                Offset = 0,
                Limit = request.Limit,
                HasMore = request.Limit < totalCount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error finding similar hands: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate a random hand from a deck
    /// </summary>
    /// <param name="deckId">Deck ID</param>
    /// <param name="handSize">Size of hand to generate (default 7)</param>
    /// <returns>Generated hand</returns>
    [HttpPost("generate")]
    public async Task<ActionResult<HandResponseDto>> GenerateRandomHand(
        [FromQuery] long deckId,
        [FromQuery] int handSize = 7)
    {
        if (deckId <= 0)
        {
            return BadRequest("Valid deck ID is required");
        }

        if (handSize <= 0 || handSize > 20)
        {
            return BadRequest("Hand size must be between 1 and 20");
        }

        try
        {
            var hand = await _handService.GenerateRandomHandAsync(deckId, handSize);
            
            return Ok(new HandResponseDto
            {
                Id = hand.Id,
                CardIntIds = hand.CardIntIds,
                Size = hand.Size,
                Hash64 = hand.Hash64,
                CanonicalKey = hand.CanonicalKey,
                CreatedAt = hand.CreatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error generating random hand: {ex.Message}");
        }
    }

    /// <summary>
    /// Get hand by ID
    /// </summary>
    /// <param name="id">Hand ID</param>
    /// <returns>Hand details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<HandResponseDto>> GetHand(long id)
    {
        try
        {
            var hand = await _context.Hands.FindAsync(id);
            
            if (hand == null)
            {
                return NotFound($"Hand {id} not found");
            }

            return Ok(new HandResponseDto
            {
                Id = hand.Id,
                CardIntIds = hand.CardIntIds,
                Size = hand.Size,
                Hash64 = hand.Hash64,
                CanonicalKey = hand.CanonicalKey,
                CreatedAt = hand.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving hand: {ex.Message}");
        }
    }
}


