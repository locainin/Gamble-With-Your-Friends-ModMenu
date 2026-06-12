using System;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModMenu
{
    [BepInPlugin("com.casinomenu.gamblewithfriends", "Casino Menu", VERSION)]
    public class ModMenuPlugin : BaseUnityPlugin
    {
        internal static ModMenuPlugin? Instance { get; private set; }

        internal ManualLogSource PluginLogger { get; private set; } = null!;

        public const string VERSION = "1.3.3";

        private bool initialized;

        // Loads preferences, installs patches, and subscribes to scene changes
        private void Awake()
        {
            Instance = this;
            PluginLogger = base.Logger;
            PluginLogger.LogInfo($"Casino Menu v{VERSION} BepInEx plugin loaded!");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // Creates the persistent menu object after the first game scene becomes available
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (initialized)
            {
                return;
            }
            try
            {
                // A named lookup prevents duplicate menus if BepInEx reloads the plugin object
                if (GameObject.Find("__CasinoMenu__") != null)
                {
                    initialized = true;
                    return;
                }

                GameObject obj = new GameObject("__CasinoMenu__");
                UnityEngine.Object.DontDestroyOnLoad(obj);
                obj.hideFlags = HideFlags.HideAndDontSave;
                obj.AddComponent<ModMenuBehaviour>();
                initialized = true;
                PluginLogger.LogInfo("ModMenuBehaviour attached!");
            }
            catch (Exception arg)
            {
                PluginLogger.LogError($"ERROR: {arg}");
            }
        }

        // Removes static references and event handlers when the plugin object is destroyed
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }
    }
}
