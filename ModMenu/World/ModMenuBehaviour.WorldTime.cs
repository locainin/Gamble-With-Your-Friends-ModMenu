using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Time controls live inside World because they mutate shared scene state
        private void DrawWorldTimeControls(bool isHost)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && isHost;

            DrawSection("Game Speed");
            gameSpeedEnabled = GUILayout.Toggle(gameSpeedEnabled, gameSpeedEnabled ? " ENABLED" : " Disabled");
            if (gameSpeedEnabled)
            {
                // The value row remains stable while the slider changes
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  {gameSpeedMultiplier:F1}x", GUILayout.Width(54f));
                gameSpeedMultiplier = GUILayout.HorizontalSlider(gameSpeedMultiplier, 0.5f, 5f);
                GUILayout.EndHorizontal();
            }

            DrawSection("Day Timer");
            GUILayout.BeginHorizontal();
            GUILayout.Label("  Change", GUILayout.Width(58f));
            GUILayout.Label($"{timeToAddSlider:F0}s", GUILayout.Width(48f));
            timeToAddSlider = GUILayout.HorizontalSlider(timeToAddSlider, 5f, 600f);
            timeToAddSlider = Mathf.Max(5f, Mathf.Round(timeToAddSlider / 5f) * 5f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Time"))
            {
                ModifyDayTimer(timeToAddSlider);
            }
            if (GUILayout.Button("Subtract Time"))
            {
                ModifyDayTimer(0f - timeToAddSlider);
            }
            GUILayout.EndHorizontal();

            DrawSection("Current Day");
            int currentDisplayDay = cachedGM != null
                ? cachedGM.NetworkdaysPassed + 1
                : DayProgressionPolicy.MinimumDisplayDay;
            GUILayout.Label($"  Current: Day {currentDisplayDay}", smallLabelStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("  Day", GUILayout.Width(44f));
            dayInput = GUILayout.TextField(dayInput, GUILayout.Width(100f));
            bool validDay = int.TryParse(dayInput, out int requestedDay);
            bool previousDayEnabled = GUI.enabled;
            GUI.enabled = previousDayEnabled && validDay;
            if (GUILayout.Button("Set Day"))
            {
                SetGameDay(requestedDay);
            }
            GUI.enabled = previousDayEnabled;
            GUILayout.EndHorizontal();
            GUILayout.Label("  Rebuilds day, quota cycle, floor, reward, save state, and active casino floor", smallLabelStyle);

            DrawSection("Timer State");
            bool wasPaused = pauseDayTimer;
            if (GUILayout.Button(pauseDayTimer ? "Resume Day Timer" : "Pause Day Timer"))
            {
                pauseDayTimer = !pauseDayTimer;
            }
            if (pauseDayTimer && !wasPaused && cachedGM != null)
            {
                // Capture once so the held value does not drift while paused
                pausedTimerValue = cachedGM.Network_timer;
            }
            if (pauseDayTimer)
            {
                GUILayout.Label($"  Paused at {pausedTimerValue:F0} seconds", smallLabelStyle);
            }

            GUI.enabled = previousEnabled;
            if (!isHost)
            {
                DrawHostWarning("Host authority is required for world time controls");
            }
        }
    }
}
