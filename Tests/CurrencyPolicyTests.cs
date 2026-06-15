namespace ModMenu.Tests;

public sealed class CurrencyPolicyTests
{
    // Representative values cover disabled, normal, and invalid gain paths
    [Theory]
    [InlineData(100L, 1, 0L)]
    [InlineData(100L, 2, 100L)]
    [InlineData(100L, 3, 200L)]
    [InlineData(0L, 10, 0L)]
    [InlineData(-100L, 10, 0L)]
    public void CalculateMultiplierBonusReturnsOnlyTheAdditionalGain(
        long balanceGain,
        int multiplier,
        long expected)
    {
        Assert.Equal(expected, CurrencyPolicy.CalculateMultiplierBonus(balanceGain, multiplier));
    }

    [Fact]
    public void CalculateMultiplierBonusPreservesPrecisionForLargeBalances()
    {
        const long gain = 999_999_999_999_999_999L;

        Assert.Equal(gain, CurrencyPolicy.CalculateMultiplierBonus(gain, 2m));
    }

    [Fact]
    public void CalculateMultiplierBonusClampsOverflowToLongMaximum()
    {
        Assert.Equal(long.MaxValue, CurrencyPolicy.CalculateMultiplierBonus(long.MaxValue, 10m));
    }
}
