using System;
using System.Collections.Generic;
using LaneSurvivor.Data;
using LaneSurvivor.Rendering;
using UnityEngine;

namespace LaneSurvivor.Gameplay
{
    public sealed class PlayerSquad : MonoBehaviour
    {
        // The generated survivor weapons all use this child name so gameplay can find muzzle anchors without art references.
        public const string WeaponMuzzleAnchorName = "Weapon Muzzle";

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

        // Registered muzzle transforms are rotated by AutoShooter so only characters with authoritative visible weapons fire.
        private readonly List<Transform> weaponMuzzles = new();

        // Exact-name candidates are gathered before selection so an imported hero can supersede hidden prototype weapons.
        private readonly List<Transform> weaponMuzzleCandidates = new();

        // This index advances after each shot and wraps to the start of the generated squad weapon list.
        private int nextWeaponMuzzleIndex;

        public int WeaponMuzzleCount => weaponMuzzles.Count;

        public void Initialize(LevelDefinition levelDefinition)
        {
            SquadCount = Mathf.Max(0, levelDefinition.startingSquadCount);
            DamagePerMember = Mathf.Max(0f, levelDefinition.startingDamagePerMember);
            MoveSpeed = Mathf.Max(0f, levelDefinition.squadMoveSpeed);
            laneChangeSpeed = Mathf.Max(0.1f, levelDefinition.laneChangeSpeed);
            ConfigureLanes(levelDefinition.lanePositions);
            RebuildWeaponMuzzleRegistry();
            IsMoving = false;
            SquadCountChanged?.Invoke(SquadCount);
            DamageChanged?.Invoke(DamagePerMember);
        }

        public void SetMoving(bool isMoving)
        {
            // Squad count gates both gameplay translation and every visible imported locomotion controller.
            IsMoving = isMoving && SquadCount > 0;

            // Push the authoritative state immediately so Animator transitions cannot miss per-frame transform sampling.
            SwatSurvivorLocomotionAnimator[] authoredLocomotion = GetComponentsInChildren<SwatSurvivorLocomotionAnimator>(true);
            foreach (SwatSurvivorLocomotionAnimator locomotion in authoredLocomotion)
            {
                locomotion.SetGameplayMoving(IsMoving);
            }
        }

        public float GetTotalDamage()
        {
            return DamagePerMember * Mathf.Max(0, SquadCount);
        }

        public bool IsInSameLaneAs(float worldX, float tolerance)
        {
            return Mathf.Abs(transform.position.x - worldX) <= Mathf.Max(0.01f, tolerance);
        }

        public void RebuildWeaponMuzzleRegistry()
        {
            // Clear stale scene or test transforms before scanning the current generated children.
            weaponMuzzles.Clear();

            // Candidate storage is reused to avoid allocating a traversal list every time scene art is rebuilt.
            weaponMuzzleCandidates.Clear();

            // Reset rotation order so a rebuilt squad begins with the leader again.
            nextWeaponMuzzleIndex = 0;

            // Recursively scan from the gameplay root because weapons live under survivor hand chains.
            CollectWeaponMuzzleCandidates(transform);

            // Every licensed-model muzzle is authoritative because each one belongs to a rendered imported rifle.
            foreach (Transform candidate in weaponMuzzleCandidates)
            {
                // Generated fallback anchors stay excluded because their placeholder weapons are hidden.
                if (candidate.GetComponentInParent<SwatSurvivorLocomotionAnimator>() != null)
                {
                    weaponMuzzles.Add(candidate);
                }
            }

            // Generated-only scenes and isolated tests retain their original round-robin firing behavior.
            if (weaponMuzzles.Count == 0)
            {
                weaponMuzzles.AddRange(weaponMuzzleCandidates);
            }
        }

        public bool TryGetNextWeaponMuzzlePosition(out Vector3 muzzlePosition)
        {
            // Reuse the transform-returning path so visual effects and older position-only callers stay in sync.
            if (!TryGetNextWeaponMuzzle(out Transform muzzle))
            {
                // The caller owns fallback origin selection when a future scene has no generated weapons.
                muzzlePosition = default;
                return false;
            }

            // World position is used by older position-only shot feedback paths.
            muzzlePosition = muzzle.position;
            return true;
        }

        public bool TryGetNextWeaponMuzzle(out Transform muzzle)
        {
            // Destroyed Unity objects compare as null, so prune before indexing the rotation list.
            PruneMissingWeaponMuzzles();

            if (weaponMuzzles.Count == 0)
            {
                // The caller owns fallback origin selection when a future scene has no generated weapons.
                muzzle = null;
                return false;
            }

            // Keep the index valid if a muzzle was removed after the last shot.
            nextWeaponMuzzleIndex %= weaponMuzzles.Count;

            // Select the current muzzle before advancing so callers receive the transform used for this shot.
            muzzle = weaponMuzzles[nextWeaponMuzzleIndex];

            // Advance for the next shot, rotating only through the authoritative visible weapon list.
            nextWeaponMuzzleIndex = (nextWeaponMuzzleIndex + 1) % weaponMuzzles.Count;
            return true;
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

        private void CollectWeaponMuzzleCandidates(Transform root)
        {
            foreach (Transform child in root)
            {
                // Exact names avoid accidentally registering unrelated VFX or future attachment points.
                if (child.name == WeaponMuzzleAnchorName)
                {
                    weaponMuzzleCandidates.Add(child);
                }

                // Depth-first order follows the generated hierarchy order: leader, left wing, then right wing.
                CollectWeaponMuzzleCandidates(child);
            }
        }

        private void PruneMissingWeaponMuzzles()
        {
            for (int i = weaponMuzzles.Count - 1; i >= 0; i--)
            {
                // UnityEngine.Object overloads null after Destroy, so this catches destroyed anchors too.
                if (weaponMuzzles[i] == null)
                {
                    weaponMuzzles.RemoveAt(i);
                }
            }

            if (weaponMuzzles.Count == 0)
            {
                // Empty registries should restart cleanly if rebuilt later.
                nextWeaponMuzzleIndex = 0;
            }
            else
            {
                // Non-empty registries keep their round-robin position after null pruning.
                nextWeaponMuzzleIndex %= weaponMuzzles.Count;
            }
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
