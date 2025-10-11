using MtgMullagain.Core.Entities;

namespace MtgMullagain.Core.Services;

/// <summary>
/// Service for managing MTG hands with deduplication
/// </summary>
public interface IHandService
{
    /// <summary>
    /// Creates or retrieves an existing hand with deduplication
    /// </summary>
    /// <param name="cardIntIds">Array of card integer IDs</param>
    /// <returns>Existing or newly created hand</returns>
    Task<Hand> CreateOrGetHandAsync(int[] cardIntIds);
    
    /// <summary>
    /// Creates or retrieves an existing hand with deduplication
    /// </summary>
    /// <param name="cardIntIds">Collection of card integer IDs</param>
    /// <returns>Existing or newly created hand</returns>
    Task<Hand> CreateOrGetHandAsync(IEnumerable<int> cardIntIds);
    
    /// <summary>
    /// Finds hands that contain all specified cards
    /// </summary>
    /// <param name="requiredCards">Required card IDs</param>
    /// <returns>Hands containing all required cards</returns>
    Task<IEnumerable<Hand>> FindHandsContainingAsync(int[] requiredCards);
    
    /// <summary>
    /// Finds hands that share at least N cards with the given hand
    /// </summary>
    /// <param name="handId">Reference hand ID</param>
    /// <param name="minSharedCards">Minimum number of shared cards</param>
    /// <returns>Hands sharing at least N cards</returns>
    Task<IEnumerable<Hand>> FindSimilarHandsAsync(long handId, int minSharedCards = 3);
    
    /// <summary>
    /// Generates a random hand from a deck
    /// </summary>
    /// <param name="deckId">Deck ID</param>
    /// <param name="handSize">Size of hand to generate (default 7)</param>
    /// <returns>Random canonicalized hand</returns>
    Task<Hand> GenerateRandomHandAsync(long deckId, int handSize = 7);
}

