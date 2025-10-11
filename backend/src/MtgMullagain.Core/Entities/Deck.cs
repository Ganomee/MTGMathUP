namespace MtgMullagain.Core.Entities;

/// <summary>
/// User deck representation
/// </summary>
public class Deck
{
    /// <summary>
    /// Deck ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Deck name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Deck format (Standard, Modern, etc.)
    /// </summary>
    public string Format { get; set; } = string.Empty;
    
    /// <summary>
    /// Deck owner (for future user system)
    /// </summary>
    public string? Owner { get; set; }
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<DeckCard> DeckCards { get; set; } = new List<DeckCard>();
}
