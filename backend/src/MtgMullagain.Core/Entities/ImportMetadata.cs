using System;
using System.ComponentModel.DataAnnotations;

namespace MtgMullagain.Core.Entities;

/// <summary>
/// Tracks metadata about card imports from external sources
/// </summary>
public class ImportMetadata
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of import (e.g., "ScryfallOracleCards")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// When we imported the data
    /// </summary>
    [Required]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the source data was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Download URI used
    /// </summary>
    [MaxLength(500)]
    public string? DownloadUri { get; set; }
}
