using LaneSurvivor.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LaneSurvivor.Heroes
{
    public sealed class HeroSceneBootstrap : MonoBehaviour
    {
        private SaveGameData saveData;

        private HeroHudController hudController;

        private string statusMessage = string.Empty;

        private void Awake()
        {
            // Load local save state before creating UI so the first frame is accurate.
            saveData = SaveGameManager.Load();

            // Existing HQ milestone saves should still receive deterministic hero grants here.
            if (HeroRewardSystem.TryGrantHqLevelTwoHero(saveData) != null)
            {
                statusMessage = "Dax Medic joined";
                SaveGameManager.Save(saveData);
            }

            CreateCamera();
            CreateLight();
            CreateEventSystem();
            CreateBackdrop(CreateMaterial(new Color(0.12f, 0.18f, 0.24f)));
            hudController = CreateHud();
            hudController.Initialize(EquipNextHero, LevelUpEquippedHero, ReturnToBase);

            RefreshView();
        }

        private void EquipNextHero()
        {
            // Cycling keeps the first dedicated screen simple while still allowing selection.
            HeroDefinition hero = HeroInventory.GetNextOwnedHeroToEquip(saveData);
            if (hero == null)
            {
                statusMessage = "No heroes owned";
                RefreshView();
                return;
            }

            // Persist equipment immediately so the next minigame run sees the selected hero.
            HeroInventory.EquipHero(saveData, hero.id);
            SaveGameManager.Save(saveData);
            statusMessage = $"{hero.displayName} equipped";
            RefreshView();
        }

        private void LevelUpEquippedHero()
        {
            // The progression helper owns cost, max-level, and ownership validation.
            HeroLevelUpResult result = HeroProgression.TryLevelUpEquippedHeroWithCoins(saveData);
            if (result.success)
            {
                SaveGameManager.Save(saveData);
            }

            // Show either the success message or the short failure reason.
            statusMessage = result.message;
            RefreshView();
        }

        private void ReturnToBase()
        {
            // Save before leaving in case normalization repaired old hero data.
            SaveGameManager.Save(saveData);
            SceneManager.LoadScene("Base");
        }

        private void RefreshView()
        {
            // Keep the HUD in lockstep with the current mutable save object.
            hudController.UpdateView(saveData, statusMessage);
        }

        private static void CreateCamera()
        {
            // The Hero scene is UI-first but still uses a camera for the placeholder backdrop.
            GameObject cameraObject = new("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 52f;
            camera.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 4f, -6f);
            cameraObject.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        }

        private static void CreateLight()
        {
            // A single directional light keeps the background primitive readable.
            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -25f, 0f);
        }

        private static void CreateEventSystem()
        {
            // UI buttons require one EventSystem in the active scene.
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static void CreateBackdrop(Material material)
        {
            // A simple slab gives the dedicated Hero screen a distinct placeholder space.
            GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backdrop.name = "Hero Screen Backdrop";
            backdrop.transform.position = new Vector3(0f, -0.1f, 0f);
            backdrop.transform.localScale = new Vector3(8f, 0.12f, 8f);
            backdrop.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static HeroHudController CreateHud()
        {
            // Build the dedicated screen at runtime so the checked-in scene remains tiny.
            Canvas canvas = CreateCanvas("Hero HUD Canvas");
            Font font = GetUiFont();

            Text titleText = CreateText(canvas.transform, "Title Text", "Heroes", font, new Vector2(0f, -20f), TextAnchor.UpperCenter, new Vector2(260f, 42f));
            Text coinsText = CreateText(canvas.transform, "Coins Text", "Coins: 0", font, new Vector2(16f, -64f), TextAnchor.UpperLeft, new Vector2(220f, 34f));
            Text equippedText = CreateText(canvas.transform, "Equipped Text", "Equipped: None", font, new Vector2(16f, -100f), TextAnchor.UpperLeft, new Vector2(360f, 34f));
            Text heroListText = CreateText(canvas.transform, "Hero List Text", "No heroes owned yet", font, new Vector2(0f, -148f), TextAnchor.UpperCenter, new Vector2(360f, 430f));
            Text statusText = CreateText(canvas.transform, "Status Text", "Earn heroes through gameplay", font, new Vector2(0f, 116f), TextAnchor.LowerCenter, new Vector2(360f, 40f));
            Button equipButton = CreateButton(canvas.transform, "Equip Next Button", "EQUIP", font, new Vector2(-112f, 52f), new Vector2(0.5f, 0f), new Vector2(104f, 46f));
            Button levelButton = CreateButton(canvas.transform, "Level Up Button", "LEVEL", font, new Vector2(0f, 52f), new Vector2(0.5f, 0f), new Vector2(104f, 46f));
            Button backButton = CreateButton(canvas.transform, "Back Button", "BACK", font, new Vector2(112f, 52f), new Vector2(0.5f, 0f), new Vector2(104f, 46f));

            HeroHudController hud = canvas.gameObject.AddComponent<HeroHudController>();
            hud.Configure(titleText, coinsText, equippedText, heroListText, statusText, equipButton, levelButton, backButton);
            return hud;
        }

        private static Canvas CreateCanvas(string name)
        {
            // Screen Space Overlay keeps this menu independent of camera framing.
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

        private static Text CreateText(Transform parent, string name, string text, Font font, Vector2 anchoredPosition, TextAnchor anchor, Vector2 size)
        {
            // UnityEngine.UI.Text keeps this prototype dependency-light.
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

        private static Button CreateButton(Transform parent, string name, string text, Font font, Vector2 anchoredPosition, Vector2 anchor, Vector2 size)
        {
            // Plain colored buttons are enough for the placeholder hero management slice.
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

            Text label = CreateText(buttonObject.transform, "Label", text, font, Vector2.zero, TextAnchor.MiddleCenter, size);
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
