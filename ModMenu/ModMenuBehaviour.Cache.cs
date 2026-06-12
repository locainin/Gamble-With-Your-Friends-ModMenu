using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Runtime lookup helpers keep scene objects cached without assuming load order

        // Finds current player and manager instances after scene changes
        private void RefreshCache()
        {
            try
            {
                if (cachedLocalPC == null)
                {
                    PlayerController[] array = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
                    foreach (PlayerController playerController in array)
                    {
                        if (!playerController.isLocalPlayer)
                        {
                            continue;
                        }
                        cachedLocalPC = playerController;
                        FieldInfo field = typeof(PlayerController).GetField("_ps", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (field != null)
                        {
                            cachedPlayerSettings = field.GetValue(playerController);
                            if (cachedPlayerSettings != null)
                            {
                                Type type = cachedPlayerSettings.GetType();
                                fMaxSpeed = type.GetField("maxSpeed");
                                fSprintSpeed = type.GetField("sprintMaxSpeed");
                                fAccel = type.GetField("acceleration");
                                fJumpForce = type.GetField("jumpForce");
                            }
                        }
                        break;
                    }
                }
                if (cachedMM == null)
                {
                    MoneyManager[] array2 = UnityEngine.Object.FindObjectsByType<MoneyManager>(FindObjectsSortMode.None);
                    if (array2.Length != 0)
                    {
                        cachedMM = array2[0];
                    }
                }
                if (cachedGM == null)
                {
                    cachedGM = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                }
                if (cachedGM != null && !gmDumped)
                {
                    gmDumped = true;
                    DumpGameManagerValues();
                }
                if (!changeTypeResolved)
                {
                    ResolveChangeType();
                }
                if (cachedSpawnables == null && cachedLocalPC != null)
                {
                    LoadItemSpawnables();
                }
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("Cache error: " + ex.Message);
            }
        }

        // Logs simple manager fields for compatibility debugging
        private void DumpGameManagerValues()
        {
            try
            {
                GameManager? obj = cachedGM;
                if (obj == null)
                {
                    return;
                }
                FieldInfo[] fields = typeof(GameManager).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                ModMenuLoader.Log("=== GameManager field dump ===");
                FieldInfo[] array = fields;
                foreach (FieldInfo fieldInfo in array)
                {
                    try
                    {
                        object value = fieldInfo.GetValue(obj);
                        if (value is int || value is long || value is float || value is bool || value is string)
                        {
                            ModMenuLoader.Log($"  GM.{fieldInfo.Name} ({fieldInfo.FieldType.Name}) = {value}");
                        }
                    }
                    catch
                    {
                    }
                }
                ModMenuLoader.Log("=== End GM dump ===");
                if (!(cachedMM != null))
                {
                    return;
                }
                FieldInfo[] fields2 = typeof(MoneyManager).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                ModMenuLoader.Log("=== MoneyManager field dump ===");
                array = fields2;
                foreach (FieldInfo fieldInfo2 in array)
                {
                    try
                    {
                        object value2 = fieldInfo2.GetValue(cachedMM);
                        if (value2 is int || value2 is long || value2 is float || value2 is bool || value2 is string)
                        {
                            ModMenuLoader.Log($"  MM.{fieldInfo2.Name} ({fieldInfo2.FieldType.Name}) = {value2}");
                        }
                    }
                    catch
                    {
                    }
                }
                ModMenuLoader.Log("=== End MM dump ===");
            }
            catch (Exception arg)
            {
                ModMenuLoader.Log($"Dump error: {arg}");
            }
        }

        // Finds the game result enum value used by money operations
        private void ResolveChangeType()
        {
            Type? type = null;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType("ChangeType");
                if (type != null)
                {
                    break;
                }
            }
            if (!(type != null))
            {
                return;
            }
            string[] names = Enum.GetNames(type);
            Array values = Enum.GetValues(type);
            for (int j = 0; j < names.Length; j++)
            {
                if (names[j] == "Misc")
                {
                    changeTypeGameResult = values.GetValue(j);
                    break;
                }
            }
            if (changeTypeGameResult == null)
            {
                changeTypeGameResult = values.GetValue(values.Length - 1);
            }
            ModMenuLoader.Log($"Using ChangeType: {changeTypeGameResult}");
            changeTypeResolved = true;
        }

        // Returns the profile owned by the client running this plugin
        private PlayerProfile? GetLocalPlayerProfile()
        {
            try
            {
                if (cachedLocalPC != null)
                {
                    PlayerProfile playerProfile = cachedLocalPC.GetComponent<PlayerProfile>() ?? cachedLocalPC.GetComponentInParent<PlayerProfile>() ?? cachedLocalPC.GetComponentInChildren<PlayerProfile>();
                    if (playerProfile != null)
                    {
                        return playerProfile;
                    }
                }
                PlayerProfile[] array = UnityEngine.Object.FindObjectsByType<PlayerProfile>(FindObjectsSortMode.None);
                foreach (PlayerProfile playerProfile2 in array)
                {
                    if (playerProfile2.isLocalPlayer)
                    {
                        return playerProfile2;
                    }
                }
            }
            catch
            {
            }
            return null;
        }

    }
}
