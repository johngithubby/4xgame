using System;
using System.Collections.Generic;
using LaneSurvivor.Rendering;
using UnityEngine;

namespace LaneSurvivor.Gameplay
{
    public sealed class AutoShooter : MonoBehaviour
    {
        public event Action<Vector3, Vector3, float, bool> ShotFired;

        [SerializeField]
        private PlayerSquad playerSquad;

        [SerializeField]
        private PrototypeHumanoidAnimator survivorAnimator;

        [SerializeField]
        private float shootRange = 8f;

        [SerializeField]
        private float shotInterval = 0.35f;

        [SerializeField]
        private float laneTolerance = 0.8f;

        private readonly List<Zombie> zombies = new();

        private float shotTimer;

        public void Initialize(PlayerSquad squad, float range, float interval, float targetLaneTolerance)
        {
            playerSquad = squad;
            shootRange = Mathf.Max(0.1f, range);
            shotInterval = Mathf.Max(0.05f, interval);
            laneTolerance = Mathf.Max(0.1f, targetLaneTolerance);
            survivorAnimator = playerSquad != null ? playerSquad.GetComponent<PrototypeHumanoidAnimator>() : null;
            shotTimer = 0f;
        }

        public void RegisterZombie(Zombie zombie)
        {
            if (zombie == null || zombies.Contains(zombie))
            {
                return;
            }

            zombies.Add(zombie);
            zombie.Defeated += HandleZombieDefeated;
        }

        private void Update()
        {
            if (playerSquad == null || !playerSquad.IsMoving || playerSquad.SquadCount <= 0)
            {
                return;
            }

            shotTimer -= Time.deltaTime;
            if (shotTimer > 0f)
            {
                return;
            }

            shotTimer = shotInterval;
            Zombie target = FindNearestTargetAhead();
            if (target != null)
            {
                // Capture damage once so visual feedback matches the gameplay mutation.
                float damage = playerSquad.GetTotalDamage();
                float appliedDamage = target.TakeDamage(damage);

                // Target points stay centered on the visible zombie body so tracers aim where damage text appears.
                Vector3 targetPoint = target.transform.position + Vector3.up * 0.5f;

                // Prefer a generated survivor muzzle, then let the visual layer know when it is safe to skip offsets.
                bool shotStartedAtWeaponMuzzle = playerSquad.TryGetNextWeaponMuzzlePosition(out Vector3 shotOrigin);
                if (!shotStartedAtWeaponMuzzle)
                {
                    // Old or test-only squads without generated weapons keep the existing root-derived fallback origin.
                    shotOrigin = playerSquad.transform.position + Vector3.up * 0.5f;
                }

                // The visible survivor rig uses the same target point as the tracer so the weapon aims at the zombie.
                survivorAnimator?.PlaySurvivorShot(shotOrigin, targetPoint);

                ShotFired?.Invoke(shotOrigin, targetPoint, appliedDamage, shotStartedAtWeaponMuzzle);
            }
        }

        private Zombie FindNearestTargetAhead()
        {
            Zombie bestTarget = null;
            float bestDistance = float.MaxValue;
            float squadZ = playerSquad.transform.position.z;

            for (int i = zombies.Count - 1; i >= 0; i--)
            {
                Zombie zombie = zombies[i];
                if (zombie == null || zombie.IsDefeated)
                {
                    zombies.RemoveAt(i);
                    continue;
                }

                float distanceAhead = zombie.transform.position.z - squadZ;
                if (distanceAhead < 0f || distanceAhead > shootRange)
                {
                    continue;
                }

                if (!playerSquad.IsInSameLaneAs(zombie.transform.position.x, laneTolerance))
                {
                    continue;
                }

                if (distanceAhead < bestDistance)
                {
                    bestDistance = distanceAhead;
                    bestTarget = zombie;
                }
            }

            return bestTarget;
        }

        private void HandleZombieDefeated(Zombie zombie)
        {
            zombies.Remove(zombie);
        }
    }
}
