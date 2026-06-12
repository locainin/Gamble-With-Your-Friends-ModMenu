using System;
using System.Reflection;
using UnityEngine;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Currency tab renders balance, ticket, and quota controls

        // Renders money, ticket, quota, and spending controls
        private void DrawCurrenciesTab(bool isHost)
        {
            // Economy mutations are authoritative even though clients can read replicated balances
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && isHost;

            DrawSection("Add / Remove Money");
            if (!isHost)
            {
                GUI.enabled = previousEnabled;
                DrawHostWarning("Host authority is required for economy controls");
                GUI.enabled = false;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("  Amount: $", GUILayout.Width(70f));
            moneyInputStr = GUILayout.TextField(moneyInputStr, GUILayout.Width(150f));
            GUILayout.EndHorizontal();
            // Invalid input stays at zero and cannot trigger either balance action
            long result = long.TryParse(moneyInputStr, out long parsedAmount) ? parsedAmount : 0L;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"  Add ${result:N0}") && result > 0)
            {
                AddMoney(result);
            }
            if (GUILayout.Button($"  Remove ${result:N0}") && result > 0)
            {
                RemoveMoney(result);
            }
            GUILayout.EndHorizontal();
            if (cachedMM != null)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11
                };
                GUILayout.Label($"  Balance: ${cachedMM.Networkbalance:N0}", style);
            }
            GUILayout.Space(6f);
            DrawSection("Add Tickets");
            // Logarithmic input keeps small and large ticket grants practical
            long num = (long)Mathf.Pow(10f, ticketSliderLog);
            GUILayout.Label($"  Amount: {num:N0}");
            ticketSliderLog = GUILayout.HorizontalSlider(ticketSliderLog, 1f, 5f);
            if (GUILayout.Button($"  Add {num:N0} Tickets"))
            {
                AddTickets(num);
            }
            if (cachedMM != null)
            {
                GUIStyle style2 = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11
                };
                PropertyInfo property = typeof(MoneyManager).GetProperty("NetworkticketBalance", BindingFlags.Instance | BindingFlags.Public);
                if (property != null)
                {
                    // Reflection keeps compatibility with the public network property name
                    long num2 = (long)property.GetValue(cachedMM);
                    GUILayout.Label($"  Tickets: {num2:N0}", style2);
                }
            }
            GUILayout.Space(6f);
            DrawSection("Meet Quota");
            if (cachedGM != null && cachedMM != null)
            {
                GUIStyle style3 = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12
                };
                long networkbalance = cachedMM.Networkbalance;
                // A negative remainder means the current quota is already satisfied
                long num3 = cachedGM.NetworkcurrentQuota - networkbalance;
                GUILayout.Label((num3 > 0) ? $"  Need: ${num3:N0} more" : "  Quota met!", style3);
            }
            if (GUILayout.Button("  Meet Quota"))
            {
                MeetQuota();
            }
            GUILayout.Space(6f);
            DrawSection("Money Gain Multiplier");
            bool flag = moneyMultiplierEnabled;
            moneyMultiplierEnabled = GUILayout.Toggle(moneyMultiplierEnabled, moneyMultiplierEnabled ? " ENABLED" : " Disabled");
            if (moneyMultiplierEnabled && !flag)
            {
                // Resetting the baseline prevents an old balance from being multiplied
                lastMultiplierBalance = -1L;
            }
            if (moneyMultiplierEnabled)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  {moneyMultiplier:F1}x", GUILayout.Width(50f));
                moneyMultiplier = GUILayout.HorizontalSlider(moneyMultiplier, 0.5f, 10f);
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(6f);
            DrawSection("No Money Spend");
            bool flag2 = noMoneySpendEnabled;
            noMoneySpendEnabled = GUILayout.Toggle(noMoneySpendEnabled, noMoneySpendEnabled ? " ACTIVE" : " Disabled");
            if (noMoneySpendEnabled && !flag2)
            {
                // The next update captures a fresh protected balance
                lastKnownBalance = -1L;
            }
            GUILayout.Space(6f);
            DrawSection("No Ticket Spend");
            bool flag3 = noTicketSpendEnabled;
            noTicketSpendEnabled = GUILayout.Toggle(noTicketSpendEnabled, noTicketSpendEnabled ? " ACTIVE" : " Disabled");
            if (noTicketSpendEnabled && !flag3)
            {
                // Ticket protection follows the same fresh-baseline rule
                lastKnownTicketBalance = -1L;
            }
            GUILayout.Space(6f);
            DrawSection("Trigger Win");
            if (GUILayout.Button("  Trigger Win Payout"))
            {
                TriggerWin();
            }
            GUIStyle style4 = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                wordWrap = true
            };
            GUILayout.Label("  Triggers win payout based on bet amount\n  set on nearby games.", style4);
            GUI.enabled = previousEnabled;
        }

    }
}
