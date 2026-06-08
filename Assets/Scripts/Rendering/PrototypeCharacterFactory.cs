using LaneSurvivor.Data;
using UnityEngine;

namespace LaneSurvivor.Rendering
{
    public static class PrototypeCharacterFactory
    {
        private const float SurvivorWingScale = 0.72f;

        private static readonly Color SurvivorSkinColor = new(0.84f, 0.62f, 0.43f);

        private static readonly Color SurvivorPantsColor = new(0.20f, 0.27f, 0.34f);

        private static readonly Color SurvivorBootColor = new(0.035f, 0.035f, 0.04f);

        private static readonly Color SurvivorGearColor = new(0.16f, 0.22f, 0.25f);

        private static readonly Color SurvivorWeaponColor = new(0.08f, 0.075f, 0.07f);

        private static readonly Color ZombieSkinColor = new(0.39f, 0.58f, 0.32f);

        private static readonly Color ZombieShirtColor = new(0.19f, 0.28f, 0.24f);

        private static readonly Color ZombiePantsColor = new(0.58f, 0.66f, 0.70f);

        private static readonly Color ZombieWoundColor = new(0.62f, 0.04f, 0.035f);

        private static readonly Color ZombieEyeColor = new(0.04f, 0.04f, 0.035f);

        private static readonly Color ArmorColor = new(0.33f, 0.39f, 0.43f);

        private static readonly Color ArmorTrimColor = new(0.12f, 0.15f, 0.17f);

        public static GameObject CreatePlayerSquad(string name, Vector3 position, Material uniformMaterial, Material accentMaterial)
        {
            // The gameplay root stays at the same authoritative transform used for lanes, camera follow, and shooting.
            GameObject squadRoot = new(name);
            squadRoot.transform.position = position;

            // Materials are intentionally shared across the three mini survivors to keep the generated scene small.
            Material skinMaterial = CreateMaterial(SurvivorSkinColor);
            Material pantsMaterial = CreateMaterial(SurvivorPantsColor);
            Material bootMaterial = CreateMaterial(SurvivorBootColor);
            Material gearMaterial = CreateMaterial(SurvivorGearColor);
            Material weaponMaterial = CreateMaterial(SurvivorWeaponColor);
            Material bodyMaterial = uniformMaterial != null ? uniformMaterial : CreateMaterial(new Color(0.12f, 0.74f, 0.86f));
            Material highlightMaterial = accentMaterial != null ? accentMaterial : CreateMaterial(new Color(1f, 0.13f, 0.72f));

            // A three-person wedge makes squad count feel like people without spawning one mesh per count value.
            CreateSurvivor(squadRoot.transform, "Survivor Leader", new Vector3(0f, 0f, 0.08f), 1f, bodyMaterial, highlightMaterial, skinMaterial, pantsMaterial, bootMaterial, gearMaterial, weaponMaterial);

            // Side survivors sit slightly lower and behind the leader so the group reads as a formation from the chase camera.
            CreateSurvivor(squadRoot.transform, "Survivor Left Wing", new Vector3(-0.40f, -0.06f, -0.30f), SurvivorWingScale, bodyMaterial, highlightMaterial, skinMaterial, pantsMaterial, bootMaterial, gearMaterial, weaponMaterial);

            // Mirroring the side placement gives the player a recognizably human squad silhouette in one lane.
            CreateSurvivor(squadRoot.transform, "Survivor Right Wing", new Vector3(0.40f, -0.06f, -0.30f), SurvivorWingScale, bodyMaterial, highlightMaterial, skinMaterial, pantsMaterial, bootMaterial, gearMaterial, weaponMaterial);

            // The procedural animator swings the generated limbs only when the gameplay root is moving.
            PrototypeHumanoidAnimator animator = squadRoot.AddComponent<PrototypeHumanoidAnimator>();
            animator.Configure(PrototypeHumanoidAnimationStyle.SurvivorSquad);

            return squadRoot;
        }

        public static GameObject CreateZombie(string name, Vector3 position, Material zombieMaterial, ZombieEnemyType enemyType)
        {
            // The zombie root remains the gameplay object that AutoShooter and breach rules target.
            GameObject zombieRoot = new(name);
            zombieRoot.transform.position = position;

            // Basic and armored zombies share a body rig but use extra armor tells for the tougher type.
            Material skinMaterial = zombieMaterial != null ? zombieMaterial : CreateMaterial(ZombieSkinColor);
            Material shirtMaterial = CreateMaterial(ZombieShirtColor);
            Material pantsMaterial = CreateMaterial(ZombiePantsColor);
            Material woundMaterial = CreateMaterial(ZombieWoundColor);
            Material eyeMaterial = CreateMaterial(ZombieEyeColor);
            Material armorMaterial = CreateMaterial(ArmorColor);
            Material armorTrimMaterial = CreateMaterial(ArmorTrimColor);

            // The internal figure root lets the whole body lean without moving the gameplay root off its lane center.
            GameObject figureRoot = new("Zombie Figure");
            figureRoot.transform.SetParent(zombieRoot.transform, false);
            figureRoot.transform.localPosition = Vector3.zero;
            figureRoot.transform.localRotation = enemyType == ZombieEnemyType.Armored ? Quaternion.Euler(0f, 4f, 0f) : Quaternion.Euler(0f, -8f, 0f);
            figureRoot.transform.localScale = enemyType == ZombieEnemyType.Armored ? Vector3.one * 1.08f : Vector3.one;

            // The torso is an ellipsoid instead of a card, giving the enemy a readable human body mass.
            CreateSpherePart(figureRoot.transform, "Zombie Torso", new Vector3(0f, 0f, 0f), new Vector3(0.38f, 0.62f, 0.23f), shirtMaterial, Quaternion.Euler(0f, 0f, enemyType == ZombieEnemyType.Armored ? 1f : -6f));

            // A rounded pelvis anchors the legs and avoids the old single-rectangle lower body.
            CreateSpherePart(figureRoot.transform, "Zombie Pelvis", new Vector3(0f, -0.34f, 0.01f), new Vector3(0.34f, 0.20f, 0.22f), pantsMaterial, Quaternion.identity);

            // The green head with facial features makes the threat read immediately as zombie-like.
            CreateSpherePart(figureRoot.transform, "Zombie Head", new Vector3(0f, 0.48f, -0.03f), new Vector3(0.25f, 0.28f, 0.24f), skinMaterial, Quaternion.Euler(0f, 0f, enemyType == ZombieEnemyType.Armored ? 2f : -9f));

            // Dark eyes are tiny, but they give the face a direction and stop it reading as a plain ball.
            CreateSpherePart(figureRoot.transform, "Zombie Eye Left", new Vector3(-0.055f, 0.51f, -0.14f), new Vector3(0.035f, 0.025f, 0.018f), eyeMaterial, Quaternion.identity);
            CreateSpherePart(figureRoot.transform, "Zombie Eye Right", new Vector3(0.055f, 0.50f, -0.14f), new Vector3(0.035f, 0.025f, 0.018f), eyeMaterial, Quaternion.identity);

            // A flat dark mouth sells the undead face from the angled chase camera.
            CreateSpherePart(figureRoot.transform, "Zombie Mouth", new Vector3(0.01f, 0.42f, -0.145f), new Vector3(0.10f, 0.025f, 0.016f), eyeMaterial, Quaternion.Euler(0f, 0f, -4f));

            // Connected shoulder chains keep zombie hands attached when the shamble swings their reaching arms.
            CreateZombieArm(figureRoot.transform, "Left", -1f, skinMaterial);
            CreateZombieArm(figureRoot.transform, "Right", 1f, skinMaterial);

            // Connected hip/knee/ankle chains make zombie legs bend as limbs instead of detached rods.
            CreateZombieLeg(figureRoot.transform, "Left", -1f, 0.02f, pantsMaterial);
            CreateZombieLeg(figureRoot.transform, "Right", 1f, -0.04f, pantsMaterial);

            // A red torn patch adds an unmistakable zombie cue while still using generated geometry only.
            CreateSpherePart(figureRoot.transform, "Zombie Wound", new Vector3(-0.08f, 0.08f, -0.13f), new Vector3(0.13f, 0.18f, 0.025f), woundMaterial, Quaternion.Euler(0f, 0f, -18f));

            if (enemyType == ZombieEnemyType.Armored)
            {
                // Extra armor geometry makes the reduced-damage zombie understandable before any shots land.
                CreateArmoredZombiePieces(figureRoot.transform, armorMaterial, armorTrimMaterial);
            }

            // Zombies shamble in place because level rules keep enemy roots stationary until contact or defeat.
            PrototypeHumanoidAnimator animator = zombieRoot.AddComponent<PrototypeHumanoidAnimator>();
            animator.Configure(enemyType == ZombieEnemyType.Armored ? PrototypeHumanoidAnimationStyle.ArmoredZombieShamble : PrototypeHumanoidAnimationStyle.ZombieShamble);

            return zombieRoot;
        }

        private static void CreateSurvivor(Transform squadRoot, string name, Vector3 localPosition, float scale, Material bodyMaterial, Material accentMaterial, Material skinMaterial, Material pantsMaterial, Material bootMaterial, Material gearMaterial, Material weaponMaterial)
        {
            // A per-survivor transform makes it cheap to scale and offset squad members as a formation.
            GameObject survivorRoot = new(name);
            survivorRoot.transform.SetParent(squadRoot, false);
            survivorRoot.transform.localPosition = localPosition;
            survivorRoot.transform.localRotation = Quaternion.identity;
            survivorRoot.transform.localScale = Vector3.one * scale;

            // Rounded torso and pelvis replace the old single cube while preserving a compact gameplay footprint.
            CreateSpherePart(survivorRoot.transform, "Human Torso", new Vector3(0f, 0.02f, 0f), new Vector3(0.30f, 0.50f, 0.20f), bodyMaterial, Quaternion.identity);
            CreateSpherePart(survivorRoot.transform, "Human Vest", new Vector3(0f, 0.06f, -0.09f), new Vector3(0.26f, 0.36f, 0.035f), accentMaterial, Quaternion.identity);
            CreateSpherePart(survivorRoot.transform, "Human Pelvis", new Vector3(0f, -0.30f, 0f), new Vector3(0.28f, 0.18f, 0.19f), gearMaterial, Quaternion.identity);

            // Head, helmet, and face direction make the player read as a person even at mobile scale.
            CreateSpherePart(survivorRoot.transform, "Human Head", new Vector3(0f, 0.46f, -0.02f), new Vector3(0.19f, 0.21f, 0.18f), skinMaterial, Quaternion.identity);
            CreateSpherePart(survivorRoot.transform, "Human Helmet", new Vector3(0f, 0.56f, -0.01f), new Vector3(0.21f, 0.09f, 0.19f), gearMaterial, Quaternion.identity);

            // Connected shoulder chains make arm swing pivot from the body instead of spinning around a forearm center.
            CreateHumanArm(survivorRoot.transform, "Left", -1f, bodyMaterial, skinMaterial);
            CreateHumanArm(survivorRoot.transform, "Right", 1f, bodyMaterial, skinMaterial);

            // Connected hip/knee/ankle chains remove the knee gap and make the run read as a real bent limb.
            CreateHumanLeg(survivorRoot.transform, "Left", -1f, 0.02f, pantsMaterial, bootMaterial);
            CreateHumanLeg(survivorRoot.transform, "Right", 1f, -0.02f, pantsMaterial, bootMaterial);

            // A small cylindrical rifle keeps the auto-shooter readable without imported weapon art.
            CreateCylinderPart(survivorRoot.transform, "Human Rifle", new Vector3(0.13f, 0.07f, -0.18f), new Vector3(0.04f, 0.42f, 0.04f), weaponMaterial, Quaternion.Euler(83f, 0f, 64f));
        }

        private static void CreateHumanArm(Transform survivorRoot, string sideName, float sideSign, Material sleeveMaterial, Material skinMaterial)
        {
            // Shoulder pivots make arm swing originate from the torso instead of rotating around the arm mesh center.
            Transform shoulder = CreateJoint(survivorRoot, $"Human Arm {sideName}", new Vector3(sideSign * 0.17f, 0.28f, -0.01f), Quaternion.Euler(0f, 0f, sideSign * 16f));

            // The visible upper-arm cylinder hangs from the shoulder pivot and inherits all run-cycle swing.
            CreateCylinderPart(shoulder, $"Human Upper Arm {sideName} Mesh", new Vector3(0f, -0.18f, 0f), new Vector3(0.06f, 0.36f, 0.06f), sleeveMaterial, Quaternion.identity);

            // Hands are children of the shoulder chain, so they cannot visually detach from the arm during animation.
            CreateSpherePart(shoulder, $"Human Hand {sideName}", new Vector3(0f, -0.38f, -0.015f), new Vector3(0.075f, 0.065f, 0.065f), skinMaterial, Quaternion.identity);
        }

        private static void CreateZombieArm(Transform figureRoot, string sideName, float sideSign, Material skinMaterial)
        {
            // Zombie shoulders start high and pitched forward so the idle silhouette still reaches toward the squad.
            Transform shoulder = CreateJoint(figureRoot, $"Zombie Arm {sideName}", new Vector3(sideSign * 0.24f, 0.25f, -0.05f), Quaternion.Euler(76f, 0f, sideSign * 21f));

            // A long cylinder under the shoulder creates a connected reaching arm instead of a center-spinning rod.
            CreateCylinderPart(shoulder, $"Zombie Forearm {sideName} Mesh", new Vector3(0f, -0.25f, 0f), new Vector3(0.075f, 0.50f, 0.075f), skinMaterial, Quaternion.identity);

            // The hand lives at the end of the arm chain, preserving contact as the shamble swings the shoulder.
            CreateSpherePart(shoulder, $"Zombie Hand {sideName}", new Vector3(0f, -0.52f, 0f), new Vector3(0.095f, 0.075f, 0.085f), skinMaterial, Quaternion.identity);
        }

        private static void CreateHumanLeg(Transform survivorRoot, string sideName, float sideSign, float localZ, Material pantsMaterial, Material bootMaterial)
        {
            // The hip pivot is the named leg part captured by the procedural animator.
            Transform hip = CreateJoint(survivorRoot, $"Human Leg {sideName}", new Vector3(sideSign * 0.09f, -0.30f, localZ), Quaternion.Euler(0f, 0f, sideSign * 3f));

            // Upper-leg length is explicit so the knee pivot can sit exactly at the cylinder end.
            const float upperLegLength = 0.30f;

            // Lower-leg length is explicit so the boot can sit exactly at the shin end.
            const float lowerLegLength = 0.31f;

            // The thigh mesh is centered halfway between hip and knee, preserving a continuous upper segment.
            CreateCylinderPart(hip, $"Human Thigh {sideName} Mesh", new Vector3(0f, -upperLegLength * 0.5f, 0f), new Vector3(0.078f, upperLegLength, 0.078f), pantsMaterial, Quaternion.identity);

            // The knee joint is a real child pivot, so bending the leg rotates the entire shin/boot chain.
            Transform knee = CreateJoint(hip, $"Human Knee {sideName}", new Vector3(0f, -upperLegLength, 0f), Quaternion.identity);

            // The visible knee cap sits on the pivot itself, closing the gap between thigh and shin.
            CreateSpherePart(knee, $"Human Knee Cap {sideName}", Vector3.zero, new Vector3(0.115f, 0.085f, 0.095f), pantsMaterial, Quaternion.identity);

            // The named shin pivot begins at the same knee point so tests and animation can verify zero separation.
            Transform shin = CreateJoint(knee, $"Human Shin {sideName}", Vector3.zero, Quaternion.identity);

            // The shin mesh is centered below the knee and inherits knee bend from the parent joint.
            CreateCylinderPart(shin, $"Human Shin {sideName} Mesh", new Vector3(0f, -lowerLegLength * 0.5f, 0f), new Vector3(0.068f, lowerLegLength, 0.068f), pantsMaterial, Quaternion.identity);

            // The boot is attached to the bottom of the shin chain, so the ankle stays visually connected.
            CreateSpherePart(shin, $"Human Boot {sideName}", new Vector3(0f, -lowerLegLength - 0.025f, -0.035f), new Vector3(0.135f, 0.065f, 0.22f), bootMaterial, Quaternion.Euler(7f, 0f, 0f));
        }

        private static void CreateZombieLeg(Transform figureRoot, string sideName, float sideSign, float localZ, Material pantsMaterial)
        {
            // Zombie hips are slightly wider than survivor hips to keep their shamble readable at distance.
            Transform hip = CreateJoint(figureRoot, $"Zombie Leg {sideName}", new Vector3(sideSign * 0.12f, -0.35f, localZ), Quaternion.Euler(0f, 0f, sideSign * 7f));

            // Longer zombie thighs create a hunched undead silhouette under the rounded pelvis.
            const float upperLegLength = 0.32f;

            // Longer zombie shins let the feet stay close to the road contact point.
            const float lowerLegLength = 0.34f;

            // The upper leg mesh hangs from the hip pivot and ends exactly where the knee pivot starts.
            CreateCylinderPart(hip, $"Zombie Thigh {sideName} Mesh", new Vector3(0f, -upperLegLength * 0.5f, 0f), new Vector3(0.088f, upperLegLength, 0.088f), pantsMaterial, Quaternion.identity);

            // The knee pivot is connected to the hip so the shamble cannot open a gap at the joint.
            Transform knee = CreateJoint(hip, $"Zombie Knee {sideName}", new Vector3(0f, -upperLegLength, 0f), Quaternion.identity);

            // A swollen knee cap keeps the undead joint visible from the chase camera.
            CreateSpherePart(knee, $"Zombie Knee Cap {sideName}", Vector3.zero, new Vector3(0.125f, 0.09f, 0.105f), pantsMaterial, Quaternion.identity);

            // The shin pivot starts exactly at the knee, giving the animator a connected lower limb.
            Transform shin = CreateJoint(knee, $"Zombie Shin {sideName}", Vector3.zero, Quaternion.identity);

            // The visible shin hangs from the connected knee pivot and bends as part of the same limb.
            CreateCylinderPart(shin, $"Zombie Shin {sideName} Mesh", new Vector3(0f, -lowerLegLength * 0.5f, 0f), new Vector3(0.078f, lowerLegLength, 0.078f), pantsMaterial, Quaternion.identity);

            // Wide feet remain attached to the shin end so ankle motion reads as one continuous zombie leg.
            CreateSpherePart(shin, $"Zombie Foot {sideName}", new Vector3(0f, -lowerLegLength - 0.03f, -0.05f), new Vector3(0.155f, 0.07f, 0.24f), pantsMaterial, Quaternion.Euler(5f, 0f, sideSign * 3f));
        }

        private static void CreateArmoredZombiePieces(Transform figureRoot, Material armorMaterial, Material armorTrimMaterial)
        {
            // A broad chest plate visibly explains why armored zombies reduce incoming damage.
            CreateSpherePart(figureRoot, "Zombie Armor Plate", new Vector3(0f, 0.07f, -0.145f), new Vector3(0.34f, 0.42f, 0.045f), armorMaterial, Quaternion.identity);

            // A flattened helmet gives the armored enemy a second high-contrast tell above the torso.
            CreateSpherePart(figureRoot, "Zombie Helmet", new Vector3(0f, 0.61f, -0.02f), new Vector3(0.26f, 0.10f, 0.22f), armorMaterial, Quaternion.identity);

            // Shoulder caps make the silhouette wider without turning the zombie back into a rectangle.
            CreateSpherePart(figureRoot, "Zombie Shoulder Armor Left", new Vector3(-0.25f, 0.22f, -0.02f), new Vector3(0.16f, 0.10f, 0.12f), armorMaterial, Quaternion.Euler(0f, 0f, -10f));
            CreateSpherePart(figureRoot, "Zombie Shoulder Armor Right", new Vector3(0.25f, 0.22f, -0.02f), new Vector3(0.16f, 0.10f, 0.12f), armorMaterial, Quaternion.Euler(0f, 0f, 10f));

            // A dark belt and strap help armor read as equipment rather than a flat gray patch.
            CreateCylinderPart(figureRoot, "Zombie Armor Belt", new Vector3(0f, -0.18f, -0.15f), new Vector3(0.045f, 0.34f, 0.045f), armorTrimMaterial, Quaternion.Euler(0f, 0f, 90f));
            CreateCylinderPart(figureRoot, "Zombie Armor Strap", new Vector3(-0.09f, 0.07f, -0.17f), new Vector3(0.035f, 0.46f, 0.035f), armorTrimMaterial, Quaternion.Euler(0f, 0f, -24f));
        }

        private static GameObject CreateSpherePart(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, Quaternion localRotation)
        {
            // The shared sphere mesh becomes an ellipsoid once each body part applies its local scale.
            GameObject part = PrototypeGeometryFactory.CreateSphere(name, Vector3.zero, Vector3.one, material);
            ConfigurePartTransform(part.transform, parent, localPosition, localScale, localRotation);
            return part;
        }

        private static GameObject CreateCylinderPart(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, Quaternion localRotation)
        {
            // Cylinders are used for limbs and narrow props because they avoid the blocky placeholder look.
            GameObject part = PrototypeGeometryFactory.CreateCylinder(name, Vector3.zero, Vector3.one, material);
            ConfigurePartTransform(part.transform, parent, localPosition, localScale, localRotation);
            return part;
        }

        private static void ConfigurePartTransform(Transform partTransform, Transform parent, Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
        {
            // Parent in local space so all offsets remain relative to the gameplay actor root.
            partTransform.SetParent(parent, false);

            // Local position places the body part inside the character silhouette.
            partTransform.localPosition = localPosition;

            // Local rotation gives limbs and armor a posed, non-card silhouette.
            partTransform.localRotation = localRotation;

            // Local scale stretches the shared primitive mesh into the requested body part.
            partTransform.localScale = localScale;
        }

        private static Transform CreateJoint(Transform parent, string name, Vector3 localPosition, Quaternion localRotation)
        {
            // Joints are empty transforms used as anatomical pivots for connected procedural limbs.
            GameObject joint = new(name);

            // Parent in local space so the joint chain follows the containing survivor or zombie figure.
            joint.transform.SetParent(parent, false);

            // The local position places the pivot exactly where an anatomical joint should connect segments.
            joint.transform.localPosition = localPosition;

            // The rest rotation gives limbs a natural stance before the procedural gait layers on top.
            joint.transform.localRotation = localRotation;

            // Unit scale keeps child mesh dimensions authored in their own local space.
            joint.transform.localScale = Vector3.one;

            return joint.transform;
        }

        private static Material CreateMaterial(Color color)
        {
            // Route all generated character colors through the same shader-safe material helper as the rest of gameplay.
            return PrototypeMaterialFactory.Create(color);
        }
    }
}
