using LaneSurvivor.Data;
using LaneSurvivor.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace LaneSurvivor.Tests.EditMode
{
    public sealed class PhaseOneGameplayTests
    {
        [Test]
        public void GateModifiers_UpdateSquadCountAndDamage()
        {
            PlayerSquad squad = CreateSquad(5, 1f);

            squad.ApplyGate(GateModifierType.AddSquad, 10, 0f);
            Assert.AreEqual(15, squad.SquadCount);

            squad.ApplyGate(GateModifierType.MultiplySquad, 2, 0f);
            Assert.AreEqual(30, squad.SquadCount);

            squad.ApplyGate(GateModifierType.SubtractSquad, 35, 0f);
            Assert.AreEqual(0, squad.SquadCount);

            squad.ApplyGate(GateModifierType.AddDamage, 0, 2.5f);
            Assert.AreEqual(3.5f, squad.DamagePerMember);
        }

        [Test]
        public void GateResolution_OnlyAppliesWhenSquadIsInGateLane()
        {
            PlayerSquad squad = CreateSquad(5, 1f);
            Gate missedGate = CreateGate(GateModifierType.AddSquad, 10, 0f, new Vector3(2f, 1f, 0f));

            squad.transform.position = new Vector3(0f, 1f, 1f);
            bool missedGateResolved = missedGate.TryResolve(squad, 0.5f);

            Assert.IsTrue(missedGateResolved);
            Assert.AreEqual(5, squad.SquadCount);
            Assert.IsTrue(missedGate.HasResolved);
            Assert.IsFalse(missedGate.LastResolutionApplied);

            Gate hitGate = CreateGate(GateModifierType.AddSquad, 10, 0f, new Vector3(0f, 1f, 0f));
            bool hitGateResolved = hitGate.TryResolve(squad, 0.5f);

            Assert.IsTrue(hitGateResolved);
            Assert.AreEqual(15, squad.SquadCount);
            Assert.IsTrue(hitGate.HasResolved);
            Assert.IsTrue(hitGate.LastResolutionApplied);
        }

        [Test]
        public void LaneMovement_ClampsToConfiguredLaneRange()
        {
            PlayerSquad squad = CreateSquad(5, 1f);

            squad.MoveLane(-1);
            squad.MoveLane(-1);
            Assert.AreEqual(0, squad.CurrentLaneIndex);

            squad.MoveLane(1);
            squad.MoveLane(1);
            squad.MoveLane(1);
            Assert.AreEqual(2, squad.CurrentLaneIndex);
        }

        [Test]
        public void Zombie_TakesDamageAndReportsDefeat()
        {
            GameObject zombieObject = new("Zombie Under Test");

            Zombie zombie = zombieObject.AddComponent<Zombie>();
            zombie.Configure(5f, 1, null);

            bool defeated = false;
            zombie.Defeated += _ => defeated = true;

            zombie.TakeDamage(5f);

            Assert.IsTrue(zombie.IsDefeated);
            Assert.IsTrue(defeated);
        }

        [Test]
        public void ZombieBreach_OnlyDamagesSquadInSameLane()
        {
            PlayerSquad squad = CreateSquad(5, 1f);
            Zombie zombie = CreateZombie(new Vector3(2f, 1f, 0f), 3);

            squad.transform.position = new Vector3(0f, 1f, 1f);
            bool resolved = zombie.TryBreach(squad, 0.5f);

            Assert.IsTrue(resolved);
            Assert.IsTrue(zombie.IsDefeated);
            Assert.IsFalse(zombie.LastBreachApplied);
            Assert.AreEqual(5, squad.SquadCount);
        }

        [Test]
        public void LevelStateEvaluator_WinsAtFinishDistance()
        {
            LevelState state = LevelStateEvaluator.Evaluate(50f, 48f, 1, LevelState.Playing);

            Assert.AreEqual(LevelState.Won, state);
        }

        [Test]
        public void LevelStateEvaluator_LosesWhenSquadCountReachesZero()
        {
            LevelState state = LevelStateEvaluator.Evaluate(10f, 48f, 0, LevelState.Playing);

            Assert.AreEqual(LevelState.Lost, state);
        }

        private static PlayerSquad CreateSquad(int startingCount, float startingDamage)
        {
            GameObject squadObject = new("Squad Under Test");

            LevelDefinition levelDefinition = ScriptableObject.CreateInstance<LevelDefinition>();
            levelDefinition.startingSquadCount = startingCount;
            levelDefinition.startingDamagePerMember = startingDamage;
            levelDefinition.squadMoveSpeed = 1f;
            levelDefinition.lanePositions = new[] { -2f, 0f, 2f };

            PlayerSquad squad = squadObject.AddComponent<PlayerSquad>();
            squad.Initialize(levelDefinition);
            return squad;
        }

        private static Gate CreateGate(GateModifierType modifierType, int squadValue, float damageValue, Vector3 position)
        {
            GameObject gateObject = new("Gate Under Test");
            GateSpawnDefinition gateDefinition = new()
            {
                modifierType = modifierType,
                squadValue = squadValue,
                damageValue = damageValue,
                position = position
            };

            Gate gate = gateObject.AddComponent<Gate>();
            gate.Configure(gateDefinition, null, null);
            return gate;
        }

        private static Zombie CreateZombie(Vector3 position, int breachPenalty)
        {
            GameObject zombieObject = new("Zombie Under Test");
            zombieObject.transform.position = position;

            Zombie zombie = zombieObject.AddComponent<Zombie>();
            zombie.Configure(5f, breachPenalty, null);
            return zombie;
        }
    }
}
