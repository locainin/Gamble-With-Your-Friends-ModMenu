using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Binds remain a separate utility destination at the bottom of navigation
        private void DrawBindsTab()
        {
            DrawSection("Input Binds");
            GUILayout.Label("  Select Set, then press the replacement key", smallLabelStyle);

            // One helper keeps persistence and reset behavior identical for every bind
            DrawKeybindSetting("Menu Toggle", ref waitingForMenuKeybind, ref menuToggleKey, KeyCode.Insert, "CasinoMenu_MenuToggleKey");
            DrawKeybindSetting("Trigger Win", ref waitingForTriggerWinKeybind, ref triggerWinKey, KeyCode.None, "CasinoMenu_TriggerWinKey");
            DrawKeybindSetting("No Clip Toggle", ref waitingForFlyKeybind, ref flyToggleKey, KeyCode.None, "CasinoMenu_FlyToggleKey");
            DrawKeybindSetting("No Clip Up", ref waitingForFlyUpKeybind, ref flyUpKey, KeyCode.Space, "CasinoMenu_FlyUpKey");
            DrawKeybindSetting("No Clip Down", ref waitingForFlyDownKeybind, ref flyDownKey, KeyCode.LeftControl, "CasinoMenu_FlyDownKey");
            DrawKeybindSetting("Add Money", ref waitingForAddMoneyKeybind, ref addMoneyKey, KeyCode.None, "CasinoMenu_AddMoneyKey");
            DrawKeybindSetting("Remove Money", ref waitingForRemoveMoneyKeybind, ref removeMoneyKey, KeyCode.None, "CasinoMenu_RemoveMoneyKey");
            DrawKeybindSetting("Add Tickets", ref waitingForAddTicketKeybind, ref addTicketKey, KeyCode.None, "CasinoMenu_AddTicketKey");
        }

        // Renders one compact key row and persists reset or clear actions
        private void DrawKeybindSetting(string title, ref bool waiting, ref KeyCode key, KeyCode resetKey, string preferenceName)
        {
            GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Height(38f));
            GUILayout.Label(title, bodyLabelStyle, GUILayout.Width(150f));

            if (waiting)
            {
                // OnGUI consumes the next key event before normal command binds run
                GUILayout.Label("Press any key", bodyLabelStyle);
            }
            else
            {
                string keyName = key == KeyCode.None ? "Unbound" : key.ToString();
                GUILayout.Label(keyName, bodyLabelStyle, GUILayout.Width(140f));
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Set", GUILayout.Width(70f)))
                {
                    waiting = true;
                }

                if (key != resetKey)
                {
                    string resetLabel = resetKey == KeyCode.None ? "Clear" : "Reset";
                    if (GUILayout.Button(resetLabel, GUILayout.Width(70f)))
                    {
                        // Immediate persistence keeps the visible value and restart value aligned
                        key = resetKey;
                        PlayerPrefs.SetInt(preferenceName, (int)key);
                        PlayerPrefs.Save();
                    }
                }
            }

            GUILayout.EndHorizontal();
        }
    }
}
