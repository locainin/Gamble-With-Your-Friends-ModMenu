using ModMenu;

namespace ModMenu.Tests;

public sealed class FrameRatePolicyTests
{
    [Theory]
    [InlineData(0.0166667f, 60f)]
    [InlineData(0.0083333f, 120f)]
    [InlineData(0.0333333f, 30f)]
    [InlineData(1f, 1f)]
    [InlineData(0f, 999f)]
    [InlineData(-1f, 999f)]
    public void ToDisplayFpsReturnsBoundedRoundedValue(float frameTime, float expectedFps)
    {
        float result = FrameRatePolicy.ToDisplayFps(frameTime);

        Assert.Equal(expectedFps, result);
    }

    [Theory]
    [InlineData(0.02f, 0.01f, 0f, 0.02f)]
    [InlineData(0.02f, 0.01f, 1f, 0.01f)]
    [InlineData(0.02f, 0.01f, 0.5f, 0.015f)]
    [InlineData(0.02f, 0.01f, -2f, 0.02f)]
    [InlineData(0.02f, 0.01f, 2f, 0.01f)]
    public void SmoothFrameTimeClampsWeightAndInterpolates(float current, float sample, float weight, float expected)
    {
        float result = FrameRatePolicy.SmoothFrameTime(current, sample, weight);

        Assert.Equal(expected, result, 5);
    }
}
