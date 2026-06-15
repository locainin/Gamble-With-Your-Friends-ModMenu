using System;
using System.Reflection;
using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Exposes visibility only for the selected local profile
        private void DrawPlayerVisibilityControl(PlayerProfile profile)
        {
            DrawSection("Visibility");
            CameramanMode? visibility = profile.GetComponent<CameramanMode>();
            bool canChangeVisibility = visibility != null && profile.isLocalPlayer;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && canChangeVisibility;

            bool requestedVisibility = visibility == null || !visibility.IsVisible;
            string actionLabel = requestedVisibility ? "Make Visible" : "Make Invisible";
            if (GUILayout.Button(actionLabel) && visibility != null)
            {
                SetLocalPlayerVisibility(visibility, requestedVisibility);
                visibilityModifiedPlayerInstanceId = profile.GetInstanceID();
            }

            GUI.enabled = previousEnabled;
            if (!canChangeVisibility)
            {
                GUILayout.Label("  Visibility can only be changed for the local player", smallLabelStyle);
            }
        }

        // Restores a menu-hidden local player when selection moves to another profile
        private void RestoreVisibilityAfterSelectionChange(PlayerProfile[] profiles, int previousInstanceId)
        {
            if (previousInstanceId == selectedPlayerInstanceId ||
                previousInstanceId != visibilityModifiedPlayerInstanceId)
            {
                return;
            }

            // Only undo visibility that this menu changed, never normal game cameraman state
            PlayerProfile? previousProfile = FindPlayerProfileByInstanceId(profiles, previousInstanceId);
            CameramanMode? visibility = previousProfile != null
                ? previousProfile.GetComponent<CameramanMode>()
                : null;
            if (visibility != null && previousProfile!.isLocalPlayer && !visibility.IsVisible)
            {
                SetLocalPlayerVisibility(visibility, true);
            }

            visibilityModifiedPlayerInstanceId = 0;
        }

        // Sends an explicit value so stale replicated state cannot invert the wrong direction
        private static void SetLocalPlayerVisibility(CameramanMode visibility, bool isVisible)
        {
            if (visibility.IsVisible == isVisible)
            {
                return;
            }

            try
            {
                MethodInfo? command = typeof(CameramanMode).GetMethod(
                    "CmdSetVisibility",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (command == null)
                {
                    ModMenuLoader.Log("Visibility command was not found");
                    return;
                }

                command.Invoke(visibility, new object[] { isVisible });
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("Visibility update error: " + ex.Message);
            }
        }

        // Leaves the local model visible if the plugin is unloaded while it owns the hidden state
        private void RestoreMenuVisibility()
        {
            if (visibilityModifiedPlayerInstanceId == 0)
            {
                return;
            }

            PlayerProfile[] profiles = UnityEngine.Object.FindObjectsByType<PlayerProfile>(FindObjectsSortMode.None);
            PlayerProfile? profile = FindPlayerProfileByInstanceId(profiles, visibilityModifiedPlayerInstanceId);
            CameramanMode? visibility = profile != null ? profile.GetComponent<CameramanMode>() : null;
            if (visibility != null && profile!.isLocalPlayer && !visibility.IsVisible)
            {
                SetLocalPlayerVisibility(visibility, true);
            }

            visibilityModifiedPlayerInstanceId = 0;
        }
    }
}
