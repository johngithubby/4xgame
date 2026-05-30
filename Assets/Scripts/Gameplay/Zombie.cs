using System;
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

        public bool IsDefeated { get; private set; }

        public int BreachPenalty => breachPenalty;

        public void Configure(float startingHealth, int startingBreachPenalty, Material zombieMaterial)
        {
            health = Mathf.Max(1f, startingHealth);
            breachPenalty = Mathf.Max(0, startingBreachPenalty);
            IsDefeated = false;

            Renderer zombieRenderer = GetComponent<Renderer>();
            if (zombieRenderer != null)
            {
                zombieRenderer.sharedMaterial = zombieMaterial;
            }
        }

        public void TakeDamage(float amount)
        {
            if (IsDefeated || amount <= 0f)
            {
                return;
            }

            health -= amount;
            if (health <= 0f)
            {
                MarkDefeated();
            }
        }

        public void TryBreach(PlayerSquad playerSquad, float laneTolerance)
        {
            if (IsDefeated || playerSquad == null)
            {
                return;
            }

            if (playerSquad.transform.position.z < transform.position.z)
            {
                return;
            }

            if (playerSquad.IsInSameLaneAs(transform.position.x, laneTolerance))
            {
                playerSquad.TakeLoss(breachPenalty);
            }

            MarkDefeated();
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
    }
}
