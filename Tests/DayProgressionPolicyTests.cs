using ModMenu;

namespace ModMenu.Tests;

public sealed class DayProgressionPolicyTests
{
    private static readonly long[] FloorThresholds = new long[] { 0L, 3L, 7L, 12L };

    [Theory]
    [InlineData(-10, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(42, 42)]
    [InlineData(999, 999)]
    [InlineData(1000, 999)]
    public void ClampDisplayDayReturnsSupportedRange(int requestedDay, int expectedDay)
    {
        int result = DayProgressionPolicy.ClampDisplayDay(requestedDay);

        Assert.Equal(expectedDay, result);
    }

    [Theory]
    [InlineData(1, 0, 3, 0)]
    [InlineData(2, 1, 2, 0)]
    [InlineData(3, 2, 1, 0)]
    [InlineData(4, 3, 3, 1)]
    [InlineData(7, 6, 3, 2)]
    [InlineData(10, 9, 3, 3)]
    public void CalculateReconstructsQuotaCycle(int displayDay, int expectedPassed, int expectedLeft, int expectedQuotas)
    {
        DayProgression result = DayProgressionPolicy.Calculate(displayDay, 3, FloorThresholds);

        Assert.Equal(expectedPassed, result.DaysPassed);
        Assert.Equal(expectedLeft, result.DaysLeft);
        Assert.Equal(expectedQuotas, result.SuccessfulQuotas);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    [InlineData(6, 1)]
    [InlineData(7, 2)]
    [InlineData(11, 2)]
    [InlineData(12, 3)]
    [InlineData(100, 3)]
    public void CalculateFloorMatchesThresholdProgression(int daysPassed, int expectedFloor)
    {
        int result = DayProgressionPolicy.CalculateFloor(daysPassed, FloorThresholds);

        Assert.Equal(expectedFloor, result);
    }

    [Fact]
    public void CalculateWithZeroCycleLengthUsesOneDayCycle()
    {
        DayProgression result = DayProgressionPolicy.Calculate(5, 0, FloorThresholds);

        Assert.Equal(4, result.SuccessfulQuotas);
        Assert.Equal(1, result.DaysLeft);
    }

    [Fact]
    public void CalculateFloorWithNoThresholdsReturnsFirstFloor()
    {
        int result = DayProgressionPolicy.CalculateFloor(50, System.Array.Empty<long>());

        Assert.Equal(0, result);
    }
}
