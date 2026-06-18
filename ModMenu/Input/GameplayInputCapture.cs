using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;

namespace ModMenu
{
    internal static class GameplayInputRelease
    {
        private const int VirtualKeyA = 65;
        private const int VirtualKeyD = 68;
        private const int VirtualKeyS = 83;
        private const int VirtualKeyW = 87;
        private const int VirtualKeyShift = 16;
        private const int VirtualKeyControl = 17;
        private const int VirtualKeySpace = 32;

        // InputEvents is resolved by name so the patch remains isolated from game internals
        private static readonly Type? InputEventsType = AccessTools.TypeByName("InputEvents");

        // Every held boolean action receives a matching release when the menu opens
        private static readonly string[] BooleanReleaseMethods = new string[11]
        {
            "UpdateJump",
            "UpdateCrouch",
            "UpdateSprint",
            "UpdateInteract",
            "UpdateZoom",
            "UpdateUseItem",
            "UpdateThrowItem",
            "UpdateEmoteWheel",
            "UpdatePing",
            "UpdateSkipUI",
            "UpdatePushToTalk"
        };

        // Clears movement, aim, and held buttons before gameplay callbacks are blocked
        internal static void Send()
        {
            if (InputEventsType == null)
            {
                return;
            }

            InvokeVector2Event("OnMoveEvent", Vector2.zero);
            InvokeVector2Event("OnAimEvent", Vector2.zero);
            foreach (string methodName in BooleanReleaseMethods)
            {
                InvokeBooleanRelease(methodName);
            }
        }

        // Replays keys still held when the menu closes so gameplay does not wait for a key cycle
        internal static void SendKeyboardSnapshot()
        {
            if (InputEventsType == null)
            {
                return;
            }

            try
            {
                Vector2 move = Vector2.zero;
                // Movement keys need a fresh state because InputReader callbacks were blocked
                if (IsVirtualKeyDown(VirtualKeyW))
                {
                    move.y += 1f;
                }
                if (IsVirtualKeyDown(VirtualKeyS))
                {
                    move.y -= 1f;
                }
                if (IsVirtualKeyDown(VirtualKeyD))
                {
                    move.x += 1f;
                }
                if (IsVirtualKeyDown(VirtualKeyA))
                {
                    move.x -= 1f;
                }

                InvokeVector2Event("OnMoveEvent", Vector2.ClampMagnitude(move, 1f));
                InvokeBooleanState("UpdateSprint", IsVirtualKeyDown(VirtualKeyShift));
                InvokeBooleanState("UpdateJump", IsVirtualKeyDown(VirtualKeySpace));
                InvokeBooleanState("UpdateCrouch", IsVirtualKeyDown(VirtualKeyControl));
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("Input resume snapshot error: " + ex.Message);
            }
        }

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        // Reads one Windows virtual key through Proton without depending on Unity input modules
        private static bool IsVirtualKeyDown(int virtualKey)
        {
            return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }

        // Invokes a static Vector2 input event when the current game build exposes it
        private static void InvokeVector2Event(string fieldName, Vector2 value)
        {
            FieldInfo? field = AccessTools.Field(InputEventsType, fieldName);
            if (field?.GetValue(null) is Action<Vector2> inputEvent)
            {
                inputEvent.Invoke(value);
            }
        }

        // Invokes a static boolean input method with a released state
        private static void InvokeBooleanRelease(string methodName)
        {
            InvokeBooleanState(methodName, false);
        }

        // Invokes a static boolean input method with the requested pressed state
        private static void InvokeBooleanState(string methodName, bool isPressed)
        {
            MethodInfo? method = AccessTools.Method(InputEventsType, methodName);
            method?.Invoke(null, new object[1] { isPressed });
        }
    }

    [HarmonyPatch]
    internal static class InputReaderGameplayPatch
    {
        // Only gameplay actions are blocked so the menu toggle and unrelated UI remain responsive
        private static readonly HashSet<string> BlockedMethods = new HashSet<string>(StringComparer.Ordinal)
        {
            "OnMove",
            "OnAim",
            "OnJump",
            "OnCrouch",
            "OnSprint",
            "OnInteract",
            "OnZoom",
            "OnItemSelect",
            "OnScroll",
            "OnUseItem",
            "OnThrowItem",
            "OnEmoteWheel",
            "OnPing",
            "OnSkipUI",
            "OnPushToTalk"
        };

        // Resolves optional methods individually so a game update cannot break the full patch set
        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type? inputReaderType = AccessTools.TypeByName("InputReader");
            if (inputReaderType == null)
            {
                yield break;
            }

            foreach (string methodName in BlockedMethods)
            {
                MethodInfo? method = AccessTools.Method(inputReaderType, methodName);
                if (method != null)
                {
                    yield return method;
                }
            }
        }

        // Returning false prevents the game callback while the menu owns mouse and keyboard input
        private static bool Prefix()
        {
            return !ModMenuBehaviour.IsMenuOpen;
        }
    }
}
