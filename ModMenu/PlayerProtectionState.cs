using System;
using System.Collections.Generic;
using HarmonyLib;

namespace ModMenu
{
    internal static class PlayerProtectionState
    {
        private static readonly HashSet<ulong> ProtectedSteamIds = new HashSet<ulong>();

        // Harmony remains alive for the plugin lifetime so patches remain registered
        private static readonly Harmony HarmonyInstance = new Harmony("com.casinomenu.gamblewithfriends.organprotection");

        private static bool isPatched;

        // Installs organ protection Harmony patches once
        internal static void EnsurePatched()
        {
            if (isPatched)
            {
                return;
            }
            try
            {
                HarmonyInstance.PatchAll(typeof(PlayerProtectionState).Assembly);
                isPatched = true;
                ModMenuLoader.Log("Organ protection patches loaded");
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("Organ protection patch error: " + ex.Message);
            }
        }

        // Adds or removes one Steam ID from organ protection
        internal static void SetProtected(ulong steamId, bool isProtected)
        {
            if (steamId == 0uL)
            {
                return;
            }
            if (isProtected)
            {
                ProtectedSteamIds.Add(steamId);
                return;
            }
            ProtectedSteamIds.Remove(steamId);
        }

        // Checks whether a Steam ID has organ protection enabled
        internal static bool IsProtected(ulong steamId)
        {
            return steamId != 0uL && ProtectedSteamIds.Contains(steamId);
        }

        // Checks whether a Steam ID has organ protection enabled
        internal static bool IsProtected(PlayerOrgans playerOrgans)
        {
            if (playerOrgans == null)
            {
                return false;
            }
            PlayerProfile playerProfile = playerOrgans.GetComponent<PlayerProfile>();
            return playerProfile != null && IsProtected(playerProfile.steamId);
        }

        // Removes protection entries that no longer belong to the active lobby
        internal static void RetainConnectedPlayers(IEnumerable<ulong> connectedSteamIds)
        {
            ProtectedSteamIds.IntersectWith(connectedSteamIds);
        }

        // Clears session-owned protection when no lobby identity remains valid
        internal static void Clear()
        {
            ProtectedSteamIds.Clear();
        }

        // Normalizes organ data to a fully healthy state
        internal static void ForceHealthy(PlayerOrganData data)
        {
            if (data == null)
            {
                return;
            }
            data.leftEye = true;
            data.rightEye = true;
            data.body = true;
            data.mouth = true;
        }
    }
}
