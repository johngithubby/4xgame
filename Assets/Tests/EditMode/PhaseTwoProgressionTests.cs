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
            int expectedDurationSeconds = PlayerProgression.GetHqUpgradeDurationSeconds(saveData.hqLevel);

            Assert.IsTrue(started);
            Assert.AreEqual(25, saveData.coins);
            Assert.IsTrue(saveData.hqUpgradeInProgress);
            Assert.AreEqual(expectedDurationSeconds, saveData.hqUpgradeDurationSeconds);
            Assert.AreEqual(expectedDurationSeconds, PlayerProgression.GetHqUpgradeRemainingSeconds(saveData, now));
            Assert.AreEqual(0.5f, PlayerProgression.GetHqUpgradeProgress01(saveData, now.AddSeconds(expectedDurationSeconds * 0.5f)), 0.01f);
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
            bool completed = PlayerProgression.CompleteReadyHqUpgrade(saveData, now.AddSeconds(PlayerProgression.GetHqUpgradeDurationSeconds(1) + 1));

            Assert.IsTrue(completed);
            Assert.AreEqual(2, saveData.hqLevel);
            Assert.AreEqual(1, saveData.highestUnlockedMissionLevel);
            Assert.AreEqual(1, saveData.currentMissionLevel);
            Assert.AreEqual(1, saveData.unlockedMinigameLevel);
            Assert.IsFalse(saveData.hqUpgradeInProgress);
            Assert.AreEqual(1, PlayerProgression.GetStartingSquadBonus(saveData));
        }

        [Test]
        public void BuildingUpgradeDurations_UseRequestedExponentialCurve()
        {
            Assert.AreEqual(1, PlayerProgression.GetBioLabUpgradeDurationSeconds(1));
            Assert.AreEqual(3, PlayerProgression.GetBioLabUpgradeDurationSeconds(2));
            Assert.AreEqual(10, PlayerProgression.GetBioLabUpgradeDurationSeconds(3));
            Assert.AreEqual(60, PlayerProgression.GetBioLabUpgradeDurationSeconds(4));
            Assert.AreEqual(60 * 5, PlayerProgression.GetBioLabUpgradeDurationSeconds(5));
            Assert.AreEqual(60 * 5 * 5, PlayerProgression.GetBioLabUpgradeDurationSeconds(6));
            Assert.AreEqual(60 * 5 * 5 * 5, PlayerProgression.GetBioLabUpgradeDurationSeconds(7));
            Assert.AreEqual(PlayerProgression.GetBioLabUpgradeDurationSeconds(1), PlayerProgression.GetHqUpgradeDurationSeconds(1));
            Assert.AreEqual(PlayerProgression.GetBioLabUpgradeDurationSeconds(4), PlayerProgression.GetHqUpgradeDurationSeconds(4));
            Assert.AreEqual(PlayerProgression.GetBioLabUpgradeDurationSeconds(9), PlayerProgression.GetHqUpgradeDurationSeconds(9));
        }

        [Test]
        public void StartBioLabUpgrade_SpendsCoinsAndStartsLevelDurationTimer()
        {
            SaveGameData saveData = new()
            {
                coins = PlayerProgression.GetBioLabUpgradeCost(2),
                bioLabLevel = 2
            };
            DateTime now = new(2026, 6, 13, 12, 0, 0, DateTimeKind.Utc);

            bool started = PlayerProgression.TryStartBioLabUpgrade(saveData, now);

            Assert.IsTrue(started);
            Assert.AreEqual(0, saveData.coins);
            Assert.IsTrue(saveData.bioLabUpgradeInProgress);
            Assert.AreEqual(now.Ticks, saveData.bioLabUpgradeStartedUtcTicks);
            Assert.AreEqual(3, saveData.bioLabUpgradeDurationSeconds);
            Assert.AreEqual(3, PlayerProgression.GetBioLabUpgradeRemainingSeconds(saveData, now));
            Assert.AreEqual(0.5f, PlayerProgression.GetBioLabUpgradeProgress01(saveData, now.AddSeconds(1.5)), 0.01f);
        }

        [Test]
        public void StartBioLabUpgrade_FailsWhenCoinsAreInsufficient()
        {
            SaveGameData saveData = new()
            {
                coins = PlayerProgression.GetBioLabUpgradeCost(1) - 1,
                bioLabLevel = 1
            };

            bool started = PlayerProgression.TryStartBioLabUpgrade(saveData, DateTime.UtcNow);

            Assert.IsFalse(started);
            Assert.AreEqual(PlayerProgression.GetBioLabUpgradeCost(1) - 1, saveData.coins);
            Assert.IsFalse(saveData.bioLabUpgradeInProgress);
            Assert.AreEqual(1, saveData.bioLabLevel);
        }

        [Test]
        public void CompleteReadyBioLabUpgrade_IncreasesLevelAndClearsTimer()
        {
            SaveGameData saveData = new()
            {
                coins = PlayerProgression.GetBioLabUpgradeCost(1),
                bioLabLevel = 1
            };
            DateTime now = new(2026, 6, 13, 12, 0, 0, DateTimeKind.Utc);

            PlayerProgression.TryStartBioLabUpgrade(saveData, now);
            bool completed = PlayerProgression.CompleteReadyBioLabUpgrade(saveData, now.AddSeconds(1.1));

            Assert.IsTrue(completed);
            Assert.AreEqual(2, saveData.bioLabLevel);
            Assert.IsFalse(saveData.bioLabUpgradeInProgress);
            Assert.AreEqual(0, saveData.bioLabUpgradeStartedUtcTicks);
            Assert.AreEqual(0, saveData.bioLabUpgradeDurationSeconds);
        }

        [Test]
        public void SameRuleUpgradeBuildings_MatchBioLabCostAndDuration()
        {
            // Same-rule buildings should stay on the existing lab cost and duration curve.
            Assert.AreEqual(PlayerProgression.GetBioLabUpgradeCost(3), PlayerProgression.GetHqUpgradeCost(3));
            Assert.AreEqual(PlayerProgression.GetBioLabUpgradeCost(3), PlayerProgression.GetHangarUpgradeCost(3));
            Assert.AreEqual(PlayerProgression.GetBioLabUpgradeCost(4), PlayerProgression.GetTrainingFacilityUpgradeCost(4));
            Assert.AreEqual(PlayerProgression.GetBioLabUpgradeCost(5), PlayerProgression.GetLivingQuartersUpgradeCost(5));
            Assert.AreEqual(PlayerProgression.GetBioLabUpgradeDurationSeconds(4), PlayerProgression.GetHqUpgradeDurationSeconds(4));
            Assert.AreEqual(PlayerProgression.GetBioLabUpgradeDurationSeconds(5), PlayerProgression.GetHangarUpgradeDurationSeconds(5));
            Assert.AreEqual(PlayerProgression.GetBioLabUpgradeDurationSeconds(6), PlayerProgression.GetTrainingFacilityUpgradeDurationSeconds(6));
            Assert.AreEqual(PlayerProgression.GetBioLabUpgradeDurationSeconds(7), PlayerProgression.GetLivingQuartersUpgradeDurationSeconds(7));
        }

        [Test]
        public void StartHangarAndTrainingUpgrades_SpendCoinsAndStartLevelDurationTimers()
        {
            SaveGameData saveData = new()
            {
                coins = PlayerProgression.GetHangarUpgradeCost(2) + PlayerProgression.GetTrainingFacilityUpgradeCost(3),
                hangarLevel = 2,
                trainingFacilityLevel = 3
            };
            DateTime now = new(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc);

            bool hangarStarted = PlayerProgression.TryStartHangarUpgrade(saveData, now);
            bool trainingStarted = PlayerProgression.TryStartTrainingFacilityUpgrade(saveData, now);

            Assert.IsTrue(hangarStarted);
            Assert.IsTrue(trainingStarted);
            Assert.AreEqual(0, saveData.coins);
            Assert.IsTrue(saveData.hangarUpgradeInProgress);
            Assert.IsTrue(saveData.trainingFacilityUpgradeInProgress);
            Assert.AreEqual(now.Ticks, saveData.hangarUpgradeStartedUtcTicks);
            Assert.AreEqual(now.Ticks, saveData.trainingFacilityUpgradeStartedUtcTicks);
            Assert.AreEqual(3, saveData.hangarUpgradeDurationSeconds);
            Assert.AreEqual(10, saveData.trainingFacilityUpgradeDurationSeconds);
            Assert.AreEqual(3, PlayerProgression.GetHangarUpgradeRemainingSeconds(saveData, now));
            Assert.AreEqual(10, PlayerProgression.GetTrainingFacilityUpgradeRemainingSeconds(saveData, now));
            Assert.AreEqual(0.5f, PlayerProgression.GetHangarUpgradeProgress01(saveData, now.AddSeconds(1.5)), 0.01f);
            Assert.AreEqual(0.5f, PlayerProgression.GetTrainingFacilityUpgradeProgress01(saveData, now.AddSeconds(5)), 0.01f);
        }

        [Test]
        public void StartHangarAndTrainingUpgrades_FailWhenCoinsAreInsufficient()
        {
            SaveGameData saveData = new()
            {
                coins = PlayerProgression.GetHangarUpgradeCost(1) - 1,
                hangarLevel = 1,
                trainingFacilityLevel = 1,
                livingQuartersLevel = 1
            };

            bool hangarStarted = PlayerProgression.TryStartHangarUpgrade(saveData, DateTime.UtcNow);
            bool trainingStarted = PlayerProgression.TryStartTrainingFacilityUpgrade(saveData, DateTime.UtcNow);
            bool livingQuartersStarted = PlayerProgression.TryStartLivingQuartersUpgrade(saveData, DateTime.UtcNow);

            Assert.IsFalse(hangarStarted);
            Assert.IsFalse(trainingStarted);
            Assert.IsFalse(livingQuartersStarted);
            Assert.AreEqual(PlayerProgression.GetHangarUpgradeCost(1) - 1, saveData.coins);
            Assert.IsFalse(saveData.hangarUpgradeInProgress);
            Assert.IsFalse(saveData.trainingFacilityUpgradeInProgress);
            Assert.IsFalse(saveData.livingQuartersUpgradeInProgress);
            Assert.AreEqual(1, saveData.hangarLevel);
            Assert.AreEqual(1, saveData.trainingFacilityLevel);
            Assert.AreEqual(1, saveData.livingQuartersLevel);
        }

        [Test]
        public void CompleteReadyHangarTrainingAndLivingQuartersUpgrades_IncreaseLevelsAndClearTimers()
        {
            SaveGameData saveData = new()
            {
                coins = PlayerProgression.GetHangarUpgradeCost(1) + PlayerProgression.GetTrainingFacilityUpgradeCost(1) + PlayerProgression.GetLivingQuartersUpgradeCost(1),
                hangarLevel = 1,
                trainingFacilityLevel = 1,
                livingQuartersLevel = 1
            };
            DateTime now = new(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc);

            PlayerProgression.TryStartHangarUpgrade(saveData, now);
            PlayerProgression.TryStartTrainingFacilityUpgrade(saveData, now);
            PlayerProgression.TryStartLivingQuartersUpgrade(saveData, now);
            bool hangarCompleted = PlayerProgression.CompleteReadyHangarUpgrade(saveData, now.AddSeconds(1.1));
            bool trainingCompleted = PlayerProgression.CompleteReadyTrainingFacilityUpgrade(saveData, now.AddSeconds(1.1));
            bool livingQuartersCompleted = PlayerProgression.CompleteReadyLivingQuartersUpgrade(saveData, now.AddSeconds(1.1));

            Assert.IsTrue(hangarCompleted);
            Assert.IsTrue(trainingCompleted);
            Assert.IsTrue(livingQuartersCompleted);
            Assert.AreEqual(2, saveData.hangarLevel);
            Assert.AreEqual(2, saveData.trainingFacilityLevel);
            Assert.AreEqual(2, saveData.livingQuartersLevel);
            Assert.IsFalse(saveData.hangarUpgradeInProgress);
            Assert.IsFalse(saveData.trainingFacilityUpgradeInProgress);
            Assert.IsFalse(saveData.livingQuartersUpgradeInProgress);
            Assert.AreEqual(0, saveData.hangarUpgradeStartedUtcTicks);
            Assert.AreEqual(0, saveData.trainingFacilityUpgradeStartedUtcTicks);
            Assert.AreEqual(0, saveData.livingQuartersUpgradeStartedUtcTicks);
            Assert.AreEqual(0, saveData.hangarUpgradeDurationSeconds);
            Assert.AreEqual(0, saveData.trainingFacilityUpgradeDurationSeconds);
            Assert.AreEqual(0, saveData.livingQuartersUpgradeDurationSeconds);
        }

        [Test]
        public void StartLivingQuartersUpgrade_SpendsCoinsAndStartsLevelDurationTimer()
        {
            SaveGameData saveData = new()
            {
                coins = PlayerProgression.GetLivingQuartersUpgradeCost(4),
                livingQuartersLevel = 4
            };
            DateTime now = new(2026, 6, 16, 12, 0, 0, DateTimeKind.Utc);

            bool started = PlayerProgression.TryStartLivingQuartersUpgrade(saveData, now);

            Assert.IsTrue(started);
            Assert.AreEqual(0, saveData.coins);
            Assert.IsTrue(saveData.livingQuartersUpgradeInProgress);
            Assert.AreEqual(now.Ticks, saveData.livingQuartersUpgradeStartedUtcTicks);
            Assert.AreEqual(60, saveData.livingQuartersUpgradeDurationSeconds);
            Assert.AreEqual(60, PlayerProgression.GetLivingQuartersUpgradeRemainingSeconds(saveData, now));
            Assert.AreEqual(0.5f, PlayerProgression.GetLivingQuartersUpgradeProgress01(saveData, now.AddSeconds(30)), 0.01f);
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
        public void Normalize_ClearsMalformedBioLabTimerWithoutLeveling()
        {
            SaveGameData saveData = new()
            {
                bioLabLevel = 2,
                bioLabUpgradeInProgress = true,
                bioLabUpgradeStartedUtcTicks = long.MaxValue,
                bioLabUpgradeDurationSeconds = 3
            };

            saveData.Normalize();

            Assert.AreEqual(2, saveData.bioLabLevel);
            Assert.IsFalse(saveData.bioLabUpgradeInProgress);
            Assert.AreEqual(0, saveData.bioLabUpgradeStartedUtcTicks);
            Assert.AreEqual(0, saveData.bioLabUpgradeDurationSeconds);
        }

        [Test]
        public void Normalize_ClearsMalformedHangarTrainingAndLivingQuartersTimersWithoutLeveling()
        {
            SaveGameData saveData = new()
            {
                hangarLevel = 3,
                hangarUpgradeInProgress = true,
                hangarUpgradeStartedUtcTicks = long.MaxValue,
                hangarUpgradeDurationSeconds = 10,
                trainingFacilityLevel = 4,
                trainingFacilityUpgradeInProgress = true,
                trainingFacilityUpgradeStartedUtcTicks = long.MaxValue,
                trainingFacilityUpgradeDurationSeconds = 60,
                livingQuartersLevel = 5,
                livingQuartersUpgradeInProgress = true,
                livingQuartersUpgradeStartedUtcTicks = long.MaxValue,
                livingQuartersUpgradeDurationSeconds = 300
            };

            saveData.Normalize();

            Assert.AreEqual(3, saveData.hangarLevel);
            Assert.AreEqual(4, saveData.trainingFacilityLevel);
            Assert.AreEqual(5, saveData.livingQuartersLevel);
            Assert.IsFalse(saveData.hangarUpgradeInProgress);
            Assert.IsFalse(saveData.trainingFacilityUpgradeInProgress);
            Assert.IsFalse(saveData.livingQuartersUpgradeInProgress);
            Assert.AreEqual(0, saveData.hangarUpgradeStartedUtcTicks);
            Assert.AreEqual(0, saveData.trainingFacilityUpgradeStartedUtcTicks);
            Assert.AreEqual(0, saveData.livingQuartersUpgradeStartedUtcTicks);
            Assert.AreEqual(0, saveData.hangarUpgradeDurationSeconds);
            Assert.AreEqual(0, saveData.trainingFacilityUpgradeDurationSeconds);
            Assert.AreEqual(0, saveData.livingQuartersUpgradeDurationSeconds);
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
                bioLabLevel = 4,
                bioLabUpgradeInProgress = true,
                bioLabUpgradeStartedUtcTicks = new DateTime(2026, 6, 13, 12, 0, 0, DateTimeKind.Utc).Ticks,
                bioLabUpgradeDurationSeconds = 60,
                hangarLevel = 5,
                hangarUpgradeInProgress = true,
                hangarUpgradeStartedUtcTicks = new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc).Ticks,
                hangarUpgradeDurationSeconds = 300,
                trainingFacilityLevel = 6,
                trainingFacilityUpgradeInProgress = true,
                trainingFacilityUpgradeStartedUtcTicks = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc).Ticks,
                trainingFacilityUpgradeDurationSeconds = 1500,
                livingQuartersLevel = 7,
                livingQuartersUpgradeInProgress = true,
                livingQuartersUpgradeStartedUtcTicks = new DateTime(2026, 6, 16, 12, 0, 0, DateTimeKind.Utc).Ticks,
                livingQuartersUpgradeDurationSeconds = 7500,
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
            Assert.AreEqual(4, loadedData.bioLabLevel);
            Assert.IsTrue(loadedData.bioLabUpgradeInProgress);
            Assert.AreEqual(saveData.bioLabUpgradeStartedUtcTicks, loadedData.bioLabUpgradeStartedUtcTicks);
            Assert.AreEqual(60, loadedData.bioLabUpgradeDurationSeconds);
            Assert.AreEqual(5, loadedData.hangarLevel);
            Assert.IsTrue(loadedData.hangarUpgradeInProgress);
            Assert.AreEqual(saveData.hangarUpgradeStartedUtcTicks, loadedData.hangarUpgradeStartedUtcTicks);
            Assert.AreEqual(300, loadedData.hangarUpgradeDurationSeconds);
            Assert.AreEqual(6, loadedData.trainingFacilityLevel);
            Assert.IsTrue(loadedData.trainingFacilityUpgradeInProgress);
            Assert.AreEqual(saveData.trainingFacilityUpgradeStartedUtcTicks, loadedData.trainingFacilityUpgradeStartedUtcTicks);
            Assert.AreEqual(1500, loadedData.trainingFacilityUpgradeDurationSeconds);
            Assert.AreEqual(7, loadedData.livingQuartersLevel);
            Assert.IsTrue(loadedData.livingQuartersUpgradeInProgress);
            Assert.AreEqual(saveData.livingQuartersUpgradeStartedUtcTicks, loadedData.livingQuartersUpgradeStartedUtcTicks);
            Assert.AreEqual(7500, loadedData.livingQuartersUpgradeDurationSeconds);
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
            Assert.AreEqual(1, loadedData.bioLabLevel);
            Assert.IsFalse(loadedData.bioLabUpgradeInProgress);
            Assert.AreEqual(1, loadedData.hangarLevel);
            Assert.IsFalse(loadedData.hangarUpgradeInProgress);
            Assert.AreEqual(1, loadedData.trainingFacilityLevel);
            Assert.IsFalse(loadedData.trainingFacilityUpgradeInProgress);
            Assert.AreEqual(1, loadedData.livingQuartersLevel);
            Assert.IsFalse(loadedData.livingQuartersUpgradeInProgress);
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
                bioLabLevel = 3,
                bioLabUpgradeInProgress = true,
                bioLabUpgradeStartedUtcTicks = new DateTime(2026, 6, 13, 12, 0, 0, DateTimeKind.Utc).Ticks,
                bioLabUpgradeDurationSeconds = 10,
                hangarLevel = 3,
                hangarUpgradeInProgress = true,
                hangarUpgradeStartedUtcTicks = new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc).Ticks,
                hangarUpgradeDurationSeconds = 10,
                trainingFacilityLevel = 3,
                trainingFacilityUpgradeInProgress = true,
                trainingFacilityUpgradeStartedUtcTicks = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc).Ticks,
                trainingFacilityUpgradeDurationSeconds = 10,
                livingQuartersLevel = 3,
                livingQuartersUpgradeInProgress = true,
                livingQuartersUpgradeStartedUtcTicks = new DateTime(2026, 6, 16, 12, 0, 0, DateTimeKind.Utc).Ticks,
                livingQuartersUpgradeDurationSeconds = 10,
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
            Assert.AreEqual(1, resetData.bioLabLevel);
            Assert.IsFalse(resetData.bioLabUpgradeInProgress);
            Assert.AreEqual(1, resetData.hangarLevel);
            Assert.IsFalse(resetData.hangarUpgradeInProgress);
            Assert.AreEqual(1, resetData.trainingFacilityLevel);
            Assert.IsFalse(resetData.trainingFacilityUpgradeInProgress);
            Assert.AreEqual(1, resetData.livingQuartersLevel);
            Assert.IsFalse(resetData.livingQuartersUpgradeInProgress);
            Assert.AreEqual(1, resetData.unlockedMinigameLevel);
            Assert.AreEqual(1, resetData.currentMissionLevel);
            Assert.AreEqual(1, resetData.highestUnlockedMissionLevel);
            Assert.AreEqual(0, resetData.completedMissionLevels.Count);
            Assert.AreEqual(0, loadedData.coins);
            Assert.AreEqual(1, loadedData.hqLevel);
            Assert.AreEqual(1, loadedData.bioLabLevel);
            Assert.IsFalse(loadedData.bioLabUpgradeInProgress);
            Assert.AreEqual(1, loadedData.hangarLevel);
            Assert.IsFalse(loadedData.hangarUpgradeInProgress);
            Assert.AreEqual(1, loadedData.trainingFacilityLevel);
            Assert.IsFalse(loadedData.trainingFacilityUpgradeInProgress);
            Assert.AreEqual(1, loadedData.livingQuartersLevel);
            Assert.IsFalse(loadedData.livingQuartersUpgradeInProgress);
            Assert.AreEqual(1, loadedData.unlockedMinigameLevel);
            Assert.AreEqual(1, loadedData.currentMissionLevel);
            Assert.AreEqual(1, loadedData.highestUnlockedMissionLevel);
            Assert.AreEqual(0, loadedData.completedMissionLevels.Count);
        }
    }
}
