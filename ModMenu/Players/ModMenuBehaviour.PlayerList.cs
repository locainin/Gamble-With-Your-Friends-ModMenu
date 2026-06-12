using System;
using System.Globalization;
using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        private readonly string[] playerInspectorModes = new string[4] { "Overview", "Teleport", "Organs", "Effects" };

        // Player management uses a compact roster and one contextual inspector
        private void DrawPlayersTab(bool isHost)
        {
            // Scene snapshots prevent stale Mirror references after lobby changes
            PlayerProfile[] profiles = UnityEngine.Object.FindObjectsByType<PlayerProfile>(FindObjectsSortMode.None);
            if (profiles.Length == 0)
            {
                DrawSection("Players");
                GUILayout.Label("  No connected profiles", bodyLabelStyle);
                return;
            }

            EnsureSelectedPlayer(profiles);
            PlayerProfile? selectedProfile = FindPlayerProfileBySteamId(profiles, selectedPlayerSteamId);

            // The roster remains narrow while the inspector receives the working width
            GUILayout.BeginHorizontal();
            DrawPlayerRoster(profiles);
            GUILayout.Space(10f);
            DrawPlayerInspector(profiles, selectedProfile, isHost);
            GUILayout.EndHorizontal();
        }

        // Keeps the current selection valid as players join or leave
        private void EnsureSelectedPlayer(PlayerProfile[] profiles)
        {
            if (FindPlayerProfileBySteamId(profiles, selectedPlayerSteamId) != null)
            {
                return;
            }

            // Prefer the local player so solo sessions open on useful data
            foreach (PlayerProfile profile in profiles)
            {
                if (profile != null && profile.isLocalPlayer)
                {
                    selectedPlayerSteamId = profile.steamId;
                    return;
                }
            }

            selectedPlayerSteamId = profiles[0].steamId;
        }

        // Renders connected players as a scan-friendly selection list
        private void DrawPlayerRoster(PlayerProfile[] profiles)
        {
            GUILayout.BeginVertical(GUILayout.Width(158f));
            GUILayout.Label($"PLAYERS  {profiles.Length}", sectionStyle, GUILayout.Height(themeSectionHeight));

            // A dedicated roster scroll scales without duplicating action panels
            playerListScrollPos = GUILayout.BeginScrollView(playerListScrollPos, false, true, GUILayout.Height(420f));
            foreach (PlayerProfile profile in profiles)
            {
                if (profile == null)
                {
                    continue;
                }

                bool isSelected = profile.steamId == selectedPlayerSteamId;
                string playerName = string.IsNullOrEmpty(profile.playerName) ? "Unknown Player" : profile.playerName;
                string ownership = profile.isLocalPlayer ? "LOCAL" : "REMOTE";
                GUIStyle rowStyle = isSelected ? activeTabStyle : tabStyle;

                GUILayout.BeginVertical(GUI.skin.box);
                if (GUILayout.Button(playerName, rowStyle, GUILayout.Height(31f)))
                {
                    selectedPlayerSteamId = profile.steamId;
                    playerInspectorScrollPos = Vector2.zero;
                }
                GUILayout.Label("  " + ownership, smallLabelStyle);
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        // Renders data and actions only for the selected profile
        private void DrawPlayerInspector(PlayerProfile[] profiles, PlayerProfile? profile, bool isHost)
        {
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            if (profile == null)
            {
                GUILayout.Label("Select a player", bodyLabelStyle);
                GUILayout.EndVertical();
                return;
            }

            DrawSelectedPlayerHeader(profile, isHost);
            DrawPlayerInspectorNavigation();

            // One inspector scroll avoids nested cards and keeps mode switching stable
            playerInspectorScrollPos = GUILayout.BeginScrollView(playerInspectorScrollPos, false, true, GUILayout.Height(350f));
            if (playerInspectorMode == 0)
            {
                DrawPlayerOverview(profile, isHost);
            }
            else if (playerInspectorMode == 1)
            {
                DrawSelectedPlayerTeleport(profiles, profile, isHost);
            }
            else if (playerInspectorMode == 2)
            {
                DrawPlayerOrganControls(profile, isHost);
            }
            else
            {
                DrawSelectedPlayerEffects(profile, isHost);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        // Draws identity and authority context above all player actions
        private void DrawSelectedPlayerHeader(PlayerProfile profile, bool isHost)
        {
            string playerName = string.IsNullOrEmpty(profile.playerName) ? "Unknown Player" : profile.playerName;
            string ownership = profile.isLocalPlayer ? "LOCAL PLAYER" : "REMOTE PLAYER";

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label(playerName, titleStyle);
            GUILayout.Label(ownership, smallLabelStyle);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            // A fixed right-aligned status block prevents long names from clipping authority text
            GUIStyle authorityStyle = new GUIStyle(statusPillStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            authorityStyle.normal.textColor = isHost ? hostStatusColor : clientStatusColor;
            GUILayout.Label(isHost ? "HOST" : "CLIENT", authorityStyle, GUILayout.Width(82f), GUILayout.Height(28f));
            GUILayout.EndHorizontal();
        }

        // Provides compact inspector modes without repeating player identity
        private void DrawPlayerInspectorNavigation()
        {
            GUILayout.BeginHorizontal();
            for (int index = 0; index < playerInspectorModes.Length; index++)
            {
                GUIStyle modeStyle = playerInspectorMode == index ? activeTabStyle : GUI.skin.button;
                if (GUILayout.Button(playerInspectorModes[index], modeStyle, GUILayout.Height(29f)))
                {
                    playerInspectorMode = index;
                    playerInspectorScrollPos = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        // Displays network and gameplay state without exposing action clutter
        private void DrawPlayerOverview(PlayerProfile profile, bool isHost)
        {
            PlayerController playerController = profile.GetComponent<PlayerController>();
            PlayerOrgans playerOrgans = profile.GetComponent<PlayerOrgans>() ?? profile.GetComponentInChildren<PlayerOrgans>();

            DrawSection("Connection");
            DrawDataRow("Steam ID", profile.steamId.ToString(CultureInfo.InvariantCulture));
            DrawDataRow("Address", GetConnectionAddress(playerOrgans, profile));
            DrawDataRow("Authority", isHost ? "Server" : profile.isLocalPlayer ? "Local client" : "Observed client");

            DrawSection("Status");
            if (playerController == null)
            {
                GUILayout.Label("  Player controller unavailable", smallLabelStyle);
                return;
            }

            PlayerEnergy playerEnergy = playerController.GetComponent<PlayerEnergy>();
            DrawDataRow("State", playerController.State.ToString());
            DrawDataRow("Body", playerController.NetworkhasBody ? "Present" : "Removed");
            DrawDataRow("Energy", playerEnergy != null ? playerEnergy.Networkenergy.ToString("F0", CultureInfo.InvariantCulture) : "Unavailable");

            DrawNpcFollowPlayerControl(profile, isHost);
            DrawPlayerVisibilityControl(profile);

            if (!isHost)
            {
                DrawHostWarning("Server-owned physical and organ actions are unavailable as client");
            }
        }

        // Exposes the synchronized model visibility command only for the owned player object
        private void DrawPlayerVisibilityControl(PlayerProfile profile)
        {
            DrawSection("Visibility");
            CameramanMode? visibility = profile.GetComponent<CameramanMode>();
            bool canChangeVisibility = visibility != null && profile.isLocalPlayer;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && canChangeVisibility;

            string actionLabel = visibility != null && visibility.IsVisible ? "Make Invisible" : "Make Visible";
            if (GUILayout.Button(actionLabel))
            {
                // Mirror permits this command only from the client that owns the profile
                visibility!.ToggleVisibility();
            }

            GUI.enabled = previousEnabled;
            if (!canChangeVisibility)
            {
                GUILayout.Label("  Visibility can only be changed for the local player", smallLabelStyle);
            }
        }

        // Draws one aligned key and value row
        private void DrawDataRow(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("  " + label, smallLabelStyle, GUILayout.Width(90f));
            GUILayout.Label(value, bodyLabelStyle);
            GUILayout.EndHorizontal();
        }

        // Routes the selected player to the focused effects panel
        private void DrawSelectedPlayerEffects(PlayerProfile profile, bool isHost)
        {
            PlayerController playerController = profile.GetComponent<PlayerController>();
            if (playerController == null)
            {
                GUILayout.Label("  Player controller unavailable", smallLabelStyle);
                return;
            }

            DrawPlayerTrollControls(profile, playerController, isHost);
        }

        // Finds one profile from the current lobby snapshot
        private static PlayerProfile? FindPlayerProfileBySteamId(PlayerProfile[] profiles, ulong steamId)
        {
            foreach (PlayerProfile profile in profiles)
            {
                if (profile != null && profile.steamId == steamId)
                {
                    return profile;
                }
            }

            return null;
        }

        // Returns Mirror transport address data when the host can access it
        private string GetConnectionAddress(PlayerOrgans playerOrgans, PlayerProfile playerProfile)
        {
            try
            {
                // Mirror exposes remote transport addresses only to the server connection
                if (playerOrgans != null && playerOrgans.connectionToClient != null && !string.IsNullOrEmpty(playerOrgans.connectionToClient.address))
                {
                    return playerOrgans.connectionToClient.address;
                }
                if (playerProfile != null && playerProfile.isLocalPlayer)
                {
                    return "localhost";
                }
            }
            catch
            {
                // Connection replacement can briefly invalidate the transport object
            }

            return IsHostAddressAvailable(playerProfile) ? "server hidden" : "unavailable";
        }

        // Distinguishes a hidden server address from a missing client connection
        private bool IsHostAddressAvailable(PlayerProfile? playerProfile)
        {
            return cachedGM != null && cachedGM.isServer && playerProfile != null && !playerProfile.isLocalPlayer;
        }
    }
}
