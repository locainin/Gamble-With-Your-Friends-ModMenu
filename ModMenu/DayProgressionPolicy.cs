using System;

namespace ModMenu
{
    internal readonly struct DayProgression
    {
        internal DayProgression(int displayDay, int daysPassed, int daysLeft, int successfulQuotas, int floor)
        {
            DisplayDay = displayDay;
            DaysPassed = daysPassed;
            DaysLeft = daysLeft;
            SuccessfulQuotas = successfulQuotas;
            Floor = floor;
        }

        internal int DisplayDay { get; }

        internal int DaysPassed { get; }

        internal int DaysLeft { get; }

        internal int SuccessfulQuotas { get; }

        internal int Floor { get; }
    }

    internal static class DayProgressionPolicy
    {
        internal const int MinimumDisplayDay = 1;

        internal const int MaximumDisplayDay = 999;

        // Reconstructs coupled progression values from the requested player-facing day
        internal static DayProgression Calculate(int displayDay, int daysBeforeQuota, long[] floorThresholds)
        {
            int safeDisplayDay = ClampDisplayDay(displayDay);
            int safeCycleLength = Math.Max(daysBeforeQuota, 1);
            int daysPassed = safeDisplayDay - 1;
            int successfulQuotas = daysPassed / safeCycleLength;
            int daysLeft = safeCycleLength - (daysPassed % safeCycleLength);
            int floor = CalculateFloor(daysPassed, floorThresholds);
            return new DayProgression(safeDisplayDay, daysPassed, daysLeft, successfulQuotas, floor);
        }

        // Matches GameSettings.DayToFloor while remaining directly testable
        internal static int CalculateFloor(int daysPassed, long[] floorThresholds)
        {
            if (floorThresholds.Length == 0)
            {
                return 0;
            }

            int floor = 0;
            for (int index = 0; index < floorThresholds.Length; index++)
            {
                if (floorThresholds[index] > daysPassed)
                {
                    break;
                }
                floor = index;
            }
            return floor;
        }

        // Keeps manually entered values inside a practical and overflow-safe range
        internal static int ClampDisplayDay(int displayDay)
        {
            return Math.Max(MinimumDisplayDay, Math.Min(displayDay, MaximumDisplayDay));
        }
    }
}
