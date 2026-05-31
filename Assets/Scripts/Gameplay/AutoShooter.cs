using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaneSurvivor.Gameplay
{
    public sealed class AutoShooter : MonoBehaviour
    {
        public event Action<Vector3, Vector3, float> ShotFired;

        [SerializeField]
        private PlayerSquad playerSquad;

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
                target.TakeDamage(damage);
                ShotFired?.Invoke(playerSquad.transform.position + Vector3.up * 0.5f, target.transform.position + Vector3.up * 0.5f, damage);
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
