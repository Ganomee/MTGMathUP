using Microsoft.EntityFrameworkCore;
using MtgMullagain.Core.Entities;
using MtgMullagain.Core.Utilities;
using MtgMullagain.Infrastructure;

namespace MtgMullagain.Core.Services;

/// <summary>
/// Service for managing MTG hands with deduplication
/// </summary>
public class HandService : IHandService
{
    private readonly MtgMullagainDbContext _context;

    public HandService(MtgMullagainDbContext context)
    {
        _context = context;
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

        // Check if hand already exists using the unique constraint
        var existingHand = await _context.Hands
            .FirstOrDefaultAsync(h => 
                h.Hash64 == canonical.Hash64 && 
                h.Size == canonical.Size && 
                h.CardIntIds.SequenceEqual(canonical.CardIntIds));

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
        var idsArray = cardIntIds?.ToArray() ?? throw new ArgumentNullException(nameof(cardIntIds));
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

        // Use PostgreSQL array containment operator
        var hands = await _context.Hands
            .Where(h => h.CardIntIds.Contains(requiredCards))
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        return hands;
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
            return Enumerable.Empty<Hand>();
        }

        // Use PostgreSQL array overlap operator and length function
        var hands = await _context.Hands
            .Where(h => h.Id != handId && 
                       h.CardIntIds.Overlaps(referenceHand.CardIntIds) &&
                       h.CardIntIds.Intersect(referenceHand.CardIntIds).Count() >= minSharedCards)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        return hands;
    }

    /// <summary>
    /// Generates a random hand from a deck
    /// </summary>
    /// <param name="deckId">Deck ID</param>
    /// <param name="handSize">Size of hand to generate (default 7)</param>
    /// <returns>Random canonicalized hand</returns>
    public async Task<Hand> GenerateRandomHandAsync(long deckId, int handSize = 7)
    {
        // Get deck composition
        var deckCards = await _context.DeckCards
            .Where(dc => dc.DeckId == deckId)
            .Include(dc => dc.Card)
            .ThenInclude(c => c.CardIndex)
            .ToListAsync();

        if (!deckCards.Any())
        {
            throw new InvalidOperationException($"Deck {deckId} not found or has no cards");
        }

        // Build weighted list of cards
        var weightedCards = new List<int>();
        foreach (var deckCard in deckCards)
        {
            if (deckCard.Card.CardIndex != null)
            {
                // Add card multiple times based on count in deck
                for (int i = 0; i < deckCard.Count; i++)
                {
                    weightedCards.Add(deckCard.Card.CardIndex.Id);
                }
            }
        }

        if (weightedCards.Count < handSize)
        {
            throw new InvalidOperationException($"Deck {deckId} has only {weightedCards.Count} cards, cannot draw {handSize}");
        }

        // Randomly select cards for hand
        var random = new Random();
        var selectedCards = new List<int>();
        var availableCards = new List<int>(weightedCards);

        for (int i = 0; i < handSize; i++)
        {
            var randomIndex = random.Next(availableCards.Count);
            selectedCards.Add(availableCards[randomIndex]);
            availableCards.RemoveAt(randomIndex);
        }

        // Create or get canonicalized hand
        return await CreateOrGetHandAsync(selectedCards.ToArray());
    }
}


