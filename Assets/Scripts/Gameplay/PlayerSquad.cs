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

        public int CurrentLaneIndex { get; private set; }

        public bool IsMoving { get; private set; }

        private float[] lanePositions = { 0f };

        private float laneChangeSpeed = 8f;

        public void Initialize(LevelDefinition levelDefinition)
        {
            SquadCount = Mathf.Max(0, levelDefinition.startingSquadCount);
            DamagePerMember = Mathf.Max(0f, levelDefinition.startingDamagePerMember);
            MoveSpeed = Mathf.Max(0f, levelDefinition.squadMoveSpeed);
            laneChangeSpeed = Mathf.Max(0.1f, levelDefinition.laneChangeSpeed);
            ConfigureLanes(levelDefinition.lanePositions);
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

        public bool IsInSameLaneAs(float worldX, float tolerance)
        {
            return Mathf.Abs(transform.position.x - worldX) <= Mathf.Max(0.01f, tolerance);
        }

        public void MoveLane(int direction)
        {
            if (direction == 0 || lanePositions.Length == 0)
            {
                return;
            }

            CurrentLaneIndex = Mathf.Clamp(CurrentLaneIndex + direction, 0, lanePositions.Length - 1);
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
                case GateModifierType.MultiplyDamage:
                    SetDamagePerMember(DamagePerMember * Mathf.Max(0f, damageValue));
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
            float targetX = lanePositions[Mathf.Clamp(CurrentLaneIndex, 0, lanePositions.Length - 1)];
            float nextX = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
            Vector3 nextPosition = transform.position;
            nextPosition.x = nextX;
            // Keep the placeholder squad locked above the road even if later scene edits add physics or animated effects.
            nextPosition.y = GameplayVisuals.PlayerCenterY;

            if (IsMoving)
            {
                nextPosition.z += MoveSpeed * Time.deltaTime;
            }

            transform.position = nextPosition;
        }

        private void ConfigureLanes(float[] configuredLanePositions)
        {
            lanePositions = configuredLanePositions is { Length: > 0 }
                ? (float[])configuredLanePositions.Clone()
                : new[] { 0f };

            CurrentLaneIndex = Mathf.Clamp(lanePositions.Length / 2, 0, lanePositions.Length - 1);
            // Preserve the authored prototype height while snapping the squad onto the starting lane.
            transform.position = new Vector3(lanePositions[CurrentLaneIndex], transform.position.y, transform.position.z);
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
