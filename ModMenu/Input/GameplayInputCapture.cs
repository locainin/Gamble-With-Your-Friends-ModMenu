using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ModMenu
{
    internal static class GameplayInputRelease
    {
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
            MethodInfo? method = AccessTools.Method(InputEventsType, methodName);
            method?.Invoke(null, new object[1] { false });
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
