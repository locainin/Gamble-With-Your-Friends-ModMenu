using System;
using System.Reflection;
using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Applies all progression fields derived by the game's current settings
        private void SetGameDay(int requestedDisplayDay)
        {
            if (cachedGM == null || !cachedGM.isServer)
            {
                return;
            }

            try
            {
                FieldInfo? settingsField = typeof(GameManager).GetField("_gs", BindingFlags.Instance | BindingFlags.NonPublic);
                GameSettings? settings = settingsField?.GetValue(cachedGM) as GameSettings;
                if (settings == null || settings.floorData == null || settings.floorData.Count == 0)
                {
                    ModMenuLoader.Log("Day change skipped: GameSettings floor data unavailable");
                    return;
                }

                long[] floorThresholds = new long[settings.floorData.Count];
                for (int index = 0; index < settings.floorData.Count; index++)
                {
                    floorThresholds[index] = settings.floorData[index].requiredQuotaToAccess;
                }

                DayProgression progression = DayProgressionPolicy.Calculate(
                    requestedDisplayDay,
                    settings.daysBeforeQuota,
                    floorThresholds);

                int previousFloor = cachedGM.NetworkcurrentFloor;
                long currentMoney = cachedMM != null ? cachedMM.Networkbalance : 0L;
                long reconstructedQuota = ReconstructQuota(settings, progression.SuccessfulQuotas, currentMoney);
                long nextFloorThreshold = progression.Floor + 1 < settings.floorData.Count
                    ? settings.floorData[progression.Floor + 1].requiredQuotaToAccess
                    : long.MaxValue;

                // SyncVars update every client before an optional scene rebuild
                cachedGM.NetworkdaysPassed = progression.DaysPassed;
                cachedGM.NetworkdaysLeft = progression.DaysLeft;
                cachedGM.NetworksuccessfulQuota = progression.SuccessfulQuotas;
                cachedGM.NetworkcurrentQuota = reconstructedQuota;
                cachedGM.NetworkcurrentFloor = progression.Floor;
                cachedGM.NetworkrequiredQuotaToNextFloor = nextFloorThreshold;
                cachedGM.NetworkcurrentTicketReward = settings.GetTicketReward(progression.DaysPassed);

                PersistCurrentSave("Day progression");
                dayInput = progression.DisplayDay.ToString(System.Globalization.CultureInfo.InvariantCulture);

                if (previousFloor != progression.Floor && cachedGM.state == GameState.Game)
                {
                    // Reloading the active gameplay scene rebuilds floor-specific content once
                    cachedGM.ServerSetScene(GameState.Game);
                }
            }
            catch (Exception exception)
            {
                ModMenuLoader.Log("SetGameDay error: " + exception);
            }
        }

        // Replays quota calculation without paying rewards or triggering loss conditions
        private static long ReconstructQuota(GameSettings settings, int successfulQuotas, long currentMoney)
        {
            long quota = settings.startingQuota;
            for (int quotaIndex = 0; quotaIndex <= successfulQuotas; quotaIndex++)
            {
                quota = settings.GetQuota(quotaIndex, quota, currentMoney);
            }
            return quota;
        }
    }
}
