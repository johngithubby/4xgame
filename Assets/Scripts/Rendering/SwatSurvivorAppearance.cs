using UnityEngine;

namespace LaneSurvivor.Rendering
{
    public enum SwatSurvivorWardrobeStyle
    {
        // The original centre survivor remains a fully equipped dark-navy squad commander.
        NavyCommander,

        // The left survivor removes rigid protection for a lightweight charcoal-and-burgundy scout silhouette.
        CharcoalScout,

        // The right survivor keeps every protective layer for a heavier dark-olive silhouette.
        OliveHeavy
    }

    public sealed class SwatSurvivorAppearance : MonoBehaviour
    {
        // Stable imported renderer names let wardrobe presets survive model re-instantiation and scene rebuilding.
        private const string SuitRendererName = "Suit";
        private const string BeltRendererName = "Belt";
        private const string BootsRendererName = "Boots";
        private const string GlovesRendererName = "Gloves_Cut";
        private const string KneePadRendererName = "Knee_Pad";
        private const string ProtectRendererName = "Protect";
        private const string ArmorRendererName = "armor";
        private const string HelmetRendererName = "AUG3M_Helmet_33393_Shape";
        private const string BalaclavaRendererName = "Balaclava_Mask";

        // Public gear bits make the three intentionally different silhouettes directly testable.
        public const int BeltGearBit = 1 << 0;
        public const int GlovesGearBit = 1 << 1;
        public const int KneePadGearBit = 1 << 2;
        public const int ProtectGearBit = 1 << 3;
        public const int ArmorGearBit = 1 << 4;
        public const int BootsGearBit = 1 << 5;
        public const int HelmetGearBit = 1 << 6;
        public const int BalaclavaGearBit = 1 << 7;

        // Property IDs avoid repeated shader-name lookup while the factory constructs the runtime squad.
        private static readonly int MainTexturePropertyId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseMapPropertyId = Shader.PropertyToID("_BaseMap");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

        // Scene-built squads serialize only the preset; Awake deterministically reapplies all transient property blocks.
        [SerializeField]
        private SwatSurvivorWardrobeStyle wardrobeStyle;

        public SwatSurvivorWardrobeStyle WardrobeStyle => wardrobeStyle;

        public Color UniformColor { get; private set; }

        public Color EquipmentColor { get; private set; }

        public Color ArmorColor { get; private set; }

        public int VisibleGearMask { get; private set; }

        public int AppearanceSignature { get; private set; }

        public bool IsConfigured { get; private set; }

        private void Awake()
        {
            // MaterialPropertyBlock values are runtime state, so scene loads must reconstruct them from the saved preset.
            Configure(wardrobeStyle);
        }

        public void Configure(SwatSurvivorWardrobeStyle wardrobeStyle)
        {
            // Retain the chosen preset so editor-built scenes reproduce the same appearance when loaded in a player.
            this.wardrobeStyle = wardrobeStyle;

            // Defaults describe the centre commander's complete dark-blue tactical uniform.
            bool beltVisible = true;
            bool glovesVisible = true;
            bool kneePadsVisible = true;
            bool protectVisible = false;
            bool armorVisible = true;
            bool helmetVisible = true;
            bool balaclavaVisible = false;
            UniformColor = new Color(0.055f, 0.105f, 0.22f);
            EquipmentColor = new Color(0.045f, 0.060f, 0.085f);
            ArmorColor = new Color(0.13f, 0.18f, 0.24f);

            switch (wardrobeStyle)
            {
                case SwatSurvivorWardrobeStyle.NavyCommander:
                    // The defaults intentionally preserve the leader's recognizable armored SWAT role.
                    break;

                case SwatSurvivorWardrobeStyle.CharcoalScout:
                    // Removing every rigid outer layer exposes a much narrower, lightweight clothing silhouette.
                    beltVisible = false;
                    kneePadsVisible = false;
                    protectVisible = false;
                    armorVisible = false;
                    helmetVisible = false;
                    balaclavaVisible = false;
                    UniformColor = new Color(0.095f, 0.105f, 0.13f);
                    EquipmentColor = new Color(0.24f, 0.045f, 0.075f);
                    ArmorColor = new Color(0.14f, 0.055f, 0.070f);
                    break;

                case SwatSurvivorWardrobeStyle.OliveHeavy:
                    // Retaining every imported layer creates the broadest, heaviest member of the formation.
                    protectVisible = true;
                    balaclavaVisible = true;
                    UniformColor = new Color(0.095f, 0.19f, 0.075f);
                    EquipmentColor = new Color(0.045f, 0.065f, 0.040f);
                    ArmorColor = new Color(0.18f, 0.17f, 0.095f);
                    break;

                default:
                    // Failing loudly prevents a newly added preset from silently duplicating the commander.
                    throw new System.ArgumentOutOfRangeException(nameof(wardrobeStyle), wardrobeStyle, "Unsupported survivor wardrobe style.");
            }

            // Boots remain visible for every style so authored foot contact is always readable against the road.
            VisibleGearMask = BootsGearBit;
            VisibleGearMask |= beltVisible ? BeltGearBit : 0;
            VisibleGearMask |= glovesVisible ? GlovesGearBit : 0;
            VisibleGearMask |= kneePadsVisible ? KneePadGearBit : 0;
            VisibleGearMask |= protectVisible ? ProtectGearBit : 0;
            VisibleGearMask |= armorVisible ? ArmorGearBit : 0;
            VisibleGearMask |= helmetVisible ? HelmetGearBit : 0;
            VisibleGearMask |= balaclavaVisible ? BalaclavaGearBit : 0;

            // Per-renderer blocks preserve shared meshes, materials, normal maps, and animation bindings.
            ApplyRendererAppearance(
                beltVisible,
                glovesVisible,
                kneePadsVisible,
                protectVisible,
                armorVisible,
                helmetVisible,
                balaclavaVisible);

            // A deterministic signature lets tests detect an accidentally duplicated colour-and-gear combination.
            AppearanceSignature = CalculateAppearanceSignature();
            IsConfigured = true;
        }

        private void ApplyRendererAppearance(
            bool beltVisible,
            bool glovesVisible,
            bool kneePadsVisible,
            bool protectVisible,
            bool armorVisible,
            bool helmetVisible,
            bool balaclavaVisible)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                // The almost-black source suit needs a direct albedo override for dark hues to remain distinguishable.
                if (renderer.gameObject.name == SuitRendererName)
                {
                    renderer.enabled = true;
                    ApplyDirectColor(renderer, UniformColor);
                    continue;
                }

                if (renderer.gameObject.name == BeltRendererName)
                {
                    renderer.enabled = beltVisible;
                    ApplyDirectColor(renderer, EquipmentColor);
                    continue;
                }

                if (renderer.gameObject.name == GlovesRendererName)
                {
                    renderer.enabled = glovesVisible;
                    ApplyDirectColor(renderer, EquipmentColor);
                    continue;
                }

                if (renderer.gameObject.name == KneePadRendererName)
                {
                    renderer.enabled = kneePadsVisible;
                    ApplyDirectColor(renderer, ArmorColor);
                    continue;
                }

                if (renderer.gameObject.name == ProtectRendererName)
                {
                    renderer.enabled = protectVisible;
                    ApplyDirectColor(renderer, ArmorColor);
                    continue;
                }

                if (renderer.gameObject.name == ArmorRendererName)
                {
                    renderer.enabled = armorVisible;
                    ApplyDirectColor(renderer, ArmorColor);
                    continue;
                }

                if (renderer.gameObject.name == HelmetRendererName)
                {
                    renderer.enabled = helmetVisible;
                    ApplyDirectColor(renderer, ArmorColor);
                    continue;
                }

                if (renderer.gameObject.name == BalaclavaRendererName)
                {
                    renderer.enabled = balaclavaVisible;
                    ApplyDirectColor(renderer, EquipmentColor);
                    continue;
                }

                if (renderer.gameObject.name == BootsRendererName)
                {
                    renderer.enabled = true;
                    ApplyDirectColor(renderer, Color.Lerp(EquipmentColor, Color.black, 0.35f));
                }
            }
        }

        private static void ApplyDirectColor(Renderer renderer, Color color)
        {
            MaterialPropertyBlock propertyBlock = new();
            renderer.GetPropertyBlock(propertyBlock);

            // White replaces flat near-black diffuse RGB while the material's separate normal/specular maps remain active.
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
                // FNV-style integer mixing is stable across Unity runtimes and independent of object identity.
                uint signature = 2166136261u;
                MixColor(ref signature, UniformColor);
                MixColor(ref signature, EquipmentColor);
                MixColor(ref signature, ArmorColor);
                MixSignature(ref signature, VisibleGearMask);
                return (int)(signature & 0x7fffffffu);
            }
        }

        private static void MixColor(ref uint signature, Color color)
        {
            // Byte quantization is precise enough for wardrobe diagnostics without runtime-dependent float hashing.
            Color32 colorBytes = color;
            MixSignature(ref signature, colorBytes.r);
            MixSignature(ref signature, colorBytes.g);
            MixSignature(ref signature, colorBytes.b);
        }

        private static void MixSignature(ref uint signature, int value)
        {
            // Mixing complete integers avoids temporary strings during runtime squad construction.
            signature ^= (uint)value;
            signature *= 16777619u;
        }
    }
}
