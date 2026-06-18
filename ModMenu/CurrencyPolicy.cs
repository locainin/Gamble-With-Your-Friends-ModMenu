using System;

namespace ModMenu
{
    // Pure balance math stays separate from Unity state so large-value rules can be tested directly
    internal static class CurrencyPolicy
    {
        internal const string DefaultMoneyInput = "10000";

        // Calculates only the extra gain because the original payout already reached the balance
        internal static long CalculateMultiplierBonus(long balanceGain, decimal multiplier)
        {
            if (balanceGain <= 0 || multiplier <= 1m)
            {
                return 0L;
            }

            decimal bonus = balanceGain * (multiplier - 1m);
            if (bonus >= long.MaxValue)
            {
                return long.MaxValue;
            }

            return decimal.ToInt64(decimal.Truncate(bonus));
        }

        // Keeps the amount editor numeric while still allowing a focused empty field
        internal static string NormalizeMoneyInput(string? rawValue, bool isFocused)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return isFocused ? string.Empty : DefaultMoneyInput;
            }

            string digits = ExtractDigits(rawValue);
            if (digits.Length == 0)
            {
                return isFocused ? string.Empty : DefaultMoneyInput;
            }

            // Leading zeros are display noise, but a pure zero remains valid text
            digits = digits.TrimStart('0');
            if (digits.Length == 0)
            {
                return "0";
            }

            // Oversized pasted values clamp instead of poisoning the field
            return long.TryParse(digits, out long parsedAmount)
                ? parsedAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : long.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        // Rejects blank, zero, and overflow values before economy calls run
        internal static bool TryParsePositiveAmount(string? rawValue, out long amount)
        {
            amount = 0L;
            if (!long.TryParse(rawValue, out long parsedAmount) || parsedAmount <= 0L)
            {
                return false;
            }

            amount = parsedAmount;
            return true;
        }

        // Filters paste input without accepting signs, decimals, or currency symbols
        private static string ExtractDigits(string value)
        {
            char[] buffer = new char[value.Length];
            int length = 0;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsDigit(value[i]))
                {
                    buffer[length] = value[i];
                    length++;
                }
            }

            return new string(buffer, 0, length);
        }
    }
}
