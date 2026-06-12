using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace ModMenu
{
    public partial class ModMenuBehaviour : MonoBehaviour
    {
        // Window state stays separate from gameplay state so closing the menu never disables features
        private bool showMenu = true;

        // Harmony input patches read this without searching the scene every input event
        internal static bool IsMenuOpen { get; private set; }

        // Tracks the open transition so held controls are released exactly once
        private bool wasMenuOpen;

        private Rect windowRect = new Rect(20f, 20f, 780f, 560f);

        private int currentTab;

        private readonly string[] tabNames = new string[7] { "Currencies", "Session", "Items", "Players", "World", "System", "Binds" };

        private readonly string[] themeNames = new string[5] { "Casino", "Midnight", "Terminal", "Graphite", "Daylight" };

        private readonly string[] themeDescriptions = new string[5]
        {
        "Warm contrast with roomy controls",
        "Cool blue precision with a wider workspace",
        "Compact green layout for dense sessions",
        "Neutral low-noise layout with thin chrome",
        "Bright high-contrast layout with generous spacing"
        };

        private int selectedThemeIndex;

        private bool speedHackEnabled;

        private bool jumpHackEnabled;

        private float speedMultiplier = 1f;

        private float jumpMultiplier = 1f;

        private float originalMaxSpeed = -1f;

        private float originalSprintMaxSpeed = -1f;

        private float originalAcceleration = -1f;

        private float originalJumpForce = -1f;

        private float originalGravity = -1f;

        private bool flyHackEnabled;

        private KeyCode flyToggleKey;

        private bool waitingForFlyKeybind;

        private float flySpeedMultiplier = 1f;

        private KeyCode flyUpKey = KeyCode.Space;

        private bool waitingForFlyUpKeybind;

        private KeyCode flyDownKey = KeyCode.LeftControl;

        private bool waitingForFlyDownKeybind;

        private bool wasFlying;

        private string moneyInputStr = "10000";

        private KeyCode addMoneyKey;

        private bool waitingForAddMoneyKeybind;

        private KeyCode removeMoneyKey;

        private bool waitingForRemoveMoneyKeybind;

        private KeyCode addTicketKey;

        private bool waitingForAddTicketKeybind;

        private float ticketSliderLog = 2f;

        private bool noMoneySpendEnabled;

        private long lastKnownBalance = -1L;

        private float revertCooldown;

        private bool noTicketSpendEnabled;

        private long lastKnownTicketBalance = -1L;

        private float ticketRevertCooldown;

        private bool moneyMultiplierEnabled;

        private float moneyMultiplier = 1f;

        private long lastMultiplierBalance = -1L;

        private float multiplierCooldown;

        private bool isInternalMoneyChange;

        // Game speed stores the value that existed before the override began
        private bool gameSpeedEnabled;

        private float gameSpeedMultiplier = 1f;

        private bool wasGameSpeedEnabled;

        private float timeScaleBeforeOverride = 1f;

        private float timeToAddSlider = 60f;

        // Day timer pause is host-owned and intentionally resets between scenes
        private bool pauseDayTimer;

        private float pausedTimerValue;

        private KeyCode triggerWinKey;

        private bool waitingForTriggerWinKeybind;

        private KeyCode menuToggleKey = KeyCode.Insert;

        private bool waitingForMenuKeybind;

        private string updateStatus = "";

        private string updateUrl = "https://raw.githubusercontent.com/locainin/Gamble-With-Your-Friends-ModMenu/main/VERSION";

        private bool showUpdateReminder;

        private string latestFetchedVersion = "";

        private Rect updateRect = new Rect((Screen.width - 320) / 2, (Screen.height - 180) / 2, 320f, 180f);

        private bool disableUpdateReminder;

        // Scene object caches are refreshed on a short unscaled interval
        private PlayerController? cachedLocalPC;

        private MoneyManager? cachedMM;

        private GameManager? cachedGM;

        private object? cachedPlayerSettings;

        private FieldInfo? fMaxSpeed;

        private FieldInfo? fSprintSpeed;

        private FieldInfo? fAccel;

        private FieldInfo? fJumpForce;

        private float cacheTimer;

        // Reflected item data is rebuilt whenever a scene replaces the game managers
        private object? cachedItemManager;

        private object? cachedSpawnableSettings;

        private IList? cachedSpawnables;

        private Type? itemManagerType;

        private Type? spawnableSettingsType;

        private Type? spawnableSOType;

        private Type? itemType;

        // GameResult changes appear in the per-player profit history
        private object? changeTypePlayerProfit;

        // Misc changes alter shared balance without inflating player profit
        private object? changeTypeMisc;

        private bool changeTypeResolved;

        private bool gmDumped;

        // One bounded recovery routine handles late network registration after scene loads
        private Coroutine? sceneRecoveryCoroutine;

        private Vector2 contentScrollPos = Vector2.zero;

        private string itemSearchFilter = "";

        private Vector2 itemListScrollPos = Vector2.zero;

        private Vector2 playerListScrollPos = Vector2.zero;

        private Vector2 playerInspectorScrollPos = Vector2.zero;

        private Vector2 npcListScrollPos = Vector2.zero;

        private Vector2 npcInspectorScrollPos = Vector2.zero;

        private int selectedNpcInstanceId;

        // World mode and NPC scope keep unrelated tools out of the same workspace
        private int worldWorkspaceMode;

        private int npcControlScope;

        // Persistent NPC follow stores stable IDs instead of scene object references
        private bool npcFollowEnabled;

        private int npcFollowScope;

        private int npcFollowInstanceId;

        private ulong npcFollowTargetSteamId;

        private float npcFollowUpdateTimer;

        private float npcFollowMissingTargetTime;

        // FPS uses a smoothed unscaled sample so pauses do not freeze the readout
        private float smoothedFrameTime = 1f / 60f;

        private float displayedFps = 60f;

        private float fpsDisplayTimer;

        private bool fpsOverlayEnabled;

        private string dayInput = "1";

        private ulong teleportTargetSteamId;

        // Temporary effect sets drive toggle labels and are cleared with their scene objects
        private readonly HashSet<ulong> frozenPlayerIds = new HashSet<ulong>();

        private readonly HashSet<ulong> headLockedPlayerIds = new HashSet<ulong>();

        private ulong selectedPlayerSteamId;

        private int playerInspectorMode;

        private float sidebarWidth = 128f;

        private float themeButtonHeight = 27f;

        private float themeSectionHeight = 25f;

        private Type? networkServerType;

        private MethodInfo? networkServerSpawnMethod;

        private Type? itemStampManagerType;

        // UI resources are created lazily because Unity skin data is unavailable during construction
        private bool uiSkinReady;

        private GUIStyle titleStyle = null!;

        private GUIStyle subtitleStyle = null!;

        private GUIStyle statusPillStyle = null!;

        private GUIStyle sectionStyle = null!;

        private GUIStyle hostWarningStyle = null!;

        private GUIStyle tabStyle = null!;

        private GUIStyle activeTabStyle = null!;

        private GUIStyle bodyLabelStyle = null!;

        private GUIStyle smallLabelStyle = null!;

        private GUIStyle sliderTrackStyle = null!;

        private GUIStyle sliderThumbStyle = null!;

        private Texture2D panelTexture = null!;

        private Texture2D headerTexture = null!;

        private Texture2D sectionTexture = null!;

        private Texture2D controlTexture = null!;

        private Texture2D controlHoverTexture = null!;

        private Texture2D activeTexture = null!;

        private Texture2D accentTexture = null!;

        private Texture2D lineTexture = null!;

        private Texture2D contentTexture = null!;

        private Texture2D activeTabTexture = null!;

        private Texture2D sliderTrackTexture = null!;

        private Texture2D sliderThumbTexture = null!;

        private Color panelColor = new Color(0.055f, 0.052f, 0.06f, 0.93f);

        private Color headerColor = new Color(0.12f, 0.108f, 0.105f, 0.95f);

        private Color contentColor = new Color(0.04f, 0.039f, 0.046f, 0.55f);

        private Color sectionColor = new Color(0.14f, 0.124f, 0.115f, 0.86f);

        private Color controlColor = new Color(0.095f, 0.092f, 0.105f, 0.95f);

        private Color controlHoverColor = new Color(0.16f, 0.142f, 0.12f, 0.98f);

        private Color borderColor = new Color(0.52f, 0.43f, 0.28f, 0.88f);

        private Color subtleBorderColor = new Color(0.27f, 0.24f, 0.19f, 0.92f);

        private Color accentColor = new Color(1f, 0.67f, 0.18f, 1f);

        private Color activeColor = new Color(0.18f, 0.58f, 0.28f, 0.95f);

        private Color activeBorderColor = new Color(0.42f, 0.88f, 0.48f, 0.95f);

        private Color activeTabColor = new Color(0.17f, 0.48f, 0.25f, 0.98f);

        private Color activeTabBorderColor = new Color(0.45f, 0.9f, 0.5f, 1f);

        private Color titleTextColor = Color.white;

        private Color bodyTextColor = new Color(0.88f, 0.86f, 0.8f);

        private Color mutedTextColor = new Color(0.72f, 0.7f, 0.65f);

        private Color buttonTextColor = new Color(0.92f, 0.9f, 0.84f);

        private Color buttonHoverTextColor = Color.white;

        private Color sectionTextColor = new Color(1f, 0.68f, 0.2f);

        private Color hostStatusColor = new Color(0.65f, 1f, 0.6f);

        private Color clientStatusColor = new Color(1f, 0.75f, 0.36f);

        private Color warningTextColor = new Color(1f, 0.38f, 0.32f);

        // Loads preferences, installs patches, and subscribes to scene changes
        private void Awake()
        {
            showMenu = true;
            IsMenuOpen = true;
            ModMenuLoader.Log("ModMenuBehaviour.Awake()!");
            PlayerProtectionState.EnsurePatched();
            disableUpdateReminder = PlayerPrefs.GetInt("CasinoMenu_DisableUpdateReminder", 0) == 1;
            fpsOverlayEnabled = PlayerPrefs.GetInt("CasinoMenu_FpsOverlay", 0) == 1;
            selectedThemeIndex = Mathf.Clamp(PlayerPrefs.GetInt("CasinoMenu_ThemeIndex", 0), 0, themeNames.Length - 1);
            if (PlayerPrefs.HasKey("CasinoMenu_MenuToggleKey"))
            {
                menuToggleKey = (KeyCode)PlayerPrefs.GetInt("CasinoMenu_MenuToggleKey");
            }
            if (PlayerPrefs.HasKey("CasinoMenu_TriggerWinKey"))
            {
                triggerWinKey = (KeyCode)PlayerPrefs.GetInt("CasinoMenu_TriggerWinKey");
            }
            if (PlayerPrefs.HasKey("CasinoMenu_FlyToggleKey"))
            {
                flyToggleKey = (KeyCode)PlayerPrefs.GetInt("CasinoMenu_FlyToggleKey");
            }
            if (PlayerPrefs.HasKey("CasinoMenu_FlyUpKey"))
            {
                flyUpKey = (KeyCode)PlayerPrefs.GetInt("CasinoMenu_FlyUpKey");
            }
            if (PlayerPrefs.HasKey("CasinoMenu_FlyDownKey"))
            {
                flyDownKey = (KeyCode)PlayerPrefs.GetInt("CasinoMenu_FlyDownKey");
            }
            if (PlayerPrefs.HasKey("CasinoMenu_AddMoneyKey"))
            {
                addMoneyKey = (KeyCode)PlayerPrefs.GetInt("CasinoMenu_AddMoneyKey");
            }
            if (PlayerPrefs.HasKey("CasinoMenu_RemoveMoneyKey"))
            {
                removeMoneyKey = (KeyCode)PlayerPrefs.GetInt("CasinoMenu_RemoveMoneyKey");
            }
            if (PlayerPrefs.HasKey("CasinoMenu_AddTicketKey"))
            {
                addTicketKey = (KeyCode)PlayerPrefs.GetInt("CasinoMenu_AddTicketKey");
            }
            StartCoroutine(CheckForUpdates(isStartup: true));
            SceneManager.sceneLoaded += OnGameSceneLoaded;
        }

        // Clears scene-bound references and schedules a bounded state reapply
        private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Every scene owns new network objects and reflected settings instances
            cachedLocalPC = null;
            cachedMM = null;
            cachedGM = null;
            cachedPlayerSettings = null;
            fMaxSpeed = null;
            fSprintSpeed = null;
            fAccel = null;
            fJumpForce = null;
            originalMaxSpeed = -1f;
            originalSprintMaxSpeed = -1f;
            originalAcceleration = -1f;
            originalJumpForce = -1f;
            originalGravity = -1f;
            wasFlying = false;
            cachedSpawnables = null;
            cachedSpawnableSettings = null;
            cachedItemManager = null;
            itemStampManagerType = null;
            networkServerType = null;
            networkServerSpawnMethod = null;
            gmDumped = false;
            cacheTimer = 0f;

            // Economy baselines from the previous manager must not modify a new lobby
            lastKnownBalance = -1L;
            lastKnownTicketBalance = -1L;
            lastMultiplierBalance = -1L;
            revertCooldown = 0f;
            ticketRevertCooldown = 0f;
            multiplierCooldown = 0f;
            isInternalMoneyChange = false;

            // Timer ownership and its captured value do not survive a floor transition
            pauseDayTimer = false;
            pausedTimerValue = 0f;

            // Temporary labels belong to player and NPC objects destroyed with the scene
            frozenPlayerIds.Clear();
            headLockedPlayerIds.Clear();
            selectedPlayerSteamId = 0uL;
            teleportTargetSteamId = 0uL;
            selectedNpcInstanceId = 0;
            if (npcFollowEnabled && npcFollowScope == 2)
            {
                // Individual NPC objects have no stable identity after a scene rebuild
                StopNpcFollow();
            }
            else if (npcFollowEnabled)
            {
                // Bulk scopes wait for the same Steam profile to register in the new scene
                npcFollowUpdateTimer = 2f;
                npcFollowMissingTargetTime = 0f;
            }

            // Stop an older recovery loop before the new scene starts registering players
            if (sceneRecoveryCoroutine != null)
            {
                StopCoroutine(sceneRecoveryCoroutine);
            }
            sceneRecoveryCoroutine = StartCoroutine(ReapplySceneState());
        }

        // Reapplies loaded organ data after late network profile registration
        private IEnumerator ReapplySceneState()
        {
            // Multiple short attempts cover host and late-join initialization order
            float[] delays = new float[3] { 0.4f, 1f, 2f };
            foreach (float delay in delays)
            {
                yield return new WaitForSecondsRealtime(delay);
                RefreshCache();

                // Pickup protection is independent from host-only organ persistence
                ReapplyPlayerGrabProtections(cachedGM != null && cachedGM.isServer);

                if (cachedGM == null || !cachedGM.isServer)
                {
                    continue;
                }

                OrganManager organManager = UnityEngine.Object.FindFirstObjectByType<OrganManager>();
                if (organManager != null)
                {
                    // The game manager owns authoritative Steam ID backed organ records
                    organManager.ServerApplyAllOrganSettings();
                    ReapplyProtectedPlayers(organManager);
                }
            }

            sceneRecoveryCoroutine = null;
        }

        // Repairs protected players after the game recreates their organ components
        private static void ReapplyProtectedPlayers(OrganManager organManager)
        {
            PlayerProfile[] profiles = UnityEngine.Object.FindObjectsByType<PlayerProfile>(FindObjectsSortMode.None);
            if (profiles.Length == 0)
            {
                // Empty snapshots are normal while Mirror rebuilds the lobby
                return;
            }

            List<ulong> connectedSteamIds = new List<ulong>(profiles.Length);
            foreach (PlayerProfile profile in profiles)
            {
                if (profile == null || profile.steamId == 0uL)
                {
                    continue;
                }

                connectedSteamIds.Add(profile.steamId);
                if (PlayerProtectionState.IsProtected(profile.steamId))
                {
                    organManager.SetOrganDataBySteamId(profile.steamId, true, true, true, true);
                }
            }

            // Lobby changes cannot leave protection attached to disconnected Steam IDs
            PlayerProtectionState.RetainConnectedPlayers(connectedSteamIds);
        }

        // Applies game-speed changes only on transitions so normal pauses remain intact
        private void UpdateGameSpeed()
        {
            if (gameSpeedEnabled && cachedGM != null && !cachedGM.isServer)
            {
                // Losing host authority must release a process-wide time override
                gameSpeedEnabled = false;
            }

            if (gameSpeedEnabled)
            {
                if (!wasGameSpeedEnabled)
                {
                    timeScaleBeforeOverride = Time.timeScale;
                    wasGameSpeedEnabled = true;
                }

                Time.timeScale = gameSpeedMultiplier;
                return;
            }

            if (wasGameSpeedEnabled)
            {
                Time.timeScale = timeScaleBeforeOverride;
                wasGameSpeedEnabled = false;
            }
        }

        // Updates a readable FPS value without allocating text every frame
        private void UpdateFpsCounter()
        {
            float frameTime = Mathf.Max(Time.unscaledDeltaTime, FrameRatePolicy.MinimumFrameTime);
            smoothedFrameTime = FrameRatePolicy.SmoothFrameTime(smoothedFrameTime, frameTime, 0.08f);
            fpsDisplayTimer -= frameTime;
            if (fpsDisplayTimer <= 0f)
            {
                displayedFps = FrameRatePolicy.ToDisplayFps(smoothedFrameTime);
                fpsDisplayTimer = 0.25f;
            }
        }

        // Runs periodic cache refreshes and active feature updates
        private void Update()
        {
            UpdateFpsCounter();
            cacheTimer -= Time.unscaledDeltaTime;
            if (cacheTimer <= 0f)
            {
                RefreshCache();
                cacheTimer = 3f;
            }
            ApplyMovementHacks();
            FlyUpdate();
            if (noMoneySpendEnabled)
            {
                NoMoneySpendUpdate();
            }
            if (noTicketSpendEnabled)
            {
                NoTicketSpendUpdate();
            }
            if (moneyMultiplierEnabled)
            {
                MoneyMultiplierUpdate();
            }
            UpdateGameSpeed();
            UpdateNpcFollow();
            if (pauseDayTimer && cachedGM != null && cachedGM.isServer)
            {
                // Avoid dirtying the SyncVar every frame when the value already matches
                if (!Mathf.Approximately(cachedGM.Network_timer, pausedTimerValue))
                {
                    cachedGM.Network_timer = pausedTimerValue;
                }
            }
        }

        // Keeps gameplay input and cursor ownership aligned with the visible menu
        private void LateUpdate()
        {
            IsMenuOpen = showMenu;
            if (!showMenu)
            {
                wasMenuOpen = false;
                return;
            }

            if (!wasMenuOpen)
            {
                // Releasing held controls prevents movement from continuing after the menu opens
                GameplayInputRelease.Send();
                wasMenuOpen = true;
            }

            // The game may relock the cursor later in the frame
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Restores process-wide state and removes scene handlers during teardown
        private void OnDestroy()
        {
            IsMenuOpen = false;
            SceneManager.sceneLoaded -= OnGameSceneLoaded;
            if (sceneRecoveryCoroutine != null)
            {
                StopCoroutine(sceneRecoveryCoroutine);
                sceneRecoveryCoroutine = null;
            }

            if (wasGameSpeedEnabled)
            {
                Time.timeScale = timeScaleBeforeOverride;
            }

            ReleaseUiSkin();
            PlayerProtectionState.Clear();
        }
    }
}
