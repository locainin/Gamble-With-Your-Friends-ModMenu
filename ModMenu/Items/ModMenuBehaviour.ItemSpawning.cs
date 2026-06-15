using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Item spawning owns prefab creation, purchase marking, and Mirror registration

        // Instantiates one configured spawnable near the local player
        private void SpawnItem(object spawnableSO)
        {
            if (spawnableSO == null)
            {
                return;
            }
            if (cachedGM == null || !cachedGM.isServer)
            {
                ModMenuLoader.Log("SpawnItem: must be host");
            }
            else
            {
                if (cachedLocalPC == null)
                {
                    return;
                }
                try
                {
                    FieldInfo? fieldInfo = spawnableSOType?.GetField("prefab", BindingFlags.Instance | BindingFlags.Public);
                    if (fieldInfo == null)
                    {
                        return;
                    }
                    GameObject? gameObject = fieldInfo.GetValue(spawnableSO) as GameObject;
                    if (!(gameObject == null))
                    {
                        string arg = "unknown";
                        FieldInfo? fieldInfo2 = spawnableSOType?.GetField("spawnableName", BindingFlags.Instance | BindingFlags.Public);
                        if (fieldInfo2 != null)
                        {
                            arg = (fieldInfo2.GetValue(spawnableSO) as string) ?? "unknown";
                        }
                        Transform transform = ((Camera.main != null) ? Camera.main.transform : cachedLocalPC.transform);
                        // Front placement keeps the new item visible and away from the player collider
                        Vector3 vector = cachedLocalPC.transform.position + transform.forward * 2f + Vector3.up * 0.5f;
                        GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, vector, Quaternion.identity);
                        if (!SpawnOnNetwork(gameObject2))
                        {
                            // A server-only clone must not survive when clients cannot receive its identity
                            UnityEngine.Object.Destroy(gameObject2);
                            ModMenuLoader.Log($"Failed to network-spawn {arg}");
                            return;
                        }
                        MarkItemPurchased(gameObject2);
                        ModMenuLoader.Log($"Spawned {arg} at {vector}");
                    }
                }
                catch (Exception arg2)
                {
                    ModMenuLoader.Log($"SpawnItem error: {arg2}");
                }
            }
        }

        // Marks a spawned item as purchased so normal game cleanup accepts it
        private void MarkItemPurchased(GameObject instance)
        {
            try
            {
                if (itemStampManagerType == null)
                {
                    // Resolve the manager lazily because lobby scenes may load before its assembly
                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (Assembly assembly in assemblies)
                    {
                        itemStampManagerType = assembly.GetType("ItemStampManager");
                        if (itemStampManagerType != null)
                        {
                            break;
                        }
                    }
                }
                if (itemStampManagerType == null)
                {
                    return;
                }
                object? obj = null;
                PropertyInfo property = itemStampManagerType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
                if (property != null)
                {
                    obj = property.GetValue(null);
                }
                if (obj == null)
                {
                    // Inactive singleton instances remain visible through the Resources lookup
                    UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(itemStampManagerType);
                    if (array.Length != 0)
                    {
                        obj = array[0];
                    }
                }
                if (obj != null)
                {
                    MethodInfo method = itemStampManagerType.GetMethod("MarkInstancePurchased", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null)
                    {
                        method.Invoke(obj, new object[1] { instance });
                        ModMenuLoader.Log("Marked item as purchased");
                    }
                }
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("MarkItemPurchased error: " + ex.Message);
            }
        }

        // Registers a spawned object with Mirror when hosting
        private bool SpawnOnNetwork(GameObject go)
        {
            if (cachedLocalPC == null || !Mirror.NetworkServer.active)
            {
                return false;
            }

            if (go.GetComponent<Mirror.NetworkIdentity>() == null)
            {
                // Mirror cannot register a prefab clone that has no network identity
                return false;
            }

            try
            {
                // This overload assigns ownership to the host player and is stable in the shipped Mirror build
                Mirror.NetworkServer.Spawn(go, cachedLocalPC.gameObject);
                return true;
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("Network spawn error: " + ex.Message);
                return false;
            }
        }

    }
}
