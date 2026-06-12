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
            if (cachedPlayerSettings == null || fMaxSpeed == null || fSprintSpeed == null || fAccel == null || fJumpForce == null)
            {
                return;
            }
            try
            {
                FieldInfo field = cachedPlayerSettings.GetType().GetField("gravity", BindingFlags.Instance | BindingFlags.Public);
                if (originalMaxSpeed < 0f)
                {
                    // Base values are captured once for this scene and restored when toggles turn off
                    originalMaxSpeed = (float)fMaxSpeed.GetValue(cachedPlayerSettings);
                    originalSprintMaxSpeed = (float)fSprintSpeed.GetValue(cachedPlayerSettings);
                    originalAcceleration = (float)fAccel.GetValue(cachedPlayerSettings);
                    originalJumpForce = (float)fJumpForce.GetValue(cachedPlayerSettings);
                    if (field != null)
                    {
                        originalGravity = (float)field.GetValue(cachedPlayerSettings);
                    }
                }
                if (speedHackEnabled)
                {
                    fMaxSpeed.SetValue(cachedPlayerSettings, originalMaxSpeed * speedMultiplier);
                    fSprintSpeed.SetValue(cachedPlayerSettings, originalSprintMaxSpeed * speedMultiplier);
                    fAccel.SetValue(cachedPlayerSettings, originalAcceleration * speedMultiplier);
                }
                else if (originalMaxSpeed > 0f)
                {
                    fMaxSpeed.SetValue(cachedPlayerSettings, originalMaxSpeed);
                    fSprintSpeed.SetValue(cachedPlayerSettings, originalSprintMaxSpeed);
                    fAccel.SetValue(cachedPlayerSettings, originalAcceleration);
                }
                if (jumpHackEnabled)
                {
                    fJumpForce.SetValue(cachedPlayerSettings, originalJumpForce * jumpMultiplier);
                }
                else if (originalJumpForce > 0f)
                {
                    fJumpForce.SetValue(cachedPlayerSettings, originalJumpForce);
                }
                if (flyHackEnabled && field != null)
                {
                    field.SetValue(cachedPlayerSettings, 0f);
                }
                else if (!flyHackEnabled && field != null && originalGravity > 0f)
                {
                    field.SetValue(cachedPlayerSettings, originalGravity);
                }
            }
            catch
            {
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
                        rigidbody.isKinematic = true;
                        wasFlying = true;
                    }
                    Transform transform = ((Camera.main != null) ? Camera.main.transform : cachedLocalPC.transform);
                    Vector3 zero = Vector3.zero;
                    FieldInfo field2 = typeof(PlayerController).GetField("_moveInput", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field2 != null)
                    {
                        // Reuse the game's processed input so rebinding and controllers still work
                        Vector2 vector = (Vector2)field2.GetValue(cachedLocalPC);
                        zero += transform.forward * vector.y;
                        zero += transform.right * vector.x;
                    }
                    else
                    {
                        // Keyboard polling remains available when the private input field changes
                        if (IsKeyDown(KeyCode.W))
                        {
                            zero += transform.forward;
                        }
                        if (IsKeyDown(KeyCode.S))
                        {
                            zero -= transform.forward;
                        }
                        if (IsKeyDown(KeyCode.A))
                        {
                            zero -= transform.right;
                        }
                        if (IsKeyDown(KeyCode.D))
                        {
                            zero += transform.right;
                        }
                    }
                    if ((flyUpKey != KeyCode.None && IsKeyDown(flyUpKey)) || IsKeyDown(KeyCode.Space))
                    {
                        zero += Vector3.up;
                    }
                    if ((flyDownKey != KeyCode.None && IsKeyDown(flyDownKey)) || IsKeyDown(KeyCode.LeftControl))
                    {
                        zero -= Vector3.up;
                    }
                    cachedLocalPC.transform.position += zero.normalized * 15f * flySpeedMultiplier * Time.deltaTime;
                }
                else if (wasFlying)
                {
                    // Physics resumes only after a no-clip session actually changed the body
                    rigidbody.isKinematic = false;
                    wasFlying = false;
                }
            }
            catch
            {
            }
        }

    }
}
