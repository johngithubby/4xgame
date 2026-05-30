using LaneSurvivor.Save;
using UnityEngine;

namespace LaneSurvivor.Economy
{
    public static class ResourceWallet
    {
        public static void AddCoins(SaveGameData data, int amount)
        {
            // Ignore non-positive grants so callers can safely pass computed values.
            if (data == null || amount <= 0)
            {
                return;
            }

            // Clamp to non-negative after addition to protect against integer underflow edge cases.
            data.coins = Mathf.Max(0, data.coins + amount);
        }

        public static bool TrySpendCoins(SaveGameData data, int amount)
        {
            // Spending zero or negative coins is never meaningful in this prototype.
            if (data == null || amount <= 0)
            {
                return false;
            }

            // Refuse the transaction if the player cannot afford it.
            if (data.coins < amount)
            {
                return false;
            }

            // Apply the spend only after all validation succeeds.
            data.coins -= amount;
            return true;
        }
    }
}
