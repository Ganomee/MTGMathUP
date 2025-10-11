using Standart.Hash.xxHash;

namespace MtgMullagain.Core.Utilities;

/// <summary>
/// Utility for canonicalizing MTG hands to ensure consistent representation and enable deduplication
/// </summary>
public static class HandCanonicalizer
{
    /// <summary>
    /// Canonicalizes a hand by sorting card IDs and computing deterministic hash
    /// </summary>
    /// <param name="cardIntIds">Array of card integer IDs (may be unsorted)</param>
    /// <returns>Canonicalized hand data</returns>
    public static CanonicalizedHand Canonicalize(int[] cardIntIds)
    {
        if (cardIntIds == null || cardIntIds.Length == 0)
        {
            throw new ArgumentException("Card IDs array cannot be null or empty", nameof(cardIntIds));
        }

        // Create a copy to avoid modifying the original array
        var sortedIds = new int[cardIntIds.Length];
        Array.Copy(cardIntIds, sortedIds, cardIntIds.Length);
        
        // Sort the array in ascending order for canonical representation
        Array.Sort(sortedIds);
        
        // Compute deterministic xxHash64 of the sorted sequence
        var hash = ComputeHash(sortedIds);
        
        // Generate canonical key string
        var canonicalKey = string.Join(",", sortedIds);
        
        return new CanonicalizedHand
        {
            CardIntIds = sortedIds,
            Size = (short)sortedIds.Length,
            Hash64 = hash,
            CanonicalKey = canonicalKey
        };
    }

    /// <summary>
    /// Canonicalizes a hand from a collection of card IDs
    /// </summary>
    /// <param name="cardIntIds">Collection of card integer IDs</param>
    /// <returns>Canonicalized hand data</returns>
    public static CanonicalizedHand Canonicalize(IEnumerable<int> cardIntIds)
    {
        if (cardIntIds == null)
        {
            throw new ArgumentNullException(nameof(cardIntIds));
        }

        var idsArray = cardIntIds.ToArray();
        return Canonicalize(idsArray);
    }

    /// <summary>
    /// Computes xxHash64 hash of the sorted card ID array
    /// </summary>
    /// <param name="sortedCardIds">Sorted array of card IDs</param>
    /// <returns>64-bit hash value</returns>
    private static long ComputeHash(int[] sortedCardIds)
    {
        // Convert int array to byte array for hashing
        var bytes = new byte[sortedCardIds.Length * sizeof(int)];
        Buffer.BlockCopy(sortedCardIds, 0, bytes, 0, bytes.Length);
        
        // Compute xxHash64 with seed 0 for deterministic results
        var hash = xxHash64.ComputeHash(bytes, bytes.Length, 0);
        
        return (long)hash;
    }

    /// <summary>
    /// Checks if two hands are equivalent (same canonical representation)
    /// </summary>
    /// <param name="hand1">First hand card IDs</param>
    /// <param name="hand2">Second hand card IDs</param>
    /// <returns>True if hands are equivalent</returns>
    public static bool AreEquivalent(int[] hand1, int[] hand2)
    {
        if (hand1 == null && hand2 == null) return true;
        if (hand1 == null || hand2 == null) return false;
        if (hand1.Length != hand2.Length) return false;

        var canonical1 = Canonicalize(hand1);
        var canonical2 = Canonicalize(hand2);
        
        return canonical1.Hash64 == canonical2.Hash64;
    }

    /// <summary>
    /// Checks if a hand contains all cards from another hand
    /// </summary>
    /// <param name="hand">The hand to check</param>
    /// <param name="requiredCards">Required cards</param>
    /// <returns>True if hand contains all required cards</returns>
    public static bool ContainsAll(int[] hand, int[] requiredCards)
    {
        if (hand == null || requiredCards == null) return false;
        if (requiredCards.Length == 0) return true;
        if (hand.Length < requiredCards.Length) return false;

        var canonicalHand = Canonicalize(hand);
        var canonicalRequired = Canonicalize(requiredCards);
        
        // Check if hand contains all required cards
        var handSet = new HashSet<int>(canonicalHand.CardIntIds);
        return canonicalRequired.CardIntIds.All(cardId => handSet.Contains(cardId));
    }

    /// <summary>
    /// Counts the number of shared cards between two hands
    /// </summary>
    /// <param name="hand1">First hand</param>
    /// <param name="hand2">Second hand</param>
    /// <returns>Number of shared cards</returns>
    public static int CountSharedCards(int[] hand1, int[] hand2)
    {
        if (hand1 == null || hand2 == null) return 0;

        var canonical1 = Canonicalize(hand1);
        var canonical2 = Canonicalize(hand2);
        
        var set1 = new HashSet<int>(canonical1.CardIntIds);
        return canonical2.CardIntIds.Count(cardId => set1.Contains(cardId));
    }
}

/// <summary>
/// Represents a canonicalized hand with computed properties
/// </summary>
public class CanonicalizedHand
{
    /// <summary>
    /// Sorted array of card integer IDs
    /// </summary>
    public int[] CardIntIds { get; set; } = Array.Empty<int>();
    
    /// <summary>
    /// Number of cards in the hand
    /// </summary>
    public short Size { get; set; }
    
    /// <summary>
    /// Deterministic xxHash64 of the sorted card sequence
    /// </summary>
    public long Hash64 { get; set; }
    
    /// <summary>
    /// Canonical key string representation
    /// </summary>
    public string CanonicalKey { get; set; } = string.Empty;
    
    /// <summary>
    /// String representation for debugging
    /// </summary>
    public override string ToString()
    {
        return $"Hand[Size={Size}, Hash={Hash64:X16}, Key='{CanonicalKey}']";
    }
}

