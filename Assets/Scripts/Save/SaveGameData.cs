using System;
using System.Collections.Generic;
using LaneSurvivor.Heroes;
using LaneSurvivor.Progression;
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

        public int unlockedMinigameLevel = 1;

        public int currentMissionLevel = 1;

        public int highestUnlockedMissionLevel = 1;

        public List<string> ownedHeroIds = new();

        public string equippedHeroId = string.Empty;

        public List<HeroProgressData> heroProgress = new();

        public void Normalize()
        {
            // Clamp save values after load so corrupt or older data cannot break gameplay assumptions.
            coins = Mathf.Max(0, coins);
            hqLevel = Mathf.Max(1, hqLevel);
            NormalizeMissionProgression();
            NormalizeHeroes();

            // Invalid persisted timer values cannot be recovered safely, so clear the active timer.
            if (hqUpgradeInProgress && !HasRecoverableHqUpgradeTimer())
            {
                ClearHqUpgrade();
                return;
            }

            // Inactive saves do not need a duration, so clamp old negative values down to zero.
            hqUpgradeDurationSeconds = Mathf.Max(0, hqUpgradeDurationSeconds);
        }

        public void ClearHqUpgrade()
        {
            // Keep timer reset logic in one place so completion and data repair clear the same fields.
            hqUpgradeInProgress = false;
            hqUpgradeStartedUtcTicks = 0L;
            hqUpgradeDurationSeconds = 0;
        }

        public SaveGameData Clone()
        {
            // JsonUtility gives a compact deep copy for this simple serializable save object.
            return JsonUtility.FromJson<SaveGameData>(JsonUtility.ToJson(this));
        }

        private bool HasRecoverableHqUpgradeTimer()
        {
            // A missing or negative start time cannot represent a real UTC DateTime.
            if (hqUpgradeStartedUtcTicks <= DateTime.MinValue.Ticks)
            {
                return false;
            }

            // Ticks beyond DateTime's maximum would throw when reconstructing the timestamp.
            if (hqUpgradeStartedUtcTicks > DateTime.MaxValue.Ticks)
            {
                return false;
            }

            // Active timers created by gameplay always have a positive duration.
            if (hqUpgradeDurationSeconds <= 0)
            {
                return false;
            }

            // Convert seconds to ticks with long arithmetic so the overflow check is explicit.
            long durationTicks = (long)hqUpgradeDurationSeconds * TimeSpan.TicksPerSecond;

            // The saved duration must fit inside DateTime's valid range from the saved start.
            return hqUpgradeStartedUtcTicks <= DateTime.MaxValue.Ticks - durationTicks;
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
