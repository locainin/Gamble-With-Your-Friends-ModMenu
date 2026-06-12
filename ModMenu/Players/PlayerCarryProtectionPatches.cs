using HarmonyLib;

namespace ModMenu
{
    [HarmonyPatch(typeof(Item), "ServerPickup")]
    internal static class ItemServerPickupPlayerProtectionPatch
    {
        // Rejects protected player pickup before the server assigns a holder
        private static bool Prefix(Item __instance)
        {
            PlayerCarry? playerCarry = __instance as PlayerCarry;
            return playerCarry == null || !PlayerProtectionState.IsNoGrab(playerCarry);
        }
    }
}
