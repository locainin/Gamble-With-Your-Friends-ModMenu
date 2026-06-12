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
        // IMGUI skin and theme helpers keep visuals centralized

        // Maps the selected theme index to all UI colors
        private void ApplyThemePalette()
        {
            ApplyThemeLayout();
            switch (selectedThemeIndex)
            {
                case 1:
                    panelColor = new Color(0.045f, 0.052f, 0.07f, 0.94f);
                    headerColor = new Color(0.07f, 0.082f, 0.11f, 0.96f);
                    contentColor = new Color(0.025f, 0.031f, 0.046f, 0.68f);
                    sectionColor = new Color(0.06f, 0.075f, 0.105f, 0.9f);
                    controlColor = new Color(0.065f, 0.073f, 0.095f, 0.96f);
                    controlHoverColor = new Color(0.08f, 0.12f, 0.18f, 0.98f);
                    borderColor = new Color(0.16f, 0.5f, 0.95f, 0.92f);
                    subtleBorderColor = new Color(0.12f, 0.2f, 0.32f, 0.94f);
                    accentColor = new Color(0.18f, 0.62f, 1f, 1f);
                    activeColor = new Color(0.11f, 0.32f, 0.58f, 0.96f);
                    activeBorderColor = new Color(0.26f, 0.72f, 1f, 1f);
                    activeTabColor = new Color(0.1f, 0.34f, 0.65f, 0.98f);
                    activeTabBorderColor = new Color(0.34f, 0.76f, 1f, 1f);
                    titleTextColor = Color.white;
                    bodyTextColor = new Color(0.86f, 0.9f, 0.96f);
                    mutedTextColor = new Color(0.58f, 0.68f, 0.8f);
                    buttonTextColor = new Color(0.84f, 0.9f, 0.98f);
                    buttonHoverTextColor = Color.white;
                    sectionTextColor = new Color(0.34f, 0.76f, 1f);
                    hostStatusColor = new Color(0.42f, 0.92f, 1f);
                    clientStatusColor = new Color(0.64f, 0.78f, 1f);
                    warningTextColor = new Color(1f, 0.45f, 0.36f);
                    break;
                case 2:
                    panelColor = new Color(0.04f, 0.058f, 0.048f, 0.94f);
                    headerColor = new Color(0.055f, 0.088f, 0.066f, 0.96f);
                    contentColor = new Color(0.025f, 0.038f, 0.032f, 0.68f);
                    sectionColor = new Color(0.055f, 0.105f, 0.07f, 0.9f);
                    controlColor = new Color(0.06f, 0.082f, 0.068f, 0.96f);
                    controlHoverColor = new Color(0.08f, 0.14f, 0.095f, 0.98f);
                    borderColor = new Color(0.16f, 0.66f, 0.31f, 0.92f);
                    subtleBorderColor = new Color(0.12f, 0.28f, 0.17f, 0.94f);
                    accentColor = new Color(0.22f, 0.82f, 0.39f, 1f);
                    activeColor = new Color(0.1f, 0.42f, 0.2f, 0.96f);
                    activeBorderColor = new Color(0.34f, 0.92f, 0.48f, 1f);
                    activeTabColor = new Color(0.12f, 0.52f, 0.24f, 0.98f);
                    activeTabBorderColor = new Color(0.4f, 0.95f, 0.56f, 1f);
                    titleTextColor = Color.white;
                    bodyTextColor = new Color(0.86f, 0.94f, 0.87f);
                    mutedTextColor = new Color(0.62f, 0.76f, 0.64f);
                    buttonTextColor = new Color(0.84f, 0.94f, 0.86f);
                    buttonHoverTextColor = Color.white;
                    sectionTextColor = new Color(0.36f, 0.9f, 0.48f);
                    hostStatusColor = new Color(0.42f, 1f, 0.54f);
                    clientStatusColor = new Color(0.74f, 0.95f, 0.68f);
                    warningTextColor = new Color(1f, 0.45f, 0.36f);
                    break;
                case 3:
                    panelColor = new Color(0.055f, 0.055f, 0.058f, 0.94f);
                    headerColor = new Color(0.11f, 0.11f, 0.115f, 0.96f);
                    contentColor = new Color(0.035f, 0.035f, 0.038f, 0.68f);
                    sectionColor = new Color(0.13f, 0.13f, 0.135f, 0.9f);
                    controlColor = new Color(0.09f, 0.09f, 0.095f, 0.96f);
                    controlHoverColor = new Color(0.16f, 0.16f, 0.17f, 0.98f);
                    borderColor = new Color(0.58f, 0.58f, 0.62f, 0.92f);
                    subtleBorderColor = new Color(0.28f, 0.28f, 0.3f, 0.94f);
                    accentColor = new Color(0.76f, 0.76f, 0.8f, 1f);
                    activeColor = new Color(0.32f, 0.32f, 0.36f, 0.96f);
                    activeBorderColor = new Color(0.82f, 0.82f, 0.88f, 1f);
                    activeTabColor = new Color(0.28f, 0.28f, 0.32f, 0.98f);
                    activeTabBorderColor = new Color(0.78f, 0.78f, 0.84f, 1f);
                    titleTextColor = Color.white;
                    bodyTextColor = new Color(0.9f, 0.9f, 0.92f);
                    mutedTextColor = new Color(0.7f, 0.7f, 0.72f);
                    buttonTextColor = new Color(0.9f, 0.9f, 0.92f);
                    buttonHoverTextColor = Color.white;
                    sectionTextColor = new Color(0.82f, 0.82f, 0.86f);
                    hostStatusColor = new Color(0.85f, 0.92f, 0.86f);
                    clientStatusColor = new Color(0.9f, 0.82f, 0.66f);
                    warningTextColor = new Color(1f, 0.45f, 0.36f);
                    break;
                case 4:
                    panelColor = new Color(0.94f, 0.96f, 0.99f, 0.96f);
                    headerColor = new Color(0.98f, 0.99f, 1f, 0.98f);
                    contentColor = new Color(0.88f, 0.92f, 0.98f, 0.78f);
                    sectionColor = new Color(0.86f, 0.91f, 0.98f, 0.95f);
                    controlColor = new Color(0.96f, 0.98f, 1f, 0.98f);
                    controlHoverColor = new Color(0.88f, 0.94f, 1f, 0.98f);
                    borderColor = new Color(0.12f, 0.36f, 0.72f, 0.95f);
                    subtleBorderColor = new Color(0.58f, 0.7f, 0.86f, 0.95f);
                    accentColor = new Color(0.04f, 0.32f, 0.78f, 1f);
                    activeColor = new Color(0.16f, 0.42f, 0.82f, 0.96f);
                    activeBorderColor = new Color(0.04f, 0.28f, 0.68f, 1f);
                    activeTabColor = new Color(0.13f, 0.38f, 0.78f, 0.98f);
                    activeTabBorderColor = new Color(0.04f, 0.24f, 0.62f, 1f);
                    titleTextColor = new Color(0.06f, 0.08f, 0.12f);
                    bodyTextColor = new Color(0.12f, 0.17f, 0.24f);
                    mutedTextColor = new Color(0.38f, 0.46f, 0.58f);
                    buttonTextColor = new Color(0.1f, 0.18f, 0.28f);
                    buttonHoverTextColor = new Color(0.02f, 0.1f, 0.22f);
                    sectionTextColor = new Color(0.04f, 0.28f, 0.72f);
                    hostStatusColor = new Color(0.04f, 0.45f, 0.24f);
                    clientStatusColor = new Color(0.04f, 0.28f, 0.72f);
                    warningTextColor = new Color(0.75f, 0.14f, 0.1f);
                    break;
                default:
                    panelColor = new Color(0.055f, 0.052f, 0.06f, 0.93f);
                    headerColor = new Color(0.12f, 0.108f, 0.105f, 0.95f);
                    contentColor = new Color(0.04f, 0.039f, 0.046f, 0.55f);
                    sectionColor = new Color(0.14f, 0.124f, 0.115f, 0.86f);
                    controlColor = new Color(0.095f, 0.092f, 0.105f, 0.95f);
                    controlHoverColor = new Color(0.16f, 0.142f, 0.12f, 0.98f);
                    borderColor = new Color(0.52f, 0.43f, 0.28f, 0.88f);
                    subtleBorderColor = new Color(0.27f, 0.24f, 0.19f, 0.92f);
                    accentColor = new Color(1f, 0.67f, 0.18f, 1f);
                    activeColor = new Color(0.18f, 0.58f, 0.28f, 0.95f);
                    activeBorderColor = new Color(0.42f, 0.88f, 0.48f, 0.95f);
                    activeTabColor = new Color(0.17f, 0.48f, 0.25f, 0.98f);
                    activeTabBorderColor = new Color(0.45f, 0.9f, 0.5f, 1f);
                    titleTextColor = Color.white;
                    bodyTextColor = new Color(0.88f, 0.86f, 0.8f);
                    mutedTextColor = new Color(0.72f, 0.7f, 0.65f);
                    buttonTextColor = new Color(0.92f, 0.9f, 0.84f);
                    buttonHoverTextColor = Color.white;
                    sectionTextColor = new Color(1f, 0.68f, 0.2f);
                    hostStatusColor = new Color(0.65f, 1f, 0.6f);
                    clientStatusColor = new Color(1f, 0.75f, 0.36f);
                    warningTextColor = new Color(1f, 0.38f, 0.32f);
                    break;
            }
        }

        // Applies layout density and window proportions for the selected presentation preset
        private void ApplyThemeLayout()
        {
            float width;
            float height;

            switch (selectedThemeIndex)
            {
                case 1:
                    width = 820f;
                    height = 570f;
                    sidebarWidth = 136f;
                    themeButtonHeight = 27f;
                    themeSectionHeight = 24f;
                    break;
                case 2:
                    width = 750f;
                    height = 520f;
                    sidebarWidth = 120f;
                    themeButtonHeight = 24f;
                    themeSectionHeight = 22f;
                    break;
                case 3:
                    width = 780f;
                    height = 540f;
                    sidebarWidth = 126f;
                    themeButtonHeight = 25f;
                    themeSectionHeight = 23f;
                    break;
                case 4:
                    width = 820f;
                    height = 580f;
                    sidebarWidth = 140f;
                    themeButtonHeight = 29f;
                    themeSectionHeight = 27f;
                    break;
                default:
                    width = 780f;
                    height = 560f;
                    sidebarWidth = 128f;
                    themeButtonHeight = 27f;
                    themeSectionHeight = 25f;
                    break;
            }

            // Preserve the dragged position while applying the selected workspace size
            windowRect.width = width;
            windowRect.height = height;
        }

        // Destroys generated textures before rebuilding a theme
        private void ReleaseUiSkin()
        {
            Texture2D[] array = new Texture2D[12] { panelTexture, headerTexture, sectionTexture, controlTexture, controlHoverTexture, activeTexture, accentTexture, lineTexture, contentTexture, activeTabTexture, sliderTrackTexture, sliderThumbTexture };
            foreach (Texture2D texture2D in array)
            {
                if (texture2D != null)
                {
                    UnityEngine.Object.Destroy(texture2D);
                }
            }
            panelTexture = null!;
            headerTexture = null!;
            sectionTexture = null!;
            controlTexture = null!;
            controlHoverTexture = null!;
            activeTexture = null!;
            accentTexture = null!;
            lineTexture = null!;
            contentTexture = null!;
            activeTabTexture = null!;
            sliderTrackTexture = null!;
            sliderThumbTexture = null!;
            uiSkinReady = false;
        }

        // Creates shared IMGUI styles and textures once per selected theme
        private void EnsureUiSkin()
        {
            if (uiSkinReady)
            {
                return;
            }
            ApplyThemePalette();
            panelTexture = MakeUiTexture(panelColor);
            headerTexture = MakeUiTexture(headerColor);
            contentTexture = MakeBorderTexture(contentColor, subtleBorderColor);
            sectionTexture = MakeBorderTexture(sectionColor, subtleBorderColor);
            controlTexture = MakeBorderTexture(controlColor, subtleBorderColor);
            controlHoverTexture = MakeBorderTexture(controlHoverColor, borderColor);
            activeTexture = MakeBorderTexture(activeColor, activeBorderColor);
            activeTabTexture = MakeBorderTexture(activeTabColor, activeTabBorderColor);
            sliderTrackTexture = MakeBorderTexture(new Color(controlColor.r * 0.65f, controlColor.g * 0.65f, controlColor.b * 0.65f, 0.95f), subtleBorderColor);
            sliderThumbTexture = MakeBorderTexture(accentColor, activeBorderColor);
            accentTexture = MakeUiTexture(accentColor);
            lineTexture = MakeUiTexture(borderColor);
            GUI.skin.window.normal.background = panelTexture;
            GUI.skin.window.onNormal.background = panelTexture;
            GUI.skin.window.padding = new RectOffset(14, 14, 12, 14);
            GUI.skin.window.border = new RectOffset(1, 1, 1, 1);
            GUI.skin.box.normal.background = contentTexture;
            GUI.skin.box.normal.textColor = bodyTextColor;
            GUI.skin.box.padding = new RectOffset(8, 8, 7, 8);
            GUI.skin.box.margin = new RectOffset(0, 0, 4, 4);
            GUI.skin.button.normal.background = controlTexture;
            GUI.skin.button.hover.background = controlHoverTexture;
            GUI.skin.button.active.background = activeTexture;
            GUI.skin.button.focused.background = controlHoverTexture;
            GUI.skin.button.normal.textColor = buttonTextColor;
            GUI.skin.button.hover.textColor = buttonHoverTextColor;
            GUI.skin.button.active.textColor = buttonHoverTextColor;
            GUI.skin.button.fontStyle = FontStyle.Bold;
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            GUI.skin.button.padding = new RectOffset(10, 10, 5, 6);
            GUI.skin.button.margin = new RectOffset(0, 0, 3, 3);
            GUI.skin.button.fixedHeight = themeButtonHeight;
            GUI.skin.textField.normal.background = controlTexture;
            GUI.skin.textField.focused.background = controlHoverTexture;
            GUI.skin.textField.normal.textColor = titleTextColor;
            GUI.skin.textField.focused.textColor = titleTextColor;
            GUI.skin.textField.padding = new RectOffset(8, 8, 4, 4);
            GUI.skin.textField.margin = new RectOffset(0, 0, 3, 3);
            GUI.skin.textField.fixedHeight = 25f;
            sliderTrackStyle = new GUIStyle(GUI.skin.horizontalSlider)
            {
                normal =
            {
                background = sliderTrackTexture
            },
                fixedHeight = 8f,
                margin = new RectOffset(0, 0, 8, 9),
                border = new RectOffset(1, 1, 1, 1)
            };
            sliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                normal =
            {
                background = sliderThumbTexture
            },
                hover =
            {
                background = sliderThumbTexture
            },
                active =
            {
                background = sliderThumbTexture
            },
                fixedWidth = 12f,
                fixedHeight = 18f,
                border = new RectOffset(1, 1, 1, 1)
            };
            GUI.skin.horizontalSlider = sliderTrackStyle;
            GUI.skin.horizontalSliderThumb = sliderThumbStyle;
            GUI.skin.toggle.normal.textColor = bodyTextColor;
            GUI.skin.toggle.hover.textColor = buttonHoverTextColor;
            GUI.skin.toggle.padding = new RectOffset(18, 4, 2, 2);
            GUI.skin.label.normal.textColor = bodyTextColor;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 17,
                alignment = TextAnchor.MiddleLeft
            };
            titleStyle.normal.textColor = titleTextColor;
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleRight
            };
            subtitleStyle.normal.textColor = mutedTextColor;
            statusPillStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 3, 4)
            };
            sectionStyle = new GUIStyle(GUI.skin.box)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                fontSize = selectedThemeIndex == 2 ? 11 : 12,
                padding = new RectOffset(10, 8, 4, 5),
                margin = new RectOffset(0, 0, 8, 5)
            };
            sectionStyle.normal.background = sectionTexture;
            sectionStyle.hover.background = sectionTexture;
            sectionStyle.normal.textColor = sectionTextColor;
            hostWarningStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                padding = new RectOffset(8, 8, 4, 4)
            };
            hostWarningStyle.normal.textColor = warningTextColor;
            tabStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                fontSize = selectedThemeIndex == 2 ? 11 : 12,
                padding = new RectOffset(12, 8, 6, 7),
                margin = new RectOffset(0, 0, 3, 3)
            };
            tabStyle.normal.background = controlTexture;
            tabStyle.hover.background = controlHoverTexture;
            tabStyle.normal.textColor = bodyTextColor;
            tabStyle.hover.textColor = buttonHoverTextColor;
            activeTabStyle = new GUIStyle(tabStyle);
            activeTabStyle.normal.background = activeTabTexture;
            activeTabStyle.normal.textColor = Color.white;
            activeTabStyle.hover.background = activeTabTexture;
            bodyLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                padding = new RectOffset(4, 4, 2, 2)
            };
            bodyLabelStyle.normal.textColor = bodyTextColor;
            smallLabelStyle = new GUIStyle(bodyLabelStyle)
            {
                fontSize = 10
            };
            smallLabelStyle.normal.textColor = mutedTextColor;
            uiSkinReady = true;
        }

        // Creates a one-color runtime texture for IMGUI backgrounds
        private static Texture2D MakeUiTexture(Color color)
        {
            Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
            texture2D.hideFlags = HideFlags.HideAndDontSave;
            texture2D.SetPixel(0, 0, color);
            texture2D.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return texture2D;
        }

        // Creates a bordered runtime texture for controls and panels
        private static Texture2D MakeBorderTexture(Color fill, Color border)
        {
            Texture2D texture2D = new Texture2D(8, 8, TextureFormat.RGBA32, mipChain: false);
            texture2D.hideFlags = HideFlags.HideAndDontSave;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == 7 || y == 7;
                    texture2D.SetPixel(x, y, isBorder ? border : fill);
                }
            }
            texture2D.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return texture2D;
        }

        // Draws a one-pixel outline around the menu window
        private void DrawOutline(Rect rect)
        {
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), lineTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), lineTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), lineTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), lineTexture);
        }

    }
}
