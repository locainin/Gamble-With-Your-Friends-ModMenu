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
                        Vector3 vector = cachedLocalPC.transform.position + transform.forward * 2f + Vector3.up * 0.5f;
                        GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, vector, Quaternion.identity);
                        SpawnOnNetwork(gameObject2);
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
        private void SpawnOnNetwork(GameObject go)
        {
            if (networkServerType == null)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    networkServerType = assembly.GetType("Mirror.NetworkServer");
                    if (networkServerType != null)
                    {
                        break;
                    }
                }
            }
            if (networkServerSpawnMethod == null && networkServerType != null)
            {
                MethodInfo[] methods = networkServerType.GetMethods(BindingFlags.Static | BindingFlags.Public);
                foreach (MethodInfo methodInfo in methods)
                {
                    if (methodInfo.Name == "Spawn")
                    {
                        ParameterInfo[] parameters = methodInfo.GetParameters();
                        if (parameters.Length >= 1 && parameters[0].ParameterType == typeof(GameObject))
                        {
                            networkServerSpawnMethod = methodInfo;
                            break;
                        }
                    }
                }
            }
            if (networkServerSpawnMethod != null)
            {
                if (networkServerSpawnMethod.GetParameters().Length == 1)
                {
                    networkServerSpawnMethod.Invoke(null, new object[1] { go });
                }
                else if (cachedLocalPC != null)
                {
                    networkServerSpawnMethod.Invoke(null, new object[2] { go, cachedLocalPC.gameObject });
                }
            }
        }

    }
}
