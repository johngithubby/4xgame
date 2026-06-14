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

        public const int MaxMissionLevel = 8;

        private const int BioLabLevelOneDurationSeconds = 1;

        private const int BioLabLevelTwoDurationSeconds = 3;

        private const int BioLabLevelThreeDurationSeconds = 10;

        private const int BioLabLevelFourDurationSeconds = 60;

        private const int BioLabDurationGrowthMultiplier = 5;

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

        public static int GetBioLabUpgradeCost(int bioLabLevel)
        {
            // Bio-lab upgrades use the same local credit scale as early HQ upgrades for rapid prototype testing.
            return 50 + Mathf.Max(1, bioLabLevel) * 25;
        }

        public static int GetHqUpgradeDurationSeconds(int hqLevel)
        {
            // HQ timers now use the same level-based curve as the other upgradeable Base buildings.
            return GetBioLabUpgradeDurationSeconds(hqLevel);
        }

        public static int GetHangarUpgradeCost(int hangarLevel)
        {
            // Hangar upgrades follow the bio-lab credit scale so all popup-arrow buildings use the same local rule.
            return GetBioLabUpgradeCost(hangarLevel);
        }

        public static int GetTrainingFacilityUpgradeCost(int trainingFacilityLevel)
        {
            // Training upgrades follow the bio-lab credit scale so all popup-arrow buildings use the same local rule.
            return GetBioLabUpgradeCost(trainingFacilityLevel);
        }

        public static int GetLivingQuartersUpgradeCost(int livingQuartersLevel)
        {
            // Living-quarters upgrades follow the same local credit scale as every same-rule Base building.
            return GetBioLabUpgradeCost(livingQuartersLevel);
        }

        public static int GetBioLabUpgradeDurationSeconds(int bioLabLevel)
        {
            // The first three lab levels use the exact requested hand-authored ramp.
            int safeLevel = Mathf.Max(1, bioLabLevel);
            if (safeLevel == 1)
            {
                return BioLabLevelOneDurationSeconds;
            }

            if (safeLevel == 2)
            {
                return BioLabLevelTwoDurationSeconds;
            }

            if (safeLevel == 3)
            {
                return BioLabLevelThreeDurationSeconds;
            }

            // Level four starts the minute-scale progression requested for advanced lab upgrades.
            long durationSeconds = BioLabLevelFourDurationSeconds;

            // Every level after four multiplies the prior duration by five, with an int clamp for save safety.
            for (int level = 5; level <= safeLevel; level += 1)
            {
                if (durationSeconds > int.MaxValue / BioLabDurationGrowthMultiplier)
                {
                    return int.MaxValue;
                }

                durationSeconds *= BioLabDurationGrowthMultiplier;
            }

            return (int)Math.Min(durationSeconds, int.MaxValue);
        }

        public static int GetHangarUpgradeDurationSeconds(int hangarLevel)
        {
            // Hangar timers reuse the bio-lab ramp so same-rule building upgrades feel consistent.
            return GetBioLabUpgradeDurationSeconds(hangarLevel);
        }

        public static int GetTrainingFacilityUpgradeDurationSeconds(int trainingFacilityLevel)
        {
            // Training timers reuse the bio-lab ramp so same-rule building upgrades feel consistent.
            return GetBioLabUpgradeDurationSeconds(trainingFacilityLevel);
        }

        public static int GetLivingQuartersUpgradeDurationSeconds(int livingQuartersLevel)
        {
            // Living-quarters timers reuse the bio-lab ramp so same-rule building upgrades feel consistent.
            return GetBioLabUpgradeDurationSeconds(livingQuartersLevel);
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

            // Persist the level-derived duration so HQ upgrades follow the same curve as other buildings.
            UpgradeTimer.Start(data, utcNow, GetHqUpgradeDurationSeconds(data.hqLevel));
            return true;
        }

        public static bool TryStartBioLabUpgrade(SaveGameData data, DateTime utcNow)
        {
            // Only one local bio-lab timer can run at a time in this slice.
            if (data == null || data.bioLabUpgradeInProgress)
            {
                return false;
            }

            // Charge the current lab level's credit cost before starting the timer.
            int cost = GetBioLabUpgradeCost(data.bioLabLevel);
            if (!ResourceWallet.TrySpendCoins(data, cost))
            {
                return false;
            }

            // Persist the start time and level-derived duration so app restarts do not pause progress.
            data.bioLabUpgradeInProgress = true;
            data.bioLabUpgradeStartedUtcTicks = utcNow.ToUniversalTime().Ticks;
            data.bioLabUpgradeDurationSeconds = GetBioLabUpgradeDurationSeconds(data.bioLabLevel);
            return true;
        }

        public static bool TryStartHangarUpgrade(SaveGameData data, DateTime utcNow)
        {
            // Only one local hangar timer can run at a time, matching the bio-lab interaction rule.
            if (data == null || data.hangarUpgradeInProgress)
            {
                return false;
            }

            // Charge the current hangar level's credit cost before starting the timer.
            int cost = GetHangarUpgradeCost(data.hangarLevel);
            if (!ResourceWallet.TrySpendCoins(data, cost))
            {
                return false;
            }

            // Persist the start time and level-derived duration so app restarts do not pause progress.
            data.hangarUpgradeInProgress = true;
            data.hangarUpgradeStartedUtcTicks = utcNow.ToUniversalTime().Ticks;
            data.hangarUpgradeDurationSeconds = GetHangarUpgradeDurationSeconds(data.hangarLevel);
            return true;
        }

        public static bool TryStartTrainingFacilityUpgrade(SaveGameData data, DateTime utcNow)
        {
            // Only one local training timer can run at a time, matching the bio-lab interaction rule.
            if (data == null || data.trainingFacilityUpgradeInProgress)
            {
                return false;
            }

            // Charge the current training level's credit cost before starting the timer.
            int cost = GetTrainingFacilityUpgradeCost(data.trainingFacilityLevel);
            if (!ResourceWallet.TrySpendCoins(data, cost))
            {
                return false;
            }

            // Persist the start time and level-derived duration so app restarts do not pause progress.
            data.trainingFacilityUpgradeInProgress = true;
            data.trainingFacilityUpgradeStartedUtcTicks = utcNow.ToUniversalTime().Ticks;
            data.trainingFacilityUpgradeDurationSeconds = GetTrainingFacilityUpgradeDurationSeconds(data.trainingFacilityLevel);
            return true;
        }

        public static bool TryStartLivingQuartersUpgrade(SaveGameData data, DateTime utcNow)
        {
            // Only one local living-quarters timer can run at a time, matching the other facility rule.
            if (data == null || data.livingQuartersUpgradeInProgress)
            {
                return false;
            }

            // Charge the current living-quarters level's credit cost before starting the timer.
            int cost = GetLivingQuartersUpgradeCost(data.livingQuartersLevel);
            if (!ResourceWallet.TrySpendCoins(data, cost))
            {
                return false;
            }

            // Persist the start time and level-derived duration so app restarts do not pause progress.
            data.livingQuartersUpgradeInProgress = true;
            data.livingQuartersUpgradeStartedUtcTicks = utcNow.ToUniversalTime().Ticks;
            data.livingQuartersUpgradeDurationSeconds = GetLivingQuartersUpgradeDurationSeconds(data.livingQuartersLevel);
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

        public static bool CompleteReadyBioLabUpgrade(SaveGameData data, DateTime utcNow)
        {
            // Completion is idempotent so callers can check from scene load and Update.
            if (data == null || !IsBioLabUpgradeComplete(data, utcNow))
            {
                return false;
            }

            // Level up once and clear the timer after the progress icon has reached completion.
            data.bioLabLevel += 1;
            data.ClearBioLabUpgrade();
            return true;
        }

        public static bool CompleteReadyHangarUpgrade(SaveGameData data, DateTime utcNow)
        {
            // Completion is idempotent so callers can check from scene load and Update.
            if (data == null || !IsHangarUpgradeComplete(data, utcNow))
            {
                return false;
            }

            // Level up once and clear the hangar timer after the circular progress reaches completion.
            data.hangarLevel += 1;
            data.ClearHangarUpgrade();
            return true;
        }

        public static bool CompleteReadyTrainingFacilityUpgrade(SaveGameData data, DateTime utcNow)
        {
            // Completion is idempotent so callers can check from scene load and Update.
            if (data == null || !IsTrainingFacilityUpgradeComplete(data, utcNow))
            {
                return false;
            }

            // Level up once and clear the training timer after the circular progress reaches completion.
            data.trainingFacilityLevel += 1;
            data.ClearTrainingFacilityUpgrade();
            return true;
        }

        public static bool CompleteReadyLivingQuartersUpgrade(SaveGameData data, DateTime utcNow)
        {
            // Completion is idempotent so callers can check from scene load and Update.
            if (data == null || !IsLivingQuartersUpgradeComplete(data, utcNow))
            {
                return false;
            }

            // Level up once and clear the living-quarters timer after the circular progress reaches completion.
            data.livingQuartersLevel += 1;
            data.ClearLivingQuartersUpgrade();
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
            // The highest unlocked value drives Base mission availability and post-win unlock checks.
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
                5 => "Armory Cut",
                6 => "Depot Push",
                7 => "Crossfire",
                8 => "Final Hold",
                _ => "Outskirts"
            };
        }

        public static bool IsMissionUnlocked(SaveGameData data, int missionLevel)
        {
            // Unknown mission ids should not become interactable even if corrupted UI calls into progression.
            if (data == null || missionLevel < 1 || missionLevel > MaxMissionLevel)
            {
                return false;
            }

            // Normalize first so legacy unlock mirrors and repaired ranges are honored by direct mission buttons.
            data.Normalize();
            return missionLevel <= data.highestUnlockedMissionLevel;
        }

        public static bool IsMissionCompleted(SaveGameData data, int missionLevel)
        {
            // Completion can only be true for authored mission ids on a real save object.
            if (data == null || missionLevel < 1 || missionLevel > MaxMissionLevel)
            {
                return false;
            }

            // Normalize fills sequential predecessor completions for saves created before this field existed.
            data.Normalize();
            return data.completedMissionLevels.Contains(missionLevel);
        }

        public static string GetMissionStatusLabel(SaveGameData data, int missionLevel)
        {
            // Locked missions need the clearest label because the buttons are visible before they are available.
            if (!IsMissionUnlocked(data, missionLevel))
            {
                return "LOCKED";
            }

            // A selected completed mission should show both that it is playable and already cleared.
            bool isSelectedMission = GetSelectedMissionLevel(data) == missionLevel;
            bool isCompletedMission = IsMissionCompleted(data, missionLevel);
            if (isSelectedMission && isCompletedMission)
            {
                return "DONE SELECTED";
            }

            // The selected frontier mission is the one the Play button will launch.
            if (isSelectedMission)
            {
                return "SELECTED";
            }

            // Completed missions remain replayable for coins, but no longer unlock new missions.
            if (isCompletedMission)
            {
                return "DONE";
            }

            // Unlocked, uncleared missions are ready to become the active selection.
            return "READY";
        }

        public static string GetMissionRewardHint(SaveGameData data, int missionLevel)
        {
            // Invalid rows should never appear in the Base panel, but keep the helper total for tests and callers.
            if (missionLevel < 1 || missionLevel > MaxMissionLevel)
            {
                return "Unavailable";
            }

            // Locked rows name the previous mission that gates progress.
            if (!IsMissionUnlocked(data, missionLevel))
            {
                return missionLevel <= 1 ? "Locked" : $"Unlock: clear M{missionLevel - 1}";
            }

            // Completed missions can still be replayed for the standard local coin reward.
            if (IsMissionCompleted(data, missionLevel))
            {
                return $"Replay: +{MinigameWinCoins}c";
            }

            // Clearing the current frontier mission previews the next local mission unlock.
            if (missionLevel >= GetHighestUnlockedMissionLevel(data) && missionLevel < MaxMissionLevel)
            {
                return $"Win: +{MinigameWinCoins}c + M{missionLevel + 1}";
            }

            // Non-frontier unlocked missions pay coins without advancing the local mission cap.
            return $"Win: +{MinigameWinCoins}c";
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

            // Locked mission ids should not be accepted as completed through corrupted scene state.
            if (missionLevel > data.highestUnlockedMissionLevel)
            {
                data.unlockedMinigameLevel = data.highestUnlockedMissionLevel;
                return new MissionCompletionResult(missionLevel, 0, false, false);
            }

            // Track completion separately from the highest unlocked mission so the Base panel can show done rows.
            bool completedFirstTime = TryMarkMissionCompleted(data, missionLevel);
            if (missionLevel != data.highestUnlockedMissionLevel || data.highestUnlockedMissionLevel >= MaxMissionLevel)
            {
                data.unlockedMinigameLevel = data.highestUnlockedMissionLevel;
                return new MissionCompletionResult(missionLevel, 0, false, completedFirstTime);
            }

            // Completing the frontier mission unlocks exactly one next mission and auto-selects it for Base return.
            int unlockedMissionLevel = Mathf.Min(MaxMissionLevel, data.highestUnlockedMissionLevel + 1);
            data.highestUnlockedMissionLevel = unlockedMissionLevel;
            data.currentMissionLevel = unlockedMissionLevel;
            data.unlockedMinigameLevel = unlockedMissionLevel;
            return new MissionCompletionResult(missionLevel, unlockedMissionLevel, true, completedFirstTime);
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

        public static float GetHqUpgradeProgress01(SaveGameData data, DateTime utcNow)
        {
            // Expose the same persisted HQ timer as a normalized fill amount for world-space progress UI.
            return UpgradeTimer.GetProgress01(data, utcNow);
        }

        public static int GetBioLabUpgradeRemainingSeconds(SaveGameData data, DateTime utcNow)
        {
            // No active bio-lab timer should display remaining time.
            if (data == null || !data.bioLabUpgradeInProgress)
            {
                return 0;
            }

            // Corrupted timer data cannot display a meaningful countdown, so repair and show zero.
            if (!TryGetBioLabUpgradeCompletionUtc(data, out DateTime completesUtc))
            {
                data.ClearBioLabUpgrade();
                return 0;
            }

            // Compare the valid completion timestamp with the current UTC time.
            TimeSpan remaining = completesUtc - utcNow.ToUniversalTime();
            return Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        }

        public static int GetHangarUpgradeRemainingSeconds(SaveGameData data, DateTime utcNow)
        {
            // No active hangar timer should display remaining time.
            if (data == null || !data.hangarUpgradeInProgress)
            {
                return 0;
            }

            // Corrupted timer data cannot display a meaningful countdown, so repair and show zero.
            if (!TryGetUpgradeCompletionUtc(data.hangarUpgradeStartedUtcTicks, data.hangarUpgradeDurationSeconds, out DateTime completesUtc))
            {
                data.ClearHangarUpgrade();
                return 0;
            }

            // Compare the valid completion timestamp with the current UTC time.
            TimeSpan remaining = completesUtc - utcNow.ToUniversalTime();
            return Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        }

        public static int GetTrainingFacilityUpgradeRemainingSeconds(SaveGameData data, DateTime utcNow)
        {
            // No active training timer should display remaining time.
            if (data == null || !data.trainingFacilityUpgradeInProgress)
            {
                return 0;
            }

            // Corrupted timer data cannot display a meaningful countdown, so repair and show zero.
            if (!TryGetUpgradeCompletionUtc(data.trainingFacilityUpgradeStartedUtcTicks, data.trainingFacilityUpgradeDurationSeconds, out DateTime completesUtc))
            {
                data.ClearTrainingFacilityUpgrade();
                return 0;
            }

            // Compare the valid completion timestamp with the current UTC time.
            TimeSpan remaining = completesUtc - utcNow.ToUniversalTime();
            return Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        }

        public static int GetLivingQuartersUpgradeRemainingSeconds(SaveGameData data, DateTime utcNow)
        {
            // No active living-quarters timer should display remaining time.
            if (data == null || !data.livingQuartersUpgradeInProgress)
            {
                return 0;
            }

            // Corrupted timer data cannot display a meaningful countdown, so repair and show zero.
            if (!TryGetUpgradeCompletionUtc(data.livingQuartersUpgradeStartedUtcTicks, data.livingQuartersUpgradeDurationSeconds, out DateTime completesUtc))
            {
                data.ClearLivingQuartersUpgrade();
                return 0;
            }

            // Compare the valid completion timestamp with the current UTC time.
            TimeSpan remaining = completesUtc - utcNow.ToUniversalTime();
            return Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        }

        public static float GetBioLabUpgradeProgress01(SaveGameData data, DateTime utcNow)
        {
            // Inactive or invalid timers have no visible circular progress fill.
            if (data == null || !data.bioLabUpgradeInProgress || data.bioLabUpgradeDurationSeconds <= 0)
            {
                return 0f;
            }

            // Corrupted timer data cannot produce a meaningful fill, so repair and show empty progress.
            if (!TryGetBioLabUpgradeCompletionUtc(data, out _))
            {
                data.ClearBioLabUpgrade();
                return 0f;
            }

            // Reconstructing after validation keeps the precise elapsed fill safe from out-of-range ticks.
            DateTime startedUtc = new(data.bioLabUpgradeStartedUtcTicks, DateTimeKind.Utc);

            // Use fractional seconds so the progress icon fills smoothly during short early upgrades.
            double elapsedSeconds = (utcNow.ToUniversalTime() - startedUtc).TotalSeconds;
            return Mathf.Clamp01((float)(elapsedSeconds / data.bioLabUpgradeDurationSeconds));
        }

        public static float GetHangarUpgradeProgress01(SaveGameData data, DateTime utcNow)
        {
            // Inactive or invalid timers have no visible circular progress fill.
            if (data == null || !data.hangarUpgradeInProgress || data.hangarUpgradeDurationSeconds <= 0)
            {
                return 0f;
            }

            // Corrupted timer data cannot produce a meaningful fill, so repair and show empty progress.
            if (!TryGetUpgradeCompletionUtc(data.hangarUpgradeStartedUtcTicks, data.hangarUpgradeDurationSeconds, out _))
            {
                data.ClearHangarUpgrade();
                return 0f;
            }

            // Reconstructing after validation keeps the precise elapsed fill safe from out-of-range ticks.
            DateTime startedUtc = new(data.hangarUpgradeStartedUtcTicks, DateTimeKind.Utc);

            // Use fractional seconds so the progress icon fills smoothly during short early upgrades.
            double elapsedSeconds = (utcNow.ToUniversalTime() - startedUtc).TotalSeconds;
            return Mathf.Clamp01((float)(elapsedSeconds / data.hangarUpgradeDurationSeconds));
        }

        public static float GetTrainingFacilityUpgradeProgress01(SaveGameData data, DateTime utcNow)
        {
            // Inactive or invalid timers have no visible circular progress fill.
            if (data == null || !data.trainingFacilityUpgradeInProgress || data.trainingFacilityUpgradeDurationSeconds <= 0)
            {
                return 0f;
            }

            // Corrupted timer data cannot produce a meaningful fill, so repair and show empty progress.
            if (!TryGetUpgradeCompletionUtc(data.trainingFacilityUpgradeStartedUtcTicks, data.trainingFacilityUpgradeDurationSeconds, out _))
            {
                data.ClearTrainingFacilityUpgrade();
                return 0f;
            }

            // Reconstructing after validation keeps the precise elapsed fill safe from out-of-range ticks.
            DateTime startedUtc = new(data.trainingFacilityUpgradeStartedUtcTicks, DateTimeKind.Utc);

            // Use fractional seconds so the progress icon fills smoothly during short early upgrades.
            double elapsedSeconds = (utcNow.ToUniversalTime() - startedUtc).TotalSeconds;
            return Mathf.Clamp01((float)(elapsedSeconds / data.trainingFacilityUpgradeDurationSeconds));
        }

        public static float GetLivingQuartersUpgradeProgress01(SaveGameData data, DateTime utcNow)
        {
            // Inactive or invalid timers have no visible circular progress fill.
            if (data == null || !data.livingQuartersUpgradeInProgress || data.livingQuartersUpgradeDurationSeconds <= 0)
            {
                return 0f;
            }

            // Corrupted timer data cannot produce a meaningful fill, so repair and show empty progress.
            if (!TryGetUpgradeCompletionUtc(data.livingQuartersUpgradeStartedUtcTicks, data.livingQuartersUpgradeDurationSeconds, out _))
            {
                data.ClearLivingQuartersUpgrade();
                return 0f;
            }

            // Reconstructing after validation keeps the precise elapsed fill safe from out-of-range ticks.
            DateTime startedUtc = new(data.livingQuartersUpgradeStartedUtcTicks, DateTimeKind.Utc);

            // Use fractional seconds so the progress icon fills smoothly during short early upgrades.
            double elapsedSeconds = (utcNow.ToUniversalTime() - startedUtc).TotalSeconds;
            return Mathf.Clamp01((float)(elapsedSeconds / data.livingQuartersUpgradeDurationSeconds));
        }

        private static bool TryMarkMissionCompleted(SaveGameData data, int missionLevel)
        {
            // Normalize guarantees the list exists before adding the newly completed mission.
            data.completedMissionLevels ??= new System.Collections.Generic.List<int>();
            if (data.completedMissionLevels.Contains(missionLevel))
            {
                return false;
            }

            // Store the mission id immediately so callers can inspect the save before it is persisted.
            data.completedMissionLevels.Add(missionLevel);
            data.completedMissionLevels.Sort();
            return true;
        }

        private static bool IsBioLabUpgradeComplete(SaveGameData data, DateTime utcNow)
        {
            // Inactive lab timers cannot be complete.
            if (data == null || !data.bioLabUpgradeInProgress)
            {
                return false;
            }

            // Corrupted timer data should be repaired instead of treated as a completed upgrade.
            if (!TryGetBioLabUpgradeCompletionUtc(data, out DateTime completesUtc))
            {
                data.ClearBioLabUpgrade();
                return false;
            }

            // A completion timestamp at or before now means the upgrade has finished.
            return completesUtc <= utcNow.ToUniversalTime();
        }

        private static bool IsHangarUpgradeComplete(SaveGameData data, DateTime utcNow)
        {
            // Inactive hangar timers cannot be complete.
            if (data == null || !data.hangarUpgradeInProgress)
            {
                return false;
            }

            // Corrupted timer data should be repaired instead of treated as a completed upgrade.
            if (!TryGetUpgradeCompletionUtc(data.hangarUpgradeStartedUtcTicks, data.hangarUpgradeDurationSeconds, out DateTime completesUtc))
            {
                data.ClearHangarUpgrade();
                return false;
            }

            // A completion timestamp at or before now means the upgrade has finished.
            return completesUtc <= utcNow.ToUniversalTime();
        }

        private static bool IsTrainingFacilityUpgradeComplete(SaveGameData data, DateTime utcNow)
        {
            // Inactive training timers cannot be complete.
            if (data == null || !data.trainingFacilityUpgradeInProgress)
            {
                return false;
            }

            // Corrupted timer data should be repaired instead of treated as a completed upgrade.
            if (!TryGetUpgradeCompletionUtc(data.trainingFacilityUpgradeStartedUtcTicks, data.trainingFacilityUpgradeDurationSeconds, out DateTime completesUtc))
            {
                data.ClearTrainingFacilityUpgrade();
                return false;
            }

            // A completion timestamp at or before now means the upgrade has finished.
            return completesUtc <= utcNow.ToUniversalTime();
        }

        private static bool IsLivingQuartersUpgradeComplete(SaveGameData data, DateTime utcNow)
        {
            // Inactive living-quarters timers cannot be complete.
            if (data == null || !data.livingQuartersUpgradeInProgress)
            {
                return false;
            }

            // Corrupted timer data should be repaired instead of treated as a completed upgrade.
            if (!TryGetUpgradeCompletionUtc(data.livingQuartersUpgradeStartedUtcTicks, data.livingQuartersUpgradeDurationSeconds, out DateTime completesUtc))
            {
                data.ClearLivingQuartersUpgrade();
                return false;
            }

            // A completion timestamp at or before now means the upgrade has finished.
            return completesUtc <= utcNow.ToUniversalTime();
        }

        private static bool TryGetBioLabUpgradeCompletionUtc(SaveGameData data, out DateTime completesUtc)
        {
            // Default the out value so callers never observe an unassigned DateTime.
            completesUtc = default;

            // Delegate the actual tick math to the shared facility timer validator.
            return TryGetUpgradeCompletionUtc(data.bioLabUpgradeStartedUtcTicks, data.bioLabUpgradeDurationSeconds, out completesUtc);
        }

        private static bool TryGetUpgradeCompletionUtc(long startedUtcTicks, int durationSeconds, out DateTime completesUtc)
        {
            // Default the out value so callers never observe an unassigned DateTime.
            completesUtc = default;

            // A missing or negative start time cannot represent a recoverable UTC DateTime.
            if (startedUtcTicks <= DateTime.MinValue.Ticks)
            {
                return false;
            }

            // Ticks beyond DateTime's maximum would throw if used to build a DateTime.
            if (startedUtcTicks > DateTime.MaxValue.Ticks)
            {
                return false;
            }

            // Active timers created by gameplay always have a positive duration.
            if (durationSeconds <= 0)
            {
                return false;
            }

            // Convert duration seconds to ticks before adding so we can avoid AddSeconds overflow.
            long durationTicks = (long)durationSeconds * TimeSpan.TicksPerSecond;

            // A timer that completes beyond DateTime's maximum cannot be evaluated safely.
            if (startedUtcTicks > DateTime.MaxValue.Ticks - durationTicks)
            {
                return false;
            }

            // Reconstruct only after range checks have proven the saved values are safe.
            DateTime startedUtc = new(startedUtcTicks, DateTimeKind.Utc);

            // Add ticks instead of seconds because the overflow has already been checked above.
            completesUtc = startedUtc.AddTicks(durationTicks);
            return true;
        }
    }

    public readonly struct MissionCompletionResult
    {
        public MissionCompletionResult(int completedMissionLevel, int unlockedMissionLevel, bool unlockedNewMission, bool completedFirstTime)
        {
            // Store the completed mission so reward UI can describe the result without re-reading mutable save data.
            this.completedMissionLevel = completedMissionLevel;

            // Zero means no new mission was unlocked by this completion.
            this.unlockedMissionLevel = unlockedMissionLevel;

            // A separate boolean keeps the reward text clear when unlockedMissionLevel is the default zero value.
            this.unlockedNewMission = unlockedNewMission;

            // The Base mission panel uses this indirectly to distinguish first clears from replays.
            this.completedFirstTime = completedFirstTime;
        }

        public readonly int completedMissionLevel;

        public readonly int unlockedMissionLevel;

        public readonly bool unlockedNewMission;

        public readonly bool completedFirstTime;
    }
}
