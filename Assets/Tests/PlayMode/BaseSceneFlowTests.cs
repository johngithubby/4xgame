using System;
using System.Collections;
using System.IO;
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
    }
}
