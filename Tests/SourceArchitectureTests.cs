using Xunit;

namespace ModMenu.Tests;

public sealed class SourceArchitectureTests
{
    private static readonly string ProjectRoot = FindProjectRoot();

    [Fact]
    public void PlayerAndItemFeaturesRemainSplitByResponsibility()
    {
        string modRoot = Path.Combine(ProjectRoot, "ModMenu");

        Assert.False(File.Exists(Path.Combine(modRoot, "Players", "ModMenuBehaviour.Players.cs")));
        Assert.False(File.Exists(Path.Combine(modRoot, "ModMenuBehaviour.Items.cs")));

        Assert.True(File.Exists(Path.Combine(modRoot, "Players", "ModMenuBehaviour.PlayerList.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Players", "ModMenuBehaviour.PlayerConnection.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Players", "ModMenuBehaviour.PlayerTeleport.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Players", "ModMenuBehaviour.PlayerOrgans.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Players", "ModMenuBehaviour.PlayerActions.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Players", "ModMenuBehaviour.PlayerNpcFollow.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Players", "ModMenuBehaviour.PlayerMovement.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Players", "ModMenuBehaviour.PlayerVisibility.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Players", "ModMenuBehaviour.PlayerGrabProtection.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Players", "PlayerCarryProtectionPatches.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Items", "ModMenuBehaviour.ItemDiscovery.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Items", "ModMenuBehaviour.ItemSpawning.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Items", "ModMenuBehaviour.ItemsTab.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "World", "ModMenuBehaviour.WorldTab.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "World", "ModMenuBehaviour.NpcWorkspace.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "World", "ModMenuBehaviour.WorldTime.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "World", "ModMenuBehaviour.NpcFollow.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "World", "ModMenuBehaviour.DayProgression.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Tabs", "ModMenuBehaviour.BindsTab.cs")));
        Assert.False(File.Exists(Path.Combine(modRoot, "Tabs", "ModMenuBehaviour.TimeTab.cs")));
        Assert.False(File.Exists(Path.Combine(modRoot, "Players", "ModMenuBehaviour.PlayerVoice.cs")));
    }

    [Fact]
    public void PlayerAndTabPartialsStayReviewable()
    {
        string modRoot = Path.Combine(ProjectRoot, "ModMenu");
        IEnumerable<string> sourceFiles = Directory.EnumerateFiles(Path.Combine(modRoot, "Players"), "*.cs")
            .Concat(Directory.EnumerateFiles(Path.Combine(modRoot, "Tabs"), "*.cs"))
            .Concat(Directory.EnumerateFiles(Path.Combine(modRoot, "Items"), "*.cs"))
            .Concat(Directory.EnumerateFiles(Path.Combine(modRoot, "World"), "*.cs"));

        foreach (string sourceFile in sourceFiles)
        {
            int lineCount = File.ReadLines(sourceFile).Count();
            Assert.True(lineCount <= 300, $"{Path.GetFileName(sourceFile)} contains {lineCount} lines");
        }
    }

    [Fact]
    public void PlayerInspectorKeepsActionsContextual()
    {
        string playersRoot = Path.Combine(ProjectRoot, "ModMenu", "Players");
        string playerList = File.ReadAllText(Path.Combine(playersRoot, "ModMenuBehaviour.PlayerList.cs"));
        string teleport = File.ReadAllText(Path.Combine(playersRoot, "ModMenuBehaviour.PlayerTeleport.cs"));

        Assert.Contains("\"Overview\", \"Teleport\", \"Organs\", \"Effects\"", playerList, StringComparison.Ordinal);
        Assert.Contains("Bring To Me", teleport, StringComparison.Ordinal);
        Assert.Contains("Go To Player", teleport, StringComparison.Ordinal);
        Assert.Contains("Swap Positions", teleport, StringComparison.Ordinal);
    }

    [Fact]
    public void TemporaryPlayerStatesUseSingleToggleActions()
    {
        string actions = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerActions.cs"));

        Assert.Contains("isFrozen ? \"Unfreeze\" : \"Freeze\"", actions, StringComparison.Ordinal);
        Assert.Contains("isHeadLocked ? \"Unlock Head\" : \"Lock Head\"", actions, StringComparison.Ordinal);
        Assert.DoesNotContain("GUILayout.Button(\"Unfreeze\")", actions, StringComparison.Ordinal);
        Assert.DoesNotContain("GUILayout.Button(\"Unlock Head\")", actions, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateChecksDoNotBypassCertificateValidation()
    {
        string source = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(Path.Combine(ProjectRoot, "ModMenu"), "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("ServerCertificateValidationCallback", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RemoteCertificateValidationCallback", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Application.OpenURL", source, StringComparison.Ordinal);
    }

    [Fact]
    public void NavigationKeepsBindsLastAndMovesTimeIntoWorld()
    {
        string behaviour = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.cs"));
        string worldTime = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "World", "ModMenuBehaviour.WorldTime.cs"));

        Assert.Contains(
            "\"Currencies\", \"Session\", \"Items\", \"Players\", \"World\", \"System\", \"Binds\"",
            behaviour,
            StringComparison.Ordinal);
        Assert.Contains("DrawWorldTimeControls", worldTime, StringComparison.Ordinal);
    }

    [Fact]
    public void NpcWorkspaceDefaultsToBulkAndSupportsClosestOrIndividualScope()
    {
        string npcWorkspace = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "World", "ModMenuBehaviour.NpcWorkspace.cs"));

        Assert.Contains("\"All NPCs\", \"Closest\", \"Individual\"", npcWorkspace, StringComparison.Ordinal);
        Assert.Contains("npcControlScope == 1", npcWorkspace, StringComparison.Ordinal);
        Assert.Contains("SortNpcsByHostDistance", npcWorkspace, StringComparison.Ordinal);
        Assert.Contains("MoveNpcsAroundPoint", npcWorkspace, StringComparison.Ordinal);
    }

    [Fact]
    public void NpcFollowPersistsUntilStoppedOrTargetIsLost()
    {
        string npcFollow = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "World", "ModMenuBehaviour.NpcFollow.cs"));
        string playerFollow = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerNpcFollow.cs"));
        string behaviour = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.cs"));

        Assert.Contains("NpcFollowInterval", npcFollow, StringComparison.Ordinal);
        Assert.Contains("UpdateNpcFollow", npcFollow, StringComparison.Ordinal);
        Assert.Contains("StopNpcFollow", npcFollow, StringComparison.Ordinal);
        Assert.Contains("NpcFollowTargetGracePeriod", npcFollow, StringComparison.Ordinal);
        Assert.Contains("npcFollowMissingTargetTime", npcFollow, StringComparison.Ordinal);
        Assert.Contains("targetProfile.hasSynced", npcFollow, StringComparison.Ordinal);
        Assert.Contains("npcFollowScope = 1", npcFollow, StringComparison.Ordinal);
        Assert.Contains("npc.State = NPC.NPCState.Free", npcFollow, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "if (targets.Length == 0)\n            {\n                StopNpcFollow();",
            npcFollow,
            StringComparison.Ordinal);
        Assert.Contains(
            "private void LateUpdate()\n        {\n            // NPC behavior runs during Update",
            behaviour,
            StringComparison.Ordinal);
        Assert.Contains("npcFollowScope = 1", behaviour, StringComparison.Ordinal);
        Assert.Contains("Stop Following This Player", playerFollow, StringComparison.Ordinal);
        Assert.Contains("StartNpcFollowForPlayer(profile)", playerFollow, StringComparison.Ordinal);
        Assert.Contains("hasStableIdentity", playerFollow, StringComparison.Ordinal);
    }

    [Fact]
    public void DayTimerUsesTheServerAdjustmentApiWithRemainingTimeSemantics()
    {
        string host = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.Host.cs"));

        Assert.Contains("cachedGM.ServerAdjustTimer(0f - seconds)", host, StringComparison.Ordinal);
        Assert.DoesNotContain("cachedGM.Network_timer -= seconds", host, StringComparison.Ordinal);
    }

    [Fact]
    public void NestedControlsRestoreTheirInheritedGuiState()
    {
        string gui = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.Gui.cs"));
        string movement = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerMovement.cs"));
        string teleport = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerTeleport.cs"));

        Assert.Contains("GUI.enabled = previousEnabled && !isSelected", gui, StringComparison.Ordinal);
        Assert.Contains("GUI.enabled = previousEnabled && canEditMovement", movement, StringComparison.Ordinal);
        Assert.Contains("GUI.enabled = previousEnabled && selectedPlayer != null", teleport, StringComparison.Ordinal);
        Assert.DoesNotContain("GUI.enabled = true;", movement, StringComparison.Ordinal);
        Assert.DoesNotContain("GUI.enabled = true;", teleport, StringComparison.Ordinal);
    }

    [Fact]
    public void PlayerTeleportUsesSceneStableProfileInstances()
    {
        string behaviour = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.cs"));
        string teleport = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerTeleport.cs"));

        Assert.Contains("private int teleportTargetInstanceId", behaviour, StringComparison.Ordinal);
        Assert.Contains("FindPlayerProfileByInstanceId(profiles, teleportTargetInstanceId)", teleport, StringComparison.Ordinal);
        Assert.Contains("playerProfile.GetInstanceID()", teleport, StringComparison.Ordinal);
    }

    [Fact]
    public void FpsOverlayIsOptionalAndIndependentFromMenuWindow()
    {
        string gui = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.Gui.cs"));
        string world = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "World", "ModMenuBehaviour.WorldTab.cs"));

        Assert.Contains("if (fpsOverlayEnabled)", gui, StringComparison.Ordinal);
        Assert.Contains("DrawFpsOverlay()", gui, StringComparison.Ordinal);
        Assert.Contains("Screen.width - 130f", gui, StringComparison.Ordinal);
        Assert.Contains("CasinoMenu_FpsOverlay", world, StringComparison.Ordinal);
    }

    [Fact]
    public void MenuInputCaptureShipsInsideTheMainPlugin()
    {
        string behaviour = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.cs"));
        string inputCapture = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Input", "GameplayInputCapture.cs"));

        Assert.Contains("internal static bool IsMenuOpen", behaviour, StringComparison.Ordinal);
        Assert.Contains("GameplayInputRelease.Send()", behaviour, StringComparison.Ordinal);
        Assert.Contains("Cursor.lockState = CursorLockMode.None", behaviour, StringComparison.Ordinal);
        Assert.Contains("InputReaderGameplayPatch", inputCapture, StringComparison.Ordinal);
        Assert.Contains("return !ModMenuBehaviour.IsMenuOpen", inputCapture, StringComparison.Ordinal);
        Assert.Contains("\"OnMove\"", inputCapture, StringComparison.Ordinal);
        Assert.Contains("\"OnInteract\"", inputCapture, StringComparison.Ordinal);
    }

    [Fact]
    public void DayChangerUpdatesCoupledProgressionAndPersists()
    {
        string progression = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "World", "ModMenuBehaviour.DayProgression.cs"));

        Assert.Contains("NetworkdaysPassed", progression, StringComparison.Ordinal);
        Assert.Contains("NetworkdaysLeft", progression, StringComparison.Ordinal);
        Assert.Contains("NetworksuccessfulQuota", progression, StringComparison.Ordinal);
        Assert.Contains("NetworkcurrentQuota", progression, StringComparison.Ordinal);
        Assert.Contains("NetworkcurrentFloor", progression, StringComparison.Ordinal);
        Assert.Contains("NetworkrequiredQuotaToNextFloor", progression, StringComparison.Ordinal);
        Assert.Contains("NetworkcurrentTicketReward", progression, StringComparison.Ordinal);
        Assert.Contains("PersistCurrentSave", progression, StringComparison.Ordinal);
        Assert.Contains("ServerSetScene(GameState.Game)", progression, StringComparison.Ordinal);
    }

    [Fact]
    public void VoiceEffectsAreNotExposed()
    {
        string source = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(Path.Combine(ProjectRoot, "ModMenu"), "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("DrawVoiceTrollControls", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CmdStartTimedVoiceFX", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PlayerVisibilityRequiresLocalOwnership()
    {
        string playerList = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerList.cs"));
        string visibility = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerVisibility.cs"));

        Assert.Contains("selectedPlayerInstanceId", playerList, StringComparison.Ordinal);
        Assert.Contains("profile.isLocalPlayer", visibility, StringComparison.Ordinal);
        Assert.Contains("CmdSetVisibility", visibility, StringComparison.Ordinal);
        Assert.Contains("RestoreVisibilityAfterSelectionChange", visibility, StringComparison.Ordinal);
        Assert.DoesNotContain("ToggleVisibility()", visibility, StringComparison.Ordinal);
    }

    [Fact]
    public void PlayerConnectionOverviewDoesNotPresentSteamPeersAsIpAddresses()
    {
        string playerList = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerList.cs"));
        string playerConnection = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerConnection.cs"));

        Assert.Contains("GetPlayerAuthorityLabel(profile, isHost)", playerList, StringComparison.Ordinal);
        Assert.Contains("TryNormalizeIpAddress", playerConnection, StringComparison.Ordinal);
        Assert.Contains("\"IP Address\"", playerConnection, StringComparison.Ordinal);
        Assert.Contains("\"Steam relay (IP hidden)\"", playerConnection, StringComparison.Ordinal);
        Assert.Contains("\"Local process (no remote IP)\"", playerConnection, StringComparison.Ordinal);
        Assert.Contains("return isHost ? \"Remote client\" : \"Remote player\"", playerConnection, StringComparison.Ordinal);
        Assert.DoesNotContain("return \"localhost\"", playerConnection, StringComparison.Ordinal);
    }

    [Fact]
    public void SteamIdentityLookupsRejectUnsynchronizedZeroIds()
    {
        string playerList = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerList.cs"));
        string npcFollow = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "World", "ModMenuBehaviour.NpcFollow.cs"));

        Assert.Contains("if (steamId == 0uL)", playerList, StringComparison.Ordinal);
        Assert.Contains("profile.hasSynced && profile.steamId == steamId", playerList, StringComparison.Ordinal);
        Assert.Contains("if (steamId == 0uL)", npcFollow, StringComparison.Ordinal);
        Assert.Contains("profile.hasSynced && profile.steamId == steamId", npcFollow, StringComparison.Ordinal);
    }

    [Fact]
    public void SceneLocalPlayerEffectsUseInstanceIdsInsteadOfUnsyncedSteamIds()
    {
        string behaviour = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.cs"));
        string actions = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerActions.cs"));

        Assert.Contains("HashSet<int> frozenPlayerIds", behaviour, StringComparison.Ordinal);
        Assert.Contains("HashSet<int> headLockedPlayerIds", behaviour, StringComparison.Ordinal);
        Assert.Contains("int playerInstanceId = playerProfile.GetInstanceID()", actions, StringComparison.Ordinal);
        Assert.DoesNotContain("frozenPlayerIds.Contains(playerProfile.steamId)", actions, StringComparison.Ordinal);
        Assert.DoesNotContain("headLockedPlayerIds.Contains(playerProfile.steamId)", actions, StringComparison.Ordinal);
    }

    [Fact]
    public void PersistentProtectionWaitsForSynchronizedSteamIdentity()
    {
        string organs = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerOrgans.cs"));
        string grab = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerGrabProtection.cs"));

        Assert.Contains("playerProfile.hasSynced && playerProfile.steamId != 0uL", organs, StringComparison.Ordinal);
        Assert.Contains("playerProfile.hasSynced && playerProfile.steamId != 0uL", grab, StringComparison.Ordinal);
        Assert.Contains("Waiting for Steam identity synchronization", organs, StringComparison.Ordinal);
        Assert.Contains("Waiting for Steam identity synchronization", grab, StringComparison.Ordinal);
    }

    [Fact]
    public void ManualMoneyChangesUseSignedAccountingAndUpdateShowcaseHistory()
    {
        string currency = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.Currency.cs"));
        string cache = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.Cache.cs"));

        Assert.Contains("CallChangeBalance(amount, countTowardPlayerProfit: true)", currency, StringComparison.Ordinal);
        Assert.Contains("CallChangeBalance(-removableAmount, countTowardPlayerProfit: true)", currency, StringComparison.Ordinal);
        Assert.Contains("CallAddBalance(num, countTowardPlayerProfit: false)", currency, StringComparison.Ordinal);
        Assert.Contains("moneyManager.TryChangeBalance(signedAmount", currency, StringComparison.Ordinal);
        Assert.Contains("RecordShowcaseBalanceChange(amount)", currency, StringComparison.Ordinal);
        Assert.Contains("RecordShowcaseBalanceChange(-removableAmount)", currency, StringComparison.Ordinal);
        Assert.Contains("GameResultsManager? resultsManager", currency, StringComparison.Ordinal);
        Assert.Contains("resultsManager.RegisterResult(", currency, StringComparison.Ordinal);
        Assert.Contains("long bet = signedAmount < 0 ? -signedAmount : 0L", currency, StringComparison.Ordinal);
        Assert.Contains("long payout = signedAmount > 0 ? signedAmount : 0L", currency, StringComparison.Ordinal);
        Assert.DoesNotContain("PayoutRecord record = new PayoutRecord", currency, StringComparison.Ordinal);
        Assert.DoesNotContain("GetMethod(\"RemoveBalance\"", currency, StringComparison.Ordinal);
        Assert.DoesNotContain("CmdTryChangeBalance", currency, StringComparison.Ordinal);
        Assert.DoesNotContain("GetField(\"balance\"", currency, StringComparison.Ordinal);
        Assert.Contains("HasEconomyAuthority()", currency, StringComparison.Ordinal);
        Assert.Contains("moneyManager.TryChangeTicketBalance(amount)", currency, StringComparison.Ordinal);
        Assert.DoesNotContain("CmdTryChangeTicketBalance", currency, StringComparison.Ordinal);
        Assert.Contains("names[j] == \"GameResult\"", cache, StringComparison.Ordinal);
        Assert.Contains("names[j] == \"Misc\"", cache, StringComparison.Ordinal);
    }

    [Fact]
    public void GameplayHotkeysDoNotFireWhileTheMenuAcceptsInput()
    {
        string gui = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.Gui.cs"));

        Assert.Contains("!showMenu && flyToggleKey", gui, StringComparison.Ordinal);
        Assert.Contains("!showMenu && triggerWinKey", gui, StringComparison.Ordinal);
        Assert.Contains("!showMenu && addMoneyKey", gui, StringComparison.Ordinal);
        Assert.Contains("!showMenu && removeMoneyKey", gui, StringComparison.Ordinal);
        Assert.Contains("!showMenu && addTicketKey", gui, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomNoClipVerticalKeysReplaceTheirDefaults()
    {
        string movement = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.Movement.cs"));

        Assert.Contains("if (IsKeyDown(flyUpKey))", movement, StringComparison.Ordinal);
        Assert.Contains("if (IsKeyDown(flyDownKey))", movement, StringComparison.Ordinal);
        Assert.DoesNotContain("|| IsKeyDown(KeyCode.Space)", movement, StringComparison.Ordinal);
        Assert.DoesNotContain("|| IsKeyDown(KeyCode.LeftControl)", movement, StringComparison.Ordinal);
    }

    [Fact]
    public void PersistentProtectionReconcilesPlayersThatJoinAfterSceneRecovery()
    {
        string behaviour = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.cs"));
        string state = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "PlayerProtectionState.cs"));

        Assert.Contains("UpdatePlayerProtectionRecovery();", behaviour, StringComparison.Ordinal);
        Assert.Contains("ReapplyPlayerGrabProtections(isHost)", behaviour, StringComparison.Ordinal);
        Assert.Contains("ReapplyProtectedPlayers(organManager)", behaviour, StringComparison.Ordinal);
        Assert.Contains("HasAnyProtection", state, StringComparison.Ordinal);
    }

    [Fact]
    public void ItemSpawnFailureDestroysTheUnnetworkedClone()
    {
        string spawning = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Items", "ModMenuBehaviour.ItemSpawning.cs"));

        Assert.Contains("if (!SpawnOnNetwork(gameObject2))", spawning, StringComparison.Ordinal);
        Assert.Contains("UnityEngine.Object.Destroy(gameObject2)", spawning, StringComparison.Ordinal);
        Assert.Contains("Mirror.NetworkServer.Spawn(go, cachedLocalPC.gameObject)", spawning, StringComparison.Ordinal);
        Assert.Contains("go.GetComponent<Mirror.NetworkIdentity>() == null", spawning, StringComparison.Ordinal);
    }

    [Fact]
    public void TakeNoHitsBlocksServerKnockbackIndependentlyFromOrganProtection()
    {
        string controls = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerOrgans.cs"));
        string patches = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "OrganProtectionPatches.cs"));
        string state = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "PlayerProtectionState.cs"));

        Assert.Contains("Enable Take No Hits", controls, StringComparison.Ordinal);
        Assert.Contains("PlayerProtectionState.SetNoHit", controls, StringComparison.Ordinal);
        Assert.Contains(
            "[HarmonyPatch(typeof(PlayerController), nameof(PlayerController.ServerKnockback))]",
            patches,
            StringComparison.Ordinal);
        Assert.Contains("!PlayerProtectionState.IsNoHit(__instance)", patches, StringComparison.Ordinal);
        Assert.Contains("NoHitSteamIds.IntersectWith(connectedSteamIds)", state, StringComparison.Ordinal);
        Assert.Contains("NoHitSteamIds.Clear()", state, StringComparison.Ordinal);
    }

    [Fact]
    public void LocalMovementControlsLiveInPlayerInspectorForHostAndClient()
    {
        string playerList = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerList.cs"));
        string session = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Tabs", "ModMenuBehaviour.SessionTab.cs"));
        string movement = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerMovement.cs"));

        Assert.Contains("if (profile.isLocalPlayer)", playerList, StringComparison.Ordinal);
        Assert.Contains("DrawLocalMovementControls(profile, playerController)", playerList, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawLocalMovementControls(", session, StringComparison.Ordinal);
        Assert.Contains("Speed Modifier", movement, StringComparison.Ordinal);
        Assert.Contains("Jump Modifier", movement, StringComparison.Ordinal);
        Assert.Contains("No Clip", movement, StringComparison.Ordinal);
    }

    [Fact]
    public void NoGrabBlocksPickupBeforeServerAssignsHolder()
    {
        string controls = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "ModMenuBehaviour.PlayerGrabProtection.cs"));
        string patch = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "Players", "PlayerCarryProtectionPatches.cs"));
        string state = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "PlayerProtectionState.cs"));

        Assert.Contains("[HarmonyPatch(typeof(Item), \"ServerPickup\")]", patch, StringComparison.Ordinal);
        Assert.Contains("!PlayerProtectionState.IsNoGrab(playerCarry)", patch, StringComparison.Ordinal);
        Assert.Contains("holderInventory.ServerDropHoldingItem()", controls, StringComparison.Ordinal);
        Assert.Contains("playerCarry.LocalSetInteractable(!isProtected)", controls, StringComparison.Ordinal);
        Assert.Contains("NO GRAB ACTIVE", controls, StringComparison.Ordinal);
        Assert.Contains("ReapplyPlayerGrabProtections(cachedGM != null && cachedGM.isServer)", File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.cs")), StringComparison.Ordinal);
        Assert.Contains("NoGrabSteamIds.IntersectWith(connectedSteamIds)", state, StringComparison.Ordinal);
    }

    [Fact]
    public void SceneRecoveryDoesNotPruneProtectionDuringEmptyProfileWindow()
    {
        string behaviour = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.cs"));

        Assert.Contains("if (profiles.Length == 0)", behaviour, StringComparison.Ordinal);
        Assert.Contains("bool identitySnapshotComplete = true", behaviour, StringComparison.Ordinal);
        Assert.Contains("!profile.hasSynced || profile.steamId == 0uL", behaviour, StringComparison.Ordinal);
        Assert.Contains("if (identitySnapshotComplete)", behaviour, StringComparison.Ordinal);
        Assert.Contains("PlayerProtectionState.RetainConnectedPlayers(connectedSteamIds)", behaviour, StringComparison.Ordinal);
    }

    [Fact]
    public void SceneChangesResetManagerOwnedBaselines()
    {
        string behaviour = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.cs"));

        Assert.Contains("lastKnownBalance = -1L", behaviour, StringComparison.Ordinal);
        Assert.Contains("lastKnownTicketBalance = -1L", behaviour, StringComparison.Ordinal);
        Assert.Contains("lastMultiplierBalance = -1L", behaviour, StringComparison.Ordinal);
        Assert.Contains("pauseDayTimer = false", behaviour, StringComparison.Ordinal);
    }

    [Fact]
    public void SceneChangesRestoreSharedMovementSettingsBeforeDroppingBaselines()
    {
        string behaviour = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.cs"));
        string movement = File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.Movement.cs"));

        int restoreIndex = behaviour.IndexOf("RestoreMovementOverrides();", StringComparison.Ordinal);
        int cacheResetIndex = behaviour.IndexOf("cachedPlayerSettings = null;", StringComparison.Ordinal);

        Assert.True(restoreIndex >= 0);
        Assert.True(cacheResetIndex > restoreIndex);
        Assert.Contains("fJumpForce.SetValue(cachedPlayerSettings, originalJumpForce)", movement, StringComparison.Ordinal);
        Assert.Contains("hasMovementBaseline", movement, StringComparison.Ordinal);
        Assert.Contains("RestoreFlightPhysics", behaviour, StringComparison.Ordinal);
        Assert.Contains("wasKinematicBeforeFlying = rigidbody.isKinematic", movement, StringComparison.Ordinal);
        Assert.Contains("rigidbody.isKinematic = wasKinematicBeforeFlying", movement, StringComparison.Ordinal);
    }

    [Fact]
    public void GameSpeedOverrideReleasesAfterHostAuthorityIsLost()
    {
        string behaviour = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.cs"));

        Assert.Contains(
            "gameSpeedEnabled && cachedGM != null && !cachedGM.isServer",
            behaviour,
            StringComparison.Ordinal);
        Assert.Contains("gameSpeedEnabled = false", behaviour, StringComparison.Ordinal);
    }

    [Fact]
    public void VersionMetadataUsesCurrentRelease()
    {
        string plugin = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuPlugin.cs"));
        string assemblyInfo = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "Properties", "AssemblyInfo.cs"));
        string repositoryVersion = File.ReadAllText(Path.Combine(ProjectRoot, "VERSION")).Trim();

        Assert.Contains("VERSION = \"1.3.3\"", plugin, StringComparison.Ordinal);
        Assert.Contains("AssemblyVersion(\"1.3.3.0\")", assemblyInfo, StringComparison.Ordinal);
        Assert.Contains("AssemblyFileVersion(\"1.3.3.0\")", assemblyInfo, StringComparison.Ordinal);
        Assert.Equal("1.3.3", repositoryVersion);
        Assert.Contains("Gamble-With-Your-Friends-ModMenu/main/VERSION", plugin + File.ReadAllText(
            Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.cs")), StringComparison.Ordinal);
    }

    private static string FindProjectRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ModMenuCleanUI.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate ModMenuCleanUI.slnx");
    }
}
