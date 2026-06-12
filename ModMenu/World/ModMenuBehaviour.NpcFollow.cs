using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        private const float NpcFollowInterval = 0.35f;

        private const float NpcFollowTargetGracePeriod = 8f;

        // Starts a persistent follow using stable player and NPC identifiers
        private void StartNpcFollow(PlayerProfile targetProfile, NPC[] currentTargets)
        {
            npcFollowEnabled = true;
            npcFollowTargetSteamId = targetProfile.steamId;
            npcFollowScope = npcControlScope;
            npcFollowInstanceId = npcControlScope == 2 && currentTargets.Length == 1
                ? currentTargets[0].GetInstanceID()
                : 0;
            npcFollowUpdateTimer = 0f;
            npcFollowMissingTargetTime = 0f;
        }

        // Starts follow from a selected lobby profile using the current World scope
        private void StartNpcFollowForPlayer(PlayerProfile targetProfile)
        {
            NPC[] targets = ResolveCurrentNpcControlTargets();
            if (targets.Length == 0)
            {
                return;
            }
            StartNpcFollow(targetProfile, targets);
        }

        // Stops follow state without issuing a destructive NPC command
        private void StopNpcFollow()
        {
            npcFollowEnabled = false;
            npcFollowTargetSteamId = 0uL;
            npcFollowInstanceId = 0;
            npcFollowUpdateTimer = 0f;
            npcFollowMissingTargetTime = 0f;
        }

        // Refreshes destinations at a bounded rate because NPC behavior may replace one-shot paths
        private void UpdateNpcFollow()
        {
            if (!npcFollowEnabled)
            {
                return;
            }

            if (cachedGM == null)
            {
                // Scene loading temporarily removes the manager while the follow target remains valid
                return;
            }
            if (!cachedGM.isServer)
            {
                StopNpcFollow();
                return;
            }

            npcFollowUpdateTimer -= Time.unscaledDeltaTime;
            if (npcFollowUpdateTimer > 0f)
            {
                return;
            }
            npcFollowUpdateTimer = NpcFollowInterval;

            PlayerProfile[] profiles = UnityEngine.Object.FindObjectsByType<PlayerProfile>(FindObjectsSortMode.None);
            PlayerController? target = FindPlayerControllerBySteamId(profiles, npcFollowTargetSteamId);
            if (target == null)
            {
                // Mirror profile registration may lag behind scene activation
                npcFollowMissingTargetTime += NpcFollowInterval;
                if (npcFollowMissingTargetTime >= NpcFollowTargetGracePeriod)
                {
                    StopNpcFollow();
                }
                return;
            }
            npcFollowMissingTargetTime = 0f;

            NPC[] targets = ResolveNpcFollowTargets();
            if (targets.Length == 0)
            {
                StopNpcFollow();
                return;
            }

            // Spaced destinations prevent a bulk scope from collapsing into one body pile
            MoveNpcsAroundPoint(targets, target.transform.position, false);
        }

        // Resolves the saved scope against current scene objects for every follow refresh
        private NPC[] ResolveNpcFollowTargets()
        {
            NPC[] npcs = UnityEngine.Object.FindObjectsByType<NPC>(FindObjectsSortMode.None);
            if (npcs.Length == 0)
            {
                return npcs;
            }

            SortNpcsByHostDistance(npcs);
            if (npcFollowScope == 0)
            {
                // All scope includes NPCs created after follow started
                return npcs;
            }
            if (npcFollowScope == 1)
            {
                // Closest scope follows the nearest current NPC rather than a stale object
                return new NPC[1] { npcs[0] };
            }

            NPC? selectedNpc = FindNpcByInstanceId(npcs, npcFollowInstanceId);
            return selectedNpc == null ? System.Array.Empty<NPC>() : new NPC[1] { selectedNpc };
        }

        // Resolves the scope currently selected in World for a new player follow request
        private NPC[] ResolveCurrentNpcControlTargets()
        {
            NPC[] npcs = UnityEngine.Object.FindObjectsByType<NPC>(FindObjectsSortMode.None);
            if (npcs.Length == 0)
            {
                return npcs;
            }

            SortNpcsByHostDistance(npcs);
            if (npcControlScope == 0)
            {
                return npcs;
            }
            if (npcControlScope == 1)
            {
                return new NPC[1] { npcs[0] };
            }

            NPC? selectedNpc = FindNpcByInstanceId(npcs, selectedNpcInstanceId);
            return selectedNpc == null ? System.Array.Empty<NPC>() : new NPC[1] { selectedNpc };
        }

        // Returns a readable player label for persistent follow status
        private static string GetPlayerNameBySteamId(PlayerProfile[] profiles, ulong steamId)
        {
            foreach (PlayerProfile profile in profiles)
            {
                if (profile != null && profile.steamId == steamId)
                {
                    return string.IsNullOrEmpty(profile.playerName) ? "Unknown Player" : profile.playerName;
                }
            }

            return "Disconnected Player";
        }
    }
}
