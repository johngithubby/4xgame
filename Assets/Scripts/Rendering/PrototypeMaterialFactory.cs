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

        private static readonly string[] PreferredFeedbackShaderNames =
        {
            "Sprites/Default",
            "Universal Render Pipeline/Particles/Unlit",
            "Unlit/Transparent",
            "Universal Render Pipeline/Unlit",
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

        public static Material CreateAlwaysVisibleFeedback(Color color)
        {
            // Effects need a transparent shader that can draw after opaque road, gates, and zombies.
            Shader shader = FindPreferredFeedbackShader();

            if (shader == null)
            {
                throw new System.InvalidOperationException("No supported transparent prototype shader was available in this Unity player.");
            }

            // Clone a fresh material so short-lived feedback can be tinted independently.
            Material material = new(shader)
            {
                name = "LaneSurvivor Generated Always-Visible Feedback Material",
                hideFlags = HideFlags.HideAndDontSave
            };

            // Foreground render state keeps world feedback readable when iOS/Metal depth ordering gets crowded.
            ConfigureForAlwaysVisibleFeedback(material);

            // Apply the tint through the same shader-agnostic color helper as opaque prototype materials.
            ApplyColor(material, color);

            return material;
        }

        public static Material CreateAlwaysVisibleSolidFeedback(Color color)
        {
            // Solid feedback uses the same reliable shader family as visible prototype geometry.
            Shader shader = FindPreferredOpaqueShader();

            if (shader == null)
            {
                throw new System.InvalidOperationException("No supported solid feedback shader was available in this Unity player.");
            }

            // Clone a fresh material so short-lived tracer strips can be colored independently.
            Material material = new(shader)
            {
                name = "LaneSurvivor Generated Always-Visible Solid Feedback Material",
                hideFlags = HideFlags.HideAndDontSave
            };

            // Keep the material visually solid but draw it in the late feedback pass.
            ConfigureForAlwaysVisibleSolidFeedback(material);

            // Apply the visible tracer tint through the shader-agnostic color helper.
            ApplyColor(material, color);

            return material;
        }

        public static Material CreateAlwaysVisibleText(Font font)
        {
            // Unity's built-in text shader keeps TextMesh glyph alpha while rendering independently of scene depth.
            Shader shader = Shader.Find("GUI/Text Shader") ?? FindPreferredFeedbackShader();

            if (shader == null)
            {
                throw new System.InvalidOperationException("No supported text feedback shader was available in this Unity player.");
            }

            // Text feedback owns its material because every spawned label can fade without mutating shared font state.
            Material material = new(shader)
            {
                name = "LaneSurvivor Generated Always-Visible Text Material",
                hideFlags = HideFlags.HideAndDontSave
            };

            // The text path uses the same late transparent pass as shot tracers.
            ConfigureForAlwaysVisibleFeedback(material);

            // TextMesh vertex color supplies the label tint, so the material stays neutral white.
            ApplyColor(material, Color.white);

            // Preserve the font atlas texture when the selected shader expects one.
            ApplyFontTexture(material, font);

            return material;
        }

        public static Material CreateTexturedTransparent(Texture texture, Color tintColor, string materialName, int renderQueue)
        {
            // Reference-backed actors need a texture-capable transparent shader instead of the solid-color prototype path.
            Shader shader = FindPreferredFeedbackShader();

            if (shader == null)
            {
                throw new System.InvalidOperationException("No supported transparent textured prototype shader was available in this Unity player.");
            }

            // Clone a material per reference texture so import settings and render state stay isolated from other art.
            Material material = new(shader)
            {
                name = materialName,
                mainTexture = texture,
                renderQueue = renderQueue
            };

            // Character reference cards should alpha-blend over the road without adding rectangular PNG backgrounds.
            ConfigureForTexturedTransparent(material, renderQueue);

            // Bind the PNG through both common built-in and URP texture property names.
            ApplyTexture(material, texture);

            // Apply a neutral or authored tint through whichever color property the shader exposes.
            ApplyColor(material, tintColor);

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

        private static Shader FindPreferredFeedbackShader()
        {
            // Prefer sprite/particle-style shaders because thin foreground feedback consumes vertex colors reliably through them.
            foreach (string shaderName in PreferredFeedbackShaderNames)
            {
                // Shader.Find returns null when the shader is stripped or unavailable in the current build.
                Shader shader = Shader.Find(shaderName);

                // Any available shader in this list can carry thin foreground feedback.
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

        private static void ConfigureForAlwaysVisibleFeedback(Material material)
        {
            // Overlay queue draws short-lived world feedback after opaque placeholder geometry.
            material.renderQueue = (int)RenderQueue.Overlay;

            // Transparent classification keeps feedback out of the opaque depth-writing pass.
            material.SetOverrideTag("RenderType", "Transparent");

            // URP surface shaders expose _Surface, where 1 means transparent.
            SetMaterialFloatIfPresent(material, "_Surface", 1f);

            // Built-in Standard exposes _Mode, where 3 means transparent.
            SetMaterialFloatIfPresent(material, "_Mode", 3f);

            // Standard alpha blending gives feedback readable edges without adding opaque blocks to the lane.
            SetMaterialFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetMaterialFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);

            // Feedback should never write depth or it can hide later gates, zombies, or the finish label.
            SetMaterialFloatIfPresent(material, "_ZWrite", 0f);

            // Always pass the depth test so iOS/Metal cannot bury impact text behind generated cards.
            SetMaterialFloatIfPresent(material, "_ZTest", (float)CompareFunction.Always);

            // Two-sided drawing keeps camera-facing line strips and glyph meshes readable through lane changes.
            SetMaterialFloatIfPresent(material, "_Cull", (float)CullMode.Off);

            // Transparent keywords help compatible shader variants enter their alpha-blended path.
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private static void ConfigureForAlwaysVisibleSolidFeedback(Material material)
        {
            // Overlay queue keeps the flat shot strip above the road and card depth pass.
            material.renderQueue = (int)RenderQueue.Overlay;

            // Solid feedback remains classified separately from normal opaque world geometry.
            material.SetOverrideTag("RenderType", "Opaque");

            // URP surface shaders expose _Surface, where 0 means opaque.
            SetMaterialFloatIfPresent(material, "_Surface", 0f);

            // Built-in Standard exposes _Mode, where 0 means opaque.
            SetMaterialFloatIfPresent(material, "_Mode", 0f);

            // Solid replacement blending avoids alpha-sorting surprises for the very small tracer mesh.
            SetMaterialFloatIfPresent(material, "_SrcBlend", (float)BlendMode.One);
            SetMaterialFloatIfPresent(material, "_DstBlend", (float)BlendMode.Zero);

            // The tracer should not write depth because it is temporary feedback, not level geometry.
            SetMaterialFloatIfPresent(material, "_ZWrite", 0f);

            // When supported, ignore depth so generated road/cards cannot bury the shot streak.
            SetMaterialFloatIfPresent(material, "_ZTest", (float)CompareFunction.Always);

            // Draw both sides of the flat strip when the shader exposes culling state.
            SetMaterialFloatIfPresent(material, "_Cull", (float)CullMode.Off);

            // Clear transparent keywords so solid feedback does not enter a stripped alpha variant.
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private static void ConfigureForTexturedTransparent(Material material, int renderQueue)
        {
            // Reference cards sit in the transparent pass so PNG alpha can cut out authored silhouettes.
            material.renderQueue = renderQueue;

            // Transparent classification keeps sprite-card backgrounds out of the opaque depth pass.
            material.SetOverrideTag("RenderType", "Transparent");

            // URP surface shaders expose _Surface, where 1 means transparent.
            SetMaterialFloatIfPresent(material, "_Surface", 1f);

            // Built-in Standard exposes _Mode, where 3 means transparent.
            SetMaterialFloatIfPresent(material, "_Mode", 3f);

            // Standard alpha blending preserves the antialiased edge from generated reference cutouts.
            SetMaterialFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);

            // Destination alpha blending keeps road and zombie geometry visible around the cutout.
            SetMaterialFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);

            // The card should not write depth because world meshes and tracer paths own gameplay positioning.
            SetMaterialFloatIfPresent(material, "_ZWrite", 0f);

            // Draw both sides so the fixed chase camera and editor view cannot cull flat reference cards.
            SetMaterialFloatIfPresent(material, "_Cull", (float)CullMode.Off);

            // Transparent keywords help compatible shader variants enter their alpha-blended path.
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private static void ApplyTexture(Material material, Texture texture)
        {
            // Built-in sprite and transparent shaders usually sample _MainTex.
            SetMaterialTextureIfPresent(material, "_MainTex", texture);

            // URP unlit and lit shaders usually sample _BaseMap.
            SetMaterialTextureIfPresent(material, "_BaseMap", texture);
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

        private static void ApplyFontTexture(Material material, Font font)
        {
            // A null font can happen in stripped test contexts, so leave texture binding optional.
            if (font == null || !material.HasProperty("_MainTex"))
            {
                return;
            }

            // Font materials own the generated atlas texture that TextMesh glyphs sample.
            Material fontMaterial = font.material;
            if (fontMaterial == null || fontMaterial.mainTexture == null)
            {
                return;
            }

            // Copy only the texture reference; color and depth state remain controlled by this feedback material.
            material.SetTexture("_MainTex", fontMaterial.mainTexture);
        }

        private static void SetMaterialFloatIfPresent(Material material, string propertyName, float value)
        {
            // Shader families expose different controls, so only write properties the active shader actually owns.
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void SetMaterialTextureIfPresent(Material material, string propertyName, Texture texture)
        {
            // Shader families expose different texture slots, so only write properties the active shader actually owns.
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }
    }
}
