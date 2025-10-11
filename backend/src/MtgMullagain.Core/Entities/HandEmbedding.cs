namespace MtgMullagain.Core.Entities;

/// <summary>
/// Derived hand vector representation
/// </summary>
public class HandEmbedding
{
    /// <summary>
    /// Hand ID
    /// </summary>
    public long HandId { get; set; }
    
    /// <summary>
    /// 256-dimensional embedding vector
    /// </summary>
    public float[] Embedding { get; set; } = Array.Empty<float>();
    
    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Hand Hand { get; set; } = null!;
}
