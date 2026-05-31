using System;
using System.IO;
using LaneSurvivor.Progression;
using LaneSurvivor.Save;
using NUnit.Framework;

namespace LaneSurvivor.Tests.EditMode
{
    public sealed class PhaseTwoProgressionTests
    {
        private string tempSavePath;

        [TearDown]
        public void TearDown()
        {
            // Reset the save manager so test paths never leak into other test fixtures.
            SaveGameManager.ClearCustomSavePathForTests();

            // Remove temp save files created by persistence tests.
            if (!string.IsNullOrEmpty(tempSavePath) && File.Exists(tempSavePath))
            {
                File.Delete(tempSavePath);
            }

            // Remove any temp write file left behind if a persistence assertion fails midway.
            if (!string.IsNullOrEmpty(tempSavePath) && File.Exists($"{tempSavePath}.tmp"))
            {
                File.Delete($"{tempSavePath}.tmp");
            }
        }

        [Test]
        public void CollectCoins_AddsPrototypeCollectAmount()
        {
            SaveGameData saveData = new();

            PlayerProgression.CollectCoins(saveData);

            Assert.AreEqual(PlayerProgression.CoinsPerCollect, saveData.coins);
        }

        [Test]
        public void MinigameWinReward_AddsCoinsOnlyOncePerRun()
        {
            // Start with existing coins so the test proves rewards add to, rather than replace, save data.
            SaveGameData saveData = new()
            {
                coins = 10
            };

            // The run-scoped flag starts false for a fresh minigame attempt.
            bool rewardClaimed = false;

            // Claiming twice simulates duplicate win-state calls in one run.
            int firstReward = PlayerProgression.TryClaimMinigameWinReward(saveData, ref rewardClaimed);
            int secondReward = PlayerProgression.TryClaimMinigameWinReward(saveData, ref rewardClaimed);

            // Only the first claim should mutate coins and report a reward amount.
            Assert.AreEqual(PlayerProgression.MinigameWinCoins, firstReward);
            Assert.AreEqual(0, secondReward);
            Assert.AreEqual(10 + PlayerProgression.MinigameWinCoins, saveData.coins);
            Assert.IsTrue(rewardClaimed);
        }

        [Test]
        public void StartHqUpgrade_SpendsCoinsAndStartsTimer()
        {
            SaveGameData saveData = new()
            {
                coins = 100,
                hqLevel = 1
            };
            DateTime now = new(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc);

            bool started = PlayerProgression.TryStartHqUpgrade(saveData, now);

            Assert.IsTrue(started);
            Assert.AreEqual(25, saveData.coins);
            Assert.IsTrue(saveData.hqUpgradeInProgress);
            Assert.AreEqual(PlayerProgression.HqUpgradeDurationSeconds, saveData.hqUpgradeDurationSeconds);
            Assert.AreEqual(PlayerProgression.HqUpgradeDurationSeconds, PlayerProgression.GetHqUpgradeRemainingSeconds(saveData, now));
        }

        [Test]
        public void StartHqUpgrade_FailsWhenCoinsAreInsufficient()
        {
            SaveGameData saveData = new()
            {
                coins = 10,
                hqLevel = 1
            };

            bool started = PlayerProgression.TryStartHqUpgrade(saveData, DateTime.UtcNow);

            Assert.IsFalse(started);
            Assert.AreEqual(10, saveData.coins);
            Assert.IsFalse(saveData.hqUpgradeInProgress);
        }

        [Test]
        public void CompleteReadyHqUpgrade_IncreasesLevelAndClearsTimer()
        {
            SaveGameData saveData = new()
            {
                coins = 100,
                hqLevel = 1
            };
            DateTime now = new(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc);

            PlayerProgression.TryStartHqUpgrade(saveData, now);
            bool completed = PlayerProgression.CompleteReadyHqUpgrade(saveData, now.AddSeconds(PlayerProgression.HqUpgradeDurationSeconds + 1));

            Assert.IsTrue(completed);
            Assert.AreEqual(2, saveData.hqLevel);
            Assert.AreEqual(2, saveData.unlockedMinigameLevel);
            Assert.IsFalse(saveData.hqUpgradeInProgress);
            Assert.AreEqual(1, PlayerProgression.GetStartingSquadBonus(saveData));
        }

        [Test]
        public void CompleteReadyHqUpgrade_RepairsOutOfRangeTimerTicks()
        {
            SaveGameData saveData = new()
            {
                hqLevel = 1,
                hqUpgradeInProgress = true,
                hqUpgradeStartedUtcTicks = long.MaxValue,
                hqUpgradeDurationSeconds = 20
            };

            bool completed = PlayerProgression.CompleteReadyHqUpgrade(saveData, DateTime.UtcNow);

            Assert.IsFalse(completed);
            Assert.AreEqual(1, saveData.hqLevel);
            Assert.IsFalse(saveData.hqUpgradeInProgress);
            Assert.AreEqual(0, saveData.hqUpgradeStartedUtcTicks);
            Assert.AreEqual(0, saveData.hqUpgradeDurationSeconds);
        }

        [Test]
        public void CompleteReadyHqUpgrade_RepairsZeroDurationTimerWithoutLeveling()
        {
            SaveGameData saveData = new()
            {
                hqLevel = 1,
                hqUpgradeInProgress = true,
                hqUpgradeStartedUtcTicks = new DateTime(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc).Ticks,
                hqUpgradeDurationSeconds = 0
            };

            bool completed = PlayerProgression.CompleteReadyHqUpgrade(saveData, new DateTime(2026, 5, 30, 12, 0, 1, DateTimeKind.Utc));

            Assert.IsFalse(completed);
            Assert.AreEqual(1, saveData.hqLevel);
            Assert.IsFalse(saveData.hqUpgradeInProgress);
            Assert.AreEqual(0, saveData.hqUpgradeStartedUtcTicks);
            Assert.AreEqual(0, saveData.hqUpgradeDurationSeconds);
        }

        [Test]
        public void Normalize_ClearsTimerThatWouldOverflowCompletionTime()
        {
            SaveGameData saveData = new()
            {
                hqLevel = 1,
                hqUpgradeInProgress = true,
                hqUpgradeStartedUtcTicks = DateTime.MaxValue.Ticks - TimeSpan.TicksPerSecond,
                hqUpgradeDurationSeconds = 20
            };

            saveData.Normalize();

            Assert.IsFalse(saveData.hqUpgradeInProgress);
            Assert.AreEqual(0, saveData.hqUpgradeStartedUtcTicks);
            Assert.AreEqual(0, saveData.hqUpgradeDurationSeconds);
        }

        [Test]
        public void SaveGameManager_PreservesLocalProgress()
        {
            tempSavePath = Path.Combine(Path.GetTempPath(), $"lane-survivor-save-{Guid.NewGuid():N}.json");
            SaveGameManager.UseCustomSavePathForTests(tempSavePath);
            SaveGameData saveData = new()
            {
                coins = 125,
                hqLevel = 3,
                hqUpgradeInProgress = true,
                hqUpgradeStartedUtcTicks = new DateTime(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc).Ticks,
                hqUpgradeDurationSeconds = 20,
                unlockedMinigameLevel = 3
            };

            SaveGameManager.Save(saveData);
            SaveGameData loadedData = SaveGameManager.Load();

            Assert.AreEqual(125, loadedData.coins);
            Assert.AreEqual(3, loadedData.hqLevel);
            Assert.IsTrue(loadedData.hqUpgradeInProgress);
            Assert.AreEqual(saveData.hqUpgradeStartedUtcTicks, loadedData.hqUpgradeStartedUtcTicks);
            Assert.AreEqual(20, loadedData.hqUpgradeDurationSeconds);
            Assert.AreEqual(3, loadedData.unlockedMinigameLevel);
        }

        [Test]
        public void SaveGameManager_RepairsMalformedSaveToFreshDefaults()
        {
            tempSavePath = Path.Combine(Path.GetTempPath(), $"lane-survivor-save-{Guid.NewGuid():N}.json");
            SaveGameManager.UseCustomSavePathForTests(tempSavePath);
            File.WriteAllText(tempSavePath, "{ not valid json");

            SaveGameData loadedData = SaveGameManager.Load();

            Assert.AreEqual(0, loadedData.coins);
            Assert.AreEqual(1, loadedData.hqLevel);
            Assert.IsFalse(loadedData.hqUpgradeInProgress);
            Assert.AreEqual(1, loadedData.unlockedMinigameLevel);
        }

        [Test]
        public void SaveGameManager_ResetToFreshData_PersistsDefaultProgress()
        {
            // Use an isolated path so reset never touches the real local prototype save.
            tempSavePath = Path.Combine(Path.GetTempPath(), $"lane-survivor-save-{Guid.NewGuid():N}.json");
            SaveGameManager.UseCustomSavePathForTests(tempSavePath);

            // Seed non-default progress so the reset has visible state to replace.
            SaveGameManager.Save(new SaveGameData
            {
                coins = 250,
                hqLevel = 4,
                unlockedMinigameLevel = 4
            });

            // Reset writes fresh data immediately, and a later load should read the same defaults.
            SaveGameData resetData = SaveGameManager.ResetToFreshData();
            SaveGameData loadedData = SaveGameManager.Load();

            // Both the returned object and persisted file should match first-launch progress.
            Assert.AreEqual(0, resetData.coins);
            Assert.AreEqual(1, resetData.hqLevel);
            Assert.AreEqual(1, resetData.unlockedMinigameLevel);
            Assert.AreEqual(0, loadedData.coins);
            Assert.AreEqual(1, loadedData.hqLevel);
            Assert.AreEqual(1, loadedData.unlockedMinigameLevel);
        }
    }
}
