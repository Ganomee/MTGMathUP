using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MtgMullagain.Core.Entities;
using MtgMullagain.Core.Services;
using MtgMullagain.Infrastructure;
using Xunit;

namespace MtgMullagain.Tests.Services;

/// <summary>
/// Unit tests for HandService
/// </summary>
public class HandServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly MtgMullagainDbContext _context;
    private readonly IHandService _handService;

    public HandServiceTests()
    {
        // Setup in-memory database for testing
        var services = new ServiceCollection();
        services.AddDbContext<MtgMullagainDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<IHandService, HandService>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<MtgMullagainDbContext>();
        _handService = _serviceProvider.GetRequiredService<IHandService>();

        // Create test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test cards
        var card1 = new Card
        {
            Id = Guid.NewGuid(),
            Name = "Lightning Bolt",
            ManaCost = "{R}",
            OracleText = "Lightning Bolt deals 3 damage to any target.",
            Type = "Instant",
            Cmc = 1,
            Colors = new[] { "R" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var card2 = new Card
        {
            Id = Guid.NewGuid(),
            Name = "Counterspell",
            ManaCost = "{U}{U}",
            OracleText = "Counter target spell.",
            Type = "Instant",
            Cmc = 2,
            Colors = new[] { "U" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var card3 = new Card
        {
            Id = Guid.NewGuid(),
            Name = "Brainstorm",
            ManaCost = "{U}",
            OracleText = "Draw three cards, then put two cards from your hand on top of your library in any order.",
            Type = "Instant",
            Cmc = 1,
            Colors = new[] { "U" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Cards.AddRange(card1, card2, card3);

        // Create card indexes
        var cardIndex1 = new CardIndex { Id = 1, CardId = card1.Id, CreatedAt = DateTime.UtcNow };
        var cardIndex2 = new CardIndex { Id = 2, CardId = card2.Id, CreatedAt = DateTime.UtcNow };
        var cardIndex3 = new CardIndex { Id = 3, CardId = card3.Id, CreatedAt = DateTime.UtcNow };

        _context.CardIndexes.AddRange(cardIndex1, cardIndex2, cardIndex3);

        // Create test deck
        var deck = new Deck
        {
            Id = 1,
            Name = "Test Deck",
            Format = "Legacy",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Decks.Add(deck);

        // Create deck cards
        var deckCard1 = new DeckCard { DeckId = 1, CardId = card1.Id, Count = 4, CreatedAt = DateTime.UtcNow };
        var deckCard2 = new DeckCard { DeckId = 1, CardId = card2.Id, Count = 4, CreatedAt = DateTime.UtcNow };
        var deckCard3 = new DeckCard { DeckId = 1, CardId = card3.Id, Count = 4, CreatedAt = DateTime.UtcNow };

        _context.DeckCards.AddRange(deckCard1, deckCard2, deckCard3);

        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateOrGetHandAsync_NewHand_CreatesHand()
    {
        // Arrange
        var cardIds = new[] { 1, 2, 3 };

        // Act
        var hand = await _handService.CreateOrGetHandAsync(cardIds);

        // Assert
        Assert.NotNull(hand);
        Assert.Equal(cardIds.OrderBy(x => x), hand.CardIntIds);
        Assert.Equal(3, hand.Size);
        Assert.NotEqual(0, hand.Hash64);
        Assert.Equal("1,2,3", hand.CanonicalKey);
    }

    [Fact]
    public async Task CreateOrGetHandAsync_DuplicateHand_ReturnsExisting()
    {
        // Arrange
        var cardIds1 = new[] { 1, 2, 3 };
        var cardIds2 = new[] { 3, 1, 2 }; // Same cards, different order

        // Act
        var hand1 = await _handService.CreateOrGetHandAsync(cardIds1);
        var hand2 = await _handService.CreateOrGetHandAsync(cardIds2);

        // Assert
        Assert.Equal(hand1.Id, hand2.Id);
        Assert.Equal(hand1.Hash64, hand2.Hash64);
    }

    [Fact]
    public async Task CreateOrGetHandAsync_EmptyArray_ThrowsException()
    {
        // Arrange
        var emptyIds = new int[0];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handService.CreateOrGetHandAsync(emptyIds));
    }

    [Fact]
    public async Task CreateOrGetHandAsync_NullArray_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handService.CreateOrGetHandAsync((int[])null!));
    }

    [Fact]
    public async Task FindHandsContainingAsync_ValidCards_ReturnsMatchingHands()
    {
        // Arrange
        var hand1 = await _handService.CreateOrGetHandAsync(new[] { 1, 2, 3 });
        var hand2 = await _handService.CreateOrGetHandAsync(new[] { 1, 2 });
        var hand3 = await _handService.CreateOrGetHandAsync(new[] { 2, 3 });

        // Act
        var results = await _handService.FindHandsContainingAsync(new[] { 1, 2 });

        // Assert
        Assert.Contains(results, h => h.Id == hand1.Id);
        Assert.Contains(results, h => h.Id == hand2.Id);
        Assert.DoesNotContain(results, h => h.Id == hand3.Id);
    }

    [Fact]
    public async Task FindSimilarHandsAsync_ValidHand_ReturnsSimilarHands()
    {
        // Arrange
        var hand1 = await _handService.CreateOrGetHandAsync(new[] { 1, 2, 3 });
        var hand2 = await _handService.CreateOrGetHandAsync(new[] { 1, 2, 4 });
        var hand3 = await _handService.CreateOrGetHandAsync(new[] { 5, 6, 7 });

        // Act
        var results = await _handService.FindSimilarHandsAsync(hand1.Id, 2);

        // Assert
        Assert.Contains(results, h => h.Id == hand2.Id);
        Assert.DoesNotContain(results, h => h.Id == hand3.Id);
    }

    [Fact]
    public async Task GenerateRandomHandAsync_ValidDeck_ReturnsHand()
    {
        // Act
        var hand = await _handService.GenerateRandomHandAsync(1, 7);

        // Assert
        Assert.NotNull(hand);
        Assert.Equal(7, hand.Size);
        Assert.True(hand.CardIntIds.All(id => id >= 1 && id <= 3));
    }

    [Fact]
    public async Task GenerateRandomHandAsync_InvalidDeck_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handService.GenerateRandomHandAsync(999, 7));
    }

    [Fact]
    public async Task GenerateRandomHandAsync_TooLargeHand_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handService.GenerateRandomHandAsync(1, 100));
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }
}


