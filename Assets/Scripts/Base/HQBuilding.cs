using LaneSurvivor.Save;
using LaneSurvivor.Rendering;
using UnityEngine;

namespace LaneSurvivor.Base
{
    public sealed class HQBuilding : MonoBehaviour
    {
        private const float BaseVisualDiameter = 2f;

        private const float DiameterPerLevel = 0.025f;

        private const float BaseVisualHeight = 1.8f;

        private const float HeightPerLevel = 0.02f;

        private const int LevelThatReachesBlack = 50;

        private const int MaximumDetailRows = 7;

        private const int PentagonSideCount = 5;

        [SerializeField]
        private TextMesh levelLabel;

        [SerializeField]
        private Transform bodyTransform;

        [SerializeField]
        private Renderer bodyRenderer;

        [SerializeField]
        private Transform detailRoot;

        [SerializeField]
        private Material detailMaterial;

        private int appliedVisualLevel = -1;

        public int Level { get; private set; } = 1;

        public static float CalculateVisualDiameter(int level)
        {
            // HQ level one uses the authored baseline, and each upgrade adds only a small visual step.
            int safeLevel = Mathf.Max(1, level);
            return BaseVisualDiameter + (safeLevel - 1) * DiameterPerLevel;
        }

        public static float CalculateVisualHeight(int level)
        {
            // Height uses a tiny per-upgrade increment so double-digit HQ levels do not become towers.
            int safeLevel = Mathf.Max(1, level);
            return BaseVisualHeight + (safeLevel - 1) * HeightPerLevel;
        }

        public static Color CalculateLevelColor(int level)
        {
            // Level one starts white, then every upgrade slowly lowers RGB channels toward black.
            int safeLevel = Mathf.Max(1, level);
            float colorProgress = Mathf.Clamp01((safeLevel - 1) / (float)(LevelThatReachesBlack - 1));
            float channel = 1f - colorProgress;
            return new Color(channel, channel, channel);
        }

        public static int CalculateDetailRows(int level)
        {
            // Detail rows increase with level but cap before the small prototype HQ becomes visually noisy.
            int safeLevel = Mathf.Max(1, level);
            return Mathf.Clamp(safeLevel - 1, 0, MaximumDetailRows);
        }

        public void Configure(TextMesh label, Transform body = null, Renderer bodyVisual = null, Transform detailContainer = null, Material detailVisualMaterial = null)
        {
            // Keep the label reference optional so tests or placeholder scenes can use the component without UI.
            levelLabel = label;

            // Optional body references let older placeholder scenes still use label-only HQ behavior.
            bodyTransform = body;
            bodyRenderer = bodyVisual;
            detailRoot = detailContainer;
            detailMaterial = detailVisualMaterial;
        }

        public void ApplySaveData(SaveGameData saveData)
        {
            // Mirror the saved HQ level onto the scene component for display and future scene interactions.
            Level = Mathf.Max(1, saveData?.hqLevel ?? 1);
            RefreshLabel();

            // Rebuild level-dependent visuals only when the saved HQ level actually changes.
            if (appliedVisualLevel != Level)
            {
                RefreshVisuals();
                appliedVisualLevel = Level;
            }
        }

        private void RefreshLabel()
        {
            // The world label makes the placeholder HQ readable without needing imported art.
            if (levelLabel != null)
            {
                levelLabel.text = $"HQ\nLv {Level}";
                levelLabel.color = CalculateLabelColor(Level);
            }
        }

        private void RefreshVisuals()
        {
            // The HQ body remains optional for older tests, but runtime Base scenes provide it.
            if (bodyTransform == null)
            {
                return;
            }

            // Convert level into size once so body, label, and detail rows stay aligned.
            float visualDiameter = CalculateVisualDiameter(Level);
            float visualHeight = CalculateVisualHeight(Level);

            // The body mesh is centered vertically, so lift it by half-height to keep its base on the ground.
            bodyTransform.localScale = new Vector3(visualDiameter, visualHeight, visualDiameter);
            bodyTransform.localPosition = new Vector3(0f, visualHeight * 0.5f, 0f);

            // The shared renderer material is owned by this HQ body, so it can be safely tinted per level.
            if (bodyRenderer != null)
            {
                SetMaterialColor(bodyRenderer.sharedMaterial, CalculateLevelColor(Level));
            }

            // Keep the label just above the roof and in front of the camera-facing side as the HQ grows.
            PositionLabel(visualDiameter, visualHeight);

            // More HQ levels add generated side bands, restoring the earlier HQ definition without changing base borders.
            RebuildDetailRows(visualDiameter, visualHeight, CalculateDetailRows(Level));
        }

        private void PositionLabel(float visualDiameter, float visualHeight)
        {
            // A missing label is valid for narrow test scenes, so label positioning stays optional.
            if (levelLabel == null)
            {
                return;
            }

            // The label follows the roof height without inheriting the body scale.
            levelLabel.transform.localPosition = new Vector3(0f, visualHeight + 0.24f, -visualDiameter * 0.22f);
            levelLabel.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            levelLabel.transform.localScale = Vector3.one * 0.22f;
        }

        private void RebuildDetailRows(float visualDiameter, float visualHeight, int detailRows)
        {
            // Detail rows need a container and material; label-only HQs can skip them.
            if (detailRoot == null || detailMaterial == null)
            {
                return;
            }

            // Clear stale rows before creating the level-specific pattern.
            ClearDetailRows();

            // Level one intentionally has no side details so the first upgrade is visually obvious.
            if (detailRows <= 0)
            {
                return;
            }

            // The prism mesh has a unit radius of 0.5, so half the visual diameter is the wall radius.
            float visualRadius = visualDiameter * 0.5f;

            for (int rowIndex = 0; rowIndex < detailRows; rowIndex += 1)
            {
                // Evenly distribute rows up the wall while leaving breathing room near base and roof.
                float rowProgress = (rowIndex + 1f) / (detailRows + 1f);
                float rowHeight = Mathf.Lerp(visualHeight * 0.26f, visualHeight * 0.86f, rowProgress);
                CreateDetailRow(rowIndex, visualRadius, rowHeight);
            }
        }

        private void CreateDetailRow(int rowIndex, float visualRadius, float rowHeight)
        {
            for (int sideIndex = 0; sideIndex < PentagonSideCount; sideIndex += 1)
            {
                // Adjacent pentagon vertices define the visible wall segment for this detail strip.
                Vector2 firstVertex = GetPentagonPoint(visualRadius, sideIndex);
                Vector2 secondVertex = GetPentagonPoint(visualRadius, (sideIndex + 1) % PentagonSideCount);
                Vector2 edge = secondVertex - firstVertex;
                Vector2 midpoint = (firstVertex + secondVertex) * 0.5f;

                // Short strips read as windows or reinforcement bands without covering the pentagon silhouette.
                GameObject detail = PrototypeGeometryFactory.CreateCube(
                    $"HQ Detail Row {rowIndex + 1} Side {sideIndex + 1}",
                    Vector3.zero,
                    new Vector3(edge.magnitude * 0.42f, 0.045f, 0.055f),
                    detailMaterial);

                // Parent after creation so the helper's world-space defaults become local HQ-space values.
                detail.transform.SetParent(detailRoot, false);
                detail.transform.localPosition = new Vector3(midpoint.x, rowHeight, midpoint.y);
                detail.transform.localRotation = Quaternion.Euler(0f, Mathf.Atan2(-edge.y, edge.x) * Mathf.Rad2Deg, 0f);
            }
        }

        private void ClearDetailRows()
        {
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

        private static Vector2 GetPentagonPoint(float radius, int pointIndex)
        {
            // Match PrototypeGeometryFactory's regular-prism orientation so detail strips sit on the walls.
            float angle = Mathf.PI * 0.5f + pointIndex * Mathf.PI * 2f / PentagonSideCount;
            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
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
        }

        private static Color CalculateLabelColor(int level)
        {
            // Light HQ bodies need dark label text, while darker upgrades need white label text.
            Color bodyColor = CalculateLevelColor(level);
            float brightness = (bodyColor.r + bodyColor.g + bodyColor.b) / 3f;
            return brightness > 0.5f ? Color.black : Color.white;
        }
    }
}
