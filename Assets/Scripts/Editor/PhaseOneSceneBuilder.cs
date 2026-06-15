using LaneSurvivor.Data;
using LaneSurvivor.Gameplay;
using LaneSurvivor.Rendering;
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
            Material playerMaterial = CreateMaterial("Assets/Materials/PlayerSquad.mat", new Color(0.12f, 0.88f, 0.98f));
            Material playerBeaconMaterial = CreateMaterial("Assets/Materials/PlayerSquadBeacon.mat", new Color(1f, 0.1f, 0.8f));

            LevelDefinition levelDefinition = CreateLevelDefinition();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Minigame";

            PlayerSquad playerSquad = CreatePlayerSquad(playerMaterial, playerBeaconMaterial);
            AutoShooter autoShooter = playerSquad.gameObject.AddComponent<AutoShooter>();
            SquadLaneInput laneInput = playerSquad.gameObject.AddComponent<SquadLaneInput>();

            Camera camera = CreateCamera(playerSquad.transform);
            CreateLight();

            MinigameHudController hudController = CreateHud(playerSquad.transform, camera);
            EndScreenController endScreenController = CreateEndScreen();
            laneInput.Configure(playerSquad, hudController.LeftLaneButton, hudController.RightLaneButton);
            CreateEventSystem();

            LevelManager levelManager = new GameObject("Level Manager").AddComponent<LevelManager>();
            levelManager.Configure(levelDefinition, playerSquad, autoShooter, hudController, endScreenController, trackMaterial, gateMaterial, zombieMaterial);

            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
            camera.tag = "MainCamera";

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Base.unity", true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
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
            levelDefinition.finishDistance = 42.5f;
            levelDefinition.squadMoveSpeed = 4.2f;
            levelDefinition.laneChangeSpeed = 8f;
            levelDefinition.laneMatchTolerance = 0.85f;
            levelDefinition.lanePositions = new[] { -2f, 0f, 2f };
            levelDefinition.shootRange = 8f;
            levelDefinition.shotInterval = 0.35f;
            levelDefinition.gates = new[]
            {
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddSquad,
                    squadValue = 4,
                    damageValue = 0f,
                    position = new Vector3(-2f, 1.1f, 9f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.MultiplySquad,
                    squadValue = 2,
                    damageValue = 0f,
                    position = new Vector3(2f, 1.1f, 9f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddDamage,
                    squadValue = 0,
                    damageValue = 0.5f,
                    position = new Vector3(0f, 1.1f, 22f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.SubtractSquad,
                    squadValue = 5,
                    damageValue = 0f,
                    position = new Vector3(-2f, 1.1f, 36f)
                }
            };
            levelDefinition.zombies = new[]
            {
                new ZombieSpawnDefinition
                {
                    health = 8f,
                    breachPenalty = 2,
                    position = new Vector3(0f, 1f, 15f)
                },
                new ZombieSpawnDefinition
                {
                    health = 18f,
                    breachPenalty = 4,
                    position = new Vector3(2f, 1f, 27f)
                },
                new ZombieSpawnDefinition
                {
                    health = 24f,
                    breachPenalty = 6,
                    position = new Vector3(-2f, 1f, 41f)
                }
            };

            EditorUtility.SetDirty(levelDefinition);
            return levelDefinition;
        }

        private static PlayerSquad CreatePlayerSquad(Material playerMaterial, Material beaconMaterial)
        {
            // Match runtime-generated humanoid squad geometry so editor rebuilds reproduce the live minigame.
            GameObject playerObject = PrototypeCharacterFactory.CreatePlayerSquad("Player Squad", new Vector3(0f, GameplayVisuals.PlayerCenterY, 0f), playerMaterial, beaconMaterial);

            // The generated scene mirrors runtime visibility without requiring a HUD-only player marker.
            SetWorldPlayerRendererVisibility(playerObject, GameplayVisuals.WorldPlayerMeshRenderersEnabled);

            return playerObject.AddComponent<PlayerSquad>();
        }

        private static void SetWorldPlayerRendererVisibility(GameObject playerRoot, bool isVisible)
        {
            // Keeping this helper local makes editor scene output match runtime bootstrap visibility.
            foreach (Renderer renderer in playerRoot.GetComponentsInChildren<Renderer>(true))
            {
                // Legacy generated scenes may still contain the removed sideways soldier cutout child.
                bool isLegacySoldierCutout = renderer.transform.name == PrototypeCharacterFactory.SoldierReferenceVisualName;

                // Fresh editor rebuilds should show the generated humanoid meshes, not the old cutout plane.
                renderer.enabled = isVisible && !isLegacySoldierCutout;
            }
        }

        private static Camera CreateCamera(Transform target)
        {
            GameObject cameraObject = new("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            // Match the runtime bootstrap so editor-generated scenes reproduce simulator framing.
            camera.orthographic = false;
            camera.fieldOfView = GameplayVisuals.CameraFieldOfView;
            camera.nearClipPlane = 0.05f;
            camera.clearFlags = CameraClearFlags.Skybox;

            SimpleCameraFollow follow = cameraObject.AddComponent<SimpleCameraFollow>();
            // The generated scene uses the same centered chase framing as the runtime bootstrap.
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
            // Keep generated-scene state text out from under iPhone Dynamic Island captures.
            Text stateText = CreateText(canvas.transform, "State Text", "Ready", font, new Vector2(-20f, -20f), TextAnchor.UpperRight);
            Button startButton = CreateButton(canvas.transform, "Start Button", "START", font, new Vector2(0f, -95f));
            Button leftButton = CreateButton(canvas.transform, "Left Lane Button", "<", font, new Vector2(-120f, 60f), new Vector2(0.5f, 0f));
            Button rightButton = CreateButton(canvas.transform, "Right Lane Button", ">", font, new Vector2(120f, 60f), new Vector2(0.5f, 0f));

            if (GameplayVisuals.UseScreenSpacePlayerMarker)
            {
                // The overlay marker follows the real gameplay transform without relying on world depth.
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

            // Mission unlocks, hero rewards, and XP can add several lines, so the reward label gets more height.
            rewardText.GetComponent<RectTransform>().sizeDelta = new Vector2(340f, 112f);
            Button restartButton = CreateButton(panel.transform, "Restart Button", "RESTART", font, new Vector2(-100f, -184f), new Vector2(0.5f, 1f));
            Button baseButton = CreateButton(panel.transform, "Base Button", "BASE", font, new Vector2(100f, -184f), new Vector2(0.5f, 1f));

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
            Vector2 anchorPoint = AnchorFromTextAnchor(anchor);
            rectTransform.anchorMin = anchorPoint;
            rectTransform.anchorMax = anchorPoint;
            // Match runtime HUD behavior so regenerated scenes keep edge labels inside the Game view.
            rectTransform.pivot = anchorPoint;
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
                TextAnchor.UpperRight => new Vector2(1f, 1f),
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
