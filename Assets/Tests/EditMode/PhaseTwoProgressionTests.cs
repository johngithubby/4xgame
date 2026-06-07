using System;
using System.Collections.Generic;
using System.IO;
using LaneSurvivor.Progression;
using LaneSurvivor.Retention;
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
        public void DailyObjective_RecordsWinsAndClaimsRewardOnce()
        {
            SaveGameData saveData = new()
            {
                coins = 10
            };
            DateTime now = new(2026, 6, 7, 12, 0, 0, DateTimeKind.Utc);

            DailyObjectiveStatus firstWinStatus = DailyObjectiveProgression.RecordMinigameWin(saveData, now);
            DailyObjectiveStatus secondWinStatus = DailyObjectiveProgression.RecordMinigameWin(saveData, now);
            bool firstClaim = DailyObjectiveProgression.TryClaimReward(saveData, now);
            bool secondClaim = DailyObjectiveProgression.TryClaimReward(saveData, now);

            Assert.AreEqual(1, firstWinStatus.wins);
            Assert.IsFalse(firstWinStatus.canClaimReward);
            Assert.AreEqual(DailyObjectiveProgression.WinsRequired, secondWinStatus.wins);
            Assert.IsTrue(secondWinStatus.canClaimReward);
            Assert.IsTrue(firstClaim);
            Assert.IsFalse(secondClaim);
            Assert.AreEqual(10 + DailyObjectiveProgression.RewardCoins, saveData.coins);
            Assert.IsTrue(saveData.dailyObjectiveRewardClaimed);
        }

        [Test]
        public void DailyObjective_RollsOverOnNewUtcDay()
        {
            SaveGameData saveData = new();
            DateTime firstDay = new(2026, 6, 7, 23, 30, 0, DateTimeKind.Utc);
            DateTime nextDay = firstDay.AddHours(2);

            DailyObjectiveProgression.RecordMinigameWin(saveData, firstDay);
            saveData.dailyObjectiveRewardClaimed = true;
            bool rolledOver = DailyObjectiveProgression.EnsureCurrentObjective(saveData, nextDay);

            Assert.IsTrue(rolledOver);
            Assert.AreEqual(DailyObjectiveProgression.GetUtcDayNumber(nextDay), saveData.dailyObjectiveUtcDayNumber);
            Assert.AreEqual(0, saveData.dailyObjectiveWins);
            Assert.IsFalse(saveData.dailyObjectiveRewardClaimed);
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
            Assert.AreEqual(1, saveData.highestUnlockedMissionLevel);
            Assert.AreEqual(1, saveData.currentMissionLevel);
            Assert.AreEqual(1, saveData.unlockedMinigameLevel);
            Assert.IsFalse(saveData.hqUpgradeInProgress);
            Assert.AreEqual(1, PlayerProgression.GetStartingSquadBonus(saveData));
        }

        [Test]
        public void Normalize_DefaultsMissionProgressionToFirstMission()
        {
            SaveGameData saveData = new()
            {
                currentMissionLevel = -4,
                highestUnlockedMissionLevel = 0,
                unlockedMinigameLevel = 0
            };

            saveData.Normalize();

            Assert.AreEqual(1, saveData.currentMissionLevel);
            Assert.AreEqual(1, saveData.highestUnlockedMissionLevel);
            Assert.AreEqual(1, saveData.unlockedMinigameLevel);
            Assert.AreEqual(0, saveData.completedMissionLevels.Count);
        }

        [Test]
        public void Normalize_MigratesLegacyUnlockedMinigameLevelToMissionProgress()
        {
            SaveGameData saveData = new()
            {
                unlockedMinigameLevel = 3,
                currentMissionLevel = 1,
                highestUnlockedMissionLevel = 1
            };

            saveData.Normalize();

            Assert.AreEqual(3, saveData.currentMissionLevel);
            Assert.AreEqual(3, saveData.highestUnlockedMissionLevel);
            Assert.AreEqual(3, saveData.unlockedMinigameLevel);
            CollectionAssert.AreEqual(new[] { 1, 2 }, saveData.completedMissionLevels);
        }

        [Test]
        public void Normalize_RepairsCompletedMissionList()
        {
            SaveGameData saveData = new()
            {
                currentMissionLevel = 2,
                highestUnlockedMissionLevel = 3,
                unlockedMinigameLevel = 3,
                completedMissionLevels = new List<int> { 2, 99, 2, 0, 1 }
            };

            saveData.Normalize();

            Assert.AreEqual(2, saveData.currentMissionLevel);
            Assert.AreEqual(3, saveData.highestUnlockedMissionLevel);
            CollectionAssert.AreEqual(new[] { 1, 2 }, saveData.completedMissionLevels);
        }

        [Test]
        public void TrySelectMission_RejectsLockedMission()
        {
            SaveGameData saveData = new()
            {
                currentMissionLevel = 1,
                highestUnlockedMissionLevel = 1
            };

            bool selected = PlayerProgression.TrySelectMission(saveData, 2);

            Assert.IsFalse(selected);
            Assert.AreEqual(1, saveData.currentMissionLevel);
        }

        [Test]
        public void TrySelectMission_PersistsUnlockedMissionSelection()
        {
            SaveGameData saveData = new()
            {
                currentMissionLevel = 1,
                highestUnlockedMissionLevel = 3
            };

            bool selected = PlayerProgression.TrySelectMission(saveData, 2);

            Assert.IsTrue(selected);
            Assert.AreEqual(2, saveData.currentMissionLevel);
            Assert.AreEqual(3, saveData.highestUnlockedMissionLevel);
            Assert.AreEqual(3, saveData.unlockedMinigameLevel);
        }

        [Test]
        public void CompleteMission_UnlocksNextMissionOnceAndAutoSelectsIt()
        {
            SaveGameData saveData = new()
            {
                currentMissionLevel = 1,
                highestUnlockedMissionLevel = 1
            };

            MissionCompletionResult firstCompletion = PlayerProgression.TryCompleteMission(saveData, 1);
            MissionCompletionResult replayCompletion = PlayerProgression.TryCompleteMission(saveData, 1);

            Assert.IsTrue(firstCompletion.unlockedNewMission);
            Assert.AreEqual(2, firstCompletion.unlockedMissionLevel);
            Assert.IsTrue(firstCompletion.completedFirstTime);
            Assert.AreEqual(1, firstCompletion.completedMissionLevel);
            Assert.AreEqual(2, saveData.currentMissionLevel);
            Assert.AreEqual(2, saveData.highestUnlockedMissionLevel);
            Assert.AreEqual(2, saveData.unlockedMinigameLevel);
            CollectionAssert.AreEqual(new[] { 1 }, saveData.completedMissionLevels);
            Assert.IsFalse(replayCompletion.unlockedNewMission);
            Assert.IsFalse(replayCompletion.completedFirstTime);
            Assert.AreEqual(2, saveData.highestUnlockedMissionLevel);
            CollectionAssert.AreEqual(new[] { 1 }, saveData.completedMissionLevels);
        }

        [Test]
        public void CompleteMission_StopsAtAuthoredMissionCap()
        {
            SaveGameData saveData = new()
            {
                currentMissionLevel = PlayerProgression.MaxMissionLevel,
                highestUnlockedMissionLevel = PlayerProgression.MaxMissionLevel
            };

            MissionCompletionResult completion = PlayerProgression.TryCompleteMission(saveData, PlayerProgression.MaxMissionLevel);

            Assert.IsFalse(completion.unlockedNewMission);
            Assert.IsTrue(completion.completedFirstTime);
            Assert.AreEqual(PlayerProgression.MaxMissionLevel, saveData.currentMissionLevel);
            Assert.AreEqual(PlayerProgression.MaxMissionLevel, saveData.highestUnlockedMissionLevel);
            Assert.AreEqual(PlayerProgression.MaxMissionLevel, saveData.unlockedMinigameLevel);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }, saveData.completedMissionLevels);
        }

        [Test]
        public void MissionStatusAndRewardHints_DescribeSelectedDoneLockedAndCapRows()
        {
            SaveGameData saveData = new()
            {
                currentMissionLevel = 2,
                highestUnlockedMissionLevel = 2,
                completedMissionLevels = new List<int> { 1 }
            };

            Assert.AreEqual("DONE", PlayerProgression.GetMissionStatusLabel(saveData, 1));
            Assert.AreEqual("Replay: +50c", PlayerProgression.GetMissionRewardHint(saveData, 1));
            Assert.AreEqual("SELECTED", PlayerProgression.GetMissionStatusLabel(saveData, 2));
            Assert.AreEqual("Win: +50c + M3", PlayerProgression.GetMissionRewardHint(saveData, 2));
            Assert.AreEqual("LOCKED", PlayerProgression.GetMissionStatusLabel(saveData, 3));
            Assert.AreEqual("Unlock: clear M2", PlayerProgression.GetMissionRewardHint(saveData, 3));

            PlayerProgression.TryCompleteMission(saveData, 2);
            PlayerProgression.TrySelectMission(saveData, 2);

            Assert.AreEqual("DONE SELECTED", PlayerProgression.GetMissionStatusLabel(saveData, 2));
            Assert.AreEqual("Replay: +50c", PlayerProgression.GetMissionRewardHint(saveData, 2));

            SaveGameData cappedData = new()
            {
                currentMissionLevel = PlayerProgression.MaxMissionLevel,
                highestUnlockedMissionLevel = PlayerProgression.MaxMissionLevel,
                completedMissionLevels = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 }
            };

            Assert.AreEqual("DONE SELECTED", PlayerProgression.GetMissionStatusLabel(cappedData, PlayerProgression.MaxMissionLevel));
            Assert.AreEqual("Replay: +50c", PlayerProgression.GetMissionRewardHint(cappedData, PlayerProgression.MaxMissionLevel));
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
                unlockedMinigameLevel = 3,
                currentMissionLevel = 2,
                highestUnlockedMissionLevel = 3,
                completedMissionLevels = new List<int> { 1, 2 }
            };

            SaveGameManager.Save(saveData);
            SaveGameData loadedData = SaveGameManager.Load();

            Assert.AreEqual(125, loadedData.coins);
            Assert.AreEqual(3, loadedData.hqLevel);
            Assert.IsTrue(loadedData.hqUpgradeInProgress);
            Assert.AreEqual(saveData.hqUpgradeStartedUtcTicks, loadedData.hqUpgradeStartedUtcTicks);
            Assert.AreEqual(20, loadedData.hqUpgradeDurationSeconds);
            Assert.AreEqual(3, loadedData.unlockedMinigameLevel);
            Assert.AreEqual(2, loadedData.currentMissionLevel);
            Assert.AreEqual(3, loadedData.highestUnlockedMissionLevel);
            CollectionAssert.AreEqual(new[] { 1, 2 }, loadedData.completedMissionLevels);
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
            Assert.AreEqual(1, loadedData.currentMissionLevel);
            Assert.AreEqual(1, loadedData.highestUnlockedMissionLevel);
            Assert.AreEqual(0, loadedData.completedMissionLevels.Count);
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
                unlockedMinigameLevel = 4,
                currentMissionLevel = 3,
                highestUnlockedMissionLevel = 4,
                completedMissionLevels = new List<int> { 1, 2, 3 }
            });

            // Reset writes fresh data immediately, and a later load should read the same defaults.
            SaveGameData resetData = SaveGameManager.ResetToFreshData();
            SaveGameData loadedData = SaveGameManager.Load();

            // Both the returned object and persisted file should match first-launch progress.
            Assert.AreEqual(0, resetData.coins);
            Assert.AreEqual(1, resetData.hqLevel);
            Assert.AreEqual(1, resetData.unlockedMinigameLevel);
            Assert.AreEqual(1, resetData.currentMissionLevel);
            Assert.AreEqual(1, resetData.highestUnlockedMissionLevel);
            Assert.AreEqual(0, resetData.completedMissionLevels.Count);
            Assert.AreEqual(0, loadedData.coins);
            Assert.AreEqual(1, loadedData.hqLevel);
            Assert.AreEqual(1, loadedData.unlockedMinigameLevel);
            Assert.AreEqual(1, loadedData.currentMissionLevel);
            Assert.AreEqual(1, loadedData.highestUnlockedMissionLevel);
            Assert.AreEqual(0, loadedData.completedMissionLevels.Count);
        }
    }
}
