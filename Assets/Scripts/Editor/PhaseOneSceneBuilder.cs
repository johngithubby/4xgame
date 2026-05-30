using LaneSurvivor.Data;
using LaneSurvivor.Gameplay;
using LaneSurvivor.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LaneSurvivor.EditorTools
{
    public static class PhaseOneSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Minigame.unity";

        private const string LevelPath = "Assets/Data/Phase1Level.asset";

        [MenuItem("Lane Survivor/Rebuild Phase 1 Scene")]
        public static void RebuildPhaseOneScene()
        {
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "Data");
            EnsureFolder("Assets", "Materials");

            Material trackMaterial = CreateMaterial("Assets/Materials/Track.mat", new Color(0.20f, 0.24f, 0.22f));
            Material gateMaterial = CreateMaterial("Assets/Materials/Gate.mat", new Color(0.10f, 0.45f, 0.95f));
            Material zombieMaterial = CreateMaterial("Assets/Materials/Zombie.mat", new Color(0.18f, 0.55f, 0.18f));
            Material playerMaterial = CreateMaterial("Assets/Materials/PlayerSquad.mat", new Color(0.95f, 0.80f, 0.20f));

            LevelDefinition levelDefinition = CreateLevelDefinition();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Minigame";

            PlayerSquad playerSquad = CreatePlayerSquad(playerMaterial);
            AutoShooter autoShooter = playerSquad.gameObject.AddComponent<AutoShooter>();

            Camera camera = CreateCamera(playerSquad.transform);
            CreateLight();

            MinigameHudController hudController = CreateHud();
            EndScreenController endScreenController = CreateEndScreen();
            CreateEventSystem();

            LevelManager levelManager = new GameObject("Level Manager").AddComponent<LevelManager>();
            levelManager.Configure(levelDefinition, playerSquad, autoShooter, hudController, endScreenController, trackMaterial, gateMaterial, zombieMaterial);

            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
            camera.tag = "MainCamera";

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        private static LevelDefinition CreateLevelDefinition()
        {
            LevelDefinition levelDefinition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath);
            if (levelDefinition == null)
            {
                levelDefinition = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(levelDefinition, LevelPath);
            }

            levelDefinition.startingSquadCount = 6;
            levelDefinition.startingDamagePerMember = 1f;
            levelDefinition.finishDistance = 48f;
            levelDefinition.squadMoveSpeed = 4.2f;
            levelDefinition.shootRange = 8f;
            levelDefinition.shotInterval = 0.35f;
            levelDefinition.gates = new[]
            {
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddSquad,
                    squadValue = 4,
                    damageValue = 0f,
                    position = new Vector3(0f, 1.1f, 9f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.MultiplySquad,
                    squadValue = 2,
                    damageValue = 0f,
                    position = new Vector3(0f, 1.1f, 18f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddDamage,
                    squadValue = 0,
                    damageValue = 0.5f,
                    position = new Vector3(0f, 1.1f, 29f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.SubtractSquad,
                    squadValue = 5,
                    damageValue = 0f,
                    position = new Vector3(0f, 1.1f, 38f)
                }
            };
            levelDefinition.zombies = new[]
            {
                new ZombieSpawnDefinition
                {
                    health = 8f,
                    breachPenalty = 2,
                    position = new Vector3(0f, 1f, 14f)
                },
                new ZombieSpawnDefinition
                {
                    health = 18f,
                    breachPenalty = 4,
                    position = new Vector3(-1.5f, 1f, 25f)
                },
                new ZombieSpawnDefinition
                {
                    health = 24f,
                    breachPenalty = 6,
                    position = new Vector3(1.5f, 1f, 34f)
                }
            };

            EditorUtility.SetDirty(levelDefinition);
            return levelDefinition;
        }

        private static PlayerSquad CreatePlayerSquad(Material playerMaterial)
        {
            GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = "Player Squad";
            playerObject.transform.position = new Vector3(0f, 1f, 0f);
            playerObject.transform.localScale = new Vector3(1.2f, 1f, 1.2f);

            Renderer playerRenderer = playerObject.GetComponent<Renderer>();
            playerRenderer.sharedMaterial = playerMaterial;

            return playerObject.AddComponent<PlayerSquad>();
        }

        private static Camera CreateCamera(Transform target)
        {
            GameObject cameraObject = new("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.clearFlags = CameraClearFlags.Skybox;

            SimpleCameraFollow follow = cameraObject.AddComponent<SimpleCameraFollow>();
            follow.Initialize(target, new Vector3(0f, 8f, -10f));

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

        private static MinigameHudController CreateHud()
        {
            Canvas canvas = CreateCanvas("HUD Canvas");
            Font font = GetUiFont();

            Text squadText = CreateText(canvas.transform, "Squad Text", "Squad: 0", font, new Vector2(20f, -20f), TextAnchor.UpperLeft);
            Text progressText = CreateText(canvas.transform, "Progress Text", "Progress: 0%", font, new Vector2(20f, -55f), TextAnchor.UpperLeft);
            Text stateText = CreateText(canvas.transform, "State Text", "Ready", font, new Vector2(0f, -20f), TextAnchor.UpperCenter);
            Button startButton = CreateButton(canvas.transform, "Start Button", "START", font, new Vector2(0f, -95f));

            MinigameHudController hudController = canvas.gameObject.AddComponent<MinigameHudController>();
            SetPrivateField(hudController, "squadCountText", squadText);
            SetPrivateField(hudController, "progressText", progressText);
            SetPrivateField(hudController, "stateText", stateText);
            SetPrivateField(hudController, "startButton", startButton);

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

            Text resultText = CreateText(panel.transform, "Result Text", "Result", font, new Vector2(0f, -45f), TextAnchor.UpperCenter);
            Button restartButton = CreateButton(panel.transform, "Restart Button", "RESTART", font, new Vector2(0f, -130f));

            EndScreenController endScreenController = canvas.gameObject.AddComponent<EndScreenController>();
            SetPrivateField(endScreenController, "panel", panel);
            SetPrivateField(endScreenController, "resultText", resultText);
            SetPrivateField(endScreenController, "restartButton", restartButton);
            panel.SetActive(false);

            return endScreenController;
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject canvasObject = new(name);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
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
            GameObject buttonObject = new(name);
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.95f, 0.78f, 0.22f);

            Button button = buttonObject.AddComponent<Button>();

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 1f);
            buttonRect.anchorMax = new Vector2(0.5f, 1f);
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

        private static Material CreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Font GetUiFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void SetPrivateField(Object target, string fieldName, Object value)
        {
            SerializedObject serializedObject = new(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
