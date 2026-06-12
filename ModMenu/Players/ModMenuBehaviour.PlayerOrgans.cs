using System;
using System.Reflection;
using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Organ state reads and host-side mutations share one persistence boundary

        // Renders organ status and mutations for the selected player
        private void DrawPlayerOrganControls(PlayerProfile playerProfile, bool isHost)
        {
            PlayerOrgans playerOrgans = playerProfile.GetComponent<PlayerOrgans>() ?? playerProfile.GetComponentInChildren<PlayerOrgans>();
            DrawSection("Organ Status");

            if (playerOrgans == null)
            {
                GUILayout.Label("  Organ component unavailable", smallLabelStyle);
                return;
            }

            bool leftEye;
            bool rightEye;
            bool body;
            bool mouth;
            bool hasState = TryGetOrganState(playerOrgans, isHost, out leftEye, out rightEye, out body, out mouth);
            if (!hasState)
            {
                GUILayout.Label("  Waiting for organ registration", smallLabelStyle);
                return;
            }

            DrawDataRow("Left eye", leftEye ? "Present" : "Removed");
            DrawDataRow("Right eye", rightEye ? "Present" : "Removed");
            DrawDataRow("Body", body ? "Present" : "Removed");
            DrawDataRow("Mouth", mouth ? "Present" : "Removed");

            if (!isHost)
            {
                DrawHostWarning("Organ mutations require host authority");
                return;
            }

            DrawSection("Protection");
            bool isProtected = PlayerProtectionState.IsProtected(playerProfile.steamId);
            bool requestedProtection = GUILayout.Toggle(isProtected, isProtected ? " GOD MODE ACTIVE" : " Enable God Mode");
            if (requestedProtection != isProtected)
            {
                // Protection repairs current damage before blocking later mutations
                PlayerProtectionState.SetProtected(playerProfile.steamId, requestedProtection);
                if (requestedProtection)
                {
                    SetPlayerOrgans(playerOrgans, true, true, true, true);
                }
            }

            DrawSection("Actions");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Restore All"))
            {
                SetPlayerOrgans(playerOrgans, true, true, true, true);
            }
            if (GUILayout.Button("Remove All"))
            {
                SetPlayerOrgans(playerOrgans, false, false, false, false);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(leftEye ? "Remove Left Eye" : "Restore Left Eye"))
            {
                SetPlayerOrgans(playerOrgans, !leftEye, rightEye, body, mouth);
            }
            if (GUILayout.Button(rightEye ? "Remove Right Eye" : "Restore Right Eye"))
            {
                SetPlayerOrgans(playerOrgans, leftEye, !rightEye, body, mouth);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(body ? "Remove Body" : "Restore Body"))
            {
                SetPlayerOrgans(playerOrgans, leftEye, rightEye, !body, mouth);
            }
            if (GUILayout.Button(mouth ? "Remove Mouth" : "Restore Mouth"))
            {
                SetPlayerOrgans(playerOrgans, leftEye, rightEye, body, !mouth);
            }
            GUILayout.EndHorizontal();
        }

        // Finds the organ component associated with a player controller
        private static PlayerOrgans? FindPlayerOrgans(PlayerController playerController)
        {
            if (playerController == null)
            {
                return null;
            }
            PlayerOrgans playerOrgans = playerController.GetComponent<PlayerOrgans>() ?? playerController.GetComponentInChildren<PlayerOrgans>();
            if (playerOrgans != null)
            {
                return playerOrgans;
            }
            PlayerOrgans[] array = UnityEngine.Object.FindObjectsByType<PlayerOrgans>(FindObjectsSortMode.None);
            // Fallback lookup covers scene layouts where organs live on a sibling object
            foreach (PlayerOrgans playerOrgans2 in array)
            {
                if (playerOrgans2 != null && playerOrgans2.GetComponent<PlayerController>() == playerController)
                {
                    return playerOrgans2;
                }
            }
            return null;
        }

        // Reads authoritative organ data on host or local replica fields on client
        private static bool TryGetOrganState(PlayerOrgans playerOrgans, bool isHost, out bool leftEye, out bool rightEye, out bool body, out bool mouth)
        {
            leftEye = true;
            rightEye = true;
            body = true;
            mouth = true;
            try
            {
                if (isHost)
                {
                    // The host manager contains the authoritative persisted organ record
                    OrganManager organManager = UnityEngine.Object.FindFirstObjectByType<OrganManager>();
                    if (organManager != null)
                    {
                        PlayerOrganData organData = organManager.GetOrganData(playerOrgans);
                        if (organData != null)
                        {
                            leftEye = organData.leftEye;
                            rightEye = organData.rightEye;
                            body = organData.body;
                            mouth = organData.mouth;
                            return true;
                        }
                    }
                }
                // Clients can still display the replicated private state for diagnostics
                leftEye = ReadPrivateBool(playerOrgans, "_localLeftEye", leftEye);
                rightEye = ReadPrivateBool(playerOrgans, "_localRightEye", rightEye);
                body = ReadPrivateBool(playerOrgans, "_localBody", body);
                mouth = ReadPrivateBool(playerOrgans, "_localMouth", mouth);
                return true;
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("TryGetOrganState error: " + ex.Message);
                return false;
            }
        }

        // Reads a private boolean field with a safe fallback
        private static bool ReadPrivateBool(object instance, string fieldName, bool fallback)
        {
            if (instance == null)
            {
                return fallback;
            }
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return fallback;
            }
            object value = field.GetValue(instance);
            // Unexpected field types retain the caller's safe fallback
            if (value is bool)
            {
                return (bool)value;
            }
            return fallback;
        }

        // Updates all organ fields through the server manager and optionally persists the result
        private void SetPlayerOrgans(PlayerOrgans playerOrgans, bool leftEye, bool rightEye, bool body, bool mouth, bool persist = true)
        {
            if (playerOrgans == null || cachedGM == null || !cachedGM.isServer)
            {
                return;
            }
            try
            {
                OrganManager organManager = UnityEngine.Object.FindFirstObjectByType<OrganManager>();
                PlayerProfile playerProfile = playerOrgans.GetComponent<PlayerProfile>();
                if (organManager != null && playerProfile != null && playerProfile.steamId != 0uL)
                {
                    // Steam ID backed changes survive the game's late-join organ refresh path
                    organManager.SetOrganDataBySteamId(playerProfile.steamId, leftEye, rightEye, body, mouth);
                    if (persist)
                    {
                        PersistOrganChanges();
                    }
                    return;
                }
                if (organManager != null)
                {
                    // Object-based toggles cover profiles not yet registered by Steam ID
                    organManager.ServerToggleOrgan(playerOrgans, OrganType.LeftEye, leftEye);
                    organManager.ServerToggleOrgan(playerOrgans, OrganType.RightEye, rightEye);
                    organManager.ServerToggleOrgan(playerOrgans, OrganType.Body, body);
                    organManager.ServerToggleOrgan(playerOrgans, OrganType.Mouth, mouth);
                    if (persist)
                    {
                        PersistOrganChanges();
                    }
                    return;
                }
                PlayerOrganData data = new PlayerOrganData
                {
                    organsReference = playerOrgans,
                    leftEye = leftEye,
                    rightEye = rightEye,
                    body = body,
                    mouth = mouth
                };
                // Direct server state is the last fallback when the manager is unavailable
                playerOrgans.ServerSetBodyParts(data);
                if (persist)
                {
                    PersistOrganChanges();
                }
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("SetPlayerOrgans error: " + ex.Message);
                ModMenuLoader.Log($"SetPlayerOrgans stack: {ex}");
            }
        }

    }
}
