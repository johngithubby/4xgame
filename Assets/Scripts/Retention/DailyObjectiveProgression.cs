using System;
using LaneSurvivor.Economy;
using LaneSurvivor.Save;
using UnityEngine;

namespace LaneSurvivor.Retention
{
    public static class DailyObjectiveProgression
    {
        public const int WinsRequired = 2;

        public const int RewardCoins = 75;

        private static readonly DateTime UnixEpochUtc = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static bool EnsureCurrentObjective(SaveGameData data, DateTime utcNow)
        {
            // Null saves cannot hold retention progress, so callers can safely ignore a false return.
            if (data == null)
            {
                return false;
            }

            // Normalize before comparing dates so corrupted counters cannot survive into the active objective.
            data.Normalize();

            // The day number is UTC-based so app relaunches on the same day keep the same objective.
            int currentDayNumber = GetUtcDayNumber(utcNow);
            if (data.dailyObjectiveUtcDayNumber == currentDayNumber)
            {
                return false;
            }

            // A new UTC day starts a fresh local objective and clears yesterday's claim state.
            data.dailyObjectiveUtcDayNumber = currentDayNumber;
            data.dailyObjectiveWins = 0;
            data.dailyObjectiveRewardClaimed = false;
            return true;
        }

        public static DailyObjectiveStatus GetStatus(SaveGameData data, DateTime utcNow)
        {
            // Ensure the save has today's objective before UI or reward text reads the progress fields.
            EnsureCurrentObjective(data, utcNow);

            // Missing data still returns a harmless empty status for tests and defensive UI calls.
            int wins = Mathf.Clamp(data?.dailyObjectiveWins ?? 0, 0, WinsRequired);
            bool rewardClaimed = data != null && data.dailyObjectiveRewardClaimed;
            bool canClaimReward = wins >= WinsRequired && !rewardClaimed;
            return new DailyObjectiveStatus(wins, WinsRequired, RewardCoins, rewardClaimed, canClaimReward);
        }

        public static DailyObjectiveStatus RecordMinigameWin(SaveGameData data, DateTime utcNow)
        {
            // Route through EnsureCurrentObjective so a win after midnight starts the new local objective cleanly.
            EnsureCurrentObjective(data, utcNow);

            // Null saves cannot progress, but callers still receive a readable empty status.
            if (data == null)
            {
                return new DailyObjectiveStatus(0, WinsRequired, RewardCoins, false, false);
            }

            // Additional wins after completion remain harmless and do not overfill the saved counter.
            if (data.dailyObjectiveWins < WinsRequired)
            {
                data.dailyObjectiveWins += 1;
            }

            // Return the post-win status so reward screens can describe the new objective state.
            return GetStatus(data, utcNow);
        }

        public static bool TryClaimReward(SaveGameData data, DateTime utcNow)
        {
            // The status helper centralizes day rollover and eligibility checks.
            DailyObjectiveStatus status = GetStatus(data, utcNow);
            if (data == null || !status.canClaimReward)
            {
                return false;
            }

            // Claiming is local-only and pays a fixed coin reward for the prototype retention loop.
            ResourceWallet.AddCoins(data, RewardCoins);
            data.dailyObjectiveRewardClaimed = true;
            return true;
        }

        public static string BuildStatusLabel(DailyObjectiveStatus status)
        {
            // The label is intentionally short because it lives in the compact Base HUD.
            string progress = $"{status.wins}/{status.winsRequired} wins";
            if (status.rewardClaimed)
            {
                return $"Daily: {progress} claimed";
            }

            // A ready label tells the player to use the nearby claim button instead of starting another run.
            if (status.canClaimReward)
            {
                return $"Daily: {progress} claim +{status.rewardCoins}c";
            }

            // Incomplete objectives keep the next session goal visible from the Base screen.
            return $"Daily: {progress} for +{status.rewardCoins}c";
        }

        public static int GetUtcDayNumber(DateTime utcNow)
        {
            // Force UTC before taking Date so local timezone changes cannot split one UTC objective day.
            DateTime normalizedUtc = utcNow.Kind == DateTimeKind.Utc ? utcNow : utcNow.ToUniversalTime();

            // Date subtraction gives an integer-like day count suitable for compact JSON storage.
            return Mathf.Max(0, (int)(normalizedUtc.Date - UnixEpochUtc).TotalDays);
        }
    }

    public readonly struct DailyObjectiveStatus
    {
        public DailyObjectiveStatus(int wins, int winsRequired, int rewardCoins, bool rewardClaimed, bool canClaimReward)
        {
            // Clamp visible progress so UI never prints impossible negative or overfilled values.
            this.wins = Mathf.Clamp(wins, 0, Mathf.Max(1, winsRequired));

            // Store the requirement and reward amount so UI does not duplicate constants.
            this.winsRequired = Mathf.Max(1, winsRequired);
            this.rewardCoins = Mathf.Max(0, rewardCoins);
            this.rewardClaimed = rewardClaimed;
            this.canClaimReward = canClaimReward;
        }

        public readonly int wins;

        public readonly int winsRequired;

        public readonly int rewardCoins;

        public readonly bool rewardClaimed;

        public readonly bool canClaimReward;
    }
}
