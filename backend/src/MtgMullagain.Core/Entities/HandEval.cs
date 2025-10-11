namespace MtgMullagain.Core.Entities;

/// <summary>
/// User preference evaluation between two hands
/// </summary>
public class HandEval
{
    /// <summary>
    /// Evaluation ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// First hand ID
    /// </summary>
    public long Hand1Id { get; set; }
    
    /// <summary>
    /// Second hand ID
    /// </summary>
    public long Hand2Id { get; set; }
    
    /// <summary>
    /// Which hand was preferred (1 or 2)
    /// </summary>
    public int PreferredHand { get; set; }
    
    /// <summary>
    /// Context JSON for additional evaluation data
    /// </summary>
    public string ContextJson { get; set; } = "{}";
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Hand Hand1 { get; set; } = null!;
    public virtual Hand Hand2 { get; set; } = null!;
}
