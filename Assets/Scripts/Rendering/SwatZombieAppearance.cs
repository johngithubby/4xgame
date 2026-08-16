using LaneSurvivor.Data;
using UnityEngine;

namespace LaneSurvivor.Rendering
{
    public sealed class SwatZombieAppearance : MonoBehaviour
    {
        // Tests and diagnostics use the exact shader/property contract applied to every imported zombie suit.
        public const string TatteredClothingShaderName = "LaneSurvivor/SWAT Zombie Tattered Clothing";
        public const string UpperColorPropertyName = "_UpperColor";
        public const string LowerColorPropertyName = "_LowerColor";
        public const string AccentColorPropertyName = "_AccentColor";
        public const string RaggedSeedPropertyName = "_RaggedSeed";

        // Five large atlas-space tears guarantee visible damage instead of leaving damage presence to chance.
        public const int TatterHoleCount = 5;

        // The seed's low nibble carries the authored spawn order, giving the first nine zombies different dominant colours.
        public const int DominantPaletteVariantMask = 0x0f;

        // Stable renderer names come directly from the optimized licensed FBX hierarchy.
        private const string SuitRendererName = "Suit";
        private const string BeltRendererName = "Belt";
        private const string BootsRendererName = "Boots";
        private const string GlovesRendererName = "Gloves_Cut";
        private const string KneePadRendererName = "Knee_Pad";
        private const string ProtectRendererName = "Protect";
        private const string ArmorRendererName = "armor";
        private const string HelmetRendererName = "AUG3M_Helmet_33393_Shape";

        // Bits make the surviving tactical silhouette compactly comparable across tests and frame diagnostics.
        private const int BeltGearBit = 1 << 0;
        private const int GlovesGearBit = 1 << 1;
        private const int KneePadGearBit = 1 << 2;
        private const int ProtectGearBit = 1 << 3;
        private const int ArmorGearBit = 1 << 4;
        private const int BootsGearBit = 1 << 5;
        private const int HelmetGearBit = 1 << 6;

        // The palette deliberately avoids the survivor's restrained teal/gray uniform and stays readable after video compression.
        private static readonly Color[] StridentPalette =
        {
            new(1.00f, 0.035f, 0.22f),
            new(1.00f, 0.24f, 0.025f),
            new(1.00f, 0.78f, 0.025f),
            new(0.55f, 1.00f, 0.035f),
            new(0.025f, 0.90f, 1.00f),
            new(0.055f, 0.25f, 1.00f),
            new(0.58f, 0.06f, 1.00f),
            new(1.00f, 0.035f, 0.72f),
            new(0.98f, 0.98f, 0.94f)
        };

        // Cached IDs avoid repeated string hashing while many zombie renderers receive property blocks at spawn time.
        private static readonly int MainTexturePropertyId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseMapPropertyId = Shader.PropertyToID("_BaseMap");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int UpperColorPropertyId = Shader.PropertyToID(UpperColorPropertyName);
        private static readonly int LowerColorPropertyId = Shader.PropertyToID(LowerColorPropertyName);
        private static readonly int AccentColorPropertyId = Shader.PropertyToID(AccentColorPropertyName);
        private static readonly int RaggedSeedPropertyId = Shader.PropertyToID(RaggedSeedPropertyName);
        private static readonly int Hole0PropertyId = Shader.PropertyToID("_Hole0");
        private static readonly int Hole1PropertyId = Shader.PropertyToID("_Hole1");
        private static readonly int Hole2PropertyId = Shader.PropertyToID("_Hole2");
        private static readonly int Hole3PropertyId = Shader.PropertyToID("_Hole3");
        private static readonly int Hole4PropertyId = Shader.PropertyToID("_Hole4");

        public int VisualSeed { get; private set; }

        public int AppearanceSignature { get; private set; }

        public Color UpperClothingColor { get; private set; }

        public Color LowerClothingColor { get; private set; }

        public Color AccentClothingColor { get; private set; }

        public int DominantPaletteIndex { get; private set; }

        public Vector4 TorsoHole { get; private set; }

        public Vector4 LeftLegHole { get; private set; }

        public Vector4 RightLegHole { get; private set; }

        public Vector4 ArmHole { get; private set; }

        public Vector4 EdgeHole { get; private set; }

        public float RaggedSeed { get; private set; }

        public int VisibleGearMask { get; private set; }

        public int HiddenTacticalGearCount { get; private set; }

        public bool IsConfigured { get; private set; }

        public void Configure(ZombieEnemyType enemyType, int visualSeed)
        {
            // A non-zero positive seed gives System.Random the same reproducible sequence on every supported platform.
            VisualSeed = NormalizeSeed(visualSeed);
            System.Random random = new(VisualSeed);

            // The factory stores spawn order in the seed's low nibble so nearby production zombies cannot share a dominant hue.
            int upperIndex = VisualSeed & DominantPaletteVariantMask;
            upperIndex %= StridentPalette.Length;
            DominantPaletteIndex = upperIndex;

            // The remaining two indices stay random but cannot duplicate the deterministic dominant colour.
            int lowerIndex = (upperIndex + 1 + random.Next(StridentPalette.Length - 1)) % StridentPalette.Length;
            int accentIndex = random.Next(StridentPalette.Length);
            while (accentIndex == upperIndex || accentIndex == lowerIndex)
            {
                // Resampling from nine entries always terminates while preserving a seed-stable sequence.
                accentIndex = random.Next(StridentPalette.Length);
            }

            UpperClothingColor = VaryPaletteColor(StridentPalette[upperIndex], random);
            LowerClothingColor = VaryPaletteColor(StridentPalette[lowerIndex], random);
            AccentClothingColor = VaryPaletteColor(StridentPalette[accentIndex], random);

            // These ranges sit inside known occupied UV islands for the front torso, both front legs, and one sleeve.
            TorsoHole = CreateHole(random, 0.53f, 0.63f, 0.74f, 0.86f, 0.09f, 0.14f, 0.09f, 0.15f);
            LeftLegHole = CreateHole(random, 0.45f, 0.53f, 0.27f, 0.47f, 0.055f, 0.085f, 0.11f, 0.18f);
            RightLegHole = CreateHole(random, 0.64f, 0.71f, 0.24f, 0.45f, 0.055f, 0.085f, 0.11f, 0.18f);

            // The two sleeve islands occupy different V bands, so one random branch damages a different arm each time.
            bool damagePositiveArm = random.Next(0, 2) == 0;
            ArmHole = damagePositiveArm
                ? CreateHole(random, 0.86f, 0.94f, 0.72f, 0.88f, 0.055f, 0.085f, 0.08f, 0.13f)
                : CreateHole(random, 0.87f, 0.95f, 0.14f, 0.32f, 0.055f, 0.085f, 0.08f, 0.13f);

            // Intersecting the outer front-panel edge removes a real bite from the silhouette rather than only drawing an interior patch.
            bool damageLeftEdge = random.Next(0, 2) == 0;
            EdgeHole = damageLeftEdge
                ? CreateHole(random, 0.425f, 0.46f, 0.69f, 0.80f, 0.06f, 0.095f, 0.10f, 0.16f)
                : CreateHole(random, 0.69f, 0.73f, 0.69f, 0.80f, 0.06f, 0.095f, 0.10f, 0.16f);

            // A wide numeric range produces unrelated tear rotations and edge frequencies even when palette colours repeat.
            RaggedSeed = 1f + (float)random.NextDouble() * 4095f;

            // Basic enemies lose the obvious vest pieces; armored enemies retain armor while other equipment still varies.
            bool armorVisible = enemyType == ZombieEnemyType.Armored;
            bool protectVisible = enemyType == ZombieEnemyType.Armored && random.NextDouble() >= 0.35;
            bool beltVisible = random.NextDouble() >= 0.42;
            bool kneePadsVisible = random.NextDouble() >= 0.48;
            bool glovesVisible = random.NextDouble() >= 0.40;
            bool helmetVisible = enemyType == ZombieEnemyType.Armored;

            // Boots remain visible to preserve readable planted-foot contact, while every other tactical layer may disappear.
            VisibleGearMask = BootsGearBit;
            VisibleGearMask |= beltVisible ? BeltGearBit : 0;
            VisibleGearMask |= glovesVisible ? GlovesGearBit : 0;
            VisibleGearMask |= kneePadsVisible ? KneePadGearBit : 0;
            VisibleGearMask |= protectVisible ? ProtectGearBit : 0;
            VisibleGearMask |= armorVisible ? ArmorGearBit : 0;
            VisibleGearMask |= helmetVisible ? HelmetGearBit : 0;
            HiddenTacticalGearCount = 5 -
                                       (beltVisible ? 1 : 0) -
                                       (glovesVisible ? 1 : 0) -
                                       (kneePadsVisible ? 1 : 0) -
                                       (protectVisible ? 1 : 0) -
                                       (armorVisible ? 1 : 0);

            // Renderer property blocks retain globally shared meshes and materials while varying every spawned instance.
            ApplyRendererAppearance(
                beltVisible,
                kneePadsVisible,
                glovesVisible,
                protectVisible,
                armorVisible,
                helmetVisible);

            // A compact deterministic signature lets tests compare complete appearances without depending on object identity.
            AppearanceSignature = CalculateAppearanceSignature();
            IsConfigured = true;
        }

        public Vector4 GetTatterHole(int holeIndex)
        {
            // A fixed index contract keeps shader parameters and test diagnostics aligned.
            return holeIndex switch
            {
                0 => TorsoHole,
                1 => LeftLegHole,
                2 => RightLegHole,
                3 => ArmHole,
                4 => EdgeHole,
                _ => throw new System.ArgumentOutOfRangeException(nameof(holeIndex))
            };
        }

        private static int NormalizeSeed(int visualSeed)
        {
            // Math.Abs cannot represent int.MinValue, so that one input maps to the largest valid positive seed.
            if (visualSeed == int.MinValue)
            {
                return int.MaxValue;
            }

            int positiveSeed = System.Math.Abs(visualSeed);
            return positiveSeed == 0 ? 1 : positiveSeed;
        }

        private static Color VaryPaletteColor(Color sourceColor, System.Random random)
        {
            // Small hue movement prevents exact palette repetition without drifting into muddy low-saturation colours.
            Color.RGBToHSV(sourceColor, out float hue, out float saturation, out float value);
            hue = Mathf.Repeat(hue + Mathf.Lerp(-0.025f, 0.025f, (float)random.NextDouble()), 1f);
            saturation = Mathf.Clamp01(Mathf.Max(0.82f, saturation * Mathf.Lerp(0.94f, 1.06f, (float)random.NextDouble())));
            value = Mathf.Clamp(Mathf.Max(0.80f, value * Mathf.Lerp(0.88f, 1.02f, (float)random.NextDouble())), 0f, 1f);
            return Color.HSVToRGB(hue, saturation, value);
        }

        private static Vector4 CreateHole(
            System.Random random,
            float minimumU,
            float maximumU,
            float minimumV,
            float maximumV,
            float minimumRadiusU,
            float maximumRadiusU,
            float minimumRadiusV,
            float maximumRadiusV)
        {
            // Independent centre and radius samples produce substantially different holes inside one guaranteed body region.
            return new Vector4(
                Mathf.Lerp(minimumU, maximumU, (float)random.NextDouble()),
                Mathf.Lerp(minimumV, maximumV, (float)random.NextDouble()),
                Mathf.Lerp(minimumRadiusU, maximumRadiusU, (float)random.NextDouble()),
                Mathf.Lerp(minimumRadiusV, maximumRadiusV, (float)random.NextDouble()));
        }

        private void ApplyRendererAppearance(
            bool beltVisible,
            bool kneePadsVisible,
            bool glovesVisible,
            bool protectVisible,
            bool armorVisible,
            bool helmetVisible)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                // The suit keeps its source alpha/normal textures and receives every tatter parameter at once.
                if (renderer.gameObject.name == SuitRendererName)
                {
                    renderer.enabled = true;
                    ApplySuitPropertyBlock(renderer);
                    continue;
                }

                // Missing complete gear renderers break the recognizable SWAT silhouette without touching animated bones.
                if (renderer.gameObject.name == BeltRendererName)
                {
                    renderer.enabled = beltVisible;
                    ApplySolidGearColor(renderer, AccentClothingColor);
                    continue;
                }

                if (renderer.gameObject.name == GlovesRendererName)
                {
                    renderer.enabled = glovesVisible;
                    ApplySolidGearColor(renderer, LowerClothingColor);
                    continue;
                }

                if (renderer.gameObject.name == KneePadRendererName)
                {
                    renderer.enabled = kneePadsVisible;
                    ApplySolidGearColor(renderer, AccentClothingColor);
                    continue;
                }

                if (renderer.gameObject.name == ProtectRendererName)
                {
                    renderer.enabled = protectVisible;
                    ApplySolidGearColor(renderer, LowerClothingColor);
                    continue;
                }

                if (renderer.gameObject.name == ArmorRendererName)
                {
                    renderer.enabled = armorVisible;
                    ApplySolidGearColor(renderer, AccentClothingColor);
                    continue;
                }

                if (renderer.gameObject.name == HelmetRendererName)
                {
                    renderer.enabled = helmetVisible;
                    ApplySolidGearColor(renderer, AccentClothingColor);
                    continue;
                }

                // Boots stay intact but use a darker saturated variant so feet remain visible against the road.
                if (renderer.gameObject.name == BootsRendererName)
                {
                    renderer.enabled = true;
                    ApplySolidGearColor(renderer, Color.Lerp(LowerClothingColor, Color.black, 0.34f));
                }
            }
        }

        private void ApplySuitPropertyBlock(Renderer suitRenderer)
        {
            MaterialPropertyBlock propertyBlock = new();
            suitRenderer.GetPropertyBlock(propertyBlock);

            // The shared shader reads these vivid colours directly instead of multiplying the almost-black source RGB.
            propertyBlock.SetColor(UpperColorPropertyId, UpperClothingColor);
            propertyBlock.SetColor(LowerColorPropertyId, LowerClothingColor);
            propertyBlock.SetColor(AccentColorPropertyId, AccentClothingColor);

            // Every guaranteed atlas region receives one independently sized tear.
            propertyBlock.SetVector(Hole0PropertyId, TorsoHole);
            propertyBlock.SetVector(Hole1PropertyId, LeftLegHole);
            propertyBlock.SetVector(Hole2PropertyId, RightLegHole);
            propertyBlock.SetVector(Hole3PropertyId, ArmHole);
            propertyBlock.SetVector(Hole4PropertyId, EdgeHole);
            propertyBlock.SetFloat(RaggedSeedPropertyId, RaggedSeed);
            suitRenderer.SetPropertyBlock(propertyBlock);
        }

        private static void ApplySolidGearColor(Renderer renderer, Color color)
        {
            MaterialPropertyBlock propertyBlock = new();
            renderer.GetPropertyBlock(propertyBlock);

            // White albedo bypasses the source's nearly black tactical colours while shared normal maps remain untouched.
            propertyBlock.SetTexture(MainTexturePropertyId, Texture2D.whiteTexture);
            propertyBlock.SetTexture(BaseMapPropertyId, Texture2D.whiteTexture);
            propertyBlock.SetColor(ColorPropertyId, color);
            propertyBlock.SetColor(BaseColorPropertyId, color);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private int CalculateAppearanceSignature()
        {
            unchecked
            {
                // FNV-style integer mixing is stable across Mono/CoreCLR and does not depend on randomized string hashes.
                uint signature = 2166136261u;
                // Omitting the input seed makes identical visual outputs collide instead of looking unique by construction.
                MixColor(ref signature, UpperClothingColor);
                MixColor(ref signature, LowerClothingColor);
                MixColor(ref signature, AccentClothingColor);
                MixHole(ref signature, TorsoHole);
                MixHole(ref signature, LeftLegHole);
                MixHole(ref signature, RightLegHole);
                MixHole(ref signature, ArmHole);
                MixHole(ref signature, EdgeHole);
                MixSignature(ref signature, Mathf.RoundToInt(RaggedSeed * 10f));
                MixSignature(ref signature, VisibleGearMask);
                return (int)(signature & 0x7fffffffu);
            }
        }

        private static void MixColor(ref uint signature, Color color)
        {
            // Byte quantization is sufficient for a diagnostic signature and avoids runtime-dependent float hashing.
            Color32 colorBytes = color;
            MixSignature(ref signature, colorBytes.r);
            MixSignature(ref signature, colorBytes.g);
            MixSignature(ref signature, colorBytes.b);
        }

        private static void MixHole(ref uint signature, Vector4 hole)
        {
            // Four-decimal UV quantization distinguishes visibly different centres and radii while remaining deterministic.
            MixSignature(ref signature, Mathf.RoundToInt(hole.x * 10000f));
            MixSignature(ref signature, Mathf.RoundToInt(hole.y * 10000f));
            MixSignature(ref signature, Mathf.RoundToInt(hole.z * 10000f));
            MixSignature(ref signature, Mathf.RoundToInt(hole.w * 10000f));
        }

        private static void MixSignature(ref uint signature, int value)
        {
            // Mixing each complete integer avoids string allocation during level construction.
            signature ^= (uint)value;
            signature *= 16777619u;
        }
    }
}
