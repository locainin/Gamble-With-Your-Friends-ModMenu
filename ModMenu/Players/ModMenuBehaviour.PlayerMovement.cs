using System;
using System.Reflection;
using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Local movement controls only change the locally owned controller

        // Renders movement overrides only for the locally owned player
        private void DrawLocalMovementControls(PlayerProfile playerProfile, PlayerController playerController)
        {
            if (playerProfile == null || playerController == null)
            {
                return;
            }
            GUILayout.Space(4f);
            DrawSection("Local Movement");
            // Unity input and movement components are owned by the local client
            bool canEditMovement = playerProfile.isLocalPlayer;
            if (!canEditMovement)
            {
                GUILayout.Label("  Movement, jump, and no clip only affect the local player.", smallLabelStyle);
            }
            GUI.enabled = canEditMovement;
            speedHackEnabled = GUILayout.Toggle(speedHackEnabled, speedHackEnabled ? " SPEED ACTIVE" : " Speed Modifier");
            if (speedHackEnabled)
            {
                // A compact value and slider row preserves the available content width
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  {speedMultiplier:F1}x", GUILayout.Width(50f));
                speedMultiplier = GUILayout.HorizontalSlider(speedMultiplier, 0.5f, 5f);
                GUILayout.EndHorizontal();
            }
            jumpHackEnabled = GUILayout.Toggle(jumpHackEnabled, jumpHackEnabled ? " JUMP ACTIVE" : " Jump Modifier");
            if (jumpHackEnabled)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  {jumpMultiplier:F1}x", GUILayout.Width(50f));
                jumpMultiplier = GUILayout.HorizontalSlider(jumpMultiplier, 0.5f, 5f);
                GUILayout.EndHorizontal();
            }
            flyHackEnabled = GUILayout.Toggle(flyHackEnabled, flyHackEnabled ? " NO CLIP ACTIVE" : " No Clip");
            if (flyHackEnabled)
            {
                // Flight speed remains separate from the grounded movement multiplier
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  Speed: {flySpeedMultiplier:F1}x", GUILayout.Width(80f));
                flySpeedMultiplier = GUILayout.HorizontalSlider(flySpeedMultiplier, 0.5f, 10f);
                GUILayout.EndHorizontal();
            }
            GUI.enabled = true;
        }

    }
}
