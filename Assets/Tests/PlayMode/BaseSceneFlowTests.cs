using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LaneSurvivor.Gameplay;
using LaneSurvivor.Heroes;
using LaneSurvivor.Progression;
using LaneSurvivor.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LaneSurvivor.Tests.PlayMode
{
    public sealed class BaseSceneFlowTests
    {
        private string tempSavePath;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Use an isolated save file so scene smoke tests never mutate the developer's real progress.
            tempSavePath = Path.Combine(Path.GetTempPath(), $"lane-survivor-playmode-save-{Guid.NewGuid():N}.json");
            SaveGameManager.UseCustomSavePathForTests(tempSavePath);

            // Start each test from an empty scene so objects from previous PlayMode tests cannot leak.
            SceneManager.LoadScene("Base");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Return to the Base scene before cleanup so active scene state is predictable for later tests.
            SceneManager.LoadScene("Base");
            yield return null;

            // Restore production save behavior after the test fixture finishes.
            SaveGameManager.ClearCustomSavePathForTests();

            // Remove the isolated save file produced by scene bootstrap and navigation.
            if (!string.IsNullOrEmpty(tempSavePath) && File.Exists(tempSavePath))
            {
                File.Delete(tempSavePath);
            }

            // Remove any temp write file left behind if a save assertion fails midway.
            if (!string.IsNullOrEmpty(tempSavePath) && File.Exists($"{tempSavePath}.tmp"))
            {
                File.Delete($"{tempSavePath}.tmp");
            }
        }

        [UnityTest]
        public IEnumerator BaseScene_PlayButtonLoadsMinigameScene()
        {
            // Wait one frame so BaseSceneBootstrap can build runtime UI and world objects.
            yield return null;

            // The active scene should be the local base hub.
            Assert.AreEqual("Base", SceneManager.GetActiveScene().name);

            // Runtime-built objects prove the bootstrap ran successfully.
            Assert.IsNotNull(GameObject.Find("Base HUD Canvas"));
            Assert.IsNotNull(GameObject.Find("HQ Building"));
            Text missionPanelText = GameObject.Find("Mission Panel Text")?.GetComponent<Text>();
            Assert.IsNotNull(missionPanelText);
            StringAssert.Contains("> M1 Outskirts [SELECTED] Win: +50c + M2", missionPanelText.text);
            StringAssert.Contains("M2 Market Run [LOCKED] Unlock: clear M1", missionPanelText.text);

            // Invoke the real UI button listener so this verifies the same navigation path as a tap.
            Button playButton = GameObject.Find("Play Button")?.GetComponent<Button>();
            Assert.IsNotNull(playButton);
            playButton.onClick.Invoke();

            // SceneManager.LoadScene completes on the next frame in this smoke-test path.
            yield return null;

            // The minigame scene should become active and build its runtime gameplay objects.
            Assert.AreEqual("Minigame", SceneManager.GetActiveScene().name);
            Assert.IsNotNull(GameObject.Find("Level Manager"));
            Assert.IsNotNull(GameObject.Find("Player Squad"));
        }

        [UnityTest]
        public IEnumerator BaseScene_TopLeftHudLabelsStayInsideCanvas()
        {
            // Wait one frame so the runtime-built Base HUD exists before inspecting its RectTransforms.
            yield return null;

            // The canvas bounds give the assertion the same left edge the player sees in Game view.
            RectTransform canvasRect = GameObject.Find("Base HUD Canvas")?.GetComponent<RectTransform>();
            Assert.IsNotNull(canvasRect);
            Vector3[] canvasCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasCorners);
            float canvasLeftEdge = canvasCorners[0].x;

            // These labels use upper-left anchoring and are the group that regressed offscreen in the editor view.
            string[] topLeftHudLabels =
            {
                "Coins Text",
                "HQ Text",
                "Timer Text",
                "Hero Text",
                "Mission Panel Title Text",
                "Mission Panel Text"
            };

            foreach (string labelName in topLeftHudLabels)
            {
                // Fetch each generated label by name so the test follows the runtime scene structure directly.
                RectTransform labelRect = GameObject.Find(labelName)?.GetComponent<RectTransform>();
                Assert.IsNotNull(labelRect, labelName);
                Vector3[] labelCorners = new Vector3[4];
                labelRect.GetWorldCorners(labelCorners);

                // The left edge can have tiny float noise, but it should never cross outside the canvas.
                Assert.GreaterOrEqual(labelCorners[0].x, canvasLeftEdge - 0.5f, $"{labelName} left edge should stay inside the Base HUD canvas.");
            }
        }

        [UnityTest]
        public IEnumerator BaseScene_MissionPanelButtonsPersistUnlockedSelectionAndRejectLockedRows()
        {
            // Seed mission two as unlocked so the Base HUD can select it while mission three remains locked.
            SaveGameManager.Save(new SaveGameData
            {
                currentMissionLevel = 1,
                highestUnlockedMissionLevel = 2,
                unlockedMinigameLevel = 2,
                completedMissionLevels = new List<int> { 1 }
            });

            // Reload Base after seeding so the bootstrap reads the prepared mission state.
            SceneManager.LoadScene("Base");
            yield return null;

            // The mission two button should be usable because mission two is already unlocked.
            Button missionTwoButton = GameObject.Find("Mission 2 Button")?.GetComponent<Button>();
            Assert.IsNotNull(missionTwoButton);
            Assert.IsTrue(missionTwoButton.interactable);
            missionTwoButton.onClick.Invoke();
            yield return null;

            // Selecting mission 2 should save immediately and update the visible mission panel.
            SaveGameData missionTwoData = SaveGameManager.Load();
            Assert.AreEqual(2, missionTwoData.currentMissionLevel);
            Text missionPanelText = GameObject.Find("Mission Panel Text")?.GetComponent<Text>();
            Assert.IsNotNull(missionPanelText);
            StringAssert.Contains("M1 Outskirts [DONE] Replay: +50c", missionPanelText.text);
            StringAssert.Contains("> M2 Market Run [SELECTED] Win: +50c + M3", missionPanelText.text);

            // Mission three should be visible but disabled until mission two is completed.
            Button missionThreeButton = GameObject.Find("Mission 3 Button")?.GetComponent<Button>();
            Assert.IsNotNull(missionThreeButton);
            Assert.IsFalse(missionThreeButton.interactable);
            missionThreeButton.onClick.Invoke();
            yield return null;

            // A direct listener invoke should still fail safely through the progression guard and leave selection alone.
            SaveGameData lockedAttemptData = SaveGameManager.Load();
            Assert.AreEqual(2, lockedAttemptData.currentMissionLevel);
            Text statusText = GameObject.Find("Status Text")?.GetComponent<Text>();
            Assert.IsNotNull(statusText);
            StringAssert.Contains("Mission 3 locked", statusText.text);
        }

        [UnityTest]
        public IEnumerator BaseScene_CollectAndUpgradeButtonsPersistLocalProgress()
        {
            // Wait one frame so BaseSceneBootstrap can build runtime UI and world objects.
            yield return null;

            // Fresh local progress should show the first HQ level before any actions run.
            Text hqText = GameObject.Find("HQ Text")?.GetComponent<Text>();
            Assert.IsNotNull(hqText);
            Assert.AreEqual("HQ Level: 1", hqText.text);

            // The real Collect button listener should grant coins and save after each click.
            Button collectButton = GameObject.Find("Collect Button")?.GetComponent<Button>();
            Assert.IsNotNull(collectButton);
            collectButton.onClick.Invoke();
            collectButton.onClick.Invoke();
            collectButton.onClick.Invoke();

            // Three prototype collections exactly fund the first HQ upgrade.
            SaveGameData collectedData = SaveGameManager.Load();
            Assert.AreEqual(PlayerProgression.GetHqUpgradeCost(1), collectedData.coins);

            // The upgrade button should become interactable once the wallet can pay the cost.
            Button upgradeButton = GameObject.Find("Upgrade Button")?.GetComponent<Button>();
            Assert.IsNotNull(upgradeButton);
            Assert.IsTrue(upgradeButton.interactable);
            upgradeButton.onClick.Invoke();

            // Wait one frame so the Base scene can refresh its timer display from the saved upgrade state.
            yield return null;

            // Starting an upgrade should spend the coins and persist an active timer locally.
            SaveGameData upgradedData = SaveGameManager.Load();
            Assert.AreEqual(0, upgradedData.coins);
            Assert.IsTrue(upgradedData.hqUpgradeInProgress);
            Assert.AreEqual(PlayerProgression.HqUpgradeDurationSeconds, upgradedData.hqUpgradeDurationSeconds);

            // The visible HUD should now show a running countdown instead of the ready state.
            Text timerText = GameObject.Find("Timer Text")?.GetComponent<Text>();
            Assert.IsNotNull(timerText);
            StringAssert.StartsWith("Upgrade: ", timerText.text);
            Assert.AreNotEqual("Upgrade: Ready", timerText.text);
        }

        [UnityTest]
        public IEnumerator BaseScene_CompletedUpgradeShowsFeedbackAndSavesMilestoneRewards()
        {
            // Seed a timer that should complete as soon as the Base scene loads.
            SaveGameData saveData = new()
            {
                hqLevel = 1,
                unlockedMinigameLevel = 1,
                hqUpgradeInProgress = true,
                hqUpgradeStartedUtcTicks = DateTime.UtcNow.AddSeconds(-PlayerProgression.HqUpgradeDurationSeconds - 1).Ticks,
                hqUpgradeDurationSeconds = PlayerProgression.HqUpgradeDurationSeconds
            };
            SaveGameManager.Save(saveData);

            // Reload Base so the bootstrap executes the same ready-upgrade path a player would hit on app reopen.
            SceneManager.LoadScene("Base");
            yield return null;

            // The saved state should now reflect the completed HQ upgrade and milestone hero reward.
            SaveGameData completedData = SaveGameManager.Load();
            Assert.AreEqual(2, completedData.hqLevel);
            Assert.AreEqual(1, completedData.highestUnlockedMissionLevel);
            Assert.AreEqual(1, completedData.currentMissionLevel);
            Assert.AreEqual(1, completedData.unlockedMinigameLevel);
            Assert.IsFalse(completedData.hqUpgradeInProgress);
            Assert.IsTrue(HeroInventory.OwnsHero(completedData, HeroCatalog.HqLevelTwoHeroId));

            // The visible Base feedback should keep the upgrade completion and hero reward readable together.
            Text statusText = GameObject.Find("Status Text")?.GetComponent<Text>();
            Assert.IsNotNull(statusText);
            StringAssert.Contains("HQ upgrade complete", statusText.text);
            StringAssert.Contains("Dax Medic joined", statusText.text);
        }

        [UnityTest]
        public IEnumerator BaseScene_HeroesButtonLoadsHeroesSceneAndBackButtonReturns()
        {
            // Wait one frame so BaseSceneBootstrap can build runtime UI and world objects.
            yield return null;

            // Invoke the Heroes button path from the Base HUD.
            Button heroesButton = GameObject.Find("Heroes Button")?.GetComponent<Button>();
            Assert.IsNotNull(heroesButton);
            heroesButton.onClick.Invoke();
            yield return null;

            // The dedicated Hero scene should build its runtime HUD.
            Assert.AreEqual("Heroes", SceneManager.GetActiveScene().name);
            Assert.IsNotNull(GameObject.Find("Hero HUD Canvas"));

            // Invoke the real Back button listener to verify return navigation.
            Button backButton = GameObject.Find("Back Button")?.GetComponent<Button>();
            Assert.IsNotNull(backButton);
            backButton.onClick.Invoke();
            yield return null;

            // Returning to Base should rebuild the base HUD.
            Assert.AreEqual("Base", SceneManager.GetActiveScene().name);
            Assert.IsNotNull(GameObject.Find("Base HUD Canvas"));
        }

        [UnityTest]
        public IEnumerator MinigameScene_LaneButtonsMoveSquadBetweenLanes()
        {
            // Load the minigame directly so this smoke test focuses on lane-control UI wiring.
            SceneManager.LoadScene("Minigame");
            yield return null;

            // Wait a second frame so LevelManager.Start and input listener registration finish.
            yield return null;

            // The runtime-built squad starts in the center lane of the three-lane definition.
            PlayerSquad playerSquad = GameObject.Find("Player Squad")?.GetComponent<PlayerSquad>();
            Assert.IsNotNull(playerSquad);
            Assert.AreEqual(1, playerSquad.CurrentLaneIndex);

            // Invoke the real right-lane button listener used by touch input on the HUD.
            Button rightButton = GameObject.Find("Right Lane Button")?.GetComponent<Button>();
            Assert.IsNotNull(rightButton);
            rightButton.onClick.Invoke();
            Assert.AreEqual(2, playerSquad.CurrentLaneIndex);

            // Invoke the real left-lane button listener twice to verify movement and lower clamping.
            Button leftButton = GameObject.Find("Left Lane Button")?.GetComponent<Button>();
            Assert.IsNotNull(leftButton);
            leftButton.onClick.Invoke();
            leftButton.onClick.Invoke();
            Assert.AreEqual(0, playerSquad.CurrentLaneIndex);
        }

        [UnityTest]
        public IEnumerator MinigameScene_HudEdgeLabelsStayInsideCanvas()
        {
            // Load the minigame directly so this test inspects the runtime HUD generated for actual play.
            SceneManager.LoadScene("Minigame");
            yield return null;

            // Canvas bounds define the visible overlay edges where simulator captures can reveal clipping.
            RectTransform canvasRect = GameObject.Find("HUD Canvas")?.GetComponent<RectTransform>();
            Assert.IsNotNull(canvasRect);
            Vector3[] canvasCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasCorners);
            float canvasLeftEdge = canvasCorners[0].x;
            float canvasTopEdge = canvasCorners[1].y;
            float canvasRightEdge = canvasCorners[2].x;

            // These upper-left labels are the minigame text group that can hang offscreen with a centered pivot.
            string[] topLeftHudLabels =
            {
                "Squad Text",
                "Progress Text"
            };

            foreach (string labelName in topLeftHudLabels)
            {
                // Inspect the generated RectTransform so the test follows the same objects the player sees.
                RectTransform labelRect = GameObject.Find(labelName)?.GetComponent<RectTransform>();
                Assert.IsNotNull(labelRect, labelName);
                Vector3[] labelCorners = new Vector3[4];
                labelRect.GetWorldCorners(labelCorners);

                // A tiny tolerance avoids float noise while still failing on visible left-edge clipping.
                Assert.GreaterOrEqual(labelCorners[0].x, canvasLeftEdge - 0.5f, $"{labelName} left edge should stay inside the Minigame HUD canvas.");
                Assert.LessOrEqual(labelCorners[1].y, canvasTopEdge + 0.5f, $"{labelName} top edge should stay inside the Minigame HUD canvas.");
            }

            // The state label lives on the upper right so it stays out from under the iPhone Dynamic Island.
            RectTransform stateRect = GameObject.Find("State Text")?.GetComponent<RectTransform>();
            Assert.IsNotNull(stateRect);
            Vector3[] stateCorners = new Vector3[4];
            stateRect.GetWorldCorners(stateCorners);

            // The upper-right anchor and pivot make the state text grow inward from the right screen inset.
            Assert.AreEqual(new Vector2(1f, 1f), stateRect.anchorMin);
            Assert.AreEqual(new Vector2(1f, 1f), stateRect.anchorMax);
            Assert.AreEqual(new Vector2(1f, 1f), stateRect.pivot);

            // A tiny tolerance avoids float noise while still failing on visible top or right-edge clipping.
            Assert.LessOrEqual(stateCorners[1].y, canvasTopEdge + 0.5f, "State Text top edge should stay inside the Minigame HUD canvas.");
            Assert.LessOrEqual(stateCorners[2].x, canvasRightEdge + 0.5f, "State Text right edge should stay inside the Minigame HUD canvas.");
        }

        [UnityTest]
        public IEnumerator MinigameScene_StartButtonMovesSquadForward()
        {
            // Load the minigame directly so this smoke test focuses on the start and movement loop.
            SceneManager.LoadScene("Minigame");
            yield return null;

            // Wait a second frame so the HUD button listener is registered.
            yield return null;

            // Capture the initialized squad position before beginning the run.
            PlayerSquad playerSquad = GameObject.Find("Player Squad")?.GetComponent<PlayerSquad>();
            Assert.IsNotNull(playerSquad);
            float startingZ = playerSquad.transform.position.z;

            // Invoke the same Start button path a player can tap before the auto-start delay expires.
            Button startButton = GameObject.Find("Start Button")?.GetComponent<Button>();
            Assert.IsNotNull(startButton);
            startButton.onClick.Invoke();

            // Let a few PlayMode frames advance so PlayerSquad.Update can move down the lane.
            yield return new WaitForSeconds(0.12f);

            // The squad should now be moving forward under the real runtime Update loop.
            Assert.Greater(playerSquad.transform.position.z, startingZ);
        }

        [UnityTest]
        public IEnumerator MinigameScene_EndScreenBaseButtonReturnsToBase()
        {
            // Load the minigame directly so this smoke test focuses on the end-screen navigation path.
            SceneManager.LoadScene("Minigame");
            yield return null;

            // Wait a second frame so LevelManager.Start initializes gameplay dependencies.
            yield return null;

            // Runtime-built gameplay objects prove the Minigame scene bootstrapped correctly.
            Assert.AreEqual("Minigame", SceneManager.GetActiveScene().name);
            LevelManager levelManager = GameObject.Find("Level Manager")?.GetComponent<LevelManager>();
            PlayerSquad playerSquad = GameObject.Find("Player Squad")?.GetComponent<PlayerSquad>();
            Assert.IsNotNull(levelManager);
            Assert.IsNotNull(playerSquad);

            // Move the initialized squad beyond the finish line and let the real Update path enter the win state.
            levelManager.BeginLevel();
            playerSquad.transform.position = new Vector3(playerSquad.transform.position.x, playerSquad.transform.position.y, 999f);
            yield return null;

            // The win screen should expose the same Base button the player taps after a completed run.
            Assert.AreEqual(LevelState.Won, levelManager.State);
            Text rewardText = GameObject.Find("Reward Text")?.GetComponent<Text>();
            Assert.IsNotNull(rewardText);
            StringAssert.Contains($"+{PlayerProgression.MinigameWinCoins} coins", rewardText.text);
            StringAssert.Contains("Mission 2 unlocked", rewardText.text);
            StringAssert.Contains("Mira Vanguard", rewardText.text);

            // Reload from disk so this proves LevelManager persisted the scene-earned rewards.
            SaveGameData rewardedData = SaveGameManager.Load();
            Assert.AreEqual(PlayerProgression.MinigameWinCoins, rewardedData.coins);
            Assert.AreEqual(2, rewardedData.currentMissionLevel);
            Assert.AreEqual(2, rewardedData.highestUnlockedMissionLevel);
            Assert.AreEqual(2, rewardedData.unlockedMinigameLevel);
            CollectionAssert.AreEqual(new[] { 1 }, rewardedData.completedMissionLevels);
            Assert.IsTrue(HeroInventory.OwnsHero(rewardedData, HeroCatalog.FirstWinHeroId));

            Button baseButton = GameObject.Find("Base Button")?.GetComponent<Button>();
            Assert.IsNotNull(baseButton);
            Assert.IsTrue(baseButton.gameObject.activeInHierarchy);
            baseButton.onClick.Invoke();
            yield return null;

            // Returning to Base should rebuild the local hub instead of leaving the player in the minigame.
            Assert.AreEqual("Base", SceneManager.GetActiveScene().name);
            Assert.IsNotNull(GameObject.Find("Base HUD Canvas"));
            Text returnedMissionPanelText = GameObject.Find("Mission Panel Text")?.GetComponent<Text>();
            Assert.IsNotNull(returnedMissionPanelText);
            StringAssert.Contains("M1 Outskirts [DONE] Replay: +50c", returnedMissionPanelText.text);
            StringAssert.Contains("> M2 Market Run [SELECTED] Win: +50c + M3", returnedMissionPanelText.text);
        }

        [UnityTest]
        public IEnumerator MinigameScene_MaxMissionCompletionMarksDoneWithoutUnlockingPastCap()
        {
            // Seed the final authored mission so the win path exercises the local progression cap.
            SaveGameManager.Save(new SaveGameData
            {
                // A large HQ bonus lets the teleported squad survive center-lane breaches on the final layout.
                hqLevel = 30,
                currentMissionLevel = PlayerProgression.MaxMissionLevel,
                highestUnlockedMissionLevel = PlayerProgression.MaxMissionLevel,
                unlockedMinigameLevel = PlayerProgression.MaxMissionLevel,
                completedMissionLevels = new List<int> { 1, 2, 3 }
            });

            // Load the minigame directly so the selected final mission is used by the runtime bootstrap.
            SceneManager.LoadScene("Minigame");
            yield return null;

            // Wait a second frame so LevelManager.Start initializes gameplay dependencies.
            yield return null;

            // Force a win on the final mission through the same state transition used by normal gameplay.
            LevelManager levelManager = GameObject.Find("Level Manager")?.GetComponent<LevelManager>();
            PlayerSquad playerSquad = GameObject.Find("Player Squad")?.GetComponent<PlayerSquad>();
            Assert.IsNotNull(levelManager);
            Assert.IsNotNull(playerSquad);
            levelManager.BeginLevel();
            playerSquad.transform.position = new Vector3(playerSquad.transform.position.x, playerSquad.transform.position.y, 999f);
            yield return null;

            // The reward text should not advertise an impossible mission five unlock.
            Text rewardText = GameObject.Find("Reward Text")?.GetComponent<Text>();
            Assert.IsNotNull(rewardText);
            StringAssert.Contains($"+{PlayerProgression.MinigameWinCoins} coins", rewardText.text);
            Assert.IsFalse(rewardText.text.Contains("Mission 5"), rewardText.text);
            Assert.IsFalse(rewardText.text.Contains("unlocked"), rewardText.text);

            // The saved state should mark mission four complete while preserving the authored mission cap.
            SaveGameData rewardedData = SaveGameManager.Load();
            Assert.AreEqual(PlayerProgression.MaxMissionLevel, rewardedData.currentMissionLevel);
            Assert.AreEqual(PlayerProgression.MaxMissionLevel, rewardedData.highestUnlockedMissionLevel);
            Assert.AreEqual(PlayerProgression.MaxMissionLevel, rewardedData.unlockedMinigameLevel);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, rewardedData.completedMissionLevels);

            // Returning to Base should show the final mission as selected and replayable rather than locked.
            Button baseButton = GameObject.Find("Base Button")?.GetComponent<Button>();
            Assert.IsNotNull(baseButton);
            baseButton.onClick.Invoke();
            yield return null;

            Assert.AreEqual("Base", SceneManager.GetActiveScene().name);
            Text returnedMissionPanelText = GameObject.Find("Mission Panel Text")?.GetComponent<Text>();
            Assert.IsNotNull(returnedMissionPanelText);
            StringAssert.Contains("> M4 Last Block [DONE SELECTED] Replay: +50c", returnedMissionPanelText.text);
        }

        [UnityTest]
        public IEnumerator MinigameScene_RestartButtonReloadsReadyRun()
        {
            // Load the minigame directly so this smoke test focuses on restart wiring.
            SceneManager.LoadScene("Minigame");
            yield return null;

            // Wait a second frame so LevelManager.Start initializes gameplay dependencies.
            yield return null;

            // Force a win through the real Update path so the end screen becomes available.
            LevelManager levelManager = GameObject.Find("Level Manager")?.GetComponent<LevelManager>();
            PlayerSquad playerSquad = GameObject.Find("Player Squad")?.GetComponent<PlayerSquad>();
            Assert.IsNotNull(levelManager);
            Assert.IsNotNull(playerSquad);
            levelManager.BeginLevel();
            playerSquad.transform.position = new Vector3(playerSquad.transform.position.x, playerSquad.transform.position.y, 999f);
            yield return null;

            // Invoke the same Restart button listener exposed on the completed-run panel.
            Button restartButton = GameObject.Find("Restart Button")?.GetComponent<Button>();
            Assert.IsNotNull(restartButton);
            restartButton.onClick.Invoke();
            yield return null;

            // Reloading should leave the player in a fresh Minigame scene with a ready LevelManager.
            Assert.AreEqual("Minigame", SceneManager.GetActiveScene().name);
            LevelManager restartedLevelManager = GameObject.Find("Level Manager")?.GetComponent<LevelManager>();
            Assert.IsNotNull(restartedLevelManager);
            Assert.AreEqual(LevelState.Ready, restartedLevelManager.State);
        }

        [UnityTest]
        public IEnumerator HeroesScene_LevelUpAndEquipButtonsPersistHeroState()
        {
            // Seed a local save with two owned heroes so the dedicated Hero screen has work to do.
            SaveGameData saveData = new()
            {
                coins = HeroProgression.GetManualLevelUpCoinCost(1),
                hqLevel = 2,
                unlockedMinigameLevel = 2,
                currentMissionLevel = 2,
                highestUnlockedMissionLevel = 2
            };
            HeroInventory.GrantHero(saveData, HeroCatalog.FirstWinHeroId);
            HeroInventory.GrantHero(saveData, HeroCatalog.HqLevelTwoHeroId);
            HeroInventory.EquipHero(saveData, HeroCatalog.FirstWinHeroId);
            SaveGameManager.Save(saveData);

            // Load the Hero scene after seeding so its bootstrap reads the prepared save.
            SceneManager.LoadScene("Heroes");
            yield return null;

            // The dedicated Hero scene should render the owned-hero list and equipped hero state.
            Assert.AreEqual("Heroes", SceneManager.GetActiveScene().name);
            Text heroListText = GameObject.Find("Hero List Text")?.GetComponent<Text>();
            Assert.IsNotNull(heroListText);
            StringAssert.Contains("Mira Vanguard", heroListText.text);
            StringAssert.Contains("Dax Medic", heroListText.text);

            // The real Level button listener should spend coins and persist a level increase.
            Button levelUpButton = GameObject.Find("Level Up Button")?.GetComponent<Button>();
            Assert.IsNotNull(levelUpButton);
            Assert.IsTrue(levelUpButton.interactable);
            levelUpButton.onClick.Invoke();
            yield return null;

            // Reload from disk so the assertion proves the Hero screen saved the manual level-up.
            SaveGameData leveledData = SaveGameManager.Load();
            Assert.AreEqual(0, leveledData.coins);
            Assert.AreEqual(2, HeroInventory.GetHeroLevel(leveledData, HeroCatalog.FirstWinHeroId));

            // The real Equip button listener should cycle to the second owned hero and persist it.
            Button equipNextButton = GameObject.Find("Equip Next Button")?.GetComponent<Button>();
            Assert.IsNotNull(equipNextButton);
            Assert.IsTrue(equipNextButton.interactable);
            equipNextButton.onClick.Invoke();
            yield return null;

            // Equipment state must survive beyond the current in-memory scene object.
            SaveGameData equippedData = SaveGameManager.Load();
            Assert.AreEqual(HeroCatalog.HqLevelTwoHeroId, equippedData.equippedHeroId);
        }

        [UnityTest]
        public IEnumerator HeroesScene_HeaderAndHeroListStayInsideCanvasWithoutOverlap()
        {
            // Seed two upgraded heroes so the Hero list produces the multi-line text that exposed clipping.
            SaveGameData saveData = new()
            {
                coins = HeroProgression.GetManualLevelUpCoinCost(4),
                hqLevel = 2,
                unlockedMinigameLevel = 2,
                currentMissionLevel = 2,
                highestUnlockedMissionLevel = 2
            };
            HeroInventory.GrantHero(saveData, HeroCatalog.FirstWinHeroId);
            HeroInventory.GrantHero(saveData, HeroCatalog.HqLevelTwoHeroId);
            HeroInventory.EquipHero(saveData, HeroCatalog.HqLevelTwoHeroId);
            HeroProgression.AddXpToEquippedHero(saveData, 240);
            SaveGameManager.Save(saveData);

            // Load the Hero screen after seeding so the runtime bootstrap builds the real HUD layout.
            SceneManager.LoadScene("Heroes");
            yield return null;

            // Canvas corners establish the visible bounds used by the generated overlay.
            RectTransform canvasRect = GameObject.Find("Hero HUD Canvas")?.GetComponent<RectTransform>();
            Assert.IsNotNull(canvasRect);
            Vector3[] canvasCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasCorners);
            float canvasLeftEdge = canvasCorners[0].x;
            float canvasTopEdge = canvasCorners[1].y;

            // These top HUD labels should all stay inside the left/top canvas edges.
            string[] topHudLabels =
            {
                "Title Text",
                "Coins Text",
                "Equipped Text",
                "Hero List Text"
            };

            foreach (string labelName in topHudLabels)
            {
                // Read actual generated rectangles instead of duplicating layout constants in the test.
                RectTransform labelRect = GameObject.Find(labelName)?.GetComponent<RectTransform>();
                Assert.IsNotNull(labelRect, labelName);
                Vector3[] labelCorners = new Vector3[4];
                labelRect.GetWorldCorners(labelCorners);

                // Small tolerance avoids float-noise failures while still catching visible clipping.
                Assert.GreaterOrEqual(labelCorners[0].x, canvasLeftEdge - 0.5f, $"{labelName} left edge should stay inside the Hero HUD canvas.");
                Assert.LessOrEqual(labelCorners[1].y, canvasTopEdge + 0.5f, $"{labelName} top edge should stay inside the Hero HUD canvas.");
            }

            // The hero list should begin below the equipped summary so their text cannot draw over each other.
            RectTransform equippedRect = GameObject.Find("Equipped Text")?.GetComponent<RectTransform>();
            RectTransform heroListRect = GameObject.Find("Hero List Text")?.GetComponent<RectTransform>();
            Assert.IsNotNull(equippedRect);
            Assert.IsNotNull(heroListRect);
            Vector3[] equippedCorners = new Vector3[4];
            Vector3[] heroListCorners = new Vector3[4];
            equippedRect.GetWorldCorners(equippedCorners);
            heroListRect.GetWorldCorners(heroListCorners);
            Assert.Less(heroListCorners[1].y, equippedCorners[0].y, "Hero list should sit below the equipped summary.");
        }
    }
}
