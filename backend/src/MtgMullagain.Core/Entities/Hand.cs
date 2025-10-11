namespace MtgMullagain.Core.Entities;

/// <summary>
/// Order-independent card multiset representing a hand
/// </summary>
public class Hand
{
    /// <summary>
    /// Hand ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Array of card integer IDs for compact storage (sorted ascending, duplicates allowed)
    /// </summary>
    public int[] CardIntIds { get; set; } = Array.Empty<int>();
    
    /// <summary>
    /// Number of cards in the hand
    /// </summary>
    public short Size { get; set; }
    
    /// <summary>
    /// 64-bit hash for equality comparison (deterministic hash of sorted array)
    /// </summary>
    public long Hash64 { get; set; }
    
    /// <summary>
    /// Canonical key generated from sorted card_int_ids array
    /// </summary>
    public string CanonicalKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual HandEmbedding? HandEmbedding { get; set; }
    public virtual ICollection<HandEval> HandEvals { get; set; } = new List<HandEval>();
}
