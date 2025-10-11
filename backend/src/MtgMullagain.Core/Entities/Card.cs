namespace MtgMullagain.Core.Entities;

/// <summary>
/// Represents a functional MTG card with oracle_id as primary key
/// </summary>
public class Card
{
    /// <summary>
    /// Oracle ID from Scryfall (UUID) - Primary Key
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Card name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Mana cost
    /// </summary>
    public string ManaCost { get; set; } = string.Empty;
    
    /// <summary>
    /// Oracle text
    /// </summary>
    public string OracleText { get; set; } = string.Empty;
    
    /// <summary>
    /// Card type
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Power/Toughness for creatures
    /// </summary>
    public string? PowerToughness { get; set; }
    
    /// <summary>
    /// Converted mana cost
    /// </summary>
    public int Cmc { get; set; }
    
    /// <summary>
    /// Colors array
    /// </summary>
    public string[] Colors { get; set; } = Array.Empty<string>();
    
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
    public virtual CardIndex? CardIndex { get; set; }
    public virtual CardEmbedding? CardEmbedding { get; set; }
}
