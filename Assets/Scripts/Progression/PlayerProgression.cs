using System;
using LaneSurvivor.Economy;
using LaneSurvivor.Save;
using UnityEngine;

namespace LaneSurvivor.Progression
{
    public static class PlayerProgression
    {
        public const int CoinsPerCollect = 25;

        public const int MinigameWinCoins = 50;

        public const int HqUpgradeDurationSeconds = 20;

        public const int MaxMissionLevel = 4;

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

        public static int TryClaimMinigameWinReward(SaveGameData data, ref bool rewardClaimed)
        {
            // A null save cannot receive rewards, and each minigame run should pay out at most once.
            if (data == null || rewardClaimed)
            {
                return 0;
            }

            // Mark the reward as claimed before mutating coins so repeated calls in the same run are harmless.
            rewardClaimed = true;

            // Keep the prototype reward fixed and visible so the base loop is easy to verify.
            ResourceWallet.AddCoins(data, MinigameWinCoins);
            return MinigameWinCoins;
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

            // Level up once and clear the timer; mission unlocks now come from completed minigame runs.
            data.hqLevel += 1;
            data.ClearHqUpgrade();
            return true;
        }

        public static int GetSelectedMissionLevel(SaveGameData data)
        {
            // Normalize first so older saves and corrupted values read through the same safe mission range.
            data?.Normalize();
            return Mathf.Clamp(data?.currentMissionLevel ?? 1, 1, data?.highestUnlockedMissionLevel ?? 1);
        }

        public static int GetHighestUnlockedMissionLevel(SaveGameData data)
        {
            // The highest unlocked value drives the Base next button and post-win unlock checks.
            data?.Normalize();
            return Mathf.Clamp(data?.highestUnlockedMissionLevel ?? 1, 1, MaxMissionLevel);
        }

        public static string GetMissionName(int missionLevel)
        {
            // Short names fit the current generated Base HUD without needing a larger mission screen.
            return Mathf.Clamp(missionLevel, 1, MaxMissionLevel) switch
            {
                1 => "Outskirts",
                2 => "Market Run",
                3 => "Overpass",
                4 => "Last Block",
                _ => "Outskirts"
            };
        }

        public static bool TrySelectMission(SaveGameData data, int missionLevel)
        {
            // Null saves cannot be mutated, and locked or unknown missions should be ignored by UI buttons.
            if (data == null)
            {
                return false;
            }

            // Normalize before comparing so legacy unlocks are available to the mission selector.
            data.Normalize();
            if (missionLevel < 1 || missionLevel > data.highestUnlockedMissionLevel || missionLevel > MaxMissionLevel)
            {
                return false;
            }

            // Persist the selected mission and keep the legacy field synchronized for old save readers.
            data.currentMissionLevel = missionLevel;
            data.unlockedMinigameLevel = data.highestUnlockedMissionLevel;
            return true;
        }

        public static bool CanSelectPreviousMission(SaveGameData data)
        {
            // Previous selection is available whenever the current mission is above the first authored mission.
            return GetSelectedMissionLevel(data) > 1;
        }

        public static bool CanSelectNextMission(SaveGameData data)
        {
            // Next selection is available only inside the already-unlocked mission range.
            return GetSelectedMissionLevel(data) < GetHighestUnlockedMissionLevel(data);
        }

        public static bool WouldUnlockNextMission(SaveGameData data)
        {
            // Base reward hints only mention mission unlocks when the selected run can advance progression.
            int selectedMissionLevel = GetSelectedMissionLevel(data);
            int highestUnlockedMissionLevel = GetHighestUnlockedMissionLevel(data);
            return selectedMissionLevel >= highestUnlockedMissionLevel && highestUnlockedMissionLevel < MaxMissionLevel;
        }

        public static MissionCompletionResult TryCompleteMission(SaveGameData data, int missionLevel)
        {
            // Mission completion is save-backed, so a missing save cannot unlock local content.
            if (data == null || missionLevel < 1 || missionLevel > MaxMissionLevel)
            {
                return default;
            }

            // Normalize before checking unlock state so legacy saves progress from their migrated mission level.
            data.Normalize();
            if (missionLevel != data.highestUnlockedMissionLevel || data.highestUnlockedMissionLevel >= MaxMissionLevel)
            {
                data.unlockedMinigameLevel = data.highestUnlockedMissionLevel;
                return new MissionCompletionResult(missionLevel, 0, false);
            }

            // Completing the frontier mission unlocks exactly one next mission and auto-selects it for Base return.
            int unlockedMissionLevel = Mathf.Min(MaxMissionLevel, data.highestUnlockedMissionLevel + 1);
            data.highestUnlockedMissionLevel = unlockedMissionLevel;
            data.currentMissionLevel = unlockedMissionLevel;
            data.unlockedMinigameLevel = unlockedMissionLevel;
            return new MissionCompletionResult(missionLevel, unlockedMissionLevel, true);
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

    public readonly struct MissionCompletionResult
    {
        public MissionCompletionResult(int completedMissionLevel, int unlockedMissionLevel, bool unlockedNewMission)
        {
            // Store the completed mission so reward UI can describe the result without re-reading mutable save data.
            this.completedMissionLevel = completedMissionLevel;

            // Zero means no new mission was unlocked by this completion.
            this.unlockedMissionLevel = unlockedMissionLevel;

            // A separate boolean keeps the reward text clear when unlockedMissionLevel is the default zero value.
            this.unlockedNewMission = unlockedNewMission;
        }

        public readonly int completedMissionLevel;

        public readonly int unlockedMissionLevel;

        public readonly bool unlockedNewMission;
    }
}
