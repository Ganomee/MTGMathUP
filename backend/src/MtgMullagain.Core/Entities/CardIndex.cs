namespace MtgMullagain.Core.Entities;

/// <summary>
/// Small integer surrogate IDs for compact hand storage
/// </summary>
public class CardIndex
{
    /// <summary>
    /// Dense integer ID for efficient storage
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Reference to the card's oracle ID
    /// </summary>
    public Guid CardId { get; set; }
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Card Card { get; set; } = null!;
}
