using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LaneSurvivor.Base;
using LaneSurvivor.Gameplay;
using LaneSurvivor.Heroes;
using LaneSurvivor.Progression;
using LaneSurvivor.Rendering;
using LaneSurvivor.Retention;
using LaneSurvivor.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LaneSurvivor.Tests.PlayMode
{
    public sealed class BaseSceneFlowTests
    {
        private string tempSavePath;

        [Test]
        public void UpgradeCompletionSoundProfiles_UseDistinctGeneratedSignatures()
        {
            // Each building family should synthesize a different local completion chime.
            UpgradeCompletionSoundProfile[] profiles =
            {
                UpgradeCompletionSoundProfile.Hq,
                UpgradeCompletionSoundProfile.BioLab,
                UpgradeCompletionSoundProfile.Hangar,
                UpgradeCompletionSoundProfile.TrainingFacility,
                UpgradeCompletionSoundProfile.LivingQuarters
            };
            HashSet<string> signatures = new();

            foreach (UpgradeCompletionSoundProfile profile in profiles)
            {
                // Signature uniqueness catches accidental reuse of one shared tone recipe.
                Assert.IsTrue(signatures.Add(UpgradeCompletionSound.GetSoundSignature(profile)), $"{profile} should use a unique completion sound signature.");
            }
        }

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
            Button missionOneButton = GameObject.Find("Mission 1 Button")?.GetComponent<Button>();
            Button missionTwoButton = GameObject.Find("Mission 2 Button")?.GetComponent<Button>();
            Assert.IsNotNull(missionOneButton);
            Assert.IsNotNull(missionTwoButton);
            Assert.AreEqual(">M1", missionOneButton.GetComponentInChildren<Text>()?.text);
            Assert.IsFalse(missionTwoButton.interactable);

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
        public IEnumerator BaseScene_CreditsButtonExpandsWithoutLeftStatusLabels()
        {
            // Wait one frame so the runtime-built Base HUD exists before inspecting its RectTransforms.
            yield return null;

            // The canvas bounds give the assertion the same left edge the player sees in Game view.
            RectTransform canvasRect = GameObject.Find("Base HUD Canvas")?.GetComponent<RectTransform>();
            Assert.IsNotNull(canvasRect);
            Vector3[] canvasCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasCorners);
            float canvasLeftEdge = canvasCorners[0].x;
            float canvasTopEdge = canvasCorners[1].y;

            // These former left-side labels should no longer exist in the generated Base HUD.
            string[] removedLeftHudLabels =
            {
                "Coins Text",
                "HQ Text",
                "Timer Text",
                "Hero Text",
                "Mission Panel Title Text",
                "Mission Panel Text",
                "Objective Text"
            };

            foreach (string labelName in removedLeftHudLabels)
            {
                // The replacement design uses a collapsible credits control instead of persistent white text.
                Assert.IsNull(GameObject.Find(labelName), $"{labelName} should not be generated on the left side of the Base HUD.");
            }

            // The Credits button should be the only top-left status entry point.
            Button creditsButton = GameObject.Find("Credits Button")?.GetComponent<Button>();
            Assert.IsNotNull(creditsButton);
            Text creditsButtonText = creditsButton.GetComponentInChildren<Text>();
            Assert.IsNotNull(creditsButtonText);
            Assert.AreEqual("Credits: 0", creditsButtonText.text);
            AssertColorApproximately(Color.black, creditsButtonText.color);

            // Button geometry should stay inside the same mobile-reference canvas bounds.
            RectTransform creditsButtonRect = creditsButton.GetComponent<RectTransform>();
            Vector3[] creditsButtonCorners = GetRectCorners(creditsButtonRect);
            Assert.GreaterOrEqual(creditsButtonCorners[0].x, canvasLeftEdge - 0.5f, "Credits button left edge should stay inside the Base HUD canvas.");
            Assert.LessOrEqual(creditsButtonCorners[1].y, canvasTopEdge + 0.5f, "Credits button top edge should stay inside the Base HUD canvas.");

            // Inactive children cannot be found globally, so inspect the generated panel through the canvas hierarchy.
            Transform creditsDetailTransform = canvasRect.transform.Find("Credits Detail Panel");
            Assert.IsNotNull(creditsDetailTransform);
            Assert.IsFalse(creditsDetailTransform.gameObject.activeSelf);

            // Tapping the Credits button should expand the summary with the requested local base values.
            creditsButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(creditsDetailTransform.gameObject.activeSelf);

            Text creditsDetailText = creditsDetailTransform.Find("Credits Detail Text")?.GetComponent<Text>();
            Assert.IsNotNull(creditsDetailText);
            StringAssert.Contains("Coins: 0", creditsDetailText.text);
            StringAssert.Contains("HQ Level: 1", creditsDetailText.text);
            StringAssert.Contains("Bio Lab: 1", creditsDetailText.text);
            StringAssert.Contains("Hangar: 1", creditsDetailText.text);
            StringAssert.Contains("Training: 1", creditsDetailText.text);
            StringAssert.Contains("Living Qtrs: 1", creditsDetailText.text);
            StringAssert.Contains("HQ Upgrade: Need 75c", creditsDetailText.text);
            StringAssert.Contains("Bio Upgrade: Ready", creditsDetailText.text);
            StringAssert.Contains("Hangar Upgrade: Ready", creditsDetailText.text);
            StringAssert.Contains("Training Upgrade: Ready", creditsDetailText.text);
            StringAssert.Contains("LQ Upgrade: Ready", creditsDetailText.text);
            AssertColorApproximately(Color.black, creditsDetailText.color);

            // Tapping again should collapse the details without requiring a save or scene refresh.
            creditsButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(creditsDetailTransform.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator BaseScene_HqLevelChangesReferenceModelHeightAndGlowScaffold()
        {
            // Seed a double-digit HQ level because height-only reference visuals are the regression target.
            SaveGameManager.Save(new SaveGameData
            {
                hqLevel = 14
            });

            // Reload Base after seeding so BaseSceneBootstrap builds the level-fourteen HQ from saved data.
            SceneManager.LoadScene("Base");
            yield return null;

            // The HQ component should mirror the saved level and own its restored pentagon body child.
            HQBuilding hqBuilding = GameObject.Find("HQ Building")?.GetComponent<HQBuilding>();
            Assert.IsNotNull(hqBuilding);
            Assert.AreEqual(14, hqBuilding.Level);
            Transform hqBody = hqBuilding.transform.Find("HQ Visual Root/HQ Body");
            Assert.IsNotNull(hqBody);

            // The hidden HQ scaffold should keep five unique X/Z plan vertices for fallback alignment.
            Mesh hqBodyMesh = hqBody.GetComponent<MeshFilter>()?.sharedMesh;
            Assert.IsNotNull(hqBodyMesh);
            Assert.AreEqual(5, CountUniquePlanVertices(hqBodyMesh));
            Assert.IsFalse(hqBody.GetComponent<Renderer>().enabled);

            // Higher HQ levels should keep the footprint fixed and grow only in height.
            Assert.AreEqual(HQBuilding.CalculateVisualDiameter(14), hqBody.localScale.x, 0.001f);
            Assert.AreEqual(HQBuilding.CalculateVisualHeight(14), hqBody.localScale.y, 0.001f);
            Assert.Greater(hqBody.localScale.y, HQBuilding.CalculateVisualHeight(1));
            Assert.AreEqual(HQBuilding.CalculateVisualDiameter(1), hqBody.localScale.x, 0.001f);
            Assert.AreEqual(HQBuilding.CalculateVisualDiameter(1), hqBody.localScale.z, 0.001f);
            Assert.LessOrEqual(HQBuilding.CalculateVisualHeight(14) - HQBuilding.CalculateVisualHeight(1), 0.27f);

            // The reference-textured HQ is the visible source of truth for the generated concept image.
            Transform referenceModel = hqBuilding.transform.Find("HQ Visual Root/HQ Reference Model");
            Assert.IsNotNull(referenceModel);
            Assert.AreEqual(HQBuilding.CalculateReferenceModelWidth(14), referenceModel.localScale.x, 0.001f);
            Assert.AreEqual(HQBuilding.CalculateReferenceModelHeight(14), referenceModel.localScale.y, 0.001f);
            Assert.AreEqual(HQBuilding.CalculateReferenceModelWidth(1), referenceModel.localScale.x, 0.001f);
            Assert.Greater(referenceModel.localScale.y, HQBuilding.CalculateReferenceModelHeight(1));
            Assert.LessOrEqual(referenceModel.localScale.y - HQBuilding.CalculateReferenceModelHeight(1), 0.35f);
            MeshRenderer referenceRenderer = referenceModel.GetComponent<MeshRenderer>();
            Assert.IsNotNull(referenceRenderer);
            Assert.IsTrue(referenceRenderer.enabled);
            Assert.IsNotNull(referenceRenderer.sharedMaterial?.mainTexture);
            // The HQ reference model should now read warmer than the cool science/industrial buildings.
            Color hqReferenceTint = GetMaterialColor(referenceRenderer.sharedMaterial);
            Assert.Greater(hqReferenceTint.r, hqReferenceTint.b);
            Assert.Greater(hqReferenceTint.g, hqReferenceTint.b);

            // The HQ upgrade symbol should exist but stay hidden until the player taps the HQ.
            Transform upgradeSymbol = hqBuilding.transform.Find("HQ Visual Root/HQ Upgrade Symbol");
            Assert.IsNotNull(upgradeSymbol);
            Assert.IsFalse(upgradeSymbol.gameObject.activeSelf);

            // The reference image already contains the HQ sign, so the old generated label stays hidden.
            TextMesh hqLabel = hqBuilding.transform.Find("HQ Visual Root/HQ Label")?.GetComponent<TextMesh>();
            Assert.IsNotNull(hqLabel);
            Assert.IsFalse(hqLabel.gameObject.activeSelf);
            Assert.AreEqual(string.Empty, hqLabel.text);

            // Detail rows no longer grow with upgrades; the new look is height-only.
            Transform detailRoot = hqBuilding.transform.Find("HQ Visual Root/HQ Detail Root");
            Assert.IsNotNull(detailRoot);
            Assert.AreEqual(0, HQBuilding.CalculateDetailRows(14));
            Assert.AreEqual(0, CountActiveChildren(detailRoot));

            // The base floor should also be a simple unoutlined slab rather than a visible pentagon footprint.
            GameObject baseGround = GameObject.Find("Base Ground");
            Assert.IsNotNull(baseGround);
            Mesh baseGroundMesh = baseGround.GetComponent<MeshFilter>()?.sharedMesh;
            Assert.IsNotNull(baseGroundMesh);
            Assert.AreEqual(4, CountUniquePlanVertices(baseGroundMesh));
            Assert.IsNull(GameObject.Find("Future Wall Space 1"));
            Assert.IsNull(GameObject.Find("Future Moat Space 1"));
            Assert.IsNotNull(GameObject.Find("Future Gate Space"));
            Assert.IsNotNull(GameObject.Find("Future Resource Drop-Off Opening"));
            GameObject labPad = GameObject.Find("Future Lab Pad");
            Assert.IsNotNull(labPad);
            Renderer hangarPadRenderer = GameObject.Find("Future Hangar Pad")?.GetComponent<Renderer>();
            Assert.IsNotNull(hangarPadRenderer);
            Assert.IsFalse(hangarPadRenderer.enabled);
            GameObject hangarPad = GameObject.Find("Future Hangar Pad");
            Assert.IsNotNull(hangarPad);
            GameObject trainingPad = GameObject.Find("Future Training Pad");
            Assert.IsNotNull(trainingPad);
            Renderer trainingPadRenderer = trainingPad.GetComponent<Renderer>();
            Assert.IsNotNull(trainingPadRenderer);
            Assert.IsFalse(trainingPadRenderer.enabled);
            GameObject livingQuartersPad = GameObject.Find("Future Living Quarters Pad");
            Assert.IsNotNull(livingQuartersPad);
            Renderer livingQuartersPadRenderer = livingQuartersPad.GetComponent<Renderer>();
            Assert.IsNotNull(livingQuartersPadRenderer);
            Assert.IsFalse(livingQuartersPadRenderer.enabled);
            Assert.IsNull(GameObject.Find("Future Hangar Pad Label"));
            Assert.IsNull(GameObject.Find("Future Training Pad Label"));
            Assert.IsNull(GameObject.Find("Future Living Quarters Pad Label"));

            // The lab and hangar should no longer crowd the HQ; their logical pads should follow the same slots.
            BioLabBuilding bioLab = GameObject.Find("Bio Lab")?.GetComponent<BioLabBuilding>();
            UpgradeableFacilityBuilding hangar = GameObject.Find("Hangar")?.GetComponent<UpgradeableFacilityBuilding>();
            Assert.IsNotNull(bioLab);
            Assert.IsNotNull(hangar);
            Assert.Less(bioLab.transform.position.x, -3f);
            Assert.Greater(hangar.transform.position.x, 3f);
            Assert.Greater(Mathf.Abs(bioLab.transform.position.x - hqBuilding.transform.position.x), 3f);
            Assert.Greater(Mathf.Abs(hangar.transform.position.x - hqBuilding.transform.position.x), 3f);
            Assert.AreEqual(bioLab.transform.position.x, labPad.transform.position.x, 0.001f);
            Assert.AreEqual(hangar.transform.position.x, hangarPad.transform.position.x, 0.001f);

            // Training should sit diagonally behind the HQ so its building is not hidden directly under the restored HQ.
            UpgradeableFacilityBuilding trainingFacility = GameObject.Find("Training Facility")?.GetComponent<UpgradeableFacilityBuilding>();
            Assert.IsNotNull(trainingFacility);
            Vector3 trainingOffsetFromHq = trainingFacility.transform.position - hqBuilding.transform.position;
            Assert.Greater(trainingOffsetFromHq.z, 2.8f);
            Assert.Greater(Mathf.Abs(trainingOffsetFromHq.x), 1.4f);
            Assert.Greater(trainingOffsetFromHq.magnitude, 3.3f);

            // Living quarters should sit in a rear-left residential slot with comfortable spacing from every existing building.
            UpgradeableFacilityBuilding livingQuarters = GameObject.Find("Living Quarters")?.GetComponent<UpgradeableFacilityBuilding>();
            Assert.IsNotNull(livingQuarters);
            Assert.Less(livingQuarters.transform.position.x, -2f);
            Assert.Greater(livingQuarters.transform.position.z, 4f);
            Assert.Greater(Vector3.Distance(livingQuarters.transform.position, hqBuilding.transform.position), 4.8f);
            Assert.Greater(Vector3.Distance(livingQuarters.transform.position, bioLab.transform.position), 3.5f);
            Assert.Greater(Vector3.Distance(livingQuarters.transform.position, hangar.transform.position), 5.0f);
            Assert.Greater(Vector3.Distance(livingQuarters.transform.position, trainingFacility.transform.position), 3.5f);
            Assert.AreEqual(livingQuarters.transform.position.x, livingQuartersPad.transform.position.x, 0.001f);
            Assert.AreEqual(livingQuarters.transform.position.z, livingQuartersPad.transform.position.z, 0.001f);
        }

        [UnityTest]
        public IEnumerator BaseScene_HqReferenceModelClickShowsSymbolThenArrowStartsTimedUpgradeWhenAffordable()
        {
            // Seed exact upgrade credits so tapping the visible HQ can reveal an affordable symbol.
            SaveGameManager.Save(new SaveGameData
            {
                coins = PlayerProgression.GetHqUpgradeCost(1),
                hqLevel = 1
            });

            // Reload Base after seeding so the generated HQ owns the start-upgrade callback.
            SceneManager.LoadScene("Base");
            yield return null;

            // The click point should come from the player-visible reference model, not the hidden scaffold.
            HQBuilding hqBuilding = GameObject.Find("HQ Building")?.GetComponent<HQBuilding>();
            Assert.IsNotNull(hqBuilding);
            Transform referenceModel = hqBuilding.transform.Find("HQ Visual Root/HQ Reference Model");
            Assert.IsNotNull(referenceModel);
            Renderer referenceRenderer = referenceModel.GetComponent<Renderer>();
            Assert.IsNotNull(referenceRenderer);

            // Project the current rendered HQ center through the real Base camera so zoom/pan-sensitive hit testing runs.
            Camera baseCamera = Camera.main;
            Assert.IsNotNull(baseCamera);
            Vector3 screenPoint = baseCamera.WorldToScreenPoint(referenceRenderer.bounds.center);
            Assert.Greater(screenPoint.z, 0f);

            // Tapping the visible HQ should only reveal the up-arrow symbol, not start the timer directly.
            bool handled = hqBuilding.TryHandleBuildingClick(new Vector2(screenPoint.x, screenPoint.y));
            yield return null;
            SaveGameData symbolOnlyData = SaveGameManager.Load();
            Assert.IsTrue(handled);
            Assert.AreEqual(PlayerProgression.GetHqUpgradeCost(1), symbolOnlyData.coins);
            Assert.IsFalse(symbolOnlyData.hqUpgradeInProgress);
            Assert.IsTrue(hqBuilding.IsUpgradeSymbolVisible);
            Assert.IsTrue(hqBuilding.CanAffordDisplayedUpgrade);

            // The affordable visible symbol should be green, matching the bio-lab upgrade affordance rule.
            Transform symbolStem = hqBuilding.transform.Find("HQ Visual Root/HQ Upgrade Symbol/HQ Upgrade Symbol Stem");
            Renderer symbolStemRenderer = symbolStem?.GetComponent<Renderer>();
            Assert.IsNotNull(symbolStemRenderer);
            AssertFlatSymbolMesh(symbolStem, "HQ upgrade symbol stem");
            AssertFlatSymbolMesh(hqBuilding.transform.Find("HQ Visual Root/HQ Upgrade Symbol/HQ Upgrade Symbol Arrow Head"), "HQ upgrade symbol head");
            Color symbolColor = GetMaterialColor(symbolStemRenderer.sharedMaterial);
            Assert.Greater(symbolColor.g, symbolColor.r);
            Assert.Greater(symbolColor.g, symbolColor.b);

            // Clicking the up arrow should spend credits and start the saved timer.
            Transform upgradeSymbol = hqBuilding.transform.Find("HQ Visual Root/HQ Upgrade Symbol");
            Assert.IsNotNull(upgradeSymbol);
            Vector3 symbolScreenPoint = baseCamera.WorldToScreenPoint(upgradeSymbol.position);
            Assert.Greater(symbolScreenPoint.z, 0f);
            bool symbolHandled = hqBuilding.TryHandleBuildingClick(new Vector2(symbolScreenPoint.x, symbolScreenPoint.y));
            yield return null;
            SaveGameData startedData = SaveGameManager.Load();
            Assert.IsTrue(symbolHandled);
            Assert.AreEqual(0, startedData.coins);
            Assert.IsTrue(startedData.hqUpgradeInProgress);
            Assert.AreEqual(1, startedData.hqLevel);
            Assert.AreEqual(PlayerProgression.GetHqUpgradeDurationSeconds(1), startedData.hqUpgradeDurationSeconds);
            Assert.IsFalse(hqBuilding.IsUpgradeSymbolVisible);
            Assert.IsTrue(hqBuilding.IsProgressVisible);

            // The visible Base feedback should prove the symbol click started the upgrade.
            Text statusText = GameObject.Find("Status Text")?.GetComponent<Text>();
            Assert.IsNotNull(statusText);
            StringAssert.Contains("HQ upgrade started", statusText.text);
        }

        [UnityTest]
        public IEnumerator BaseScene_HqRunningUpgradeShowsCircularProgressIcon()
        {
            // Seed a valid in-progress HQ timer so the scene should render the same circular progress treatment as the lab.
            int hqLevelWithStableTimer = 4;
            int hqDurationSeconds = PlayerProgression.GetHqUpgradeDurationSeconds(hqLevelWithStableTimer);
            SaveGameManager.Save(new SaveGameData
            {
                hqLevel = hqLevelWithStableTimer,
                hqUpgradeInProgress = true,
                hqUpgradeStartedUtcTicks = DateTime.UtcNow.AddSeconds(-hqDurationSeconds * 0.5f).Ticks,
                hqUpgradeDurationSeconds = hqDurationSeconds
            });

            // Reload Base so bootstrap applies the saved timer to the generated HQ.
            SceneManager.LoadScene("Base");
            yield return null;

            // The HQ should hide its start arrow while a circular progress icon tracks the active saved timer.
            HQBuilding hqBuilding = GameObject.Find("HQ Building")?.GetComponent<HQBuilding>();
            Assert.IsNotNull(hqBuilding);
            Assert.IsFalse(hqBuilding.IsUpgradeSymbolVisible);
            Assert.IsTrue(hqBuilding.IsProgressVisible);
            Assert.Greater(hqBuilding.ProgressFillAmount, 0.35f);
            Assert.Less(hqBuilding.ProgressFillAmount, 0.90f);

            // The generated progress icon should use the same background-disc plus dynamic fill shape as the bio lab.
            Transform progressRoot = hqBuilding.transform.Find("HQ Visual Root/HQ Progress Icon");
            Assert.IsNotNull(progressRoot);
            Assert.IsNotNull(progressRoot.Find("HQ Progress Back Disc")?.GetComponent<Renderer>());
            MeshFilter fillMeshFilter = progressRoot.Find("HQ Progress Fill")?.GetComponent<MeshFilter>();
            Assert.IsNotNull(fillMeshFilter);
            Assert.IsNotNull(fillMeshFilter.sharedMesh);
            Assert.Greater(fillMeshFilter.sharedMesh.vertexCount, 0);
        }

        [UnityTest]
        public IEnumerator BaseScene_ClickingElsewhereHidesBuildingUpgradeArrows()
        {
            // Wait one frame so BaseSceneBootstrap can build the runtime buildings and camera.
            yield return null;

            // Use the real components so the same screen-hit logic runs for both building types.
            HQBuilding hqBuilding = GameObject.Find("HQ Building")?.GetComponent<HQBuilding>();
            BioLabBuilding bioLab = GameObject.Find("Bio Lab")?.GetComponent<BioLabBuilding>();
            UpgradeableFacilityBuilding hangar = GameObject.Find("Hangar")?.GetComponent<UpgradeableFacilityBuilding>();
            UpgradeableFacilityBuilding trainingFacility = GameObject.Find("Training Facility")?.GetComponent<UpgradeableFacilityBuilding>();
            UpgradeableFacilityBuilding livingQuarters = GameObject.Find("Living Quarters")?.GetComponent<UpgradeableFacilityBuilding>();
            Camera baseCamera = Camera.main;
            Assert.IsNotNull(hqBuilding);
            Assert.IsNotNull(bioLab);
            Assert.IsNotNull(hangar);
            Assert.IsNotNull(trainingFacility);
            Assert.IsNotNull(livingQuarters);
            Assert.IsNotNull(baseCamera);

            // Tapping the HQ reveals the HQ arrow.
            Renderer hqReferenceRenderer = hqBuilding.transform.Find("HQ Visual Root/HQ Reference Model")?.GetComponent<Renderer>();
            Assert.IsNotNull(hqReferenceRenderer);
            Vector3 hqScreenPoint = baseCamera.WorldToScreenPoint(hqReferenceRenderer.bounds.center);
            Assert.IsTrue(hqBuilding.TryHandleBuildingClick(new Vector2(hqScreenPoint.x, hqScreenPoint.y)));
            Assert.IsTrue(hqBuilding.IsUpgradeSymbolVisible);
            Assert.IsFalse(bioLab.IsUpgradeSymbolVisible);
            Assert.IsFalse(hangar.IsUpgradeSymbolVisible);
            Assert.IsFalse(trainingFacility.IsUpgradeSymbolVisible);
            Assert.IsFalse(livingQuarters.IsUpgradeSymbolVisible);

            // Tapping the lab is a different building click, so the HQ arrow closes and the lab arrow opens.
            Vector3 labWorldPoint = bioLab.transform.position + new Vector3(0f, BioLabBuilding.CalculateVisualHeight(bioLab.Level) * 0.55f, 0f);
            Vector3 labScreenPoint = baseCamera.WorldToScreenPoint(labWorldPoint);
            Assert.IsFalse(hqBuilding.TryHandleBuildingClick(new Vector2(labScreenPoint.x, labScreenPoint.y)));
            Assert.IsTrue(bioLab.TryHandleWorldClick(new Vector2(labScreenPoint.x, labScreenPoint.y)));
            Assert.IsFalse(hangar.TryHandleBuildingClick(new Vector2(labScreenPoint.x, labScreenPoint.y)));
            Assert.IsFalse(trainingFacility.TryHandleBuildingClick(new Vector2(labScreenPoint.x, labScreenPoint.y)));
            Assert.IsFalse(hqBuilding.IsUpgradeSymbolVisible);
            Assert.IsTrue(bioLab.IsUpgradeSymbolVisible);
            Assert.IsFalse(hangar.IsUpgradeSymbolVisible);
            Assert.IsFalse(trainingFacility.IsUpgradeSymbolVisible);
            Assert.IsFalse(livingQuarters.IsUpgradeSymbolVisible);

            // Tapping the hangar should close the lab arrow and open only the hangar arrow.
            Renderer hangarReferenceRenderer = hangar.transform.Find("Hangar Visual Root/Hangar Reference Model")?.GetComponent<Renderer>();
            Assert.IsNotNull(hangarReferenceRenderer);
            Vector3 hangarScreenPoint = baseCamera.WorldToScreenPoint(hangarReferenceRenderer.bounds.center);
            Assert.IsFalse(hqBuilding.TryHandleBuildingClick(new Vector2(hangarScreenPoint.x, hangarScreenPoint.y)));
            Assert.IsFalse(bioLab.TryHandleWorldClick(new Vector2(hangarScreenPoint.x, hangarScreenPoint.y)));
            Assert.IsTrue(hangar.TryHandleBuildingClick(new Vector2(hangarScreenPoint.x, hangarScreenPoint.y)));
            Assert.IsFalse(trainingFacility.TryHandleBuildingClick(new Vector2(hangarScreenPoint.x, hangarScreenPoint.y)));
            Assert.IsFalse(hqBuilding.IsUpgradeSymbolVisible);
            Assert.IsFalse(bioLab.IsUpgradeSymbolVisible);
            Assert.IsTrue(hangar.IsUpgradeSymbolVisible);
            Assert.IsFalse(trainingFacility.IsUpgradeSymbolVisible);
            Assert.IsFalse(livingQuarters.IsUpgradeSymbolVisible);

            // Tapping the training facility should close the hangar arrow and open only the training arrow.
            Renderer trainingReferenceRenderer = trainingFacility.transform.Find("Training Facility Visual Root/Training Facility Reference Model")?.GetComponent<Renderer>();
            Assert.IsNotNull(trainingReferenceRenderer);
            Vector3 trainingScreenPoint = baseCamera.WorldToScreenPoint(trainingReferenceRenderer.bounds.center);
            Assert.IsFalse(hqBuilding.TryHandleBuildingClick(new Vector2(trainingScreenPoint.x, trainingScreenPoint.y)));
            Assert.IsFalse(bioLab.TryHandleWorldClick(new Vector2(trainingScreenPoint.x, trainingScreenPoint.y)));
            Assert.IsFalse(hangar.TryHandleBuildingClick(new Vector2(trainingScreenPoint.x, trainingScreenPoint.y)));
            Assert.IsTrue(trainingFacility.TryHandleBuildingClick(new Vector2(trainingScreenPoint.x, trainingScreenPoint.y)));
            Assert.IsFalse(livingQuarters.TryHandleBuildingClick(new Vector2(trainingScreenPoint.x, trainingScreenPoint.y)));
            Assert.IsFalse(hqBuilding.IsUpgradeSymbolVisible);
            Assert.IsFalse(bioLab.IsUpgradeSymbolVisible);
            Assert.IsFalse(hangar.IsUpgradeSymbolVisible);
            Assert.IsTrue(trainingFacility.IsUpgradeSymbolVisible);
            Assert.IsFalse(livingQuarters.IsUpgradeSymbolVisible);

            // Tapping living quarters should close the training arrow and open only the living-quarters arrow.
            Renderer livingQuartersReferenceRenderer = livingQuarters.transform.Find("Living Quarters Visual Root/Living Quarters Reference Model")?.GetComponent<Renderer>();
            Assert.IsNotNull(livingQuartersReferenceRenderer);
            Vector3 livingQuartersScreenPoint = baseCamera.WorldToScreenPoint(livingQuartersReferenceRenderer.bounds.center);
            Assert.IsFalse(hqBuilding.TryHandleBuildingClick(new Vector2(livingQuartersScreenPoint.x, livingQuartersScreenPoint.y)));
            Assert.IsFalse(bioLab.TryHandleWorldClick(new Vector2(livingQuartersScreenPoint.x, livingQuartersScreenPoint.y)));
            Assert.IsFalse(hangar.TryHandleBuildingClick(new Vector2(livingQuartersScreenPoint.x, livingQuartersScreenPoint.y)));
            Assert.IsFalse(trainingFacility.TryHandleBuildingClick(new Vector2(livingQuartersScreenPoint.x, livingQuartersScreenPoint.y)));
            Assert.IsTrue(livingQuarters.TryHandleBuildingClick(new Vector2(livingQuartersScreenPoint.x, livingQuartersScreenPoint.y)));
            Assert.IsFalse(hqBuilding.IsUpgradeSymbolVisible);
            Assert.IsFalse(bioLab.IsUpgradeSymbolVisible);
            Assert.IsFalse(hangar.IsUpgradeSymbolVisible);
            Assert.IsFalse(trainingFacility.IsUpgradeSymbolVisible);
            Assert.IsTrue(livingQuarters.IsUpgradeSymbolVisible);

            // A later tap on empty map space should dismiss every building popup arrow.
            Vector2 emptyMapScreenPoint = new(10f, 10f);
            Assert.IsFalse(hqBuilding.TryHandleBuildingClick(emptyMapScreenPoint));
            Assert.IsFalse(bioLab.TryHandleWorldClick(emptyMapScreenPoint));
            Assert.IsFalse(hangar.TryHandleBuildingClick(emptyMapScreenPoint));
            Assert.IsFalse(trainingFacility.TryHandleBuildingClick(emptyMapScreenPoint));
            Assert.IsFalse(livingQuarters.TryHandleBuildingClick(emptyMapScreenPoint));
            Assert.IsFalse(hqBuilding.IsUpgradeSymbolVisible);
            Assert.IsFalse(bioLab.IsUpgradeSymbolVisible);
            Assert.IsFalse(hangar.IsUpgradeSymbolVisible);
            Assert.IsFalse(trainingFacility.IsUpgradeSymbolVisible);
            Assert.IsFalse(livingQuarters.IsUpgradeSymbolVisible);

            // The Credits toggle is a HUD action too, so it should dismiss popups before expanding details.
            Assert.IsTrue(hqBuilding.TryHandleBuildingClick(new Vector2(hqScreenPoint.x, hqScreenPoint.y)));
            Assert.IsTrue(hqBuilding.IsUpgradeSymbolVisible);
            Button creditsButton = GameObject.Find("Credits Button")?.GetComponent<Button>();
            Assert.IsNotNull(creditsButton);
            creditsButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(hqBuilding.IsUpgradeSymbolVisible);
            Assert.IsFalse(bioLab.IsUpgradeSymbolVisible);
            Assert.IsFalse(hangar.IsUpgradeSymbolVisible);
            Assert.IsFalse(trainingFacility.IsUpgradeSymbolVisible);
            Assert.IsFalse(livingQuarters.IsUpgradeSymbolVisible);

            // Other HUD clicks are also elsewhere, so they should close a newly opened world arrow.
            Assert.IsTrue(hqBuilding.TryHandleBuildingClick(new Vector2(hqScreenPoint.x, hqScreenPoint.y)));
            Assert.IsTrue(hqBuilding.IsUpgradeSymbolVisible);
            Button collectButton = GameObject.Find("Collect Button")?.GetComponent<Button>();
            Assert.IsNotNull(collectButton);
            collectButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(hqBuilding.IsUpgradeSymbolVisible);
            Assert.IsFalse(bioLab.IsUpgradeSymbolVisible);
            Assert.IsFalse(hangar.IsUpgradeSymbolVisible);
            Assert.IsFalse(trainingFacility.IsUpgradeSymbolVisible);
            Assert.IsFalse(livingQuarters.IsUpgradeSymbolVisible);
        }

        [UnityTest]
        public IEnumerator BaseScene_HqUpgradeSymbolStaysGreyAndFailsWhenCreditsAreInsufficient()
        {
            // Fresh saves have no credits, so tapping HQ should reveal a grey symbol that cannot start a timer.
            yield return null;

            // Use the visible HQ reference model center so this follows the player-facing tap target.
            HQBuilding hqBuilding = GameObject.Find("HQ Building")?.GetComponent<HQBuilding>();
            Assert.IsNotNull(hqBuilding);
            Transform referenceModel = hqBuilding.transform.Find("HQ Visual Root/HQ Reference Model");
            Assert.IsNotNull(referenceModel);
            Renderer referenceRenderer = referenceModel.GetComponent<Renderer>();
            Assert.IsNotNull(referenceRenderer);
            Camera baseCamera = Camera.main;
            Assert.IsNotNull(baseCamera);
            Vector3 screenPoint = baseCamera.WorldToScreenPoint(referenceRenderer.bounds.center);
            Assert.Greater(screenPoint.z, 0f);

            // Body tap reveals the symbol and should not mutate the local save.
            bool handled = hqBuilding.TryHandleBuildingClick(new Vector2(screenPoint.x, screenPoint.y));
            yield return null;
            SaveGameData symbolOnlyData = SaveGameManager.Load();
            Assert.IsTrue(handled);
            Assert.IsTrue(hqBuilding.IsUpgradeSymbolVisible);
            Assert.IsFalse(hqBuilding.CanAffordDisplayedUpgrade);
            Assert.IsFalse(symbolOnlyData.hqUpgradeInProgress);
            Assert.AreEqual(0, symbolOnlyData.coins);

            // The unaffordable visible symbol should be grey like the bio-lab symbol.
            Renderer symbolStemRenderer = hqBuilding.transform.Find("HQ Visual Root/HQ Upgrade Symbol/HQ Upgrade Symbol Stem")?.GetComponent<Renderer>();
            Assert.IsNotNull(symbolStemRenderer);
            Color symbolColor = GetMaterialColor(symbolStemRenderer.sharedMaterial);
            Assert.Less(Mathf.Abs(symbolColor.r - symbolColor.g), 0.05f);
            Assert.Less(Mathf.Abs(symbolColor.g - symbolColor.b), 0.05f);

            // Clicking the grey arrow should fail safely, keep the symbol visible, and show helpful feedback.
            Transform upgradeSymbol = hqBuilding.transform.Find("HQ Visual Root/HQ Upgrade Symbol");
            Assert.IsNotNull(upgradeSymbol);
            Vector3 symbolScreenPoint = baseCamera.WorldToScreenPoint(upgradeSymbol.position);
            Assert.Greater(symbolScreenPoint.z, 0f);
            bool symbolHandled = hqBuilding.TryHandleBuildingClick(new Vector2(symbolScreenPoint.x, symbolScreenPoint.y));
            yield return null;
            SaveGameData failedData = SaveGameManager.Load();
            Assert.IsTrue(symbolHandled);
            Assert.IsFalse(failedData.hqUpgradeInProgress);
            Assert.AreEqual(1, failedData.hqLevel);
            Assert.IsTrue(hqBuilding.IsUpgradeSymbolVisible);

            Text statusText = GameObject.Find("Status Text")?.GetComponent<Text>();
            Assert.IsNotNull(statusText);
            StringAssert.Contains("HQ needs", statusText.text);
        }

        [UnityTest]
        public IEnumerator BaseScene_BioLabShowsGreyUpgradeSymbolWhenCreditsAreInsufficient()
        {
            // Wait one frame so BaseSceneBootstrap can build the runtime lab and HUD.
            yield return null;

            // Fresh saves should create the generated bio lab on the future lab pad.
            BioLabBuilding bioLab = GameObject.Find("Bio Lab")?.GetComponent<BioLabBuilding>();
            Assert.IsNotNull(bioLab);
            Assert.AreEqual(1, bioLab.Level);
            Assert.IsFalse(bioLab.IsUpgradeSymbolVisible);

            // Tapping the lab should reveal the start-upgrade symbol even when it cannot be afforded.
            bioLab.ShowUpgradeSymbol();
            Assert.IsTrue(bioLab.IsUpgradeSymbolVisible);
            Assert.IsFalse(bioLab.CanAffordDisplayedUpgrade);

            // The visible symbol should be grey when the wallet cannot pay the level-one cost.
            Transform symbolStem = bioLab.transform.Find("Bio Lab Visual Root/Bio Lab Upgrade Symbol/Bio Lab Upgrade Symbol Stem");
            Renderer symbolStemRenderer = symbolStem?.GetComponent<Renderer>();
            Assert.IsNotNull(symbolStemRenderer);
            AssertFlatSymbolMesh(symbolStem, "Bio lab upgrade symbol stem");
            AssertFlatSymbolMesh(bioLab.transform.Find("Bio Lab Visual Root/Bio Lab Upgrade Symbol/Bio Lab Upgrade Symbol Arrow Head"), "Bio lab upgrade symbol head");
            Color symbolColor = GetMaterialColor(symbolStemRenderer.sharedMaterial);
            Assert.Less(Mathf.Abs(symbolColor.r - symbolColor.g), 0.05f);
            Assert.Less(Mathf.Abs(symbolColor.g - symbolColor.b), 0.05f);

            // Clicking the grey symbol should fail safely and leave the local save without an active lab timer.
            bool started = bioLab.RequestUpgradeFromVisibleSymbol();
            yield return null;
            SaveGameData saveData = SaveGameManager.Load();
            Assert.IsFalse(started);
            Assert.IsFalse(saveData.bioLabUpgradeInProgress);
            Assert.AreEqual(1, saveData.bioLabLevel);
            Assert.IsTrue(bioLab.IsUpgradeSymbolVisible);
        }

        [UnityTest]
        public IEnumerator BaseScene_BioLabUpgradeSymbolStartsTimedProgressWhenAffordable()
        {
            // Seed a level-three lab so the PlayMode progress assertion has a ten-second timer window.
            SaveGameManager.Save(new SaveGameData
            {
                coins = PlayerProgression.GetBioLabUpgradeCost(3),
                bioLabLevel = 3
            });

            // Reload Base after seeding so the generated lab reflects the saved level and wallet.
            SceneManager.LoadScene("Base");
            yield return null;

            // The lab should render as level three with no exterior add-on pieces.
            BioLabBuilding bioLab = GameObject.Find("Bio Lab")?.GetComponent<BioLabBuilding>();
            Assert.IsNotNull(bioLab);
            Assert.AreEqual(3, bioLab.Level);
            Transform detailRoot = bioLab.transform.Find("Bio Lab Visual Root/Bio Lab Detail Root");
            Assert.IsNotNull(detailRoot);
            Assert.AreEqual(0, CountActiveChildren(detailRoot));
            Assert.IsNull(GameObject.Find("Future Lab Pad Label"));
            Renderer labPadRenderer = GameObject.Find("Future Lab Pad")?.GetComponent<Renderer>();
            Assert.IsNotNull(labPadRenderer);
            Assert.IsFalse(labPadRenderer.enabled);

            // Upgrades should keep the lab footprint fixed and only lift the body by a few pixels.
            Transform bodyTransform = bioLab.transform.Find("Bio Lab Visual Root/Bio Lab Body");
            Assert.IsNotNull(bodyTransform);
            Assert.AreEqual(BioLabBuilding.CalculateVisualWidth(1), bodyTransform.localScale.x, 0.001f);
            Assert.AreEqual(BioLabBuilding.CalculateVisualWidth(1) * 0.82f, bodyTransform.localScale.z, 0.001f);
            Assert.AreEqual(BioLabBuilding.CalculateVisualHeight(3), bodyTransform.localScale.y, 0.001f);
            Assert.AreEqual(BioLabBuilding.ModelYawDegrees, Mathf.DeltaAngle(0f, bodyTransform.localEulerAngles.y), 0.001f);
            Assert.Greater(bodyTransform.localScale.y, BioLabBuilding.CalculateVisualHeight(1));
            Assert.LessOrEqual(bodyTransform.localScale.y - BioLabBuilding.CalculateVisualHeight(1), 0.06f);

            // The reference-textured model is the visible source of truth for matching the generated concept image.
            Transform referenceModel = bioLab.transform.Find("Bio Lab Visual Root/Bio Lab Reference Model");
            Assert.IsNotNull(referenceModel);
            MeshRenderer referenceRenderer = referenceModel.GetComponent<MeshRenderer>();
            Assert.IsNotNull(referenceRenderer);
            Assert.IsTrue(referenceRenderer.enabled);
            Assert.IsNotNull(referenceRenderer.sharedMaterial?.mainTexture);
            // The lab tint should stay cool and clinical rather than matching the warmer HQ/hangar colors.
            Color bioLabTint = GetMaterialColor(referenceRenderer.sharedMaterial);
            Assert.Greater(bioLabTint.g, bioLabTint.r);
            Assert.Greater(bioLabTint.b, bioLabTint.r);

            // The procedural details stay present as the upgrade/click/glow scaffold, but their renderers stay hidden.
            Transform plinthRoot = bioLab.transform.Find("Bio Lab Visual Root/Bio Lab Plinth Root");
            Assert.IsNotNull(plinthRoot);
            Assert.IsNotNull(plinthRoot.Find("Bio Lab Plinth Stone Deck"));
            Assert.IsNotNull(plinthRoot.Find("Bio Lab Plinth Front Left Corner Cap"));
            Assert.IsNotNull(bodyTransform.Find("Bio Lab Front Sign Panel"));
            Assert.IsNotNull(bodyTransform.Find("Bio Lab Front Door Panel"));
            Assert.IsNotNull(bodyTransform.Find("Bio Lab Front Entry Lower Step"));
            Assert.IsNotNull(bodyTransform.Find("Bio Lab Roof Front Left Corner Block"));
            Assert.IsNotNull(bodyTransform.Find("Bio Lab Front Left Vertical Light Strip"));
            Assert.IsNotNull(bodyTransform.Find("Bio Lab Left Side Upper Light Strip"));
            Assert.IsNotNull(bodyTransform.Find("Bio Lab Upper Utility Vent"));
            Assert.IsNotNull(bodyTransform.Find("Bio Lab Left Side Upper Vent"));
            Transform domeTransform = bioLab.transform.Find("Bio Lab Visual Root/Bio Lab Dome");
            Assert.IsNotNull(domeTransform);
            Assert.AreEqual(BioLabBuilding.ModelYawDegrees, Mathf.DeltaAngle(0f, domeTransform.localEulerAngles.y), 0.001f);
            Assert.IsNotNull(domeTransform.Find("Bio Lab Dome Lower Socket"));
            Assert.IsNotNull(domeTransform.Find("Bio Lab Dome Base Ring Segment 1"));
            Assert.IsNotNull(domeTransform.Find("Bio Lab Front Back Dome Rib"));
            Assert.IsNotNull(domeTransform.Find("Bio Lab Left Right Dome Rib"));
            Assert.IsNotNull(domeTransform.Find("Bio Lab Dome Glass Glint"));
            Assert.IsFalse(bodyTransform.GetComponent<Renderer>().enabled);
            Assert.IsFalse(domeTransform.GetComponent<Renderer>().enabled);
            TextMesh bioLabSignText = bioLab.transform.Find("Bio Lab Visual Root/Bio Lab Label")?.GetComponent<TextMesh>();
            Assert.IsNotNull(bioLabSignText);
            Assert.IsFalse(bioLabSignText.gameObject.activeSelf);
            Assert.AreEqual(string.Empty, bioLabSignText.text);

            // Revealing the symbol with enough credits should color it green.
            bioLab.ShowUpgradeSymbol();
            Assert.IsTrue(bioLab.IsUpgradeSymbolVisible);
            Assert.IsTrue(bioLab.CanAffordDisplayedUpgrade);
            Renderer symbolStemRenderer = bioLab.transform.Find("Bio Lab Visual Root/Bio Lab Upgrade Symbol/Bio Lab Upgrade Symbol Stem")?.GetComponent<Renderer>();
            Assert.IsNotNull(symbolStemRenderer);
            Color symbolColor = GetMaterialColor(symbolStemRenderer.sharedMaterial);
            Assert.Greater(symbolColor.g, symbolColor.r);
            Assert.Greater(symbolColor.g, symbolColor.b);

            // Clicking the visible green symbol should spend credits and start the saved timer.
            bool started = bioLab.RequestUpgradeFromVisibleSymbol();
            yield return null;
            Assert.IsTrue(started);
            SaveGameData startedData = SaveGameManager.Load();
            Assert.AreEqual(0, startedData.coins);
            Assert.IsTrue(startedData.bioLabUpgradeInProgress);
            Assert.AreEqual(10, startedData.bioLabUpgradeDurationSeconds);
            Assert.IsFalse(bioLab.IsUpgradeSymbolVisible);
            Assert.IsTrue(bioLab.IsProgressVisible);

            // Let a small amount of PlayMode time pass so the circular fill advances but cannot complete.
            yield return new WaitForSeconds(0.2f);
            Assert.Greater(bioLab.ProgressFillAmount, 0.01f);
            Assert.Less(bioLab.ProgressFillAmount, 0.8f);
        }

        [UnityTest]
        public IEnumerator BaseScene_GenericFacilitiesUseReferenceModelsAndTimedUpgradeArrows()
        {
            // Seed facilities with exact credits so their same-rule popup arrows can start timers.
            SaveGameManager.Save(new SaveGameData
            {
                coins = PlayerProgression.GetHangarUpgradeCost(2) + PlayerProgression.GetTrainingFacilityUpgradeCost(3) + PlayerProgression.GetLivingQuartersUpgradeCost(4),
                hangarLevel = 2,
                trainingFacilityLevel = 3,
                livingQuartersLevel = 4
            });

            // Reload Base after seeding so the runtime buildings reflect saved levels and wallet state.
            SceneManager.LoadScene("Base");
            yield return null;

            // The new facilities should replace the future pads with reference-textured models.
            UpgradeableFacilityBuilding hangar = GameObject.Find("Hangar")?.GetComponent<UpgradeableFacilityBuilding>();
            UpgradeableFacilityBuilding trainingFacility = GameObject.Find("Training Facility")?.GetComponent<UpgradeableFacilityBuilding>();
            UpgradeableFacilityBuilding livingQuarters = GameObject.Find("Living Quarters")?.GetComponent<UpgradeableFacilityBuilding>();
            Assert.IsNotNull(hangar);
            Assert.IsNotNull(trainingFacility);
            Assert.IsNotNull(livingQuarters);
            Assert.AreEqual(2, hangar.Level);
            Assert.AreEqual(3, trainingFacility.Level);
            Assert.AreEqual(4, livingQuarters.Level);
            AssertFacilityReferenceModel(hangar, "Hangar");
            AssertFacilityReferenceModel(trainingFacility, "Training Facility");
            AssertFacilityReferenceModel(livingQuarters, "Living Quarters");

            // Occupied future pads should remain as logical slots but draw no slab or label.
            Renderer hangarPadRenderer = GameObject.Find("Future Hangar Pad")?.GetComponent<Renderer>();
            Renderer trainingPadRenderer = GameObject.Find("Future Training Pad")?.GetComponent<Renderer>();
            Renderer livingQuartersPadRenderer = GameObject.Find("Future Living Quarters Pad")?.GetComponent<Renderer>();
            Assert.IsNotNull(hangarPadRenderer);
            Assert.IsNotNull(trainingPadRenderer);
            Assert.IsNotNull(livingQuartersPadRenderer);
            Assert.IsFalse(hangarPadRenderer.enabled);
            Assert.IsFalse(trainingPadRenderer.enabled);
            Assert.IsFalse(livingQuartersPadRenderer.enabled);
            Assert.IsNull(GameObject.Find("Future Hangar Pad Label"));
            Assert.IsNull(GameObject.Find("Future Training Pad Label"));
            Assert.IsNull(GameObject.Find("Future Living Quarters Pad Label"));

            // Higher levels should stretch the concept images and hidden scaffolds upward without changing footprint.
            Transform hangarReference = hangar.transform.Find("Hangar Visual Root/Hangar Reference Model");
            Transform trainingReference = trainingFacility.transform.Find("Training Facility Visual Root/Training Facility Reference Model");
            Transform livingQuartersReference = livingQuarters.transform.Find("Living Quarters Visual Root/Living Quarters Reference Model");
            Transform hangarBody = hangar.transform.Find("Hangar Visual Root/Hangar Body");
            Transform trainingBody = trainingFacility.transform.Find("Training Facility Visual Root/Training Facility Body");
            Transform livingQuartersBody = livingQuarters.transform.Find("Living Quarters Visual Root/Living Quarters Body");
            Assert.IsNotNull(hangarBody);
            Assert.IsNotNull(trainingBody);
            Assert.IsNotNull(livingQuartersBody);
            Assert.Greater(hangar.CurrentReferenceHeight, hangar.CalculateReferenceModelHeight(1));
            Assert.Greater(trainingFacility.CurrentReferenceHeight, trainingFacility.CalculateReferenceModelHeight(1));
            Assert.Greater(livingQuarters.CurrentReferenceHeight, livingQuarters.CalculateReferenceModelHeight(1));
            Assert.AreEqual(hangar.CurrentReferenceHeight, hangarReference.localScale.y, 0.001f);
            Assert.AreEqual(trainingFacility.CurrentReferenceHeight, trainingReference.localScale.y, 0.001f);
            Assert.AreEqual(livingQuarters.CurrentReferenceHeight, livingQuartersReference.localScale.y, 0.001f);
            Assert.AreEqual(1.42f, hangarBody.localScale.x, 0.001f);
            Assert.AreEqual(1.48f, trainingBody.localScale.x, 0.001f);
            Assert.AreEqual(1.56f, livingQuartersBody.localScale.x, 0.001f);
            Assert.AreEqual(hangar.CurrentVisualHeight, hangarBody.localScale.y, 0.001f);
            Assert.AreEqual(trainingFacility.CurrentVisualHeight, trainingBody.localScale.y, 0.001f);
            Assert.AreEqual(livingQuarters.CurrentVisualHeight, livingQuartersBody.localScale.y, 0.001f);

            // Generic facilities use distinct tints so their reference art does not collapse into one color scheme.
            Color hangarTint = GetMaterialColor(hangarReference.GetComponent<MeshRenderer>().sharedMaterial);
            Color trainingTint = GetMaterialColor(trainingReference.GetComponent<MeshRenderer>().sharedMaterial);
            Color livingQuartersTint = GetMaterialColor(livingQuartersReference.GetComponent<MeshRenderer>().sharedMaterial);
            Assert.Greater(hangarTint.r, hangarTint.b);
            Assert.Less(hangarTint.b, hangarTint.g);
            Assert.Greater(trainingTint.b, trainingTint.r);
            Assert.Greater(livingQuartersTint.r, livingQuartersTint.g);
            Assert.Greater(livingQuartersTint.b, livingQuartersTint.g);
            Assert.Greater(Vector4.Distance(hangarTint, trainingTint), 0.30f);
            Assert.Greater(Vector4.Distance(livingQuartersTint, hangarTint), 0.30f);
            Assert.Greater(Vector4.Distance(livingQuartersTint, trainingTint), 0.30f);

            // Revealing the hangar symbol with enough credits should color its 2D arrow green.
            hangar.ShowUpgradeSymbol();
            Assert.IsTrue(hangar.IsUpgradeSymbolVisible);
            Assert.IsTrue(hangar.CanAffordDisplayedUpgrade);
            Transform hangarStem = hangar.transform.Find("Hangar Visual Root/Hangar Upgrade Symbol/Hangar Upgrade Symbol Stem");
            Renderer hangarStemRenderer = hangarStem?.GetComponent<Renderer>();
            Assert.IsNotNull(hangarStemRenderer);
            AssertFlatSymbolMesh(hangarStem, "Hangar upgrade symbol stem");
            AssertFlatSymbolMesh(hangar.transform.Find("Hangar Visual Root/Hangar Upgrade Symbol/Hangar Upgrade Symbol Arrow Head"), "Hangar upgrade symbol head");
            Color hangarSymbolColor = GetMaterialColor(hangarStemRenderer.sharedMaterial);
            Assert.Greater(hangarSymbolColor.g, hangarSymbolColor.r);
            Assert.Greater(hangarSymbolColor.g, hangarSymbolColor.b);

            // Clicking the visible green hangar symbol should spend only the hangar cost and start its timer.
            bool hangarStarted = hangar.RequestUpgradeFromVisibleSymbol();
            yield return null;
            SaveGameData hangarStartedData = SaveGameManager.Load();
            Assert.IsTrue(hangarStarted);
            Assert.IsTrue(hangarStartedData.hangarUpgradeInProgress);
            Assert.IsFalse(hangar.IsUpgradeSymbolVisible);
            Assert.IsTrue(hangar.IsProgressVisible);
            Assert.AreEqual(3, hangarStartedData.hangarUpgradeDurationSeconds);

            // The training symbol should still be affordable from the remaining credits and use the same 2D arrow.
            trainingFacility.ShowUpgradeSymbol();
            Assert.IsTrue(trainingFacility.IsUpgradeSymbolVisible);
            Assert.IsTrue(trainingFacility.CanAffordDisplayedUpgrade);
            Transform trainingStem = trainingFacility.transform.Find("Training Facility Visual Root/Training Facility Upgrade Symbol/Training Facility Upgrade Symbol Stem");
            Renderer trainingStemRenderer = trainingStem?.GetComponent<Renderer>();
            Assert.IsNotNull(trainingStemRenderer);
            AssertFlatSymbolMesh(trainingStem, "Training facility upgrade symbol stem");
            AssertFlatSymbolMesh(trainingFacility.transform.Find("Training Facility Visual Root/Training Facility Upgrade Symbol/Training Facility Upgrade Symbol Arrow Head"), "Training facility upgrade symbol head");
            Color trainingSymbolColor = GetMaterialColor(trainingStemRenderer.sharedMaterial);
            Assert.Greater(trainingSymbolColor.g, trainingSymbolColor.r);
            Assert.Greater(trainingSymbolColor.g, trainingSymbolColor.b);

            // Clicking the visible green training symbol should spend the remaining credits and start its timer.
            bool trainingStarted = trainingFacility.RequestUpgradeFromVisibleSymbol();
            yield return null;
            SaveGameData bothStartedData = SaveGameManager.Load();
            Assert.IsTrue(trainingStarted);
            Assert.AreEqual(PlayerProgression.GetLivingQuartersUpgradeCost(4), bothStartedData.coins);
            Assert.IsTrue(bothStartedData.hangarUpgradeInProgress);
            Assert.IsTrue(bothStartedData.trainingFacilityUpgradeInProgress);
            Assert.AreEqual(10, bothStartedData.trainingFacilityUpgradeDurationSeconds);
            Assert.IsFalse(trainingFacility.IsUpgradeSymbolVisible);
            Assert.IsTrue(trainingFacility.IsProgressVisible);

            // The living-quarters symbol should remain affordable from the final credits and use the same 2D arrow.
            livingQuarters.ShowUpgradeSymbol();
            Assert.IsTrue(livingQuarters.IsUpgradeSymbolVisible);
            Assert.IsTrue(livingQuarters.CanAffordDisplayedUpgrade);
            Transform livingQuartersStem = livingQuarters.transform.Find("Living Quarters Visual Root/Living Quarters Upgrade Symbol/Living Quarters Upgrade Symbol Stem");
            Renderer livingQuartersStemRenderer = livingQuartersStem?.GetComponent<Renderer>();
            Assert.IsNotNull(livingQuartersStemRenderer);
            AssertFlatSymbolMesh(livingQuartersStem, "Living quarters upgrade symbol stem");
            AssertFlatSymbolMesh(livingQuarters.transform.Find("Living Quarters Visual Root/Living Quarters Upgrade Symbol/Living Quarters Upgrade Symbol Arrow Head"), "Living quarters upgrade symbol head");
            Color livingQuartersSymbolColor = GetMaterialColor(livingQuartersStemRenderer.sharedMaterial);
            Assert.Greater(livingQuartersSymbolColor.g, livingQuartersSymbolColor.r);
            Assert.Greater(livingQuartersSymbolColor.g, livingQuartersSymbolColor.b);

            // Clicking the visible green living-quarters symbol should spend the final credits and start its timer.
            bool livingQuartersStarted = livingQuarters.RequestUpgradeFromVisibleSymbol();
            yield return null;
            SaveGameData allStartedData = SaveGameManager.Load();
            Assert.IsTrue(livingQuartersStarted);
            Assert.AreEqual(0, allStartedData.coins);
            Assert.IsTrue(allStartedData.hangarUpgradeInProgress);
            Assert.IsTrue(allStartedData.trainingFacilityUpgradeInProgress);
            Assert.IsTrue(allStartedData.livingQuartersUpgradeInProgress);
            Assert.AreEqual(60, allStartedData.livingQuartersUpgradeDurationSeconds);
            Assert.IsFalse(livingQuarters.IsUpgradeSymbolVisible);
            Assert.IsTrue(livingQuarters.IsProgressVisible);

            // Let a small amount of PlayMode time pass so all circular fills advance without completing.
            yield return new WaitForSeconds(0.2f);
            Assert.Greater(hangar.ProgressFillAmount, 0.01f);
            Assert.Greater(trainingFacility.ProgressFillAmount, 0.01f);
            Assert.Greater(livingQuarters.ProgressFillAmount, 0.001f);
            Assert.Less(trainingFacility.ProgressFillAmount, 0.8f);
            Assert.Less(livingQuarters.ProgressFillAmount, 0.1f);
        }

        [UnityTest]
        public IEnumerator BaseScene_CompletedGenericFacilityUpgradesPlayReferenceGlowAndSave()
        {
            // Seed generic facility timers as already complete so Base load runs the same app-reopen completion path.
            SaveGameManager.Save(new SaveGameData
            {
                hangarLevel = 1,
                hangarUpgradeInProgress = true,
                hangarUpgradeStartedUtcTicks = DateTime.UtcNow.AddSeconds(-2).Ticks,
                hangarUpgradeDurationSeconds = 1,
                trainingFacilityLevel = 1,
                trainingFacilityUpgradeInProgress = true,
                trainingFacilityUpgradeStartedUtcTicks = DateTime.UtcNow.AddSeconds(-2).Ticks,
                trainingFacilityUpgradeDurationSeconds = 1,
                livingQuartersLevel = 1,
                livingQuartersUpgradeInProgress = true,
                livingQuartersUpgradeStartedUtcTicks = DateTime.UtcNow.AddSeconds(-2).Ticks,
                livingQuartersUpgradeDurationSeconds = 1
            });

            // Reload Base so the bootstrap completes and saves both facility upgrades.
            SceneManager.LoadScene("Base");
            yield return null;

            // The save should now reflect completed levels and cleared timers.
            SaveGameData completedData = SaveGameManager.Load();
            Assert.AreEqual(2, completedData.hangarLevel);
            Assert.AreEqual(2, completedData.trainingFacilityLevel);
            Assert.AreEqual(2, completedData.livingQuartersLevel);
            Assert.IsFalse(completedData.hangarUpgradeInProgress);
            Assert.IsFalse(completedData.trainingFacilityUpgradeInProgress);
            Assert.IsFalse(completedData.livingQuartersUpgradeInProgress);
            Assert.AreEqual(0, completedData.hangarUpgradeStartedUtcTicks);
            Assert.AreEqual(0, completedData.trainingFacilityUpgradeStartedUtcTicks);
            Assert.AreEqual(0, completedData.livingQuartersUpgradeStartedUtcTicks);

            // Generated facilities should trigger a mesh-free reference-silhouette glow and pop.
            UpgradeableFacilityBuilding hangar = GameObject.Find("Hangar")?.GetComponent<UpgradeableFacilityBuilding>();
            UpgradeableFacilityBuilding trainingFacility = GameObject.Find("Training Facility")?.GetComponent<UpgradeableFacilityBuilding>();
            UpgradeableFacilityBuilding livingQuarters = GameObject.Find("Living Quarters")?.GetComponent<UpgradeableFacilityBuilding>();
            Assert.IsNotNull(hangar);
            Assert.IsNotNull(trainingFacility);
            Assert.IsNotNull(livingQuarters);
            Assert.AreEqual(2, hangar.Level);
            Assert.AreEqual(2, trainingFacility.Level);
            Assert.AreEqual(2, livingQuarters.Level);
            AssertFacilityGlowActive(hangar, "Hangar");
            AssertFacilityGlowActive(trainingFacility, "Training Facility");
            AssertFacilityGlowActive(livingQuarters, "Living Quarters");

            // The visible Base feedback should mention every completed generic facility upgrade.
            Text statusText = GameObject.Find("Status Text")?.GetComponent<Text>();
            Assert.IsNotNull(statusText);
            StringAssert.Contains("Hangar upgrade complete", statusText.text);
            StringAssert.Contains("Training upgrade complete", statusText.text);
            StringAssert.Contains("Living quarters upgrade complete", statusText.text);
        }

        [UnityTest]
        public IEnumerator BaseScene_CompletedBioLabUpgradeRebuildsGlowPopAndSaves()
        {
            // Seed a bio-lab timer that should complete as soon as the Base scene opens.
            SaveGameManager.Save(new SaveGameData
            {
                bioLabLevel = 1,
                bioLabUpgradeInProgress = true,
                bioLabUpgradeStartedUtcTicks = DateTime.UtcNow.AddSeconds(-2).Ticks,
                bioLabUpgradeDurationSeconds = 1
            });

            // Reload Base so the bootstrap executes the same ready-upgrade path as an app reopen.
            SceneManager.LoadScene("Base");
            yield return null;

            // The save should now reflect the completed lab level and cleared timer.
            SaveGameData completedData = SaveGameManager.Load();
            Assert.AreEqual(2, completedData.bioLabLevel);
            Assert.IsFalse(completedData.bioLabUpgradeInProgress);
            Assert.AreEqual(0, completedData.bioLabUpgradeStartedUtcTicks);
            Assert.AreEqual(0, completedData.bioLabUpgradeDurationSeconds);

            // The generated lab should rebuild for level two and trigger local completion effects.
            BioLabBuilding bioLab = GameObject.Find("Bio Lab")?.GetComponent<BioLabBuilding>();
            Assert.IsNotNull(bioLab);
            Assert.AreEqual(2, bioLab.Level);
            Assert.IsTrue(bioLab.IsCompletionGlowVisible);
            Assert.IsTrue(bioLab.IsPopAnimating);
            Assert.AreEqual(1, bioLab.CompletionEffectPlayCount);
            Assert.AreEqual(1, bioLab.CompletionSoundRequestCount);
            Assert.IsTrue(bioLab.HasCompletionSoundSource);
            Assert.AreEqual(UpgradeCompletionSoundProfile.BioLab, bioLab.CompletionSoundProfile);
            Assert.IsTrue(bioLab.HasGeneratedCompletionSoundClip);
            Assert.IsFalse(bioLab.IsProgressVisible);

            // The completion glow should be a mesh-free root with one reference-silhouette aura, not fallback strips.
            Transform glowRoot = bioLab.transform.Find("Bio Lab Visual Root/Bio Lab Completion Glow");
            Assert.IsNotNull(glowRoot);
            Assert.IsNull(glowRoot.GetComponent<MeshFilter>());
            Transform referenceModel = bioLab.transform.Find("Bio Lab Visual Root/Bio Lab Reference Model");
            Transform referenceAura = glowRoot.Find("Bio Lab Completion Reference Aura");
            Assert.IsNotNull(referenceModel);
            Assert.IsNotNull(referenceAura);
            Assert.IsNull(glowRoot.Find("Bio Lab Completion Body Outline"));
            Assert.IsNull(glowRoot.Find("Bio Lab Completion Dome Outline"));
            Assert.Greater(referenceAura.localScale.x, referenceModel.localScale.x);
            Assert.Greater(referenceAura.localScale.y, referenceModel.localScale.y);
            Assert.Greater(referenceAura.localPosition.z, referenceModel.localPosition.z);
            MeshRenderer auraRenderer = referenceAura.GetComponent<MeshRenderer>();
            MeshRenderer referenceRenderer = referenceModel.GetComponent<MeshRenderer>();
            Assert.IsNotNull(auraRenderer);
            Assert.IsNotNull(referenceRenderer);
            Assert.IsNotNull(auraRenderer.sharedMaterial?.mainTexture);
            Assert.Less(auraRenderer.sharedMaterial.renderQueue, referenceRenderer.sharedMaterial.renderQueue);

            // The aura should visibly pulse instead of staying as a static outline.
            yield return new WaitForSeconds(0.15f);
            Assert.Greater(bioLab.CompletionGlowPulseScale, 1.03f);
            Assert.Greater(bioLab.CompletionGlowAlpha, 0.32f);

            // Level two should stay free of exterior structure and only grow slightly taller.
            Transform detailRoot = bioLab.transform.Find("Bio Lab Visual Root/Bio Lab Detail Root");
            Assert.IsNotNull(detailRoot);
            Assert.AreEqual(0, CountActiveChildren(detailRoot));
            Transform bodyTransform = bioLab.transform.Find("Bio Lab Visual Root/Bio Lab Body");
            Assert.IsNotNull(bodyTransform);
            Assert.AreEqual(BioLabBuilding.CalculateVisualWidth(1), bodyTransform.localScale.x, 0.001f);
            Assert.AreEqual(BioLabBuilding.CalculateVisualHeight(2), bodyTransform.localScale.y, 0.001f);
            Assert.LessOrEqual(bodyTransform.localScale.y - BioLabBuilding.CalculateVisualHeight(1), 0.03f);

            // The visible Base feedback should mention the completed lab upgrade.
            Text statusText = GameObject.Find("Status Text")?.GetComponent<Text>();
            Assert.IsNotNull(statusText);
            StringAssert.Contains("Bio lab upgrade complete", statusText.text);
        }

        [UnityTest]
        public IEnumerator BaseScene_ZoomControlsAdjustCameraWithoutMovingOverlayHud()
        {
            // Wait one frame so BaseSceneBootstrap has created the camera and overlay HUD.
            yield return null;

            // The Base camera should expose the V6 zoom controller.
            Camera baseCamera = Camera.main;
            Assert.IsNotNull(baseCamera);
            BaseCameraController zoomController = baseCamera.GetComponent<BaseCameraController>();
            Assert.IsNotNull(zoomController);

            // The HUD canvas must remain screen-space overlay so camera zoom cannot push controls offscreen.
            Canvas canvas = GameObject.Find("Base HUD Canvas")?.GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);

            // Capture the Play button corners before any camera zoom changes.
            RectTransform playButtonRect = GameObject.Find("Play Button")?.GetComponent<RectTransform>();
            Assert.IsNotNull(playButtonRect);
            Vector3[] playButtonCornersBefore = GetRectCorners(playButtonRect);

            // The visible zoom-in button should tighten the field of view.
            float startingFieldOfView = baseCamera.fieldOfView;
            Button zoomInButton = GameObject.Find("Zoom In Button")?.GetComponent<Button>();
            Assert.IsNotNull(zoomInButton);
            zoomInButton.onClick.Invoke();
            yield return null;
            Assert.Less(baseCamera.fieldOfView, startingFieldOfView);

            // The controller's pinch path should also zoom in when touch distance expands.
            float buttonZoomFieldOfView = baseCamera.fieldOfView;
            zoomController.ApplyPinchZoom(100f, 170f);
            Assert.Less(baseCamera.fieldOfView, buttonZoomFieldOfView);

            // The visible zoom-out button should widen the field of view again.
            Button zoomOutButton = GameObject.Find("Zoom Out Button")?.GetComponent<Button>();
            Assert.IsNotNull(zoomOutButton);
            zoomOutButton.onClick.Invoke();
            yield return null;
            Assert.Greater(baseCamera.fieldOfView, BaseCameraController.MinimumFieldOfView);

            // Explicit clamping proves the controller cannot zoom beyond the authored inspection range.
            zoomController.SetZoomNormalized(1f);
            Assert.AreEqual(BaseCameraController.MaximumFieldOfView, baseCamera.fieldOfView, 0.001f);
            zoomController.SetZoomNormalized(0f);
            Assert.AreEqual(BaseCameraController.MinimumFieldOfView, baseCamera.fieldOfView, 0.001f);

            // Overlay HUD geometry should not move when the world camera zoom changes.
            Vector3[] playButtonCornersAfter = GetRectCorners(playButtonRect);
            AssertCornersApproximately(playButtonCornersBefore, playButtonCornersAfter, "Play Button");
        }

        [UnityTest]
        public IEnumerator BaseScene_MapDragPansCameraWithoutMovingOverlayHud()
        {
            // Wait one frame so the runtime-built Base camera and HUD exist.
            yield return null;

            // The draggable map behavior lives on the same camera controller as zoom.
            Camera baseCamera = Camera.main;
            Assert.IsNotNull(baseCamera);
            BaseCameraController cameraController = baseCamera.GetComponent<BaseCameraController>();
            Assert.IsNotNull(cameraController);

            // Capture overlay HUD geometry before moving the world camera.
            RectTransform playButtonRect = GameObject.Find("Play Button")?.GetComponent<RectTransform>();
            Assert.IsNotNull(playButtonRect);
            Vector3[] playButtonCornersBefore = GetRectCorners(playButtonRect);

            // Use camera pixel bounds so the test follows the active PlayMode render target.
            Vector2 screenCenter = new(baseCamera.pixelWidth * 0.5f, baseCamera.pixelHeight * 0.5f);
            Vector2 draggedRight = screenCenter + new Vector2(120f, 0f);
            Vector3 startingCameraPosition = baseCamera.transform.position;

            // Dragging the map to the right should pan the camera left so the map appears to follow the pointer.
            cameraController.ApplyMapDrag(screenCenter, draggedRight);
            Vector3 draggedCameraPosition = baseCamera.transform.position;
            Assert.Less(draggedCameraPosition.x, startingCameraPosition.x);
            Assert.AreEqual(startingCameraPosition.y, draggedCameraPosition.y, 0.001f);
            Assert.AreEqual(startingCameraPosition.z, draggedCameraPosition.z, 0.001f);

            // A vertical drag should pan along the ground-plane depth axis without changing camera height.
            Vector2 draggedDown = draggedRight + new Vector2(0f, -120f);
            cameraController.ApplyMapDrag(draggedRight, draggedDown);
            Vector3 secondDragCameraPosition = baseCamera.transform.position;
            Assert.AreNotEqual(draggedCameraPosition.z, secondDragCameraPosition.z);
            Assert.AreEqual(startingCameraPosition.y, secondDragCameraPosition.y, 0.001f);

            // Even after map dragging, the overlay HUD should stay anchored to the same screen pixels.
            Vector3[] playButtonCornersAfter = GetRectCorners(playButtonRect);
            AssertCornersApproximately(playButtonCornersBefore, playButtonCornersAfter, "Play Button");
        }

        [UnityTest]
        public IEnumerator BaseScene_MissionButtonsPersistUnlockedSelectionAndRejectLockedRows()
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

            // Selecting mission 2 should save immediately and update the visible mission controls.
            SaveGameData missionTwoData = SaveGameManager.Load();
            Assert.AreEqual(2, missionTwoData.currentMissionLevel);
            Assert.AreEqual(">M2", missionTwoButton.GetComponentInChildren<Text>()?.text);
            Text statusText = GameObject.Find("Status Text")?.GetComponent<Text>();
            Assert.IsNotNull(statusText);
            StringAssert.Contains("Mission 2: Market Run", statusText.text);

            // Mission three should be visible but disabled until mission two is completed.
            Button missionThreeButton = GameObject.Find("Mission 3 Button")?.GetComponent<Button>();
            Assert.IsNotNull(missionThreeButton);
            Assert.IsFalse(missionThreeButton.interactable);
            missionThreeButton.onClick.Invoke();
            yield return null;

            // A direct listener invoke should still fail safely through the progression guard and leave selection alone.
            SaveGameData lockedAttemptData = SaveGameManager.Load();
            Assert.AreEqual(2, lockedAttemptData.currentMissionLevel);
            StringAssert.Contains("Mission 3 locked", statusText.text);
        }

        [UnityTest]
        public IEnumerator BaseScene_CollectAndUpgradeButtonsPersistLocalProgress()
        {
            // Wait one frame so BaseSceneBootstrap can build runtime UI and world objects.
            yield return null;

            // Fresh local progress should show the first HQ level before any actions run.
            Text creditsDetailText = ExpandCreditsDetailPanel();
            StringAssert.Contains("HQ Level: 1", creditsDetailText.text);

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
            Assert.AreEqual(PlayerProgression.GetHqUpgradeDurationSeconds(1), upgradedData.hqUpgradeDurationSeconds);

            // The visible HUD should now show a running countdown instead of the ready state.
            StringAssert.Contains("HQ Upgrade: ", creditsDetailText.text);
            Assert.IsFalse(creditsDetailText.text.Contains("HQ Upgrade: Ready"), creditsDetailText.text);
        }

        [UnityTest]
        public IEnumerator BaseScene_HighLevelHqUpgradeAndResetButtonsAcceptVisibleClicks()
        {
            // Seed the exact late-HQ wallet state from the reported screenshot.
            SaveGameManager.Save(new SaveGameData
            {
                coins = 300,
                hqLevel = 9,
                bioLabLevel = 6,
                hangarLevel = 6,
                trainingFacilityLevel = 6
            });

            // Reload Base after seeding so generated HUD interactability comes from the saved level-nine cost.
            SceneManager.LoadScene("Base");
            yield return null;

            // Keep the credits panel open because the reported issue happened with expanded building details visible.
            Text creditsDetailText = ExpandCreditsDetailPanel();
            StringAssert.Contains("Coins: 300", creditsDetailText.text);
            StringAssert.Contains("HQ Level: 9", creditsDetailText.text);
            StringAssert.Contains("HQ Upgrade: Ready (275c)", creditsDetailText.text);

            // The visible upgrade button should be the click receiver and should not clip its label.
            Button upgradeButton = GameObject.Find("Upgrade Button")?.GetComponent<Button>();
            Assert.IsNotNull(upgradeButton);
            Assert.IsTrue(upgradeButton.interactable);
            AssertButtonCanHandlePointerClick(upgradeButton, "Upgrade Button");
            AssertButtonLabelUsesBestFit(upgradeButton, "UPGRADE");

            // A real UI-style click should start the level-nine HQ timer and spend only that level's cost.
            ClickButtonThroughEventSystem(upgradeButton, "Upgrade Button");
            yield return null;
            SaveGameData startedData = SaveGameManager.Load();
            Assert.AreEqual(25, startedData.coins);
            Assert.IsTrue(startedData.hqUpgradeInProgress);
            Assert.AreEqual(PlayerProgression.GetHqUpgradeDurationSeconds(9), startedData.hqUpgradeDurationSeconds);
            StringAssert.Contains("Coins: 25", creditsDetailText.text);
            Assert.IsFalse(creditsDetailText.text.Contains("HQ Upgrade: Ready"), creditsDetailText.text);

            // The reset button should sit fully inside the HUD canvas and receive the top raycast as well.
            Button resetButton = GameObject.Find("Reset Save Button")?.GetComponent<Button>();
            RectTransform canvasRect = GameObject.Find("Base HUD Canvas")?.GetComponent<RectTransform>();
            Assert.IsNotNull(resetButton);
            Assert.IsNotNull(canvasRect);
            AssertRectInsideCanvas(resetButton.GetComponent<RectTransform>(), canvasRect, "Reset Save Button");
            AssertButtonCanHandlePointerClick(resetButton, "Reset Save Button");
            AssertButtonLabelUsesBestFit(resetButton, "RESET");

            // Clicking reset should persist fresh defaults and refresh the still-open detail panel immediately.
            ClickButtonThroughEventSystem(resetButton, "Reset Save Button");
            yield return null;
            SaveGameData resetData = SaveGameManager.Load();
            Assert.AreEqual(0, resetData.coins);
            Assert.AreEqual(1, resetData.hqLevel);
            Assert.IsFalse(resetData.hqUpgradeInProgress);
            StringAssert.Contains("Coins: 0", creditsDetailText.text);
            StringAssert.Contains("HQ Level: 1", creditsDetailText.text);
            StringAssert.Contains("HQ Upgrade: Need 75c", creditsDetailText.text);

            // The idle HQ button should now respond even when unaffordable by explaining the missing credits.
            Assert.IsTrue(upgradeButton.interactable);
            ClickButtonThroughEventSystem(upgradeButton, "Upgrade Button");
            yield return null;
            SaveGameData failedStartData = SaveGameManager.Load();
            Assert.AreEqual(0, failedStartData.coins);
            Assert.IsFalse(failedStartData.hqUpgradeInProgress);
            Text statusText = GameObject.Find("Status Text")?.GetComponent<Text>();
            Assert.IsNotNull(statusText);
            StringAssert.Contains("HQ needs 75 credits", statusText.text);
        }

        [UnityTest]
        public IEnumerator BaseScene_DailyObjectiveClaimButtonPersistsReward()
        {
            DateTime now = DateTime.UtcNow;
            SaveGameManager.Save(new SaveGameData
            {
                coins = 10,
                dailyObjectiveUtcDayNumber = DailyObjectiveProgression.GetUtcDayNumber(now),
                dailyObjectiveWins = DailyObjectiveProgression.WinsRequired,
                dailyObjectiveRewardClaimed = false
            });

            SceneManager.LoadScene("Base");
            yield return null;

            Button claimObjectiveButton = GameObject.Find("Claim Objective Button")?.GetComponent<Button>();
            Assert.IsNotNull(claimObjectiveButton);
            Assert.IsTrue(claimObjectiveButton.interactable);
            claimObjectiveButton.onClick.Invoke();
            yield return null;

            SaveGameData claimedData = SaveGameManager.Load();
            Assert.AreEqual(10 + DailyObjectiveProgression.RewardCoins, claimedData.coins);
            Assert.IsTrue(claimedData.dailyObjectiveRewardClaimed);

            Text statusText = GameObject.Find("Status Text")?.GetComponent<Text>();
            Assert.IsNotNull(statusText);
            StringAssert.Contains("daily coins claimed", statusText.text);
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
                hqUpgradeStartedUtcTicks = DateTime.UtcNow.AddSeconds(-PlayerProgression.GetHqUpgradeDurationSeconds(1) - 1).Ticks,
                hqUpgradeDurationSeconds = PlayerProgression.GetHqUpgradeDurationSeconds(1)
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

            // The generated HQ should rebuild at level two and trigger the same local glow/pop feedback as the lab.
            HQBuilding hqBuilding = GameObject.Find("HQ Building")?.GetComponent<HQBuilding>();
            Assert.IsNotNull(hqBuilding);
            Assert.AreEqual(2, hqBuilding.Level);
            Assert.IsTrue(hqBuilding.IsCompletionGlowVisible);
            Assert.IsTrue(hqBuilding.IsPopAnimating);
            Assert.AreEqual(1, hqBuilding.CompletionEffectPlayCount);

            // HQ completion should now exercise its own upgrade chime, not just the bio-lab path.
            Assert.AreEqual(1, hqBuilding.CompletionSoundRequestCount);
            Assert.IsTrue(hqBuilding.HasCompletionSoundSource);
            Assert.AreEqual(UpgradeCompletionSoundProfile.Hq, hqBuilding.CompletionSoundProfile);
            Assert.IsTrue(hqBuilding.HasGeneratedCompletionSoundClip);

            // The completion glow should be a mesh-free root with one reference-silhouette aura, not generated clutter.
            Transform glowRoot = hqBuilding.transform.Find("HQ Visual Root/HQ Completion Glow");
            Assert.IsNotNull(glowRoot);
            Assert.IsNull(glowRoot.GetComponent<MeshFilter>());
            Transform referenceModel = hqBuilding.transform.Find("HQ Visual Root/HQ Reference Model");
            Transform referenceAura = glowRoot.Find("HQ Completion Reference Aura");
            Assert.IsNotNull(referenceModel);
            Assert.IsNotNull(referenceAura);
            Assert.IsNull(glowRoot.Find("HQ Completion Fallback Aura"));
            Assert.Greater(referenceAura.localScale.x, referenceModel.localScale.x);
            Assert.Greater(referenceAura.localScale.y, referenceModel.localScale.y);
            Assert.Greater(referenceAura.localPosition.z, referenceModel.localPosition.z);
            MeshRenderer auraRenderer = referenceAura.GetComponent<MeshRenderer>();
            MeshRenderer referenceRenderer = referenceModel.GetComponent<MeshRenderer>();
            Assert.IsNotNull(auraRenderer);
            Assert.IsNotNull(referenceRenderer);
            Assert.IsNotNull(auraRenderer.sharedMaterial?.mainTexture);
            Assert.Less(auraRenderer.sharedMaterial.renderQueue, referenceRenderer.sharedMaterial.renderQueue);

            // The aura should visibly pulse instead of staying as a static outline.
            yield return new WaitForSeconds(0.15f);
            Assert.Greater(hqBuilding.CompletionGlowPulseScale, 1.03f);
            Assert.Greater(hqBuilding.CompletionGlowAlpha, 0.32f);

            // Level two should stay free of generated detail rows and grow only slightly taller.
            Transform detailRoot = hqBuilding.transform.Find("HQ Visual Root/HQ Detail Root");
            Assert.IsNotNull(detailRoot);
            Assert.AreEqual(0, CountActiveChildren(detailRoot));
            Transform bodyTransform = hqBuilding.transform.Find("HQ Visual Root/HQ Body");
            Assert.IsNotNull(bodyTransform);
            Assert.AreEqual(HQBuilding.CalculateVisualDiameter(1), bodyTransform.localScale.x, 0.001f);
            Assert.AreEqual(HQBuilding.CalculateVisualHeight(2), bodyTransform.localScale.y, 0.001f);
            Assert.LessOrEqual(bodyTransform.localScale.y - HQBuilding.CalculateVisualHeight(1), 0.03f);

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
        public IEnumerator MinigameScene_RuntimeCharactersUseHumanoidParts()
        {
            // Load the minigame directly so this verifies the real runtime bootstrap, not just a factory unit test.
            SceneManager.LoadScene("Minigame");
            yield return null;

            // Wait a second frame so LevelManager.Start can build gates, zombies, and the player visuals.
            yield return null;

            // The player root should remain the gameplay anchor while child figures provide the visible squad.
            GameObject playerSquad = GameObject.Find("Player Squad");
            Assert.IsNotNull(playerSquad);
            Assert.IsNull(playerSquad.GetComponent<MeshFilter>());
            Assert.IsNotNull(playerSquad.transform.Find("Survivor Leader/Human Leg Left/Human Knee Left"));
            Assert.IsNotNull(playerSquad.transform.Find("Survivor Leader/Human Leg Left/Human Knee Left/Human Shin Left"));
            Transform leaderSoldierVisual = playerSquad.transform.Find($"Survivor Leader/{PrototypeCharacterFactory.SoldierReferenceVisualName}");
            Transform leftSoldierVisual = playerSquad.transform.Find($"Survivor Left Wing/{PrototypeCharacterFactory.SoldierReferenceVisualName}");
            Transform rightSoldierVisual = playerSquad.transform.Find($"Survivor Right Wing/{PrototypeCharacterFactory.SoldierReferenceVisualName}");
            Assert.IsNotNull(leaderSoldierVisual);
            Assert.IsNotNull(leftSoldierVisual);
            Assert.IsNotNull(rightSoldierVisual);
            // Runtime weapon pose checks use visible body anchors, not hard-coded world heights.
            Transform leaderHead = playerSquad.transform.Find("Survivor Leader/Human Head");
            Transform leaderMuzzle = playerSquad.transform.Find($"Survivor Leader/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeaderRifleName}/{PlayerSquad.WeaponMuzzleAnchorName}");
            Transform smgHead = playerSquad.transform.Find("Survivor Right Wing/Human Head");
            Transform smgPelvis = playerSquad.transform.Find("Survivor Right Wing/Human Pelvis");
            Transform smgMuzzle = playerSquad.transform.Find($"Survivor Right Wing/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.RightWingSmgName}/{PlayerSquad.WeaponMuzzleAnchorName}");
            Assert.IsNotNull(leaderHead);
            Assert.IsNotNull(leaderMuzzle);
            Assert.IsNotNull(playerSquad.transform.Find($"Survivor Left Wing/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeftWingShotgunName}/{PlayerSquad.WeaponMuzzleAnchorName}"));
            Assert.IsNotNull(smgMuzzle);
            Assert.IsNotNull(smgHead);
            Assert.IsNotNull(smgPelvis);
            // Long-gun muzzle height should stay near the survivor head in the real Minigame scene.
            Assert.GreaterOrEqual(leaderMuzzle.position.y, leaderHead.position.y - 0.08f);
            // The compact SMG should be clearly lower, but still above the lower-body anchor.
            Assert.LessOrEqual(smgMuzzle.position.y, smgHead.position.y - 0.16f);
            Assert.Greater(smgMuzzle.position.y, smgPelvis.position.y);
            Assert.GreaterOrEqual(playerSquad.GetComponentsInChildren<MeshRenderer>(true).Length, 30);
            MeshRenderer leaderHeadRenderer = leaderHead.GetComponent<MeshRenderer>();
            MeshRenderer leaderSoldierRenderer = leaderSoldierVisual.GetComponent<MeshRenderer>();
            Assert.IsNotNull(leaderHeadRenderer);
            Assert.IsNotNull(leaderSoldierRenderer);
            Assert.IsFalse(leaderHeadRenderer.enabled);
            Assert.IsTrue(leaderSoldierRenderer.enabled);
            Assert.IsNotNull(leaderSoldierRenderer.sharedMaterial.mainTexture);
            PlayerSquad playerSquadComponent = playerSquad.GetComponent<PlayerSquad>();
            Assert.IsNotNull(playerSquadComponent);
            Assert.AreEqual(3, playerSquadComponent.WeaponMuzzleCount);
            PrototypeHumanoidAnimator playerAnimator = playerSquad.GetComponent<PrototypeHumanoidAnimator>();
            Assert.IsNotNull(playerAnimator);
            Assert.AreEqual(PrototypeHumanoidAnimationStyle.SurvivorSquad, playerAnimator.AnimationStyle);
            Assert.AreEqual(3, playerAnimator.AnimatedRigCount);

            // The HUD-only marker should be absent in the current art direction because the world squad is visible.
            Assert.IsNull(GameObject.Find("Player Squad Screen Marker"));

            // At least one spawned enemy should be a humanoid zombie rather than a single rectangular card.
            GameObject zombie = GameObject.Find("Zombie");
            Assert.IsNotNull(zombie);
            Assert.IsNull(zombie.GetComponent<MeshFilter>());
            Assert.IsNotNull(zombie.transform.Find("Zombie Figure/Zombie Head"));
            Assert.IsNotNull(zombie.transform.Find("Zombie Figure/Zombie Eye Left"));
            Assert.IsNotNull(zombie.transform.Find("Zombie Figure/Zombie Arm Right"));
            Assert.IsNotNull(zombie.transform.Find("Zombie Figure/Zombie Leg Left/Zombie Knee Left"));
            Assert.IsNotNull(zombie.transform.Find("Zombie Figure/Zombie Leg Left/Zombie Knee Left/Zombie Shin Left"));
            Assert.GreaterOrEqual(zombie.GetComponentsInChildren<MeshRenderer>(true).Length, 14);
            PrototypeHumanoidAnimator zombieAnimator = zombie.GetComponent<PrototypeHumanoidAnimator>();
            Assert.IsNotNull(zombieAnimator);
            Assert.AreEqual(PrototypeHumanoidAnimationStyle.ZombieShamble, zombieAnimator.AnimationStyle);
            Assert.AreEqual(1, zombieAnimator.AnimatedRigCount);
        }

        [UnityTest]
        public IEnumerator MinigameScene_WorldSpaceCombatFeedbackUsesDepthSafeSceneObjects()
        {
            // Load the minigame directly so the real runtime camera and LevelManager exist.
            SceneManager.LoadScene("Minigame");
            yield return null;

            // Wait a second frame so LevelManager.Start initializes gameplay dependencies.
            yield return null;

            // The scene manager owns the same combat feedback hook used by AutoShooter.ShotFired.
            LevelManager levelManager = GameObject.Find("Level Manager")?.GetComponent<LevelManager>();
            Assert.IsNotNull(levelManager);

            // Invoke the private shot-feedback handler to avoid timing flake from very short tracer lifetimes.
            MethodInfo shotFeedbackMethod = typeof(LevelManager).GetMethod("HandleShotFired", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(shotFeedbackMethod);
            Vector3 weaponMuzzleOrigin = new(0.35f, GameplayVisuals.ShotTracerMinimumY + 0.12f, 8f);
            Vector3 zombieTargetPoint = new(0.10f, GameplayVisuals.ZombieCenterY + 0.5f, 13f);
            shotFeedbackMethod.Invoke(levelManager, new object[]
            {
                weaponMuzzleOrigin,
                zombieTargetPoint,
                3f,
                true
            });

            // Shot feedback should be a flat mesh strip, not a stretched cube that can look like a gate.
            GameObject shotTracer = GameObject.Find("Shot Tracer");
            Assert.IsNotNull(shotTracer);
            Assert.IsNull(shotTracer.GetComponent<LineRenderer>());
            MeshFilter tracerMeshFilter = shotTracer.GetComponent<MeshFilter>();
            Assert.IsNotNull(tracerMeshFilter);
            Assert.IsNotNull(tracerMeshFilter.sharedMesh);
            Assert.AreEqual(4, tracerMeshFilter.sharedMesh.vertexCount);
            Assert.AreEqual(12, tracerMeshFilter.sharedMesh.triangles.Length);

            // The generated strip should begin exactly at the weapon muzzle instead of a lane/root approximation.
            Vector3[] tracerVertices = tracerMeshFilter.sharedMesh.vertices;
            float averageStartX = (tracerVertices[0].x + tracerVertices[1].x) * 0.5f;
            float averageStartY = (tracerVertices[0].y + tracerVertices[1].y) * 0.5f;
            float averageStartZ = (tracerVertices[0].z + tracerVertices[1].z) * 0.5f;
            Assert.AreEqual(weaponMuzzleOrigin.x, averageStartX, 0.001f);
            Assert.AreEqual(weaponMuzzleOrigin.y, averageStartY, 0.001f);
            Assert.AreEqual(weaponMuzzleOrigin.z, averageStartZ, 0.001f);

            // The tracer material should render in the foreground without writing scene depth.
            MeshRenderer shotTracerRenderer = shotTracer.GetComponent<MeshRenderer>();
            Assert.IsNotNull(shotTracerRenderer);
            Assert.IsNotNull(shotTracerRenderer.sharedMaterial);
            Assert.GreaterOrEqual(shotTracerRenderer.sharedMaterial.renderQueue, (int)UnityEngine.Rendering.RenderQueue.Overlay);

            // Floating feedback should remain a world object anchored to the combat point.
            FloatingFeedback[] feedbackLabels = UnityEngine.Object.FindObjectsByType<FloatingFeedback>(FindObjectsSortMode.None);
            Assert.Greater(feedbackLabels.Length, 0);
            FloatingFeedback feedbackLabel = feedbackLabels[0];
            Assert.IsNotNull(feedbackLabel.GetComponent<TextMesh>());
            Assert.IsNotNull(feedbackLabel.transform.Find("Feedback Text Shadow"));
            Assert.GreaterOrEqual(feedbackLabel.transform.position.y, GameplayVisuals.ShotTracerMinimumY);

            // The label material uses the same foreground render queue as tracers so iOS depth cannot bury it.
            Renderer labelRenderer = feedbackLabel.GetComponent<Renderer>();
            Assert.IsNotNull(labelRenderer);
            Assert.IsNotNull(labelRenderer.sharedMaterial);
            Assert.GreaterOrEqual(labelRenderer.sharedMaterial.renderQueue, (int)UnityEngine.Rendering.RenderQueue.Overlay);
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
            Button returnedMissionOneButton = GameObject.Find("Mission 1 Button")?.GetComponent<Button>();
            Button returnedMissionTwoButton = GameObject.Find("Mission 2 Button")?.GetComponent<Button>();
            Assert.IsNotNull(returnedMissionOneButton);
            Assert.IsNotNull(returnedMissionTwoButton);
            Assert.IsTrue(returnedMissionOneButton.interactable);
            Assert.IsTrue(returnedMissionTwoButton.interactable);
            Assert.AreEqual(">M2", returnedMissionTwoButton.GetComponentInChildren<Text>()?.text);
        }

        [UnityTest]
        public IEnumerator MinigameScene_MaxMissionCompletionMarksDoneWithoutUnlockingPastCap()
        {
            // Seed the final authored mission so the win path exercises the local progression cap.
            SaveGameManager.Save(new SaveGameData
            {
                // A large HQ bonus lets the teleported squad survive center-lane breaches on the final layout.
                hqLevel = 80,
                currentMissionLevel = PlayerProgression.MaxMissionLevel,
                highestUnlockedMissionLevel = PlayerProgression.MaxMissionLevel,
                unlockedMinigameLevel = PlayerProgression.MaxMissionLevel,
                completedMissionLevels = new List<int> { 1, 2, 3, 4, 5, 6, 7 }
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

            // The reward text should not advertise an impossible mission nine unlock.
            Text rewardText = GameObject.Find("Reward Text")?.GetComponent<Text>();
            Assert.IsNotNull(rewardText);
            StringAssert.Contains($"+{PlayerProgression.MinigameWinCoins} coins", rewardText.text);
            Assert.IsFalse(rewardText.text.Contains("Mission 9"), rewardText.text);
            Assert.IsFalse(rewardText.text.Contains("unlocked"), rewardText.text);

            // The saved state should mark mission eight complete while preserving the authored mission cap.
            SaveGameData rewardedData = SaveGameManager.Load();
            Assert.AreEqual(PlayerProgression.MaxMissionLevel, rewardedData.currentMissionLevel);
            Assert.AreEqual(PlayerProgression.MaxMissionLevel, rewardedData.highestUnlockedMissionLevel);
            Assert.AreEqual(PlayerProgression.MaxMissionLevel, rewardedData.unlockedMinigameLevel);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }, rewardedData.completedMissionLevels);

            // Returning to Base should show the final mission as selected and replayable rather than locked.
            Button baseButton = GameObject.Find("Base Button")?.GetComponent<Button>();
            Assert.IsNotNull(baseButton);
            baseButton.onClick.Invoke();
            yield return null;

            Assert.AreEqual("Base", SceneManager.GetActiveScene().name);
            Button returnedFinalMissionButton = GameObject.Find("Mission 8 Button")?.GetComponent<Button>();
            Assert.IsNotNull(returnedFinalMissionButton);
            Assert.IsTrue(returnedFinalMissionButton.interactable);
            Assert.AreEqual(">M8", returnedFinalMissionButton.GetComponentInChildren<Text>()?.text);
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

        private static int CountUniquePlanVertices(Mesh mesh)
        {
            // Rounding X/Z coordinates collapses duplicated side/cap vertices into real polygon corners.
            HashSet<string> uniquePlanPoints = new();
            foreach (Vector3 vertex in mesh.vertices)
            {
                // Skip cap-center vertices because they are not part of the visible footprint.
                if (new Vector2(vertex.x, vertex.z).sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                // A millimeter-style precision avoids float noise while preserving distinct footprint corners.
                int roundedX = Mathf.RoundToInt(vertex.x * 1000f);
                int roundedZ = Mathf.RoundToInt(vertex.z * 1000f);
                uniquePlanPoints.Add($"{roundedX}:{roundedZ}");
            }

            return uniquePlanPoints.Count;
        }

        private static int CountActiveChildren(Transform parent)
        {
            // Count only active rows so delayed Play Mode destruction cannot make hidden stale details fail tests.
            int activeChildren = 0;
            for (int childIndex = 0; childIndex < parent.childCount; childIndex += 1)
            {
                if (parent.GetChild(childIndex).gameObject.activeSelf)
                {
                    activeChildren += 1;
                }
            }

            return activeChildren;
        }

        private static Color GetMaterialColor(Material material)
        {
            // URP Lit and Unlit expose _BaseColor as the primary tint.
            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }

            // Built-in and fallback shaders usually expose _Color.
            if (material.HasProperty("_Color"))
            {
                return material.GetColor("_Color");
            }

            return material.color;
        }

        private static void AssertFlatSymbolMesh(Transform symbolPart, string label)
        {
            // Upgrade arrows should be generated as 2D mesh pieces, not blocky 3D primitives.
            Assert.IsNotNull(symbolPart, $"{label} should exist.");
            MeshFilter meshFilter = symbolPart.GetComponent<MeshFilter>();
            Assert.IsNotNull(meshFilter, $"{label} should own a flat mesh.");
            Mesh mesh = meshFilter.sharedMesh;
            Assert.IsNotNull(mesh, $"{label} should have generated mesh data.");
            foreach (Vector3 vertex in mesh.vertices)
            {
                // A flat symbol keeps every vertex on local Z zero so it has no mesh thickness.
                Assert.AreEqual(0f, vertex.z, 0.0001f, $"{label} vertex z should be flat.");
            }
        }

        private static void AssertFacilityReferenceModel(UpgradeableFacilityBuilding facilityBuilding, string displayName)
        {
            // The reference-textured model is the visible source of truth for each new facility concept image.
            Transform referenceModel = facilityBuilding.transform.Find($"{displayName} Visual Root/{displayName} Reference Model");
            Assert.IsNotNull(referenceModel, $"{displayName} reference model should exist.");
            MeshRenderer referenceRenderer = referenceModel.GetComponent<MeshRenderer>();
            Assert.IsNotNull(referenceRenderer, $"{displayName} reference model should render.");
            Assert.IsTrue(referenceRenderer.enabled, $"{displayName} reference renderer should be enabled.");
            Assert.IsNotNull(referenceRenderer.sharedMaterial?.mainTexture, $"{displayName} reference texture should be loaded.");

            // The procedural fallback body should remain for sizing but not alter the exact reference-art look.
            Transform body = facilityBuilding.transform.Find($"{displayName} Visual Root/{displayName} Body");
            Assert.IsNotNull(body, $"{displayName} scaffold body should exist.");
            Renderer bodyRenderer = body.GetComponent<Renderer>();
            Assert.IsNotNull(bodyRenderer, $"{displayName} scaffold body should have a renderer.");
            Assert.IsFalse(bodyRenderer.enabled, $"{displayName} scaffold renderer should stay hidden behind reference art.");

            // The reference image already contains the sign text, so fallback text should be hidden.
            TextMesh label = facilityBuilding.transform.Find($"{displayName} Visual Root/{displayName} Label")?.GetComponent<TextMesh>();
            Assert.IsNotNull(label, $"{displayName} fallback label should exist.");
            Assert.IsFalse(label.gameObject.activeSelf, $"{displayName} fallback label should stay hidden.");
            Assert.AreEqual(string.Empty, label.text, $"{displayName} fallback label text should be empty.");
        }

        private static void AssertFacilityGlowActive(UpgradeableFacilityBuilding facilityBuilding, string displayName)
        {
            // Completion should show the same kind of pulsing silhouette feedback used by the bio lab.
            Assert.IsTrue(facilityBuilding.IsCompletionGlowVisible, $"{displayName} glow should be visible.");
            Assert.IsTrue(facilityBuilding.IsPopAnimating, $"{displayName} pop should be active.");
            Assert.AreEqual(1, facilityBuilding.CompletionEffectPlayCount, $"{displayName} completion effect should play once.");

            // Generic-facility completion should use the building-specific generated chime profile.
            Assert.AreEqual(1, facilityBuilding.CompletionSoundRequestCount, $"{displayName} completion sound should play once.");
            Assert.IsTrue(facilityBuilding.HasCompletionSoundSource, $"{displayName} should have a completion sound source.");
            Assert.AreEqual(GetExpectedCompletionSoundProfile(displayName), facilityBuilding.CompletionSoundProfile, $"{displayName} should use its own completion sound profile.");
            Assert.IsTrue(facilityBuilding.HasGeneratedCompletionSoundClip, $"{displayName} completion sound clip should be generated.");

            // The glow root should be a mesh-free holder with a padded reference-silhouette child.
            Transform glowRoot = facilityBuilding.transform.Find($"{displayName} Visual Root/{displayName} Completion Glow");
            Transform referenceModel = facilityBuilding.transform.Find($"{displayName} Visual Root/{displayName} Reference Model");
            Transform referenceAura = glowRoot?.Find($"{displayName} Completion Reference Aura");
            Assert.IsNotNull(glowRoot, $"{displayName} glow root should exist.");
            Assert.IsNull(glowRoot.GetComponent<MeshFilter>(), $"{displayName} glow root should not be a solid mesh.");
            Assert.IsNotNull(referenceModel, $"{displayName} reference model should exist.");
            Assert.IsNotNull(referenceAura, $"{displayName} reference aura should exist.");
            Assert.Greater(referenceAura.localScale.x, referenceModel.localScale.x, $"{displayName} aura should pad width.");
            Assert.Greater(referenceAura.localScale.y, referenceModel.localScale.y, $"{displayName} aura should pad height.");

            // The aura material should load the generated silhouette texture and render behind the visible model.
            MeshRenderer auraRenderer = referenceAura.GetComponent<MeshRenderer>();
            MeshRenderer referenceRenderer = referenceModel.GetComponent<MeshRenderer>();
            Assert.IsNotNull(auraRenderer, $"{displayName} aura should render.");
            Assert.IsNotNull(referenceRenderer, $"{displayName} reference should render.");
            Assert.IsNotNull(auraRenderer.sharedMaterial?.mainTexture, $"{displayName} aura texture should load.");
            Assert.Less(auraRenderer.sharedMaterial.renderQueue, referenceRenderer.sharedMaterial.renderQueue, $"{displayName} aura should render behind reference art.");
        }

        private static UpgradeCompletionSoundProfile GetExpectedCompletionSoundProfile(string displayName)
        {
            // The generic facility component is shared, so display name maps each runtime instance to its sound identity.
            return displayName switch
            {
                "Hangar" => UpgradeCompletionSoundProfile.Hangar,
                "Training Facility" => UpgradeCompletionSoundProfile.TrainingFacility,
                "Living Quarters" => UpgradeCompletionSoundProfile.LivingQuarters,
                _ => UpgradeCompletionSoundProfile.Hangar
            };
        }

        private static Vector3[] GetRectCorners(RectTransform rectTransform)
        {
            // Allocate a fresh array so before/after comparisons cannot alias the same buffer.
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return corners;
        }

        private static void AssertRectInsideCanvas(RectTransform rectTransform, RectTransform canvasRect, string displayName)
        {
            // World corners let this check match what the player sees in Screen Space Overlay.
            Vector3[] rectCorners = GetRectCorners(rectTransform);
            Vector3[] canvasCorners = GetRectCorners(canvasRect);
            Assert.GreaterOrEqual(rectCorners[0].x, canvasCorners[0].x - 0.5f, $"{displayName} left edge should stay inside the canvas.");
            Assert.GreaterOrEqual(rectCorners[0].y, canvasCorners[0].y - 0.5f, $"{displayName} bottom edge should stay inside the canvas.");
            Assert.LessOrEqual(rectCorners[2].x, canvasCorners[2].x + 0.5f, $"{displayName} right edge should stay inside the canvas.");
            Assert.LessOrEqual(rectCorners[2].y, canvasCorners[2].y + 0.5f, $"{displayName} top edge should stay inside the canvas.");
        }

        private static void AssertButtonLabelUsesBestFit(Button button, string expectedText)
        {
            // Best-fit labels keep compact HUD controls from visually truncating action names.
            Text label = button.GetComponentInChildren<Text>();
            Assert.IsNotNull(label);
            Assert.AreEqual(expectedText, label.text);
            Assert.IsTrue(label.resizeTextForBestFit, $"{expectedText} label should shrink before clipping.");
        }

        private static void AssertButtonCanHandlePointerClick(Button button, string displayName)
        {
            // Active/interactable Button objects should be able to receive Unity EventSystem pointer clicks.
            Assert.IsNotNull(EventSystem.current, $"{displayName} requires an active EventSystem.");
            Assert.IsTrue(button.gameObject.activeInHierarchy, $"{displayName} should be active in the HUD.");
            Assert.IsTrue(button.interactable, $"{displayName} should be interactable for this scenario.");
            Assert.IsTrue(ExecuteEvents.CanHandleEvent<IPointerClickHandler>(button.gameObject), $"{displayName} should handle pointer clicks.");
        }

        private static void ClickButtonThroughEventSystem(Button button, string displayName)
        {
            // Execute the same pointer-click interface Unity's EventSystem dispatches after a visible tap.
            AssertButtonCanHandlePointerClick(button, displayName);
            PointerEventData eventData = new(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left
            };
            ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerClickHandler);
        }

        private static Text ExpandCreditsDetailPanel()
        {
            // The credits details start inactive, so discover the panel from the active canvas hierarchy.
            RectTransform canvasRect = GameObject.Find("Base HUD Canvas")?.GetComponent<RectTransform>();
            Assert.IsNotNull(canvasRect);
            Transform creditsDetailTransform = canvasRect.transform.Find("Credits Detail Panel");
            Assert.IsNotNull(creditsDetailTransform);

            // Invoke the production button listener so tests cover the same expand path as a player tap.
            Button creditsButton = GameObject.Find("Credits Button")?.GetComponent<Button>();
            Assert.IsNotNull(creditsButton);
            creditsButton.onClick.Invoke();

            // Once expanded, the inactive detail text becomes inspectable through the cached transform.
            Assert.IsTrue(creditsDetailTransform.gameObject.activeSelf);
            Text creditsDetailText = creditsDetailTransform.Find("Credits Detail Text")?.GetComponent<Text>();
            Assert.IsNotNull(creditsDetailText);
            return creditsDetailText;
        }

        private static void AssertColorApproximately(Color expected, Color actual)
        {
            // Color checks use a small tolerance because shader-backed colors can round in serialized material state.
            Assert.AreEqual(expected.r, actual.r, 0.01f);
            Assert.AreEqual(expected.g, actual.g, 0.01f);
            Assert.AreEqual(expected.b, actual.b, 0.01f);
            Assert.AreEqual(expected.a, actual.a, 0.01f);
        }

        private static void AssertCornersApproximately(Vector3[] expected, Vector3[] actual, string label)
        {
            // Four corners are required for Unity RectTransform world-corner comparisons.
            Assert.AreEqual(4, expected.Length);
            Assert.AreEqual(4, actual.Length);
            for (int cornerIndex = 0; cornerIndex < expected.Length; cornerIndex += 1)
            {
                Assert.AreEqual(expected[cornerIndex].x, actual[cornerIndex].x, 0.5f, $"{label} corner {cornerIndex} x should stay fixed.");
                Assert.AreEqual(expected[cornerIndex].y, actual[cornerIndex].y, 0.5f, $"{label} corner {cornerIndex} y should stay fixed.");
                Assert.AreEqual(expected[cornerIndex].z, actual[cornerIndex].z, 0.5f, $"{label} corner {cornerIndex} z should stay fixed.");
            }
        }
    }
}
