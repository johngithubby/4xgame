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

            // Mission progression now selects the minigame layout while HQ and heroes still provide stat bonuses.
            int levelNumber = PlayerProgression.GetSelectedMissionLevel(saveData);
            return levelNumber switch
            {
                4 => CreateLevelFour(saveData),
                3 => CreateLevelThree(saveData),
                2 => CreateLevelTwo(saveData),
                _ => CreateLevelOne(saveData)
            };
        }

        private static LevelDefinition CreateLevelOne(SaveGameData saveData)
        {
            // Level one stays tiny and familiar for the first advertised minigame loop.
            LevelDefinition levelDefinition = CreateBaseDefinition(saveData, 1);
            levelDefinition.finishDistance = 50f;
            levelDefinition.squadMoveSpeed = 4.2f;
            // Lane aliases keep gate and zombie definitions tied to the shared portrait-safe lane spacing.
            float leftLane = -GameplayVisuals.SideLaneX;
            float rightLane = GameplayVisuals.SideLaneX;

            levelDefinition.gates = new[]
            {
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddSquad,
                    squadValue = 4,
                    damageValue = 0f,
                    position = new Vector3(leftLane, 1.1f, 9f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.MultiplySquad,
                    squadValue = 2,
                    damageValue = 0f,
                    position = new Vector3(rightLane, 1.1f, 9f)
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
                    position = new Vector3(leftLane, 1.1f, 36f)
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
                    position = new Vector3(rightLane, 1f, 27f)
                },
                new ZombieSpawnDefinition
                {
                    health = 24f,
                    breachPenalty = 6,
                    position = new Vector3(leftLane, 1f, 42f)
                }
            };
            return levelDefinition;
        }

        private static LevelDefinition CreateLevelTwo(SaveGameData saveData)
        {
            // Level two is a small unlocked variant with longer distance and tougher lane choices.
            LevelDefinition levelDefinition = CreateBaseDefinition(saveData, 2);
            levelDefinition.finishDistance = 58.5f;
            levelDefinition.squadMoveSpeed = 4.6f;
            // Lane aliases keep the unlocked layout using the same gameplay-visible side lanes.
            float leftLane = -GameplayVisuals.SideLaneX;
            float rightLane = GameplayVisuals.SideLaneX;

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
                    position = new Vector3(leftLane, 1.1f, 20f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.MultiplySquad,
                    squadValue = 2,
                    damageValue = 0f,
                    position = new Vector3(rightLane, 1.1f, 32f)
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
                    position = new Vector3(leftLane, 1f, 14f)
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
                    position = new Vector3(rightLane, 1f, 44f)
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

        private static LevelDefinition CreateLevelThree(SaveGameData saveData)
        {
            // Level three adds a longer midgame course with more alternating lane pressure.
            LevelDefinition levelDefinition = CreateBaseDefinition(saveData, 3);
            levelDefinition.finishDistance = 66f;
            levelDefinition.squadMoveSpeed = 4.8f;
            // Shared lane aliases keep authored obstacles aligned with the existing portrait-safe lane spacing.
            float leftLane = -GameplayVisuals.SideLaneX;
            float rightLane = GameplayVisuals.SideLaneX;

            levelDefinition.gates = new[]
            {
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddSquad,
                    squadValue = 5,
                    damageValue = 0f,
                    position = new Vector3(rightLane, 1.1f, 8f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddDamage,
                    squadValue = 0,
                    damageValue = 0.7f,
                    position = new Vector3(leftLane, 1.1f, 18f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.SubtractSquad,
                    squadValue = 6,
                    damageValue = 0f,
                    position = new Vector3(0f, 1.1f, 29f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.MultiplySquad,
                    squadValue = 2,
                    damageValue = 0f,
                    position = new Vector3(leftLane, 1.1f, 41f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddDamage,
                    squadValue = 0,
                    damageValue = 0.5f,
                    position = new Vector3(rightLane, 1.1f, 55f)
                }
            };
            levelDefinition.zombies = new[]
            {
                new ZombieSpawnDefinition
                {
                    health = 14f,
                    breachPenalty = 3,
                    position = new Vector3(0f, 1f, 11f)
                },
                new ZombieSpawnDefinition
                {
                    health = 26f,
                    breachPenalty = 5,
                    position = new Vector3(rightLane, 1f, 24f)
                },
                new ZombieSpawnDefinition
                {
                    health = 32f,
                    breachPenalty = 6,
                    position = new Vector3(leftLane, 1f, 36f)
                },
                new ZombieSpawnDefinition
                {
                    health = 45f,
                    breachPenalty = 8,
                    position = new Vector3(0f, 1f, 50f)
                },
                new ZombieSpawnDefinition
                {
                    health = 50f,
                    breachPenalty = 9,
                    position = new Vector3(rightLane, 1f, 63f)
                }
            };
            return levelDefinition;
        }

        private static LevelDefinition CreateLevelFour(SaveGameData saveData)
        {
            // Level four is the current local cap and layers more late-run decisions before the finish.
            LevelDefinition levelDefinition = CreateBaseDefinition(saveData, 4);
            levelDefinition.finishDistance = 74f;
            levelDefinition.squadMoveSpeed = 5f;
            // Lane aliases avoid scattered magic numbers and keep the final slice aligned with earlier missions.
            float leftLane = -GameplayVisuals.SideLaneX;
            float rightLane = GameplayVisuals.SideLaneX;

            levelDefinition.gates = new[]
            {
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddDamage,
                    squadValue = 0,
                    damageValue = 0.8f,
                    position = new Vector3(0f, 1.1f, 9f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddSquad,
                    squadValue = 7,
                    damageValue = 0f,
                    position = new Vector3(leftLane, 1.1f, 19f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.SubtractSquad,
                    squadValue = 8,
                    damageValue = 0f,
                    position = new Vector3(rightLane, 1.1f, 31f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.MultiplySquad,
                    squadValue = 2,
                    damageValue = 0f,
                    position = new Vector3(0f, 1.1f, 43f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddDamage,
                    squadValue = 0,
                    damageValue = 0.6f,
                    position = new Vector3(leftLane, 1.1f, 56f)
                },
                new GateSpawnDefinition
                {
                    modifierType = GateModifierType.AddSquad,
                    squadValue = 5,
                    damageValue = 0f,
                    position = new Vector3(rightLane, 1.1f, 65f)
                }
            };
            levelDefinition.zombies = new[]
            {
                new ZombieSpawnDefinition
                {
                    health = 18f,
                    breachPenalty = 4,
                    position = new Vector3(rightLane, 1f, 13f)
                },
                new ZombieSpawnDefinition
                {
                    health = 30f,
                    breachPenalty = 6,
                    position = new Vector3(0f, 1f, 25f)
                },
                new ZombieSpawnDefinition
                {
                    health = 38f,
                    breachPenalty = 7,
                    position = new Vector3(leftLane, 1f, 38f)
                },
                new ZombieSpawnDefinition
                {
                    health = 48f,
                    breachPenalty = 8,
                    position = new Vector3(rightLane, 1f, 51f)
                },
                new ZombieSpawnDefinition
                {
                    health = 56f,
                    breachPenalty = 10,
                    position = new Vector3(0f, 1f, 62f)
                },
                new ZombieSpawnDefinition
                {
                    health = 62f,
                    breachPenalty = 11,
                    position = new Vector3(leftLane, 1f, 71f)
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
            levelDefinition.laneMatchTolerance = GameplayVisuals.LaneMatchTolerance;
            levelDefinition.lanePositions = new[] { -GameplayVisuals.SideLaneX, 0f, GameplayVisuals.SideLaneX };
            levelDefinition.shootRange = 8f;
            levelDefinition.shotInterval = 0.35f;
            return levelDefinition;
        }
    }
}
