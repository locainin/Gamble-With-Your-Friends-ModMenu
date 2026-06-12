using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Host actions update server-owned state and write organ edits to the active save

        // Restores selected local organs through the persistent host path
        private void HealPlayer(string part = "all")
        {
            if (cachedLocalPC == null)
            {
                ModMenuLoader.Log("No local player found");
                return;
            }
            try
            {
                PlayerOrgans? playerOrgans = FindPlayerOrgans(cachedLocalPC);
                if (playerOrgans == null)
                {
                    ModMenuLoader.Log("PlayerOrgans not found");
                    return;
                }
                bool leftEye;
                bool rightEye;
                bool body;
                bool mouth;
                TryGetOrganState(playerOrgans, cachedGM != null && cachedGM.isServer, out leftEye, out rightEye, out body, out mouth);
                if (part == "all" || part == "leftEye")
                {
                    leftEye = true;
                }
                if (part == "all" || part == "rightEye")
                {
                    rightEye = true;
                }
                if (part == "all" || part == "body")
                {
                    body = true;
                }
                if (part == "all" || part == "mouth")
                {
                    mouth = true;
                }
                SetPlayerOrgans(playerOrgans, leftEye, rightEye, body, mouth);
                ModMenuLoader.Log("Persistent heal complete: " + part);
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("Heal error: " + ex.Message);
            }
        }

        // Adds or removes host timer seconds through synchronized state
        private void ModifyDayTimer(float seconds)
        {
            if (cachedGM == null)
            {
                ModMenuLoader.Log("Not in a game");
                return;
            }
            try
            {
                if (cachedGM.isServer)
                {
                    cachedGM.Network_timer -= seconds;
                    ModMenuLoader.Log(string.Format(CultureInfo.InvariantCulture, "{0} {1}s!", (seconds > 0f) ? "Added" : "Subtracted", Mathf.Abs(seconds)));
                    ModMenuLoader.Log($"Timer modified by {seconds}s. New timer: {cachedGM.Network_timer}");
                }
                else
                {
                    ModMenuLoader.Log("Must be host to modify timer!");
                }
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("Error: " + ex.Message);
                ModMenuLoader.Log($"ModifyDayTimer error: {ex}");
            }
        }

        // Writes current organ state to both the save file and selected PlayerPrefs data
        private void PersistOrganChanges()
        {
            if (cachedGM == null || !cachedGM.isServer)
            {
                return;
            }
            try
            {
                SaveManager saveManager = UnityEngine.Object.FindFirstObjectByType<SaveManager>();
                if (saveManager == null)
                {
                    ModMenuLoader.Log("Organ save skipped: SaveManager not found");
                    return;
                }
                PersistCurrentSave("Organ state", saveManager);
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("PersistOrganChanges error: " + ex.Message);
            }
        }

        // Writes synchronized manager state to the active file and selected-save preferences
        private void PersistCurrentSave(string reason, SaveManager? existingSaveManager = null)
        {
            if (cachedGM == null || !cachedGM.isServer)
            {
                return;
            }

            SaveManager? saveManager = existingSaveManager ?? UnityEngine.Object.FindFirstObjectByType<SaveManager>();
            if (saveManager == null)
            {
                ModMenuLoader.Log(reason + " save skipped: SaveManager not found");
                return;
            }

            // The game reads PlayerPrefs before loading the file, so both copies stay aligned
            saveManager.SaveGame();
            FieldInfo? saveDataField = typeof(SaveManager).GetField("currentSaveData", BindingFlags.Instance | BindingFlags.NonPublic);
            object? saveData = saveDataField?.GetValue(saveManager);
            if (saveData == null)
            {
                ModMenuLoader.Log(reason + " save skipped: active save data unavailable");
                return;
            }

            PlayerPrefs.SetString("SelectedSaveData", JsonUtility.ToJson(saveData));
            if (!string.IsNullOrEmpty(saveManager.CurrentSaveName))
            {
                PlayerPrefs.SetString("SelectedSaveName", saveManager.CurrentSaveName);
            }
            PlayerPrefs.Save();
            ModMenuLoader.Log(reason + " saved to active save");
        }

    }
}
