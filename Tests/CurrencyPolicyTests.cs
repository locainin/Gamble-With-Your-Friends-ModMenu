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

    [Theory]
    [InlineData("", true, "")]
    [InlineData("", false, "10000")]
    [InlineData("$1,250", true, "1250")]
    [InlineData("00042", true, "42")]
    [InlineData("abc", true, "")]
    [InlineData("abc", false, "10000")]
    public void NormalizeMoneyInputKeepsEditingRecoverable(
        string rawValue,
        bool isFocused,
        string expected)
    {
        Assert.Equal(expected, CurrencyPolicy.NormalizeMoneyInput(rawValue, isFocused));
    }

    [Fact]
    public void NormalizeMoneyInputClampsOverflowToLongMaximum()
    {
        string maximum = long.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string tooLarge = maximum + "9";

        Assert.Equal(maximum, CurrencyPolicy.NormalizeMoneyInput(tooLarge, isFocused: true));
    }

    [Theory]
    [InlineData("1", true, 1L)]
    [InlineData("0", false, 0L)]
    [InlineData("", false, 0L)]
    public void TryParsePositiveAmountRejectsNonPositiveValues(
        string rawValue,
        bool expected,
        long expectedAmount)
    {
        bool parsed = CurrencyPolicy.TryParsePositiveAmount(rawValue, out long amount);

        Assert.Equal(expected, parsed);
        Assert.Equal(expectedAmount, amount);
    }
}
