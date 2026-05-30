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

            PlayerSquad squad = squadObject.AddComponent<PlayerSquad>();
            squad.Initialize(levelDefinition);
            return squad;
        }
    }
}
