using System;
using LaneSurvivor.Data;
using UnityEngine;

namespace LaneSurvivor.Gameplay
{
    public sealed class PlayerSquad : MonoBehaviour
    {
        public event Action<int> SquadCountChanged;

        public event Action<float> DamageChanged;

        public event Action Defeated;

        public int SquadCount { get; private set; }

        public float DamagePerMember { get; private set; }

        public float MoveSpeed { get; private set; }

        public bool IsMoving { get; private set; }

        public void Initialize(LevelDefinition levelDefinition)
        {
            SquadCount = Mathf.Max(0, levelDefinition.startingSquadCount);
            DamagePerMember = Mathf.Max(0f, levelDefinition.startingDamagePerMember);
            MoveSpeed = Mathf.Max(0f, levelDefinition.squadMoveSpeed);
            IsMoving = false;
            SquadCountChanged?.Invoke(SquadCount);
            DamageChanged?.Invoke(DamagePerMember);
        }

        public void SetMoving(bool isMoving)
        {
            IsMoving = isMoving && SquadCount > 0;
        }

        public float GetTotalDamage()
        {
            return DamagePerMember * Mathf.Max(0, SquadCount);
        }

        public void ApplyGate(GateModifierType modifierType, int squadValue, float damageValue)
        {
            switch (modifierType)
            {
                case GateModifierType.AddSquad:
                    SetSquadCount(SquadCount + squadValue);
                    break;
                case GateModifierType.MultiplySquad:
                    SetSquadCount(SquadCount * Mathf.Max(0, squadValue));
                    break;
                case GateModifierType.SubtractSquad:
                    SetSquadCount(SquadCount - squadValue);
                    break;
                case GateModifierType.AddDamage:
                    SetDamagePerMember(DamagePerMember + damageValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modifierType), modifierType, "Unsupported gate modifier.");
            }
        }

        public void TakeLoss(int amount)
        {
            if (amount <= 0 || SquadCount <= 0)
            {
                return;
            }

            SetSquadCount(SquadCount - amount);
        }

        private void Update()
        {
            if (!IsMoving)
            {
                return;
            }

            transform.position += Vector3.forward * (MoveSpeed * Time.deltaTime);
        }

        private void SetSquadCount(int value)
        {
            int clampedValue = Mathf.Max(0, value);
            if (SquadCount == clampedValue)
            {
                return;
            }

            SquadCount = clampedValue;
            SquadCountChanged?.Invoke(SquadCount);

            if (SquadCount == 0)
            {
                IsMoving = false;
                Defeated?.Invoke();
            }
        }

        private void SetDamagePerMember(float value)
        {
            float clampedValue = Mathf.Max(0f, value);
            if (Mathf.Approximately(DamagePerMember, clampedValue))
            {
                return;
            }

            DamagePerMember = clampedValue;
            DamageChanged?.Invoke(DamagePerMember);
        }
    }
}
