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
        // Currency helpers wrap the game managers and keep anti-spend state stable

        // Refunds detected money spending while the protection toggle is active
        private void NoMoneySpendUpdate()
        {
            if (cachedMM == null)
            {
                return;
            }
            if (revertCooldown > 0f)
            {
                revertCooldown -= Time.unscaledDeltaTime;
                return;
            }
            try
            {
                long networkbalance = cachedMM.Networkbalance;
                // The first sample becomes the protected balance instead of creating a refund
                if (lastKnownBalance < 0)
                {
                    lastKnownBalance = networkbalance;
                }
                else if (networkbalance < lastKnownBalance)
                {
                    long num = lastKnownBalance - networkbalance;
                    // Internal changes are excluded from multiplier detection
                    isInternalMoneyChange = true;
                    // Refunds restore shared funds without recording fake player earnings
                    CallAddBalance(num, countTowardPlayerProfit: false);
                    isInternalMoneyChange = false;
                    revertCooldown = 0.3f;
                    lastKnownBalance = networkbalance;
                    ModMenuLoader.Log($"No Money Spend reverted: +${num:N0}");
                }
                else
                {
                    lastKnownBalance = networkbalance;
                }
            }
            catch
            {
            }
        }

        // Refunds detected ticket spending while the protection toggle is active
        private void NoTicketSpendUpdate()
        {
            if (cachedMM == null)
            {
                return;
            }
            if (ticketRevertCooldown > 0f)
            {
                ticketRevertCooldown -= Time.unscaledDeltaTime;
                return;
            }
            try
            {
                PropertyInfo property = typeof(MoneyManager).GetProperty("NetworkticketBalance", BindingFlags.Instance | BindingFlags.Public);
                if (!(property == null))
                {
                    long num = (long)property.GetValue(cachedMM);
                    // A fresh manager needs one observation before spending can be detected
                    if (lastKnownTicketBalance < 0)
                    {
                        lastKnownTicketBalance = num;
                    }
                    else if (num < lastKnownTicketBalance)
                    {
                        long num2 = lastKnownTicketBalance - num;
                        CallAddTickets(num2);
                        ticketRevertCooldown = 0.3f;
                        lastKnownTicketBalance = num;
                        ModMenuLoader.Log($"No Ticket Spend: refunded {num2:N0} tickets");
                    }
                    else
                    {
                        lastKnownTicketBalance = num;
                    }
                }
            }
            catch
            {
            }
        }

        // Adds a configurable bonus after detecting a positive balance change
        private void MoneyMultiplierUpdate()
        {
            if (cachedMM == null)
            {
                return;
            }
            if (multiplierCooldown > 0f)
            {
                multiplierCooldown -= Time.unscaledDeltaTime;
                return;
            }
            try
            {
                long networkbalance = cachedMM.Networkbalance;
                if (lastMultiplierBalance < 0)
                {
                    lastMultiplierBalance = networkbalance;
                    return;
                }
                if (networkbalance > lastMultiplierBalance && !isInternalMoneyChange)
                {
                    long num = (long)((float)(networkbalance - lastMultiplierBalance) * (moneyMultiplier - 1f));
                    if (num > 0)
                    {
                        // Add only the bonus because the game already applied the original gain
                        isInternalMoneyChange = true;
                        // Multiplier bonuses are real gains and belong in the player total
                        CallAddBalance(num, countTowardPlayerProfit: true);
                        isInternalMoneyChange = false;
                        multiplierCooldown = 0.5f;
                        lastMultiplierBalance = networkbalance + num;
                        ModMenuLoader.Log($"Multiplier bonus: +${num:N0} ({moneyMultiplier:F1}x)");
                        return;
                    }
                }
                lastMultiplierBalance = networkbalance;
            }
            catch
            {
            }
        }

        // Validates and routes a positive money addition
        private void AddMoney(long amount)
        {
            if (cachedMM == null)
            {
                ModMenuLoader.Log("Not in a game");
                return;
            }
            isInternalMoneyChange = true;
            bool num = CallAddBalance(amount, countTowardPlayerProfit: true);
            isInternalMoneyChange = false;
            if (num)
            {
                lastKnownBalance = -1L;
            }
            ModMenuLoader.Log(num ? $"Added ${amount:N0}!" : "Failed");
        }

        // Validates and routes a positive money removal
        private void RemoveMoney(long amount)
        {
            if (cachedMM == null)
            {
                ModMenuLoader.Log("Not in a game");
                return;
            }
            isInternalMoneyChange = true;
            bool num = CallRemoveBalance(amount, countTowardPlayerProfit: true);
            isInternalMoneyChange = false;
            if (num)
            {
                lastKnownBalance = -1L;
                revertCooldown = 1f;
            }
            ModMenuLoader.Log(num ? $"Removed ${amount:N0}!" : "Failed");
        }

        // Invokes the best available game method for increasing balance
        private bool CallAddBalance(long amount, bool countTowardPlayerProfit)
        {
            if (cachedMM == null)
            {
                return false;
            }
            try
            {
                PlayerProfile? localPlayerProfile = GetLocalPlayerProfile();
                object? changeType = countTowardPlayerProfit ? changeTypePlayerProfit : changeTypeMisc;
                if (cachedMM.isServer && changeType != null && localPlayerProfile != null)
                {
                    // Hosts can call the authoritative balance path directly
                    MethodInfo method = typeof(MoneyManager).GetMethod("AddBalance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null)
                    {
                        method.Invoke(cachedMM, new object[3] { amount, localPlayerProfile, changeType });
                        return true;
                    }
                }
                if (changeType != null && localPlayerProfile != null)
                {
                    // Clients request the same change through the game command
                    MethodInfo method2 = typeof(MoneyManager).GetMethod("CmdTryChangeBalance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method2 != null)
                    {
                        method2.Invoke(cachedMM, new object[3] { amount, localPlayerProfile, changeType });
                        return true;
                    }
                }
                FieldInfo field = typeof(MoneyManager).GetField("balance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    // Direct synchronized state is a compatibility fallback for changed method names
                    long num = (long)field.GetValue(cachedMM);
                    field.SetValue(cachedMM, num + amount);
                    cachedMM.Networkbalance = num + amount;
                    return true;
                }
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("AddBalance error: " + ex.Message);
            }
            return false;
        }

        // Invokes the best available game method for decreasing balance
        private bool CallRemoveBalance(long amount, bool countTowardPlayerProfit)
        {
            if (cachedMM == null)
            {
                return false;
            }
            try
            {
                PlayerProfile? localPlayerProfile = GetLocalPlayerProfile();
                object? changeType = countTowardPlayerProfit ? changeTypePlayerProfit : changeTypeMisc;
                if (cachedMM.isServer && changeType != null && localPlayerProfile != null)
                {
                    // Keep normal host-side accounting and notifications when available
                    MethodInfo method = typeof(MoneyManager).GetMethod("RemoveBalance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null)
                    {
                        method.Invoke(cachedMM, new object[3] { amount, localPlayerProfile, changeType });
                        return true;
                    }
                }
                if (changeType != null && localPlayerProfile != null)
                {
                    MethodInfo method2 = typeof(MoneyManager).GetMethod("CmdTryChangeBalance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method2 != null)
                    {
                        method2.Invoke(cachedMM, new object[3]
                        {
                            -amount,
                            localPlayerProfile,
                            changeType
                        });
                        return true;
                    }
                }
                FieldInfo field = typeof(MoneyManager).GetField("balance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    // Balance never falls below the game's zero lower bound
                    long num = (long)field.GetValue(cachedMM) - amount;
                    if (num < 0)
                    {
                        num = 0L;
                    }
                    field.SetValue(cachedMM, num);
                    cachedMM.Networkbalance = num;
                    return true;
                }
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("RemoveBalance error: " + ex.Message);
            }
            return false;
        }

        // Adds only the remaining balance required to satisfy the quota
        private void MeetQuota()
        {
            if (cachedGM == null || cachedMM == null)
            {
                ModMenuLoader.Log("Not in a game");
                return;
            }
            try
            {
                long networkbalance = cachedMM.Networkbalance;
                long networkcurrentQuota = cachedGM.NetworkcurrentQuota;
                ModMenuLoader.Log($"MeetQuota: money={networkbalance}, quotaTarget={networkcurrentQuota}");
                if (networkbalance >= networkcurrentQuota)
                {
                    ModMenuLoader.Log($"Already have ${networkbalance:N0} >= quota ${networkcurrentQuota:N0}!");
                    return;
                }
                long num = networkcurrentQuota - networkbalance;
                ModMenuLoader.Log($"MeetQuota: adding ${num} to reach quota");
                isInternalMoneyChange = true;
                bool num2 = CallAddBalance(num, countTowardPlayerProfit: true);
                isInternalMoneyChange = false;
                if (num2)
                {
                    lastKnownBalance = -1L;
                }
                ModMenuLoader.Log(num2 ? $"Added ${num:N0} to meet quota!" : "Failed");
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("Error: " + ex.Message);
                ModMenuLoader.Log($"MeetQuota error: {ex}");
            }
        }

        // Invokes the best available game method for increasing tickets
        private bool CallAddTickets(long amount)
        {
            if (cachedMM == null)
            {
                return false;
            }
            try
            {
                if (cachedGM != null && cachedGM.isServer)
                {
                    // Hosts use the direct ticket mutation before trying a command path
                    MethodInfo method = typeof(MoneyManager).GetMethod("AddTicket", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null)
                    {
                        method.Invoke(cachedMM, new object[1] { amount });
                        return true;
                    }
                }
                MethodInfo method2 = typeof(MoneyManager).GetMethod("CmdTryChangeTicketBalance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method2 != null)
                {
                    method2.Invoke(cachedMM, new object[1] { amount });
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        // Validates and applies a ticket increase
        private void AddTickets(long amount)
        {
            if (CallAddTickets(amount))
            {
                ModMenuLoader.Log($"Added {amount:N0} tickets!");
            }
            else
            {
                ModMenuLoader.Log("Failed to add tickets");
            }
        }

        // Finds a nearby game and requests its normal payout flow
        private void TriggerWin()
        {
            try
            {
                NewConsole newConsole = UnityEngine.Object.FindFirstObjectByType<NewConsole>();
                if (newConsole != null)
                {
                    MethodInfo method = typeof(NewConsole).GetMethod("SimulateWinLose", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null)
                    {
                        method.Invoke(newConsole, new object[1] { true });
                        lastKnownBalance = -1L;
                        ModMenuLoader.Log("Win triggered!");
                        return;
                    }
                }
                ModMenuLoader.Log("Not in a game");
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("Error: " + ex.Message);
            }
        }

    }
}
