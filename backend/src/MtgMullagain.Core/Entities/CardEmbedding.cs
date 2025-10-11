namespace MtgMullagain.Core.Entities;

/// <summary>
/// 256-dimensional vector representation of a card
/// </summary>
public class CardEmbedding
{
    /// <summary>
    /// Card ID (oracle_id)
    /// </summary>
    public Guid CardId { get; set; }
    
    /// <summary>
    /// 256-dimensional embedding vector
    /// </summary>
    public float[] Embedding { get; set; } = Array.Empty<float>();
    
    /// <summary>
    /// Model version used for embedding
    /// </summary>
    public string ModelVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Card Card { get; set; } = null!;
}
