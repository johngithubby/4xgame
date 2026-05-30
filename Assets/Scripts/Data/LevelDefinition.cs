using System;
using UnityEngine;

namespace LaneSurvivor.Data
{
    [CreateAssetMenu(menuName = "Lane Survivor/Level Definition", fileName = "LevelDefinition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Min(1)]
        public int startingSquadCount = 5;

        [Min(0.1f)]
        public float startingDamagePerMember = 1f;

        [Min(1f)]
        public float finishDistance = 45f;

        [Min(0.1f)]
        public float squadMoveSpeed = 4f;

        [Min(0.1f)]
        public float shootRange = 8f;

        [Min(0.05f)]
        public float shotInterval = 0.35f;

        public GateSpawnDefinition[] gates = Array.Empty<GateSpawnDefinition>();

        public ZombieSpawnDefinition[] zombies = Array.Empty<ZombieSpawnDefinition>();
    }

    [Serializable]
    public struct GateSpawnDefinition
    {
        public GateModifierType modifierType;

        public int squadValue;

        public float damageValue;

        public Vector3 position;
    }

    [Serializable]
    public struct ZombieSpawnDefinition
    {
        [Min(1f)]
        public float health;

        [Min(0)]
        public int breachPenalty;

        public Vector3 position;
    }
}
