using System;
using System.Collections.Generic;
using LaneSurvivor.Heroes;
using LaneSurvivor.Progression;
using LaneSurvivor.Retention;
using UnityEngine;

namespace LaneSurvivor.Save
{
    [Serializable]
    public sealed class SaveGameData
    {
        public int coins = 0;

        public int hqLevel = 1;

        public bool hqUpgradeInProgress;

        public long hqUpgradeStartedUtcTicks;

        public int hqUpgradeDurationSeconds;

        public int bioLabLevel = 1;

        public bool bioLabUpgradeInProgress;

        public long bioLabUpgradeStartedUtcTicks;

        public int bioLabUpgradeDurationSeconds;

        public int hangarLevel = 1;

        public bool hangarUpgradeInProgress;

        public long hangarUpgradeStartedUtcTicks;

        public int hangarUpgradeDurationSeconds;

        public int trainingFacilityLevel = 1;

        public bool trainingFacilityUpgradeInProgress;

        public long trainingFacilityUpgradeStartedUtcTicks;

        public int trainingFacilityUpgradeDurationSeconds;

        public int livingQuartersLevel = 1;

        public bool livingQuartersUpgradeInProgress;

        public long livingQuartersUpgradeStartedUtcTicks;

        public int livingQuartersUpgradeDurationSeconds;

        public int unlockedMinigameLevel = 1;

        public int currentMissionLevel = 1;

        public int highestUnlockedMissionLevel = 1;

        public List<int> completedMissionLevels = new();

        public List<string> ownedHeroIds = new();

        public string equippedHeroId = string.Empty;

        public List<HeroProgressData> heroProgress = new();

        public int dailyObjectiveUtcDayNumber;

        public int dailyObjectiveWins;

        public bool dailyObjectiveRewardClaimed;

        public void Normalize()
        {
            // Clamp save values after load so corrupt or older data cannot break gameplay assumptions.
            coins = Mathf.Max(0, coins);
            hqLevel = Mathf.Max(1, hqLevel);
            bioLabLevel = Mathf.Max(1, bioLabLevel);
            hangarLevel = Mathf.Max(1, hangarLevel);
            trainingFacilityLevel = Mathf.Max(1, trainingFacilityLevel);
            livingQuartersLevel = Mathf.Max(1, livingQuartersLevel);
            NormalizeMissionProgression();
            NormalizeHeroes();
            NormalizeDailyObjective();

            // Invalid persisted timer values cannot be recovered safely, so clear the active timer.
            if (hqUpgradeInProgress && !HasRecoverableHqUpgradeTimer())
            {
                ClearHqUpgrade();
            }

            // Inactive saves do not need a duration, so clamp old negative values down to zero.
            hqUpgradeDurationSeconds = Mathf.Max(0, hqUpgradeDurationSeconds);

            // Invalid persisted bio-lab timer values cannot be recovered safely, so clear that active timer.
            if (bioLabUpgradeInProgress && !HasRecoverableBioLabUpgradeTimer())
            {
                ClearBioLabUpgrade();
            }

            // Inactive bio-lab saves do not need a duration, so clamp old negative values down to zero.
            bioLabUpgradeDurationSeconds = Mathf.Max(0, bioLabUpgradeDurationSeconds);

            // Invalid persisted hangar timer values cannot be recovered safely, so clear that active timer.
            if (hangarUpgradeInProgress && !HasRecoverableHangarUpgradeTimer())
            {
                ClearHangarUpgrade();
            }

            // Inactive hangar saves do not need a duration, so clamp old negative values down to zero.
            hangarUpgradeDurationSeconds = Mathf.Max(0, hangarUpgradeDurationSeconds);

            // Invalid persisted training timer values cannot be recovered safely, so clear that active timer.
            if (trainingFacilityUpgradeInProgress && !HasRecoverableTrainingFacilityUpgradeTimer())
            {
                ClearTrainingFacilityUpgrade();
            }

            // Inactive training saves do not need a duration, so clamp old negative values down to zero.
            trainingFacilityUpgradeDurationSeconds = Mathf.Max(0, trainingFacilityUpgradeDurationSeconds);

            // Invalid persisted living-quarters timer values cannot be recovered safely, so clear that active timer.
            if (livingQuartersUpgradeInProgress && !HasRecoverableLivingQuartersUpgradeTimer())
            {
                ClearLivingQuartersUpgrade();
            }

            // Inactive living-quarters saves do not need a duration, so clamp old negative values down to zero.
            livingQuartersUpgradeDurationSeconds = Mathf.Max(0, livingQuartersUpgradeDurationSeconds);
        }

        public void ClearHqUpgrade()
        {
            // Keep timer reset logic in one place so completion and data repair clear the same fields.
            hqUpgradeInProgress = false;
            hqUpgradeStartedUtcTicks = 0L;
            hqUpgradeDurationSeconds = 0;
        }

        public void ClearBioLabUpgrade()
        {
            // Keep bio-lab timer reset logic in one place so completion and data repair clear the same fields.
            bioLabUpgradeInProgress = false;
            bioLabUpgradeStartedUtcTicks = 0L;
            bioLabUpgradeDurationSeconds = 0;
        }

        public void ClearHangarUpgrade()
        {
            // Keep hangar timer reset logic in one place so completion and data repair clear the same fields.
            hangarUpgradeInProgress = false;
            hangarUpgradeStartedUtcTicks = 0L;
            hangarUpgradeDurationSeconds = 0;
        }

        public void ClearTrainingFacilityUpgrade()
        {
            // Keep training timer reset logic in one place so completion and data repair clear the same fields.
            trainingFacilityUpgradeInProgress = false;
            trainingFacilityUpgradeStartedUtcTicks = 0L;
            trainingFacilityUpgradeDurationSeconds = 0;
        }

        public void ClearLivingQuartersUpgrade()
        {
            // Keep living-quarters timer reset logic in one place so completion and data repair clear the same fields.
            livingQuartersUpgradeInProgress = false;
            livingQuartersUpgradeStartedUtcTicks = 0L;
            livingQuartersUpgradeDurationSeconds = 0;
        }

        public SaveGameData Clone()
        {
            // JsonUtility gives a compact deep copy for this simple serializable save object.
            return JsonUtility.FromJson<SaveGameData>(JsonUtility.ToJson(this));
        }

        private bool HasRecoverableHqUpgradeTimer()
        {
            // HQ timers use the shared UTC tick validation helper so save repair matches progression checks.
            return HasRecoverableUpgradeTimer(hqUpgradeStartedUtcTicks, hqUpgradeDurationSeconds);
        }

        private bool HasRecoverableBioLabUpgradeTimer()
        {
            // Bio-lab timers use the same UTC tick validation rules as HQ timers.
            return HasRecoverableUpgradeTimer(bioLabUpgradeStartedUtcTicks, bioLabUpgradeDurationSeconds);
        }

        private bool HasRecoverableHangarUpgradeTimer()
        {
            // Hangar timers use the same UTC tick validation rules as the other local buildings.
            return HasRecoverableUpgradeTimer(hangarUpgradeStartedUtcTicks, hangarUpgradeDurationSeconds);
        }

        private bool HasRecoverableTrainingFacilityUpgradeTimer()
        {
            // Training timers use the same UTC tick validation rules as the other local buildings.
            return HasRecoverableUpgradeTimer(trainingFacilityUpgradeStartedUtcTicks, trainingFacilityUpgradeDurationSeconds);
        }

        private bool HasRecoverableLivingQuartersUpgradeTimer()
        {
            // Living-quarters timers use the same UTC tick validation rules as the other local buildings.
            return HasRecoverableUpgradeTimer(livingQuartersUpgradeStartedUtcTicks, livingQuartersUpgradeDurationSeconds);
        }

        private static bool HasRecoverableUpgradeTimer(long startedUtcTicks, int durationSeconds)
        {
            // A missing or negative start time cannot represent a real UTC DateTime.
            if (startedUtcTicks <= DateTime.MinValue.Ticks)
            {
                return false;
            }

            // Ticks beyond DateTime's maximum would throw when reconstructing the timestamp.
            if (startedUtcTicks > DateTime.MaxValue.Ticks)
            {
                return false;
            }

            // Active timers created by gameplay always have a positive duration.
            if (durationSeconds <= 0)
            {
                return false;
            }

            // Convert seconds to ticks with long arithmetic so the overflow check is explicit.
            long durationTicks = (long)durationSeconds * TimeSpan.TicksPerSecond;

            // The saved duration must fit inside DateTime's valid range from the saved start.
            return startedUtcTicks <= DateTime.MaxValue.Ticks - durationTicks;
        }

        private void NormalizeHeroes()
        {
            // Older saves do not have hero lists, so create one before inventory code reads it.
            ownedHeroIds ??= new List<string>();
            heroProgress ??= new List<HeroProgressData>();

            // Remove empty ids and duplicates so corrupted saves cannot display phantom hero entries.
            HashSet<string> seenHeroIds = new();
            for (int index = ownedHeroIds.Count - 1; index >= 0; index -= 1)
            {
                string heroId = ownedHeroIds[index];
                if (string.IsNullOrWhiteSpace(heroId) || !seenHeroIds.Add(heroId))
                {
                    ownedHeroIds.RemoveAt(index);
                }
            }

            // Keep hero progress aligned with the repaired ownership list.
            NormalizeHeroProgress();

            // Equipped heroes must also be owned; otherwise clear the invalid equipped id.
            if (string.IsNullOrWhiteSpace(equippedHeroId) || !ownedHeroIds.Contains(equippedHeroId))
            {
                equippedHeroId = string.Empty;
            }
        }

        private void NormalizeHeroProgress()
        {
            // Progress is meaningful only for currently owned hero ids.
            HashSet<string> ownedHeroIdSet = new(ownedHeroIds);

            // Track retained ids so duplicate progress entries from corrupt saves can be removed.
            HashSet<string> seenProgressIds = new();
            for (int index = heroProgress.Count - 1; index >= 0; index -= 1)
            {
                HeroProgressData progress = heroProgress[index];
                if (progress == null || string.IsNullOrWhiteSpace(progress.heroId) || !ownedHeroIdSet.Contains(progress.heroId) || !seenProgressIds.Add(progress.heroId))
                {
                    heroProgress.RemoveAt(index);
                    continue;
                }

                // Level one is the baseline, max level is enforced before stats read the save, and XP cannot be negative.
                progress.level = Mathf.Clamp(progress.level, 1, HeroProgression.MaxHeroLevel);
                progress.xp = Mathf.Max(0, progress.xp);
            }

            // Every owned hero should have a progress record so UI/stat code can read one shape.
            foreach (string heroId in ownedHeroIds)
            {
                if (!seenProgressIds.Contains(heroId))
                {
                    heroProgress.Add(new HeroProgressData
                    {
                        heroId = heroId,
                        level = 1,
                        xp = 0
                    });
                }
            }
        }

        private void NormalizeMissionProgression()
        {
            // Legacy saves used unlockedMinigameLevel as both unlock and selection, so keep that value as migration input.
            int legacyUnlockedMissionLevel = Mathf.Clamp(Mathf.Max(1, unlockedMinigameLevel), 1, PlayerProgression.MaxMissionLevel);

            // New saves track selected and highest-unlocked missions separately for the Base mission panel.
            int normalizedHighestMissionLevel = Mathf.Clamp(Mathf.Max(Mathf.Max(1, highestUnlockedMissionLevel), legacyUnlockedMissionLevel), 1, PlayerProgression.MaxMissionLevel);

            // If only the legacy field carries progress, preserve the old implicit behavior by selecting that mission.
            if (legacyUnlockedMissionLevel > highestUnlockedMissionLevel && currentMissionLevel <= 1)
            {
                currentMissionLevel = legacyUnlockedMissionLevel;
            }

            // The selected mission must always be unlocked and inside the authored local mission range.
            currentMissionLevel = Mathf.Clamp(Mathf.Max(1, currentMissionLevel), 1, normalizedHighestMissionLevel);
            highestUnlockedMissionLevel = normalizedHighestMissionLevel;

            // Keep the old field as a compatibility mirror for tests, docs, and older local JSON saves.
            unlockedMinigameLevel = highestUnlockedMissionLevel;

            // Older saves did not persist completion rows, so repair or create the list before mission UI reads it.
            NormalizeCompletedMissions();
        }

        private void NormalizeCompletedMissions()
        {
            // Unity JsonUtility can load missing list fields as null when older JSON is read.
            completedMissionLevels ??= new List<int>();

            // Mission unlocks are sequential, so every predecessor of the highest unlocked mission is complete.
            for (int missionLevel = 1; missionLevel < highestUnlockedMissionLevel; missionLevel += 1)
            {
                if (!completedMissionLevels.Contains(missionLevel))
                {
                    completedMissionLevels.Add(missionLevel);
                }
            }

            // Remove duplicates and unsupported ids so corrupted saves cannot show phantom mission rows as done.
            HashSet<int> seenMissionLevels = new();
            for (int index = completedMissionLevels.Count - 1; index >= 0; index -= 1)
            {
                int missionLevel = completedMissionLevels[index];
                if (missionLevel < 1 || missionLevel > highestUnlockedMissionLevel || missionLevel > PlayerProgression.MaxMissionLevel || !seenMissionLevels.Add(missionLevel))
                {
                    completedMissionLevels.RemoveAt(index);
                }
            }

            // Sorting keeps JSON diffs and mission panel status checks deterministic.
            completedMissionLevels.Sort();
        }

        private void NormalizeDailyObjective()
        {
            // A zero day means no local daily objective has been initialized on this save yet.
            dailyObjectiveUtcDayNumber = Mathf.Max(0, dailyObjectiveUtcDayNumber);

            // Wins are capped at the current objective requirement so corrupted saves cannot overfill the panel.
            dailyObjectiveWins = Mathf.Clamp(dailyObjectiveWins, 0, DailyObjectiveProgression.WinsRequired);

            // A claimed objective must also show completed progress, otherwise reset the impossible claim flag.
            if (dailyObjectiveRewardClaimed && dailyObjectiveWins < DailyObjectiveProgression.WinsRequired)
            {
                dailyObjectiveRewardClaimed = false;
            }
        }
    }

    [Serializable]
    public sealed class HeroProgressData
    {
        public string heroId;

        public int level = 1;

        public int xp;
    }
}
