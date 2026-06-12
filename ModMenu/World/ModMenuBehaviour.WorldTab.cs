using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        private readonly string[] worldWorkspaceModes = new string[3] { "NPCs", "Time", "Recovery" };

        // World tools use one focused workspace instead of stacking unrelated sections
        private void DrawWorldTab(bool isHost)
        {
            DrawWorldDisplayControls();
            DrawWorldWorkspaceNavigation();

            if (worldWorkspaceMode == 0)
            {
                DrawNpcWorkspace(isHost);
            }
            else if (worldWorkspaceMode == 1)
            {
                DrawWorldTimeControls(isHost);
            }
            else
            {
                DrawWorldRecoveryControls(isHost);
            }
        }

        // Persists optional overlays that remain visible while the menu is closed
        private void DrawWorldDisplayControls()
        {
            bool requestedFpsState = GUILayout.Toggle(fpsOverlayEnabled, fpsOverlayEnabled ? " FPS OVERLAY ACTIVE" : " Show FPS Overlay");
            if (requestedFpsState != fpsOverlayEnabled)
            {
                fpsOverlayEnabled = requestedFpsState;
                PlayerPrefs.SetInt("CasinoMenu_FpsOverlay", fpsOverlayEnabled ? 1 : 0);
                PlayerPrefs.Save();
            }
            GUILayout.Space(4f);
        }

        // Switches World content without adding another sidebar level
        private void DrawWorldWorkspaceNavigation()
        {
            GUILayout.BeginHorizontal();
            for (int index = 0; index < worldWorkspaceModes.Length; index++)
            {
                GUIStyle modeStyle = worldWorkspaceMode == index ? activeTabStyle : GUI.skin.button;
                if (GUILayout.Button(worldWorkspaceModes[index], modeStyle, GUILayout.Height(31f)))
                {
                    worldWorkspaceMode = index;
                    npcInspectorScrollPos = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
        }

        // Exposes the game's non-purchased item recovery path for stuck lobby loot
        private void DrawWorldRecoveryControls(bool isHost)
        {
            DrawSection("World Recovery");
            GUILayout.Label("  Rebuilds unpurchased item stamps without changing bought items", smallLabelStyle);

            ItemStampManager? itemStampManager = UnityEngine.Object.FindFirstObjectByType<ItemStampManager>();
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && isHost && itemStampManager != null;
            if (GUILayout.Button("Recover Lobby Items"))
            {
                // The game includes a one-second guard against accidental repeated recovery
                itemStampManager!.RetrieveAndRespawnAllItemStamps();
            }
            GUI.enabled = previousEnabled;

            if (!isHost)
            {
                DrawHostWarning("Host authority is required for world recovery");
            }
        }
    }
}
