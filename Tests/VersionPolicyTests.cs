using ModMenu;

namespace ModMenu.Tests;

public sealed class VersionPolicyTests
{
    [Theory]
    [InlineData("1.3.2", "1.2.0")]
    [InlineData("1.3.2", "1.3.2")]
    [InlineData("1.3.2", "v1.3.1")]
    public void CompareWhenFeedIsNotNewerReturnsCurrent(string current, string latest)
    {
        VersionCheckResult result = VersionPolicy.Compare(current, latest);

        Assert.Equal(VersionCheckResult.Current, result);
    }

    [Theory]
    [InlineData("1.3.2", "1.3.3")]
    [InlineData("1.3.2", "v2.0.0")]
    public void CompareWhenFeedIsNewerReturnsUpdateAvailable(string current, string latest)
    {
        VersionCheckResult result = VersionPolicy.Compare(current, latest);

        Assert.Equal(VersionCheckResult.UpdateAvailable, result);
    }

    [Theory]
    [InlineData("not-a-version", "1.3.2")]
    [InlineData("1.3.2", "latest")]
    [InlineData("", "")]
    public void CompareWhenEitherVersionIsInvalidReturnsInvalid(string current, string latest)
    {
        VersionCheckResult result = VersionPolicy.Compare(current, latest);

        Assert.Equal(VersionCheckResult.Invalid, result);
    }
}
