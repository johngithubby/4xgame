using UnityEngine;
using UnityEngine.Rendering;

namespace LaneSurvivor.Rendering
{
    public static class PrototypeMaterialFactory
    {
        private static readonly string[] PreferredOpaqueShaderNames =
        {
            "Unlit/Color",
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Lit",
            "Standard",
            "Mobile/Diffuse",
            "Hidden/Internal-Colored"
        };

        public static Material Create(Color color)
        {
            // Try named opaque shaders first because world geometry needs depth writes on iOS/Metal.
            Shader shader = FindPreferredOpaqueShader();

            if (shader == null)
            {
                throw new System.InvalidOperationException("No supported opaque prototype shader was available in this Unity player.");
            }

            // Clone a fresh material so each placeholder can be tinted without mutating shared state.
            Material material = new(shader);

            // Force opaque depth behavior when the selected shader exposes blend, surface, or z-write controls.
            ConfigureForOpaqueWorldGeometry(material);

            // Apply the tint through whichever color property the selected shader exposes.
            ApplyColor(material, color);

            return material;
        }

        private static Shader FindPreferredOpaqueShader()
        {
            // Probe a short ordered list so built-in, URP, mobile, and internal shaders can satisfy prototypes.
            foreach (string shaderName in PreferredOpaqueShaderNames)
            {
                // Shader.Find returns null when the shader is stripped or not present in the current build.
                Shader shader = Shader.Find(shaderName);

                // The first available opaque shader is good enough for placeholder cubes and slabs.
                if (shader != null)
                {
                    return shader;
                }
            }

            return null;
        }

        private static void ConfigureForOpaqueWorldGeometry(Material material)
        {
            // Geometry queue keeps prototype meshes in the opaque pass so the track cannot transparent-sort over gates.
            material.renderQueue = (int)RenderQueue.Geometry;

            // RenderType helps compatible shaders and replacement passes classify the material as opaque.
            material.SetOverrideTag("RenderType", "Opaque");

            // URP surface shaders expose _Surface, where 0 means opaque and 1 means transparent.
            SetMaterialFloatIfPresent(material, "_Surface", 0f);

            // Built-in Standard exposes _Mode, where 0 means opaque.
            SetMaterialFloatIfPresent(material, "_Mode", 0f);

            // Blend One/Zero is the fixed-function equivalent of normal opaque color replacement.
            SetMaterialFloatIfPresent(material, "_SrcBlend", (float)BlendMode.One);
            SetMaterialFloatIfPresent(material, "_DstBlend", (float)BlendMode.Zero);

            // Depth writes are the critical part that keeps distant gates from being hidden by the generated road.
            SetMaterialFloatIfPresent(material, "_ZWrite", 1f);

            // Back-face culling keeps cube faces normal while still letting the double-sided road mesh draw from above.
            SetMaterialFloatIfPresent(material, "_Cull", (float)CullMode.Back);

            // Transparent keywords can remain enabled on cloned materials, so clear the common variants defensively.
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private static void ApplyColor(Material material, Color color)
        {
            // URP Lit and Unlit use _BaseColor for the visible tint.
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            // Built-in, mobile, and internal fallback shaders commonly use _Color for their tint.
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void SetMaterialFloatIfPresent(Material material, string propertyName, float value)
        {
            // Shader families expose different controls, so only write properties the active shader actually owns.
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }
    }
}
