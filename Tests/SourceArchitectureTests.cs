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
        Assert.True(File.Exists(Path.Combine(modRoot, "Players", "ModMenuBehaviour.PlayerTeleport.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Players", "ModMenuBehaviour.PlayerOrgans.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Players", "ModMenuBehaviour.PlayerActions.cs")));
        Assert.True(File.Exists(Path.Combine(modRoot, "Players", "ModMenuBehaviour.PlayerNpcFollow.cs")));
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

        Assert.Contains("NpcFollowInterval", npcFollow, StringComparison.Ordinal);
        Assert.Contains("UpdateNpcFollow", npcFollow, StringComparison.Ordinal);
        Assert.Contains("StopNpcFollow", npcFollow, StringComparison.Ordinal);
        Assert.Contains("NpcFollowTargetGracePeriod", npcFollow, StringComparison.Ordinal);
        Assert.Contains("npcFollowMissingTargetTime", npcFollow, StringComparison.Ordinal);
        Assert.Contains("Stop Following This Player", playerFollow, StringComparison.Ordinal);
        Assert.Contains("StartNpcFollowForPlayer(profile)", playerFollow, StringComparison.Ordinal);
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

        Assert.Contains("profile.isLocalPlayer", playerList, StringComparison.Ordinal);
        Assert.Contains("visibility!.ToggleVisibility()", playerList, StringComparison.Ordinal);
        Assert.Contains("Make Invisible", playerList, StringComparison.Ordinal);
    }

    [Fact]
    public void SceneRecoveryDoesNotPruneProtectionDuringEmptyProfileWindow()
    {
        string behaviour = File.ReadAllText(Path.Combine(ProjectRoot, "ModMenu", "ModMenuBehaviour.cs"));

        Assert.Contains("if (profiles.Length == 0)", behaviour, StringComparison.Ordinal);
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
