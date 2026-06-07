using LaneSurvivor.Data;
using UnityEngine;

namespace LaneSurvivor.Gameplay
{
    public sealed class Gate : MonoBehaviour
    {
        // A short contact flash prevents resolved gates from occluding the squad through the trailing camera.
        public const float ResolvedVisualLifetimeSeconds = 0.16f;

        [SerializeField]
        private GateModifierType modifierType;

        [SerializeField]
        private int squadValue;

        [SerializeField]
        private float damageValue;

        [SerializeField]
        private TextMesh label;

        private Material ownedVisualMaterial;

        public bool HasResolved { get; private set; }

        public bool LastResolutionApplied { get; private set; }

        public string DisplayText => label != null ? label.text : BuildLabelText();

        public void Configure(GateSpawnDefinition definition, Material gateMaterial, TextMesh labelText)
        {
            modifierType = definition.modifierType;
            squadValue = definition.squadValue;
            damageValue = definition.damageValue;
            label = labelText;
            transform.position = definition.position;

            // Reconfiguration is rare, but releasing the old clone keeps tests and editor play sessions tidy.
            ReleaseOwnedVisualMaterial();

            // Each gate owns its visible material so resolving one gate cannot recolor every other gate.
            ownedVisualMaterial = CreateOwnedMaterial(gateMaterial);

            // The tint is applied to the owned material before renderers receive it, avoiding edit-mode material instantiation.
            SetMaterialColor(ownedVisualMaterial, GetGateColor(modifierType));

            foreach (Renderer gateRenderer in GetComponentsInChildren<Renderer>())
            {
                // Gate tinting should affect marker meshes only; TextMesh keeps its font material for legibility.
                if (IsLabelRenderer(gateRenderer))
                {
                    continue;
                }

                // Runtime gates are built from child marker meshes, so every visible piece needs the configured material clone.
                if (ownedVisualMaterial != null)
                {
                    gateRenderer.sharedMaterial = ownedVisualMaterial;
                    continue;
                }

                // Some tests configure gates without render materials, so tint only when Unity exposes one.
                if (gateRenderer.sharedMaterial != null)
                {
                    SetMaterialColor(gateRenderer.sharedMaterial, GetGateColor(modifierType));
                }
            }

            SetLabelText();
        }

        public bool TryResolve(PlayerSquad playerSquad, float laneTolerance)
        {
            if (HasResolved || playerSquad == null)
            {
                return false;
            }

            // Resolve on contact so gates do not disappear while the player is still approaching them.
            if (playerSquad.transform.position.z < transform.position.z)
            {
                return false;
            }

            HasResolved = true;
            LastResolutionApplied = playerSquad.IsInSameLaneAs(transform.position.x, laneTolerance);
            if (LastResolutionApplied)
            {
                playerSquad.ApplyGate(modifierType, squadValue, damageValue);
                MarkResolved(Color.gray);
            }
            else
            {
                // Missed gates keep their modifier color so side-lane gates remain readable after the pass-by.
                MarkResolved(GetGateColor(modifierType));
            }

            return true;
        }

        private void SetLabelText()
        {
            if (label == null)
            {
                return;
            }

            label.text = BuildLabelText();
        }

        private string BuildLabelText()
        {
            return modifierType switch
            {
                GateModifierType.AddSquad => $"+{squadValue}",
                GateModifierType.MultiplySquad => $"x{squadValue}",
                GateModifierType.SubtractSquad => $"-{squadValue}",
                GateModifierType.AddDamage => $"+{damageValue:0.#} DMG",
                GateModifierType.MultiplyDamage => $"x{damageValue:0.#} DMG",
                _ => "?"
            };
        }

        private static Color GetGateColor(GateModifierType gateModifierType)
        {
            return gateModifierType switch
            {
                GateModifierType.AddSquad => new Color(0.10f, 0.72f, 0.32f),
                GateModifierType.MultiplySquad => new Color(0.10f, 0.55f, 0.95f),
                GateModifierType.SubtractSquad => new Color(0.90f, 0.18f, 0.16f),
                GateModifierType.AddDamage => new Color(0.95f, 0.72f, 0.12f),
                GateModifierType.MultiplyDamage => new Color(0.62f, 0.28f, 0.95f),
                _ => Color.white
            };
        }

        private static Material CreateOwnedMaterial(Material sourceMaterial)
        {
            if (sourceMaterial == null)
            {
                return null;
            }

            // Clone the source so per-gate tint and resolved-state tint cannot mutate the shared prototype material.
            return new Material(sourceMaterial);
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            // URP Lit and Unlit use _BaseColor for the visible tint.
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            // Built-in, sprite, UI, and some test shaders commonly expose _Color.
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private void MarkResolved(Color resolvedColor)
        {
            foreach (Renderer gateRenderer in GetComponentsInChildren<Renderer>())
            {
                // Resolved-state tint should not recolor the text label into the same color as the gate body.
                if (IsLabelRenderer(gateRenderer))
                {
                    continue;
                }

                // Tint every marker piece before consuming so the pass-through result stays readable.
                if (gateRenderer.sharedMaterial != null)
                {
                    SetMaterialColor(gateRenderer.sharedMaterial, resolvedColor);
                }
            }

            if (Application.isPlaying)
            {
                // Remove the high gate card quickly so the trailing camera cannot let it hide the player after contact.
                Destroy(gameObject, ResolvedVisualLifetimeSeconds);
            }
        }

        private bool IsLabelRenderer(Renderer renderer)
        {
            if (renderer == null || label == null)
            {
                return false;
            }

            // TextMesh owns its renderer on the same GameObject as the label component in the generated gates.
            return renderer == label.GetComponent<Renderer>();
        }

        private void OnDestroy()
        {
            // The gate creates a runtime material clone, so the gate also owns cleanup for that clone.
            ReleaseOwnedVisualMaterial();
        }

        private void ReleaseOwnedVisualMaterial()
        {
            if (ownedVisualMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                // In play mode Unity expects normal delayed destruction for engine objects.
                Destroy(ownedVisualMaterial);
            }
            else
            {
                // EditMode tests need immediate cleanup because there is no player loop to process Destroy.
                DestroyImmediate(ownedVisualMaterial);
            }

            ownedVisualMaterial = null;
        }
    }
}
