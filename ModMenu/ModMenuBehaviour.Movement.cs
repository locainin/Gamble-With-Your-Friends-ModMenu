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
        // Local movement helpers only affect the client running this plugin

        // Applies local speed, jump, and gravity overrides from saved base values
        private void ApplyMovementHacks()
        {
            if (cachedPlayerSettings == null)
            {
                return;
            }
            try
            {
                FieldInfo field = cachedPlayerSettings.GetType().GetField("gravity", BindingFlags.Instance | BindingFlags.Public);
                if (!hasMovementBaseline)
                {
                    // Each available field keeps its real value, including valid zero values
                    originalMaxSpeed = ReadMovementValue(fMaxSpeed);
                    originalSprintMaxSpeed = ReadMovementValue(fSprintSpeed);
                    originalAcceleration = ReadMovementValue(fAccel);
                    originalJumpForce = ReadMovementValue(fJumpForce);
                    if (field != null)
                    {
                        originalGravity = (float)field.GetValue(cachedPlayerSettings);
                    }
                    hasMovementBaseline = true;
                }

                if (fMaxSpeed != null && fSprintSpeed != null && fAccel != null && originalMaxSpeed >= 0f &&
                    originalSprintMaxSpeed >= 0f && originalAcceleration >= 0f)
                {
                    float appliedSpeedMultiplier = speedHackEnabled ? speedMultiplier : 1f;
                    fMaxSpeed.SetValue(cachedPlayerSettings, originalMaxSpeed * appliedSpeedMultiplier);
                    fSprintSpeed.SetValue(cachedPlayerSettings, originalSprintMaxSpeed * appliedSpeedMultiplier);
                    fAccel.SetValue(cachedPlayerSettings, originalAcceleration * appliedSpeedMultiplier);
                }

                if (fJumpForce != null && originalJumpForce >= 0f)
                {
                    float appliedJumpMultiplier = jumpHackEnabled ? jumpMultiplier : 1f;
                    fJumpForce.SetValue(cachedPlayerSettings, originalJumpForce * appliedJumpMultiplier);
                }

                if (flyHackEnabled && field != null)
                {
                    field.SetValue(cachedPlayerSettings, 0f);
                }
                else if (field != null && originalGravity >= 0f)
                {
                    field.SetValue(cachedPlayerSettings, originalGravity);
                }
            }
            catch
            {
            }
        }

        // Reads one optional movement field without coupling unrelated overrides
        private float ReadMovementValue(FieldInfo? field)
        {
            return cachedPlayerSettings != null && field != null
                ? (float)field.GetValue(cachedPlayerSettings)
                : -1f;
        }

        // Restores the shared settings resource before scene changes or plugin teardown
        private void RestoreMovementOverrides()
        {
            if (cachedPlayerSettings == null || !hasMovementBaseline)
            {
                return;
            }

            try
            {
                if (fMaxSpeed != null && originalMaxSpeed >= 0f)
                {
                    fMaxSpeed.SetValue(cachedPlayerSettings, originalMaxSpeed);
                }
                if (fSprintSpeed != null && originalSprintMaxSpeed >= 0f)
                {
                    fSprintSpeed.SetValue(cachedPlayerSettings, originalSprintMaxSpeed);
                }
                if (fAccel != null && originalAcceleration >= 0f)
                {
                    fAccel.SetValue(cachedPlayerSettings, originalAcceleration);
                }
                if (fJumpForce != null && originalJumpForce >= 0f)
                {
                    fJumpForce.SetValue(cachedPlayerSettings, originalJumpForce);
                }

                FieldInfo? gravityField = cachedPlayerSettings.GetType().GetField("gravity", BindingFlags.Instance | BindingFlags.Public);
                if (gravityField != null && originalGravity >= 0f)
                {
                    gravityField.SetValue(cachedPlayerSettings, originalGravity);
                }
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("Movement restore error: " + ex.Message);
            }
        }

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        // Maps Unity key codes to virtual keys for no-clip movement
        private static bool IsKeyDown(KeyCode key)
        {
            if (key == KeyCode.None)
            {
                return false;
            }
            int num = 0;
            if (key >= KeyCode.A && key <= KeyCode.Z)
            {
                num = (int)(key - 32);
            }
            else if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
            {
                num = (int)key;
            }
            else
            {
                switch (key)
                {
                    case KeyCode.Space:
                        num = 32;
                        break;
                    case KeyCode.RightControl:
                    case KeyCode.LeftControl:
                        num = 17;
                        break;
                    case KeyCode.RightShift:
                    case KeyCode.LeftShift:
                        num = 16;
                        break;
                    case KeyCode.RightAlt:
                    case KeyCode.LeftAlt:
                        num = 18;
                        break;
                    case KeyCode.Return:
                        num = 13;
                        break;
                    case KeyCode.Escape:
                        num = 27;
                        break;
                    case KeyCode.Tab:
                        num = 9;
                        break;
                    case KeyCode.Mouse0:
                        num = 1;
                        break;
                    case KeyCode.Mouse1:
                        num = 2;
                        break;
                    case KeyCode.Mouse2:
                        num = 4;
                        break;
                }
            }
            if (num == 0)
            {
                num = (int)key;
            }
            return (GetAsyncKeyState(num) & 0x8000) != 0;
        }

        // Routes every no-clip toggle through the same input and physics cleanup
        private void SetFlyHackEnabled(bool isEnabled)
        {
            if (flyHackEnabled == isEnabled)
            {
                return;
            }

            flyHackEnabled = isEnabled;
            if (flyHackEnabled)
            {
                // Existing gameplay input is released before direct movement owns the body
                GameplayInputRelease.Send();
            }
            else
            {
                RestoreFlightPhysics();
            }
        }

        // Moves the local player while no clip is enabled and restores physics on exit
        private void FlyUpdate()
        {
            if (cachedLocalPC == null || (!flyHackEnabled && !wasFlying))
            {
                return;
            }
            try
            {
                FieldInfo field = typeof(PlayerController).GetField("_rb", BindingFlags.Instance | BindingFlags.NonPublic);
                if (!(field != null))
                {
                    return;
                }
                Rigidbody? rigidbody = field.GetValue(cachedLocalPC) as Rigidbody;
                if (!(rigidbody != null))
                {
                    return;
                }
                if (flyHackEnabled)
                {
                    if (!wasFlying)
                    {
                        // Kinematic mode prevents physics from fighting direct position movement
                        wasKinematicBeforeFlying = rigidbody.isKinematic;
                        rigidbody.linearVelocity = Vector3.zero;
                        rigidbody.angularVelocity = Vector3.zero;
                        rigidbody.isKinematic = true;
                        wasFlying = true;
                    }
                    Transform transform = ((Camera.main != null) ? Camera.main.transform : cachedLocalPC.transform);
                    // Horizontal vectors keep normal movement from inheriting camera pitch
                    Vector3 forward = FlattenFlightDirection(transform.forward, cachedLocalPC.transform.forward);
                    Vector3 right = FlattenFlightDirection(transform.right, cachedLocalPC.transform.right);
                    Vector3 zero = Vector3.zero;
                    FieldInfo field2 = typeof(PlayerController).GetField("_moveInput", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field2 != null)
                    {
                        // Reuse the game's processed input so rebinding and controllers still work
                        Vector2 vector = (Vector2)field2.GetValue(cachedLocalPC);
                        zero += forward * vector.y;
                        zero += right * vector.x;
                    }
                    else
                    {
                        // Keyboard polling remains available when the private input field changes
                        if (IsKeyDown(KeyCode.W))
                        {
                            zero += forward;
                        }
                        if (IsKeyDown(KeyCode.S))
                        {
                            zero -= forward;
                        }
                        if (IsKeyDown(KeyCode.A))
                        {
                            zero -= right;
                        }
                        if (IsKeyDown(KeyCode.D))
                        {
                            zero += right;
                        }
                    }
                    if (IsKeyDown(flyUpKey))
                    {
                        zero += Vector3.up;
                    }
                    if (IsKeyDown(flyDownKey))
                    {
                        zero -= Vector3.up;
                    }
                    if (zero.sqrMagnitude > 0.0001f)
                    {
                        cachedLocalPC.transform.position += zero.normalized * 15f * flySpeedMultiplier * Time.deltaTime;
                    }
                }
                else if (wasFlying)
                {
                    // Physics resumes only after a no-clip session actually changed the body
                    rigidbody.linearVelocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                    rigidbody.isKinematic = wasKinematicBeforeFlying;
                    wasFlying = false;
                    wasKinematicBeforeFlying = false;
                }
            }
            catch
            {
            }
        }

        // Keeps camera pitch from turning forward movement into vertical falling or climbing
        private static Vector3 FlattenFlightDirection(Vector3 direction, Vector3 fallback)
        {
            Vector3 flattened = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (flattened.sqrMagnitude > 0.0001f)
            {
                return flattened.normalized;
            }

            return Vector3.ProjectOnPlane(fallback, Vector3.up).normalized;
        }

        // Restores the local rigidbody when no clip is interrupted outside the normal update path
        private void RestoreFlightPhysics()
        {
            if (cachedLocalPC == null || !wasFlying)
            {
                return;
            }

            try
            {
                FieldInfo? field = typeof(PlayerController).GetField("_rb", BindingFlags.Instance | BindingFlags.NonPublic);
                Rigidbody? rigidbody = field?.GetValue(cachedLocalPC) as Rigidbody;
                if (rigidbody != null)
                {
                    rigidbody.linearVelocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                    rigidbody.isKinematic = wasKinematicBeforeFlying;
                }
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("No clip restore error: " + ex.Message);
            }
            finally
            {
                wasFlying = false;
                wasKinematicBeforeFlying = false;
            }
        }

    }
}
