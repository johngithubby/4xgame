using UnityEngine;

namespace LaneSurvivor.Rendering
{
    public static class PrototypeMaterialFactory
    {
        private static readonly string[] PreferredShaderNames =
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Unlit",
            "Standard",
            "Mobile/Diffuse",
            "Sprites/Default",
            "UI/Default"
        };

        public static Material Create(Color color)
        {
            // Try named shaders first because they are cheap and keep materials predictable in the editor.
            Shader shader = FindPreferredShader();

            if (shader == null)
            {
                throw new System.InvalidOperationException("No supported prototype shader was available in this Unity player.");
            }

            // Clone a fresh material so each placeholder can be tinted without mutating shared state.
            Material material = new(shader);

            // Apply the tint through whichever color property the selected shader exposes.
            ApplyColor(material, color);

            return material;
        }

        private static Shader FindPreferredShader()
        {
            // Probe a short ordered list so URP, built-in, and minimal UI/sprite shaders can all satisfy prototypes.
            foreach (string shaderName in PreferredShaderNames)
            {
                // Shader.Find returns null when the shader is stripped or not present in the current build.
                Shader shader = Shader.Find(shaderName);

                // The first available shader is good enough for placeholder cubes and slabs.
                if (shader != null)
                {
                    return shader;
                }
            }

            return null;
        }

        private static void ApplyColor(Material material, Color color)
        {
            // URP Lit and Unlit use _BaseColor for the visible tint.
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            // Built-in, sprite, and UI shaders commonly use _Color for their tint.
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }
    }
}
