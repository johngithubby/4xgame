using System;
using System.Collections.Generic;
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

        public List<string> ownedHeroIds = new();

        public string equippedHeroId = string.Empty;

        public void Normalize()
        {
            // Clamp save values after load so corrupt or older data cannot break gameplay assumptions.
            coins = Mathf.Max(0, coins);
            hqLevel = Mathf.Max(1, hqLevel);
            unlockedMinigameLevel = Mathf.Max(1, unlockedMinigameLevel);
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

            // Equipped heroes must also be owned; otherwise clear the invalid equipped id.
            if (string.IsNullOrWhiteSpace(equippedHeroId) || !ownedHeroIds.Contains(equippedHeroId))
            {
                equippedHeroId = string.Empty;
            }
        }
    }
}
