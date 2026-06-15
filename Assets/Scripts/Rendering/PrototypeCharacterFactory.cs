using LaneSurvivor.Data;
using LaneSurvivor.Gameplay;
using UnityEngine;

namespace LaneSurvivor.Rendering
{
    public static class PrototypeCharacterFactory
    {
        private const float SurvivorWingScale = 1f;

        // Legacy generated scenes may still contain this old sideways cutout child, so visibility code can filter it.
        public const string SoldierReferenceVisualName = "Soldier Reference Visual";

        // The leader keeps the longest profile so the front survivor reads as the primary shooter.
        public const string LeaderRifleName = "Leader Rifle";

        // The left wing uses a chunkier shotgun silhouette to distinguish the side survivor at phone scale.
        public const string LeftWingShotgunName = "Left Wing Shotgun";

        // The right wing uses a compact SMG silhouette to complete the three-profile weapon set.
        public const string RightWingSmgName = "Right Wing SMG";

        // Weapon hold style controls whether a survivor aims from the face or from the waist.
        private enum SurvivorWeaponHoldStyle
        {
            EyeLevel,
            HipFire
        }

        private static readonly Color SurvivorSkinColor = new(0.84f, 0.62f, 0.43f);

        // Dark face-detail material gives generated eyes and mouth enough contrast at gameplay scale.
        private static readonly Color SurvivorFaceDetailColor = new(0.07f, 0.05f, 0.04f);

        // Warm beard color keeps the tiny face closer to the approved soldier reference without texture cards.
        private static readonly Color SurvivorBeardColor = new(0.20f, 0.11f, 0.065f);

        private static readonly Color SurvivorSuitColor = new(0.03f, 0.42f, 0.45f);

        private static readonly Color SurvivorPantsColor = new(0.13f, 0.20f, 0.26f);

        private static readonly Color SurvivorBootColor = new(0.035f, 0.035f, 0.04f);

        private static readonly Color SurvivorGearColor = new(0.10f, 0.14f, 0.16f);

        private static readonly Color SurvivorArmorColor = new(0.29f, 0.32f, 0.32f);

        private static readonly Color SurvivorArmorTrimColor = new(0.12f, 0.14f, 0.14f);

        private static readonly Color SurvivorGlowColor = new(0.02f, 0.90f, 1f);

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

            // Legacy palette parameters stay in the API, while the new 3D soldier uses a fixed reference-art palette.
            _ = uniformMaterial;
            _ = accentMaterial;

            // Materials are intentionally shared across the three mini survivors to keep the generated scene small.
            Material skinMaterial = CreateMaterial(SurvivorSkinColor);
            Material faceDetailMaterial = CreateMaterial(SurvivorFaceDetailColor);
            Material beardMaterial = CreateMaterial(SurvivorBeardColor);
            Material pantsMaterial = CreateMaterial(SurvivorPantsColor);
            Material bootMaterial = CreateMaterial(SurvivorBootColor);
            Material gearMaterial = CreateMaterial(SurvivorGearColor);
            Material armorMaterial = CreateMaterial(SurvivorArmorColor);
            Material armorTrimMaterial = CreateMaterial(SurvivorArmorTrimColor);
            Material glowMaterial = CreateMaterial(SurvivorGlowColor);
            Material weaponMaterial = CreateMaterial(SurvivorWeaponColor);
            Material bodyMaterial = CreateMaterial(SurvivorSuitColor);

            // A three-person wedge makes squad count feel like people without spawning one mesh per count value.
            CreateSurvivor(squadRoot.transform, "Survivor Leader", new Vector3(0f, 0f, 0.08f), 1f, LeaderRifleName, bodyMaterial, skinMaterial, faceDetailMaterial, beardMaterial, pantsMaterial, bootMaterial, gearMaterial, armorMaterial, armorTrimMaterial, glowMaterial, weaponMaterial);

            // Side survivors sit behind the leader with a wider offset so full-size soldiers do not overlap.
            CreateSurvivor(squadRoot.transform, "Survivor Left Wing", new Vector3(-0.52f, -0.02f, -0.38f), SurvivorWingScale, LeftWingShotgunName, bodyMaterial, skinMaterial, faceDetailMaterial, beardMaterial, pantsMaterial, bootMaterial, gearMaterial, armorMaterial, armorTrimMaterial, glowMaterial, weaponMaterial);

            // Mirroring the side placement gives the player a recognizably human squad silhouette in one lane.
            CreateSurvivor(squadRoot.transform, "Survivor Right Wing", new Vector3(0.52f, -0.02f, -0.38f), SurvivorWingScale, RightWingSmgName, bodyMaterial, skinMaterial, faceDetailMaterial, beardMaterial, pantsMaterial, bootMaterial, gearMaterial, armorMaterial, armorTrimMaterial, glowMaterial, weaponMaterial);

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

        private static void CreateSurvivor(Transform squadRoot, string name, Vector3 localPosition, float scale, string weaponProfileName, Material bodyMaterial, Material skinMaterial, Material faceDetailMaterial, Material beardMaterial, Material pantsMaterial, Material bootMaterial, Material gearMaterial, Material armorMaterial, Material armorTrimMaterial, Material glowMaterial, Material weaponMaterial)
        {
            // A per-survivor transform makes it cheap to scale and offset squad members as a formation.
            GameObject survivorRoot = new(name);
            survivorRoot.transform.SetParent(squadRoot, false);
            survivorRoot.transform.localPosition = localPosition;
            survivorRoot.transform.localRotation = Quaternion.identity;
            survivorRoot.transform.localScale = Vector3.one * scale;

            // Rounded torso and pelvis provide the 3D mass that can rotate toward a target.
            CreateSpherePart(survivorRoot.transform, "Human Torso", new Vector3(0f, 0.02f, 0f), new Vector3(0.30f, 0.50f, 0.20f), bodyMaterial, Quaternion.identity);
            CreateSpherePart(survivorRoot.transform, "Human Vest", new Vector3(0f, 0.06f, 0.11f), new Vector3(0.28f, 0.38f, 0.040f), armorTrimMaterial, Quaternion.identity);
            CreateSpherePart(survivorRoot.transform, "Human Pelvis", new Vector3(0f, -0.30f, 0f), new Vector3(0.28f, 0.18f, 0.19f), gearMaterial, Quaternion.identity);

            // Armor plates and light strips make the procedural 3D model read like the generated soldier concept art.
            CreateSurvivorTorsoArmor(survivorRoot.transform, armorMaterial, armorTrimMaterial, glowMaterial);

            // Head, helmet, and glow details make the player read as a tactical soldier instead of a prototype blob.
            CreateSpherePart(survivorRoot.transform, "Human Head", new Vector3(0f, 0.46f, -0.02f), new Vector3(0.19f, 0.21f, 0.18f), skinMaterial, Quaternion.identity);
            CreateSpherePart(survivorRoot.transform, "Human Hood Collar", new Vector3(0f, 0.36f, -0.05f), new Vector3(0.26f, 0.12f, 0.20f), bodyMaterial, Quaternion.identity);
            CreateSpherePart(survivorRoot.transform, "Human Helmet", new Vector3(0f, 0.56f, -0.01f), new Vector3(0.22f, 0.10f, 0.20f), armorTrimMaterial, Quaternion.identity);
            CreateSurvivorFaceDetails(survivorRoot.transform, skinMaterial, faceDetailMaterial, beardMaterial);
            CreateSurvivorHelmetDetails(survivorRoot.transform, armorMaterial, glowMaterial);

            // The weapon profile decides the authored firing pose before any procedural walk animation runs.
            SurvivorWeaponHoldStyle holdStyle = GetWeaponHoldStyle(weaponProfileName);

            // Connected shoulder chains make arm swing pivot from the body instead of spinning around a forearm center.
            CreateHumanArm(survivorRoot.transform, "Left", -1f, holdStyle, bodyMaterial, gearMaterial, armorMaterial, glowMaterial);

            // The weapon is parented under the right hand so muzzle anchors follow the procedural arm animation.
            Transform weaponHand = CreateHumanArm(survivorRoot.transform, "Right", 1f, holdStyle, bodyMaterial, gearMaterial, armorMaterial, glowMaterial);

            // Connected hip/knee/ankle chains remove the knee gap and make the run read as a real bent limb.
            CreateHumanLeg(survivorRoot.transform, "Left", -1f, 0.02f, pantsMaterial, bootMaterial, armorMaterial, armorTrimMaterial, glowMaterial);
            CreateHumanLeg(survivorRoot.transform, "Right", 1f, -0.02f, pantsMaterial, bootMaterial, armorMaterial, armorTrimMaterial, glowMaterial);

            // Procedural weapon profiles keep the squad readable without importing any firearm art.
            CreateSurvivorWeapon(weaponHand, weaponProfileName, holdStyle, weaponMaterial, armorMaterial, glowMaterial);
        }

        private static void CreateSurvivorTorsoArmor(Transform survivorRoot, Material armorMaterial, Material armorTrimMaterial, Material glowMaterial)
        {
            // Chest armor gives the 3D body a strong tactical front instead of a simple colored oval.
            CreateCubePart(survivorRoot, "Human Chest Armor", new Vector3(0f, 0.12f, 0.155f), new Vector3(0.34f, 0.34f, 0.045f), armorMaterial, Quaternion.identity);

            // A darker center panel echoes the layered vest in the approved soldier concept art.
            CreateCubePart(survivorRoot, "Human Chest Center Plate", new Vector3(0f, 0.02f, 0.185f), new Vector3(0.16f, 0.44f, 0.035f), armorTrimMaterial, Quaternion.identity);

            // Thin zipper rails break up the teal torso like the reference soldier's layered vest panels.
            CreateCubePart(survivorRoot, "Human Chest Left Rail", new Vector3(-0.085f, 0.02f, 0.210f), new Vector3(0.018f, 0.42f, 0.018f), armorMaterial, Quaternion.identity);
            CreateCubePart(survivorRoot, "Human Chest Right Rail", new Vector3(0.085f, 0.02f, 0.210f), new Vector3(0.018f, 0.42f, 0.018f), armorMaterial, Quaternion.identity);

            // Cyan chest light makes the soldier readable at small scale and matches the concept glow language.
            CreateCubePart(survivorRoot, "Human Chest Glow", new Vector3(0f, 0.20f, 0.215f), new Vector3(0.16f, 0.035f, 0.020f), glowMaterial, Quaternion.identity);

            // A compact backpack gives the rear chase view the same equipment-heavy silhouette as the cutout art.
            CreateCubePart(survivorRoot, "Human Backpack", new Vector3(0f, 0.08f, -0.29f), new Vector3(0.28f, 0.56f, 0.11f), armorTrimMaterial, Quaternion.identity);

            // A vertical backpack light keeps the rear view visually connected to the cyan suit highlights.
            CreateCubePart(survivorRoot, "Human Backpack Glow", new Vector3(0f, 0.12f, -0.36f), new Vector3(0.055f, 0.30f, 0.025f), glowMaterial, Quaternion.identity);

            // A raised antenna keeps the rotated back view from looking like the old plain body.
            CreateCylinderPart(survivorRoot, "Human Backpack Antenna", new Vector3(-0.12f, 0.50f, -0.35f), new Vector3(0.012f, 0.24f, 0.012f), armorTrimMaterial, Quaternion.identity);

            // Belt pouches build the chunky utility silhouette visible in the concept without adding colliders.
            CreateCubePart(survivorRoot, "Human Belt Pouch Left", new Vector3(-0.18f, -0.23f, 0.17f), new Vector3(0.10f, 0.13f, 0.07f), armorTrimMaterial, Quaternion.Euler(0f, 0f, 5f));
            CreateCubePart(survivorRoot, "Human Belt Pouch Right", new Vector3(0.18f, -0.23f, 0.17f), new Vector3(0.10f, 0.13f, 0.07f), armorTrimMaterial, Quaternion.Euler(0f, 0f, -5f));

            // The center buckle gives the front-facing model the same tactical belt focal point as the reference.
            CreateCubePart(survivorRoot, "Human Belt Buckle", new Vector3(0f, -0.225f, 0.205f), new Vector3(0.13f, 0.070f, 0.028f), armorMaterial, Quaternion.identity);
        }

        private static void CreateSurvivorFaceDetails(Transform survivorRoot, Material skinMaterial, Material faceDetailMaterial, Material beardMaterial)
        {
            // Dark brows and eyes give the head a forward-facing read when the 3D model rotates toward camera.
            CreateSpherePart(survivorRoot, "Human Eye Left", new Vector3(-0.060f, 0.490f, 0.190f), new Vector3(0.022f, 0.014f, 0.010f), faceDetailMaterial, Quaternion.identity);
            CreateSpherePart(survivorRoot, "Human Eye Right", new Vector3(0.060f, 0.490f, 0.190f), new Vector3(0.022f, 0.014f, 0.010f), faceDetailMaterial, Quaternion.identity);

            // A small raised nose keeps the face from flattening into the head ellipsoid on side rotations.
            CreateSpherePart(survivorRoot, "Human Nose", new Vector3(0f, 0.455f, 0.205f), new Vector3(0.030f, 0.040f, 0.020f), skinMaterial, Quaternion.Euler(0f, 0f, 2f));

            // The beard and jaw shadow echo the approved art's human face without needing a texture card.
            CreateSpherePart(survivorRoot, "Human Beard", new Vector3(0f, 0.395f, 0.175f), new Vector3(0.105f, 0.050f, 0.020f), beardMaterial, Quaternion.identity);
            CreateCubePart(survivorRoot, "Human Mouth Shadow", new Vector3(0f, 0.410f, 0.205f), new Vector3(0.080f, 0.012f, 0.008f), faceDetailMaterial, Quaternion.identity);
        }

        private static void CreateSurvivorHelmetDetails(Transform survivorRoot, Material armorMaterial, Material glowMaterial)
        {
            // The brim/cap plate helps the helmet read like the new soldier art from the chase camera.
            CreateCubePart(survivorRoot, "Human Helmet Brim", new Vector3(0f, 0.56f, 0.18f), new Vector3(0.28f, 0.035f, 0.12f), armorMaterial, Quaternion.Euler(6f, 0f, 0f));

            // A front cyan visor strip gives the small head a high-tech focal point.
            CreateCubePart(survivorRoot, "Human Helmet Glow", new Vector3(0f, 0.61f, 0.20f), new Vector3(0.13f, 0.025f, 0.018f), glowMaterial, Quaternion.identity);

            // Raised cap seams make the helmet read as a dimensional object rather than a smooth ball.
            CreateCubePart(survivorRoot, "Human Helmet Top Seam", new Vector3(0f, 0.645f, -0.020f), new Vector3(0.030f, 0.020f, 0.24f), armorMaterial, Quaternion.identity);
            CreateCubePart(survivorRoot, "Human Helmet Rear Plate", new Vector3(0f, 0.565f, -0.155f), new Vector3(0.19f, 0.060f, 0.030f), armorMaterial, Quaternion.identity);

            // Side headset discs make the silhouette closer to the concept-art helmet.
            CreateSpherePart(survivorRoot, "Human Headset Left", new Vector3(-0.19f, 0.52f, -0.03f), new Vector3(0.055f, 0.075f, 0.045f), armorMaterial, Quaternion.identity);
            CreateSpherePart(survivorRoot, "Human Headset Right", new Vector3(0.19f, 0.52f, -0.03f), new Vector3(0.055f, 0.075f, 0.045f), armorMaterial, Quaternion.identity);

            // A short mic boom makes the side profile read as the headset from the concept image.
            CreateCylinderBetween(survivorRoot, "Human Headset Mic", new Vector3(-0.18f, 0.48f, 0.08f), new Vector3(-0.08f, 0.44f, 0.17f), 0.009f, armorMaterial);

            // Cyan headset lights keep side rotations from losing the reference-art glow motif.
            CreateCubePart(survivorRoot, "Human Headset Glow Left", new Vector3(-0.22f, 0.53f, 0.055f), new Vector3(0.016f, 0.045f, 0.030f), glowMaterial, Quaternion.identity);
            CreateCubePart(survivorRoot, "Human Headset Glow Right", new Vector3(0.22f, 0.53f, 0.055f), new Vector3(0.016f, 0.045f, 0.030f), glowMaterial, Quaternion.identity);
        }

        private static Transform CreateHumanArm(Transform survivorRoot, string sideName, float sideSign, SurvivorWeaponHoldStyle holdStyle, Material sleeveMaterial, Material gloveMaterial, Material armorMaterial, Material glowMaterial)
        {
            // Shoulder pivots make arm swing originate from the torso instead of rotating around the arm mesh center.
            Transform shoulder = CreateJoint(survivorRoot, $"Human Arm {sideName}", new Vector3(sideSign * 0.17f, 0.36f, -0.01f), GetHumanShoulderRestRotation(sideSign, holdStyle));

            // The hand target separates shoulder-fired weapons from lower hip-fire weapons.
            Vector3 handLocalPosition = GetHumanHandLocalPosition(sideSign, holdStyle);

            // The visible upper arm spans from shoulder toward the raised hand instead of hanging at the side.
            CreateCylinderBetween(shoulder, $"Human Upper Arm {sideName} Mesh", Vector3.zero, handLocalPosition, 0.06f, sleeveMaterial);

            // A shoulder armor pad creates the bulky plated silhouette from the new soldier model.
            CreateSpherePart(shoulder, $"Human Shoulder Armor {sideName}", new Vector3(sideSign * 0.015f, 0.01f, 0.035f), new Vector3(0.13f, 0.085f, 0.10f), armorMaterial, Quaternion.identity);

            // A cyan strip on each shoulder keeps the squad distinct against the road.
            CreateCubePart(shoulder, $"Human Shoulder Glow {sideName}", new Vector3(sideSign * 0.040f, 0.025f, 0.105f), new Vector3(0.075f, 0.022f, 0.018f), glowMaterial, Quaternion.Euler(0f, 0f, sideSign * 8f));

            // Forearm guards preserve the soldier-art armored sleeve feel while the arm chain rotates in 3D.
            CreateCylinderBetween(shoulder, $"Human Forearm Armor {sideName}", handLocalPosition * 0.52f, handLocalPosition * 0.82f, 0.075f, armorMaterial);

            // A small forearm light reads clearly in the same cyan accent color as the concept art.
            CreateCubePart(shoulder, $"Human Forearm Glow {sideName}", handLocalPosition * 0.70f + new Vector3(0f, 0.012f, 0.045f), new Vector3(0.035f, 0.12f, 0.018f), glowMaterial, Quaternion.FromToRotation(Vector3.up, handLocalPosition.normalized));

            // The hand is a unit-scale joint so attached weapons are not distorted by the hand mesh scale.
            Transform hand = CreateJoint(shoulder, $"Human Hand {sideName}", handLocalPosition, Quaternion.identity);

            // The visible glove mesh stays on the joint while child weapons inherit a clean transform.
            CreateSpherePart(hand, $"Human Hand {sideName} Mesh", Vector3.zero, new Vector3(0.075f, 0.065f, 0.065f), gloveMaterial, Quaternion.identity);

            return hand;
        }

        private static void CreateSurvivorWeapon(Transform hand, string weaponProfileName, SurvivorWeaponHoldStyle holdStyle, Material weaponMaterial, Material armorMaterial, Material glowMaterial)
        {
            // A profile root makes the whole weapon easy for tests, animation, and muzzle lookup to reason about.
            GameObject weaponRoot = new(weaponProfileName);

            // Parenting under the hand chain makes the weapon follow arm swing and keeps the muzzle anchor animated.
            weaponRoot.transform.SetParent(hand, false);

            // The root offset keeps the grip readable while preserving the selected hold height.
            weaponRoot.transform.localPosition = GetWeaponRootLocalPosition(holdStyle);

            // Weapon child pieces are authored in hand-local space with their barrels pointing down-lane on +Z.
            weaponRoot.transform.localRotation = GetWeaponRootLocalRotation(holdStyle);

            // Unit scale preserves the distinct profile proportions below even on smaller wing survivors.
            weaponRoot.transform.localScale = Vector3.one;

            switch (weaponProfileName)
            {
                case LeaderRifleName:
                    CreateLeaderRifle(weaponRoot.transform, weaponMaterial, armorMaterial, glowMaterial);
                    break;
                case LeftWingShotgunName:
                    CreateLeftWingShotgun(weaponRoot.transform, weaponMaterial, glowMaterial);
                    break;
                case RightWingSmgName:
                    CreateRightWingSmg(weaponRoot.transform, weaponMaterial, glowMaterial);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(weaponProfileName), weaponProfileName, "Unsupported survivor weapon profile.");
            }
        }

        private static SurvivorWeaponHoldStyle GetWeaponHoldStyle(string weaponProfileName)
        {
            switch (weaponProfileName)
            {
                case LeaderRifleName:
                case LeftWingShotgunName:
                    // Long weapons are shoulder-fired so their muzzles stay near the survivor's face.
                    return SurvivorWeaponHoldStyle.EyeLevel;
                case RightWingSmgName:
                    // The compact SMG gives the squad a second read by firing from the hip.
                    return SurvivorWeaponHoldStyle.HipFire;
                default:
                    // Unsupported profiles should fail close to the source of the bad generated hierarchy.
                    throw new System.ArgumentOutOfRangeException(nameof(weaponProfileName), weaponProfileName, "Unsupported survivor weapon hold style.");
            }
        }

        private static Vector3 GetHumanHandLocalPosition(float sideSign, SurvivorWeaponHoldStyle holdStyle)
        {
            // Negative side values are the support hand on the survivor's left side.
            bool isLeftHand = sideSign < 0f;

            switch (holdStyle)
            {
                case SurvivorWeaponHoldStyle.EyeLevel:
                    // Eye-level support hands cross toward the weapon fore-end instead of swinging at the side.
                    return isLeftHand ? new Vector3(0.18f, 0.02f, 0.34f) : new Vector3(-0.03f, 0.09f, 0.31f);
                case SurvivorWeaponHoldStyle.HipFire:
                    // Hip-fire hands stay lower around the waist while still pointing the barrel down-lane.
                    return isLeftHand ? new Vector3(0.17f, -0.21f, 0.33f) : new Vector3(-0.02f, -0.18f, 0.27f);
                default:
                    // New hold styles should define exact hand targets rather than falling back to a generic pose.
                    throw new System.ArgumentOutOfRangeException(nameof(holdStyle), holdStyle, "Unsupported survivor weapon hold style.");
            }
        }

        private static Quaternion GetHumanShoulderRestRotation(float sideSign, SurvivorWeaponHoldStyle holdStyle)
        {
            switch (holdStyle)
            {
                case SurvivorWeaponHoldStyle.EyeLevel:
                    // A small outward roll keeps shoulder-fired arms from collapsing into the chest.
                    return Quaternion.Euler(0f, 0f, sideSign * 5f);
                case SurvivorWeaponHoldStyle.HipFire:
                    // Hip-fire arms need less roll because the hands already sit lower and wider.
                    return Quaternion.Euler(0f, 0f, sideSign * 3f);
                default:
                    // Unknown styles should not silently reuse an unrelated shoulder pose.
                    throw new System.ArgumentOutOfRangeException(nameof(holdStyle), holdStyle, "Unsupported survivor weapon hold style.");
            }
        }

        private static Vector3 GetWeaponRootLocalPosition(SurvivorWeaponHoldStyle holdStyle)
        {
            switch (holdStyle)
            {
                case SurvivorWeaponHoldStyle.EyeLevel:
                    // Shoulder-fired weapons sit close to the hand so the barrel stays near eye height.
                    return new Vector3(0f, -0.015f, 0.035f);
                case SurvivorWeaponHoldStyle.HipFire:
                    // Hip-fire weapons sit a touch lower and farther forward to clear the waist silhouette.
                    return new Vector3(0f, -0.025f, 0.045f);
                default:
                    // New styles should provide their own authored root offset.
                    throw new System.ArgumentOutOfRangeException(nameof(holdStyle), holdStyle, "Unsupported survivor weapon hold style.");
            }
        }

        private static Quaternion GetWeaponRootLocalRotation(SurvivorWeaponHoldStyle holdStyle)
        {
            switch (holdStyle)
            {
                case SurvivorWeaponHoldStyle.EyeLevel:
                    // Eye-level weapons point straight down-lane so tracer origins line up with sighted shots.
                    return Quaternion.identity;
                case SurvivorWeaponHoldStyle.HipFire:
                    // Hip-fire weapons stay mostly level; a tiny upward pitch keeps the muzzle visible above the road.
                    return Quaternion.Euler(-2f, 0f, 0f);
                default:
                    // New styles should declare an explicit local weapon rotation.
                    throw new System.ArgumentOutOfRangeException(nameof(holdStyle), holdStyle, "Unsupported survivor weapon hold style.");
            }
        }

        private static void CreateLeaderRifle(Transform weaponRoot, Material weaponMaterial, Material armorMaterial, Material glowMaterial)
        {
            // The rifle body is long and narrow so the leader reads as the precision shooter.
            CreateCylinderPart(weaponRoot, "Leader Rifle Body", new Vector3(0f, 0f, 0.24f), new Vector3(0.055f, 0.42f, 0.055f), weaponMaterial, Quaternion.Euler(90f, 0f, 0f));

            // A thinner forward barrel extends beyond the body and defines the muzzle anchor position.
            CreateCylinderPart(weaponRoot, "Leader Rifle Barrel", new Vector3(0f, 0f, 0.58f), new Vector3(0.028f, 0.42f, 0.028f), weaponMaterial, Quaternion.Euler(90f, 0f, 0f));

            // A boxy receiver makes the weapon feel closer to the approved futuristic rifle art.
            CreateCubePart(weaponRoot, "Leader Rifle Receiver", new Vector3(0f, 0.018f, 0.26f), new Vector3(0.13f, 0.09f, 0.34f), armorMaterial, Quaternion.identity);

            // A cyan side strip gives the rifle a readable sci-fi accent from the chase camera.
            CreateCubePart(weaponRoot, "Leader Rifle Glow Strip", new Vector3(0f, 0.075f, 0.34f), new Vector3(0.105f, 0.020f, 0.20f), glowMaterial, Quaternion.identity);

            // The rear stock gives the rifle a shoulder-fired silhouette without imported art.
            CreateCylinderPart(weaponRoot, "Leader Rifle Stock", new Vector3(0f, 0f, -0.08f), new Vector3(0.050f, 0.24f, 0.050f), weaponMaterial, Quaternion.Euler(90f, 0f, 0f));

            // A small vertical grip visually connects the weapon to the hand joint.
            CreateCylinderPart(weaponRoot, "Leader Rifle Grip", new Vector3(0f, -0.09f, 0.15f), new Vector3(0.035f, 0.18f, 0.035f), weaponMaterial, Quaternion.identity);

            // The sight block creates a recognizable top-mounted optic that rotates with the 3D weapon.
            CreateCubePart(weaponRoot, "Leader Rifle Sight", new Vector3(0f, 0.105f, 0.39f), new Vector3(0.10f, 0.08f, 0.11f), armorMaterial, Quaternion.identity);

            // A glowing sight lens gives shots a clear forward aiming cue.
            CreateCubePart(weaponRoot, "Leader Rifle Sight Glow", new Vector3(0f, 0.155f, 0.39f), new Vector3(0.055f, 0.018f, 0.055f), glowMaterial, Quaternion.identity);

            // The muzzle anchor sits at the barrel tip so tracer origins match the visible rifle.
            CreateWeaponMuzzleAnchor(weaponRoot, 0.79f);
        }

        private static void CreateLeftWingShotgun(Transform weaponRoot, Material weaponMaterial, Material glowMaterial)
        {
            // A chunkier receiver distinguishes the shotgun from the leader's long rifle.
            CreateCylinderPart(weaponRoot, "Shotgun Receiver", new Vector3(0f, 0f, 0.20f), new Vector3(0.070f, 0.32f, 0.070f), weaponMaterial, Quaternion.Euler(90f, 0f, 0f));

            // Twin side-by-side barrels give the left survivor a broad shotgun silhouette.
            CreateCylinderPart(weaponRoot, "Shotgun Barrel Left", new Vector3(-0.032f, 0f, 0.54f), new Vector3(0.028f, 0.48f, 0.028f), weaponMaterial, Quaternion.Euler(90f, 0f, 0f));
            CreateCylinderPart(weaponRoot, "Shotgun Barrel Right", new Vector3(0.032f, 0f, 0.54f), new Vector3(0.028f, 0.48f, 0.028f), weaponMaterial, Quaternion.Euler(90f, 0f, 0f));

            // The fore-end under the barrels gives the generated shotgun a pump-like read.
            CreateCylinderPart(weaponRoot, "Shotgun Fore End", new Vector3(0f, -0.04f, 0.45f), new Vector3(0.045f, 0.28f, 0.045f), weaponMaterial, Quaternion.Euler(90f, 0f, 0f));

            // A short stock keeps the side survivor weapon compact inside one lane.
            CreateCylinderPart(weaponRoot, "Shotgun Stock", new Vector3(0f, 0f, -0.06f), new Vector3(0.060f, 0.20f, 0.060f), weaponMaterial, Quaternion.Euler(90f, 0f, 0f));

            // A top glow strip makes the shotgun share the same visual tech language as the rifle.
            CreateCubePart(weaponRoot, "Shotgun Glow Strip", new Vector3(0f, 0.068f, 0.40f), new Vector3(0.11f, 0.018f, 0.18f), glowMaterial, Quaternion.identity);

            // The muzzle anchor is centered between the two visible barrel tips.
            CreateWeaponMuzzleAnchor(weaponRoot, 0.78f);
        }

        private static void CreateRightWingSmg(Transform weaponRoot, Material weaponMaterial, Material glowMaterial)
        {
            // The SMG body is short and tall so it reads differently from rifle and shotgun profiles.
            CreateSpherePart(weaponRoot, "SMG Receiver", new Vector3(0f, 0f, 0.18f), new Vector3(0.085f, 0.075f, 0.18f), weaponMaterial, Quaternion.identity);

            // A short barrel makes the right-wing weapon compact while still providing a real muzzle tip.
            CreateCylinderPart(weaponRoot, "SMG Barrel", new Vector3(0f, 0f, 0.42f), new Vector3(0.026f, 0.26f, 0.026f), weaponMaterial, Quaternion.Euler(90f, 0f, 0f));

            // A vertical magazine gives the small weapon its most recognizable shape at mobile scale.
            CreateCylinderPart(weaponRoot, "SMG Magazine", new Vector3(0f, -0.14f, 0.14f), new Vector3(0.040f, 0.22f, 0.040f), weaponMaterial, Quaternion.identity);

            // A tiny rear stock keeps the profile from looking like just another barrel.
            CreateCylinderPart(weaponRoot, "SMG Stock", new Vector3(0f, 0f, -0.03f), new Vector3(0.045f, 0.16f, 0.045f), weaponMaterial, Quaternion.Euler(90f, 0f, 0f));

            // A compact cyan side light makes the smaller weapon readable beside the larger long guns.
            CreateCubePart(weaponRoot, "SMG Glow Strip", new Vector3(0f, 0.065f, 0.20f), new Vector3(0.095f, 0.018f, 0.11f), glowMaterial, Quaternion.identity);

            // The muzzle anchor sits on the shorter SMG barrel tip.
            CreateWeaponMuzzleAnchor(weaponRoot, 0.55f);
        }

        private static Transform CreateWeaponMuzzleAnchor(Transform weaponRoot, float muzzleLocalZ)
        {
            // Empty anchors give gameplay an exact world-space muzzle without adding extra render geometry.
            GameObject muzzleObject = new(PlayerSquad.WeaponMuzzleAnchorName);

            // The anchor is a direct weapon child, so tests can verify it belongs to the corresponding profile.
            muzzleObject.transform.SetParent(weaponRoot, false);

            // Barrels point down-lane on local +Z, making the tip position easy to audit.
            muzzleObject.transform.localPosition = new Vector3(0f, 0f, muzzleLocalZ);

            // Identity rotation lets future projectile effects inherit the weapon's authored aim directly.
            muzzleObject.transform.localRotation = Quaternion.identity;

            // Unit scale prevents parented effects from inheriting any accidental marker sizing.
            muzzleObject.transform.localScale = Vector3.one;

            return muzzleObject.transform;
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

        private static void CreateHumanLeg(Transform survivorRoot, string sideName, float sideSign, float localZ, Material pantsMaterial, Material bootMaterial, Material armorMaterial, Material armorTrimMaterial, Material glowMaterial)
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

            // Raised knee armor matches the concept-art hard plates and remains attached while the knee bends.
            CreateSpherePart(knee, $"Human Knee Armor {sideName}", new Vector3(0f, 0.005f, 0.055f), new Vector3(0.13f, 0.06f, 0.06f), armorMaterial, Quaternion.identity);

            // A small cyan knee light keeps running legs visible against the dark track.
            CreateCubePart(knee, $"Human Knee Glow {sideName}", new Vector3(0f, 0.020f, 0.105f), new Vector3(0.050f, 0.018f, 0.014f), glowMaterial, Quaternion.identity);

            // The named shin pivot begins at the same knee point so tests and animation can verify zero separation.
            Transform shin = CreateJoint(knee, $"Human Shin {sideName}", Vector3.zero, Quaternion.identity);

            // The shin mesh is centered below the knee and inherits knee bend from the parent joint.
            CreateCylinderPart(shin, $"Human Shin {sideName} Mesh", new Vector3(0f, -lowerLegLength * 0.5f, 0f), new Vector3(0.068f, lowerLegLength, 0.068f), pantsMaterial, Quaternion.identity);

            // Shin armor gives each leg a chunkier silhouette like the reference soldier boots and guards.
            CreateCylinderPart(shin, $"Human Shin Armor {sideName}", new Vector3(0f, -lowerLegLength * 0.45f, 0.040f), new Vector3(0.082f, lowerLegLength * 0.55f, 0.058f), armorMaterial, Quaternion.identity);

            // A thigh strap and side holster echo the reference soldier's utility gear and add side-view depth.
            CreateCubePart(hip, $"Human Thigh Strap {sideName}", new Vector3(sideSign * 0.012f, -upperLegLength * 0.48f, 0.075f), new Vector3(0.14f, 0.030f, 0.035f), armorMaterial, Quaternion.Euler(0f, 0f, sideSign * 3f));
            CreateCubePart(hip, $"Human Thigh Holster {sideName}", new Vector3(sideSign * 0.075f, -upperLegLength * 0.58f, 0.030f), new Vector3(0.055f, 0.17f, 0.075f), armorTrimMaterial, Quaternion.Euler(0f, 0f, sideSign * 5f));
            CreateCubePart(hip, $"Human Thigh Glow {sideName}", new Vector3(sideSign * 0.098f, -upperLegLength * 0.58f, 0.075f), new Vector3(0.018f, 0.095f, 0.014f), glowMaterial, Quaternion.Euler(0f, 0f, sideSign * 5f));

            // The boot is attached to the bottom of the shin chain, so the ankle stays visually connected.
            CreateSpherePart(shin, $"Human Boot {sideName}", new Vector3(0f, -lowerLegLength - 0.025f, 0.035f), new Vector3(0.135f, 0.065f, 0.22f), bootMaterial, Quaternion.Euler(-7f, 0f, 0f));

            // A toe cap makes the boot feel armored rather than like a simple foot oval.
            CreateCubePart(shin, $"Human Boot Armor {sideName}", new Vector3(0f, -lowerLegLength - 0.020f, 0.16f), new Vector3(0.15f, 0.040f, 0.12f), armorMaterial, Quaternion.Euler(-7f, 0f, 0f));

            // Cyan boot lights reproduce the reference-art luminous boot accents at gameplay scale.
            CreateCubePart(shin, $"Human Boot Glow {sideName}", new Vector3(0f, -lowerLegLength + 0.015f, 0.205f), new Vector3(0.075f, 0.018f, 0.018f), glowMaterial, Quaternion.Euler(-7f, 0f, 0f));
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

        private static GameObject CreateCylinderBetween(Transform parent, string name, Vector3 localStart, Vector3 localEnd, float radius, Material material)
        {
            // Segment math lets raised arms connect shoulder to hand without guessing cylinder rotations.
            Vector3 segment = localEnd - localStart;

            // The mesh is centered between both endpoints so the cylinder spans the exact authored limb segment.
            Vector3 midpoint = (localStart + localEnd) * 0.5f;

            // Very short segments would make FromToRotation unstable, so keep an inert fallback rotation.
            Quaternion rotation = segment.sqrMagnitude > 0.0001f ? Quaternion.FromToRotation(Vector3.up, segment.normalized) : Quaternion.identity;

            // Unity cylinder meshes run along local Y, so scale.y becomes the segment length.
            Vector3 scale = new(radius, Mathf.Max(segment.magnitude, 0.001f), radius);

            return CreateCylinderPart(parent, name, midpoint, scale, material, rotation);
        }

        private static GameObject CreateCubePart(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, Quaternion localRotation)
        {
            // Cubes work well for armor plates, lights, pouches, and weapon receivers with crisp hard edges.
            GameObject part = PrototypeGeometryFactory.CreateCube(name, Vector3.zero, Vector3.one, material);
            ConfigurePartTransform(part.transform, parent, localPosition, localScale, localRotation);
            return part;
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
