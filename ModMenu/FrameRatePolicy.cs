using System;

namespace ModMenu
{
    internal static class FrameRatePolicy
    {
        internal const float MinimumFrameTime = 0.0001f;

        // Smooths frame time without allowing invalid or zero delta samples
        internal static float SmoothFrameTime(float current, float sample, float weight)
        {
            float safeCurrent = Math.Max(current, MinimumFrameTime);
            float safeSample = Math.Max(sample, MinimumFrameTime);
            float safeWeight = Math.Max(0f, Math.Min(weight, 1f));
            return safeCurrent + ((safeSample - safeCurrent) * safeWeight);
        }

        // Converts a frame-time sample into a bounded display value
        internal static float ToDisplayFps(float frameTime)
        {
            float safeFrameTime = Math.Max(frameTime, MinimumFrameTime);
            return Math.Max(0f, Math.Min((float)Math.Round(1f / safeFrameTime), 999f));
        }
    }
}
