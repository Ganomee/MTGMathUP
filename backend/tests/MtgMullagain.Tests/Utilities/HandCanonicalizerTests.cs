using MtgMullagain.Core.Utilities;
using Xunit;

namespace MtgMullagain.Tests.Utilities;

/// <summary>
/// Unit tests for HandCanonicalizer
/// </summary>
public class HandCanonicalizerTests
{
    [Fact]
    public void Canonicalize_SortsCardIds()
    {
        // Arrange
        var unsortedIds = new[] { 3, 1, 4, 1, 5 };

        // Act
        var result = HandCanonicalizer.Canonicalize(unsortedIds);

        // Assert
        Assert.Equal(new[] { 1, 1, 3, 4, 5 }, result.CardIntIds);
        Assert.Equal(5, result.Size);
        Assert.Equal("1,1,3,4,5", result.CanonicalKey);
    }

    [Fact]
    public void Canonicalize_DeterministicHash()
    {
        // Arrange
        var ids1 = new[] { 1, 2, 3, 4, 5 };
        var ids2 = new[] { 5, 4, 3, 2, 1 }; // Same cards, different order

        // Act
        var result1 = HandCanonicalizer.Canonicalize(ids1);
        var result2 = HandCanonicalizer.Canonicalize(ids2);

        // Assert
        Assert.Equal(result1.Hash64, result2.Hash64);
        Assert.Equal(result1.CanonicalKey, result2.CanonicalKey);
        Assert.Equal(result1.CardIntIds, result2.CardIntIds);
    }

    [Fact]
    public void Canonicalize_EmptyArray_ThrowsException()
    {
        // Arrange
        var emptyIds = new int[0];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => HandCanonicalizer.Canonicalize(emptyIds));
    }

    [Fact]
    public void Canonicalize_NullArray_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => HandCanonicalizer.Canonicalize((int[])null!));
    }

    [Fact]
    public void Canonicalize_NullEnumerable_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => HandCanonicalizer.Canonicalize((IEnumerable<int>)null!));
    }

    [Fact]
    public void Canonicalize_WithDuplicates()
    {
        // Arrange
        var idsWithDuplicates = new[] { 2, 1, 2, 3, 1 };

        // Act
        var result = HandCanonicalizer.Canonicalize(idsWithDuplicates);

        // Assert
        Assert.Equal(new[] { 1, 1, 2, 2, 3 }, result.CardIntIds);
        Assert.Equal(5, result.Size);
        Assert.Equal("1,1,2,2,3", result.CanonicalKey);
    }

    [Fact]
    public void AreEquivalent_IdenticalHands_ReturnsTrue()
    {
        // Arrange
        var hand1 = new[] { 1, 2, 3, 4, 5 };
        var hand2 = new[] { 5, 4, 3, 2, 1 };

        // Act
        var result = HandCanonicalizer.AreEquivalent(hand1, hand2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreEquivalent_DifferentHands_ReturnsFalse()
    {
        // Arrange
        var hand1 = new[] { 1, 2, 3, 4, 5 };
        var hand2 = new[] { 1, 2, 3, 4, 6 };

        // Act
        var result = HandCanonicalizer.AreEquivalent(hand1, hand2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AreEquivalent_NullHands_ReturnsTrue()
    {
        // Act
        var result = HandCanonicalizer.AreEquivalent(null!, null!);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreEquivalent_OneNullHand_ReturnsFalse()
    {
        // Arrange
        var hand1 = new[] { 1, 2, 3 };
        int[]? hand2 = null;

        // Act
        var result = HandCanonicalizer.AreEquivalent(hand1, hand2!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsAll_HandContainsAllRequired_ReturnsTrue()
    {
        // Arrange
        var hand = new[] { 1, 2, 3, 4, 5 };
        var required = new[] { 2, 4 };

        // Act
        var result = HandCanonicalizer.ContainsAll(hand, required);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsAll_HandMissingRequired_ReturnsFalse()
    {
        // Arrange
        var hand = new[] { 1, 2, 3, 4, 5 };
        var required = new[] { 2, 6 };

        // Act
        var result = HandCanonicalizer.ContainsAll(hand, required);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsAll_EmptyRequired_ReturnsTrue()
    {
        // Arrange
        var hand = new[] { 1, 2, 3 };
        var required = new int[0];

        // Act
        var result = HandCanonicalizer.ContainsAll(hand, required);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CountSharedCards_NoSharedCards_ReturnsZero()
    {
        // Arrange
        var hand1 = new[] { 1, 2, 3 };
        var hand2 = new[] { 4, 5, 6 };

        // Act
        var result = HandCanonicalizer.CountSharedCards(hand1, hand2);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CountSharedCards_SomeSharedCards_ReturnsCorrectCount()
    {
        // Arrange
        var hand1 = new[] { 1, 2, 3, 4 };
        var hand2 = new[] { 2, 3, 5, 6 };

        // Act
        var result = HandCanonicalizer.CountSharedCards(hand1, hand2);

        // Assert
        Assert.Equal(2, result); // Cards 2 and 3 are shared
    }

    [Fact]
    public void CountSharedCards_AllSharedCards_ReturnsHandSize()
    {
        // Arrange
        var hand1 = new[] { 1, 2, 3 };
        var hand2 = new[] { 3, 2, 1 };

        // Act
        var result = HandCanonicalizer.CountSharedCards(hand1, hand2);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void CanonicalizedHand_ToString_ReturnsFormattedString()
    {
        // Arrange
        var canonical = HandCanonicalizer.Canonicalize(new[] { 1, 2, 3 });

        // Act
        var result = canonical.ToString();

        // Assert
        Assert.Contains("Size=3", result);
        Assert.Contains("Key='1,2,3'", result);
        Assert.Contains("Hash=", result);
    }

    [Theory]
    [InlineData(new[] { 1 }, new[] { 1 }, true)]
    [InlineData(new[] { 1, 2 }, new[] { 2, 1 }, true)]
    [InlineData(new[] { 1, 2, 3 }, new[] { 3, 1, 2 }, true)]
    [InlineData(new[] { 1, 2 }, new[] { 1, 3 }, false)]
    [InlineData(new[] { 1, 1, 2 }, new[] { 1, 2, 1 }, true)]
    public void Canonicalize_VariousInputs_ProducesConsistentResults(int[] input, int[] expected, bool shouldBeEqual)
    {
        // Act
        var result1 = HandCanonicalizer.Canonicalize(input);
        var result2 = HandCanonicalizer.Canonicalize(expected);

        // Assert
        if (shouldBeEqual)
        {
            // Both inputs should produce the same canonical hash and key
            Assert.Equal(result1.Hash64, result2.Hash64);
            Assert.Equal(result1.CanonicalKey, result2.CanonicalKey);
        }
        else
        {
            // Different inputs should produce different hashes
            Assert.NotEqual(result1.Hash64, result2.Hash64);
        }
    }
}


