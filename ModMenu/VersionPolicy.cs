using System;

namespace ModMenu
{
    internal enum VersionCheckResult
    {
        Invalid,
        Current,
        UpdateAvailable
    }

    internal static class VersionPolicy
    {
        // Compares semantic versions so an older feed never becomes an update prompt
        internal static VersionCheckResult Compare(string currentValue, string latestValue)
        {
            if (!TryParse(currentValue, out Version currentVersion) ||
                !TryParse(latestValue, out Version latestVersion))
            {
                return VersionCheckResult.Invalid;
            }

            return latestVersion > currentVersion
                ? VersionCheckResult.UpdateAvailable
                : VersionCheckResult.Current;
        }

        // Accepts the common optional v prefix while rejecting malformed version text
        private static bool TryParse(string value, out Version version)
        {
            string normalized = value.Trim();
            if (normalized.Length > 0 && (normalized[0] == 'v' || normalized[0] == 'V'))
            {
                normalized = normalized.Substring(1);
            }

            if (Version.TryParse(normalized, out Version? parsedVersion) && parsedVersion != null)
            {
                version = parsedVersion;
                return true;
            }

            version = new Version(0, 0);
            return false;
        }
    }
}
