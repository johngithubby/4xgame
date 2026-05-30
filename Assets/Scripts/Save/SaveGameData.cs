using System;
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

        public void Normalize()
        {
            // Clamp save values after load so corrupt or older data cannot break gameplay assumptions.
            coins = Mathf.Max(0, coins);
            hqLevel = Mathf.Max(1, hqLevel);
            unlockedMinigameLevel = Mathf.Max(1, unlockedMinigameLevel);

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
    }
}
