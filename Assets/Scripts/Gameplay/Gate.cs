using LaneSurvivor.Data;
using UnityEngine;

namespace LaneSurvivor.Gameplay
{
    public sealed class Gate : MonoBehaviour
    {
        [SerializeField]
        private GateModifierType modifierType;

        [SerializeField]
        private int squadValue;

        [SerializeField]
        private float damageValue;

        [SerializeField]
        private TextMesh label;

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

            Renderer gateRenderer = GetComponent<Renderer>();
            if (gateRenderer != null)
            {
                gateRenderer.sharedMaterial = gateMaterial;
                gateRenderer.material.color = GetGateColor(modifierType);
            }

            SetLabelText();
        }

        public bool TryResolve(PlayerSquad playerSquad, float laneTolerance)
        {
            if (HasResolved || playerSquad == null)
            {
                return false;
            }

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
                MarkResolved(new Color(0.18f, 0.18f, 0.18f));
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
                _ => Color.white
            };
        }

        private void MarkResolved(Color resolvedColor)
        {
            Renderer gateRenderer = GetComponent<Renderer>();
            if (gateRenderer != null)
            {
                gateRenderer.material.color = resolvedColor;
            }
        }
    }
}
