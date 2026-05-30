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
            }

            SetLabelText();
        }

        public void TryResolve(PlayerSquad playerSquad, float laneTolerance)
        {
            if (HasResolved || playerSquad == null)
            {
                return;
            }

            if (playerSquad.transform.position.z < transform.position.z)
            {
                return;
            }

            HasResolved = true;
            if (playerSquad.IsInSameLaneAs(transform.position.x, laneTolerance))
            {
                playerSquad.ApplyGate(modifierType, squadValue, damageValue);
                MarkResolved(Color.gray);
            }
            else
            {
                MarkResolved(new Color(0.18f, 0.18f, 0.18f));
            }
        }

        private void SetLabelText()
        {
            if (label == null)
            {
                return;
            }

            label.text = modifierType switch
            {
                GateModifierType.AddSquad => $"+{squadValue}",
                GateModifierType.MultiplySquad => $"x{squadValue}",
                GateModifierType.SubtractSquad => $"-{squadValue}",
                GateModifierType.AddDamage => $"+{damageValue:0.#} DMG",
                _ => "?"
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
