using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Teleport controls operate from the selected player instead of a global router
        private void DrawSelectedPlayerTeleport(PlayerProfile[] profiles, PlayerProfile selectedProfile, bool isHost)
        {
            DrawSection("Quick Teleport");
            if (!isHost)
            {
                DrawHostWarning("Teleport actions require host authority");
                return;
            }

            PlayerController? selectedPlayer = selectedProfile.GetComponent<PlayerController>();
            PlayerProfile? localProfile = GetLocalPlayerProfile();
            PlayerController? localPlayer = localProfile != null ? localProfile.GetComponent<PlayerController>() : cachedLocalPC;
            bool selectedIsLocal = selectedProfile.isLocalPlayer;

            // Quick actions cover the common host-to-player workflow
            GUI.enabled = selectedPlayer != null && localPlayer != null && !selectedIsLocal;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bring To Me") && selectedPlayer != null && localPlayer != null)
            {
                TeleportPlayerTo(selectedPlayer, localPlayer);
            }
            if (GUILayout.Button("Go To Player") && selectedPlayer != null && localPlayer != null)
            {
                TeleportPlayerTo(localPlayer, selectedPlayer);
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            DrawSection("Player To Player");
            if (profiles.Length < 2)
            {
                GUILayout.Label("  Another player is required", smallLabelStyle);
                return;
            }

            EnsureTeleportTarget(profiles, selectedProfile.steamId);
            GUILayout.Label("  Destination", smallLabelStyle);
            GUILayout.BeginHorizontal();
            foreach (PlayerProfile targetProfile in profiles)
            {
                if (targetProfile == null || targetProfile.steamId == selectedProfile.steamId)
                {
                    continue;
                }

                DrawTeleportTargetButton(targetProfile);
            }
            GUILayout.EndHorizontal();

            PlayerController? targetPlayer = FindPlayerControllerBySteamId(profiles, teleportTargetSteamId);
            GUI.enabled = selectedPlayer != null && targetPlayer != null && selectedPlayer != targetPlayer;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Move Selected") && selectedPlayer != null && targetPlayer != null)
            {
                TeleportPlayerTo(selectedPlayer, targetPlayer);
            }
            if (GUILayout.Button("Swap Positions") && selectedPlayer != null && targetPlayer != null)
            {
                SwapPlayers(selectedPlayer, targetPlayer);
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        // Selects a valid destination distinct from the inspected player
        private void EnsureTeleportTarget(PlayerProfile[] profiles, ulong selectedSteamId)
        {
            PlayerProfile? currentTarget = FindPlayerProfileBySteamId(profiles, teleportTargetSteamId);
            if (currentTarget != null && currentTarget.steamId != selectedSteamId)
            {
                return;
            }

            foreach (PlayerProfile profile in profiles)
            {
                if (profile != null && profile.steamId != selectedSteamId)
                {
                    teleportTargetSteamId = profile.steamId;
                    return;
                }
            }

            teleportTargetSteamId = 0uL;
        }

        // Renders one destination in the selected-player workflow
        private void DrawTeleportTargetButton(PlayerProfile playerProfile)
        {
            bool isSelected = teleportTargetSteamId == playerProfile.steamId;
            string playerName = string.IsNullOrEmpty(playerProfile.playerName) ? "Unknown" : playerProfile.playerName;
            GUIStyle targetStyle = isSelected ? activeTabStyle : GUI.skin.button;
            if (GUILayout.Button(playerName, targetStyle, GUILayout.Height(28f)))
            {
                teleportTargetSteamId = playerProfile.steamId;
            }
        }

        // Finds a player controller from the current profile snapshot
        private static PlayerController? FindPlayerControllerBySteamId(PlayerProfile[] profiles, ulong steamId)
        {
            PlayerProfile? profile = FindPlayerProfileBySteamId(profiles, steamId);
            return profile != null ? profile.GetComponent<PlayerController>() : null;
        }

        // Moves one player to a nearby randomized position
        private void TeleportPlayerNearby(PlayerController playerController)
        {
            if (playerController == null || cachedGM == null || !cachedGM.isServer)
            {
                return;
            }

            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(2f, 6f);
            Vector3 position = playerController.transform.position + new Vector3(randomCircle.x, 0.35f, randomCircle.y);
            playerController.ServerTeleport(position);
        }

        // Exchanges two players using their current world positions
        private void SwapPlayers(PlayerController firstPlayer, PlayerController secondPlayer)
        {
            if (firstPlayer == null || secondPlayer == null || cachedGM == null || !cachedGM.isServer)
            {
                return;
            }

            // Capture both positions before either network transform changes
            Vector3 firstPosition = firstPlayer.transform.position;
            Vector3 secondPosition = secondPlayer.transform.position;
            firstPlayer.ServerTeleport(secondPosition + Vector3.up * 0.2f);
            secondPlayer.ServerTeleport(firstPosition + Vector3.up * 0.2f);
        }

        // Places one player beside another using the server teleport path
        private void TeleportPlayerTo(PlayerController movingPlayer, PlayerController targetPlayer)
        {
            if (movingPlayer == null || targetPlayer == null || cachedGM == null || !cachedGM.isServer)
            {
                return;
            }

            // Side placement prevents player colliders from occupying the same point
            Vector3 position = targetPlayer.transform.position + targetPlayer.transform.right * 1.25f + Vector3.up * 0.2f;
            movingPlayer.ServerTeleport(position);
        }
    }
}
