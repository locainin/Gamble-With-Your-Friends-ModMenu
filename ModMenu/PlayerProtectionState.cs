using System;
using System.Collections.Generic;
using HarmonyLib;

namespace ModMenu
{
    internal static class PlayerProtectionState
    {
        private static readonly HashSet<ulong> ProtectedSteamIds = new HashSet<ulong>();

        private static readonly HashSet<ulong> NoGrabSteamIds = new HashSet<ulong>();

        private static readonly HashSet<ulong> NoHitSteamIds = new HashSet<ulong>();

        // Harmony remains alive for the plugin lifetime so patches remain registered
        private static readonly Harmony HarmonyInstance = new Harmony("com.casinomenu.gamblewithfriends.organprotection");

        private static bool isPatched;

        // Recovery polling sleeps completely when no player has a persistent protection
        internal static bool HasAnyProtection =>
            ProtectedSteamIds.Count > 0 ||
            NoGrabSteamIds.Count > 0 ||
            NoHitSteamIds.Count > 0;

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

        // Adds or removes one Steam ID from physical hit protection
        internal static void SetNoHit(ulong steamId, bool isProtected)
        {
            if (steamId == 0uL)
            {
                return;
            }

            if (isProtected)
            {
                NoHitSteamIds.Add(steamId);
                return;
            }

            NoHitSteamIds.Remove(steamId);
        }

        // Checks whether a Steam ID rejects server knockback
        internal static bool IsNoHit(ulong steamId)
        {
            return steamId != 0uL && NoHitSteamIds.Contains(steamId);
        }

        // Checks whether one player controller rejects server knockback
        internal static bool IsNoHit(PlayerController playerController)
        {
            if (playerController == null)
            {
                return false;
            }

            PlayerProfile playerProfile = playerController.GetComponent<PlayerProfile>();
            return playerProfile != null && IsNoHit(playerProfile.steamId);
        }

        // Adds or removes one Steam ID from pickup protection
        internal static void SetNoGrab(ulong steamId, bool isProtected)
        {
            if (steamId == 0uL)
            {
                return;
            }

            if (isProtected)
            {
                NoGrabSteamIds.Add(steamId);
                return;
            }

            NoGrabSteamIds.Remove(steamId);
        }

        // Checks whether a Steam ID has pickup protection enabled
        internal static bool IsNoGrab(ulong steamId)
        {
            return steamId != 0uL && NoGrabSteamIds.Contains(steamId);
        }

        // Checks whether one player body rejects pickup requests
        internal static bool IsNoGrab(PlayerCarry playerCarry)
        {
            if (playerCarry == null)
            {
                return false;
            }

            PlayerProfile playerProfile = playerCarry.GetComponent<PlayerProfile>();
            return playerProfile != null && IsNoGrab(playerProfile.steamId);
        }

        // Removes protection entries that no longer belong to the active lobby
        internal static void RetainConnectedPlayers(IEnumerable<ulong> connectedSteamIds)
        {
            ProtectedSteamIds.IntersectWith(connectedSteamIds);
            NoGrabSteamIds.IntersectWith(connectedSteamIds);
            NoHitSteamIds.IntersectWith(connectedSteamIds);
        }

        // Clears session-owned protection when no lobby identity remains valid
        internal static void Clear()
        {
            ProtectedSteamIds.Clear();
            NoGrabSteamIds.Clear();
            NoHitSteamIds.Clear();
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
