using System;
using System.Collections;
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
            Assert.AreEqual(2, completedData.unlockedMinigameLevel);
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
            StringAssert.Contains("Mira Vanguard", rewardText.text);

            // Reload from disk so this proves LevelManager persisted the scene-earned rewards.
            SaveGameData rewardedData = SaveGameManager.Load();
            Assert.AreEqual(PlayerProgression.MinigameWinCoins, rewardedData.coins);
            Assert.IsTrue(HeroInventory.OwnsHero(rewardedData, HeroCatalog.FirstWinHeroId));

            Button baseButton = GameObject.Find("Base Button")?.GetComponent<Button>();
            Assert.IsNotNull(baseButton);
            Assert.IsTrue(baseButton.gameObject.activeInHierarchy);
            baseButton.onClick.Invoke();
            yield return null;

            // Returning to Base should rebuild the local hub instead of leaving the player in the minigame.
            Assert.AreEqual("Base", SceneManager.GetActiveScene().name);
            Assert.IsNotNull(GameObject.Find("Base HUD Canvas"));
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
                unlockedMinigameLevel = 2
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
    }
}
