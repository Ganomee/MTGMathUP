namespace MtgMullagain.Core.Entities;

/// <summary>
/// Deck composition - many-to-many relationship between Deck and Card
/// </summary>
public class DeckCard
{
    /// <summary>
    /// Deck ID
    /// </summary>
    public long DeckId { get; set; }
    
    /// <summary>
    /// Card ID (oracle_id)
    /// </summary>
    public Guid CardId { get; set; }
    
    /// <summary>
    /// Number of copies in the deck
    /// </summary>
    public int Count { get; set; }
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Deck Deck { get; set; } = null!;
    public virtual Card Card { get; set; } = null!;
}
