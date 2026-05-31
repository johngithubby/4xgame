using LaneSurvivor.Data;
using LaneSurvivor.Heroes;
using LaneSurvivor.Progression;
using LaneSurvivor.Save;
using UnityEngine;

namespace LaneSurvivor.Gameplay
{
    public static class LevelDefinitionFactory
    {
        public static LevelDefinition CreateForSave(SaveGameData saveData)
        {
            // Normalize the save before deriving level unlocks or hero stat bonuses.
            saveData?.Normalize();

            // HQ progression unlocks level 2 through unlockedMinigameLevel.
            int levelNumber = Mathf.Max(1, saveData?.unlockedMinigameLevel ?? 1);
            return levelNumber >= 2
                ? CreateLevelTwo(saveData)
                : CreateLevelOne(saveData);
        }

        private static LevelDefinition CreateLevelOne(SaveGameData saveData)
        {
            // Level one stays tiny and familiar for the first advertised minigame loop.
            LevelDefinition levelDefinition = CreateBaseDefinition(saveData, 1);
            levelDefinition.finishDistance = 48f;
            levelDefinition.squadMoveSpeed = 4.2f;
            levelDefinition.gates = new[]
            {
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddSquad,
                    squadValue = 4,
                    damageValue = 0f,
                    position = new Vector3(-2f, 1.1f, 9f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.MultiplySquad,
                    squadValue = 2,
                    damageValue = 0f,
                    position = new Vector3(2f, 1.1f, 9f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddDamage,
                    squadValue = 0,
                    damageValue = 0.5f,
                    position = new Vector3(0f, 1.1f, 22f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.SubtractSquad,
                    squadValue = 5,
                    damageValue = 0f,
                    position = new Vector3(-2f, 1.1f, 36f)
                }
            };
            levelDefinition.zombies = new[]
            {
                new ZombieSpawnDefinition
                {
                    health = 8f,
                    breachPenalty = 2,
                    position = new Vector3(0f, 1f, 15f)
                },
                new ZombieSpawnDefinition
                {
                    health = 18f,
                    breachPenalty = 4,
                    position = new Vector3(2f, 1f, 27f)
                },
                new ZombieSpawnDefinition
                {
                    health = 24f,
                    breachPenalty = 6,
                    position = new Vector3(-2f, 1f, 41f)
                }
            };
            return levelDefinition;
        }

        private static LevelDefinition CreateLevelTwo(SaveGameData saveData)
        {
            // Level two is a small unlocked variant with longer distance and tougher lane choices.
            LevelDefinition levelDefinition = CreateBaseDefinition(saveData, 2);
            levelDefinition.finishDistance = 62f;
            levelDefinition.squadMoveSpeed = 4.6f;
            levelDefinition.gates = new[]
            {
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddDamage,
                    squadValue = 0,
                    damageValue = 0.6f,
                    position = new Vector3(0f, 1.1f, 10f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddSquad,
                    squadValue = 6,
                    damageValue = 0f,
                    position = new Vector3(-2f, 1.1f, 20f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.MultiplySquad,
                    squadValue = 2,
                    damageValue = 0f,
                    position = new Vector3(2f, 1.1f, 32f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.SubtractSquad,
                    squadValue = 7,
                    damageValue = 0f,
                    position = new Vector3(0f, 1.1f, 48f)
                }
            };
            levelDefinition.zombies = new[]
            {
                new ZombieSpawnDefinition
                {
                    health = 12f,
                    breachPenalty = 3,
                    position = new Vector3(-2f, 1f, 14f)
                },
                new ZombieSpawnDefinition
                {
                    health = 22f,
                    breachPenalty = 5,
                    position = new Vector3(0f, 1f, 28f)
                },
                new ZombieSpawnDefinition
                {
                    health = 34f,
                    breachPenalty = 7,
                    position = new Vector3(2f, 1f, 44f)
                },
                new ZombieSpawnDefinition
                {
                    health = 42f,
                    breachPenalty = 8,
                    position = new Vector3(0f, 1f, 57f)
                }
            };
            return levelDefinition;
        }

        private static LevelDefinition CreateBaseDefinition(SaveGameData saveData, int levelNumber)
        {
            // Shared level setup applies saved HQ and equipped hero bonuses.
            LevelDefinition levelDefinition = ScriptableObject.CreateInstance<LevelDefinition>();
            levelDefinition.levelNumber = levelNumber;
            levelDefinition.startingSquadCount = 6
                + PlayerProgression.GetStartingSquadBonus(saveData)
                + HeroInventory.GetStartingSquadBonus(saveData);
            levelDefinition.startingDamagePerMember = 1f + HeroInventory.GetDamageBonus(saveData);
            levelDefinition.laneChangeSpeed = 8f;
            levelDefinition.laneMatchTolerance = 0.85f;
            levelDefinition.lanePositions = new[] { -2f, 0f, 2f };
            levelDefinition.shootRange = 8f;
            levelDefinition.shotInterval = 0.35f;
            return levelDefinition;
        }
    }
}
