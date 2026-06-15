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
            if (!HasEconomyAuthority())
            {
                return;
            }
            MoneyManager moneyManager = cachedMM!;
            if (revertCooldown > 0f)
            {
                revertCooldown -= Time.unscaledDeltaTime;
                return;
            }
            try
            {
                long networkbalance = moneyManager.Networkbalance;
                // The first sample becomes the protected balance instead of creating a refund
                if (lastKnownBalance < 0)
                {
                    lastKnownBalance = networkbalance;
                }
                else if (networkbalance < lastKnownBalance)
                {
                    long num = lastKnownBalance - networkbalance;
                    // Internal changes are excluded from multiplier detection
                    // Refunds restore shared funds without recording fake player earnings
                    RunInternalMoneyChange(() => CallAddBalance(num, countTowardPlayerProfit: false));
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
            if (!HasEconomyAuthority())
            {
                return;
            }
            MoneyManager moneyManager = cachedMM!;
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
                    long num = (long)property.GetValue(moneyManager);
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
            if (!HasEconomyAuthority())
            {
                return;
            }
            MoneyManager moneyManager = cachedMM!;
            if (multiplierCooldown > 0f)
            {
                multiplierCooldown -= Time.unscaledDeltaTime;
                return;
            }
            try
            {
                long networkbalance = moneyManager.Networkbalance;
                if (lastMultiplierBalance < 0)
                {
                    lastMultiplierBalance = networkbalance;
                    return;
                }
                if (networkbalance > lastMultiplierBalance && !isInternalMoneyChange)
                {
                    long num = CurrencyPolicy.CalculateMultiplierBonus(
                        networkbalance - lastMultiplierBalance,
                        (decimal)moneyMultiplier);
                    if (num > 0)
                    {
                        // Add only the bonus because the game already applied the original gain
                        // Multiplier bonuses are real gains and belong in the player total
                        bool changed = RunInternalMoneyChange(
                            () => CallAddBalance(num, countTowardPlayerProfit: true));
                        if (!changed)
                        {
                            lastMultiplierBalance = networkbalance;
                            return;
                        }
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
            if (!HasEconomyAuthority() || amount <= 0)
            {
                ModMenuLoader.Log("Host authority is required to add money");
                return;
            }
            bool num = RunInternalMoneyChange(
                () => CallChangeBalance(amount, countTowardPlayerProfit: true));
            if (num)
            {
                lastKnownBalance = -1L;
                RecordShowcaseBalanceChange(amount);
            }
            ModMenuLoader.Log(num ? $"Added ${amount:N0}!" : "Failed");
        }

        // Validates and routes a positive money removal
        private void RemoveMoney(long amount)
        {
            if (!HasEconomyAuthority() || amount <= 0)
            {
                ModMenuLoader.Log("Host authority is required to remove money");
                return;
            }
            MoneyManager moneyManager = cachedMM!;
            long removableAmount = Math.Min(amount, moneyManager.Networkbalance);
            if (removableAmount <= 0)
            {
                ModMenuLoader.Log("No money available to remove");
                return;
            }
            bool num = RunInternalMoneyChange(
                () => CallChangeBalance(-removableAmount, countTowardPlayerProfit: true));
            if (num)
            {
                lastKnownBalance = -1L;
                revertCooldown = 1f;
                RecordShowcaseBalanceChange(-removableAmount);
            }
            ModMenuLoader.Log(num ? $"Removed ${removableAmount:N0}!" : "Failed");
        }

        // Converts a positive grant into the signed balance mutation used by the game
        private bool CallAddBalance(long amount, bool countTowardPlayerProfit)
        {
            return amount > 0 && CallChangeBalance(amount, countTowardPlayerProfit);
        }

        // Uses one signed path so balance and player-profit listeners receive the same direction
        private bool CallChangeBalance(long signedAmount, bool countTowardPlayerProfit)
        {
            if (!HasEconomyAuthority() || signedAmount == 0)
            {
                return false;
            }
            MoneyManager moneyManager = cachedMM!;
            try
            {
                PlayerProfile? localPlayerProfile = GetLocalPlayerProfile();
                object? changeType = countTowardPlayerProfit ? changeTypePlayerProfit : changeTypeMisc;
                if (changeType != null && localPlayerProfile != null)
                {
                    // TryChangeBalance validates both bounds before publishing the signed change
                    return moneyManager.TryChangeBalance(signedAmount, localPlayerProfile, (ChangeType)changeType);
                }
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("ChangeBalance error: " + ex.Message);
            }
            return false;
        }

        // Registers manual changes through the same result pipeline used by casino games
        private void RecordShowcaseBalanceChange(long signedAmount)
        {
            if (signedAmount == 0 || cachedGM == null || !cachedGM.isServer)
            {
                return;
            }

            try
            {
                PlayerProfile? localPlayerProfile = GetLocalPlayerProfile();
                GameResultsManager? resultsManager = UnityEngine.Object.FindFirstObjectByType<GameResultsManager>();
                if (localPlayerProfile == null || resultsManager == null)
                {
                    ModMenuLoader.Log("Game result tracking unavailable in this scene");
                    return;
                }

                long bet = signedAmount < 0 ? -signedAmount : 0L;
                long payout = signedAmount > 0 ? signedAmount : 0L;
                // The game has no manual result type, so its first valid enum keeps the record serializable
                resultsManager.RegisterResult(
                    bet,
                    payout,
                    localPlayerProfile,
                    default(CasinoGameType),
                    localPlayerProfile.transform.position);
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("Game result tracking error: " + ex.Message);
            }
        }

        // Adds only the remaining balance required to satisfy the quota
        private void MeetQuota()
        {
            if (!HasEconomyAuthority())
            {
                ModMenuLoader.Log("Host authority is required to meet quota");
                return;
            }
            MoneyManager moneyManager = cachedMM!;
            GameManager gameManager = cachedGM!;
            try
            {
                long networkbalance = moneyManager.Networkbalance;
                long networkcurrentQuota = gameManager.NetworkcurrentQuota;
                ModMenuLoader.Log($"MeetQuota: money={networkbalance}, quotaTarget={networkcurrentQuota}");
                if (networkbalance >= networkcurrentQuota)
                {
                    ModMenuLoader.Log($"Already have ${networkbalance:N0} >= quota ${networkcurrentQuota:N0}!");
                    return;
                }
                long num = networkcurrentQuota - networkbalance;
                ModMenuLoader.Log($"MeetQuota: adding ${num} to reach quota");
                bool num2 = RunInternalMoneyChange(
                    () => CallAddBalance(num, countTowardPlayerProfit: true));
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

        // Uses the host-only ticket validator instead of the client command accepted by the base game
        private bool CallAddTickets(long amount)
        {
            if (!HasEconomyAuthority() || amount <= 0)
            {
                return false;
            }
            MoneyManager moneyManager = cachedMM!;
            try
            {
                // Direct server validation avoids exposing the game's unauthenticated command path
                return moneyManager.TryChangeTicketBalance(amount);
            }
            catch (Exception ex)
            {
                ModMenuLoader.Log("Ticket change error: " + ex.Message);
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
            if (!HasEconomyAuthority())
            {
                ModMenuLoader.Log("Host authority is required to trigger a win");
                return;
            }

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

        // Economy mutations require both scene managers to be authoritative
        private bool HasEconomyAuthority()
        {
            // Checking both managers prevents stale cross-scene references from authorizing a mutation
            return cachedGM != null &&
                cachedGM.isServer &&
                cachedMM != null &&
                cachedMM.isServer;
        }

        // Always releases the multiplier suppression flag when a game call fails
        private bool RunInternalMoneyChange(Func<bool> mutation)
        {
            isInternalMoneyChange = true;
            try
            {
                return mutation();
            }
            finally
            {
                isInternalMoneyChange = false;
            }
        }

    }
}
