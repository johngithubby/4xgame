using System;
using LaneSurvivor.Progression;
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

        private string statusMessage = string.Empty;

        private void Awake()
        {
            // Load local progress before building UI so the first frame reflects persisted state.
            saveData = SaveGameManager.Load();

            // Complete any timer that finished while the app was closed.
            if (PlayerProgression.CompleteReadyHqUpgrade(saveData, DateTime.UtcNow))
            {
                SaveGameManager.Save(saveData);
                statusMessage = "HQ upgrade complete";
            }

            Material groundMaterial = CreateMaterial(new Color(0.18f, 0.25f, 0.24f));
            Material hqMaterial = CreateMaterial(new Color(0.18f, 0.45f, 0.85f));

            CreateCamera();
            CreateLight();
            CreateEventSystem();
            CreateGround(groundMaterial);
            hqBuilding = CreateHqBuilding(hqMaterial);
            hudController = CreateHud();
            hudController.Initialize(CollectCoins, StartHqUpgrade, LaunchMinigame, ResetSave);

            RefreshScene();
        }

        private void Update()
        {
            // Poll timer completion locally; no server authority exists in Phase 2.
            if (PlayerProgression.CompleteReadyHqUpgrade(saveData, DateTime.UtcNow))
            {
                SaveGameManager.Save(saveData);
                statusMessage = "HQ upgrade complete";
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

        private void LaunchMinigame()
        {
            // Save before leaving the base scene so the minigame sees the latest HQ bonus.
            SaveGameManager.Save(saveData);
            SceneManager.LoadScene("Minigame");
        }

        private void RefreshScene()
        {
            // Keep world and HUD state synchronized from one saved data object.
            int remainingSeconds = PlayerProgression.GetHqUpgradeRemainingSeconds(saveData, DateTime.UtcNow);
            hqBuilding.ApplySaveData(saveData);
            hudController.UpdateView(saveData, remainingSeconds, statusMessage);
        }

        private static void CreateCamera()
        {
            // Use a fixed camera for the tiny base prototype.
            GameObject cameraObject = new("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 52f;
            camera.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 7f, -8f);
            cameraObject.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
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
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static void CreateGround(Material groundMaterial)
        {
            // A single flat cube is enough to establish the base area.
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Base Ground";
            ground.transform.position = new Vector3(0f, -0.1f, 0f);
            ground.transform.localScale = new Vector3(8f, 0.12f, 8f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
        }

        private static HQBuilding CreateHqBuilding(Material hqMaterial)
        {
            // The HQ is represented by a simple cube for the first base-building slice.
            GameObject hqObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hqObject.name = "HQ Building";
            hqObject.transform.position = new Vector3(0f, 1f, 0f);
            hqObject.transform.localScale = new Vector3(2f, 2f, 2f);
            hqObject.GetComponent<Renderer>().sharedMaterial = hqMaterial;

            // A world-space TextMesh labels the placeholder building without needing UI layout.
            GameObject labelObject = new("HQ Label");
            labelObject.transform.SetParent(hqObject.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.9f, -0.55f);
            labelObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.22f;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 1f;
            label.color = Color.white;

            HQBuilding hqBuilding = hqObject.AddComponent<HQBuilding>();
            hqBuilding.Configure(label);
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
            Text statusText = CreateText(canvas.transform, "Status Text", "Next HQ upgrade", font, new Vector2(0f, 92f), TextAnchor.LowerCenter, new Vector2(360f, 34f));
            Text playHintText = CreateText(canvas.transform, "Play Hint Text", $"Win reward: +{PlayerProgression.MinigameWinCoins} coins", font, new Vector2(0f, 128f), TextAnchor.LowerCenter, new Vector2(360f, 34f));
            Button collectButton = CreateButton(canvas.transform, "Collect Button", "COLLECT", font, new Vector2(-126f, 34f), new Vector2(0.5f, 0f), new Vector2(114f, 46f));
            Button upgradeButton = CreateButton(canvas.transform, "Upgrade Button", "UPGRADE", font, new Vector2(0f, 34f), new Vector2(0.5f, 0f), new Vector2(114f, 46f));
            Button playButton = CreateButton(canvas.transform, "Play Button", "PLAY", font, new Vector2(126f, 34f), new Vector2(0.5f, 0f), new Vector2(114f, 46f));
            Button resetButton = CreateButton(canvas.transform, "Reset Save Button", "RESET", font, new Vector2(-58f, -18f), new Vector2(1f, 1f), new Vector2(72f, 34f));

            BaseHudController hud = canvas.gameObject.AddComponent<BaseHudController>();
            hud.Configure(titleText, coinsText, hqText, timerText, statusText, playHintText, collectButton, upgradeButton, playButton, resetButton);
            return hud;
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
            rectTransform.anchorMin = AnchorFromTextAnchor(anchor);
            rectTransform.anchorMax = AnchorFromTextAnchor(anchor);
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
            // Prefer URP Lit when available, falling back to the built-in Standard shader.
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader);
            material.color = color;
            return material;
        }

        private static Font GetUiFont()
        {
            // Unity 6 provides LegacyRuntime.ttf; Arial is a fallback for older editor layouts.
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
