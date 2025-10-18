using Microsoft.EntityFrameworkCore;
using MtgMullagain.Core.Entities;
using MtgMullagain.Core.Services;
using MtgMullagain.Core.Utilities;

namespace MtgMullagain.Infrastructure.Services;

/// <summary>
/// Service for managing MTG hands with deduplication
/// </summary>
public class HandService : IHandService
{
    private readonly MtgMullagainDbContext _context;

    public HandService(MtgMullagainDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates or retrieves an existing hand with deduplication
    /// </summary>
    /// <param name="cardIntIds">Array of card integer IDs</param>
    /// <returns>Existing or newly created hand</returns>
    public async Task<Hand> CreateOrGetHandAsync(int[] cardIntIds)
    {
        if (cardIntIds == null || cardIntIds.Length == 0)
        {
            throw new ArgumentException("Card IDs array cannot be null or empty", nameof(cardIntIds));
        }

        // Canonicalize the hand
        var canonical = HandCanonicalizer.Canonicalize(cardIntIds);

        // Try to find existing hand by hash and canonical key
        var existingHand = await _context.Hands
            .FirstOrDefaultAsync(h => 
                h.Hash64 == canonical.Hash64 && 
                h.CanonicalKey == canonical.CanonicalKey);

        if (existingHand != null)
        {
            return existingHand;
        }

        // Create new hand
        var newHand = new Hand
        {
            CardIntIds = canonical.CardIntIds,
            Size = canonical.Size,
            Hash64 = canonical.Hash64,
            CanonicalKey = canonical.CanonicalKey,
            CreatedAt = DateTime.UtcNow
        };

        _context.Hands.Add(newHand);
        await _context.SaveChangesAsync();

        return newHand;
    }

    /// <summary>
    /// Creates or retrieves an existing hand with deduplication
    /// </summary>
    /// <param name="cardIntIds">Collection of card integer IDs</param>
    /// <returns>Existing or newly created hand</returns>
    public async Task<Hand> CreateOrGetHandAsync(IEnumerable<int> cardIntIds)
    {
        if (cardIntIds == null)
        {
            throw new ArgumentException("Card IDs collection cannot be null", nameof(cardIntIds));
        }

        var idsArray = cardIntIds.ToArray();
        return await CreateOrGetHandAsync(idsArray);
    }

    /// <summary>
    /// Finds hands that contain all specified cards
    /// </summary>
    /// <param name="requiredCards">Required card IDs</param>
    /// <returns>Hands containing all required cards</returns>
    public async Task<IEnumerable<Hand>> FindHandsContainingAsync(int[] requiredCards)
    {
        if (requiredCards == null || requiredCards.Length == 0)
        {
            return Enumerable.Empty<Hand>();
        }

        // Canonicalize required cards to ensure proper ordering
        var canonical = HandCanonicalizer.Canonicalize(requiredCards);
        var requiredSet = canonical.CardIntIds.ToHashSet();

        // Get all hands and filter in memory
        // Note: In production with PostgreSQL, this should use array containment operator (@>)
        // For now, using LINQ for compatibility with in-memory databases
        var allHands = await _context.Hands.ToListAsync();
        var matchingHands = allHands
            .Where(h => requiredSet.IsSubsetOf(h.CardIntIds))
            .ToList();

        return matchingHands;
    }

    /// <summary>
    /// Finds hands that share at least N cards with the given hand
    /// </summary>
    /// <param name="handId">Reference hand ID</param>
    /// <param name="minSharedCards">Minimum number of shared cards</param>
    /// <returns>Hands sharing at least N cards</returns>
    public async Task<IEnumerable<Hand>> FindSimilarHandsAsync(long handId, int minSharedCards = 3)
    {
        var referenceHand = await _context.Hands.FindAsync(handId);
        if (referenceHand == null)
        {
            throw new InvalidOperationException($"Hand with ID {handId} not found");
        }

        // Get all hands except the reference hand
        // Note: In production with PostgreSQL, this should use array overlap operator (&&)
        // For now, using LINQ for compatibility with in-memory databases
        var allHands = await _context.Hands
            .Where(h => h.Id != handId)
            .ToListAsync();

        // Filter by minimum shared cards count
        var similarHands = allHands
            .Where(h => HandCanonicalizer.CountSharedCards(
                referenceHand.CardIntIds, 
                h.CardIntIds) >= minSharedCards)
            .ToList();

        return similarHands;
    }

    /// <summary>
    /// Generates a random hand from a deck
    /// </summary>
    /// <param name="deckId">Deck ID</param>
    /// <param name="handSize">Size of hand to generate (default 7)</param>
    /// <returns>Random canonicalized hand</returns>
    public async Task<Hand> GenerateRandomHandAsync(long deckId, int handSize = 7)
    {
        // Get deck cards
        var deckCards = await _context.DeckCards
            .Where(dc => dc.DeckId == deckId)
            .Include(dc => dc.Card)
            .ToListAsync();

        if (!deckCards.Any())
        {
            throw new InvalidOperationException($"Deck with ID {deckId} not found or has no cards");
        }

        // Build card pool (expand based on count)
        var cardPool = new List<int>();
        foreach (var deckCard in deckCards)
        {
            var cardIndex = await _context.CardIndexes
                .Where(ci => ci.CardId == deckCard.CardId)
                .Select(ci => ci.Id)
                .FirstOrDefaultAsync();

            if (cardIndex != 0)
            {
                for (int i = 0; i < deckCard.Count; i++)
                {
                    cardPool.Add(cardIndex);
                }
            }
        }

        if (cardPool.Count < handSize)
        {
            throw new InvalidOperationException(
                $"Deck has only {cardPool.Count} cards, cannot generate hand of size {handSize}");
        }

        // Shuffle and select cards
        var random = new Random();
        var shuffled = cardPool.OrderBy(x => random.Next()).ToArray();
        var selectedCards = shuffled.Take(handSize).ToArray();

        // Create or get the hand
        return await CreateOrGetHandAsync(selectedCards);
    }
}

