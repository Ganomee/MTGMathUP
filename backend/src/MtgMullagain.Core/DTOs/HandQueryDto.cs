namespace MtgMullagain.Core.DTOs;

/// <summary>
/// DTO for hand query requests
/// </summary>
public class HandQueryDto
{
    /// <summary>
    /// Array of card integer IDs to search for
    /// </summary>
    public int[] CardIntIds { get; set; } = Array.Empty<int>();
    
    /// <summary>
    /// Minimum number of shared cards for overlap queries
    /// </summary>
    public int MinSharedCards { get; set; } = 1;
    
    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int Limit { get; set; } = 50;
    
    /// <summary>
    /// Offset for pagination
    /// </summary>
    public int Offset { get; set; } = 0;
}

/// <summary>
/// DTO for vector similarity search requests
/// </summary>
public class HandSimilarityDto
{
    /// <summary>
    /// Hand ID to find similar hands for
    /// </summary>
    public long HandId { get; set; }
    
    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int Limit { get; set; } = 10;
    
    /// <summary>
    /// Similarity threshold (0.0 to 1.0)
    /// </summary>
    public double Threshold { get; set; } = 0.5;
}

/// <summary>
/// DTO for hand response
/// </summary>
public class HandResponseDto
{
    /// <summary>
    /// Hand ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Array of card integer IDs
    /// </summary>
    public int[] CardIntIds { get; set; } = Array.Empty<int>();
    
    /// <summary>
    /// Number of cards in the hand
    /// </summary>
    public short Size { get; set; }
    
    /// <summary>
    /// Hash64 value
    /// </summary>
    public long Hash64 { get; set; }
    
    /// <summary>
    /// Canonical key string
    /// </summary>
    public string CanonicalKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Similarity score (for similarity queries)
    /// </summary>
    public double? SimilarityScore { get; set; }
    
    /// <summary>
    /// Number of shared cards (for overlap queries)
    /// </summary>
    public int? SharedCardsCount { get; set; }
}

/// <summary>
/// DTO for paginated hand results
/// </summary>
public class PaginatedHandResponseDto
{
    /// <summary>
    /// List of hands
    /// </summary>
    public List<HandResponseDto> Hands { get; set; } = new();
    
    /// <summary>
    /// Total count of matching hands
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Current page offset
    /// </summary>
    public int Offset { get; set; }
    
    /// <summary>
    /// Number of results per page
    /// </summary>
    public int Limit { get; set; }
    
    /// <summary>
    /// Whether there are more results
    /// </summary>
    public bool HasMore { get; set; }
}


