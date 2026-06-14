using System;
using LaneSurvivor.Save;

namespace LaneSurvivor.Progression
{
    public static class UpgradeTimer
    {
        public static void Start(SaveGameData data, DateTime utcNow, int durationSeconds)
        {
            // Store timestamps in UTC ticks so timers persist across app restarts and time zones.
            data.hqUpgradeInProgress = true;
            data.hqUpgradeStartedUtcTicks = utcNow.ToUniversalTime().Ticks;
            data.hqUpgradeDurationSeconds = Math.Max(1, durationSeconds);
        }

        public static bool IsComplete(SaveGameData data, DateTime utcNow)
        {
            // Inactive timers cannot be complete.
            if (data == null || !data.hqUpgradeInProgress)
            {
                return false;
            }

            // Corrupted timer data should be repaired instead of treated as a completed upgrade.
            if (!TryGetCompletionUtc(data, out DateTime completesUtc))
            {
                data.ClearHqUpgrade();
                return false;
            }

            // A completion timestamp at or before now means the upgrade has finished.
            return completesUtc <= utcNow.ToUniversalTime();
        }

        public static int GetRemainingSeconds(SaveGameData data, DateTime utcNow)
        {
            // No active timer should display no remaining time.
            if (data == null || !data.hqUpgradeInProgress)
            {
                return 0;
            }

            // Corrupted timer data cannot display a meaningful countdown, so repair and show zero.
            if (!TryGetCompletionUtc(data, out DateTime completesUtc))
            {
                data.ClearHqUpgrade();
                return 0;
            }

            // Compare the valid completion timestamp with the current UTC time.
            TimeSpan remaining = completesUtc - utcNow.ToUniversalTime();
            return Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        }

        public static float GetProgress01(SaveGameData data, DateTime utcNow)
        {
            // Inactive or invalid timers have no visible circular progress fill.
            if (data == null || !data.hqUpgradeInProgress || data.hqUpgradeDurationSeconds <= 0)
            {
                return 0f;
            }

            // Corrupted timer data cannot produce a meaningful fill, so repair and show empty progress.
            if (!TryGetCompletionUtc(data, out _))
            {
                data.ClearHqUpgrade();
                return 0f;
            }

            // Reconstructing after validation keeps the precise elapsed fill safe from out-of-range ticks.
            DateTime startedUtc = new(data.hqUpgradeStartedUtcTicks, DateTimeKind.Utc);

            // Use fractional seconds so the progress icon fills smoothly during the local HQ timer.
            double elapsedSeconds = (utcNow.ToUniversalTime() - startedUtc).TotalSeconds;
            double progress = elapsedSeconds / data.hqUpgradeDurationSeconds;
            if (progress <= 0d)
            {
                return 0f;
            }

            if (progress >= 1d)
            {
                return 1f;
            }

            return (float)progress;
        }

        private static bool TryGetCompletionUtc(SaveGameData data, out DateTime completesUtc)
        {
            // Default the out value so callers never observe an unassigned DateTime.
            completesUtc = default;

            // A missing or negative start time cannot represent a recoverable UTC DateTime.
            if (data.hqUpgradeStartedUtcTicks <= DateTime.MinValue.Ticks)
            {
                return false;
            }

            // Ticks beyond DateTime's maximum would throw if used to build a DateTime.
            if (data.hqUpgradeStartedUtcTicks > DateTime.MaxValue.Ticks)
            {
                return false;
            }

            // Active timers created by gameplay always have a positive duration.
            if (data.hqUpgradeDurationSeconds <= 0)
            {
                return false;
            }

            // Convert duration seconds to ticks before adding so we can avoid AddSeconds overflow.
            long durationTicks = (long)data.hqUpgradeDurationSeconds * TimeSpan.TicksPerSecond;

            // A timer that completes beyond DateTime's maximum cannot be evaluated safely.
            if (data.hqUpgradeStartedUtcTicks > DateTime.MaxValue.Ticks - durationTicks)
            {
                return false;
            }

            // Reconstruct only after range checks have proven the saved values are safe.
            DateTime startedUtc = new(data.hqUpgradeStartedUtcTicks, DateTimeKind.Utc);

            // Add ticks instead of seconds because the overflow has already been checked above.
            completesUtc = startedUtc.AddTicks(durationTicks);
            return true;
        }
    }
}
