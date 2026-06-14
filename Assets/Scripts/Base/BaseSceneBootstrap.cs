using System;
using System.Reflection;
using LaneSurvivor.Heroes;
using LaneSurvivor.Progression;
using LaneSurvivor.Rendering;
using LaneSurvivor.Retention;
using LaneSurvivor.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LaneSurvivor.Base
{
    public sealed class BaseSceneBootstrap : MonoBehaviour
    {
        private SaveGameData saveData;

        private HQBuilding hqBuilding;

        private BioLabBuilding bioLabBuilding;

        private UpgradeableFacilityBuilding hangarBuilding;

        private UpgradeableFacilityBuilding trainingFacilityBuilding;

        private BaseHudController hudController;

        private BaseCameraController cameraController;

        private string statusMessage = string.Empty;

        private void Awake()
        {
            // Load local progress before building UI so the first frame reflects persisted state.
            saveData = SaveGameManager.Load();
            bool saveDirty = DailyObjectiveProgression.EnsureCurrentObjective(saveData, DateTime.UtcNow);
            bool hqCompletedOnLoad = false;
            bool bioLabCompletedOnLoad = false;
            bool hangarCompletedOnLoad = false;
            bool trainingCompletedOnLoad = false;

            // Complete any timer that finished while the app was closed.
            if (PlayerProgression.CompleteReadyHqUpgrade(saveData, DateTime.UtcNow))
            {
                statusMessage = "HQ upgrade complete";
                hqCompletedOnLoad = true;
                GrantHqMilestoneRewards();
                saveDirty = true;
            }
            else if (GrantHqMilestoneRewards())
            {
                saveDirty = true;
            }

            // Complete a ready bio-lab timer before visuals are built so level-dependent geometry is current.
            if (PlayerProgression.CompleteReadyBioLabUpgrade(saveData, DateTime.UtcNow))
            {
                AppendStatusMessage("Bio lab upgrade complete");
                bioLabCompletedOnLoad = true;
                saveDirty = true;
            }

            // Complete ready hangar timers before visuals are built so the loaded art matches saved level.
            if (PlayerProgression.CompleteReadyHangarUpgrade(saveData, DateTime.UtcNow))
            {
                AppendStatusMessage("Hangar upgrade complete");
                hangarCompletedOnLoad = true;
                saveDirty = true;
            }

            // Complete ready training timers before visuals are built so the loaded art matches saved level.
            if (PlayerProgression.CompleteReadyTrainingFacilityUpgrade(saveData, DateTime.UtcNow))
            {
                AppendStatusMessage("Training upgrade complete");
                trainingCompletedOnLoad = true;
                saveDirty = true;
            }

            if (saveDirty)
            {
                // Persist UTC-day objective rollover, upgrade completion, and milestone rewards in one local write.
                SaveGameManager.Save(saveData);
            }

            Material groundMaterial = CreateMaterial(new Color(0.18f, 0.25f, 0.24f));
            Material reservedSpaceMaterial = CreateMaterial(new Color(0.30f, 0.35f, 0.32f));
            Material hqMaterial = CreateMaterial(Color.white);
            Material hqGlowMaterial = PrototypeMaterialFactory.CreateAlwaysVisibleFeedback(new Color(0.20f, 1f, 0.72f, 0.34f));
            Material hqReferenceMaterial = CreateHqReferenceMaterial();
            Material hqReferenceGlowMaterial = CreateHqReferenceGlowMaterial();
            Material hqSymbolMaterial = CreateMaterial(new Color(0.44f, 0.46f, 0.48f));
            Material hqProgressBackMaterial = CreateMaterial(new Color(0.10f, 0.12f, 0.13f));
            Material hqProgressFillMaterial = CreateMaterial(new Color(0.12f, 0.92f, 0.34f));
            Material bioLabMaterial = CreateMaterial(BioLabBuilding.CalculateBodyColor(saveData.bioLabLevel));
            Material bioLabDetailMaterial = CreateMaterial(new Color(0.12f, 0.74f, 0.78f));
            Material bioLabTrimMaterial = CreateMaterial(new Color(0.66f, 0.68f, 0.60f));
            Material bioLabDarkMaterial = CreateMaterial(new Color(0.03f, 0.16f, 0.17f));
            Material bioLabLightMaterial = CreateMaterial(new Color(0.66f, 1f, 0.96f));
            Material bioLabSymbolMaterial = CreateMaterial(new Color(0.44f, 0.46f, 0.48f));
            Material bioLabProgressBackMaterial = CreateMaterial(new Color(0.10f, 0.12f, 0.13f));
            Material bioLabProgressFillMaterial = CreateMaterial(new Color(0.12f, 0.92f, 0.34f));
            Material bioLabGlowMaterial = PrototypeMaterialFactory.CreateAlwaysVisibleFeedback(new Color(0.20f, 1f, 0.72f, 0.34f));
            Material bioLabReferenceMaterial = CreateBioLabReferenceMaterial();
            Material bioLabReferenceGlowMaterial = CreateBioLabReferenceGlowMaterial();
            Material hangarBodyMaterial = CreateMaterial(new Color(0.28f, 0.32f, 0.32f));
            Material hangarSymbolMaterial = CreateMaterial(new Color(0.44f, 0.46f, 0.48f));
            Material hangarProgressBackMaterial = CreateMaterial(new Color(0.10f, 0.12f, 0.13f));
            Material hangarProgressFillMaterial = CreateMaterial(new Color(0.12f, 0.92f, 0.34f));
            Material hangarReferenceMaterial = CreateHangarReferenceMaterial();
            Material hangarReferenceGlowMaterial = CreateHangarReferenceGlowMaterial();
            Material trainingBodyMaterial = CreateMaterial(new Color(0.30f, 0.33f, 0.32f));
            Material trainingSymbolMaterial = CreateMaterial(new Color(0.44f, 0.46f, 0.48f));
            Material trainingProgressBackMaterial = CreateMaterial(new Color(0.10f, 0.12f, 0.13f));
            Material trainingProgressFillMaterial = CreateMaterial(new Color(0.12f, 0.92f, 0.34f));
            Material trainingReferenceMaterial = CreateTrainingReferenceMaterial();
            Material trainingReferenceGlowMaterial = CreateTrainingReferenceGlowMaterial();

            cameraController = CreateCamera();
            CreateLight();
            CreateEventSystem();
            CreateGround(groundMaterial, reservedSpaceMaterial);
            hqBuilding = CreateHqBuilding(hqMaterial, hqReferenceMaterial, hqReferenceGlowMaterial, hqGlowMaterial, hqSymbolMaterial, hqProgressBackMaterial, hqProgressFillMaterial, TryStartHqUpgradeFromInteraction);
            bioLabBuilding = CreateBioLabBuilding(bioLabMaterial, bioLabDetailMaterial, bioLabTrimMaterial, bioLabDarkMaterial, bioLabLightMaterial, bioLabReferenceMaterial, bioLabReferenceGlowMaterial, bioLabSymbolMaterial, bioLabProgressBackMaterial, bioLabProgressFillMaterial, bioLabGlowMaterial, StartBioLabUpgrade);
            hangarBuilding = CreateHangarBuilding(hangarBodyMaterial, hangarReferenceMaterial, hangarReferenceGlowMaterial, hangarSymbolMaterial, hangarProgressBackMaterial, hangarProgressFillMaterial, StartHangarUpgrade);
            trainingFacilityBuilding = CreateTrainingFacilityBuilding(trainingBodyMaterial, trainingReferenceMaterial, trainingReferenceGlowMaterial, trainingSymbolMaterial, trainingProgressBackMaterial, trainingProgressFillMaterial, StartTrainingFacilityUpgrade);
            hudController = CreateHud();
            hudController.Initialize(CollectCoins, StartHqUpgrade, LaunchMinigame, ClaimDailyObjective, SelectMission, ResetSave, EquipNextOwnedHero, LaunchHeroes, ZoomInFromHud, ZoomOutFromHud, HideBuildingUpgradeSymbols);

            RefreshScene();

            // If the upgrade completed while the app was closed, play the requested local completion feedback now.
            if (hqCompletedOnLoad)
            {
                hqBuilding.PlayCompletionEffects();
            }

            if (bioLabCompletedOnLoad)
            {
                bioLabBuilding.PlayCompletionEffects();
            }

            if (hangarCompletedOnLoad)
            {
                hangarBuilding.PlayCompletionEffects();
            }

            if (trainingCompletedOnLoad)
            {
                trainingFacilityBuilding.PlayCompletionEffects();
            }
        }

        private void Update()
        {
            // Poll timer completion locally; no server authority exists in Phase 2.
            bool completedAnyUpgrade = false;
            bool completedHqUpgrade = false;
            bool completedBioLabUpgrade = false;
            bool completedHangarUpgrade = false;
            bool completedTrainingUpgrade = false;
            if (PlayerProgression.CompleteReadyHqUpgrade(saveData, DateTime.UtcNow))
            {
                AppendStatusMessage("HQ upgrade complete");
                GrantHqMilestoneRewards();
                completedAnyUpgrade = true;
                completedHqUpgrade = true;
            }

            // Bio-lab completion adds local visual/sound feedback after the saved level increases.
            if (PlayerProgression.CompleteReadyBioLabUpgrade(saveData, DateTime.UtcNow))
            {
                AppendStatusMessage("Bio lab upgrade complete");
                completedAnyUpgrade = true;
                completedBioLabUpgrade = true;
            }

            // Hangar completion uses the same local timer polling as the bio lab.
            if (PlayerProgression.CompleteReadyHangarUpgrade(saveData, DateTime.UtcNow))
            {
                AppendStatusMessage("Hangar upgrade complete");
                completedAnyUpgrade = true;
                completedHangarUpgrade = true;
            }

            // Training completion uses the same local timer polling as the bio lab.
            if (PlayerProgression.CompleteReadyTrainingFacilityUpgrade(saveData, DateTime.UtcNow))
            {
                AppendStatusMessage("Training upgrade complete");
                completedAnyUpgrade = true;
                completedTrainingUpgrade = true;
            }

            if (completedAnyUpgrade)
            {
                SaveGameManager.Save(saveData);
                RefreshScene();
                if (completedHqUpgrade)
                {
                    hqBuilding.PlayCompletionEffects();
                }

                if (completedBioLabUpgrade)
                {
                    bioLabBuilding.PlayCompletionEffects();
                }

                if (completedHangarUpgrade)
                {
                    hangarBuilding.PlayCompletionEffects();
                }

                if (completedTrainingUpgrade)
                {
                    trainingFacilityBuilding.PlayCompletionEffects();
                }
                return;
            }

            // Refresh countdowns each frame while an upgrade is active.
            if (saveData.hqUpgradeInProgress || saveData.bioLabUpgradeInProgress || saveData.hangarUpgradeInProgress || saveData.trainingFacilityUpgradeInProgress)
            {
                RefreshScene();
            }
        }

        private void CollectCoins()
        {
            // HUD actions count as clicking away from world building popups.
            HideBuildingUpgradeSymbols();

            // Save immediately so tapping collect then closing the app preserves progress.
            PlayerProgression.CollectCoins(saveData);
            SaveGameManager.Save(saveData);
            statusMessage = $"+{PlayerProgression.CoinsPerCollect} coins collected";
            RefreshScene();
        }

        private void StartHqUpgrade()
        {
            // HUD actions count as clicking away from world building popups.
            HideBuildingUpgradeSymbols();

            // HUD button clicks use the same save-backed request path as the HQ popup arrow.
            TryStartHqUpgradeFromInteraction();
        }

        private bool TryStartHqUpgradeFromInteraction()
        {
            // The progression layer handles cost and duplicate-timer validation.
            if (PlayerProgression.TryStartHqUpgrade(saveData, DateTime.UtcNow))
            {
                SaveGameManager.Save(saveData);
                statusMessage = "HQ upgrade started";
                RefreshScene();
                return true;
            }

            // HQ popup-arrow clicks need visible feedback when the timer cannot start.
            int neededCredits = PlayerProgression.GetHqUpgradeCost(saveData.hqLevel);
            statusMessage = saveData.hqUpgradeInProgress
                ? "HQ upgrade in progress"
                : $"HQ needs {neededCredits} credits";
            RefreshScene();
            return false;
        }

        private bool StartBioLabUpgrade()
        {
            // The progression layer handles credit cost, duration curve, and duplicate-timer validation.
            if (PlayerProgression.TryStartBioLabUpgrade(saveData, DateTime.UtcNow))
            {
                SaveGameManager.Save(saveData);
                statusMessage = "Bio lab upgrade started";
                RefreshScene();
                return true;
            }

            // Failed starts keep the visible popup symbol but tell the player why nothing happened.
            int neededCredits = PlayerProgression.GetBioLabUpgradeCost(saveData.bioLabLevel);
            statusMessage = saveData.bioLabUpgradeInProgress
                ? "Bio lab upgrade in progress"
                : $"Bio lab needs {neededCredits} credits";
            RefreshScene();
            return false;
        }

        private bool StartHangarUpgrade()
        {
            // The progression layer handles credit cost, duration curve, and duplicate-timer validation.
            if (PlayerProgression.TryStartHangarUpgrade(saveData, DateTime.UtcNow))
            {
                SaveGameManager.Save(saveData);
                statusMessage = "Hangar upgrade started";
                RefreshScene();
                return true;
            }

            // Failed starts keep the visible popup symbol but tell the player why nothing happened.
            int neededCredits = PlayerProgression.GetHangarUpgradeCost(saveData.hangarLevel);
            statusMessage = saveData.hangarUpgradeInProgress
                ? "Hangar upgrade in progress"
                : $"Hangar needs {neededCredits} credits";
            RefreshScene();
            return false;
        }

        private bool StartTrainingFacilityUpgrade()
        {
            // The progression layer handles credit cost, duration curve, and duplicate-timer validation.
            if (PlayerProgression.TryStartTrainingFacilityUpgrade(saveData, DateTime.UtcNow))
            {
                SaveGameManager.Save(saveData);
                statusMessage = "Training upgrade started";
                RefreshScene();
                return true;
            }

            // Failed starts keep the visible popup symbol but tell the player why nothing happened.
            int neededCredits = PlayerProgression.GetTrainingFacilityUpgradeCost(saveData.trainingFacilityLevel);
            statusMessage = saveData.trainingFacilityUpgradeInProgress
                ? "Training upgrade in progress"
                : $"Training needs {neededCredits} credits";
            RefreshScene();
            return false;
        }

        private void ResetSave()
        {
            // HUD actions count as clicking away from world building popups.
            HideBuildingUpgradeSymbols();

            // Reset through the save manager so the same default data is written to disk and shown in UI.
            saveData = SaveGameManager.ResetToFreshData();
            statusMessage = "Local save reset";
            RefreshScene();
        }

        private void SelectMission(int missionLevel)
        {
            // HUD actions count as clicking away from world building popups.
            HideBuildingUpgradeSymbols();

            // Direct mission buttons still route through progression so locked or corrupted targets are rejected.
            if (!PlayerProgression.TrySelectMission(saveData, missionLevel))
            {
                statusMessage = $"Mission {missionLevel} locked";
                RefreshScene();
                return;
            }

            // Save immediately so launching the minigame or closing the app preserves the selected mission.
            SaveGameManager.Save(saveData);
            statusMessage = PlayerProgression.IsMissionCompleted(saveData, missionLevel)
                ? $"Mission {missionLevel} replay ready"
                : $"Mission {missionLevel}: {PlayerProgression.GetMissionName(missionLevel)}";
            RefreshScene();
        }

        private void ClaimDailyObjective()
        {
            // HUD actions count as clicking away from world building popups.
            HideBuildingUpgradeSymbols();

            // Claim through the retention system so day rollover, eligibility, and coin reward stay centralized.
            if (!DailyObjectiveProgression.TryClaimReward(saveData, DateTime.UtcNow))
            {
                statusMessage = "Daily objective not ready";
                RefreshScene();
                return;
            }

            // Save immediately so the once-per-day claim cannot be repeated after returning from another scene.
            SaveGameManager.Save(saveData);
            statusMessage = $"+{DailyObjectiveProgression.RewardCoins} daily coins claimed";
            RefreshScene();
        }

        private void EquipNextOwnedHero()
        {
            // HUD actions count as clicking away from world building popups.
            HideBuildingUpgradeSymbols();

            // The Base panel cycles through owned heroes until a fuller selection UI exists.
            HeroDefinition hero = HeroInventory.GetNextOwnedHeroToEquip(saveData);
            if (hero == null)
            {
                statusMessage = "No heroes owned";
                RefreshScene();
                return;
            }

            // Persist the manual equip action immediately so the next minigame run sees the selected hero.
            if (HeroInventory.EquipHero(saveData, hero.id))
            {
                SaveGameManager.Save(saveData);
                statusMessage = $"{hero.displayName} equipped";
                RefreshScene();
            }
        }

        private bool GrantHqMilestoneRewards()
        {
            // HQ milestone hero rewards stay local and deterministic, with no gacha or backend.
            HeroDefinition heroReward = HeroRewardSystem.TryGrantHqLevelTwoHero(saveData);
            if (heroReward == null)
            {
                return false;
            }

            // Surface the reward without hiding any upgrade-complete feedback already prepared by the caller.
            string heroMessage = $"{heroReward.displayName} joined";
            statusMessage = string.IsNullOrWhiteSpace(statusMessage)
                ? heroMessage
                : $"{statusMessage} - {heroMessage}";
            return true;
        }

        private void LaunchMinigame()
        {
            // HUD actions count as clicking away from world building popups.
            HideBuildingUpgradeSymbols();

            // Save before leaving the base scene so the minigame sees the latest HQ bonus.
            SaveGameManager.Save(saveData);
            SceneManager.LoadScene("Minigame");
        }

        private void LaunchHeroes()
        {
            // HUD actions count as clicking away from world building popups.
            HideBuildingUpgradeSymbols();

            // Save before leaving the base scene so the Hero screen sees the latest local state.
            SaveGameManager.Save(saveData);
            SceneManager.LoadScene("Heroes");
        }

        private void ZoomInFromHud()
        {
            // HUD zoom clicks should dismiss building popups before changing the camera.
            HideBuildingUpgradeSymbols();
            cameraController.ZoomIn();
        }

        private void ZoomOutFromHud()
        {
            // HUD zoom clicks should dismiss building popups before changing the camera.
            HideBuildingUpgradeSymbols();
            cameraController.ZoomOut();
        }

        private void HideBuildingUpgradeSymbols()
        {
            // Any non-arrow interaction should leave no building upgrade popup selected.
            hqBuilding?.HideUpgradeSymbol();
            bioLabBuilding?.HideUpgradeSymbol();
            hangarBuilding?.HideUpgradeSymbol();
            trainingFacilityBuilding?.HideUpgradeSymbol();
        }

        private void RefreshScene()
        {
            // Keep world and HUD state synchronized from one saved data object.
            int remainingSeconds = PlayerProgression.GetHqUpgradeRemainingSeconds(saveData, DateTime.UtcNow);
            int bioLabRemainingSeconds = PlayerProgression.GetBioLabUpgradeRemainingSeconds(saveData, DateTime.UtcNow);
            int hangarRemainingSeconds = PlayerProgression.GetHangarUpgradeRemainingSeconds(saveData, DateTime.UtcNow);
            int trainingRemainingSeconds = PlayerProgression.GetTrainingFacilityUpgradeRemainingSeconds(saveData, DateTime.UtcNow);
            hqBuilding.ApplySaveData(saveData, DateTime.UtcNow);
            bioLabBuilding.ApplySaveData(saveData, DateTime.UtcNow);
            hangarBuilding.ApplyState(saveData.hangarLevel, saveData.coins, saveData.hangarUpgradeInProgress, PlayerProgression.GetHangarUpgradeCost(saveData.hangarLevel), PlayerProgression.GetHangarUpgradeProgress01(saveData, DateTime.UtcNow));
            trainingFacilityBuilding.ApplyState(saveData.trainingFacilityLevel, saveData.coins, saveData.trainingFacilityUpgradeInProgress, PlayerProgression.GetTrainingFacilityUpgradeCost(saveData.trainingFacilityLevel), PlayerProgression.GetTrainingFacilityUpgradeProgress01(saveData, DateTime.UtcNow));
            hudController.UpdateView(saveData, remainingSeconds, bioLabRemainingSeconds, hangarRemainingSeconds, trainingRemainingSeconds, statusMessage);
        }

        private void AppendStatusMessage(string message)
        {
            // Completion paths can happen together, so append instead of overwriting earlier feedback.
            statusMessage = string.IsNullOrWhiteSpace(statusMessage)
                ? message
                : $"{statusMessage} - {message}";
        }

        private static BaseCameraController CreateCamera()
        {
            // Use a stable perspective camera with zoom controlled by BaseCameraController.
            GameObject cameraObject = new("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = BaseCameraController.DefaultFieldOfView;
            camera.tag = "MainCamera";
            cameraObject.transform.position = BaseCameraController.DefaultCameraPosition;
            cameraObject.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            CreateOptionalAudioListener(cameraObject);

            // The controller handles pinch, scroll, keyboard, and HUD-button zoom without moving overlay UI.
            BaseCameraController zoomController = cameraObject.AddComponent<BaseCameraController>();
            zoomController.Configure(camera);
            return zoomController;
        }

        private static void CreateOptionalAudioListener(GameObject cameraObject)
        {
            // Resolve AudioListener lazily so the runtime assembly still compiles without AudioModule references.
            Type audioListenerType = BioLabBuilding.FindOptionalUnityAudioType("UnityEngine.AudioListener");
            if (audioListenerType == null)
            {
                return;
            }

            // One listener on the Base camera is enough for the generated bio-lab completion chime.
            if (cameraObject.GetComponent(audioListenerType) == null)
            {
                cameraObject.AddComponent(audioListenerType);
            }
        }

        private static void CreateLight()
        {
            // Directional light gives the placeholder primitives readable shape.
            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -25f, 0f);
        }

        private static void CreateEventSystem()
        {
            // Only create an EventSystem when the scene does not already provide one.
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static void CreateGround(Material groundMaterial, Material reservedSpaceMaterial)
        {
            // The base floor is intentionally oversized and rectangular so no footprint border or outline is visible.
            PrototypeGeometryFactory.CreateCube("Base Ground", new Vector3(0f, -0.06f, 0f), new Vector3(24f, 0.12f, 24f), groundMaterial);

            // Reserve a front opening where later rescued humans, trucks, or resources can enter the base.
            CreateReservedPad("Future Gate Space", new Vector3(0f, 0.04f, -4.25f), new Vector3(1.55f, 0.08f, 0.54f), reservedSpaceMaterial);

            // Reserve a separate unload opening so future truck/resource loops have a distinct destination.
            CreateReservedPad("Future Resource Drop-Off Opening", new Vector3(2.35f, 0.04f, -3.05f), new Vector3(1.22f, 0.08f, 0.48f), reservedSpaceMaterial);

            // Future lab, hangar, and training pads keep logical build slots while occupied slots hide their slab art.
            CreateReservedPad("Future Lab Pad", new Vector3(-2.35f, 0.04f, 0.75f), new Vector3(1.35f, 0.08f, 1.05f), reservedSpaceMaterial, false, false);
            CreateReservedPad("Future Hangar Pad", new Vector3(2.05f, 0.04f, 0.75f), new Vector3(1.45f, 0.08f, 1.12f), reservedSpaceMaterial, false, false);

            // Offset training diagonally behind the HQ so the restored HQ no longer hides most of the facility.
            CreateReservedPad("Future Training Pad", new Vector3(1.60f, 0.04f, 4.85f), new Vector3(1.75f, 0.08f, 0.9f), reservedSpaceMaterial, false, false);
        }

        private static GameObject CreateReservedPad(string name, Vector3 position, Vector3 scale, Material material, bool showLabel = true, bool showPad = true)
        {
            // Low pads are deliberately non-functional placeholders that keep future build slots visible.
            GameObject padObject = PrototypeGeometryFactory.CreateCube(name, position, scale, material);

            // Occupied pads can keep their logical object without drawing a slab behind finished building art.
            if (!showPad)
            {
                Renderer padRenderer = padObject.GetComponent<Renderer>();
                if (padRenderer != null)
                {
                    padRenderer.enabled = false;
                }
            }

            // Occupied pads can skip labels while preserving their logical world slot.
            if (!showLabel)
            {
                return padObject;
            }

            // A separate world label avoids inheriting the pad's non-uniform scale.
            GameObject labelObject = new($"{name} Label");
            labelObject.transform.position = position + new Vector3(0f, 0.16f, 0f);
            labelObject.transform.rotation = Quaternion.Euler(65f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.18f;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 1f;
            label.color = Color.white;
            label.text = BuildReservedPadLabel(name);
            return padObject;
        }

        private static string BuildReservedPadLabel(string name)
        {
            // Short labels keep the world-space pads legible without crowding the Base HUD.
            if (name.Contains("Gate"))
            {
                return "GATE";
            }

            if (name.Contains("Drop-Off"))
            {
                return "DROP";
            }

            if (name.Contains("Lab"))
            {
                return "LAB";
            }

            if (name.Contains("Hangar"))
            {
                return "HANGAR";
            }

            if (name.Contains("Training"))
            {
                return "TRAIN";
            }

            return "FUTURE";
        }

        private static HQBuilding CreateHqBuilding(Material hqMaterial, Material referenceMaterial, Material referenceGlowMaterial, Material glowMaterial, Material symbolMaterial, Material progressBackMaterial, Material progressFillMaterial, Func<bool> startUpgradeAction)
        {
            // The HQ root owns positioning while its visual root can pop on upgrade completion.
            GameObject hqObject = new("HQ Building");

            // The visual root lets completion effects pop the HQ without moving the logical map slot.
            GameObject visualRootObject = new("HQ Visual Root");
            visualRootObject.transform.SetParent(hqObject.transform, false);

            // The HQ keeps a five-sided prism scaffold even though the exact reference art is player-visible.
            GameObject hqBodyObject = PrototypeGeometryFactory.CreateRegularPrism("HQ Body", Vector3.zero, Vector3.one, 5, hqMaterial);
            hqBodyObject.transform.SetParent(visualRootObject.transform, false);

            // Detail roots stay present so HQBuilding can clear old generated rows from prior prototypes.
            GameObject detailRootObject = new("HQ Detail Root");
            detailRootObject.transform.SetParent(visualRootObject.transform, false);

            // The reference-textured model is the visible source of truth for the generated HQ concept.
            GameObject referenceModelObject = CreateHqReferenceModel(visualRootObject.transform, referenceMaterial);
            if (referenceModelObject != null)
            {
                // Keep the scaffold available for alignment and tests without letting it alter the visual match.
                SetRenderersEnabled(hqBodyObject.transform, false);
            }

            // A world-space TextMesh labels fallback geometry when the reference art cannot load.
            GameObject labelObject = new("HQ Label");
            labelObject.transform.SetParent(visualRootObject.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 2.05f, -0.45f);
            labelObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.22f;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 1f;
            label.color = Color.white;
            labelObject.SetActive(referenceModelObject == null);

            // The upgrade symbol appears only after tapping the HQ, matching the bio-lab upgrade flow.
            GameObject symbolRootObject = CreateHqUpgradeSymbol(visualRootObject.transform, symbolMaterial, out Renderer[] symbolRenderers);

            // The circular progress icon overlays the HQ while the saved timer is active.
            GameObject progressRootObject = CreateHqProgressIcon(visualRootObject.transform, progressBackMaterial, progressFillMaterial, out MeshFilter progressFillMeshFilter);

            // The completion glow is a blurred duplicate of the exact HQ silhouette.
            GameObject glowObject = CreateHqCompletionGlow(visualRootObject.transform, referenceGlowMaterial, glowMaterial);

            HQBuilding hqBuilding = hqObject.AddComponent<HQBuilding>();
            hqBuilding.Configure(visualRootObject.transform, label, hqBodyObject.transform, hqBodyObject.GetComponent<Renderer>(), detailRootObject.transform, referenceModelObject != null ? referenceModelObject.transform : null, symbolRootObject.transform, symbolRenderers, progressRootObject.transform, progressFillMeshFilter, glowObject.transform, startUpgradeAction);
            return hqBuilding;
        }

        private static GameObject CreateHqUpgradeSymbol(Transform parent, Material symbolMaterial, out Renderer[] symbolRenderers)
        {
            // The popup symbol is a simple flat upward arrow made from 2D meshes.
            GameObject symbolRootObject = new("HQ Upgrade Symbol");
            symbolRootObject.transform.SetParent(parent, false);
            symbolRootObject.transform.localPosition = new Vector3(0f, 2.32f, -0.20f);

            // The stem is a flat rectangle so the arrow reads as 2D rather than a raised block.
            GameObject stemObject = CreateFlatArrowStem("HQ Upgrade Symbol Stem", 0.18f, 0.36f, symbolMaterial);
            stemObject.transform.SetParent(symbolRootObject.transform, false);
            stemObject.transform.localPosition = new Vector3(0f, -0.12f, 0f);

            // The arrow head is a flat triangle paired with the flat stem.
            GameObject arrowHeadObject = CreateFlatArrowHead("HQ Upgrade Symbol Arrow Head", 0.48f, 0.32f, symbolMaterial);
            arrowHeadObject.transform.SetParent(symbolRootObject.transform, false);
            arrowHeadObject.transform.localPosition = new Vector3(0f, 0.17f, 0f);

            // The symbol label sits low enough to stay inside the default camera crop.
            GameObject textObject = new("HQ Upgrade Symbol Text");
            textObject.transform.SetParent(symbolRootObject.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0.32f, -0.08f);
            textObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            textObject.transform.localScale = Vector3.one * 0.10f;
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 1f;
            text.color = Color.white;
            text.text = "UP";

            symbolRenderers = new[]
            {
                stemObject.GetComponent<Renderer>(),
                arrowHeadObject.GetComponent<Renderer>()
            };
            symbolRootObject.SetActive(false);
            return symbolRootObject;
        }

        private static GameObject CreateFlatArrowStem(string name, float width, float height, Material material)
        {
            // A rectangle in local X/Y space keeps the symbol visually flat while staying in the 3D world.
            Mesh mesh = new()
            {
                name = $"{name} Mesh",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[]
                {
                    new Vector3(-width * 0.5f, -height * 0.5f, 0f),
                    new Vector3(width * 0.5f, -height * 0.5f, 0f),
                    new Vector3(width * 0.5f, height * 0.5f, 0f),
                    new Vector3(-width * 0.5f, height * 0.5f, 0f)
                },
                normals = new[]
                {
                    Vector3.back,
                    Vector3.back,
                    Vector3.back,
                    Vector3.back
                },
                uv = new[]
                {
                    Vector2.zero,
                    Vector2.right,
                    Vector2.one,
                    Vector2.up
                },
                triangles = new[]
                {
                    0, 2, 1,
                    0, 3, 2,
                    0, 1, 2,
                    0, 2, 3
                }
            };
            mesh.RecalculateBounds();
            return CreateFlatSymbolMeshObject(name, mesh, material);
        }

        private static GameObject CreateFlatArrowHead(string name, float width, float height, Material material)
        {
            // A single local X/Y triangle gives the popup a true 2D arrow head.
            Mesh mesh = new()
            {
                name = $"{name} Mesh",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[]
                {
                    new Vector3(-width * 0.5f, -height * 0.5f, 0f),
                    new Vector3(width * 0.5f, -height * 0.5f, 0f),
                    new Vector3(0f, height * 0.5f, 0f)
                },
                normals = new[]
                {
                    Vector3.back,
                    Vector3.back,
                    Vector3.back
                },
                uv = new[]
                {
                    Vector2.zero,
                    Vector2.right,
                    new Vector2(0.5f, 1f)
                },
                triangles = new[]
                {
                    0, 2, 1,
                    0, 1, 2
                }
            };
            mesh.RecalculateBounds();
            return CreateFlatSymbolMeshObject(name, mesh, material);
        }

        private static GameObject CreateFlatSymbolMeshObject(string name, Mesh mesh, Material material)
        {
            // Build the flat symbol object manually so no primitive helper adds depth or collider state.
            GameObject gameObject = new(name);

            // MeshFilter owns the generated 2D symbol mesh.
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            // MeshRenderer draws the flat shape with the mutable green/grey symbol material.
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            return gameObject;
        }

        private static GameObject CreateHqProgressIcon(Transform parent, Material backMaterial, Material fillMaterial, out MeshFilter fillMeshFilter)
        {
            // The progress root sits over the HQ roof like a diegetic circular timer.
            GameObject progressRootObject = new("HQ Progress Icon");
            progressRootObject.transform.SetParent(parent, false);
            progressRootObject.transform.localPosition = new Vector3(0f, 1.74f, -0.04f);

            // A dark background disc makes partial fill readable against the detailed HQ roof art.
            GameObject backDiscObject = PrototypeGeometryFactory.CreateCylinder("HQ Progress Back Disc", Vector3.zero, new Vector3(0.94f, 0.035f, 0.94f), backMaterial);
            backDiscObject.transform.SetParent(progressRootObject.transform, false);

            // The fill mesh is rebuilt as a pie wedge by HQBuilding.
            GameObject fillObject = new("HQ Progress Fill");
            fillObject.transform.SetParent(progressRootObject.transform, false);
            fillObject.transform.localPosition = new Vector3(0f, 0.032f, 0f);
            fillMeshFilter = fillObject.AddComponent<MeshFilter>();
            MeshRenderer fillRenderer = fillObject.AddComponent<MeshRenderer>();
            fillRenderer.sharedMaterial = fillMaterial;

            // Start hidden until an active upgrade timer exists.
            progressRootObject.SetActive(false);
            return progressRootObject;
        }

        private static Material CreateHqReferenceMaterial()
        {
            // The visible HQ model should preserve the generated command-center concept image.
            return CreateTexturedTransparentMaterial("HQ/HQReferenceCutout", "HQ Reference Cutout Material", Color.white, (int)RenderQueue.Transparent);
        }

        private static Material CreateHqReferenceGlowMaterial()
        {
            // The glow texture keeps the exact HQ silhouette but tints it into one aura color.
            return CreateTexturedTransparentMaterial("HQ/HQReferenceGlowSilhouette", "HQ Reference Glow Silhouette Material", new Color(0.20f, 1f, 0.72f, 0.40f), (int)RenderQueue.Transparent - 10);
        }

        private static GameObject CreateHqReferenceModel(Transform parent, Material referenceMaterial)
        {
            // The reference model names the player-visible quad used as the exact HQ art.
            return CreateReferenceQuad(parent, referenceMaterial, "HQ Reference Model", "HQ Reference Model Quad");
        }

        private static GameObject CreateHqReferenceGlowAura(Transform parent, Material referenceGlowMaterial)
        {
            // The completion aura uses the same cutout shape as the visible model so the pulse follows the outline.
            return CreateReferenceQuad(parent, referenceGlowMaterial, "HQ Completion Reference Aura", "HQ Completion Reference Aura Quad");
        }

        private static GameObject CreateHqCompletionGlow(Transform parent, Material referenceGlowMaterial, Material fallbackGlowMaterial)
        {
            // The root stays mesh-free so toggling and pulsing cannot produce one large opaque volume.
            GameObject glowRootObject = new("HQ Completion Glow");
            glowRootObject.transform.SetParent(parent, false);

            // The reference aura is preferred because it exactly matches the generated HQ silhouette.
            GameObject referenceAuraObject = CreateHqReferenceGlowAura(glowRootObject.transform, referenceGlowMaterial);
            if (referenceAuraObject == null && fallbackGlowMaterial != null)
            {
                // Fallback builds a small pentagon aura if the reference texture cannot load.
                GameObject fallbackAuraObject = PrototypeGeometryFactory.CreateRegularPrism("HQ Completion Fallback Aura", Vector3.zero, Vector3.one, 5, fallbackGlowMaterial);
                fallbackAuraObject.transform.SetParent(glowRootObject.transform, false);
                fallbackAuraObject.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                fallbackAuraObject.transform.localScale = new Vector3(2.16f, 0.08f, 2.16f);
            }

            // Completion feedback starts hidden and is activated by HQBuilding.PlayCompletionEffects.
            glowRootObject.SetActive(false);
            return glowRootObject;
        }

        private static BioLabBuilding CreateBioLabBuilding(Material bodyMaterial, Material domeMaterial, Material trimMaterial, Material darkMaterial, Material lightMaterial, Material referenceMaterial, Material referenceGlowMaterial, Material symbolMaterial, Material progressBackMaterial, Material progressFillMaterial, Material glowMaterial, Func<bool> startUpgradeAction)
        {
            // The bio lab occupies the existing future lab pad so V6 base-space reservations become useful.
            GameObject labObject = new("Bio Lab");
            labObject.transform.position = new Vector3(-2.35f, 0.10f, 0.75f);

            // The visual root lets completion effects pop the lab without moving click colliders.
            GameObject visualRootObject = new("Bio Lab Visual Root");
            visualRootObject.transform.SetParent(labObject.transform, false);

            // The plinth matches the concept's layered square base instead of relying only on the reserved pad.
            GameObject plinthRootObject = new("Bio Lab Plinth Root");
            plinthRootObject.transform.SetParent(visualRootObject.transform, false);
            plinthRootObject.transform.localRotation = Quaternion.Euler(0f, BioLabBuilding.ModelYawDegrees, 0f);
            CreateBioLabPlinth(plinthRootObject.transform, trimMaterial, darkMaterial, lightMaterial);

            // A simple generated body gives level one a readable research-building silhouette.
            GameObject bodyObject = PrototypeGeometryFactory.CreateCube("Bio Lab Body", Vector3.zero, Vector3.one, bodyMaterial);
            bodyObject.transform.SetParent(visualRootObject.transform, false);
            bodyObject.transform.localRotation = Quaternion.Euler(0f, BioLabBuilding.ModelYawDegrees, 0f);
            CreateBioLabBodyIntegratedDetails(bodyObject.transform, trimMaterial, darkMaterial, lightMaterial);

            // The dome suggests a lab/reactor without importing any art.
            GameObject domeObject = PrototypeGeometryFactory.CreateSphere("Bio Lab Dome", Vector3.zero, new Vector3(0.72f, 0.32f, 0.72f), domeMaterial);
            domeObject.transform.SetParent(visualRootObject.transform, false);
            domeObject.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            domeObject.transform.localRotation = Quaternion.Euler(0f, BioLabBuilding.ModelYawDegrees, 0f);
            CreateBioLabDomeIntegratedDetails(domeObject.transform, trimMaterial, darkMaterial, lightMaterial);

            // The reference-textured model is the visible source of truth for matching the generated concept image.
            GameObject referenceModelObject = CreateBioLabReferenceModel(visualRootObject.transform, referenceMaterial);
            if (referenceModelObject != null)
            {
                // Keep the procedural pieces as interaction/progression scaffolding without letting them alter the visual match.
                SetRenderersEnabled(plinthRootObject.transform, false);
                SetRenderersEnabled(bodyObject.transform, false);
                SetRenderersEnabled(domeObject.transform, false);
            }

            // The empty detail root lets BioLabBuilding clear legacy exterior pieces from older prototype runs.
            GameObject detailRootObject = new("Bio Lab Detail Root");
            detailRootObject.transform.SetParent(visualRootObject.transform, false);

            // A world-space label keeps the placeholder lab identifiable in the angled Base camera.
            GameObject labelObject = new("Bio Lab Label");
            labelObject.transform.SetParent(visualRootObject.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.38f, -0.36f);
            labelObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.18f;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 1f;
            label.color = Color.white;
            labelObject.SetActive(referenceModelObject == null);

            // The upgrade symbol is generated above the lab and hidden until the lab is tapped.
            GameObject symbolRootObject = CreateBioLabUpgradeSymbol(visualRootObject.transform, symbolMaterial, out Renderer[] symbolRenderers);

            // The circular progress icon overlays the lab while the saved timer is active.
            GameObject progressRootObject = CreateBioLabProgressIcon(visualRootObject.transform, progressBackMaterial, progressFillMaterial, out MeshFilter progressFillMeshFilter);

            // The completion glow uses the same reference silhouette when the exact concept model is visible.
            GameObject glowObject = CreateBioLabCompletionGlow(visualRootObject.transform, glowMaterial, referenceModelObject != null ? referenceGlowMaterial : null);

            // Generated audio lets the upgrade finish with sound without adding imported files.
            Component audioSource = CreateOptionalBioLabAudioSource(labObject);

            BioLabBuilding bioLab = labObject.AddComponent<BioLabBuilding>();
            bioLab.Configure(visualRootObject.transform, label, bodyObject.transform, bodyObject.GetComponent<Renderer>(), domeObject.transform, referenceModelObject != null ? referenceModelObject.transform : null, detailRootObject.transform, symbolRootObject.transform, symbolRenderers, progressRootObject.transform, progressFillMeshFilter, glowObject.transform, audioSource, startUpgradeAction);
            return bioLab;
        }

        private static UpgradeableFacilityBuilding CreateHangarBuilding(Material bodyMaterial, Material referenceMaterial, Material referenceGlowMaterial, Material symbolMaterial, Material progressBackMaterial, Material progressFillMaterial, Func<bool> startUpgradeAction)
        {
            // The hangar occupies the right-side reserved base slot that previously showed only a future pad.
            return CreateUpgradeableFacilityBuilding(
                "Hangar",
                new Vector3(2.05f, 0.10f, 0.75f),
                bodyMaterial,
                referenceMaterial,
                referenceGlowMaterial,
                symbolMaterial,
                progressBackMaterial,
                progressFillMaterial,
                startUpgradeAction,
                new Vector2(142f, 116f),
                new Vector3(0f, 0.78f, -0.54f),
                new Vector3(0f, 0.78f, -0.49f),
                new Vector3(0f, 1.36f, -0.18f),
                new Vector3(0f, 1.46f, -0.02f),
                new Vector3(0f, 1.10f, -0.36f),
                1.42f,
                1.04f,
                0.82f,
                0.025f,
                2.06f,
                1.69f,
                0.035f,
                0.08f,
                new Color(0.28f, 0.32f, 0.32f));
        }

        private static UpgradeableFacilityBuilding CreateTrainingFacilityBuilding(Material bodyMaterial, Material referenceMaterial, Material referenceGlowMaterial, Material symbolMaterial, Material progressBackMaterial, Material progressFillMaterial, Func<bool> startUpgradeAction)
        {
            // The training facility occupies the diagonal rear slot so its obstacle-course art stays readable.
            return CreateUpgradeableFacilityBuilding(
                "Training Facility",
                new Vector3(1.60f, 0.10f, 4.85f),
                bodyMaterial,
                referenceMaterial,
                referenceGlowMaterial,
                symbolMaterial,
                progressBackMaterial,
                progressFillMaterial,
                startUpgradeAction,
                new Vector2(146f, 116f),
                new Vector3(0f, 0.76f, -0.54f),
                new Vector3(0f, 0.76f, -0.49f),
                new Vector3(0f, 1.34f, -0.18f),
                new Vector3(0f, 1.43f, -0.02f),
                new Vector3(0f, 1.08f, -0.36f),
                1.48f,
                1.02f,
                0.80f,
                0.025f,
                2.12f,
                1.64f,
                0.035f,
                0.08f,
                new Color(0.30f, 0.33f, 0.32f));
        }

        private static UpgradeableFacilityBuilding CreateUpgradeableFacilityBuilding(string displayName, Vector3 rootPosition, Material bodyMaterial, Material referenceMaterial, Material referenceGlowMaterial, Material symbolMaterial, Material progressBackMaterial, Material progressFillMaterial, Func<bool> startUpgradeAction, Vector2 clickSizePixels, Vector3 referenceLocalPosition, Vector3 glowReferenceLocalPosition, Vector3 symbolLocalPosition, Vector3 progressLocalPosition, Vector3 labelLocalPosition, float visualWidth, float visualDepth, float visualHeight, float heightPerLevel, float referenceWidth, float referenceHeight, float referenceHeightPerLevel, float referenceGlowPadding, Color bodyColor)
        {
            // The facility root owns map placement while its visual root can pop on upgrade completion.
            GameObject facilityObject = new(displayName);
            facilityObject.transform.position = rootPosition;

            // The visual root lets completion effects bounce the art without moving the logical map slot.
            GameObject visualRootObject = new($"{displayName} Visual Root");
            visualRootObject.transform.SetParent(facilityObject.transform, false);

            // A simple scaffold cube stays present for fallback rendering, sizing, and test inspection.
            GameObject bodyObject = PrototypeGeometryFactory.CreateCube($"{displayName} Body", Vector3.zero, Vector3.one, bodyMaterial);
            bodyObject.transform.SetParent(visualRootObject.transform, false);

            // The reference-textured model is the visible source of truth for matching the generated concept image.
            GameObject referenceModelObject = CreateReferenceQuad(visualRootObject.transform, referenceMaterial, $"{displayName} Reference Model", $"{displayName} Reference Model Quad");
            if (referenceModelObject != null)
            {
                // Keep the scaffold available for alignment and tests without letting it alter the visual match.
                SetRenderersEnabled(bodyObject.transform, false);
            }

            // A fallback label keeps missing-texture builds identifiable in the angled Base camera.
            GameObject labelObject = new($"{displayName} Label");
            labelObject.transform.SetParent(visualRootObject.transform, false);
            labelObject.transform.localPosition = labelLocalPosition;
            labelObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.16f;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 1f;
            label.color = Color.white;
            labelObject.SetActive(referenceModelObject == null);

            // The upgrade symbol is generated above the facility and hidden until the facility is tapped.
            GameObject symbolRootObject = CreateFacilityUpgradeSymbol(displayName, visualRootObject.transform, symbolMaterial, out Renderer[] symbolRenderers);

            // The circular progress icon overlays the facility while the saved timer is active.
            GameObject progressRootObject = CreateFacilityProgressIcon(displayName, visualRootObject.transform, progressBackMaterial, progressFillMaterial, out MeshFilter progressFillMeshFilter);

            // The completion glow uses the same reference silhouette when the exact concept model is visible.
            GameObject glowObject = CreateFacilityCompletionGlow(displayName, visualRootObject.transform, referenceModelObject != null ? referenceGlowMaterial : null);

            UpgradeableFacilityBuilding facilityBuilding = facilityObject.AddComponent<UpgradeableFacilityBuilding>();
            facilityBuilding.Configure(displayName, visualRootObject.transform, label, bodyObject.transform, bodyObject.GetComponent<Renderer>(), referenceModelObject != null ? referenceModelObject.transform : null, symbolRootObject.transform, symbolRenderers, progressRootObject.transform, progressFillMeshFilter, glowObject.transform, startUpgradeAction, clickSizePixels, referenceLocalPosition, glowReferenceLocalPosition, symbolLocalPosition, progressLocalPosition, labelLocalPosition, visualWidth, visualDepth, visualHeight, heightPerLevel, referenceWidth, referenceHeight, referenceHeightPerLevel, referenceGlowPadding, bodyColor);
            return facilityBuilding;
        }

        private static GameObject CreateFacilityUpgradeSymbol(string displayName, Transform parent, Material symbolMaterial, out Renderer[] symbolRenderers)
        {
            // The popup symbol is a simple flat upward arrow made from 2D meshes.
            GameObject symbolRootObject = new($"{displayName} Upgrade Symbol");
            symbolRootObject.transform.SetParent(parent, false);

            // The stem is a flat rectangle so the arrow reads as 2D rather than a raised block.
            GameObject stemObject = CreateFlatArrowStem($"{displayName} Upgrade Symbol Stem", 0.16f, 0.32f, symbolMaterial);
            stemObject.transform.SetParent(symbolRootObject.transform, false);
            stemObject.transform.localPosition = new Vector3(0f, -0.12f, 0f);

            // The arrow head is a flat triangle paired with the flat stem.
            GameObject arrowHeadObject = CreateFlatArrowHead($"{displayName} Upgrade Symbol Arrow Head", 0.42f, 0.28f, symbolMaterial);
            arrowHeadObject.transform.SetParent(symbolRootObject.transform, false);
            arrowHeadObject.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            // The symbol label makes the affordance readable in early placeholder art.
            GameObject textObject = new($"{displayName} Upgrade Symbol Text");
            textObject.transform.SetParent(symbolRootObject.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0.36f, -0.08f);
            textObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            textObject.transform.localScale = Vector3.one * 0.12f;
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 1f;
            text.color = Color.white;
            text.text = "UP";

            symbolRenderers = new[]
            {
                stemObject.GetComponent<Renderer>(),
                arrowHeadObject.GetComponent<Renderer>()
            };
            symbolRootObject.SetActive(false);
            return symbolRootObject;
        }

        private static GameObject CreateFacilityProgressIcon(string displayName, Transform parent, Material backMaterial, Material fillMaterial, out MeshFilter fillMeshFilter)
        {
            // The progress root sits over the facility roof like a diegetic circular timer.
            GameObject progressRootObject = new($"{displayName} Progress Icon");
            progressRootObject.transform.SetParent(parent, false);

            // A dark background disc makes partial fill readable against detailed reference art.
            GameObject backDiscObject = PrototypeGeometryFactory.CreateCylinder($"{displayName} Progress Back Disc", Vector3.zero, new Vector3(0.88f, 0.035f, 0.88f), backMaterial);
            backDiscObject.transform.SetParent(progressRootObject.transform, false);

            // The fill mesh is rebuilt as a pie wedge by UpgradeableFacilityBuilding.
            GameObject fillObject = new($"{displayName} Progress Fill");
            fillObject.transform.SetParent(progressRootObject.transform, false);
            fillObject.transform.localPosition = new Vector3(0f, 0.032f, 0f);
            fillMeshFilter = fillObject.AddComponent<MeshFilter>();
            MeshRenderer fillRenderer = fillObject.AddComponent<MeshRenderer>();
            fillRenderer.sharedMaterial = fillMaterial;

            // Start hidden until an active upgrade timer exists.
            progressRootObject.SetActive(false);
            return progressRootObject;
        }

        private static GameObject CreateFacilityCompletionGlow(string displayName, Transform parent, Material referenceGlowMaterial)
        {
            // The root stays mesh-free so toggling and pulsing cannot produce one large opaque volume.
            GameObject glowRootObject = new($"{displayName} Completion Glow");
            glowRootObject.transform.SetParent(parent, false);

            // The reference aura exactly matches the generated facility silhouette.
            CreateReferenceQuad(glowRootObject.transform, referenceGlowMaterial, $"{displayName} Completion Reference Aura", $"{displayName} Completion Reference Aura Quad");

            // Completion feedback starts hidden and is activated by UpgradeableFacilityBuilding.PlayCompletionEffects.
            glowRootObject.SetActive(false);
            return glowRootObject;
        }

        private static Material CreateBioLabReferenceMaterial()
        {
            // The visible reference model should preserve the generated concept image without tinting it.
            return CreateTexturedTransparentMaterial("BioLab/BioLabReferenceCutout", "Bio Lab Reference Cutout Material", Color.white, (int)RenderQueue.Transparent);
        }

        private static Material CreateBioLabReferenceGlowMaterial()
        {
            // The glow texture keeps the reference silhouette but replaces detail pixels with one aura color.
            return CreateTexturedTransparentMaterial("BioLab/BioLabReferenceGlowSilhouette", "Bio Lab Reference Glow Silhouette Material", new Color(0.20f, 1f, 0.72f, 0.40f), (int)RenderQueue.Transparent - 10);
        }

        private static Material CreateHangarReferenceMaterial()
        {
            // The visible hangar model should preserve the generated concept image without tinting it.
            return CreateTexturedTransparentMaterial("Hangar/HangarReferenceCutout", "Hangar Reference Cutout Material", Color.white, (int)RenderQueue.Transparent);
        }

        private static Material CreateHangarReferenceGlowMaterial()
        {
            // The glow texture keeps the exact hangar silhouette but replaces detail pixels with one aura color.
            return CreateTexturedTransparentMaterial("Hangar/HangarReferenceGlowSilhouette", "Hangar Reference Glow Silhouette Material", new Color(0.20f, 1f, 0.72f, 0.40f), (int)RenderQueue.Transparent - 10);
        }

        private static Material CreateTrainingReferenceMaterial()
        {
            // The visible training model should preserve the generated concept image without tinting it.
            return CreateTexturedTransparentMaterial("Training/TrainingFacilityReferenceCutout", "Training Facility Reference Cutout Material", Color.white, (int)RenderQueue.Transparent);
        }

        private static Material CreateTrainingReferenceGlowMaterial()
        {
            // The glow texture keeps the exact training silhouette but replaces detail pixels with one aura color.
            return CreateTexturedTransparentMaterial("Training/TrainingFacilityReferenceGlowSilhouette", "Training Facility Reference Glow Silhouette Material", new Color(0.20f, 1f, 0.72f, 0.40f), (int)RenderQueue.Transparent - 10);
        }

        private static Material CreateTexturedTransparentMaterial(string resourcePath, string materialName, Color tintColor, int renderQueue)
        {
            // Resources keeps art available in builds without hard-coded filesystem paths.
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            // Prefer unlit transparent shaders so reference art and aura colors are not altered by scene lighting.
            Shader shader = Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Standard");
            if (shader == null)
            {
                return null;
            }

            // The material owns the texture, tint, and draw order for one reference-backed world quad.
            Material material = new(shader)
            {
                name = materialName,
                mainTexture = texture,
                renderQueue = renderQueue
            };

            // Transparent setup keeps cutout backgrounds from drawing over the base floor.
            material.SetOverrideTag("RenderType", "Transparent");
            SetMaterialTextureIfPresent(material, "_MainTex", texture);
            SetMaterialTextureIfPresent(material, "_BaseMap", texture);
            SetMaterialColorIfPresent(material, "_Color", tintColor);
            SetMaterialColorIfPresent(material, "_BaseColor", tintColor);
            SetMaterialColorIfPresent(material, "_TintColor", tintColor);
            SetMaterialFloatIfPresent(material, "_Surface", 1f);
            SetMaterialFloatIfPresent(material, "_Mode", 3f);
            SetMaterialFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetMaterialFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetMaterialFloatIfPresent(material, "_ZWrite", 0f);
            SetMaterialFloatIfPresent(material, "_Cull", (float)CullMode.Off);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            return material;
        }

        private static GameObject CreateBioLabReferenceModel(Transform parent, Material referenceMaterial)
        {
            // The reference model names the player-visible quad used as the exact biolab art.
            return CreateReferenceQuad(parent, referenceMaterial, "Bio Lab Reference Model", "Bio Lab Reference Model Quad");
        }

        private static GameObject CreateBioLabReferenceGlowAura(Transform parent, Material referenceGlowMaterial)
        {
            // The completion aura uses the same cutout shape as the visible model so the pulse follows the outline.
            return CreateReferenceQuad(parent, referenceGlowMaterial, "Bio Lab Completion Reference Aura", "Bio Lab Completion Reference Aura Quad");
        }

        private static GameObject CreateReferenceQuad(Transform parent, Material referenceMaterial, string objectName, string meshName)
        {
            // Missing material means the procedural fallback remains visible instead of producing an empty building.
            if (referenceMaterial == null)
            {
                return null;
            }

            // A quad preserves the exact generated concept pixels while still living inside the 3D base world.
            GameObject modelObject = new(objectName);
            modelObject.transform.SetParent(parent, false);

            // The quad is authored in local X/Y space so BioLabBuilding can size and lift it per level.
            Mesh mesh = new()
            {
                name = meshName,
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f)
                },
                normals = new[]
                {
                    Vector3.back,
                    Vector3.back,
                    Vector3.back,
                    Vector3.back
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f)
                },
                triangles = new[]
                {
                    0, 2, 1,
                    0, 3, 2,
                    0, 1, 2,
                    0, 2, 3
                }
            };
            mesh.RecalculateBounds();

            // MeshFilter owns the generated quad mesh without adding runtime colliders.
            MeshFilter meshFilter = modelObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            // MeshRenderer draws the imported reference image or matching silhouette aura.
            MeshRenderer meshRenderer = modelObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = referenceMaterial;
            return modelObject;
        }

        private static void CreateBioLabPlinth(Transform plinthParent, Material trimMaterial, Material darkMaterial, Material lightMaterial)
        {
            // The lower teal lip echoes the concept art's colored platform edge.
            CreateBioLabFreeDetail(plinthParent, lightMaterial, "Bio Lab Plinth Teal Lip", new Vector3(0f, -0.035f, 0f), new Vector3(1.72f, 0.055f, 1.36f), Quaternion.identity);

            // A dark inset shadow separates the stone deck from the bright outer lip.
            CreateBioLabFreeDetail(plinthParent, darkMaterial, "Bio Lab Plinth Shadow Inset", new Vector3(0f, 0.005f, 0f), new Vector3(1.60f, 0.055f, 1.24f), Quaternion.identity);

            // The broad warm-grey deck is the main stone base visible around the building.
            CreateBioLabFreeDetail(plinthParent, trimMaterial, "Bio Lab Plinth Stone Deck", new Vector3(0f, 0.055f, 0f), new Vector3(1.48f, 0.070f, 1.12f), Quaternion.identity);

            // Thin dark inset panels make the top of the plinth read as layered and beveled.
            CreateBioLabFreeDetail(plinthParent, darkMaterial, "Bio Lab Plinth Top Inset", new Vector3(0f, 0.098f, 0f), new Vector3(1.22f, 0.025f, 0.90f), Quaternion.identity);

            // Individual corner caps give the platform the segmented, clipped-corner look from the reference.
            CreateBioLabFreeDetail(plinthParent, trimMaterial, "Bio Lab Plinth Front Left Corner Cap", new Vector3(-0.63f, 0.125f, -0.48f), new Vector3(0.22f, 0.060f, 0.16f), Quaternion.Euler(0f, -30f, 0f));
            CreateBioLabFreeDetail(plinthParent, trimMaterial, "Bio Lab Plinth Front Right Corner Cap", new Vector3(0.63f, 0.125f, -0.48f), new Vector3(0.22f, 0.060f, 0.16f), Quaternion.Euler(0f, 30f, 0f));
            CreateBioLabFreeDetail(plinthParent, trimMaterial, "Bio Lab Plinth Back Left Corner Cap", new Vector3(-0.63f, 0.125f, 0.48f), new Vector3(0.22f, 0.060f, 0.16f), Quaternion.Euler(0f, 30f, 0f));
            CreateBioLabFreeDetail(plinthParent, trimMaterial, "Bio Lab Plinth Back Right Corner Cap", new Vector3(0.63f, 0.125f, 0.48f), new Vector3(0.22f, 0.060f, 0.16f), Quaternion.Euler(0f, -30f, 0f));
        }

        private static void CreateBioLabBodyIntegratedDetails(Transform bodyParent, Material trimMaterial, Material darkMaterial, Material lightMaterial)
        {
            // A darker teal panel material creates the reference's inset panels without changing upgrade color.
            Material panelMaterial = CreateMaterial(new Color(0.07f, 0.39f, 0.43f));

            // The sign panel is only a subtle inset, so keep it close to the main body color.
            Material signPanelMaterial = CreateMaterial(new Color(0.08f, 0.48f, 0.52f));

            // Grey trim gives the generated cube the chunky roof/base frame from the concept image.
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Roof Front Trim", new Vector3(0f, 0.555f, -0.525f), new Vector3(0.95f, 0.095f, 0.090f));
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Roof Back Trim", new Vector3(0f, 0.555f, 0.525f), new Vector3(0.95f, 0.095f, 0.090f));
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Roof Left Trim", new Vector3(-0.525f, 0.555f, 0f), new Vector3(0.090f, 0.095f, 0.86f));
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Roof Right Trim", new Vector3(0.525f, 0.555f, 0f), new Vector3(0.090f, 0.095f, 0.86f));

            // Larger roof corner caps recreate the clipped stone blocks visible around the dome.
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Roof Front Left Corner Block", new Vector3(-0.465f, 0.575f, -0.465f), new Vector3(0.21f, 0.11f, 0.16f), Quaternion.Euler(0f, -28f, 0f));
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Roof Front Right Corner Block", new Vector3(0.465f, 0.575f, -0.465f), new Vector3(0.21f, 0.11f, 0.16f), Quaternion.Euler(0f, 28f, 0f));
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Roof Back Left Corner Block", new Vector3(-0.465f, 0.575f, 0.465f), new Vector3(0.21f, 0.11f, 0.16f), Quaternion.Euler(0f, 28f, 0f));
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Roof Back Right Corner Block", new Vector3(0.465f, 0.575f, 0.465f), new Vector3(0.21f, 0.11f, 0.16f), Quaternion.Euler(0f, -28f, 0f));

            // Dark roof deck panels make the dome base sit inside a recessed mechanical ring.
            CreateBioLabBodyDetail(bodyParent, darkMaterial, "Roof Center Recess", new Vector3(0f, 0.608f, 0f), new Vector3(0.78f, 0.035f, 0.66f));
            CreateBioLabBodyDetail(bodyParent, panelMaterial, "Dome Base Teal Ring Front", new Vector3(0f, 0.645f, -0.26f), new Vector3(0.56f, 0.055f, 0.060f));
            CreateBioLabBodyDetail(bodyParent, panelMaterial, "Dome Base Teal Ring Back", new Vector3(0f, 0.645f, 0.26f), new Vector3(0.56f, 0.055f, 0.060f));
            CreateBioLabBodyDetail(bodyParent, panelMaterial, "Dome Base Teal Ring Left", new Vector3(-0.28f, 0.645f, 0f), new Vector3(0.060f, 0.055f, 0.52f));
            CreateBioLabBodyDetail(bodyParent, panelMaterial, "Dome Base Teal Ring Right", new Vector3(0.28f, 0.645f, 0f), new Vector3(0.060f, 0.055f, 0.52f));

            // Base trim keeps the building grounded on the reserved pad without becoming a separate attachment.
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Base Front Trim", new Vector3(0f, -0.525f, -0.525f), new Vector3(1.02f, 0.085f, 0.080f));
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Base Back Trim", new Vector3(0f, -0.525f, 0.525f), new Vector3(1.02f, 0.085f, 0.080f));
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Base Left Trim", new Vector3(-0.525f, -0.525f, 0f), new Vector3(0.080f, 0.085f, 0.96f));
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Base Right Trim", new Vector3(0.525f, -0.525f, 0f), new Vector3(0.080f, 0.085f, 0.96f));

            // Raised teal columns and dark grooves give the body the reference's beveled tower corners.
            CreateBioLabBodyDetail(bodyParent, panelMaterial, "Front Left Corner Trim", new Vector3(-0.455f, 0f, -0.535f), new Vector3(0.135f, 0.98f, 0.060f));
            CreateBioLabBodyDetail(bodyParent, panelMaterial, "Front Right Corner Trim", new Vector3(0.455f, 0f, -0.535f), new Vector3(0.135f, 0.98f, 0.060f));
            CreateBioLabBodyDetail(bodyParent, panelMaterial, "Left Face Front Corner Column", new Vector3(-0.535f, 0f, -0.32f), new Vector3(0.060f, 0.98f, 0.180f));
            CreateBioLabBodyDetail(bodyParent, panelMaterial, "Right Face Front Corner Column", new Vector3(0.535f, 0f, -0.32f), new Vector3(0.060f, 0.98f, 0.180f));

            // Thin dark seams divide the front panels like the reference image's molded metal plates.
            CreateBioLabBodyDetail(bodyParent, darkMaterial, "Front Upper Panel Seam", new Vector3(0f, 0.285f, -0.568f), new Vector3(0.86f, 0.020f, 0.026f));
            CreateBioLabBodyDetail(bodyParent, darkMaterial, "Front Lower Panel Seam", new Vector3(0f, -0.295f, -0.568f), new Vector3(0.86f, 0.020f, 0.026f));
            CreateBioLabBodyDetail(bodyParent, darkMaterial, "Front Center Panel Groove", new Vector3(-0.235f, -0.05f, -0.568f), new Vector3(0.020f, 0.45f, 0.026f));
            CreateBioLabBodyDetail(bodyParent, darkMaterial, "Left Wall Panel Groove", new Vector3(-0.568f, -0.03f, 0.02f), new Vector3(0.026f, 0.58f, 0.020f), Quaternion.Euler(0f, 90f, 0f));

            // A teal inset panel keeps the BIO LAB text integrated into the tower instead of floating over black.
            CreateBioLabBodyDetail(bodyParent, signPanelMaterial, "Front Sign Panel", new Vector3(0.075f, 0.135f, -0.575f), new Vector3(0.45f, 0.39f, 0.030f));

            // Thin cyan strips echo the concept art's sci-fi light bars but stay on the wall face.
            CreateBioLabBodyDetail(bodyParent, lightMaterial, "Front Sign Top Light Strip", new Vector3(0.075f, 0.372f, -0.603f), new Vector3(0.38f, 0.026f, 0.026f));
            CreateBioLabBodyDetail(bodyParent, lightMaterial, "Front Sign Bottom Light Strip", new Vector3(0.075f, -0.080f, -0.603f), new Vector3(0.38f, 0.026f, 0.026f));
            CreateBioLabBodyDetail(bodyParent, lightMaterial, "Front Left Vertical Light Strip", new Vector3(-0.335f, 0.02f, -0.603f), new Vector3(0.030f, 0.50f, 0.026f));
            CreateBioLabBodyDetail(bodyParent, lightMaterial, "Front Right Vertical Light Strip", new Vector3(0.470f, 0.00f, -0.603f), new Vector3(0.030f, 0.54f, 0.026f));
            CreateBioLabBodyDetail(bodyParent, lightMaterial, "Left Side Upper Light Strip", new Vector3(-0.603f, 0.315f, -0.145f), new Vector3(0.030f, 0.028f, 0.31f), Quaternion.Euler(0f, 90f, 0f));

            // A compact dark door panel gives the front face the same architectural anchor as the reference.
            CreateBioLabBodyDetail(bodyParent, darkMaterial, "Front Door Panel", new Vector3(0.060f, -0.375f, -0.603f), new Vector3(0.245f, 0.275f, 0.040f));
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Front Door Left Frame", new Vector3(-0.110f, -0.360f, -0.625f), new Vector3(0.060f, 0.335f, 0.055f));
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Front Door Right Frame", new Vector3(0.230f, -0.360f, -0.625f), new Vector3(0.060f, 0.335f, 0.055f));
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Front Door Top Frame", new Vector3(0.060f, -0.165f, -0.625f), new Vector3(0.340f, 0.065f, 0.055f));
            CreateBioLabBodyDetail(bodyParent, lightMaterial, "Front Door Center Light", new Vector3(0.060f, -0.375f, -0.650f), new Vector3(0.018f, 0.220f, 0.026f));
            CreateBioLabBodyDetail(bodyParent, lightMaterial, "Front Door Top Light", new Vector3(0.060f, -0.205f, -0.655f), new Vector3(0.170f, 0.022f, 0.026f));

            // Two shallow steps reproduce the concept entrance without adding separate exterior machinery.
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Front Entry Upper Step", new Vector3(0.060f, -0.575f, -0.660f), new Vector3(0.360f, 0.055f, 0.160f));
            CreateBioLabBodyDetail(bodyParent, trimMaterial, "Front Entry Lower Step", new Vector3(0.060f, -0.630f, -0.750f), new Vector3(0.470f, 0.055f, 0.190f));

            // Small dark vents add scale cues while staying flush with the wall instead of adding outside equipment.
            CreateBioLabVent(bodyParent, darkMaterial, "Front Left Vent", new Vector3(-0.350f, -0.425f, -0.605f), Quaternion.identity);
            CreateBioLabVent(bodyParent, darkMaterial, "Front Right Vent", new Vector3(0.380f, -0.425f, -0.605f), Quaternion.identity);
            CreateBioLabVent(bodyParent, darkMaterial, "Upper Utility Vent", new Vector3(-0.030f, 0.450f, -0.605f), Quaternion.identity);
            CreateBioLabVent(bodyParent, darkMaterial, "Left Side Upper Vent", new Vector3(-0.605f, 0.145f, 0.115f), Quaternion.Euler(0f, 90f, 0f));
            CreateBioLabVent(bodyParent, darkMaterial, "Left Side Lower Vent", new Vector3(-0.605f, -0.385f, 0.170f), Quaternion.Euler(0f, 90f, 0f));
        }

        private static void CreateBioLabDomeIntegratedDetails(Transform domeParent, Material trimMaterial, Material darkMaterial, Material lightMaterial)
        {
            // The low dark ring anchors the glass dome into the same roof socket as the concept art.
            CreateBioLabDomeCylinderDetail(domeParent, darkMaterial, "Dome Lower Socket", new Vector3(0f, -0.395f, 0f), new Vector3(1.05f, 0.070f, 1.05f), Quaternion.identity);

            // A segmented trim ring avoids the former plus-sign look and resembles the reference's roof band.
            CreateBioLabDomeRingSegments(domeParent, trimMaterial);

            // Curved segmented ribs make the smooth roof dome read as a glass lab cap instead of a flat cross.
            CreateBioLabDomeArc(domeParent, trimMaterial, "Front Back Dome Rib", true);
            CreateBioLabDomeArc(domeParent, trimMaterial, "Left Right Dome Rib", false);

            // A small bright glint sells the roof as glass without making a full-screen glow.
            CreateBioLabDomeDetail(domeParent, lightMaterial, "Dome Glass Glint", new Vector3(0.180f, 0.250f, -0.170f), new Vector3(0.040f, 0.230f, 0.026f), Quaternion.Euler(0f, 0f, -18f));
        }

        private static GameObject CreateBioLabBodyDetail(Transform parent, Material material, string detailName, Vector3 localPosition, Vector3 localScale)
        {
            // Most body details lie flat against a front-facing wall and need no extra rotation.
            return CreateBioLabBodyDetail(parent, material, detailName, localPosition, localScale, Quaternion.identity);
        }

        private static GameObject CreateBioLabBodyDetail(Transform parent, Material material, string detailName, Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
        {
            // Body details are primitive strips parented to the body, so height-only upgrades carry them along.
            GameObject detailObject = PrototypeGeometryFactory.CreateCube($"Bio Lab {detailName}", Vector3.zero, localScale, material);
            detailObject.transform.SetParent(parent, false);
            detailObject.transform.localPosition = localPosition;
            detailObject.transform.localRotation = localRotation;
            return detailObject;
        }

        private static GameObject CreateBioLabFreeDetail(Transform parent, Material material, string detailName, Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
        {
            // Free details belong to unscaled helper roots, which keeps plinth and roof layers from growing with upgrades.
            GameObject detailObject = PrototypeGeometryFactory.CreateCube(detailName, Vector3.zero, localScale, material);
            detailObject.transform.SetParent(parent, false);
            detailObject.transform.localPosition = localPosition;
            detailObject.transform.localRotation = localRotation;
            return detailObject;
        }

        private static void CreateBioLabVent(Transform parent, Material material, string ventName, Vector3 localPosition, Quaternion localRotation)
        {
            // The vent backing gives each louver group the dark inset block shown in the concept.
            CreateBioLabBodyDetail(parent, material, ventName, localPosition, new Vector3(0.150f, 0.095f, 0.030f), localRotation);

            // Three thin slats make the vents readable at base-scene camera distance.
            CreateBioLabBodyDetail(parent, material, $"{ventName} Top Slat", localPosition + localRotation * new Vector3(0f, 0.035f, -0.018f), new Vector3(0.165f, 0.014f, 0.022f), localRotation);
            CreateBioLabBodyDetail(parent, material, $"{ventName} Middle Slat", localPosition + localRotation * new Vector3(0f, 0.000f, -0.018f), new Vector3(0.165f, 0.014f, 0.022f), localRotation);
            CreateBioLabBodyDetail(parent, material, $"{ventName} Bottom Slat", localPosition + localRotation * new Vector3(0f, -0.035f, -0.018f), new Vector3(0.165f, 0.014f, 0.022f), localRotation);
        }

        private static void CreateBioLabDomeDetail(Transform parent, Material material, string detailName, Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
        {
            // Dome details are simple raised strips that follow the dome transform as lab height changes.
            GameObject detailObject = PrototypeGeometryFactory.CreateCube($"Bio Lab {detailName}", Vector3.zero, localScale, material);
            detailObject.transform.SetParent(parent, false);
            detailObject.transform.localPosition = localPosition;
            detailObject.transform.localRotation = localRotation;
        }

        private static void CreateBioLabDomeCylinderDetail(Transform parent, Material material, string detailName, Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
        {
            // Cylindrical dome details preserve the rounded roof silhouette from the concept art.
            GameObject detailObject = PrototypeGeometryFactory.CreateCylinder($"Bio Lab {detailName}", Vector3.zero, localScale, material);
            detailObject.transform.SetParent(parent, false);
            detailObject.transform.localPosition = localPosition;
            detailObject.transform.localRotation = localRotation;
        }

        private static void CreateBioLabDomeRingSegments(Transform parent, Material material)
        {
            // Twelve ring blocks make the dome base look segmented like the generated reference.
            const int segmentCount = 12;

            // The flat blocks sit near the lower edge of the scaled dome mesh.
            const float ringRadius = 0.43f;

            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex += 1)
            {
                // Even angular spacing leaves a tiny seam between neighboring ring blocks.
                float angle = Mathf.PI * 2f * segmentIndex / segmentCount;

                // X/Z placement follows the dome's unit local circle before parent scaling.
                Vector3 localPosition = new(Mathf.Cos(angle) * ringRadius, -0.335f, Mathf.Sin(angle) * ringRadius);

                // Tangential rotation makes each rectangular block follow the circular rim.
                Quaternion localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);

                // Slightly longer tangential blocks produce a continuous ring from the game camera.
                CreateBioLabDomeDetail(parent, material, $"Dome Base Ring Segment {segmentIndex + 1}", localPosition, new Vector3(0.150f, 0.060f, 0.055f), localRotation);
            }
        }

        private static void CreateBioLabDomeArc(Transform parent, Material material, string detailName, bool runsFrontToBack)
        {
            // Keep the expected rib name as a root so tests and future prefab tooling can find the whole feature.
            GameObject ribRootObject = new($"Bio Lab {detailName}");
            ribRootObject.transform.SetParent(parent, false);

            // Seven short segments approximate a curved dome rib while staying compatible with primitive meshes.
            const int ribSegmentCount = 7;

            for (int segmentIndex = 0; segmentIndex < ribSegmentCount; segmentIndex += 1)
            {
                // Normalized offset runs from one side of the dome to the other.
                float t = -1f + 2f * segmentIndex / (ribSegmentCount - 1);

                // The rib follows the upper half of the unit sphere in local dome coordinates.
                float horizontal = t * 0.40f;
                float y = Mathf.Sqrt(Mathf.Max(0f, 0.25f - horizontal * horizontal)) + 0.015f;

                // Choose whether this rib spans front/back or left/right across the glass cap.
                Vector3 localPosition = runsFrontToBack
                    ? new Vector3(0f, y, horizontal)
                    : new Vector3(horizontal, y, 0f);

                // Rotate each small cube so its long axis roughly follows the dome curve.
                float tiltDegrees = -t * 34f;
                Quaternion localRotation = runsFrontToBack
                    ? Quaternion.Euler(tiltDegrees, 0f, 0f)
                    : Quaternion.Euler(0f, 0f, -tiltDegrees);

                // The rib segments are thick enough to read at base-camera distance but leave glass visible.
                Vector3 segmentScale = runsFrontToBack
                    ? new Vector3(0.055f, 0.060f, 0.140f)
                    : new Vector3(0.140f, 0.060f, 0.055f);
                GameObject segmentObject = PrototypeGeometryFactory.CreateCube($"Bio Lab {detailName} Segment {segmentIndex + 1}", Vector3.zero, segmentScale, material);
                segmentObject.transform.SetParent(ribRootObject.transform, false);
                segmentObject.transform.localPosition = localPosition;
                segmentObject.transform.localRotation = localRotation;
            }
        }

        private static GameObject CreateBioLabCompletionGlow(Transform parent, Material glowMaterial, Material referenceGlowMaterial)
        {
            // The root stays mesh-free so toggling and pulsing cannot produce one large opaque volume.
            GameObject glowRootObject = new("Bio Lab Completion Glow");
            glowRootObject.transform.SetParent(parent, false);

            // The reference aura is preferred because it exactly matches the generated biolab silhouette.
            GameObject referenceAuraObject = CreateBioLabReferenceGlowAura(glowRootObject.transform, referenceGlowMaterial);
            if (referenceAuraObject == null)
            {
                // The body outline root is resized by BioLabBuilding to hug procedural fallback body dimensions.
                GameObject bodyOutlineRootObject = new("Bio Lab Completion Body Outline");
                bodyOutlineRootObject.transform.SetParent(glowRootObject.transform, false);
                CreateBioLabCompletionBodyOutline(bodyOutlineRootObject.transform, glowMaterial);

                // The dome outline root is resized by BioLabBuilding to follow the procedural fallback roof cap.
                GameObject domeOutlineRootObject = new("Bio Lab Completion Dome Outline");
                domeOutlineRootObject.transform.SetParent(glowRootObject.transform, false);
                CreateBioLabCompletionDomeOutline(domeOutlineRootObject.transform, glowMaterial);
            }

            // Completion feedback starts hidden and is activated by BioLabBuilding.PlayCompletionEffects.
            glowRootObject.SetActive(false);
            return glowRootObject;
        }

        private static void CreateBioLabCompletionBodyOutline(Transform parent, Material glowMaterial)
        {
            // Unit-cube edge strips let BioLabBuilding scale the aura around each level's body dimensions.
            const float edgeOffset = 0.53f;

            // Thin strips look like an outline aura instead of solid replacement walls.
            const float stripThickness = 0.04f;

            // Slight over-length keeps the line corners connected after scale pulse expands the root.
            const float longStripLength = 1.10f;

            // Four vertical corners trace the main readable silhouette of the lab block.
            CreateBioLabCompletionAuraStrip(parent, glowMaterial, "Body Vertical Front Left", new Vector3(-edgeOffset, 0f, -edgeOffset), new Vector3(stripThickness, longStripLength, stripThickness));
            CreateBioLabCompletionAuraStrip(parent, glowMaterial, "Body Vertical Front Right", new Vector3(edgeOffset, 0f, -edgeOffset), new Vector3(stripThickness, longStripLength, stripThickness));
            CreateBioLabCompletionAuraStrip(parent, glowMaterial, "Body Vertical Back Left", new Vector3(-edgeOffset, 0f, edgeOffset), new Vector3(stripThickness, longStripLength, stripThickness));
            CreateBioLabCompletionAuraStrip(parent, glowMaterial, "Body Vertical Back Right", new Vector3(edgeOffset, 0f, edgeOffset), new Vector3(stripThickness, longStripLength, stripThickness));

            // Top and bottom horizontal strips complete the cuboid outline without covering the center faces.
            CreateBioLabCompletionAuraStrip(parent, glowMaterial, "Body Top Front Edge", new Vector3(0f, edgeOffset, -edgeOffset), new Vector3(longStripLength, stripThickness, stripThickness));
            CreateBioLabCompletionAuraStrip(parent, glowMaterial, "Body Top Back Edge", new Vector3(0f, edgeOffset, edgeOffset), new Vector3(longStripLength, stripThickness, stripThickness));
            CreateBioLabCompletionAuraStrip(parent, glowMaterial, "Body Top Left Edge", new Vector3(-edgeOffset, edgeOffset, 0f), new Vector3(stripThickness, stripThickness, longStripLength));
            CreateBioLabCompletionAuraStrip(parent, glowMaterial, "Body Top Right Edge", new Vector3(edgeOffset, edgeOffset, 0f), new Vector3(stripThickness, stripThickness, longStripLength));
            CreateBioLabCompletionAuraStrip(parent, glowMaterial, "Body Bottom Front Edge", new Vector3(0f, -edgeOffset, -edgeOffset), new Vector3(longStripLength, stripThickness, stripThickness));
            CreateBioLabCompletionAuraStrip(parent, glowMaterial, "Body Bottom Back Edge", new Vector3(0f, -edgeOffset, edgeOffset), new Vector3(longStripLength, stripThickness, stripThickness));
            CreateBioLabCompletionAuraStrip(parent, glowMaterial, "Body Bottom Left Edge", new Vector3(-edgeOffset, -edgeOffset, 0f), new Vector3(stripThickness, stripThickness, longStripLength));
            CreateBioLabCompletionAuraStrip(parent, glowMaterial, "Body Bottom Right Edge", new Vector3(edgeOffset, -edgeOffset, 0f), new Vector3(stripThickness, stripThickness, longStripLength));
        }

        private static void CreateBioLabCompletionDomeOutline(Transform parent, Material glowMaterial)
        {
            // Sixteen pieces are enough for a round roof aura while leaving clear gaps around the label.
            const int segmentCount = 16;

            // Thin tangential segments trace the dome base instead of forming a large filled bubble.
            const float segmentThickness = 0.045f;
            const float segmentLength = 0.22f;

            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex += 1)
            {
                // Even spacing keeps the roof aura balanced around the cap outline.
                float angle = Mathf.PI * 2f * segmentIndex / segmentCount;

                // Unit-circle placement lets BioLabBuilding scale the outline to the actual dome width.
                float x = Mathf.Cos(angle) * 0.5f;
                float z = Mathf.Sin(angle) * 0.5f;

                // The tangent direction orients each small segment along the dome perimeter.
                Vector3 tangent = new(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));

                // Each short cube uses its local Z axis as the tangent-aligned long edge.
                GameObject segmentObject = PrototypeGeometryFactory.CreateCube(
                    $"Bio Lab Completion Dome Segment {segmentIndex + 1}",
                    Vector3.zero,
                    new Vector3(segmentThickness, segmentThickness, segmentLength),
                    glowMaterial);
                segmentObject.transform.SetParent(parent, false);
                segmentObject.transform.localPosition = new Vector3(x, 0f, z);
                segmentObject.transform.localRotation = Quaternion.LookRotation(tangent.normalized, Vector3.up);
            }
        }

        private static void CreateBioLabCompletionAuraStrip(Transform parent, Material glowMaterial, string stripName, Vector3 localPosition, Vector3 localScale)
        {
            // Generated cube strips avoid custom imported meshes while still reading as an outline aura.
            GameObject stripObject = PrototypeGeometryFactory.CreateCube($"Bio Lab Completion {stripName}", Vector3.zero, localScale, glowMaterial);
            stripObject.transform.SetParent(parent, false);
            stripObject.transform.localPosition = localPosition;
        }

        private static Component CreateOptionalBioLabAudioSource(GameObject labObject)
        {
            // Resolve AudioSource lazily so the prototype can compile even when AudioModule is not referenced.
            Type audioSourceType = BioLabBuilding.FindOptionalUnityAudioType("UnityEngine.AudioSource");
            if (audioSourceType == null)
            {
                return null;
            }

            // Add the reflected component to the lab root so completion sound follows the building.
            Component audioSource = labObject.AddComponent(audioSourceType);
            SetReflectedProperty(audioSource, "playOnAwake", false);
            SetReflectedProperty(audioSource, "spatialBlend", 0.15f);
            SetReflectedProperty(audioSource, "volume", 0.55f);
            return audioSource;
        }

        private static void SetReflectedProperty(object target, string propertyName, object value)
        {
            // Optional audio properties differ by Unity module version, so silently skip missing setters.
            PropertyInfo propertyInfo = target?.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (propertyInfo == null || !propertyInfo.CanWrite)
            {
                return;
            }

            // Apply simple bool/float values to the reflected AudioSource component.
            propertyInfo.SetValue(target, value);
        }

        private static void SetRenderersEnabled(Transform root, bool isEnabled)
        {
            // Missing roots are valid when the reference material fails and the procedural fallback is in use.
            if (root == null)
            {
                return;
            }

            // Disable only rendering; transforms stay live so tests, progression sizing, and glow alignment still work.
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                {
                    renderer.enabled = isEnabled;
                }
            }
        }

        private static void SetMaterialTextureIfPresent(Material material, string propertyName, Texture texture)
        {
            // Unity shader families do not agree on texture property names, so probe before assigning.
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void SetMaterialColorIfPresent(Material material, string propertyName, Color color)
        {
            // Color property names differ between built-in and URP shaders.
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }

        private static void SetMaterialFloatIfPresent(Material material, string propertyName, float value)
        {
            // Render-state properties are shader-specific, so silently skip unavailable controls.
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static GameObject CreateBioLabUpgradeSymbol(Transform parent, Material symbolMaterial, out Renderer[] symbolRenderers)
        {
            // The popup symbol is a simple flat upward arrow made from 2D meshes.
            GameObject symbolRootObject = new("Bio Lab Upgrade Symbol");
            symbolRootObject.transform.SetParent(parent, false);
            symbolRootObject.transform.localPosition = new Vector3(0f, 1.62f, -0.18f);

            // The stem is a flat rectangle so the arrow reads as 2D rather than a raised block.
            GameObject stemObject = CreateFlatArrowStem("Bio Lab Upgrade Symbol Stem", 0.16f, 0.32f, symbolMaterial);
            stemObject.transform.SetParent(symbolRootObject.transform, false);
            stemObject.transform.localPosition = new Vector3(0f, -0.12f, 0f);

            // The arrow head is a flat triangle paired with the flat stem.
            GameObject arrowHeadObject = CreateFlatArrowHead("Bio Lab Upgrade Symbol Arrow Head", 0.42f, 0.28f, symbolMaterial);
            arrowHeadObject.transform.SetParent(symbolRootObject.transform, false);
            arrowHeadObject.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            // The symbol label makes the affordance readable in early placeholder art.
            GameObject textObject = new("Bio Lab Upgrade Symbol Text");
            textObject.transform.SetParent(symbolRootObject.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0.36f, -0.08f);
            textObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            textObject.transform.localScale = Vector3.one * 0.12f;
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 1f;
            text.color = Color.white;
            text.text = "UP";

            symbolRenderers = new[]
            {
                stemObject.GetComponent<Renderer>(),
                arrowHeadObject.GetComponent<Renderer>()
            };
            symbolRootObject.SetActive(false);
            return symbolRootObject;
        }

        private static GameObject CreateBioLabProgressIcon(Transform parent, Material backMaterial, Material fillMaterial, out MeshFilter fillMeshFilter)
        {
            // The progress root sits over the lab roof like a diegetic circular timer.
            GameObject progressRootObject = new("Bio Lab Progress Icon");
            progressRootObject.transform.SetParent(parent, false);
            progressRootObject.transform.localPosition = new Vector3(0f, 1.55f, -0.02f);

            // A dark background disc makes partial fill readable against the lab body.
            GameObject backDiscObject = PrototypeGeometryFactory.CreateCylinder("Bio Lab Progress Back Disc", Vector3.zero, new Vector3(0.94f, 0.035f, 0.94f), backMaterial);
            backDiscObject.transform.SetParent(progressRootObject.transform, false);

            // The fill mesh is rebuilt as a pie wedge by BioLabBuilding.
            GameObject fillObject = new("Bio Lab Progress Fill");
            fillObject.transform.SetParent(progressRootObject.transform, false);
            fillObject.transform.localPosition = new Vector3(0f, 0.032f, 0f);
            fillMeshFilter = fillObject.AddComponent<MeshFilter>();
            MeshRenderer fillRenderer = fillObject.AddComponent<MeshRenderer>();
            fillRenderer.sharedMaterial = fillMaterial;

            // Start hidden until an active upgrade timer exists.
            progressRootObject.SetActive(false);
            return progressRootObject;
        }

        private static BaseHudController CreateHud()
        {
            // The base HUD is built at runtime so the checked-in scene stays tiny.
            Canvas canvas = CreateCanvas("Base HUD Canvas");
            Font font = GetUiFont();

            Text titleText = CreateText(canvas.transform, "Title Text", "Base", font, new Vector2(0f, -18f), TextAnchor.UpperCenter, new Vector2(160f, 36f));
            Button creditsButton = CreateButton(canvas.transform, "Credits Button", "Credits: 0", font, new Vector2(108f, -82f), new Vector2(0f, 1f), new Vector2(184f, 42f));
            Text creditsButtonText = creditsButton.GetComponentInChildren<Text>();
            GameObject creditsDetailPanel = CreateCreditsDetailPanel(canvas.transform, font, out Text creditsDetailText);
            ConfigureCreditsButtonLabel(creditsButton);
            Text heroPanelTitleText = CreateText(canvas.transform, "Hero Panel Title Text", "Owned Heroes", font, new Vector2(0f, 260f), TextAnchor.LowerCenter, new Vector2(360f, 30f));
            Text heroPanelText = CreateText(canvas.transform, "Hero Panel Text", "None earned yet", font, new Vector2(0f, 214f), TextAnchor.LowerCenter, new Vector2(360f, 58f));
            Text statusText = CreateText(canvas.transform, "Status Text", "Next HQ upgrade", font, new Vector2(0f, 92f), TextAnchor.LowerCenter, new Vector2(360f, 34f));
            Text playHintText = CreateText(canvas.transform, "Play Hint Text", $"Win reward: +{PlayerProgression.MinigameWinCoins} coins", font, new Vector2(0f, 128f), TextAnchor.LowerCenter, new Vector2(360f, 34f));
            Button collectButton = CreateButton(canvas.transform, "Collect Button", "COLLECT", font, new Vector2(-126f, 34f), new Vector2(0.5f, 0f), new Vector2(114f, 46f));
            Button upgradeButton = CreateButton(canvas.transform, "Upgrade Button", "UPGRADE", font, new Vector2(0f, 34f), new Vector2(0.5f, 0f), new Vector2(114f, 46f));
            Button playButton = CreateButton(canvas.transform, "Play Button", "PLAY", font, new Vector2(126f, 34f), new Vector2(0.5f, 0f), new Vector2(114f, 46f));
            Button claimObjectiveButton = CreateButton(canvas.transform, "Claim Objective Button", "CLAIM", font, new Vector2(-46f, -401f), new Vector2(1f, 1f), new Vector2(76f, 32f));
            Button[] missionButtons = CreateMissionButtons(canvas.transform, font);
            Button resetButton = CreateButton(canvas.transform, "Reset Save Button", "RESET", font, new Vector2(-58f, -18f), new Vector2(1f, 1f), new Vector2(72f, 34f));
            Button equipHeroButton = CreateButton(canvas.transform, "Equip Hero Button", "EQUIP", font, new Vector2(-56f, 166f), new Vector2(0.5f, 0f), new Vector2(96f, 36f));
            Button heroesButton = CreateButton(canvas.transform, "Heroes Button", "HEROES", font, new Vector2(56f, 166f), new Vector2(0.5f, 0f), new Vector2(96f, 36f));
            Button zoomInButton = CreateButton(canvas.transform, "Zoom In Button", "+", font, new Vector2(-30f, 42f), new Vector2(1f, 0.5f), new Vector2(44f, 44f));
            Button zoomOutButton = CreateButton(canvas.transform, "Zoom Out Button", "-", font, new Vector2(-30f, -8f), new Vector2(1f, 0.5f), new Vector2(44f, 44f));
            ConfigureZoomButtonLabel(zoomInButton);
            ConfigureZoomButtonLabel(zoomOutButton);

            BaseHudController hud = canvas.gameObject.AddComponent<BaseHudController>();
            hud.Configure(titleText, creditsButton, creditsButtonText, creditsDetailPanel, creditsDetailText, heroPanelTitleText, heroPanelText, statusText, playHintText, collectButton, upgradeButton, playButton, claimObjectiveButton, missionButtons, resetButton, equipHeroButton, heroesButton, zoomInButton, zoomOutButton);
            return hud;
        }

        private static GameObject CreateCreditsDetailPanel(Transform parent, Font font, out Text detailText)
        {
            // The detail panel replaces the former left-side white text stack with one compact expanded surface.
            GameObject panelObject = new("Credits Detail Panel");
            panelObject.transform.SetParent(parent, false);

            // Match the existing gold controls while adding enough opacity for black text to stay readable.
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.96f, 0.82f, 0.28f, 0.94f);

            // Upper-left anchoring keeps the panel visually attached to the Credits button on mobile layouts.
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(16f, -116f);
            panelRect.sizeDelta = new Vector2(254f, 192f);

            // The local base values are grouped into a short black-text summary instead of separate HUD labels.
            detailText = CreateText(panelObject.transform, "Credits Detail Text", "Coins: 0\nHQ Level: 1\nBio Lab: 1\nHangar: 1\nTraining: 1\nHQ Upgrade: Ready\nBio Upgrade: Ready\nHangar Upgrade: Ready\nTraining Upgrade: Ready", font, new Vector2(12f, -10f), TextAnchor.UpperLeft, new Vector2(230f, 168f));
            detailText.color = Color.black;
            detailText.fontSize = 14;
            detailText.lineSpacing = 1f;

            // Start collapsed so the Base view opens without the old left-side text wall.
            panelObject.SetActive(false);
            return panelObject;
        }

        private static Button[] CreateMissionButtons(Transform parent, Font font)
        {
            // Build one compact direct-select button for each authored local mission.
            Button[] buttons = new Button[PlayerProgression.MaxMissionLevel];
            for (int index = 0; index < buttons.Length; index += 1)
            {
                // Buttons are anchored to the top-right so the row grows inward and stays visible on narrow screens.
                int missionLevel = index + 1;
                float xOffset = -326f + index * 42f;
                Button missionButton = CreateButton(parent, $"Mission {missionLevel} Button", $"M{missionLevel}", font, new Vector2(xOffset, -232f), new Vector2(1f, 1f), new Vector2(38f, 32f));
                ConfigureCompactButtonLabel(missionButton);
                buttons[index] = missionButton;
            }

            return buttons;
        }

        private static Canvas CreateCanvas(string name)
        {
            // Screen Space Overlay keeps the UI independent from camera setup.
            GameObject canvasObject = new(name);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390f, 844f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static Text CreateText(Transform parent, string name, string text, Font font, Vector2 anchoredPosition, TextAnchor anchor)
        {
            return CreateText(parent, name, text, font, anchoredPosition, anchor, new Vector2(360f, 40f));
        }

        private static Text CreateText(Transform parent, string name, string text, Font font, Vector2 anchoredPosition, TextAnchor anchor, Vector2 size)
        {
            // UnityEngine.UI.Text is deprecated long-term, but it keeps this prototype dependency-light.
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent, false);

            Text label = textObject.AddComponent<Text>();
            label.text = text;
            label.font = font;
            label.fontSize = 24;
            label.color = Color.white;
            label.alignment = anchor;
            label.raycastTarget = false;

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            Vector2 anchorPoint = AnchorFromTextAnchor(anchor);
            rectTransform.anchorMin = anchorPoint;
            rectTransform.anchorMax = anchorPoint;
            // Match the pivot to the anchor so top-left HUD labels use their x value as an inset instead of straddling the screen edge.
            rectTransform.pivot = anchorPoint;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string text, Font font, Vector2 anchoredPosition, Vector2 anchor)
        {
            return CreateButton(parent, name, text, font, anchoredPosition, anchor, new Vector2(190f, 48f));
        }

        private static Button CreateButton(Transform parent, string name, string text, Font font, Vector2 anchoredPosition, Vector2 anchor, Vector2 size)
        {
            // Buttons use primitive UI colors for now; no paid or imported assets are required.
            GameObject buttonObject = new(name);
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.95f, 0.78f, 0.22f);

            Button button = buttonObject.AddComponent<Button>();

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchor;
            buttonRect.anchorMax = anchor;
            buttonRect.anchoredPosition = anchoredPosition;
            buttonRect.sizeDelta = size;

            Text label = CreateText(buttonObject.transform, "Label", text, font, Vector2.zero, TextAnchor.MiddleCenter);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.color = Color.black;
            return button;
        }

        private static void ConfigureCompactButtonLabel(Button button)
        {
            // Mission buttons are intentionally narrow, so allow their selected marker to shrink instead of clipping.
            Text label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            // Best-fit keeps the compact mission row stable while still naming the selected mission.
            label.fontSize = 18;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 18;
        }

        private static void ConfigureCreditsButtonLabel(Button button)
        {
            // Credit totals can grow during testing, so let the button text shrink before it can clip.
            Text label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            // Keep the credits control readable while preserving its compact top-left footprint.
            label.fontSize = 18;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 18;
        }

        private static void ConfigureZoomButtonLabel(Button button)
        {
            // Zoom buttons are icon-like text controls, so they need larger glyphs than action buttons.
            Text label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            // Best fit keeps the simple plus/minus symbols centered on compact mobile-sized controls.
            label.fontSize = 30;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = 30;
        }

        private static Vector2 AnchorFromTextAnchor(TextAnchor anchor)
        {
            return anchor switch
            {
                TextAnchor.UpperLeft => new Vector2(0f, 1f),
                TextAnchor.UpperCenter => new Vector2(0.5f, 1f),
                TextAnchor.UpperRight => new Vector2(1f, 1f),
                TextAnchor.LowerCenter => new Vector2(0.5f, 0f),
                TextAnchor.MiddleCenter => new Vector2(0.5f, 0.5f),
                _ => new Vector2(0.5f, 0.5f)
            };
        }

        private static Material CreateMaterial(Color color)
        {
            // Use the shared prototype factory so iOS players survive stripped or unavailable named shaders.
            return PrototypeMaterialFactory.Create(color);
        }

        private static Font GetUiFont()
        {
            // Unity 6 provides LegacyRuntime.ttf; Arial is a fallback for older editor layouts.
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
