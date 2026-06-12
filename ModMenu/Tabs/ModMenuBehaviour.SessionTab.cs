using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Session tab shows only capabilities valid for the current authority level
        private void DrawSessionTab(bool isHost)
        {
            if (isHost)
            {
                DrawHostSessionControls();
                return;
            }

            DrawClientSessionControls();
        }

        // Renders host-wide recovery and cleanup actions without duplicating player inspection
        private void DrawHostSessionControls()
        {
            DrawSection("Lobby Recovery");
            GUILayout.Label("  Apply recovery actions to every connected player", smallLabelStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Heal Everyone"))
            {
                HealAllPlayers();
            }
            if (GUILayout.Button("Wake Everyone"))
            {
                WakeAllPlayers();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Reset All Temporary Effects"))
            {
                ResetAllPlayerEffects();
            }

            DrawSection("Save State");
            GUILayout.Label("  Organ edits save automatically after each change", smallLabelStyle);
            if (GUILayout.Button("Force Organ Save"))
            {
                PersistOrganChanges();
            }
        }

        // Directs clients to contextual controls without duplicating the player inspector
        private void DrawClientSessionControls()
        {
            PlayerProfile? localProfile = GetLocalPlayerProfile();
            if (localProfile == null)
            {
                DrawSection("Client Controls");
                GUILayout.Label("  Join a lobby to load local controls", bodyLabelStyle);
                return;
            }

            DrawSection("Client Capability");
            GUILayout.Label("  Select the local profile in Players for movement and pickup protection", smallLabelStyle);
        }

        // Restores every registered player through the persistent organ path
        private void HealAllPlayers()
        {
            PlayerProfile[] profiles = UnityEngine.Object.FindObjectsByType<PlayerProfile>(FindObjectsSortMode.None);
            bool changedAnyPlayer = false;
            foreach (PlayerProfile? profile in profiles)
            {
                if (profile == null)
                {
                    continue;
                }

                PlayerOrgans playerOrgans = profile.GetComponent<PlayerOrgans>() ?? profile.GetComponentInChildren<PlayerOrgans>();
                if (playerOrgans != null)
                {
                    // Defer the expensive full save until every record is updated
                    SetPlayerOrgans(playerOrgans, true, true, true, true, persist: false);
                    changedAnyPlayer = true;
                }
            }

            if (changedAnyPlayer)
            {
                PersistOrganChanges();
            }
        }

        // Wakes every player and clears requested freeze state
        private void WakeAllPlayers()
        {
            PlayerProfile[] profiles = UnityEngine.Object.FindObjectsByType<PlayerProfile>(FindObjectsSortMode.None);
            foreach (PlayerProfile? profile in profiles)
            {
                PlayerController? playerController = profile != null ? profile.GetComponent<PlayerController>() : null;
                if (playerController == null)
                {
                    continue;
                }

                playerController.ServerWakeUp();
                frozenPlayerIds.Remove(profile!.steamId);
            }
        }

        // Clears all temporary menu effects from connected players
        private void ResetAllPlayerEffects()
        {
            PlayerProfile[] profiles = UnityEngine.Object.FindObjectsByType<PlayerProfile>(FindObjectsSortMode.None);
            foreach (PlayerProfile? profile in profiles)
            {
                PlayerController? playerController = profile != null ? profile.GetComponent<PlayerController>() : null;
                if (playerController != null)
                {
                    ResetPlayerEffects(profile!, playerController);
                }
            }
        }
    }
}
