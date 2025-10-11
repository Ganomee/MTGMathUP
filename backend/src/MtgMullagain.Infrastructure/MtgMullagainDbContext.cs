using Microsoft.EntityFrameworkCore;
using MtgMullagain.Core.Entities;
using Pgvector.EntityFrameworkCore;

namespace MtgMullagain.Infrastructure;

/// <summary>
/// Entity Framework DbContext for MTG Mullagain
/// </summary>
public class MtgMullagainDbContext : DbContext
{
    public MtgMullagainDbContext(DbContextOptions<MtgMullagainDbContext> options) : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<Card> Cards { get; set; } = null!;
    public DbSet<CardIndex> CardIndexes { get; set; } = null!;
    public DbSet<Deck> Decks { get; set; } = null!;
    public DbSet<DeckCard> DeckCards { get; set; } = null!;
    public DbSet<Hand> Hands { get; set; } = null!;
    public DbSet<HandEval> HandEvals { get; set; } = null!;
    public DbSet<CardEmbedding> CardEmbeddings { get; set; } = null!;
    public DbSet<HandEmbedding> HandEmbeddings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // Configure Card entity
        modelBuilder.Entity<Card>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ManaCost).HasMaxLength(50);
            entity.Property(e => e.OracleText).HasColumnType("text");
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PowerToughness).HasMaxLength(10);
            entity.Property(e => e.Colors).HasColumnType("text[]");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            
            // Indexes
            entity.HasIndex(e => e.Name).HasDatabaseName("IX_Cards_Name");
            entity.HasIndex(e => e.Type).HasDatabaseName("IX_Cards_Type");
            entity.HasIndex(e => e.Cmc).HasDatabaseName("IX_Cards_Cmc");
        });

        // Configure CardIndex entity
        modelBuilder.Entity<CardIndex>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            
            // Unique index on CardId
            entity.HasIndex(e => e.CardId).IsUnique().HasDatabaseName("IX_CardIndexes_CardId");
            
            // Foreign key relationship
            entity.HasOne(e => e.Card)
                  .WithOne(e => e.CardIndex)
                  .HasForeignKey<CardIndex>(e => e.CardId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Deck entity
        modelBuilder.Entity<Deck>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Format).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Owner).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            
            // Indexes
            entity.HasIndex(e => e.Name).HasDatabaseName("IX_Decks_Name");
            entity.HasIndex(e => e.Format).HasDatabaseName("IX_Decks_Format");
            entity.HasIndex(e => e.Owner).HasDatabaseName("IX_Decks_Owner");
        });

        // Configure DeckCard entity (many-to-many)
        modelBuilder.Entity<DeckCard>(entity =>
        {
            entity.HasKey(e => new { e.DeckId, e.CardId });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            
            // Foreign key relationships
            entity.HasOne(e => e.Deck)
                  .WithMany(e => e.DeckCards)
                  .HasForeignKey(e => e.DeckId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Card)
                  .WithMany(e => e.DeckCards)
                  .HasForeignKey(e => e.CardId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Hand entity
        modelBuilder.Entity<Hand>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CardIntIds).HasColumnType("integer[]");
            entity.Property(e => e.Size).HasColumnType("smallint");
            entity.Property(e => e.CanonicalKey)
                  .HasComputedColumnSql("array_to_string(\"CardIntIds\", ',')", stored: true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            
            // Unique constraint for deduplication (hash64, size, card_int_ids)
            entity.HasIndex(e => new { e.Hash64, e.Size, e.CardIntIds })
                  .IsUnique()
                  .HasDatabaseName("IX_Hands_Hash64_Size_CardIntIds_Unique");
            
            // B-tree index on Hash64 for deduplication
            entity.HasIndex(e => e.Hash64)
                  .HasDatabaseName("IX_Hands_Hash64");
            
            // GIN index for array containment/overlap search
            entity.HasIndex(e => e.CardIntIds)
                  .HasMethod("gin")
                  .HasDatabaseName("IX_Hands_CardIntIds_GIN");
            
            // Index on created_at for temporal queries
            entity.HasIndex(e => e.CreatedAt)
                  .HasDatabaseName("IX_Hands_CreatedAt");
        });

        // Configure HandEval entity
        modelBuilder.Entity<HandEval>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ContextJson).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            
            // Indexes
            entity.HasIndex(e => e.Hand1Id).HasDatabaseName("IX_HandEvals_Hand1Id");
            entity.HasIndex(e => e.Hand2Id).HasDatabaseName("IX_HandEvals_Hand2Id");
            entity.HasIndex(e => e.PreferredHand).HasDatabaseName("IX_HandEvals_PreferredHand");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_HandEvals_CreatedAt");
            
            // Foreign key relationships
            entity.HasOne(e => e.Hand1)
                  .WithMany()
                  .HasForeignKey(e => e.Hand1Id)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Hand2)
                  .WithMany()
                  .HasForeignKey(e => e.Hand2Id)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CardEmbedding entity
        modelBuilder.Entity<CardEmbedding>(entity =>
        {
            entity.HasKey(e => e.CardId);
            entity.Property(e => e.Embedding).HasColumnType("vector(256)");
            entity.Property(e => e.ModelVersion).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            
            // Indexes
            entity.HasIndex(e => e.ModelVersion).HasDatabaseName("IX_CardEmbeddings_ModelVersion");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_CardEmbeddings_CreatedAt");
            
            // IVFFlat index for vector similarity search
            entity.HasIndex(e => e.Embedding)
                  .HasMethod("ivfflat")
                  .HasOperators("vector_cosine_ops")
                  .HasDatabaseName("IX_CardEmbeddings_Embedding_IVFFlat");
            
            // Foreign key relationship
            entity.HasOne(e => e.Card)
                  .WithOne(e => e.CardEmbedding)
                  .HasForeignKey<CardEmbedding>(e => e.CardId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure HandEmbedding entity
        modelBuilder.Entity<HandEmbedding>(entity =>
        {
            entity.HasKey(e => e.HandId);
            entity.Property(e => e.Embedding).HasColumnType("vector(256)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            
            // IVFFlat index for vector similarity search
            entity.HasIndex(e => e.Embedding)
                  .HasMethod("ivfflat")
                  .HasOperators("vector_cosine_ops")
                  .HasDatabaseName("IX_HandEmbeddings_Embedding_IVFFlat");
            
            // Foreign key relationship
            entity.HasOne(e => e.Hand)
                  .WithOne(e => e.HandEmbedding)
                  .HasForeignKey<HandEmbedding>(e => e.HandId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
