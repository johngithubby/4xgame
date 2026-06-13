using System;
using System.Collections.Generic;
using System.Reflection;
using LaneSurvivor.Progression;
using LaneSurvivor.Save;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LaneSurvivor.Base
{
    public sealed class BioLabBuilding : MonoBehaviour
    {
        private const float BaseVisualWidth = 1.08f;

        private const float BaseVisualHeight = 0.92f;

        private const float HeightPerLevel = 0.025f;

        public const float ModelYawDegrees = -17f;

        private const float ReferenceModelWidth = 1.96f;

        private const float ReferenceModelHeight = 1.96f;

        private const float ReferenceModelHeightPerLevel = 0.035f;

        private const int ProgressSegments = 36;

        private const float ClickMoveTolerancePixels = 22f;

        private static readonly Vector2 LabClickSizePixels = new(132f, 112f);

        private static readonly Vector2 UpgradeSymbolClickSizePixels = new(92f, 92f);

        private const float CompletionGlowDurationSeconds = 5f;

        private const float CompletionPopDurationSeconds = 0.34f;

        private const float CompletionGlowPulseFrequency = 1.75f;

        private const float CompletionGlowMinimumScale = 1f;

        private const float CompletionGlowMaximumScale = 1.08f;

        private const float CompletionGlowAuraPadding = 0.10f;

        private const float CompletionReferenceGlowPadding = 0.06f;

        private const float ReferenceModelZ = -0.54f;

        private const float ReferenceGlowZ = -0.49f;

        private static readonly Color AffordableUpgradeSymbolColor = new(0.12f, 0.92f, 0.34f);

        private static readonly Color UnaffordableUpgradeSymbolColor = new(0.44f, 0.46f, 0.48f);

        private static object finishSoundClip;

        [SerializeField]
        private Transform visualRoot;

        [SerializeField]
        private TextMesh levelLabel;

        [SerializeField]
        private Transform bodyTransform;

        [SerializeField]
        private Renderer bodyRenderer;

        [SerializeField]
        private Transform domeTransform;

        [SerializeField]
        private Transform referenceModelTransform;

        [SerializeField]
        private Transform detailRoot;

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
        private Transform glowBodyOutlineRoot;

        [SerializeField]
        private Transform glowDomeOutlineRoot;

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

        private Color glowBaseColor = new(0.20f, 1f, 0.72f, 0.34f);

        public int Level { get; private set; } = 1;

        public bool CanAffordDisplayedUpgrade { get; private set; }

        public float ProgressFillAmount { get; private set; }

        public int CompletionEffectPlayCount { get; private set; }

        public int CompletionSoundRequestCount { get; private set; }

        public float CompletionGlowPulseScale { get; private set; } = 1f;

        public float CompletionGlowAlpha { get; private set; } = 0.34f;

        public bool HasCompletionSoundSource => audioSource != null;

        public bool HasGeneratedCompletionSoundClip => finishSoundClip != null;

        public bool IsUpgradeSymbolVisible => upgradeSymbolRoot != null && upgradeSymbolRoot.gameObject.activeSelf;

        public bool IsProgressVisible => progressRoot != null && progressRoot.gameObject.activeSelf;

        public bool IsCompletionGlowVisible => glowRoot != null && glowRoot.gameObject.activeSelf;

        public bool IsPopAnimating => popRemainingSeconds > 0f;

        public static float CalculateVisualWidth(int level)
        {
            // The lab footprint stays fixed; upgrades should now read only as tiny height gains.
            return BaseVisualWidth;
        }

        public static float CalculateVisualHeight(int level)
        {
            // A small per-level lift gives progression feedback without adding exterior structures.
            int safeLevel = Mathf.Max(1, level);
            return BaseVisualHeight + (safeLevel - 1) * HeightPerLevel;
        }

        public static Color CalculateBodyColor(int level)
        {
            // Keep lab color stable so the only level-dependent visual change is a subtle height increase.
            return new Color(0.08f, 0.50f, 0.54f);
        }

        public void Configure(
            Transform visualContainer,
            TextMesh label,
            Transform body,
            Renderer bodyVisual,
            Transform dome,
            Transform referenceModel,
            Transform detailContainer,
            Transform symbolRoot,
            Renderer[] symbolRenderers,
            Transform progressContainer,
            MeshFilter progressFill,
            Transform glowContainer,
            Component soundSource,
            Func<bool> startUpgradeAction)
        {
            // Keep every generated reference optional so tests can instantiate a narrow visual-only lab.
            visualRoot = visualContainer;
            levelLabel = label;
            bodyTransform = body;
            bodyRenderer = bodyVisual;
            domeTransform = dome;
            referenceModelTransform = referenceModel;
            detailRoot = detailContainer;
            upgradeSymbolRoot = symbolRoot;
            upgradeSymbolRenderers = symbolRenderers ?? Array.Empty<Renderer>();
            progressRoot = progressContainer;
            progressFillMeshFilter = progressFill;
            glowRoot = glowContainer;
            glowReferenceAuraTransform = glowRoot != null ? glowRoot.Find("Bio Lab Completion Reference Aura") : null;
            glowBodyOutlineRoot = glowRoot != null ? glowRoot.Find("Bio Lab Completion Body Outline") : null;
            glowDomeOutlineRoot = glowRoot != null ? glowRoot.Find("Bio Lab Completion Dome Outline") : null;
            glowRenderers = glowRoot != null ? glowRoot.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
            glowBaseColor = glowRenderers.Length > 0 && glowRenderers[0] != null ? GetMaterialColor(glowRenderers[0].sharedMaterial) : glowBaseColor;
            CompletionGlowAlpha = glowBaseColor.a;
            audioSource = soundSource;
            requestUpgradeAction = startUpgradeAction;

            // The symbol should appear only after the player taps the lab.
            upgradeSymbolRoot?.gameObject.SetActive(false);

            // The progress icon is visible only while a saved upgrade timer is active.
            progressRoot?.gameObject.SetActive(false);

            // Completion glow starts hidden and is activated by PlayCompletionEffects.
            glowRoot?.gameObject.SetActive(false);

            // The reference texture already contains the BIO LAB sign, so the old generated text stays hidden.
            if (levelLabel != null && referenceModelTransform != null)
            {
                levelLabel.gameObject.SetActive(false);
            }

            // A dynamic mesh lets the circular progress wedge update without replacing the component.
            if (progressFillMeshFilter != null && progressFillMeshFilter.sharedMesh == null)
            {
                progressFillMeshFilter.sharedMesh = new Mesh
                {
                    name = "Bio Lab Progress Fill Mesh",
                    hideFlags = HideFlags.HideAndDontSave
                };
                progressFillMeshFilter.sharedMesh.MarkDynamic();
            }
        }

        public void ApplySaveData(SaveGameData saveData, DateTime utcNow)
        {
            // Mirror the saved lab level onto the scene component for display and interaction rules.
            Level = Mathf.Max(1, saveData?.bioLabLevel ?? 1);

            // Cache the active timer state so click handlers know whether to show the upgrade symbol.
            isUpgradeRunning = saveData != null && saveData.bioLabUpgradeInProgress;

            // The popup symbol uses the current wallet state to decide whether it should be green or grey.
            int coins = Mathf.Max(0, saveData?.coins ?? 0);
            int upgradeCost = PlayerProgression.GetBioLabUpgradeCost(Level);
            CanAffordDisplayedUpgrade = coins >= upgradeCost;

            // Rebuild level-dependent visuals only when the saved lab level actually changes.
            if (appliedVisualLevel != Level)
            {
                RefreshVisuals();
                appliedVisualLevel = Level;
            }

            // Timer state controls whether the popup symbol or circular progress icon should be visible.
            RefreshUpgradeAffordance();
            RefreshProgressIcon(saveData, utcNow);
        }

        public void ShowUpgradeSymbol()
        {
            // Upgrading labs already show the circular progress icon instead of a start-upgrade affordance.
            if (isUpgradeRunning || upgradeSymbolRoot == null)
            {
                return;
            }

            // Make the existing symbol visible and recolor it from the latest wallet affordability state.
            upgradeSymbolRoot.gameObject.SetActive(true);
            SetUpgradeSymbolColor(CanAffordDisplayedUpgrade ? AffordableUpgradeSymbolColor : UnaffordableUpgradeSymbolColor);
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
                upgradeSymbolRoot.gameObject.SetActive(false);
            }

            return started;
        }

        public void PlayCompletionEffects()
        {
            // The glow lasts exactly the acceptance-requested five seconds.
            glowRemainingSeconds = CompletionGlowDurationSeconds;

            // The pop animation makes the upgraded lab reappear with a short scale bounce.
            popRemainingSeconds = CompletionPopDurationSeconds;

            // Keep a counter so PlayMode tests can verify the finish path without relying on audio hardware.
            CompletionEffectPlayCount += 1;

            // Show the glow immediately so the completion frame is readable.
            glowRoot?.gameObject.SetActive(true);
            UpdateCompletionGlowPulse();

            // Play a generated local chime instead of importing or licensing a sound asset.
            PlayFinishSound();
        }

        private void Update()
        {
            // World clicks are handled here so the generated lab can be interacted with without scene YAML.
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

            // Only a short press-and-release should trigger lab interaction; drags belong to map panning.
            if (!Input.GetMouseButtonUp(0) || !mousePressStartedAwayFromUi)
            {
                return;
            }

            // Large movement means the player was dragging the map rather than tapping the building.
            Vector2 releasePosition = Input.mousePosition;
            if (Vector2.Distance(mousePressPosition, releasePosition) <= ClickMoveTolerancePixels)
            {
                TryHandleWorldClick(releasePosition);
            }

            mousePressStartedAwayFromUi = false;
        }

        private void HandleTouchClick()
        {
            // The bio lab only needs simple one-finger taps; two-finger gestures belong to camera pinch.
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
                    TryHandleWorldClick(touch.position);
                }
            }

            touchPressStartedAwayFromUi = false;
            touchFingerId = -1;
        }

        private void TryHandleWorldClick(Vector2 screenPosition)
        {
            // A camera is required to convert screen taps into world-space lab hits.
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            // The visible upgrade symbol has priority over the lab body when both overlap onscreen.
            if (IsUpgradeSymbolVisible && IsScreenPointNearWorldPoint(camera, screenPosition, upgradeSymbolRoot.position, UpgradeSymbolClickSizePixels))
            {
                RequestUpgradeFromVisibleSymbol();
                return;
            }

            // A tap on the lab body reveals the start-upgrade affordance.
            if (IsScreenPointNearWorldPoint(camera, screenPosition, GetLabClickWorldPosition(), LabClickSizePixels))
            {
                ShowUpgradeSymbol();
            }
        }

        private Vector3 GetLabClickWorldPosition()
        {
            // Aim the tappable lab center at the body mass rather than the root's ground position.
            return transform.position + new Vector3(0f, CalculateVisualHeight(Level) * 0.55f, 0f);
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

        private void RefreshVisuals()
        {
            // A missing visual root is valid for small tests that only exercise progression state.
            if (visualRoot == null)
            {
                return;
            }

            // Convert level into visual dimensions once so body, label, details, and colliders stay aligned.
            float visualWidth = CalculateVisualWidth(Level);
            float visualHeight = CalculateVisualHeight(Level);

            // The body mesh is centered vertically, so lift it by half-height to keep its base on the pad.
            if (bodyTransform != null)
            {
                bodyTransform.localScale = new Vector3(visualWidth, visualHeight, visualWidth * 0.82f);
                bodyTransform.localPosition = new Vector3(0f, visualHeight * 0.5f, 0f);
                bodyTransform.localRotation = Quaternion.Euler(0f, ModelYawDegrees, 0f);
            }

            // Body color stays stable because visual leveling now comes from height only.
            if (bodyRenderer != null)
            {
                SetMaterialColor(bodyRenderer.sharedMaterial, CalculateBodyColor(Level));
            }

            // The dome rides on the current roof height so both the building and aura silhouette stay aligned.
            if (domeTransform != null)
            {
                domeTransform.localScale = new Vector3(visualWidth * 0.58f, 0.32f, visualWidth * 0.58f);
                domeTransform.localPosition = new Vector3(0f, visualHeight + 0.18f, 0f);
                domeTransform.localRotation = Quaternion.Euler(0f, ModelYawDegrees, 0f);
            }

            // The completion aura hugs the current body and dome outline instead of floating as a separate ring.
            PositionCompletionGlowOutline(visualWidth, visualHeight);

            // The reference card is the visible model, with only tiny height changes after upgrades.
            PositionReferenceModel(visualHeight);

            // The front sign follows the body height without inheriting body scale.
            PositionLabel(visualWidth, visualHeight);

            // Remove any stale generated exterior pieces from older prototype versions.
            ClearDetailChildren(detailRoot);
        }

        private void PositionCompletionGlowOutline(float visualWidth, float visualHeight)
        {
            // Missing aura roots are allowed for reduced test setups and older scene objects.
            if (glowRoot == null)
            {
                return;
            }

            // Keep the outer root reserved for pulse animation so sizing and pulsing do not fight each other.
            glowRoot.localPosition = Vector3.zero;

            // The reference aura reuses the exact cutout silhouette and sits just behind the visible model.
            if (glowReferenceAuraTransform != null)
            {
                PositionReferenceQuad(glowReferenceAuraTransform, CompletionReferenceGlowPadding, CompletionReferenceGlowPadding, ReferenceGlowZ);
                return;
            }

            // The body outline is a unit cube template scaled just outside the fallback lab body.
            if (glowBodyOutlineRoot != null)
            {
                float bodyDepth = visualWidth * 0.82f;
                glowBodyOutlineRoot.localPosition = new Vector3(0f, visualHeight * 0.5f, 0f);
                glowBodyOutlineRoot.localRotation = Quaternion.Euler(0f, ModelYawDegrees, 0f);
                glowBodyOutlineRoot.localScale = new Vector3(visualWidth + CompletionGlowAuraPadding, visualHeight + CompletionGlowAuraPadding, bodyDepth + CompletionGlowAuraPadding);
            }

            // The dome outline tracks the roof cap so the aura follows the full silhouette, not just the block body.
            if (glowDomeOutlineRoot != null)
            {
                float domeDiameter = visualWidth * 0.64f + CompletionGlowAuraPadding;
                glowDomeOutlineRoot.localPosition = new Vector3(0f, visualHeight + 0.18f, 0f);
                glowDomeOutlineRoot.localRotation = Quaternion.Euler(0f, ModelYawDegrees, 0f);
                glowDomeOutlineRoot.localScale = new Vector3(domeDiameter, 1f, domeDiameter);
            }
        }

        private void PositionReferenceModel(float visualHeight)
        {
            // Missing reference models are valid when tests instantiate a narrow procedural fallback.
            if (referenceModelTransform == null)
            {
                return;
            }

            // Level one renders the concept image at its authored aspect ratio; later levels only stretch upward slightly.
            PositionReferenceQuad(referenceModelTransform, 0f, 0f, ReferenceModelZ);
        }

        private void PositionReferenceQuad(Transform targetTransform, float widthPadding, float heightPadding, float zPosition)
        {
            // Level gains stretch the exact image upward by only a few pixels instead of adding outside pieces.
            float extraHeight = Mathf.Max(0, Level - 1) * ReferenceModelHeightPerLevel;

            // The textured card faces the same angled Base camera whether it is visible art or the aura behind it.
            targetTransform.localPosition = new Vector3(0f, 0.82f + extraHeight * 0.5f, zPosition);
            targetTransform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            targetTransform.localScale = new Vector3(ReferenceModelWidth + widthPadding, ReferenceModelHeight + extraHeight + heightPadding, 1f);
        }

        private void PositionLabel(float visualWidth, float visualHeight)
        {
            // A missing label is valid for narrow test scenes, so label positioning stays optional.
            if (levelLabel == null)
            {
                return;
            }

            // The sign names the concept-model building while HUD text carries the saved level.
            levelLabel.text = referenceModelTransform != null ? string.Empty : "BIO\nLAB";
            levelLabel.gameObject.SetActive(referenceModelTransform == null);
            levelLabel.color = Color.white;
            Quaternion modelYaw = Quaternion.Euler(0f, ModelYawDegrees, 0f);
            levelLabel.transform.localPosition = modelYaw * new Vector3(0.08f, visualHeight * 0.66f, -visualWidth * 0.52f);
            levelLabel.transform.localRotation = Quaternion.Euler(65f, ModelYawDegrees, 0f);
            levelLabel.transform.localScale = Vector3.one * 0.105f;
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

        private void RefreshProgressIcon(SaveGameData saveData, DateTime utcNow)
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
            RebuildProgressFillMesh(PlayerProgression.GetBioLabUpgradeProgress01(saveData, utcNow));
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

            // A sine wave gives the outline aura a breathing rhythm without needing animation clips.
            float pulse01 = (Mathf.Sin(elapsedSeconds * Mathf.PI * 2f * CompletionGlowPulseFrequency) + 1f) * 0.5f;

            // Slight scale changes push the aura just outside the building outline and then pull it back in.
            CompletionGlowPulseScale = Mathf.Lerp(CompletionGlowMinimumScale, CompletionGlowMaximumScale, pulse01);

            // The final half-second fades out so the aura disappears softly instead of snapping off.
            float fadeOut = Mathf.Clamp01(glowRemainingSeconds / 0.5f);
            CompletionGlowAlpha = glowBaseColor.a * Mathf.Lerp(0.82f, 1.35f, pulse01) * fadeOut;

            // Pulse only the outer glow root so the sized body/dome outline templates stay aligned.
            if (glowRoot != null)
            {
                glowRoot.localScale = Vector3.one * CompletionGlowPulseScale;
            }

            // Reapply alpha to every generated aura segment because all pieces share the same pulse.
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
            // The lab starts small, overshoots, then settles back to its authored scale.
            float safeProgress = Mathf.Clamp01(normalizedProgress);
            if (safeProgress < 0.48f)
            {
                return Mathf.Lerp(0.58f, 1.18f, safeProgress / 0.48f);
            }

            return Mathf.Lerp(1.18f, 1f, (safeProgress - 0.48f) / 0.52f);
        }

        private void PlayFinishSound()
        {
            // Record every finish-sound request even when an audio module is unavailable in a test runner.
            CompletionSoundRequestCount += 1;

            // Audio can be absent in some test runners, so missing components simply skip playback.
            if (audioSource == null)
            {
                return;
            }

            // Lazily generate the chime only when the completion path is actually used.
            object clip = GetFinishSoundClip();
            if (clip != null)
            {
                // Invoke PlayOneShot through reflection so the runtime assembly does not depend on AudioModule.
                MethodInfo playOneShotMethod = audioSource.GetType().GetMethod("PlayOneShot", new[] { clip.GetType() });
                playOneShotMethod?.Invoke(audioSource, new[] { clip });
            }
        }

        private static object GetFinishSoundClip()
        {
            // Reuse one generated clip so repeated upgrades do not allocate new audio buffers.
            if (finishSoundClip != null)
            {
                return finishSoundClip;
            }

            // Resolve AudioClip only when Unity has loaded the optional audio module.
            Type audioClipType = FindOptionalUnityAudioType("UnityEngine.AudioClip");
            if (audioClipType == null)
            {
                return null;
            }

            // A short two-tone synthesized chime satisfies the finish-sound requirement without imported assets.
            const int sampleRate = 22050;
            const float clipSeconds = 0.42f;
            int sampleCount = Mathf.CeilToInt(sampleRate * clipSeconds);
            float[] samples = new float[sampleCount];

            for (int index = 0; index < sampleCount; index += 1)
            {
                // Time in seconds drives the generated sine waves.
                float time = index / (float)sampleRate;

                // The envelope fades out quickly so repeated local upgrades do not become harsh.
                float envelope = 1f - Mathf.Clamp01(time / clipSeconds);

                // Two simple tones create a tiny "upgrade complete" flourish.
                float firstTone = Mathf.Sin(Mathf.PI * 2f * 660f * time);
                float secondTone = Mathf.Sin(Mathf.PI * 2f * 990f * time) * 0.55f;
                samples[index] = (firstTone + secondTone) * envelope * 0.22f;
            }

            // Create the clip through reflection so builds without AudioModule can still compile the prototype.
            MethodInfo createMethod = audioClipType.GetMethod("Create", new[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(bool) });
            finishSoundClip = createMethod?.Invoke(null, new object[] { "Bio Lab Upgrade Complete Chime", sampleCount, 1, sampleRate, false });
            if (finishSoundClip == null)
            {
                return null;
            }

            // Push the generated samples into the reflected AudioClip instance when the method is present.
            MethodInfo setDataMethod = audioClipType.GetMethod("SetData", new[] { typeof(float[]), typeof(int) });
            setDataMethod?.Invoke(finishSoundClip, new object[] { samples, 0 });
            return finishSoundClip;
        }

        public static Type FindOptionalUnityAudioType(string fullTypeName)
        {
            // First try the direct assembly-qualified lookup, which works when Unity has loaded AudioModule normally.
            Type directType = Type.GetType($"{fullTypeName}, UnityEngine.AudioModule");
            if (directType != null)
            {
                return directType;
            }

            // Some test runners load engine modules without making Type.GetType resolve them by name.
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type existingType = assembly.GetType(fullTypeName);
                if (existingType != null)
                {
                    return existingType;
                }
            }

            try
            {
                // Loading by assembly name gives the optional audio path one last chance without compile-time references.
                Assembly audioAssembly = Assembly.Load("UnityEngine.AudioModule");
                return audioAssembly.GetType(fullTypeName);
            }
            catch
            {
                // Missing AudioModule is allowed in stripped test environments; callers will skip playback.
                return null;
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

        private static void ClearDetailChildren(Transform parent)
        {
            // Null detail roots are valid for reduced tests that instantiate only progression-facing pieces.
            if (parent == null)
            {
                return;
            }

            for (int childIndex = parent.childCount - 1; childIndex >= 0; childIndex -= 1)
            {
                // Disable first so legacy exterior pieces disappear immediately even when Destroy waits.
                GameObject childObject = parent.GetChild(childIndex).gameObject;
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

            // Touch input needs the finger id so Unity checks the correct pointer against UI.
            return EventSystem.current.IsPointerOverGameObject(pointerId);
        }
    }
}
