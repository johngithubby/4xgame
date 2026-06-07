using System;
using LaneSurvivor.Data;
using UnityEngine;

namespace LaneSurvivor.Gameplay
{
    public sealed class Zombie : MonoBehaviour
    {
        public event Action<Zombie> Defeated;

        [SerializeField]
        private float health = 5f;

        [SerializeField]
        private int breachPenalty = 1;

        [SerializeField]
        private ZombieEnemyType enemyType;

        public bool IsDefeated { get; private set; }

        public int BreachPenalty => breachPenalty;

        public ZombieEnemyType EnemyType => enemyType;

        public bool LastBreachApplied { get; private set; }

        public void Configure(float startingHealth, int startingBreachPenalty, Material zombieMaterial)
        {
            // Preserve the original configure API for existing tests and basic zombie definitions.
            Configure(startingHealth, startingBreachPenalty, zombieMaterial, ZombieEnemyType.Basic);
        }

        public void Configure(float startingHealth, int startingBreachPenalty, Material zombieMaterial, ZombieEnemyType configuredEnemyType)
        {
            health = Mathf.Max(1f, startingHealth);
            breachPenalty = Mathf.Max(0, startingBreachPenalty);
            enemyType = configuredEnemyType;
            IsDefeated = false;
            LastBreachApplied = false;

            Renderer zombieRenderer = GetComponent<Renderer>();
            if (zombieRenderer != null)
            {
                zombieRenderer.sharedMaterial = zombieMaterial;
            }
        }

        public float TakeDamage(float amount)
        {
            if (IsDefeated || amount <= 0f)
            {
                return 0f;
            }

            // Armored zombies absorb part of each volley so late missions read differently from pure health scaling.
            float appliedDamage = CalculateAppliedDamage(amount);
            health -= appliedDamage;
            if (health <= 0f)
            {
                MarkDefeated();
            }

            return appliedDamage;
        }

        public bool TryBreach(PlayerSquad playerSquad, float laneTolerance)
        {
            if (IsDefeated || playerSquad == null)
            {
                return false;
            }

            if (playerSquad.transform.position.z < transform.position.z)
            {
                return false;
            }

            LastBreachApplied = playerSquad.IsInSameLaneAs(transform.position.x, laneTolerance);
            if (LastBreachApplied)
            {
                playerSquad.TakeLoss(breachPenalty);
            }

            MarkDefeated();
            return true;
        }

        private void MarkDefeated()
        {
            IsDefeated = true;

            Renderer zombieRenderer = GetComponent<Renderer>();
            if (zombieRenderer != null)
            {
                zombieRenderer.material.color = Color.black;
            }

            gameObject.SetActive(false);
            Defeated?.Invoke(this);
        }

        private float CalculateAppliedDamage(float amount)
        {
            // Basic zombies take the full squad volley and preserve the Phase 1 behavior.
            if (enemyType != ZombieEnemyType.Armored)
            {
                return amount;
            }

            // Armored zombies still take a little damage from tiny squads so they cannot become hard-stuck.
            return Mathf.Max(0.1f, amount * 0.6f);
        }
    }
}
