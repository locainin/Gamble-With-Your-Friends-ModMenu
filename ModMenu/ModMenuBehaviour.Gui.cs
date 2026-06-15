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
        // Window drawing and shared menu controls live here

        // Checks the configured version endpoint without blocking the game loop
        private IEnumerator CheckForUpdates(bool isStartup = false)
        {
            updateStatus = "Checking...";
            using (UnityWebRequest req = UnityWebRequest.Get(new Uri(updateUrl)))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    latestFetchedVersion = req.downloadHandler.text.Trim();
                    VersionCheckResult versionResult = VersionPolicy.Compare(ModMenuPlugin.VERSION, latestFetchedVersion);
                    if (versionResult == VersionCheckResult.Invalid)
                    {
                        updateStatus = "Check failed (invalid version data)";
                        showUpdateReminder = false;
                    }
                    else if (versionResult == VersionCheckResult.UpdateAvailable)
                    {
                        updateStatus = "Update available: v" + latestFetchedVersion.TrimStart('v', 'V');
                        if (!isStartup || !disableUpdateReminder)
                        {
                            showUpdateReminder = true;
                        }
                    }
                    else
                    {
                        updateStatus = "Up to date! (v" + ModMenuPlugin.VERSION + ")";
                        showUpdateReminder = false;
                    }
                }
                else
                {
                    updateStatus = "Check failed (network error)";
                }
            }
        }

        // Handles menu key input and renders visible IMGUI windows
        private void OnGUI()
        {
            EnsureUiSkin();
            Event current = Event.current;
            if (current != null && current.type == EventType.KeyDown)
            {
                if (waitingForMenuKeybind && current.keyCode != KeyCode.None)
                {
                    menuToggleKey = current.keyCode;
                    PlayerPrefs.SetInt("CasinoMenu_MenuToggleKey", (int)menuToggleKey);
                    PlayerPrefs.Save();
                    waitingForMenuKeybind = false;
                    current.Use();
                }
                else if (waitingForTriggerWinKeybind && current.keyCode != KeyCode.None)
                {
                    triggerWinKey = current.keyCode;
                    PlayerPrefs.SetInt("CasinoMenu_TriggerWinKey", (int)triggerWinKey);
                    PlayerPrefs.Save();
                    waitingForTriggerWinKeybind = false;
                    current.Use();
                }
                else if (waitingForFlyKeybind && current.keyCode != KeyCode.None)
                {
                    flyToggleKey = current.keyCode;
                    PlayerPrefs.SetInt("CasinoMenu_FlyToggleKey", (int)flyToggleKey);
                    PlayerPrefs.Save();
                    waitingForFlyKeybind = false;
                    current.Use();
                }
                else if (waitingForFlyUpKeybind && current.keyCode != KeyCode.None)
                {
                    flyUpKey = current.keyCode;
                    PlayerPrefs.SetInt("CasinoMenu_FlyUpKey", (int)flyUpKey);
                    PlayerPrefs.Save();
                    waitingForFlyUpKeybind = false;
                    current.Use();
                }
                else if (waitingForFlyDownKeybind && current.keyCode != KeyCode.None)
                {
                    flyDownKey = current.keyCode;
                    PlayerPrefs.SetInt("CasinoMenu_FlyDownKey", (int)flyDownKey);
                    PlayerPrefs.Save();
                    waitingForFlyDownKeybind = false;
                    current.Use();
                }
                else if (waitingForAddMoneyKeybind && current.keyCode != KeyCode.None)
                {
                    addMoneyKey = current.keyCode;
                    PlayerPrefs.SetInt("CasinoMenu_AddMoneyKey", (int)addMoneyKey);
                    PlayerPrefs.Save();
                    waitingForAddMoneyKeybind = false;
                    current.Use();
                }
                else if (waitingForRemoveMoneyKeybind && current.keyCode != KeyCode.None)
                {
                    removeMoneyKey = current.keyCode;
                    PlayerPrefs.SetInt("CasinoMenu_RemoveMoneyKey", (int)removeMoneyKey);
                    PlayerPrefs.Save();
                    waitingForRemoveMoneyKeybind = false;
                    current.Use();
                }
                else if (waitingForAddTicketKeybind && current.keyCode != KeyCode.None)
                {
                    addTicketKey = current.keyCode;
                    PlayerPrefs.SetInt("CasinoMenu_AddTicketKey", (int)addTicketKey);
                    PlayerPrefs.Save();
                    waitingForAddTicketKeybind = false;
                    current.Use();
                }
                else if (current.keyCode == menuToggleKey)
                {
                    showMenu = !showMenu;
                    IsMenuOpen = showMenu;
                    current.Use();
                }
                else if (!showMenu && flyToggleKey != KeyCode.None && current.keyCode == flyToggleKey)
                {
                    flyHackEnabled = !flyHackEnabled;
                    ModMenuLoader.Log("Fly " + (flyHackEnabled ? "Enabled" : "Disabled"));
                    current.Use();
                }
                else if (!showMenu && triggerWinKey != KeyCode.None && current.keyCode == triggerWinKey)
                {
                    TriggerWin();
                    current.Use();
                }
                else if (!showMenu && addMoneyKey != KeyCode.None && current.keyCode == addMoneyKey)
                {
                    long result = 0L;
                    if (long.TryParse(moneyInputStr, out result) && result > 0)
                    {
                        AddMoney(result);
                    }
                    current.Use();
                }
                else if (!showMenu && removeMoneyKey != KeyCode.None && current.keyCode == removeMoneyKey)
                {
                    long result2 = 0L;
                    if (long.TryParse(moneyInputStr, out result2) && result2 > 0)
                    {
                        RemoveMoney(result2);
                    }
                    current.Use();
                }
                else if (!showMenu && addTicketKey != KeyCode.None && current.keyCode == addTicketKey)
                {
                    long amount = (long)Mathf.Pow(10f, ticketSliderLog);
                    AddTickets(amount);
                    current.Use();
                }
            }
            if (showMenu || showUpdateReminder)
            {
                GUI.skin.window.padding.top = 20;
                if (showMenu)
                {
                    windowRect = GUI.Window(0, windowRect, DrawMenu, "");
                }
                if (showUpdateReminder)
                {
                    updateRect = GUI.Window(999, updateRect, DrawUpdateWindow, "Update Available!");
                }
            }

            if (fpsOverlayEnabled)
            {
                DrawFpsOverlay();
            }
        }

        // Draws FPS independently from the menu so closing the menu leaves the overlay visible
        private void DrawFpsOverlay()
        {
            GUIStyle overlayStyle = new GUIStyle(statusPillStyle)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 13
            };
            overlayStyle.normal.textColor = titleTextColor;
            GUI.Label(new Rect(Screen.width - 130f, 10f, 112f, 28f), $"{displayedFps:F0} FPS", overlayStyle);
        }

        // Renders the optional update notice window
        private void DrawUpdateWindow(int windowID)
        {
            GUILayout.Space(10f);
            GUIStyle style = new GUIStyle(bodyLabelStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("A new version of Casino Menu is available!\n\nCurrent: " + ModMenuPlugin.VERSION + "\nLatest: " + latestFetchedVersion, style);
            GUILayout.Space(15f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dismiss", GUILayout.Height(30f)))
            {
                showUpdateReminder = false;
            }
            GUILayout.EndHorizontal();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        // Renders the shared header, sidebar, and active tab content
        private void DrawMenu(int windowID)
        {
            bool flag = cachedGM != null && cachedGM.isServer;
            DrawOutline(new Rect(0f, 0f, windowRect.width, windowRect.height));
            Rect headerRect = new Rect(1f, 1f, windowRect.width - 2f, 52f);
            GUI.DrawTexture(headerRect, headerTexture);
            GUI.DrawTexture(new Rect(1f, 52f, windowRect.width - 2f, 2f), accentTexture);
            float contentStart = sidebarWidth + 26f;
            GUI.DrawTexture(new Rect(contentStart, 66f, windowRect.width - contentStart - 16f, windowRect.height - 82f), contentTexture);
            GUI.DrawTexture(new Rect(contentStart - 4f, 64f, 1f, windowRect.height - 78f), lineTexture);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal(GUILayout.Height(42f));
            GUILayout.BeginVertical(GUILayout.Width(220f));
            GUILayout.Label("Casino Menu", titleStyle);
            GUILayout.Label("v" + ModMenuPlugin.VERSION, smallLabelStyle);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUILayout.Width(150f));
            GUILayout.Label($"Press {menuToggleKey} to toggle", subtitleStyle);
            GUILayout.EndVertical();
            GUIStyle gUIStyle = new GUIStyle(statusPillStyle);
            gUIStyle.normal.textColor = flag ? hostStatusColor : clientStatusColor;
            GUILayout.Label(flag ? "HOST" : "CLIENT", gUIStyle, GUILayout.Width(70f), GUILayout.Height(24f));
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(sidebarWidth));
            GUILayout.Label("WORKSPACES", smallLabelStyle);
            // Primary workspaces stay grouped while Binds remains anchored at the bottom
            for (int i = 0; i < tabNames.Length - 1; i++)
            {
                DrawSidebarTab(tabNames[i], i);
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label("UTILITY", smallLabelStyle);
            DrawSidebarTab(tabNames[tabNames.Length - 1], tabNames.Length - 1);
            GUILayout.EndVertical();
            GUILayout.Space(10f);
            GUILayout.BeginVertical(GUILayout.Width(windowRect.width - sidebarWidth - 46f));
            DrawWorkspaceHeading(tabNames[currentTab]);
            // Complex inspectors own their scroll regions and should not gain a third scrollbar
            bool ownsFullWorkspace = currentTab == 2 || currentTab == 3 || currentTab == 4;
            if (!ownsFullWorkspace)
            {
                contentScrollPos = GUILayout.BeginScrollView(contentScrollPos, false, true);
            }
            if (currentTab == 0)
            {
                DrawCurrenciesTab(flag);
            }
            else if (currentTab == 1)
            {
                DrawSessionTab(flag);
            }
            else if (currentTab == 2)
            {
                DrawItemsTab(flag);
            }
            else if (currentTab == 3)
            {
                DrawPlayersTab(flag);
            }
            else if (currentTab == 4)
            {
                DrawWorldTab(flag);
            }
            else if (currentTab == 5)
            {
                DrawSystemTab();
            }
            else if (currentTab == 6)
            {
                DrawBindsTab();
            }
            if (!ownsFullWorkspace)
            {
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 50f));
        }

        // Adds orientation without introducing another framed panel
        private void DrawWorkspaceHeading(string title)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(30f));
            GUILayout.Label(title.ToUpperInvariant(), titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(cachedGM != null && cachedGM.isServer ? "SERVER AUTHORITY" : "LOCAL AUTHORITY", smallLabelStyle);
            GUILayout.EndHorizontal();
            GUILayout.Space(2f);
        }

        // Renders one navigation button and switches tabs on click
        private void DrawSidebarTab(string title, int tabIndex)
        {
            GUIStyle gUIStyle = currentTab == tabIndex ? activeTabStyle : tabStyle;
            string marker = currentTab == tabIndex ? "  " : "  ";
            if (GUILayout.Button(marker + title, gUIStyle, GUILayout.Height(themeButtonHeight + 7f), GUILayout.Width(sidebarWidth - 4f)))
            {
                currentTab = tabIndex;
                contentScrollPos = Vector2.zero;
            }

            if (currentTab == tabIndex && Event.current.type == EventType.Repaint)
            {
                // A narrow accent rail makes active navigation visible at a glance
                Rect activeRect = GUILayoutUtility.GetLastRect();
                GUI.DrawTexture(new Rect(activeRect.x, activeRect.y + 3f, 3f, activeRect.height - 6f), accentTexture);
            }
        }

        // Renders a consistent section heading
        private void DrawSection(string title)
        {
            GUILayout.Box(title.ToUpperInvariant(), sectionStyle, GUILayout.ExpandWidth(expand: true), GUILayout.Height(themeSectionHeight));
        }

        // Renders a high-contrast host requirement message
        private void DrawHostWarning(string text)
        {
            GUILayout.Label("  " + text, hostWarningStyle);
        }

        // Renders all available theme choices
        private void DrawThemeSection()
        {
            DrawSection("Presentation");
            GUILayout.Label("  Presets change color, density, control size, and workspace proportions", smallLabelStyle);
            for (int i = 0; i < themeNames.Length; i++)
            {
                DrawThemeButton(i);
            }
        }

        // Renders and persists one theme choice
        private void DrawThemeButton(int themeIndex)
        {
            bool isSelected = selectedThemeIndex == themeIndex;
            GetThemePreviewColors(themeIndex, out Color surface, out Color accent);

            GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Height(48f));
            DrawThemeSwatch(surface);
            DrawThemeSwatch(accent);
            GUILayout.Space(6f);

            GUILayout.BeginVertical();
            GUILayout.Label(themeNames[themeIndex], bodyLabelStyle);
            GUILayout.Label(themeDescriptions[themeIndex], smallLabelStyle);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && !isSelected;
            if (GUILayout.Button(isSelected ? "Active" : "Apply", isSelected ? activeTabStyle : GUI.skin.button, GUILayout.Width(70f), GUILayout.Height(30f)))
            {
                selectedThemeIndex = themeIndex;
                PlayerPrefs.SetInt("CasinoMenu_ThemeIndex", selectedThemeIndex);
                PlayerPrefs.Save();
                ReleaseUiSkin();
            }
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
        }

        // Draws a compact color sample without allocating a texture
        private static void DrawThemeSwatch(Color color)
        {
            Rect swatchRect = GUILayoutUtility.GetRect(18f, 30f, GUILayout.Width(18f), GUILayout.Height(30f));
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(swatchRect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        // Returns representative surface and accent colors for a presentation preset
        private static void GetThemePreviewColors(int themeIndex, out Color surface, out Color accent)
        {
            switch (themeIndex)
            {
                case 1:
                    surface = new Color(0.045f, 0.052f, 0.07f);
                    accent = new Color(0.18f, 0.62f, 1f);
                    break;
                case 2:
                    surface = new Color(0.04f, 0.058f, 0.048f);
                    accent = new Color(0.22f, 0.82f, 0.39f);
                    break;
                case 3:
                    surface = new Color(0.055f, 0.055f, 0.058f);
                    accent = new Color(0.76f, 0.76f, 0.8f);
                    break;
                case 4:
                    surface = new Color(0.94f, 0.96f, 0.99f);
                    accent = new Color(0.04f, 0.32f, 0.78f);
                    break;
                default:
                    surface = new Color(0.055f, 0.052f, 0.06f);
                    accent = new Color(1f, 0.67f, 0.18f);
                    break;
            }
        }

    }
}
