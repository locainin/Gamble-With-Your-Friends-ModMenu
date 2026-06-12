using System;
using System.Reflection;
using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Items tab owns search and spawn presentation beside item runtime helpers

        // Renders searchable item spawning controls
        private void DrawItemsTab(bool isHost)
        {
            // Item spawning uses server-owned NetworkServer.Spawn calls
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && isHost;

            int totalItems = cachedSpawnables?.Count ?? 0;
            DrawSection($"Item Spawner  {totalItems}");
            if (!isHost)
            {
                GUI.enabled = previousEnabled;
                DrawHostWarning("Host authority is required to spawn items");
                GUI.enabled = false;
            }
            if (cachedSpawnables == null || cachedSpawnables.Count == 0)
            {
                // Spawnables are populated only after the game item manager exists
                GUIStyle gUIStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    wordWrap = true
                };
                gUIStyle.normal.textColor = new Color(1f, 0.6f, 0.2f);
                GUILayout.Label("  No items loaded yet. Enter a game to populate the item list.", gUIStyle);
                GUI.enabled = previousEnabled;
                return;
            }
            // Search occupies the full row while Clear remains a predictable fixed action
            GUILayout.BeginHorizontal();
            itemSearchFilter = GUILayout.TextField(itemSearchFilter, GUILayout.ExpandWidth(true));
            bool hasFilter = !string.IsNullOrEmpty(itemSearchFilter);
            GUI.enabled = previousEnabled && isHost && hasFilter;
            if (GUILayout.Button("Clear", GUILayout.Width(72f)))
            {
                itemSearchFilter = "";
            }
            GUI.enabled = previousEnabled && isHost;
            GUILayout.EndHorizontal();

            GUILayout.Label(hasFilter ? $"  Filter: {itemSearchFilter}" : "  Showing all spawnable items", smallLabelStyle);
            itemListScrollPos = GUILayout.BeginScrollView(itemListScrollPos, false, true, GUILayout.Height(390f));

            // Normalize the filter once instead of once for every item
            string value = itemSearchFilter ?? "";
            int visibleCount = 0;
            for (int i = 0; i < cachedSpawnables.Count; i++)
            {
                object spawnable = cachedSpawnables[i];
                if (spawnable == null)
                {
                    continue;
                }

                string itemName = GetSpawnableDisplayName(spawnable);
                if (string.IsNullOrEmpty(value) || itemName.Contains(value, StringComparison.OrdinalIgnoreCase))
                {
                    visibleCount++;
                    GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Height(36f));
                    GUILayout.Label(itemName, bodyLabelStyle);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Spawn", GUILayout.Width(88f), GUILayout.Height(28f)))
                    {
                        SpawnItem(spawnable);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            if (visibleCount == 0)
            {
                GUILayout.Label("  No items match this search", bodyLabelStyle);
            }
            GUILayout.EndScrollView();
            GUI.enabled = previousEnabled;
        }

        // Reads the reflected ScriptableObject name and maps internal labels to menu labels
        private string GetSpawnableDisplayName(object spawnable)
        {
            try
            {
                FieldInfo? nameField = spawnableSOType?.GetField("spawnableName", BindingFlags.Instance | BindingFlags.Public);
                string itemName = (nameField?.GetValue(spawnable) as string) ?? "Unknown Item";

                // Coordinator is the internal name for the player-facing camera item
                return itemName == "Coordinator" ? "Camera" : itemName;
            }
            catch (Exception exception)
            {
                // One malformed entry should not hide the remaining spawnable list
                ModMenuLoader.Log("Could not read spawnable name: " + exception.Message);
                return "Unknown Item";
            }
        }

    }
}
