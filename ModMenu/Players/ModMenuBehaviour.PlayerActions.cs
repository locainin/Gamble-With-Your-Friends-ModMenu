using System;
using System.Reflection;
using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Reversible host actions use the game server methods and synchronized state

        // Renders reversible host actions for one target player
        private void DrawPlayerTrollControls(PlayerProfile playerProfile, PlayerController playerController, bool isHost)
        {
            if (playerProfile == null || playerController == null)
            {
                return;
            }

            if (!isHost)
            {
                DrawHostWarning("Host authority is required for player effects");
                return;
            }

            DrawSection("Physical Effects");
            // These call the game's server methods so the target client receives normal TargetRpc updates
            bool isFrozen = frozenPlayerIds.Contains(playerProfile.steamId);
            bool isHeadLocked = headLockedPlayerIds.Contains(playerProfile.steamId);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Wake"))
            {
                // Wake also clears the menu's requested freeze state for this player
                playerController.ServerWakeUp();
                frozenPlayerIds.Remove(playerProfile.steamId);
            }
            if (GUILayout.Button(isFrozen ? "Unfreeze" : "Freeze"))
            {
                // One stateful action avoids presenting opposite commands at the same time
                bool shouldFreeze = !isFrozen;
                playerController.ServerLock(shouldFreeze);
                if (shouldFreeze)
                {
                    frozenPlayerIds.Add(playerProfile.steamId);
                }
                else
                {
                    frozenPlayerIds.Remove(playerProfile.steamId);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(isHeadLocked ? "Unlock Head" : "Lock Head"))
            {
                // Requested state is tracked because the game does not expose a synced lock flag
                bool shouldLockHead = !isHeadLocked;
                playerController.ServerLockHead(shouldLockHead);
                if (shouldLockHead)
                {
                    headLockedPlayerIds.Add(playerProfile.steamId);
                }
                else
                {
                    headLockedPlayerIds.Remove(playerProfile.steamId);
                }
            }
            if (GUILayout.Button("Spin View"))
            {
                playerController.ServerRotate(new Vector2(UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-60f, 60f)));
            }
            GUILayout.EndHorizontal();

            DrawSection("Movement Effects");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pop Up"))
            {
                KnockPlayer(playerController, Vector3.up * 8f, UnityEngine.Random.insideUnitSphere * 6f);
            }
            if (GUILayout.Button("Shove"))
            {
                Vector3 direction = cachedLocalPC != null ? (playerController.transform.position - cachedLocalPC.transform.position).normalized : playerController.transform.forward;
                KnockPlayer(playerController, direction * 8f + Vector3.up * 2f, UnityEngine.Random.insideUnitSphere * 8f);
            }
            if (GUILayout.Button("Random Hop"))
            {
                TeleportPlayerNearby(playerController);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ragdoll"))
            {
                KnockPlayer(playerController, Vector3.up * 0.5f, UnityEngine.Random.insideUnitSphere * 2f);
            }
            if (GUILayout.Button("Sky Launch"))
            {
                KnockPlayer(playerController, Vector3.up * 22f, UnityEngine.Random.insideUnitSphere * 12f);
            }
            if (GUILayout.Button("Drain Energy"))
            {
                SetPlayerEnergy(playerController, 0f);
            }
            GUILayout.EndHorizontal();

            DrawSection("Recovery");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refill Energy"))
            {
                SetPlayerEnergy(playerController, 100f);
            }
            if (GUILayout.Button("Reset Effects"))
            {
                ResetPlayerEffects(playerProfile, playerController);
            }
            GUILayout.EndHorizontal();

            DrawPlayerBuffControls(playerController);
        }

        // Applies short server-owned buffs through the game's synchronized buff component
        private void DrawPlayerBuffControls(PlayerController playerController)
        {
            PlayerBuff playerBuff = playerController.GetComponent<PlayerBuff>();
            if (playerBuff == null)
            {
                return;
            }

            DrawSection("Temporary Buffs");
            GUILayout.Label("  Buffs last 30 seconds and use the game's normal replicated state", smallLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Immunity"))
            {
                playerBuff.ApplyBuff(PlayerBuffType.Immunity, 1f, 30f);
            }
            if (GUILayout.Button("Tipsy Fortune"))
            {
                playerBuff.ApplyBuff(PlayerBuffType.TipsyFortune, 1f, 30f);
            }
            if (GUILayout.Button("Inspiring Melody"))
            {
                playerBuff.ApplyBuff(PlayerBuffType.InspiringMelody, 1f, 30f);
            }
            GUILayout.EndHorizontal();
        }

        // Applies a host-authoritative knockback to one player
        private void KnockPlayer(PlayerController playerController, Vector3 force, Vector3 torque)
        {
            if (playerController == null || cachedGM == null || !cachedGM.isServer)
            {
                return;
            }
            // ServerKnockback also drops the held item through the game's normal flow
            playerController.ServerKnockback(force, torque);
        }

        // Sets synchronized energy while running as host
        private void SetPlayerEnergy(PlayerController playerController, float energy)
        {
            if (playerController == null || cachedGM == null || !cachedGM.isServer)
            {
                return;
            }
            PlayerEnergy playerEnergy = playerController.GetComponent<PlayerEnergy>();
            if (playerEnergy != null)
            {
                // The SyncVar setter propagates the value to connected clients
                playerEnergy.Networkenergy = Mathf.Clamp(energy, 0f, 100f);
            }
        }

        // Clears temporary menu effects from one player
        private void ResetPlayerEffects(PlayerProfile playerProfile, PlayerController playerController)
        {
            if (playerProfile == null || playerController == null || cachedGM == null || !cachedGM.isServer)
            {
                return;
            }

            // Restore the server-owned movement state first
            playerController.ServerLock(false);
            playerController.ServerLockHead(false);
            playerController.ServerWakeUp();
            SetPlayerEnergy(playerController, 100f);

            // Remove local state markers so button labels immediately return to their defaults
            frozenPlayerIds.Remove(playerProfile.steamId);
            headLockedPlayerIds.Remove(playerProfile.steamId);

        }

    }
}
