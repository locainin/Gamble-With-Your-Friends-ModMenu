using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Assigns the current World NPC scope to any selected lobby player
        private void DrawNpcFollowPlayerControl(PlayerProfile profile, bool isHost)
        {
            DrawSection("NPC Follow Target");
            bool hasStableIdentity = profile.hasSynced && profile.steamId != 0uL;
            bool followsThisPlayer = hasStableIdentity &&
                npcFollowEnabled &&
                npcFollowTargetSteamId == profile.steamId;
            NPC[] availableTargets = UnityEngine.Object.FindObjectsByType<NPC>(FindObjectsSortMode.None);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled &&
                isHost &&
                hasStableIdentity &&
                (followsThisPlayer || availableTargets.Length > 0);

            if (GUILayout.Button(followsThisPlayer ? "Stop Following This Player" : "Follow This Player"))
            {
                if (followsThisPlayer)
                {
                    StopNpcFollow();
                }
                else
                {
                    // Starting with another profile replaces the previous follow target
                    StartNpcFollowForPlayer(profile);
                }
            }

            GUI.enabled = previousEnabled;
            string scopeLabel = npcScopeNames[Mathf.Clamp(npcControlScope, 0, npcScopeNames.Length - 1)];
            GUILayout.Label($"  NPC scope: {scopeLabel}  |  Change scope in World", smallLabelStyle);
            if (!hasStableIdentity)
            {
                GUILayout.Label("  Waiting for player identity synchronization", smallLabelStyle);
            }
            if (!isHost)
            {
                DrawHostWarning("Host authority is required for NPC follow");
            }
        }
    }
}
