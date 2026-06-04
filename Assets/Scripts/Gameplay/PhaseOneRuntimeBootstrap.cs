using LaneSurvivor.Data;
using LaneSurvivor.Heroes;
using LaneSurvivor.Progression;
using LaneSurvivor.Rendering;
using LaneSurvivor.Save;
using LaneSurvivor.UI;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LaneSurvivor.Gameplay
{
    public sealed class PhaseOneRuntimeBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Material trackMaterial = CreateMaterial(new Color(0.20f, 0.24f, 0.22f));
            Material gateMaterial = CreateMaterial(new Color(0.10f, 0.45f, 0.95f));
            Material zombieMaterial = CreateMaterial(new Color(0.18f, 0.55f, 0.18f));
            Material playerMaterial = CreateMaterial(new Color(0.12f, 0.88f, 0.98f));
            Material playerBeaconMaterial = CreateMaterial(new Color(1f, 0.1f, 0.8f));

            LevelDefinition levelDefinition = CreateLevelDefinition();
            PlayerSquad playerSquad = CreatePlayerSquad(playerMaterial, playerBeaconMaterial);
            AutoShooter autoShooter = playerSquad.gameObject.AddComponent<AutoShooter>();
            SquadLaneInput laneInput = playerSquad.gameObject.AddComponent<SquadLaneInput>();

            Camera gameplayCamera = CreateCamera(playerSquad.transform);
            CreateLight();
            CreateEventSystem();

            MinigameHudController hudController = CreateHud(playerSquad.transform, gameplayCamera);
            EndScreenController endScreenController = CreateEndScreen();
            laneInput.Configure(playerSquad, hudController.LeftLaneButton, hudController.RightLaneButton);

            LevelManager levelManager = new GameObject("Level Manager").AddComponent<LevelManager>();
            levelManager.Configure(levelDefinition, playerSquad, autoShooter, hudController, endScreenController, trackMaterial, gateMaterial, zombieMaterial);

            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
        }

        private static LevelDefinition CreateLevelDefinition()
        {
            SaveGameData saveData = SaveGameManager.Load();
            bool saveNeeded = false;
            if (PlayerProgression.CompleteReadyHqUpgrade(saveData, DateTime.UtcNow))
            {
                saveNeeded = true;
            }

            // HQ milestone hero rewards can complete while entering the minigame from a stale save.
            if (HeroRewardSystem.TryGrantHqLevelTwoHero(saveData) != null)
            {
                saveNeeded = true;
            }

            if (saveNeeded)
            {
                SaveGameManager.Save(saveData);
            }

            // Level selection is data-derived so HQ unlocks can switch to level 2 without scene edits.
            return LevelDefinitionFactory.CreateForSave(saveData);
        }

        private static PlayerSquad CreatePlayerSquad(Material playerMaterial, Material beaconMaterial)
        {
            // A collider-free cube avoids pulling physics modules into the simulator prototype.
            GameObject playerObject = PrototypeGeometryFactory.CreateCube("Player Squad", new Vector3(0f, GameplayVisuals.PlayerCenterY, 0f), new Vector3(GameplayVisuals.PlayerFootprint, GameplayVisuals.PlayerHeight, GameplayVisuals.PlayerFootprint), playerMaterial);

            // A trailing marker avoids the center lane stripe and remains readable immediately after gate overlaps.
            GameObject beaconObject = PrototypeGeometryFactory.CreateCube("Player Squad Beacon", playerObject.transform.position + new Vector3(0f, GameplayVisuals.PlayerBeaconOffsetY, GameplayVisuals.PlayerBeaconBackOffsetZ), new Vector3(GameplayVisuals.PlayerBeaconFootprint, GameplayVisuals.PlayerBeaconHeight, GameplayVisuals.PlayerBeaconFootprint), beaconMaterial);
            beaconObject.transform.SetParent(playerObject.transform, true);

            // A thin mast keeps the squad position visible during the full run without making the squad body oversized.
            GameObject mastObject = PrototypeGeometryFactory.CreateCube("Player Squad Visibility Mast", playerObject.transform.position + new Vector3(0f, GameplayVisuals.PlayerMastOffsetY, 0f), new Vector3(GameplayVisuals.PlayerMastWidth, GameplayVisuals.PlayerMastHeight, GameplayVisuals.PlayerMastWidth), beaconMaterial);
            mastObject.transform.SetParent(playerObject.transform, true);

            // The gameplay object still drives rules and camera motion, while the HUD marker owns visibility.
            SetWorldPlayerRendererVisibility(playerObject, GameplayVisuals.WorldPlayerMeshRenderersEnabled);

            return playerObject.AddComponent<PlayerSquad>();
        }

        private static void SetWorldPlayerRendererVisibility(GameObject playerRoot, bool isVisible)
        {
            // Hide every world-space player renderer so the simulator cannot intermittently depth-cull the squad.
            foreach (Renderer renderer in playerRoot.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = isVisible;
            }
        }

        private static Camera CreateCamera(Transform target)
        {
            GameObject cameraObject = new("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            // Perspective framing preserves a forward chase angle instead of a top-down board view.
            camera.orthographic = false;
            camera.fieldOfView = GameplayVisuals.CameraFieldOfView;
            camera.nearClipPlane = 0.05f;
            camera.tag = "MainCamera";

            SimpleCameraFollow follow = cameraObject.AddComponent<SimpleCameraFollow>();
            // Shared camera constants keep runtime scenes aligned with editor-regenerated scenes.
            follow.Initialize(target, GameplayVisuals.CameraOffset, GameplayVisuals.CameraLookAtOffset, false);
            return camera;
        }

        private static void CreateLight()
        {
            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static MinigameHudController CreateHud(Transform playerTarget, Camera gameplayCamera)
        {
            Canvas canvas = CreateCanvas("HUD Canvas");
            Font font = GetUiFont();

            Text squadText = CreateText(canvas.transform, "Squad Text", "Squad: 0", font, new Vector2(20f, -20f), TextAnchor.UpperLeft);
            Text progressText = CreateText(canvas.transform, "Progress Text", "Progress: 0%", font, new Vector2(20f, -55f), TextAnchor.UpperLeft);
            Text stateText = CreateText(canvas.transform, "State Text", "Ready", font, new Vector2(0f, -20f), TextAnchor.UpperCenter);
            Button startButton = CreateButton(canvas.transform, "Start Button", "START", font, new Vector2(0f, -95f));
            Button leftButton = CreateButton(canvas.transform, "Left Lane Button", "<", font, new Vector2(-120f, 60f), new Vector2(0.5f, 0f));
            Button rightButton = CreateButton(canvas.transform, "Right Lane Button", ">", font, new Vector2(120f, 60f), new Vector2(0.5f, 0f));

            if (GameplayVisuals.UseScreenSpacePlayerMarker)
            {
                // The overlay marker follows the same player transform used by gameplay and camera logic.
                PlayerSquadScreenMarker.Create(canvas.GetComponent<RectTransform>(), playerTarget, gameplayCamera);
            }

            MinigameHudController hudController = canvas.gameObject.AddComponent<MinigameHudController>();
            hudController.Configure(squadText, progressText, stateText, startButton, leftButton, rightButton);
            return hudController;
        }

        private static EndScreenController CreateEndScreen()
        {
            Canvas canvas = CreateCanvas("End Screen Canvas");
            Font font = GetUiFont();

            GameObject panel = new("End Panel");
            panel.transform.SetParent(canvas.transform, false);

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.72f);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.25f, 0.3f);
            panelRect.anchorMax = new Vector2(0.75f, 0.7f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Text resultText = CreateText(panel.transform, "Result Text", "Result", font, new Vector2(0f, -38f), TextAnchor.UpperCenter);
            Text rewardText = CreateText(panel.transform, "Reward Text", string.Empty, font, new Vector2(0f, -78f), TextAnchor.UpperCenter);

            // Hero unlocks and XP can add extra lines, so the reward label gets a taller text box.
            rewardText.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 90f);
            Button restartButton = CreateButton(panel.transform, "Restart Button", "RESTART", font, new Vector2(-100f, -162f), new Vector2(0.5f, 1f));
            Button baseButton = CreateButton(panel.transform, "Base Button", "BASE", font, new Vector2(100f, -162f), new Vector2(0.5f, 1f));

            EndScreenController endScreenController = canvas.gameObject.AddComponent<EndScreenController>();
            endScreenController.Configure(panel, resultText, restartButton, baseButton, rewardText);
            panel.SetActive(false);
            return endScreenController;
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject canvasObject = new(name);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static Text CreateText(Transform parent, string name, string text, Font font, Vector2 anchoredPosition, TextAnchor anchor)
        {
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent, false);

            Text label = textObject.AddComponent<Text>();
            label.text = text;
            label.font = font;
            label.fontSize = 24;
            label.color = Color.white;
            label.alignment = anchor;

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = AnchorFromTextAnchor(anchor);
            rectTransform.anchorMax = AnchorFromTextAnchor(anchor);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(320f, 40f);
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string text, Font font, Vector2 anchoredPosition)
        {
            return CreateButton(parent, name, text, font, anchoredPosition, new Vector2(0.5f, 1f));
        }

        private static Button CreateButton(Transform parent, string name, string text, Font font, Vector2 anchoredPosition, Vector2 anchor)
        {
            GameObject buttonObject = new(name);
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.95f, 0.78f, 0.22f);

            Button button = buttonObject.AddComponent<Button>();

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchor;
            buttonRect.anchorMax = anchor;
            buttonRect.anchoredPosition = anchoredPosition;
            buttonRect.sizeDelta = new Vector2(180f, 44f);

            Text label = CreateText(buttonObject.transform, "Label", text, font, Vector2.zero, TextAnchor.MiddleCenter);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.color = Color.black;
            return button;
        }

        private static Vector2 AnchorFromTextAnchor(TextAnchor anchor)
        {
            return anchor switch
            {
                TextAnchor.UpperLeft => new Vector2(0f, 1f),
                TextAnchor.UpperCenter => new Vector2(0.5f, 1f),
                TextAnchor.MiddleCenter => new Vector2(0.5f, 0.5f),
                _ => new Vector2(0.5f, 0.5f)
            };
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static Material CreateMaterial(Color color)
        {
            return PrototypeMaterialFactory.Create(color);
        }

        private static Font GetUiFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
