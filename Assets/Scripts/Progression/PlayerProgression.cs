using System;
using LaneSurvivor.Economy;
using LaneSurvivor.Save;
using UnityEngine;

namespace LaneSurvivor.Progression
{
    public static class PlayerProgression
    {
        public const int CoinsPerCollect = 25;

        public const int HqUpgradeDurationSeconds = 20;

        public static void CollectCoins(SaveGameData data)
        {
            // The collect button is deliberately simple for the first base-building slice.
            ResourceWallet.AddCoins(data, CoinsPerCollect);
        }

        public static int GetHqUpgradeCost(int hqLevel)
        {
            // Costs rise slowly so the prototype can be exercised in a few taps.
            return 50 + Mathf.Max(1, hqLevel) * 25;
        }

        public static bool TryStartHqUpgrade(SaveGameData data, DateTime utcNow)
        {
            // Only one local HQ timer can run at a time in this slice.
            if (data == null || data.hqUpgradeInProgress)
            {
                return false;
            }

            // Charge the current level's upgrade cost before starting the timer.
            int cost = GetHqUpgradeCost(data.hqLevel);
            if (!ResourceWallet.TrySpendCoins(data, cost))
            {
                return false;
            }

            // Persist the start time so closing the game does not pause progress.
            UpgradeTimer.Start(data, utcNow, HqUpgradeDurationSeconds);
            return true;
        }

        public static bool CompleteReadyHqUpgrade(SaveGameData data, DateTime utcNow)
        {
            // Completion is idempotent so callers can check from scene load and Update.
            if (data == null || !UpgradeTimer.IsComplete(data, utcNow))
            {
                return false;
            }

            // Level up once, clear the timer, and expose a matching local content unlock value.
            data.hqLevel += 1;
            data.unlockedMinigameLevel = Mathf.Max(data.unlockedMinigameLevel, data.hqLevel);
            data.ClearHqUpgrade();
            return true;
        }

        public static int GetStartingSquadBonus(SaveGameData data)
        {
            // HQ level 1 is baseline; each additional HQ level adds one starting squad member.
            return Mathf.Max(0, (data?.hqLevel ?? 1) - 1);
        }

        public static int GetHqUpgradeRemainingSeconds(SaveGameData data, DateTime utcNow)
        {
            // Expose timer formatting data without making UI depend on timer internals.
            return UpgradeTimer.GetRemainingSeconds(data, utcNow);
        }
    }
}
