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
                8 => CreateLevelEight(saveData),
                7 => CreateLevelSeven(saveData),
                6 => CreateLevelSix(saveData),
                5 => CreateLevelFive(saveData),
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

        private static LevelDefinition CreateLevelFive(SaveGameData saveData)
        {
            // Level five introduces armored zombies and the new damage multiplier gate in a still-readable course.
            LevelDefinition levelDefinition = CreateBaseDefinition(saveData, 5);
            levelDefinition.finishDistance = 82f;
            levelDefinition.squadMoveSpeed = 5.1f;
            // Lane aliases keep the first advanced content aligned with the existing portrait-safe lanes.
            float leftLane = -GameplayVisuals.SideLaneX;
            float centerLane = 0f;
            float rightLane = GameplayVisuals.SideLaneX;

            levelDefinition.gates = new[]
            {
                CreateGate(GateModifierType.AddSquad, 8, 0f, leftLane, 9f),
                CreateGate(GateModifierType.MultiplyDamage, 0, 1.4f, centerLane, 18f),
                CreateGate(GateModifierType.AddDamage, 0, 0.8f, rightLane, 29f),
                CreateGate(GateModifierType.SubtractSquad, 9, 0f, centerLane, 42f),
                CreateGate(GateModifierType.MultiplySquad, 2, 0f, leftLane, 54f),
                CreateGate(GateModifierType.AddSquad, 6, 0f, rightLane, 66f),
                CreateGate(GateModifierType.AddDamage, 0, 0.6f, centerLane, 75f)
            };
            levelDefinition.zombies = new[]
            {
                CreateZombie(ZombieEnemyType.Basic, 22f, 4, centerLane, 13f),
                CreateZombie(ZombieEnemyType.Armored, 30f, 6, rightLane, 24f),
                CreateZombie(ZombieEnemyType.Basic, 34f, 6, leftLane, 36f),
                CreateZombie(ZombieEnemyType.Armored, 44f, 8, centerLane, 49f),
                CreateZombie(ZombieEnemyType.Basic, 52f, 9, rightLane, 61f),
                CreateZombie(ZombieEnemyType.Armored, 60f, 10, leftLane, 73f),
                CreateZombie(ZombieEnemyType.Basic, 58f, 10, centerLane, 80f)
            };
            return levelDefinition;
        }

        private static LevelDefinition CreateLevelSix(SaveGameData saveData)
        {
            // Level six asks the player to trade lane safety against damage scaling more often.
            LevelDefinition levelDefinition = CreateBaseDefinition(saveData, 6);
            levelDefinition.finishDistance = 90f;
            levelDefinition.squadMoveSpeed = 5.2f;
            // Shared lane aliases avoid reintroducing wide side lanes in late content.
            float leftLane = -GameplayVisuals.SideLaneX;
            float centerLane = 0f;
            float rightLane = GameplayVisuals.SideLaneX;

            levelDefinition.gates = new[]
            {
                CreateGate(GateModifierType.AddDamage, 0, 0.9f, rightLane, 8f),
                CreateGate(GateModifierType.AddSquad, 9, 0f, centerLane, 17f),
                CreateGate(GateModifierType.MultiplyDamage, 0, 1.5f, leftLane, 27f),
                CreateGate(GateModifierType.SubtractSquad, 10, 0f, rightLane, 38f),
                CreateGate(GateModifierType.MultiplySquad, 2, 0f, centerLane, 50f),
                CreateGate(GateModifierType.AddDamage, 0, 0.7f, leftLane, 63f),
                CreateGate(GateModifierType.AddSquad, 7, 0f, rightLane, 76f),
                CreateGate(GateModifierType.MultiplyDamage, 0, 1.25f, centerLane, 84f)
            };
            levelDefinition.zombies = new[]
            {
                CreateZombie(ZombieEnemyType.Basic, 24f, 5, leftLane, 12f),
                CreateZombie(ZombieEnemyType.Basic, 32f, 6, centerLane, 23f),
                CreateZombie(ZombieEnemyType.Armored, 42f, 8, rightLane, 34f),
                CreateZombie(ZombieEnemyType.Basic, 48f, 8, leftLane, 46f),
                CreateZombie(ZombieEnemyType.Armored, 58f, 10, centerLane, 58f),
                CreateZombie(ZombieEnemyType.Basic, 64f, 11, rightLane, 70f),
                CreateZombie(ZombieEnemyType.Armored, 68f, 12, leftLane, 82f),
                CreateZombie(ZombieEnemyType.Basic, 72f, 12, centerLane, 88f)
            };
            return levelDefinition;
        }

        private static LevelDefinition CreateLevelSeven(SaveGameData saveData)
        {
            // Level seven alternates armored blockers between side lanes to reward planned lane changes.
            LevelDefinition levelDefinition = CreateBaseDefinition(saveData, 7);
            levelDefinition.finishDistance = 98f;
            levelDefinition.squadMoveSpeed = 5.35f;
            // Lane aliases keep the late mission readable on the same camera framing.
            float leftLane = -GameplayVisuals.SideLaneX;
            float centerLane = 0f;
            float rightLane = GameplayVisuals.SideLaneX;

            levelDefinition.gates = new[]
            {
                CreateGate(GateModifierType.AddSquad, 10, 0f, centerLane, 8f),
                CreateGate(GateModifierType.MultiplyDamage, 0, 1.45f, rightLane, 19f),
                CreateGate(GateModifierType.SubtractSquad, 11, 0f, leftLane, 31f),
                CreateGate(GateModifierType.AddDamage, 0, 0.9f, centerLane, 43f),
                CreateGate(GateModifierType.MultiplySquad, 2, 0f, rightLane, 55f),
                CreateGate(GateModifierType.AddSquad, 8, 0f, leftLane, 67f),
                CreateGate(GateModifierType.MultiplyDamage, 0, 1.3f, centerLane, 80f),
                CreateGate(GateModifierType.AddDamage, 0, 0.8f, rightLane, 91f)
            };
            levelDefinition.zombies = new[]
            {
                CreateZombie(ZombieEnemyType.Armored, 34f, 7, rightLane, 13f),
                CreateZombie(ZombieEnemyType.Basic, 38f, 7, centerLane, 25f),
                CreateZombie(ZombieEnemyType.Armored, 48f, 9, leftLane, 37f),
                CreateZombie(ZombieEnemyType.Basic, 56f, 10, rightLane, 49f),
                CreateZombie(ZombieEnemyType.Armored, 66f, 12, centerLane, 61f),
                CreateZombie(ZombieEnemyType.Basic, 70f, 12, leftLane, 73f),
                CreateZombie(ZombieEnemyType.Armored, 78f, 13, rightLane, 85f),
                CreateZombie(ZombieEnemyType.Basic, 82f, 14, centerLane, 94f),
                CreateZombie(ZombieEnemyType.Armored, 80f, 14, leftLane, 97f)
            };
            return levelDefinition;
        }

        private static LevelDefinition CreateLevelEight(SaveGameData saveData)
        {
            // Level eight is the new local cap and uses every current local mechanic without adding backend work.
            LevelDefinition levelDefinition = CreateBaseDefinition(saveData, 8);
            levelDefinition.finishDistance = 106f;
            levelDefinition.squadMoveSpeed = 5.5f;
            // The final local layout still uses only the prototype's three authored lanes.
            float leftLane = -GameplayVisuals.SideLaneX;
            float centerLane = 0f;
            float rightLane = GameplayVisuals.SideLaneX;

            levelDefinition.gates = new[]
            {
                CreateGate(GateModifierType.MultiplyDamage, 0, 1.5f, leftLane, 9f),
                CreateGate(GateModifierType.AddSquad, 10, 0f, rightLane, 18f),
                CreateGate(GateModifierType.AddDamage, 0, 1f, centerLane, 29f),
                CreateGate(GateModifierType.SubtractSquad, 12, 0f, rightLane, 40f),
                CreateGate(GateModifierType.MultiplySquad, 2, 0f, leftLane, 52f),
                CreateGate(GateModifierType.MultiplyDamage, 0, 1.35f, centerLane, 65f),
                CreateGate(GateModifierType.AddSquad, 8, 0f, rightLane, 78f),
                CreateGate(GateModifierType.AddDamage, 0, 0.9f, leftLane, 91f),
                CreateGate(GateModifierType.SubtractSquad, 10, 0f, centerLane, 101f)
            };
            levelDefinition.zombies = new[]
            {
                CreateZombie(ZombieEnemyType.Basic, 32f, 6, centerLane, 13f),
                CreateZombie(ZombieEnemyType.Armored, 44f, 8, leftLane, 24f),
                CreateZombie(ZombieEnemyType.Basic, 48f, 9, rightLane, 35f),
                CreateZombie(ZombieEnemyType.Armored, 60f, 11, centerLane, 47f),
                CreateZombie(ZombieEnemyType.Basic, 68f, 12, leftLane, 59f),
                CreateZombie(ZombieEnemyType.Armored, 76f, 13, rightLane, 72f),
                CreateZombie(ZombieEnemyType.Basic, 82f, 14, centerLane, 84f),
                CreateZombie(ZombieEnemyType.Armored, 88f, 15, leftLane, 96f),
                CreateZombie(ZombieEnemyType.Basic, 92f, 15, rightLane, 103f),
                CreateZombie(ZombieEnemyType.Armored, 90f, 16, centerLane, 105f)
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

        private static GateSpawnDefinition CreateGate(GateModifierType modifierType, int squadValue, float damageValue, float laneX, float distance)
        {
            // Runtime-generated levels keep gate roots on a consistent readable height for every authored mission.
            return new GateSpawnDefinition
            {
                modifierType = modifierType,
                squadValue = squadValue,
                damageValue = damageValue,
                position = new Vector3(laneX, 1.1f, distance)
            };
        }

        private static ZombieSpawnDefinition CreateZombie(ZombieEnemyType enemyType, float health, int breachPenalty, float laneX, float distance)
        {
            // Enemy type stays in level data so runtime spawning and tests can reason about authored late content.
            return new ZombieSpawnDefinition
            {
                enemyType = enemyType,
                health = health,
                breachPenalty = breachPenalty,
                position = new Vector3(laneX, 1f, distance)
            };
        }
    }
}
