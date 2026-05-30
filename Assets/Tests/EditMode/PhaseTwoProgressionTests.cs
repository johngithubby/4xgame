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
        }

        [Test]
        public void CollectCoins_AddsPrototypeCollectAmount()
        {
            SaveGameData saveData = new();

            PlayerProgression.CollectCoins(saveData);

            Assert.AreEqual(PlayerProgression.CoinsPerCollect, saveData.coins);
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
    }
}
