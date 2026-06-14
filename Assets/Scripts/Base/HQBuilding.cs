using System;
using System.Collections.Generic;
using LaneSurvivor.Progression;
using LaneSurvivor.Save;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LaneSurvivor.Base
{
    public sealed class HQBuilding : MonoBehaviour
    {
        private const float BaseVisualDiameter = 2f;

        private const float BaseVisualHeight = 1.8f;

        private const float HeightPerLevel = 0.02f;

        private const float ReferenceModelWidth = 2.95f;

        private const float ReferenceModelHeight = 2.36f;

        private const float ReferenceModelHeightPerLevel = 0.025f;

        private const float ReferenceGlowPadding = 0.08f;

        private const float ReferenceModelZ = -0.58f;

        private const float ReferenceGlowZ = -0.53f;

        private const int ProgressSegments = 36;

        private const float CompletionGlowDurationSeconds = 5f;

        private const float CompletionPopDurationSeconds = 0.34f;

        private const float CompletionGlowPulseFrequency = 1.75f;

        private const float CompletionGlowMinimumScale = 1f;

        private const float CompletionGlowMaximumScale = 1.08f;

        private const float ClickMoveTolerancePixels = 22f;

        private static readonly Vector2 ReferenceClickPaddingPixels = new(8f, 8f);

        private static readonly Vector2 FallbackClickSizePixels = new(220f, 180f);

        private static readonly Vector2 UpgradeSymbolClickSizePixels = new(92f, 92f);

        private static readonly Color AffordableUpgradeSymbolColor = new(0.12f, 0.92f, 0.34f);

        private static readonly Color UnaffordableUpgradeSymbolColor = new(0.44f, 0.46f, 0.48f);

        [SerializeField]
        private Transform visualRoot;

        [SerializeField]
        private TextMesh levelLabel;

        [SerializeField]
        private Transform bodyTransform;

        [SerializeField]
        private Renderer bodyRenderer;

        [SerializeField]
        private Transform detailRoot;

        [SerializeField]
        private Transform referenceModelTransform;

        [SerializeField]
        private Renderer referenceModelRenderer;

        [SerializeField]
        private Transform upgradeSymbolRoot;

        [SerializeField]
        private Renderer[] upgradeSymbolRenderers = Array.Empty<Renderer>();

        [SerializeField]
        private Transform progressRoot;

        [SerializeField]
        private MeshFilter progressFillMeshFilter;

        [SerializeField]
        private Transform glowRoot;

        [SerializeField]
        private Transform glowReferenceAuraTransform;

        [SerializeField]
        private Renderer[] glowRenderers = Array.Empty<Renderer>();

        [SerializeField]
        private Component audioSource;

        private Func<bool> requestUpgradeAction;

        private int appliedVisualLevel = -1;

        private bool isUpgradeRunning;

        private bool mousePressStartedAwayFromUi;

        private bool touchPressStartedAwayFromUi;

        private int touchFingerId = -1;

        private Vector2 mousePressPosition;

        private Vector2 touchPressPosition;

        private float glowRemainingSeconds;

        private float popRemainingSeconds;

        private Color glowBaseColor = new(0.20f, 1f, 0.72f, 0.40f);

        public int Level { get; private set; } = 1;

        public bool CanAffordDisplayedUpgrade { get; private set; }

        public float ProgressFillAmount { get; private set; }

        public int CompletionEffectPlayCount { get; private set; }

        public int CompletionSoundRequestCount { get; private set; }

        public float CompletionGlowPulseScale { get; private set; } = 1f;

        public float CompletionGlowAlpha { get; private set; } = 0.40f;

        public bool HasCompletionSoundSource => audioSource != null;

        public bool HasGeneratedCompletionSoundClip => UpgradeCompletionSound.HasGeneratedCompletionSoundClip;

        public bool IsCompletionGlowVisible => glowRoot != null && glowRoot.gameObject.activeSelf;

        public bool IsUpgradeSymbolVisible => upgradeSymbolRoot != null && upgradeSymbolRoot.gameObject.activeSelf;

        public bool IsProgressVisible => progressRoot != null && progressRoot.gameObject.activeSelf;

        public bool IsPopAnimating => popRemainingSeconds > 0f;

        public static float CalculateVisualDiameter(int level)
        {
            // HQ upgrades no longer widen the footprint; the reference model should only grow upward.
            return BaseVisualDiameter;
        }

        public static float CalculateVisualHeight(int level)
        {
            // A small per-upgrade lift gives progression feedback without making double-digit HQs sprawl.
            int safeLevel = Mathf.Max(1, level);
            return BaseVisualHeight + (safeLevel - 1) * HeightPerLevel;
        }

        public static float CalculateReferenceModelWidth(int level)
        {
            // Width remains fixed so the new HQ art keeps a stable base footprint after upgrades.
            return ReferenceModelWidth;
        }

        public static float CalculateReferenceModelHeight(int level)
        {
            // The visible card stretches only a few pixels taller per saved HQ level.
            int safeLevel = Mathf.Max(1, level);
            return ReferenceModelHeight + (safeLevel - 1) * ReferenceModelHeightPerLevel;
        }

        public static Color CalculateLevelColor(int level)
        {
            // The reference texture owns the visible color, so fallback geometry keeps a neutral baseline.
            return Color.white;
        }

        public static int CalculateDetailRows(int level)
        {
            // Upgrades now use height-only progression instead of adding side-detail complications.
            return 0;
        }

        public void Configure(
            Transform visualContainer,
            TextMesh label,
            Transform body = null,
            Renderer bodyVisual = null,
            Transform detailContainer = null,
            Transform referenceModel = null,
            Transform symbolRoot = null,
            Renderer[] symbolRenderers = null,
            Transform progressContainer = null,
            MeshFilter progressFill = null,
            Transform glowContainer = null,
            Component soundSource = null,
            Func<bool> startUpgradeAction = null)
        {
            // Keep every generated reference optional so older tests can still instantiate a label-only HQ.
            visualRoot = visualContainer;
            levelLabel = label;
            bodyTransform = body;
            bodyRenderer = bodyVisual;
            detailRoot = detailContainer;
            referenceModelTransform = referenceModel;
            referenceModelRenderer = referenceModelTransform != null ? referenceModelTransform.GetComponent<Renderer>() : null;
            upgradeSymbolRoot = symbolRoot;
            upgradeSymbolRenderers = symbolRenderers ?? Array.Empty<Renderer>();
            progressRoot = progressContainer;
            progressFillMeshFilter = progressFill;
            glowRoot = glowContainer;
            glowReferenceAuraTransform = glowRoot != null ? glowRoot.Find("HQ Completion Reference Aura") : null;
            glowRenderers = glowRoot != null ? glowRoot.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
            glowBaseColor = glowRenderers.Length > 0 && glowRenderers[0] != null ? GetMaterialColor(glowRenderers[0].sharedMaterial) : glowBaseColor;
            CompletionGlowAlpha = glowBaseColor.a;
            audioSource = soundSource;
            requestUpgradeAction = startUpgradeAction;

            // The upgrade symbol should appear only after the player taps the HQ body.
            upgradeSymbolRoot?.gameObject.SetActive(false);

            // The progress icon is visible only while a saved HQ upgrade timer is active.
            progressRoot?.gameObject.SetActive(false);

            // The reference image already contains the HQ sign, so the generated text stays hidden when art loads.
            if (levelLabel != null && referenceModelTransform != null)
            {
                levelLabel.gameObject.SetActive(false);
                levelLabel.text = string.Empty;
            }

            // Completion glow starts hidden and is activated by PlayCompletionEffects.
            glowRoot?.gameObject.SetActive(false);

            // A dynamic mesh lets the circular progress wedge update without replacing the component.
            if (progressFillMeshFilter != null && progressFillMeshFilter.sharedMesh == null)
            {
                progressFillMeshFilter.sharedMesh = new Mesh
                {
                    name = "HQ Progress Fill Mesh",
                    hideFlags = HideFlags.HideAndDontSave
                };
                progressFillMeshFilter.sharedMesh.MarkDynamic();
            }
        }

        public void ApplySaveData(SaveGameData saveData, DateTime utcNow)
        {
            // Mirror the saved HQ level onto the scene component for display and future scene interactions.
            Level = Mathf.Max(1, saveData?.hqLevel ?? 1);
            isUpgradeRunning = saveData != null && saveData.hqUpgradeInProgress;
            int coins = Mathf.Max(0, saveData?.coins ?? 0);
            int upgradeCost = PlayerProgression.GetHqUpgradeCost(Level);
            CanAffordDisplayedUpgrade = coins >= upgradeCost;
            RefreshLabel();

            // Rebuild level-dependent visuals only when the saved HQ level actually changes.
            if (appliedVisualLevel != Level)
            {
                RefreshVisuals();
                appliedVisualLevel = Level;
            }

            // Timer and wallet state control whether the popup arrow is shown and what color it uses.
            RefreshUpgradeAffordance();
            RefreshProgressIcon(saveData, utcNow);
        }

        public void PlayCompletionEffects()
        {
            // The aura lasts the same five-second window as the bio-lab upgrade feedback.
            glowRemainingSeconds = CompletionGlowDurationSeconds;

            // A small pop reinforces that the upgraded HQ has refreshed into its new height.
            popRemainingSeconds = CompletionPopDurationSeconds;

            // Tests use this counter to prove the finish path triggered without inspecting frames.
            CompletionEffectPlayCount += 1;

            // Show the glow immediately so the completion frame is readable.
            glowRoot?.gameObject.SetActive(true);
            UpdateCompletionGlowPulse();

            // Play the same generated local chime as every other building upgrade completion.
            PlayFinishSound();
        }

        private void PlayFinishSound()
        {
            // Record every finish-sound request even when an audio module is unavailable in a test runner.
            CompletionSoundRequestCount += 1;

            // The shared helper handles optional AudioModule reflection and playback.
            UpgradeCompletionSound.Play(audioSource);
        }

        public void ShowUpgradeSymbol()
        {
            // Upgrading HQs should not offer a second start-upgrade affordance.
            if (isUpgradeRunning || upgradeSymbolRoot == null)
            {
                return;
            }

            // Make the existing symbol visible and recolor it from the latest wallet affordability state.
            upgradeSymbolRoot.gameObject.SetActive(true);
            SetUpgradeSymbolColor(CanAffordDisplayedUpgrade ? AffordableUpgradeSymbolColor : UnaffordableUpgradeSymbolColor);
        }

        public void HideUpgradeSymbol()
        {
            // Centralized hiding keeps outside-click and timer-start behavior visually consistent.
            upgradeSymbolRoot?.gameObject.SetActive(false);
        }

        public bool RequestUpgradeFromVisibleSymbol()
        {
            // Hidden symbols cannot be clicked by normal play and should not start timers from tests either.
            if (!IsUpgradeSymbolVisible)
            {
                return false;
            }

            // The bootstrap owns save mutation and persistence, so the HQ delegates the request upward.
            bool started = requestUpgradeAction?.Invoke() == true;
            if (started)
            {
                HideUpgradeSymbol();
            }

            return started;
        }

        public bool RequestUpgradeFromBuilding()
        {
            // Older tests may still call this helper; keep it guarded by the same visible-symbol rule as play.
            return RequestUpgradeFromVisibleSymbol();
        }

        public bool TryHandleBuildingClick(Vector2 screenPosition)
        {
            // A camera is required to compare a screen tap with the rendered HQ target.
            Camera camera = Camera.main;
            if (camera == null)
            {
                return false;
            }

            // The visible upgrade symbol has priority over the HQ body when both overlap onscreen.
            if (IsUpgradeSymbolVisible && IsScreenPointNearWorldPoint(camera, screenPosition, upgradeSymbolRoot.position, UpgradeSymbolClickSizePixels))
            {
                RequestUpgradeFromVisibleSymbol();
                return true;
            }

            // Ignore taps outside the current visual bounds so map drags and nearby buildings keep their behavior.
            if (!IsScreenPointInsideCurrentVisual(camera, screenPosition))
            {
                HideUpgradeSymbol();
                return false;
            }

            // A tap on the HQ body reveals the start-upgrade arrow without starting the timer directly.
            ShowUpgradeSymbol();
            return true;
        }

        private void Update()
        {
            // World clicks are handled here so the generated HQ can be interacted with without scene YAML.
            HandleMouseClick();
            HandleTouchClick();

            // Completion effects decay in unscaled time so future pauses do not freeze feedback.
            UpdateCompletionEffects();
        }

        private void HandleMouseClick()
        {
            // The initial press decides whether a HUD control should own the gesture.
            if (Input.GetMouseButtonDown(0))
            {
                mousePressStartedAwayFromUi = !IsPointerOverUi();
                mousePressPosition = Input.mousePosition;
                return;
            }

            // Only a short press-and-release should trigger HQ interaction; drags belong to map panning.
            if (!Input.GetMouseButtonUp(0) || !mousePressStartedAwayFromUi)
            {
                return;
            }

            // Large movement means the player was dragging the map rather than tapping the building.
            Vector2 releasePosition = Input.mousePosition;
            if (Vector2.Distance(mousePressPosition, releasePosition) <= ClickMoveTolerancePixels)
            {
                TryHandleBuildingClick(releasePosition);
            }

            mousePressStartedAwayFromUi = false;
        }

        private void HandleTouchClick()
        {
            // The HQ only needs simple one-finger taps; two-finger gestures belong to camera pinch.
            if (Input.touchCount != 1)
            {
                touchPressStartedAwayFromUi = false;
                touchFingerId = -1;
                return;
            }

            // The active touch is tracked by finger id so a canceled gesture cannot leak into another tap.
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                touchPressStartedAwayFromUi = !IsPointerOverUi(touch.fingerId);
                touchPressPosition = touch.position;
                touchFingerId = touch.fingerId;
                return;
            }

            // Ended and canceled touches both finish the local tap attempt.
            if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
            {
                return;
            }

            // Ignore touch endings that began over UI or belong to a different finger.
            if (touchPressStartedAwayFromUi && touchFingerId == touch.fingerId && touch.phase == TouchPhase.Ended)
            {
                if (Vector2.Distance(touchPressPosition, touch.position) <= ClickMoveTolerancePixels)
                {
                    TryHandleBuildingClick(touch.position);
                }
            }

            touchPressStartedAwayFromUi = false;
            touchFingerId = -1;
        }

        private void RefreshLabel()
        {
            // The reference image contains the HQ letters, while fallback scenes still need generated text.
            if (levelLabel == null)
            {
                return;
            }

            levelLabel.text = referenceModelTransform != null ? string.Empty : $"HQ\nLv {Level}";
            levelLabel.gameObject.SetActive(referenceModelTransform == null);
            levelLabel.color = CalculateLabelColor(Level);
        }

        private bool IsScreenPointInsideCurrentVisual(Camera camera, Vector2 screenPosition)
        {
            // The reference quad is the player-visible HQ, so its renderer bounds define the preferred tap target.
            if (referenceModelRenderer != null && referenceModelRenderer.enabled)
            {
                return IsScreenPointInsideReferenceQuad(camera, screenPosition, referenceModelTransform, ReferenceClickPaddingPixels);
            }

            // Fallback procedural scenes use the body renderer bounds when reference art is unavailable.
            if (bodyRenderer != null && IsScreenPointInsideRendererBounds(camera, screenPosition, bodyRenderer, ReferenceClickPaddingPixels))
            {
                return true;
            }

            // Reduced test scenes can still hit a centered rectangle around the logical HQ body.
            return IsScreenPointNearWorldPoint(camera, screenPosition, GetFallbackClickWorldPosition(), FallbackClickSizePixels);
        }

        private static bool IsScreenPointInsideReferenceQuad(Camera camera, Vector2 screenPosition, Transform quadTransform, Vector2 paddingPixels)
        {
            // Missing transforms cannot provide the precise quad corners, so callers should fall back elsewhere.
            if (quadTransform == null)
            {
                return false;
            }

            // The reference mesh is authored as a unit quad in local X/Y space, so these are the exact visual corners.
            Vector3[] worldCorners =
            {
                quadTransform.TransformPoint(new Vector3(-0.5f, -0.5f, 0f)),
                quadTransform.TransformPoint(new Vector3(0.5f, -0.5f, 0f)),
                quadTransform.TransformPoint(new Vector3(0.5f, 0.5f, 0f)),
                quadTransform.TransformPoint(new Vector3(-0.5f, 0.5f, 0f))
            };

            // Project the corners into screen space so the hit target matches camera zoom, pan, and tilt.
            Vector2[] screenCorners = new Vector2[worldCorners.Length];
            Vector2 screenCenter = Vector2.zero;
            for (int cornerIndex = 0; cornerIndex < worldCorners.Length; cornerIndex += 1)
            {
                Vector3 screenCorner = camera.WorldToScreenPoint(worldCorners[cornerIndex]);
                if (screenCorner.z < 0f)
                {
                    return false;
                }

                screenCorners[cornerIndex] = new Vector2(screenCorner.x, screenCorner.y);
                screenCenter += screenCorners[cornerIndex];
            }

            // Padding expands each corner away from the polygon center without using the renderer's inflated AABB.
            screenCenter /= screenCorners.Length;
            for (int cornerIndex = 0; cornerIndex < screenCorners.Length; cornerIndex += 1)
            {
                Vector2 fromCenter = screenCorners[cornerIndex] - screenCenter;
                if (fromCenter.sqrMagnitude <= 0.001f)
                {
                    continue;
                }

                Vector2 padding = new(Mathf.Sign(fromCenter.x) * paddingPixels.x, Mathf.Sign(fromCenter.y) * paddingPixels.y);
                screenCorners[cornerIndex] += padding;
            }

            // A convex quad contains the point only when it stays on the same side of every directed edge.
            bool hasPositive = false;
            bool hasNegative = false;
            for (int cornerIndex = 0; cornerIndex < screenCorners.Length; cornerIndex += 1)
            {
                Vector2 edgeStart = screenCorners[cornerIndex];
                Vector2 edgeEnd = screenCorners[(cornerIndex + 1) % screenCorners.Length];
                float cross = Cross(edgeEnd - edgeStart, screenPosition - edgeStart);
                hasPositive |= cross > 0f;
                hasNegative |= cross < 0f;
                if (hasPositive && hasNegative)
                {
                    return false;
                }
            }

            return true;
        }

        private static float Cross(Vector2 first, Vector2 second)
        {
            // Two-dimensional cross products let the polygon hit test avoid physics colliders.
            return first.x * second.y - first.y * second.x;
        }

        private Vector3 GetFallbackClickWorldPosition()
        {
            // Aim the fallback tap center at the body mass rather than the root's ground position.
            return transform.position + new Vector3(0f, CalculateVisualHeight(Level) * 0.55f, 0f);
        }

        private static bool IsScreenPointInsideRendererBounds(Camera camera, Vector2 screenPosition, Renderer renderer, Vector2 paddingPixels)
        {
            // Null renderers cannot provide world bounds for a screen-space tap check.
            if (renderer == null)
            {
                return false;
            }

            // Project every world-bound corner so camera zoom and pan naturally change the tap rectangle.
            Bounds bounds = renderer.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            bool hasScreenBounds = false;
            float minimumX = 0f;
            float maximumX = 0f;
            float minimumY = 0f;
            float maximumY = 0f;

            for (int xSign = -1; xSign <= 1; xSign += 2)
            {
                for (int ySign = -1; ySign <= 1; ySign += 2)
                {
                    for (int zSign = -1; zSign <= 1; zSign += 2)
                    {
                        // Each sign combination picks one of the renderer bounds' eight corners.
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(xSign, ySign, zSign));
                        Vector3 screenCorner = camera.WorldToScreenPoint(corner);
                        if (screenCorner.z < 0f)
                        {
                            continue;
                        }

                        if (!hasScreenBounds)
                        {
                            minimumX = screenCorner.x;
                            maximumX = screenCorner.x;
                            minimumY = screenCorner.y;
                            maximumY = screenCorner.y;
                            hasScreenBounds = true;
                            continue;
                        }

                        minimumX = Mathf.Min(minimumX, screenCorner.x);
                        maximumX = Mathf.Max(maximumX, screenCorner.x);
                        minimumY = Mathf.Min(minimumY, screenCorner.y);
                        maximumY = Mathf.Max(maximumY, screenCorner.y);
                    }
                }
            }

            if (!hasScreenBounds)
            {
                return false;
            }

            // A small padding makes taps near the anti-aliased building edge feel forgiving.
            minimumX -= paddingPixels.x;
            maximumX += paddingPixels.x;
            minimumY -= paddingPixels.y;
            maximumY += paddingPixels.y;
            return screenPosition.x >= minimumX
                && screenPosition.x <= maximumX
                && screenPosition.y >= minimumY
                && screenPosition.y <= maximumY;
        }

        private static bool IsScreenPointNearWorldPoint(Camera camera, Vector2 screenPosition, Vector3 worldPosition, Vector2 sizePixels)
        {
            // Convert the current world position into screen pixels so panning and zooming stay aligned.
            Vector3 screenPoint = camera.WorldToScreenPoint(worldPosition);

            // Points behind the camera cannot be tapped from the current view.
            if (screenPoint.z < 0f)
            {
                return false;
            }

            // Rectangular screen hit areas are enough for generated placeholder buildings and avoid physics modules.
            float halfWidth = sizePixels.x * 0.5f;
            float halfHeight = sizePixels.y * 0.5f;
            return Mathf.Abs(screenPosition.x - screenPoint.x) <= halfWidth
                && Mathf.Abs(screenPosition.y - screenPoint.y) <= halfHeight;
        }

        private static bool IsPointerOverUi(int pointerId = -1)
        {
            // Without an EventSystem there is no HUD layer to block world taps.
            if (EventSystem.current == null)
            {
                return false;
            }

            // Touch input needs the finger id so Unity checks the correct pointer against UI.
            return pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        private void RefreshVisuals()
        {
            // Convert level into visual dimensions once so all optional pieces stay aligned.
            float visualDiameter = CalculateVisualDiameter(Level);
            float visualHeight = CalculateVisualHeight(Level);

            // The hidden body remains a pentagon scaffold for tests, hit alignment, and fallback visuals.
            if (bodyTransform != null)
            {
                bodyTransform.localScale = new Vector3(visualDiameter, visualHeight, visualDiameter);
                bodyTransform.localPosition = new Vector3(0f, visualHeight * 0.5f, 0f);
            }

            // Fallback geometry stays neutral because the reference card owns the player-visible look.
            if (bodyRenderer != null)
            {
                SetMaterialColor(bodyRenderer.sharedMaterial, CalculateLevelColor(Level));
            }

            // The reference-textured HQ is the visible source of truth.
            PositionReferenceModel();

            // The completion aura uses the same shape and tracks the same height-only growth.
            PositionCompletionGlow();

            // The upgrade symbol floats above the visible HQ, but stays hidden until a body tap reveals it.
            PositionUpgradeSymbol();

            // The progress icon overlays the HQ roof while the saved timer is active.
            PositionProgressIcon();

            // Keep fallback labels aligned even when no reference image is available.
            PositionLabel(visualDiameter, visualHeight);

            // Remove old generated level-detail rows so upgrades no longer add exterior complications.
            ClearDetailRows();
        }

        private void PositionUpgradeSymbol()
        {
            // Missing symbol roots are valid for reduced test setups and older scene objects.
            if (upgradeSymbolRoot == null)
            {
                return;
            }

            // Place the popup just above the current reference-art height without clipping at the default camera top.
            float modelHeight = CalculateReferenceModelHeight(Level);
            upgradeSymbolRoot.localPosition = new Vector3(0f, 0.94f + modelHeight * 0.46f, -0.20f);
            upgradeSymbolRoot.localRotation = Quaternion.identity;
            upgradeSymbolRoot.localScale = Vector3.one;
        }

        private void PositionProgressIcon()
        {
            // Missing progress roots are valid for reduced test setups and older scene objects.
            if (progressRoot == null)
            {
                return;
            }

            // Keep the circular timer centered on the HQ roof and slightly forward of the reference art.
            float modelHeight = CalculateReferenceModelHeight(Level);
            progressRoot.localPosition = new Vector3(0f, 0.72f + modelHeight * 0.43f, -0.04f);
            progressRoot.localRotation = Quaternion.identity;
            progressRoot.localScale = Vector3.one;
        }

        private void PositionReferenceModel()
        {
            // Missing reference art is valid when tests instantiate a reduced procedural HQ.
            if (referenceModelTransform == null)
            {
                return;
            }

            // Level one renders the concept image at its authored ratio; later levels stretch upward slightly.
            PositionReferenceQuad(referenceModelTransform, 0f, 0f, ReferenceModelZ);
        }

        private void PositionCompletionGlow()
        {
            // Missing glow roots are valid for reduced test setups and fallback scenes.
            if (glowRoot == null)
            {
                return;
            }

            // Keep the outer root reserved for pulse animation so sizing and pulsing stay independent.
            glowRoot.localPosition = Vector3.zero;

            // The reference aura reuses the exact HQ cutout silhouette and sits behind the visible model.
            if (glowReferenceAuraTransform != null)
            {
                PositionReferenceQuad(glowReferenceAuraTransform, ReferenceGlowPadding, ReferenceGlowPadding, ReferenceGlowZ);
            }
        }

        private void PositionReferenceQuad(Transform targetTransform, float widthPadding, float heightPadding, float zPosition)
        {
            // Height-only upgrades stretch the image upward and lift its center so the base remains planted.
            float modelHeight = CalculateReferenceModelHeight(Level);
            float extraHeight = Mathf.Max(0f, modelHeight - ReferenceModelHeight);

            // Match the same camera-facing quad strategy used by the bio lab reference model.
            targetTransform.localPosition = new Vector3(0f, 1.02f + extraHeight * 0.5f, zPosition);
            targetTransform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            targetTransform.localScale = new Vector3(ReferenceModelWidth + widthPadding, modelHeight + heightPadding, 1f);
        }

        private void PositionLabel(float visualDiameter, float visualHeight)
        {
            // A missing or hidden label is valid for reference-textured HQ scenes.
            if (levelLabel == null || !levelLabel.gameObject.activeSelf)
            {
                return;
            }

            // The label follows fallback roof height without inheriting body scale.
            levelLabel.transform.localPosition = new Vector3(0f, visualHeight + 0.24f, -visualDiameter * 0.22f);
            levelLabel.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            levelLabel.transform.localScale = Vector3.one * 0.22f;
        }

        private void RefreshUpgradeAffordance()
        {
            // Active upgrades hide the start symbol because the timer is already running.
            if (isUpgradeRunning)
            {
                HideUpgradeSymbol();
                return;
            }

            // A visible symbol should immediately reflect wallet changes after collecting credits.
            if (IsUpgradeSymbolVisible)
            {
                SetUpgradeSymbolColor(CanAffordDisplayedUpgrade ? AffordableUpgradeSymbolColor : UnaffordableUpgradeSymbolColor);
            }
        }

        private void RefreshProgressIcon(SaveGameData saveData, DateTime utcNow)
        {
            // The progress icon should exist only while the local HQ upgrade timer is running.
            if (progressRoot == null || !isUpgradeRunning)
            {
                progressRoot?.gameObject.SetActive(false);
                ProgressFillAmount = 0f;
                RebuildProgressFillMesh(0f);
                return;
            }

            // Show the overlay and fill it from the persisted timer progress.
            progressRoot.gameObject.SetActive(true);
            RebuildProgressFillMesh(PlayerProgression.GetHqUpgradeProgress01(saveData, utcNow));
        }

        private void RebuildProgressFillMesh(float progress)
        {
            // Cache the clamped value for tests and for any future label text.
            ProgressFillAmount = Mathf.Clamp01(progress);

            // Missing mesh filters are valid for tests that only inspect save state.
            Mesh mesh = progressFillMeshFilter != null ? progressFillMeshFilter.sharedMesh : null;
            if (mesh == null)
            {
                return;
            }

            // A zero fill is represented as an empty mesh so the background disc remains visible.
            if (ProgressFillAmount <= 0f)
            {
                mesh.Clear();
                return;
            }

            // The wedge uses one center vertex and a fan around the requested progress angle.
            int segmentCount = Mathf.Clamp(Mathf.CeilToInt(ProgressSegments * ProgressFillAmount), 1, ProgressSegments);
            float coveredRadians = Mathf.PI * 2f * ProgressFillAmount;
            List<Vector3> vertices = new(segmentCount + 2);
            List<Vector3> normals = new(segmentCount + 2);
            List<int> triangles = new(segmentCount * 6);
            vertices.Add(Vector3.zero);
            normals.Add(Vector3.up);

            for (int segment = 0; segment <= segmentCount; segment += 1)
            {
                // Start at the front of the disc and sweep clockwise from the Base camera's perspective.
                float angle = Mathf.PI * 0.5f - coveredRadians * segment / segmentCount;
                vertices.Add(new Vector3(Mathf.Cos(angle) * 0.42f, 0.018f, Mathf.Sin(angle) * 0.42f));
                normals.Add(Vector3.up);
            }

            for (int segment = 1; segment <= segmentCount; segment += 1)
            {
                // Add both windings so the wedge stays visible across shader culling defaults.
                triangles.Add(0);
                triangles.Add(segment);
                triangles.Add(segment + 1);
                triangles.Add(0);
                triangles.Add(segment + 1);
                triangles.Add(segment);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        private void ClearDetailRows()
        {
            // Detail roots are optional in narrow tests and fallback scenes.
            if (detailRoot == null)
            {
                return;
            }

            for (int childIndex = detailRoot.childCount - 1; childIndex >= 0; childIndex -= 1)
            {
                // Disable first so old rows disappear immediately even when Destroy waits until frame end.
                GameObject childObject = detailRoot.GetChild(childIndex).gameObject;
                childObject.SetActive(false);

                if (Application.isPlaying)
                {
                    // Play Mode uses delayed destruction to stay inside Unity's object lifecycle rules.
                    Destroy(childObject);
                }
                else
                {
                    // Edit Mode helper use needs immediate cleanup because no frame loop may follow.
                    DestroyImmediate(childObject);
                }
            }
        }

        private void UpdateCompletionEffects()
        {
            // Glow visibility counts down independently from the pop scale bounce.
            if (glowRemainingSeconds > 0f)
            {
                glowRemainingSeconds = Mathf.Max(0f, glowRemainingSeconds - Time.unscaledDeltaTime);
                bool glowStillActive = glowRemainingSeconds > 0f;
                glowRoot?.gameObject.SetActive(glowStillActive);

                if (glowStillActive)
                {
                    UpdateCompletionGlowPulse();
                }
                else
                {
                    ResetCompletionGlowPulse();
                }
            }
            else
            {
                ResetCompletionGlowPulse();
            }

            // A missing visual root should not break logic-only tests.
            if (visualRoot == null)
            {
                return;
            }

            if (popRemainingSeconds <= 0f)
            {
                visualRoot.localScale = Vector3.one;
                return;
            }

            // Convert remaining time into normalized animation progress for the bounce helper.
            popRemainingSeconds = Mathf.Max(0f, popRemainingSeconds - Time.unscaledDeltaTime);
            float normalizedProgress = 1f - popRemainingSeconds / CompletionPopDurationSeconds;
            visualRoot.localScale = Vector3.one * CalculatePopScale(normalizedProgress);
        }

        private void UpdateCompletionGlowPulse()
        {
            // Convert remaining time into elapsed effect time so the pulse starts immediately on completion.
            float elapsedSeconds = CompletionGlowDurationSeconds - glowRemainingSeconds;

            // A sine wave gives the silhouette aura the same breathing rhythm as the bio lab glow.
            float pulse01 = (Mathf.Sin(elapsedSeconds * Mathf.PI * 2f * CompletionGlowPulseFrequency) + 1f) * 0.5f;

            // Slight scale changes push the aura just outside the HQ outline and then pull it back in.
            CompletionGlowPulseScale = Mathf.Lerp(CompletionGlowMinimumScale, CompletionGlowMaximumScale, pulse01);

            // The final half-second fades out so the aura disappears softly instead of snapping off.
            float fadeOut = Mathf.Clamp01(glowRemainingSeconds / 0.5f);
            CompletionGlowAlpha = glowBaseColor.a * Mathf.Lerp(0.82f, 1.35f, pulse01) * fadeOut;

            // Pulse only the outer glow root so the sized silhouette stays aligned with the visible art.
            if (glowRoot != null)
            {
                glowRoot.localScale = Vector3.one * CompletionGlowPulseScale;
            }

            // Reapply alpha to every generated aura renderer because all pieces share the same pulse.
            SetCompletionGlowAlpha(CompletionGlowAlpha);
        }

        private void ResetCompletionGlowPulse()
        {
            // Reset public pulse state and transform scale so later completions start from a known outline.
            CompletionGlowPulseScale = CompletionGlowMinimumScale;
            CompletionGlowAlpha = glowBaseColor.a;
            if (glowRoot != null)
            {
                glowRoot.localScale = Vector3.one;
            }

            // Restore the authored material alpha after the completion window ends.
            SetCompletionGlowAlpha(glowBaseColor.a);
        }

        private void SetCompletionGlowAlpha(float alpha)
        {
            foreach (Renderer renderer in glowRenderers)
            {
                // Generated aura pieces can be absent in reduced tests or stripped prototype scenes.
                if (renderer == null || renderer.sharedMaterial == null)
                {
                    continue;
                }

                // Preserve the aura hue while changing only the pulse transparency.
                Color pulsedColor = glowBaseColor;
                pulsedColor.a = Mathf.Clamp01(alpha);
                SetMaterialColor(renderer.sharedMaterial, pulsedColor);
            }
        }

        private void SetUpgradeSymbolColor(Color color)
        {
            foreach (Renderer renderer in upgradeSymbolRenderers)
            {
                // Individual generated pieces can be missing in reduced test setups.
                if (renderer != null)
                {
                    SetMaterialColor(renderer.sharedMaterial, color);
                }
            }
        }

        private static float CalculatePopScale(float normalizedProgress)
        {
            // The HQ starts small, overshoots, then settles back to its authored scale.
            float safeProgress = Mathf.Clamp01(normalizedProgress);
            if (safeProgress < 0.48f)
            {
                return Mathf.Lerp(0.58f, 1.18f, safeProgress / 0.48f);
            }

            return Mathf.Lerp(1.18f, 1f, (safeProgress - 0.48f) / 0.52f);
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            // A null material can occur in deliberately minimal test objects.
            if (material == null)
            {
                return;
            }

            // URP Lit and Unlit expose _BaseColor as their visible tint.
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            // Built-in and fallback prototype shaders usually expose _Color.
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            // Built-in transparent/unlit shaders can expose _TintColor instead of _Color.
            if (material.HasProperty("_TintColor"))
            {
                material.SetColor("_TintColor", color);
            }
        }

        private static Color GetMaterialColor(Material material)
        {
            // A null material falls back to the authored glow color used by runtime generation.
            if (material == null)
            {
                return new Color(0.20f, 1f, 0.72f, 0.40f);
            }

            // URP Lit and Unlit expose _BaseColor as their visible tint.
            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }

            // Built-in and fallback prototype shaders usually expose _Color.
            if (material.HasProperty("_Color"))
            {
                return material.GetColor("_Color");
            }

            // Built-in transparent/unlit shaders can expose tint only through this legacy property.
            if (material.HasProperty("_TintColor"))
            {
                return material.GetColor("_TintColor");
            }

            // Unknown shaders fall back to the authored aura color without touching missing shader properties.
            return new Color(0.20f, 1f, 0.72f, 0.40f);
        }

        private static Color CalculateLabelColor(int level)
        {
            // Reference art hides this label; fallback white HQ geometry needs black text for contrast.
            return Color.black;
        }
    }
}
