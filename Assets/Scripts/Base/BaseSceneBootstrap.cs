using System;
using LaneSurvivor.Heroes;
using LaneSurvivor.Progression;
using LaneSurvivor.Rendering;
using LaneSurvivor.Retention;
using LaneSurvivor.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LaneSurvivor.Base
{
    public sealed class BaseSceneBootstrap : MonoBehaviour
    {
        private SaveGameData saveData;

        private HQBuilding hqBuilding;

        private BaseHudController hudController;

        private BaseCameraController cameraController;

        private string statusMessage = string.Empty;

        private void Awake()
        {
            // Load local progress before building UI so the first frame reflects persisted state.
            saveData = SaveGameManager.Load();
            bool saveDirty = DailyObjectiveProgression.EnsureCurrentObjective(saveData, DateTime.UtcNow);

            // Complete any timer that finished while the app was closed.
            if (PlayerProgression.CompleteReadyHqUpgrade(saveData, DateTime.UtcNow))
            {
                statusMessage = "HQ upgrade complete";
                GrantHqMilestoneRewards();
                SaveGameManager.Save(saveData);
                saveDirty = false;
            }
            else if (GrantHqMilestoneRewards())
            {
                SaveGameManager.Save(saveData);
                saveDirty = false;
            }
            else if (saveDirty)
            {
                // Persist a UTC-day objective rollover even when no upgrade or milestone reward also changes the save.
                SaveGameManager.Save(saveData);
            }

            Material groundMaterial = CreateMaterial(new Color(0.18f, 0.25f, 0.24f));
            Material perimeterMaterial = CreateMaterial(new Color(0.08f, 0.13f, 0.12f));
            Material reservedSpaceMaterial = CreateMaterial(new Color(0.30f, 0.35f, 0.32f));
            Material hqMaterial = CreateMaterial(Color.white);
            Material hqDetailMaterial = CreateMaterial(new Color(0.95f, 0.72f, 0.22f));

            cameraController = CreateCamera();
            CreateLight();
            CreateEventSystem();
            CreateGround(groundMaterial, perimeterMaterial, reservedSpaceMaterial);
            hqBuilding = CreateHqBuilding(hqMaterial, hqDetailMaterial);
            hudController = CreateHud();
            hudController.Initialize(CollectCoins, StartHqUpgrade, LaunchMinigame, ClaimDailyObjective, SelectMission, ResetSave, EquipNextOwnedHero, LaunchHeroes, cameraController.ZoomIn, cameraController.ZoomOut);

            RefreshScene();
        }

        private void Update()
        {
            // Poll timer completion locally; no server authority exists in Phase 2.
            if (PlayerProgression.CompleteReadyHqUpgrade(saveData, DateTime.UtcNow))
            {
                statusMessage = "HQ upgrade complete";
                GrantHqMilestoneRewards();
                SaveGameManager.Save(saveData);
                RefreshScene();
                return;
            }

            // Refresh the countdown each frame while an upgrade is active.
            if (saveData.hqUpgradeInProgress)
            {
                RefreshScene();
            }
        }

        private void CollectCoins()
        {
            // Save immediately so tapping collect then closing the app preserves progress.
            PlayerProgression.CollectCoins(saveData);
            SaveGameManager.Save(saveData);
            statusMessage = $"+{PlayerProgression.CoinsPerCollect} coins collected";
            RefreshScene();
        }

        private void StartHqUpgrade()
        {
            // The progression layer handles cost and duplicate-timer validation.
            if (PlayerProgression.TryStartHqUpgrade(saveData, DateTime.UtcNow))
            {
                SaveGameManager.Save(saveData);
                statusMessage = "HQ upgrade started";
                RefreshScene();
            }
        }

        private void ResetSave()
        {
            // Reset through the save manager so the same default data is written to disk and shown in UI.
            saveData = SaveGameManager.ResetToFreshData();
            statusMessage = "Local save reset";
            RefreshScene();
        }

        private void SelectMission(int missionLevel)
        {
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
            // Save before leaving the base scene so the minigame sees the latest HQ bonus.
            SaveGameManager.Save(saveData);
            SceneManager.LoadScene("Minigame");
        }

        private void LaunchHeroes()
        {
            // Save before leaving the base scene so the Hero screen sees the latest local state.
            SaveGameManager.Save(saveData);
            SceneManager.LoadScene("Heroes");
        }

        private void RefreshScene()
        {
            // Keep world and HUD state synchronized from one saved data object.
            int remainingSeconds = PlayerProgression.GetHqUpgradeRemainingSeconds(saveData, DateTime.UtcNow);
            hqBuilding.ApplySaveData(saveData);
            hudController.UpdateView(saveData, remainingSeconds, statusMessage);
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

            // The controller handles pinch, scroll, keyboard, and HUD-button zoom without moving overlay UI.
            BaseCameraController zoomController = cameraObject.AddComponent<BaseCameraController>();
            zoomController.Configure(camera);
            return zoomController;
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

        private static void CreateGround(Material groundMaterial, Material perimeterMaterial, Material reservedSpaceMaterial)
        {
            // The base footprint is now a pentagon so future defenses and openings have a clear perimeter.
            PrototypeGeometryFactory.CreateRegularPrism("Base Ground", new Vector3(0f, -0.06f, 0f), new Vector3(12f, 0.12f, 12f), 5, groundMaterial);

            // Low perimeter strips reserve wall placement without implementing full base-defense systems yet.
            CreatePentagonPerimeterSegments("Future Wall Space", 5.45f, 0.04f, 0.13f, 0.18f, perimeterMaterial);

            // A larger second ring marks future moat space while leaving the playable base interior clear.
            CreatePentagonPerimeterSegments("Future Moat Space", 6.2f, 0.005f, 0.055f, 0.15f, perimeterMaterial);

            // Reserve a front opening where later rescued humans, trucks, or resources can enter the base.
            CreateReservedPad("Future Gate Space", new Vector3(0f, 0.04f, -4.25f), new Vector3(1.55f, 0.08f, 0.54f), reservedSpaceMaterial);

            // Reserve a separate unload opening so future truck/resource loops have a distinct destination.
            CreateReservedPad("Future Resource Drop-Off Opening", new Vector3(2.35f, 0.04f, -3.05f), new Vector3(1.22f, 0.08f, 0.48f), reservedSpaceMaterial);

            // Future lab, hangar, and training pads keep strategic expansion space visible from the first Base slice.
            CreateReservedPad("Future Lab Pad", new Vector3(-2.35f, 0.04f, 0.75f), new Vector3(1.35f, 0.08f, 1.05f), reservedSpaceMaterial);
            CreateReservedPad("Future Hangar Pad", new Vector3(2.35f, 0.04f, 0.75f), new Vector3(1.45f, 0.08f, 1.12f), reservedSpaceMaterial);
            CreateReservedPad("Future Training Pad", new Vector3(0f, 0.04f, 3.25f), new Vector3(1.75f, 0.08f, 0.9f), reservedSpaceMaterial);
        }

        private static void CreatePentagonPerimeterSegments(string namePrefix, float radius, float yPosition, float height, float thickness, Material material)
        {
            for (int sideIndex = 0; sideIndex < 5; sideIndex += 1)
            {
                // Adjacent pentagon points define one perimeter segment.
                Vector2 firstPoint = GetPentagonPoint(radius, sideIndex);
                Vector2 secondPoint = GetPentagonPoint(radius, (sideIndex + 1) % 5);
                Vector2 edge = secondPoint - firstPoint;
                Vector2 midpoint = (firstPoint + secondPoint) * 0.5f;

                // Shorten each strip a little so future gate and moat breaks remain readable.
                GameObject segment = PrototypeGeometryFactory.CreateCube(
                    $"{namePrefix} {sideIndex + 1}",
                    new Vector3(midpoint.x, yPosition, midpoint.y),
                    new Vector3(edge.magnitude * 0.78f, height, thickness),
                    material);

                // Rotate the cube's local X axis onto the pentagon edge.
                segment.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(-edge.y, edge.x) * Mathf.Rad2Deg, 0f);
            }
        }

        private static void CreateReservedPad(string name, Vector3 position, Vector3 scale, Material material)
        {
            // Low pads are deliberately non-functional placeholders that keep future build slots visible.
            PrototypeGeometryFactory.CreateCube(name, position, scale, material);

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

        private static Vector2 GetPentagonPoint(float radius, int pointIndex)
        {
            // Match the regular-prism mesh orientation so perimeter markers track the ground edge directions.
            float angle = Mathf.PI * 0.5f + pointIndex * Mathf.PI * 2f / 5f;
            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }

        private static HQBuilding CreateHqBuilding(Material hqMaterial, Material hqDetailMaterial)
        {
            // The HQ root owns unscaled labels and detail rows while the body child grows per level.
            GameObject hqObject = new("HQ Building");

            // A five-sided prism makes the HQ read as part of the pentagon base plan.
            GameObject hqBodyObject = PrototypeGeometryFactory.CreateRegularPrism("HQ Body", Vector3.zero, Vector3.one, 5, hqMaterial);
            hqBodyObject.transform.SetParent(hqObject.transform, false);

            // Detail rows are generated under a container so HQBuilding can rebuild them by level.
            GameObject detailRootObject = new("HQ Detail Root");
            detailRootObject.transform.SetParent(hqObject.transform, false);

            // A world-space TextMesh labels the placeholder building without needing UI layout.
            GameObject labelObject = new("HQ Label");
            labelObject.transform.SetParent(hqObject.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 2.05f, -0.45f);
            labelObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.22f;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 1f;
            label.color = Color.white;

            HQBuilding hqBuilding = hqObject.AddComponent<HQBuilding>();
            hqBuilding.Configure(label, hqBodyObject.transform, hqBodyObject.GetComponent<Renderer>(), detailRootObject.transform, hqDetailMaterial);
            return hqBuilding;
        }

        private static BaseHudController CreateHud()
        {
            // The base HUD is built at runtime so the checked-in scene stays tiny.
            Canvas canvas = CreateCanvas("Base HUD Canvas");
            Font font = GetUiFont();

            Text titleText = CreateText(canvas.transform, "Title Text", "Base", font, new Vector2(0f, -18f), TextAnchor.UpperCenter, new Vector2(160f, 36f));
            Text coinsText = CreateText(canvas.transform, "Coins Text", "Coins: 0", font, new Vector2(16f, -58f), TextAnchor.UpperLeft, new Vector2(240f, 34f));
            Text hqText = CreateText(canvas.transform, "HQ Text", "HQ Level: 1", font, new Vector2(16f, -90f), TextAnchor.UpperLeft, new Vector2(240f, 34f));
            Text timerText = CreateText(canvas.transform, "Timer Text", "Upgrade: Ready", font, new Vector2(16f, -122f), TextAnchor.UpperLeft, new Vector2(280f, 34f));
            Text heroText = CreateText(canvas.transform, "Hero Text", "Hero: None", font, new Vector2(16f, -154f), TextAnchor.UpperLeft, new Vector2(340f, 34f));
            Text missionPanelTitleText = CreateText(canvas.transform, "Mission Panel Title Text", "Missions", font, new Vector2(16f, -190f), TextAnchor.UpperLeft, new Vector2(184f, 28f));
            Text missionPanelText = CreateText(canvas.transform, "Mission Panel Text", "M1 Outskirts", font, new Vector2(16f, -224f), TextAnchor.UpperLeft, new Vector2(358f, 150f));
            Text objectiveText = CreateText(canvas.transform, "Objective Text", "Daily: 0/2 wins", font, new Vector2(16f, -384f), TextAnchor.UpperLeft, new Vector2(268f, 34f));
            // Eight mission rows need compact type so the panel still fits the phone-sized reference HUD.
            missionPanelTitleText.fontSize = 22;
            missionPanelText.fontSize = 12;
            missionPanelText.lineSpacing = 0.86f;
            objectiveText.fontSize = 15;
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
            hud.Configure(titleText, coinsText, hqText, timerText, heroText, missionPanelTitleText, missionPanelText, objectiveText, heroPanelTitleText, heroPanelText, statusText, playHintText, collectButton, upgradeButton, playButton, claimObjectiveButton, missionButtons, resetButton, equipHeroButton, heroesButton, zoomInButton, zoomOutButton);
            return hud;
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
                Button missionButton = CreateButton(parent, $"Mission {missionLevel} Button", $"M{missionLevel}", font, new Vector2(xOffset, -196f), new Vector2(1f, 1f), new Vector2(38f, 32f));
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
