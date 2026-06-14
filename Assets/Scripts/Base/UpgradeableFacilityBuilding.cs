using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LaneSurvivor.Base
{
    public sealed class UpgradeableFacilityBuilding : MonoBehaviour
    {
        private const int ProgressSegments = 36;

        private const float ClickMoveTolerancePixels = 22f;

        private const float CompletionGlowDurationSeconds = 5f;

        private const float CompletionPopDurationSeconds = 0.34f;

        private const float CompletionGlowPulseFrequency = 1.75f;

        private const float CompletionGlowMinimumScale = 1f;

        private const float CompletionGlowMaximumScale = 1.08f;

        private static readonly Vector2 UpgradeSymbolClickSizePixels = new(92f, 92f);

        private static readonly Color AffordableUpgradeSymbolColor = new(0.12f, 0.92f, 0.34f);

        private static readonly Color UnaffordableUpgradeSymbolColor = new(0.44f, 0.46f, 0.48f);

        [SerializeField]
        private string facilityName = "Facility";

        [SerializeField]
        private Transform visualRoot;

        [SerializeField]
        private TextMesh fallbackLabel;

        [SerializeField]
        private Transform bodyTransform;

        [SerializeField]
        private Renderer bodyRenderer;

        [SerializeField]
        private Transform referenceModelTransform;

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

        private Func<bool> requestUpgradeAction;

        private Vector2 buildingClickSizePixels = new(132f, 112f);

        private Vector3 referenceBaseLocalPosition;

        private Vector3 glowReferenceBaseLocalPosition;

        private Vector3 upgradeSymbolBaseLocalPosition;

        private Vector3 progressBaseLocalPosition;

        private Vector3 fallbackLabelBaseLocalPosition;

        private float baseVisualWidth = 1f;

        private float baseVisualDepth = 0.8f;

        private float baseVisualHeight = 0.82f;

        private float heightPerLevel = 0.025f;

        private float referenceModelWidth = 1.9f;

        private float referenceModelHeight = 1.5f;

        private float referenceModelHeightPerLevel = 0.035f;

        private float referenceGlowPadding = 0.08f;

        private Color bodyColor = new(0.28f, 0.32f, 0.32f);

        private Color glowBaseColor = new(0.20f, 1f, 0.72f, 0.34f);

        private int appliedVisualLevel = -1;

        private bool isUpgradeRunning;

        private bool mousePressStartedAwayFromUi;

        private bool touchPressStartedAwayFromUi;

        private int touchFingerId = -1;

        private Vector2 mousePressPosition;

        private Vector2 touchPressPosition;

        private float glowRemainingSeconds;

        private float popRemainingSeconds;

        public int Level { get; private set; } = 1;

        public bool CanAffordDisplayedUpgrade { get; private set; }

        public float ProgressFillAmount { get; private set; }

        public int CompletionEffectPlayCount { get; private set; }

        public float CompletionGlowPulseScale { get; private set; } = 1f;

        public float CompletionGlowAlpha { get; private set; } = 0.34f;

        public bool IsUpgradeSymbolVisible => upgradeSymbolRoot != null && upgradeSymbolRoot.gameObject.activeSelf;

        public bool IsProgressVisible => progressRoot != null && progressRoot.gameObject.activeSelf;

        public bool IsCompletionGlowVisible => glowRoot != null && glowRoot.gameObject.activeSelf;

        public bool IsPopAnimating => popRemainingSeconds > 0f;

        public float CurrentVisualHeight => CalculateVisualHeight(Level);

        public float CurrentReferenceHeight => CalculateReferenceModelHeight(Level);

        public void Configure(
            string displayName,
            Transform visualContainer,
            TextMesh label,
            Transform body,
            Renderer bodyVisual,
            Transform referenceModel,
            Transform symbolRoot,
            Renderer[] symbolRenderers,
            Transform progressContainer,
            MeshFilter progressFill,
            Transform glowContainer,
            Func<bool> startUpgradeAction,
            Vector2 screenClickSizePixels,
            Vector3 referenceLocalPosition,
            Vector3 glowReferenceLocalPosition,
            Vector3 symbolLocalPosition,
            Vector3 progressLocalPosition,
            Vector3 labelLocalPosition,
            float visualWidth,
            float visualDepth,
            float visualHeight,
            float perLevelHeight,
            float modelWidth,
            float modelHeight,
            float modelHeightPerLevel,
            float glowPadding,
            Color visualBodyColor)
        {
            // Store the display name so status-free debug objects still identify which facility owns this behavior.
            facilityName = string.IsNullOrWhiteSpace(displayName) ? facilityName : displayName;
            visualRoot = visualContainer;
            fallbackLabel = label;
            bodyTransform = body;
            bodyRenderer = bodyVisual;
            referenceModelTransform = referenceModel;
            upgradeSymbolRoot = symbolRoot;
            upgradeSymbolRenderers = symbolRenderers ?? Array.Empty<Renderer>();
            progressRoot = progressContainer;
            progressFillMeshFilter = progressFill;
            glowRoot = glowContainer;
            glowReferenceAuraTransform = glowRoot != null ? glowRoot.Find($"{facilityName} Completion Reference Aura") : null;
            glowRenderers = glowRoot != null ? glowRoot.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
            requestUpgradeAction = startUpgradeAction;
            buildingClickSizePixels = screenClickSizePixels;
            referenceBaseLocalPosition = referenceLocalPosition;
            glowReferenceBaseLocalPosition = glowReferenceLocalPosition;
            upgradeSymbolBaseLocalPosition = symbolLocalPosition;
            progressBaseLocalPosition = progressLocalPosition;
            fallbackLabelBaseLocalPosition = labelLocalPosition;
            baseVisualWidth = visualWidth;
            baseVisualDepth = visualDepth;
            baseVisualHeight = visualHeight;
            heightPerLevel = perLevelHeight;
            referenceModelWidth = modelWidth;
            referenceModelHeight = modelHeight;
            referenceModelHeightPerLevel = modelHeightPerLevel;
            referenceGlowPadding = glowPadding;
            bodyColor = visualBodyColor;
            glowBaseColor = glowRenderers.Length > 0 && glowRenderers[0] != null ? GetMaterialColor(glowRenderers[0].sharedMaterial) : glowBaseColor;
            CompletionGlowAlpha = glowBaseColor.a;

            // The start arrow appears only after the player taps the facility.
            upgradeSymbolRoot?.gameObject.SetActive(false);

            // Progress is visible only while a saved local timer is active.
            progressRoot?.gameObject.SetActive(false);

            // Completion glow starts hidden and is activated by PlayCompletionEffects.
            glowRoot?.gameObject.SetActive(false);

            // Reference art already contains the sign text, so fallback labels stay hidden when art is present.
            if (fallbackLabel != null && referenceModelTransform != null)
            {
                fallbackLabel.gameObject.SetActive(false);
            }

            // A dynamic mesh lets the circular progress wedge update without replacing the component.
            if (progressFillMeshFilter != null && progressFillMeshFilter.sharedMesh == null)
            {
                progressFillMeshFilter.sharedMesh = new Mesh
                {
                    name = $"{facilityName} Progress Fill Mesh",
                    hideFlags = HideFlags.HideAndDontSave
                };
                progressFillMeshFilter.sharedMesh.MarkDynamic();
            }
        }

        public float CalculateVisualHeight(int level)
        {
            // Facilities grow only upward by a few pixels per level, keeping their footprint fixed.
            return baseVisualHeight + Mathf.Max(0, level - 1) * heightPerLevel;
        }

        public float CalculateReferenceModelHeight(int level)
        {
            // The visible concept image gets the same subtle height-only stretch as the hidden scaffold.
            return referenceModelHeight + Mathf.Max(0, level - 1) * referenceModelHeightPerLevel;
        }

        public void ApplyState(int level, int coins, bool upgradeRunning, int upgradeCost, float progress01)
        {
            // Mirror save-backed data onto the scene component for display and interaction rules.
            Level = Mathf.Max(1, level);
            isUpgradeRunning = upgradeRunning;
            CanAffordDisplayedUpgrade = Mathf.Max(0, coins) >= Mathf.Max(0, upgradeCost);

            // Rebuild transforms only when the saved level changes, because wallet/progress refresh every frame.
            if (appliedVisualLevel != Level)
            {
                RefreshVisuals();
                appliedVisualLevel = Level;
            }

            // Timer state controls whether the popup arrow or circular progress icon should be visible.
            RefreshUpgradeAffordance();
            RefreshProgressIcon(progress01);
        }

        public void ShowUpgradeSymbol()
        {
            // Upgrading facilities already show circular progress instead of a start affordance.
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

            // The bootstrap owns save mutation and persistence, so the visual component delegates the request.
            bool started = requestUpgradeAction?.Invoke() == true;
            if (started)
            {
                HideUpgradeSymbol();
            }

            return started;
        }

        public void PlayCompletionEffects()
        {
            // The glow lasts exactly the same local window as the bio-lab completion aura.
            glowRemainingSeconds = CompletionGlowDurationSeconds;

            // The pop animation makes the upgraded facility reappear with a short scale bounce.
            popRemainingSeconds = CompletionPopDurationSeconds;

            // Keep a counter so PlayMode tests can verify the finish path without relying on rendering timing.
            CompletionEffectPlayCount += 1;

            // Show the glow immediately so the completion frame is readable.
            glowRoot?.gameObject.SetActive(true);
            UpdateCompletionGlowPulse();
        }

        private void Update()
        {
            // World clicks are handled here so generated facilities can be interacted with without scene YAML.
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

            // Only a short press-and-release should trigger facility interaction; drags belong to map panning.
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
            // Facilities only need simple one-finger taps; two-finger gestures belong to camera pinch.
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

        public bool TryHandleBuildingClick(Vector2 screenPosition)
        {
            // A camera is required to convert screen taps into world-space facility hits.
            Camera camera = Camera.main;
            if (camera == null)
            {
                return false;
            }

            // The visible upgrade symbol has priority over the building body when both overlap onscreen.
            if (IsUpgradeSymbolVisible && IsScreenPointNearWorldPoint(camera, screenPosition, upgradeSymbolRoot.position, UpgradeSymbolClickSizePixels))
            {
                RequestUpgradeFromVisibleSymbol();
                return true;
            }

            // A tap on the visible facility reveals the start-upgrade affordance.
            if (IsScreenPointNearWorldPoint(camera, screenPosition, GetBuildingClickWorldPosition(), buildingClickSizePixels))
            {
                ShowUpgradeSymbol();
                return true;
            }

            // Any other world tap dismisses the current popup symbol.
            HideUpgradeSymbol();
            return false;
        }

        private Vector3 GetBuildingClickWorldPosition()
        {
            // Prefer the reference quad center because that is the player-visible building.
            Renderer referenceRenderer = referenceModelTransform != null ? referenceModelTransform.GetComponent<Renderer>() : null;
            if (referenceRenderer != null)
            {
                return referenceRenderer.bounds.center;
            }

            // Fallback to the hidden scaffold body mass when reference art is unavailable.
            return transform.position + new Vector3(0f, CurrentVisualHeight * 0.55f, 0f);
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

            // Rectangular screen hit areas are enough for generated reference buildings and avoid physics modules.
            float halfWidth = sizePixels.x * 0.5f;
            float halfHeight = sizePixels.y * 0.5f;
            return Mathf.Abs(screenPosition.x - screenPoint.x) <= halfWidth
                && Mathf.Abs(screenPosition.y - screenPoint.y) <= halfHeight;
        }

        private void RefreshVisuals()
        {
            // A missing visual root is valid for small tests that only exercise progression state.
            if (visualRoot == null)
            {
                return;
            }

            // Convert level into visual dimensions once so body, label, and aura stay aligned.
            float visualHeight = CurrentVisualHeight;

            // The hidden scaffold keeps a stable footprint and grows only upward with level.
            if (bodyTransform != null)
            {
                bodyTransform.localScale = new Vector3(baseVisualWidth, visualHeight, baseVisualDepth);
                bodyTransform.localPosition = new Vector3(0f, visualHeight * 0.5f, 0f);
            }

            // Body color stays stable because visible leveling now comes from height only.
            if (bodyRenderer != null)
            {
                SetMaterialColor(bodyRenderer.sharedMaterial, bodyColor);
            }

            // The exact concept card is the visible model, with only tiny height changes after upgrades.
            PositionReferenceQuad(referenceModelTransform, referenceBaseLocalPosition, 0f);

            // The completion aura is a softly padded duplicate of the same reference silhouette.
            PositionReferenceQuad(glowReferenceAuraTransform, glowReferenceBaseLocalPosition, referenceGlowPadding);

            // Popup arrows and circular progress rise along with the subtly stretched reference model.
            PositionFloatingUi();

            // Fallback labels remain available only when the reference image is missing.
            PositionFallbackLabel(visualHeight);
        }

        private void PositionReferenceQuad(Transform targetTransform, Vector3 baseLocalPosition, float padding)
        {
            // Missing reference models are valid when tests instantiate a reduced procedural fallback.
            if (targetTransform == null)
            {
                return;
            }

            // Level gains stretch the exact image upward by only a few pixels instead of adding outside pieces.
            float extraHeight = Mathf.Max(0, Level - 1) * referenceModelHeightPerLevel;
            targetTransform.localPosition = baseLocalPosition + new Vector3(0f, extraHeight * 0.5f, 0f);
            targetTransform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            targetTransform.localScale = new Vector3(referenceModelWidth + padding, referenceModelHeight + extraHeight + padding, 1f);
        }

        private void PositionFloatingUi()
        {
            // Use the same extra-height lift for arrows and progress so they do not drift into the roof art.
            float extraHeight = Mathf.Max(0, Level - 1) * referenceModelHeightPerLevel;
            if (upgradeSymbolRoot != null)
            {
                upgradeSymbolRoot.localPosition = upgradeSymbolBaseLocalPosition + new Vector3(0f, extraHeight, 0f);
            }

            if (progressRoot != null)
            {
                progressRoot.localPosition = progressBaseLocalPosition + new Vector3(0f, extraHeight, 0f);
            }
        }

        private void PositionFallbackLabel(float visualHeight)
        {
            // A missing label is valid for narrow test scenes, so label positioning stays optional.
            if (fallbackLabel == null)
            {
                return;
            }

            // Reference art contains the facility sign; fallback text is only for missing-texture builds.
            fallbackLabel.text = referenceModelTransform != null ? string.Empty : facilityName.ToUpperInvariant();
            fallbackLabel.gameObject.SetActive(referenceModelTransform == null);
            fallbackLabel.transform.localPosition = fallbackLabelBaseLocalPosition + new Vector3(0f, Mathf.Max(0f, visualHeight - baseVisualHeight), 0f);
        }

        private void RefreshUpgradeAffordance()
        {
            // Active upgrades hide the start symbol and show the circular progress icon instead.
            if (isUpgradeRunning)
            {
                upgradeSymbolRoot?.gameObject.SetActive(false);
                return;
            }

            // A visible symbol should immediately reflect wallet changes after collecting credits.
            if (IsUpgradeSymbolVisible)
            {
                SetUpgradeSymbolColor(CanAffordDisplayedUpgrade ? AffordableUpgradeSymbolColor : UnaffordableUpgradeSymbolColor);
            }
        }

        private void RefreshProgressIcon(float progress01)
        {
            // The progress icon should exist only while the local upgrade timer is running.
            if (progressRoot == null || !isUpgradeRunning)
            {
                progressRoot?.gameObject.SetActive(false);
                ProgressFillAmount = 0f;
                RebuildProgressFillMesh(0f);
                return;
            }

            // Show the overlay and fill it from the persisted timer progress.
            progressRoot.gameObject.SetActive(true);
            RebuildProgressFillMesh(progress01);
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

            // A missing visual root should not break tests that instantiate only logic-facing pieces.
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

            // A sine wave gives the silhouette aura a breathing rhythm without needing animation clips.
            float pulse01 = (Mathf.Sin(elapsedSeconds * Mathf.PI * 2f * CompletionGlowPulseFrequency) + 1f) * 0.5f;

            // Slight scale changes push the aura just outside the building outline and then pull it back in.
            CompletionGlowPulseScale = Mathf.Lerp(CompletionGlowMinimumScale, CompletionGlowMaximumScale, pulse01);

            // The final half-second fades out so the aura disappears softly instead of snapping off.
            float fadeOut = Mathf.Clamp01(glowRemainingSeconds / 0.5f);
            CompletionGlowAlpha = glowBaseColor.a * Mathf.Lerp(0.82f, 1.35f, pulse01) * fadeOut;

            // Pulse only the outer glow root so the sized reference silhouette stays aligned.
            if (glowRoot != null)
            {
                glowRoot.localScale = Vector3.one * CompletionGlowPulseScale;
            }

            // Reapply alpha to every aura renderer because all pieces share the same pulse.
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

        private static float CalculatePopScale(float normalizedProgress)
        {
            // The facility starts small, overshoots, then settles back to its authored scale.
            float safeProgress = Mathf.Clamp01(normalizedProgress);
            if (safeProgress < 0.48f)
            {
                return Mathf.Lerp(0.58f, 1.18f, safeProgress / 0.48f);
            }

            return Mathf.Lerp(1.18f, 1f, (safeProgress - 0.48f) / 0.52f);
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
                return new Color(0.20f, 1f, 0.72f, 0.34f);
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
            return new Color(0.20f, 1f, 0.72f, 0.34f);
        }

        private static bool IsPointerOverUi()
        {
            // Without an EventSystem, no UI can be consuming the pointer.
            if (EventSystem.current == null)
            {
                return false;
            }

            // The parameterless overload is the mouse/pointer path used in editor and desktop players.
            return EventSystem.current.IsPointerOverGameObject();
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            // Without an EventSystem, no UI can be consuming the touch.
            if (EventSystem.current == null)
            {
                return false;
            }

            // Touch ids need the overload so only the active finger is checked against UI.
            return EventSystem.current.IsPointerOverGameObject(pointerId);
        }
    }
}
