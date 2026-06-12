using System;
using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // System tab owns presentation and update status

        // Renders presentation and update settings without external download actions
        private void DrawSystemTab()
        {
            // Theme controls stay first because they affect the entire menu immediately
            DrawThemeSection();
            GUILayout.Space(6f);

            // Update checks remain manual so gameplay never waits on a network request
            DrawSection("Update Checker");
            if (GUILayout.Button("Check for Updates"))
            {
                StartCoroutine(CheckForUpdates());
            }

            bool requestedReminderState = GUILayout.Toggle(disableUpdateReminder, " Disable Startup Update Reminder");
            if (requestedReminderState != disableUpdateReminder)
            {
                disableUpdateReminder = requestedReminderState;
                PlayerPrefs.SetInt("CasinoMenu_DisableUpdateReminder", disableUpdateReminder ? 1 : 0);
                PlayerPrefs.Save();
            }

            if (!string.IsNullOrEmpty(updateStatus))
            {
                // Status color makes success, availability, and neutral messages easy to scan
                GUIStyle statusStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11
                };
                statusStyle.normal.textColor = updateStatus.Contains("Up to date", StringComparison.Ordinal)
                    ? Color.green
                    : updateStatus.Contains("available", StringComparison.Ordinal) ? Color.yellow : Color.gray;
                GUILayout.Label("  " + updateStatus, statusStyle);
            }
        }

    }
}
