using System;
using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        private readonly string[] npcScopeNames = new string[3] { "All NPCs", "Closest", "Individual" };

        // NPC management defaults to a bulk target and reveals a roster only when needed
        private void DrawNpcWorkspace(bool isHost)
        {
            NPC[] npcs = UnityEngine.Object.FindObjectsByType<NPC>(FindObjectsSortMode.None);
            SortNpcsByHostDistance(npcs);

            DrawNpcScopeNavigation(npcs.Length);
            if (npcs.Length == 0)
            {
                GUILayout.Label("  No NPCs are active in this scene", bodyLabelStyle);
                return;
            }

            EnsureSelectedNpc(npcs);
            if (npcControlScope == 2)
            {
                DrawIndividualNpcWorkspace(npcs, isHost);
                return;
            }

            NPC[] targets = npcControlScope == 1 ? new NPC[1] { npcs[0] } : npcs;
            string targetLabel = npcControlScope == 1 ? "Closest NPC" : $"All {npcs.Length} NPCs";
            DrawNpcTargetInspector(targets, targetLabel, isHost);
        }

        // Presents bulk, nearest, and individual scope as mutually exclusive modes
        private void DrawNpcScopeNavigation(int npcCount)
        {
            DrawSection($"NPC Control  {npcCount}");
            GUILayout.BeginHorizontal();
            for (int index = 0; index < npcScopeNames.Length; index++)
            {
                GUIStyle scopeStyle = npcControlScope == index ? activeTabStyle : GUI.skin.button;
                if (GUILayout.Button(npcScopeNames[index], scopeStyle, GUILayout.Height(30f)))
                {
                    npcControlScope = index;
                    npcInspectorScrollPos = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
        }

        // Uses a roster only for explicit individual targeting
        private void DrawIndividualNpcWorkspace(NPC[] npcs, bool isHost)
        {
            NPC? selectedNpc = FindNpcByInstanceId(npcs, selectedNpcInstanceId);
            GUILayout.BeginHorizontal();
            DrawNpcRoster(npcs);
            GUILayout.Space(10f);

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            if (selectedNpc != null)
            {
                int selectedIndex = Array.IndexOf(npcs, selectedNpc);
                DrawNpcTargetInspector(new NPC[1] { selectedNpc }, GetNpcLabel(selectedNpc, selectedIndex), isHost);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        // Keeps NPC selection valid when a floor rebuilds its population
        private void EnsureSelectedNpc(NPC[] npcs)
        {
            if (FindNpcByInstanceId(npcs, selectedNpcInstanceId) == null)
            {
                selectedNpcInstanceId = npcs[0].GetInstanceID();
            }
        }

        // Renders a distance-sorted individual roster
        private void DrawNpcRoster(NPC[] npcs)
        {
            GUILayout.BeginVertical(GUILayout.Width(176f));
            GUILayout.Label("NEAREST FIRST", smallLabelStyle);
            npcListScrollPos = GUILayout.BeginScrollView(npcListScrollPos, false, true, GUILayout.Height(350f));
            for (int index = 0; index < npcs.Length; index++)
            {
                NPC npc = npcs[index];
                int instanceId = npc.GetInstanceID();
                GUIStyle rowStyle = instanceId == selectedNpcInstanceId ? activeTabStyle : tabStyle;
                if (GUILayout.Button(GetNpcLabel(npc, index), rowStyle, GUILayout.Height(34f)))
                {
                    selectedNpcInstanceId = instanceId;
                    npcInspectorScrollPos = Vector2.zero;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        // Renders one action surface for the current NPC target scope
        private void DrawNpcTargetInspector(NPC[] targets, string targetLabel, bool isHost)
        {
            npcInspectorScrollPos = GUILayout.BeginScrollView(npcInspectorScrollPos, false, true, GUILayout.Height(390f));
            DrawSection(targetLabel);
            GUILayout.Label(GetNpcSummary(targets), smallLabelStyle);

            if (!isHost)
            {
                DrawHostWarning("NPC movement and physics are server-owned");
                GUILayout.EndScrollView();
                return;
            }

            DrawNpcDestinationControls(targets);
            DrawNpcPhysicsControls(targets);
            GUILayout.EndScrollView();
        }

        // Sends the active NPC scope toward a player or beside the host
        private void DrawNpcDestinationControls(NPC[] targets)
        {
            DrawSection("Destination");
            PlayerController? localPlayer = cachedLocalPC;

            if (npcFollowEnabled)
            {
                PlayerProfile[] profiles = UnityEngine.Object.FindObjectsByType<PlayerProfile>(FindObjectsSortMode.None);
                string followedName = GetPlayerNameBySteamId(profiles, npcFollowTargetSteamId);
                GUILayout.Label($"  Following {followedName}  |  {npcScopeNames[npcFollowScope]}", smallLabelStyle);
            }
            else
            {
                GUILayout.Label("  Choose a follow target from Players", smallLabelStyle);
            }

            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && localPlayer != null;
            if (GUILayout.Button("Warp Beside Host"))
            {
                MoveNpcsAroundPoint(targets, localPlayer!.transform.position, true);
            }
            GUI.enabled = previousEnabled;
        }

        // Applies reversible server-side physics to every NPC in scope
        private void DrawNpcPhysicsControls(NPC[] targets)
        {
            DrawSection("Physical Effects");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Shove"))
            {
                ApplyNpcKnockback(targets, npc => npc.Transform.forward * 8f + Vector3.up * 2f, 8f);
            }
            if (GUILayout.Button("Sky Launch"))
            {
                ApplyNpcKnockback(targets, _ => Vector3.up * 20f, 10f);
            }
            if (GUILayout.Button("Ragdoll"))
            {
                ApplyNpcKnockback(targets, _ => Vector3.up, 3f);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Reset To Free State"))
            {
                foreach (NPC npc in targets)
                {
                    npc.State = NPC.NPCState.Free;
                }
            }
        }

        // Spaces bulk movement targets so NPCs do not occupy one exact point
        private static void MoveNpcsAroundPoint(NPC[] targets, Vector3 center, bool warp)
        {
            for (int index = 0; index < targets.Length; index++)
            {
                float angle = targets.Length == 1 ? 0f : index * Mathf.PI * 2f / targets.Length;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.8f;
                if (warp)
                {
                    targets[index].Warp(center + offset);
                }
                else
                {
                    targets[index].SetDestination(center + offset);
                }
            }
        }

        // Applies one force profile while keeping per-NPC torque varied
        private static void ApplyNpcKnockback(NPC[] targets, Func<NPC, Vector3> forceFactory, float torqueStrength)
        {
            foreach (NPC npc in targets)
            {
                npc.ServerKnockback(forceFactory(npc), UnityEngine.Random.insideUnitSphere * torqueStrength);
            }
        }

        // Orders NPCs by distance from the host without allocating wrapper objects
        private void SortNpcsByHostDistance(NPC[] npcs)
        {
            if (cachedLocalPC == null)
            {
                return;
            }

            Vector3 hostPosition = cachedLocalPC.transform.position;
            Array.Sort(npcs, (left, right) =>
                (left.Transform.position - hostPosition).sqrMagnitude.CompareTo(
                    (right.Transform.position - hostPosition).sqrMagnitude));
        }

        // Builds a stable row label from sorted position and host distance
        private string GetNpcLabel(NPC npc, int sortedIndex)
        {
            int displayIndex = sortedIndex + 1;
            if (cachedLocalPC == null)
            {
                return $"NPC {displayIndex}";
            }

            float distance = Vector3.Distance(cachedLocalPC.transform.position, npc.Transform.position);
            return $"NPC {displayIndex}   {distance:F1}m";
        }

        // Summarizes a bulk scope without repeating an inspector for every NPC
        private static string GetNpcSummary(NPC[] targets)
        {
            if (targets.Length == 1)
            {
                NPC npc = targets[0];
                string behavior = npc.Behavior != null ? npc.Behavior.GetType().Name : "Unavailable";
                return $"  {npc.State}  |  {behavior}  |  {FormatPosition(npc.Transform.position)}";
            }

            int freeCount = 0;
            for (int index = 0; index < targets.Length; index++)
            {
                if (targets[index].State == NPC.NPCState.Free)
                {
                    freeCount++;
                }
            }
            return $"  {freeCount} free  |  {targets.Length - freeCount} ragdolled";
        }

        // Finds an NPC from the current scene snapshot
        private static NPC? FindNpcByInstanceId(NPC[] npcs, int instanceId)
        {
            foreach (NPC npc in npcs)
            {
                if (npc.GetInstanceID() == instanceId)
                {
                    return npc;
                }
            }
            return null;
        }

        // Formats world coordinates without noisy precision
        private static string FormatPosition(Vector3 position)
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0:F1}, {1:F1}, {2:F1}",
                position.x,
                position.y,
                position.z);
        }
    }
}
