using System;

namespace ModMenu
{
    // Pure balance math stays separate from Unity state so large-value rules can be tested directly
    internal static class CurrencyPolicy
    {
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
    }
}
