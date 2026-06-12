using System;
using System.Reflection;
using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        private static readonly MethodInfo? PlayerCarryRpcSetInteractableMethod =
            typeof(PlayerCarry).GetMethod("RpcSetInteractable", BindingFlags.Instance | BindingFlags.NonPublic);

        // Renders pickup protection for host-selected players or the local client
        private void DrawPlayerGrabProtection(PlayerProfile playerProfile, bool isHost)
        {
            DrawSection("Pickup Protection");

            PlayerCarry playerCarry = playerProfile.GetComponent<PlayerCarry>();
            bool canChange = playerCarry != null && (isHost || playerProfile.isLocalPlayer);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && canChange;

            bool isProtected = PlayerProtectionState.IsNoGrab(playerProfile.steamId);
            bool requestedProtection = GUILayout.Toggle(
                isProtected,
                isProtected ? " NO GRAB ACTIVE" : " Prevent Player Pickup");

            if (requestedProtection != isProtected)
            {
                SetPlayerGrabProtection(playerProfile, requestedProtection, isHost);
            }

            GUI.enabled = wasEnabled;
            if (!canChange)
            {
                GUILayout.Label("  Host authority or local ownership is required", smallLabelStyle);
            }
        }

        // Applies pickup protection through the authority path owned by this process
        private static void SetPlayerGrabProtection(PlayerProfile playerProfile, bool isProtected, bool isHost)
        {
            if (playerProfile == null || playerProfile.steamId == 0uL)
            {
                return;
            }

            PlayerCarry playerCarry = playerProfile.GetComponent<PlayerCarry>();
            if (playerCarry == null)
            {
                return;
            }

            // State is recorded first so the server pickup patch closes the race window
            PlayerProtectionState.SetNoGrab(playerProfile.steamId, isProtected);

            try
            {
                if (isHost)
                {
                    // A held player is dropped before future pickup requests are blocked
                    if (isProtected && playerCarry.TryGetHolderInventory(out PlayerInventory holderInventory))
                    {
                        holderInventory.ServerDropHoldingItem();
                    }

                    // The host updates every client cursor while the patch enforces the rule
                    playerCarry.IsInteractable = !isProtected;
                    PlayerCarryRpcSetInteractableMethod?.Invoke(playerCarry, new object[1] { !isProtected });
                    return;
                }

                if (playerProfile.isLocalPlayer)
                {
                    // The owned command asks the server to replicate local pickup availability
                    playerCarry.LocalSetInteractable(!isProtected);
                }
            }
            catch (Exception ex)
            {
                PlayerProtectionState.SetNoGrab(playerProfile.steamId, !isProtected);
                ModMenuLoader.Log("Pickup protection error: " + ex.Message);
            }
        }

        // Reapplies pickup visibility after scene objects are recreated
        private static void ReapplyPlayerGrabProtections(bool isHost)
        {
            PlayerProfile[] profiles = UnityEngine.Object.FindObjectsByType<PlayerProfile>(FindObjectsSortMode.None);
            foreach (PlayerProfile playerProfile in profiles)
            {
                if (playerProfile == null || !PlayerProtectionState.IsNoGrab(playerProfile.steamId))
                {
                    continue;
                }

                PlayerCarry playerCarry = playerProfile.GetComponent<PlayerCarry>();
                if (playerCarry == null)
                {
                    continue;
                }

                try
                {
                    if (isHost)
                    {
                        // Host replication updates interaction cursors for every connected client
                        playerCarry.IsInteractable = false;
                        PlayerCarryRpcSetInteractableMethod?.Invoke(playerCarry, new object[1] { false });
                    }
                    else if (playerProfile.isLocalPlayer)
                    {
                        // A client can only restore the command on its owned player object
                        playerCarry.LocalSetInteractable(false);
                    }
                }
                catch (Exception ex)
                {
                    ModMenuLoader.Log("Pickup protection recovery error: " + ex.Message);
                }
            }
        }
    }
}
