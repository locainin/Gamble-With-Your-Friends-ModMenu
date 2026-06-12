using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Item discovery resolves runtime game types and builds the spawnable cache

        // Locates the active item manager before spawning an item
        private void EnsureItemManager()
        {
            if (cachedItemManager != null || itemManagerType == null)
            {
                return;
            }
            try
            {
                UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(itemManagerType);
                if (array.Length != 0)
                {
                    cachedItemManager = array[0];
                    ModMenuLoader.Log($"ItemManager found via Resources: {array.Length} instances");
                    return;
                }
            }
            catch
            {
            }
            try
            {
                MethodInfo method = typeof(UnityEngine.Object).GetMethod("FindObjectOfType", Type.EmptyTypes);
                if (method != null)
                {
                    MethodInfo methodInfo = method.MakeGenericMethod(itemManagerType);
                    cachedItemManager = methodInfo.Invoke(null, null);
                }
            }
            catch
            {
            }
            ModMenuLoader.Log($"ItemManager found: {cachedItemManager != null}");
        }

        // Builds the item list from runtime spawnable settings
        private void LoadItemSpawnables()
        {
            try
            {
                if (itemManagerType == null || spawnableSettingsType == null || spawnableSOType == null)
                {
                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (Assembly assembly in assemblies)
                    {
                        if (itemManagerType == null)
                        {
                            itemManagerType = assembly.GetType("ItemManager");
                        }
                        if (spawnableSettingsType == null)
                        {
                            spawnableSettingsType = assembly.GetType("SpawnableSettings");
                        }
                        if (spawnableSOType == null)
                        {
                            spawnableSOType = assembly.GetType("SpawnableSO");
                        }
                    }
                }
                if (cachedItemManager == null && itemManagerType != null)
                {
                    try
                    {
                        UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(itemManagerType);
                        if (array.Length != 0)
                        {
                            cachedItemManager = array[0];
                            ModMenuLoader.Log($"ItemManager found (cache): {array.Length} instances");
                        }
                    }
                    catch
                    {
                    }
                    ModMenuLoader.Log($"ItemManager found (cache): {cachedItemManager != null}");
                }
                if (spawnableSettingsType == null || spawnableSOType == null)
                {
                    ModMenuLoader.Log("SpawnableSettings or SpawnableSO type not found");
                    return;
                }
                if (cachedSpawnableSettings == null)
                {
                    MethodInfo method = typeof(UnityEngine.Object).GetMethod("FindObjectOfType", Array.Empty<Type>());
                    if (method != null)
                    {
                        MethodInfo methodInfo = method.MakeGenericMethod(spawnableSettingsType);
                        cachedSpawnableSettings = methodInfo.Invoke(null, null);
                    }
                    ModMenuLoader.Log($"SpawnableSettings found via FindObjectOfType: {cachedSpawnableSettings != null}");
                }
                if (cachedSpawnableSettings == null)
                {
                    try
                    {
                        UnityEngine.Object[] array2 = Resources.FindObjectsOfTypeAll(spawnableSettingsType);
                        if (array2.Length != 0)
                        {
                            cachedSpawnableSettings = array2[0];
                            ModMenuLoader.Log($"SpawnableSettings found via Resources: {array2.Length} instances");
                        }
                    }
                    catch (Exception ex)
                    {
                        ModMenuLoader.Log("Resources.FindObjectsOfTypeAll failed: " + ex.Message);
                    }
                }
                if (cachedSpawnableSettings == null)
                {
                    ModMenuLoader.Log("SpawnableSettings instance not found - try loading from asset");
                    if (cachedItemManager == null && itemManagerType != null)
                    {
                        PropertyInfo property = itemManagerType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
                        if (property != null)
                        {
                            cachedItemManager = property.GetValue(null);
                        }
                        else
                        {
                            MethodInfo method2 = typeof(UnityEngine.Object).GetMethod("FindObjectOfType", Array.Empty<Type>());
                            if (method2 != null)
                            {
                                MethodInfo methodInfo2 = method2.MakeGenericMethod(itemManagerType);
                                cachedItemManager = methodInfo2.Invoke(null, null);
                            }
                        }
                        ModMenuLoader.Log($"ItemManager found: {cachedItemManager != null}");
                    }
                    if (cachedItemManager != null && itemManagerType != null)
                    {
                        FieldInfo[] fields = itemManagerType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        foreach (FieldInfo fieldInfo in fields)
                        {
                            if (fieldInfo.FieldType == spawnableSettingsType)
                            {
                                cachedSpawnableSettings = fieldInfo.GetValue(cachedItemManager);
                                ModMenuLoader.Log("SpawnableSettings found via ItemManager." + fieldInfo.Name);
                                break;
                            }
                        }
                    }
                }
                if (cachedSpawnableSettings == null)
                {
                    ModMenuLoader.Log("Could not locate SpawnableSettings instance");
                    return;
                }
                FieldInfo field = spawnableSettingsType.GetField("spawnables", BindingFlags.Instance | BindingFlags.Public);
                if (field == null)
                {
                    ModMenuLoader.Log("'spawnables' field not found on SpawnableSettings");
                    return;
                }
                object value = field.GetValue(cachedSpawnableSettings);
                if (!(value is IList list))
                {
                    ModMenuLoader.Log("spawnables field is not a list: " + (value?.GetType().Name ?? "null"));
                    return;
                }
                ModMenuLoader.Log($"SpawnableSettings.spawnables count: {list.Count}");
                List<object> list2 = new List<object>();
                FieldInfo field2 = spawnableSOType.GetField("prefab", BindingFlags.Instance | BindingFlags.Public);
                FieldInfo field3 = spawnableSOType.GetField("spawnableName", BindingFlags.Instance | BindingFlags.Public);
                itemType = itemManagerType?.Assembly.GetType("Item");
                foreach (object item in list)
                {
                    if (item == null)
                    {
                        continue;
                    }
                    object? obj2 = null;
                    if (spawnableSOType.IsInstanceOfType(item))
                    {
                        obj2 = item;
                    }
                    else
                    {
                        FieldInfo field4 = item.GetType().GetField("spawnable", BindingFlags.Instance | BindingFlags.Public);
                        if (field4 != null)
                        {
                            obj2 = field4.GetValue(item);
                        }
                    }
                    if (obj2 == null)
                    {
                        continue;
                    }
                    string text = (field3?.GetValue(obj2) as string) ?? "???";
                    if (field2 != null && itemType != null)
                    {
                        GameObject? gameObject = field2.GetValue(obj2) as GameObject;
                        if (gameObject == null || gameObject.GetComponentInChildren(itemType) == null)
                        {
                            continue;
                        }
                    }
                    if (!(text == "Cosmetic Item"))
                    {
                        list2.Add(obj2);
                    }
                }
                ModMenuLoader.Log($"Extracted {list2.Count} grabbable items from {list.Count} entries");
                if (itemType != null)
                {
                    try
                    {
                        UnityEngine.Object[] array3 = UnityEngine.Object.FindObjectsByType(itemType, FindObjectsSortMode.None);
                        ModMenuLoader.Log($"Scene scan: found {array3.Length} Item instances");
                        HashSet<int> hashSet = new HashSet<int>();
                        FieldInfo field5 = spawnableSOType.GetField("spawnableID", BindingFlags.Instance | BindingFlags.Public);
                        foreach (object item2 in list2)
                        {
                            if (field5 != null && item2 != null)
                            {
                                hashSet.Add((int)field5.GetValue(item2));
                            }
                        }
                        int num = 0;
                        UnityEngine.Object[] array4 = array3;
                        foreach (UnityEngine.Object obj3 in array4)
                        {
                            if (obj3 == null)
                            {
                                continue;
                            }
                            FieldInfo field6 = itemType.GetField("spawnableSo", BindingFlags.Instance | BindingFlags.Public);
                            if (field6 == null)
                            {
                                continue;
                            }
                            object value2 = field6.GetValue(obj3);
                            if (value2 != null && spawnableSOType.IsInstanceOfType(value2) && !(field5 == null))
                            {
                                int num2 = (int)field5.GetValue(value2);
                                string text2 = (field3?.GetValue(value2) as string) ?? "???";
                                if (!hashSet.Contains(num2) && !(text2 == "Cosmetic Item"))
                                {
                                    hashSet.Add(num2);
                                    list2.Add(value2);
                                    num++;
                                    ModMenuLoader.Log($"  Added from scene: {text2} (ID={num2})");
                                }
                            }
                        }
                        if (num > 0)
                        {
                            ModMenuLoader.Log($"Added {num} items from scene scan");
                        }
                        else
                        {
                            ModMenuLoader.Log("Scene scan: no new items found");
                        }
                    }
                    catch (Exception ex2)
                    {
                        ModMenuLoader.Log("Scene scan error: " + ex2.Message);
                    }
                }
                cachedSpawnables = list2;
                ModMenuLoader.Log($"Loaded {cachedSpawnables.Count} spawnable items");
            }
            catch (Exception arg)
            {
                ModMenuLoader.Log($"LoadItemSpawnables error: {arg}");
            }
        }

    }
}
