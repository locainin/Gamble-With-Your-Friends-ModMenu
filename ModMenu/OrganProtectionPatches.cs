using HarmonyLib;

namespace ModMenu
{
    [HarmonyPatch(typeof(OrganManager), nameof(OrganManager.ServerToggleOrgan))]
    internal static class OrganManagerServerToggleOrganPatch
    {
        // Intercepts a game call before it can disable protected organs
        private static bool Prefix(PlayerOrgans organs, bool isEnabled)
        {
            return isEnabled || !PlayerProtectionState.IsProtected(organs);
        }
    }

    [HarmonyPatch(typeof(OrganManager), nameof(OrganManager.SetOrganDataBySteamId))]
    internal static class OrganManagerSetOrganDataBySteamIdPatch
    {
        // Intercepts a game call before it can disable protected organs
        private static void Prefix(ulong steamId, ref bool leftEye, ref bool rightEye, ref bool body, ref bool mouth)
        {
            if (!PlayerProtectionState.IsProtected(steamId))
            {
                return;
            }
            leftEye = true;
            rightEye = true;
            body = true;
            mouth = true;
        }
    }

    [HarmonyPatch(typeof(PlayerOrgans), nameof(PlayerOrgans.ServerSetBodyParts))]
    internal static class PlayerOrgansServerSetBodyPartsPatch
    {
        // Intercepts a game call before it can disable protected organs
        private static void Prefix(PlayerOrgans __instance, PlayerOrganData data)
        {
            if (PlayerProtectionState.IsProtected(__instance))
            {
                PlayerProtectionState.ForceHealthy(data);
            }
        }
    }
}
