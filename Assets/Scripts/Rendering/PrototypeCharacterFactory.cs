using System.Collections.Generic;
using System.Linq;
using LaneSurvivor.Data;
using LaneSurvivor.Gameplay;
using UnityEngine;
using UnityEngine.Rendering;

namespace LaneSurvivor.Rendering
{
    public static class PrototypeCharacterFactory
    {
        private const float SurvivorWingScale = 1f;

        // Old generated scenes may still contain this flat child, so visibility code can suppress it.
        public const string SoldierReferenceVisualName = "Soldier Reference Visual";

        // The approved front decal is skinned onto generated walk joints and hidden from rear camera views.
        public const string FemaleReferenceUpperName = "Female Survivor Model";

        // The rear decal uses the same approved silhouette but a dark back-facing tint for chase-camera views.
        public const string FemaleReferenceRearName = "Female Survivor Rear Model";

        // Legacy tests and scene audits use this name to ensure old detached left-leg cards are absent.
        public const string FemaleReferenceLeftLegName = "Female Survivor Left Leg Model";

        // Legacy tests and scene audits use this name to ensure old detached right-leg cards are absent.
        public const string FemaleReferenceRightLegName = "Female Survivor Right Leg Model";

        // Legacy tests and scene audits use this name to ensure old detached rifle cards are absent.
        public const string FemaleReferenceRifleName = "Female Survivor Rifle Model";

        // Every visible squad member uses this same rifle silhouette so the soldiers read as one uniform model.
        public const string LeaderRifleName = "Leader Rifle";

        // The technical-trial survivor is kept under a stable child name for animation, tests, and scene audits.
        public const string SwatSurvivorModelName = "SWAT Survivor 3D Model";

        // Zombies reuse the same licensed mesh under a distinct name so combat tests can resolve visible enemies exactly.
        public const string SwatZombieModelName = "SWAT Zombie 3D Model";

        // Stable attachment names let gait animation, tests, and frame diagnostics resolve the enlarged infected eyes.
        public const string SwatZombieBloodyEyeLeftName = "Zombie Bloody Eye Left";
        public const string SwatZombieBloodyEyeRightName = "Zombie Bloody Eye Right";

        // Drool uses separate strand and drop objects so the strand can sway while the heavy drop follows the jaw.
        public const string SwatZombieDroolStrandName = "Zombie Drool Strand";
        public const string SwatZombieDroolDropName = "Zombie Drool Drop";

        // Wound names make the two body-region infection cues independently testable.
        public const string SwatZombieChestWoundName = "Zombie Chest Wound";
        public const string SwatZombieHeadWoundName = "Zombie Head Wound";

        // The imported weapon-body bone directly skins the visible rifle meshes and therefore owns target aiming.
        public const string SwatWeaponAimPivotName = "b_Body";

        // This imported barrel-end bone provides the exact visible location for muzzle flashes and tracers.
        public const string SwatWeaponSourceMuzzleName = "SM_WP_Muzzle_011_01";

        // This imported barrel joint sits behind the muzzle and defines the visible bore independently of the MPX pivot.
        public const string SwatWeaponBarrelBaseName = "s_Barrel";

        // This imported stock joint is the visible rear endpoint used with the muzzle to define the complete rifle axis.
        public const string SwatWeaponStockBoneName = "s_Stock";

        // This skinned mesh is the visible front of the MPX and provides the rendered rifle-axis endpoint.
        public const string SwatWeaponMuzzleRendererName = "SM_WP_Muzzle_011_01_2";

        // Vertices within one millimetre of the furthest muzzle plane define the visible barrel opening centre.
        public const float SwatWeaponBarrelTipPlaneTolerance = 0.001f;

        // This skinned mesh is the visible rear of the MPX and provides the rendered rifle-axis origin.
        public const string SwatWeaponStockRendererName = "SKM_WP_Stock_027_01";

        // Runtime-baked weapon renderers use this suffix so tests can distinguish them from hidden FBX skins.
        public const string SwatWeaponRigidRendererSuffix = " Rigid Aim Proxy";

        // The obsolete generated leader marker stays available for hierarchy tests but is excluded from shot rotation.
        public const string HiddenGeneratedWeaponMuzzleName = "Hidden Generated Weapon Muzzle";

        // Resources keeps the licensed FBX available to both editor-built and runtime-bootstrapped scenes.
        private const string SwatSurvivorResourcePath = "Survivor3D/SWAT_Survivor_Mobile";

        // The generated controller blends the authored rifle idle and run actions based on gameplay-root movement.
        private const string SwatSurvivorControllerResourcePath = "Survivor3D/SWAT_Survivor_Controller";

        // The calmer tracked Mixamo walk supplies grounded foot exchange beneath the procedural drunk stumble.
        private const string SwatZombieWalkResourcePath = "Survivor3D/Animations/Mixamo_Rifle_Walk";

        // Resources loading prevents the runtime-only tattered suit shader from being stripped from iOS player builds.
        private const string SwatZombieTatteredClothingShaderResourcePath = "Survivor3D/SWAT_Zombie_Tattered_Clothing";

        // The imported character is 1.8 metres tall, so this scale matches the existing 1.58-metre prototype rig.
        private const float SwatSurvivorScale = 0.88f;

        // The downloaded FBX places its feet at its origin; this offset aligns them with the generated boot joints.
        private const float SwatSurvivorYOffset = -0.91f;

        // Enemy roots sit below the squad root, so this compensation keeps the reused boots on the same road plane.
        private const float SwatZombieYOffset = SwatSurvivorYOffset + GameplayVisuals.PlayerCenterY - GameplayVisuals.ZombieCenterY;

        // One shared runtime override keeps every zombie on the same walk asset without duplicating controller assets.
        private static AnimatorOverrideController swatZombieRuntimeController;

        // Runtime zombie PBR materials are immutable, so sharing them preserves batching and avoids per-enemy native allocations.
        private static readonly Dictionary<string, Material> SwatZombieSharedPbrMaterials = new();

        // Small infection props likewise share one palette across every basic and armored zombie instance.
        private static Material swatZombieSharedBloodMaterial;
        private static Material swatZombieSharedEyeMaterial;
        private static Material swatZombieSharedPupilMaterial;
        private static Material swatZombieSharedDroolMaterial;

        // Legacy scenes may still contain this alternate weapon profile, but new squads use the shared rifle.
        public const string LeftWingShotgunName = "Left Wing Shotgun";

        // Legacy scenes may still contain this compact profile, but new squads use the shared rifle.
        public const string RightWingSmgName = "Right Wing SMG";

        // Weapon hold style controls whether a survivor aims from the face or from the waist.
        private enum SurvivorWeaponHoldStyle
        {
            EyeLevel,
            HipFire
        }

        private const string FemaleSurvivorReferenceResourcePath = "Survivor/FemaleSurvivorReferenceCutout";

        // Tests use this texture name to verify rear views are not reusing the front-facing approved PNG.
        public const string FemaleSurvivorRearReferenceTextureName = "FemaleSurvivorRearReferenceCutout";

        // The full reference card keeps the authored model proportions while fitting the current zombie scale.
        private const float FemaleReferenceModelHeight = 1.56f;

        // The texture has a 1024x1536 aspect, so a two-thirds width preserves the original model silhouette.
        private const float FemaleReferenceModelAspect = 2f / 3f;

        // The card center is lowered so the boots sit on the road while the helmet stays near the old head height.
        private const float FemaleReferenceModelYOffset = -0.24f;

        // The front decal sits on the zombie-facing side for cameras looking back at the squad.
        private const float FemaleReferenceModelZOffset = 0.44f;

        // The rear cutout sits on the chase-camera side so opaque 3D backpack pieces cannot hide it.
        private const float FemaleRearReferenceModelZOffset = -0.365f;

        // More columns give the skinned texture enough geometry to deform legs without coarse warping.
        private const int FemaleReferenceMeshColumns = 12;

        // Extra rows give hip, knee, and boot bones enough vertical bands to show a real stride.
        private const int FemaleReferenceMeshRows = 28;

        // The source model's legs divide near the texture center after alpha padding is included.
        private const float FemaleReferenceLegSplitU = 0.50f;

        // Vertices below this V coordinate follow hip/knee/boot bones strongly for visible stepping.
        private const float FemaleReferenceLegFullWeightV = 0.48f;

        // Vertices above this V coordinate stay on the body bone so the torso remains model-exact.
        private const float FemaleReferenceBodyFullWeightV = 0.60f;

        // Boot texture rows bind mostly to the generated boot joints so feet visibly trade places.
        private const float FemaleReferenceBootFullWeightV = 0.18f;

        // Shin texture rows bind mostly to generated knee/shin chains so the run reads as jointed walking.
        private const float FemaleReferenceShinFullWeightV = 0.34f;

        // The flat texture should show walking but never let a leg bone pull the model apart.
        private const float FemaleReferenceMaximumLegWeight = 0.78f;

        // Body/root bone index for the skinned front and rear soldier decals.
        private const int FemaleReferenceBodyBoneIndex = 0;

        // Left hip bone index in the shared soldier decal bone array.
        private const int FemaleReferenceLeftHipBoneIndex = 1;

        // Right hip bone index in the shared soldier decal bone array.
        private const int FemaleReferenceRightHipBoneIndex = 2;

        // Left knee bone index in the shared soldier decal bone array.
        private const int FemaleReferenceLeftKneeBoneIndex = 3;

        // Right knee bone index in the shared soldier decal bone array.
        private const int FemaleReferenceRightKneeBoneIndex = 4;

        // Left boot bone index in the shared soldier decal bone array.
        private const int FemaleReferenceLeftBootBoneIndex = 5;

        // Right boot bone index in the shared soldier decal bone array.
        private const int FemaleReferenceRightBootBoneIndex = 6;

        private static readonly Color SurvivorSkinColor = new(0.84f, 0.62f, 0.43f);

        // Dark face-detail material gives generated eyes and mouth enough contrast at gameplay scale.
        private static readonly Color SurvivorFaceDetailColor = new(0.07f, 0.05f, 0.04f);

        // Dark hair visible under the helmet makes the soldier read feminine without reducing armor coverage.
        private static readonly Color SurvivorHairColor = new(0.13f, 0.07f, 0.04f);

        private static readonly Color SurvivorSuitColor = new(0.03f, 0.42f, 0.45f);

        private static readonly Color SurvivorPantsColor = new(0.13f, 0.20f, 0.26f);

        private static readonly Color SurvivorBootColor = new(0.035f, 0.035f, 0.04f);

        private static readonly Color SurvivorGearColor = new(0.10f, 0.14f, 0.16f);

        private static readonly Color SurvivorArmorColor = new(0.29f, 0.32f, 0.32f);

        private static readonly Color SurvivorArmorTrimColor = new(0.12f, 0.14f, 0.14f);

        private static readonly Color SurvivorGlowColor = new(0.02f, 0.90f, 1f);

        private static readonly Color SurvivorWeaponColor = new(0.18f, 0.17f, 0.15f);

        private static readonly Color ZombieSkinColor = new(0.39f, 0.58f, 0.32f);

        private static readonly Color ZombieShirtColor = new(0.19f, 0.28f, 0.24f);

        private static readonly Color ZombiePantsColor = new(0.58f, 0.66f, 0.70f);

        private static readonly Color ZombieWoundColor = new(0.62f, 0.04f, 0.035f);

        private static readonly Color ZombieEyeColor = new(0.04f, 0.04f, 0.035f);

        // The imported zombie skin keeps authored texture detail while reading as cold, infected flesh.
        private static readonly Color SwatZombieSkinTint = new(0.43f, 0.63f, 0.38f);

        // Desaturated olive dirties the tactical uniform without erasing its readable PBR fabric texture.
        private static readonly Color SwatZombieClothingTint = new(0.48f, 0.50f, 0.39f);

        // Rust-brown equipment separates undead armor from the survivor's clean neutral hardware.
        private static readonly Color SwatZombieGearTint = new(0.43f, 0.34f, 0.28f);

        // Saturated red overlays remain legible as blood at the portrait gameplay camera distance.
        private static readonly Color SwatZombieBloodColor = new(0.30f, 0.008f, 0.004f);

        // Pale red sclera make the oversized attached eyes look bloodshot instead of glowing like robots.
        private static readonly Color SwatZombieBloodshotEyeColor = new(0.76f, 0.39f, 0.25f);

        // Sickly translucent-looking green reads as saliva even through compressed gameplay recordings.
        private static readonly Color SwatZombieDroolColor = new(0.54f, 0.92f, 0.50f);

        private static readonly Color ArmorColor = new(0.33f, 0.39f, 0.43f);

        private static readonly Color ArmorTrimColor = new(0.12f, 0.15f, 0.17f);

        public static GameObject CreatePlayerSquad(string name, Vector3 position, Material uniformMaterial, Material accentMaterial)
        {
            // The gameplay root stays at the same authoritative transform used for lanes, camera follow, and shooting.
            GameObject squadRoot = new(name);
            squadRoot.transform.position = position;

            // Legacy palette parameters stay in the API, while the generated soldier uses a fixed reference-art palette.
            _ = uniformMaterial;
            _ = accentMaterial;

            // Materials are shared across all three soldiers so the visible squad keeps one uniform armor scheme.
            Material skinMaterial = CreateMaterial(SurvivorSkinColor);
            Material faceDetailMaterial = CreateMaterial(SurvivorFaceDetailColor);
            Material hairMaterial = CreateMaterial(SurvivorHairColor);
            Material pantsMaterial = CreateMaterial(SurvivorPantsColor);
            Material bootMaterial = CreateMaterial(SurvivorBootColor);
            Material gearMaterial = CreateMaterial(SurvivorGearColor);
            Material armorMaterial = CreateMaterial(SurvivorArmorColor);
            Material armorTrimMaterial = CreateMaterial(SurvivorArmorTrimColor);
            Material glowMaterial = CreateMaterial(SurvivorGlowColor);
            Material weaponMaterial = CreateMaterial(SurvivorWeaponColor);
            Material bodyMaterial = CreateMaterial(SurvivorSuitColor);
            Material referenceModelMaterial = CreateFemaleSurvivorReferenceMaterial();
            Material referenceRearMaterial = CreateFemaleSurvivorRearMaterial();

            // A three-person wedge makes squad count feel like people without spawning one mesh per count value.
            CreateSurvivor(squadRoot.transform, "Survivor Leader", new Vector3(0f, 0f, 0.08f), 1f, LeaderRifleName, bodyMaterial, skinMaterial, faceDetailMaterial, hairMaterial, pantsMaterial, bootMaterial, gearMaterial, armorMaterial, armorTrimMaterial, glowMaterial, weaponMaterial, referenceModelMaterial, referenceRearMaterial, true);

            // Side survivors sit behind the leader with a wider offset so full-size soldiers do not overlap.
            CreateSurvivor(squadRoot.transform, "Survivor Left Wing", new Vector3(-0.52f, -0.02f, -0.38f), SurvivorWingScale, LeaderRifleName, bodyMaterial, skinMaterial, faceDetailMaterial, hairMaterial, pantsMaterial, bootMaterial, gearMaterial, armorMaterial, armorTrimMaterial, glowMaterial, weaponMaterial, referenceModelMaterial, referenceRearMaterial, false);

            // Mirroring the side placement gives the player a recognizably human squad silhouette in one lane.
            CreateSurvivor(squadRoot.transform, "Survivor Right Wing", new Vector3(0.52f, -0.02f, -0.38f), SurvivorWingScale, LeaderRifleName, bodyMaterial, skinMaterial, faceDetailMaterial, hairMaterial, pantsMaterial, bootMaterial, gearMaterial, armorMaterial, armorTrimMaterial, glowMaterial, weaponMaterial, referenceModelMaterial, referenceRearMaterial, false);

            // The procedural animator moves the visible jointed soldiers when the gameplay root is moving.
            PrototypeHumanoidAnimator animator = squadRoot.AddComponent<PrototypeHumanoidAnimator>();
            animator.Configure(PrototypeHumanoidAnimationStyle.SurvivorSquad);

            return squadRoot;
        }

        public static GameObject CreateZombie(string name, Vector3 position, Material zombieMaterial, ZombieEnemyType enemyType)
        {
            // Standalone factory callers still receive deterministic variety from position and enemy type alone.
            int appearanceSeed = CreateZombieAppearanceSeed(0, 0, position, enemyType);
            return CreateZombie(name, position, zombieMaterial, enemyType, appearanceSeed);
        }

        public static GameObject CreateZombie(
            string name,
            Vector3 position,
            Material zombieMaterial,
            ZombieEnemyType enemyType,
            int appearanceSeed)
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

            // The generated animator remains a complete fallback when the optional licensed Resources model is absent.
            PrototypeHumanoidAnimator animator = zombieRoot.AddComponent<PrototypeHumanoidAnimator>();
            animator.Configure(enemyType == ZombieEnemyType.Armored ? PrototypeHumanoidAnimationStyle.ArmoredZombieShamble : PrototypeHumanoidAnimationStyle.ZombieShamble);

            // A successful imported build replaces only rendering and gait; gameplay targeting stays on zombieRoot.
            if (CreateSwatZombieTechnicalTrial(figureRoot.transform, enemyType, appearanceSeed))
            {
                // Hidden generated joints no longer need a per-frame LateUpdate once the visible Humanoid is active.
                animator.enabled = false;
            }

            return zombieRoot;
        }

        public static int CreateZombieAppearanceSeed(
            int levelNumber,
            int spawnIndex,
            Vector3 position,
            ZombieEnemyType enemyType)
        {
            unchecked
            {
                // FNV-style mixing is stable across platforms and does not consume UnityEngine.Random gameplay state.
                uint hash = 2166136261u;
                MixZombieAppearanceSeed(ref hash, levelNumber);
                MixZombieAppearanceSeed(ref hash, spawnIndex);
                MixZombieAppearanceSeed(ref hash, Mathf.RoundToInt(position.x * 100f));
                MixZombieAppearanceSeed(ref hash, Mathf.RoundToInt(position.z * 100f));
                MixZombieAppearanceSeed(ref hash, (int)enemyType);

                // Reserve the low nibble for spawn order so the first palette cycle cannot repeat a dominant colour.
                uint paletteVariant = (uint)spawnIndex & SwatZombieAppearance.DominantPaletteVariantMask;
                uint variantMask = SwatZombieAppearance.DominantPaletteVariantMask;
                uint encodedHash = (hash & ~variantMask) | paletteVariant;

                // System.Random accepts positive seeds; sixteen preserves variant zero for the otherwise all-zero result.
                int seed = (int)(encodedHash & 0x7fffffffu);
                return seed == 0 ? SwatZombieAppearance.DominantPaletteVariantMask + 1 : seed;
            }
        }

        private static void MixZombieAppearanceSeed(ref uint hash, int value)
        {
            // Mixing complete quantized values gives nearby lane/distance spawns unrelated-looking sequences.
            hash ^= (uint)value;
            hash *= 16777619u;
        }

        private static bool CreateSwatZombieTechnicalTrial(
            Transform figureRoot,
            ZombieEnemyType enemyType,
            int appearanceSeed)
        {
            // Reuse the same optimized licensed prefab as the survivor so no second human mesh is distributed.
            GameObject swatPrefab = Resources.Load<GameObject>(SwatSurvivorResourcePath);
            if (swatPrefab == null)
            {
                // Leaving the generated renderers enabled preserves the existing recognizable zombie fallback.
                Debug.LogWarning($"SWAT zombie model was not found at Resources/{SwatSurvivorResourcePath}.");
                return false;
            }

            // Hide the old primitive body before adding imported children so only one visible zombie silhouette remains.
            HideGeneratedZombieMeshRenderers(figureRoot);

            // The imported child shares meshes and texture assets while retaining independent bones and materials.
            GameObject swatModel = Object.Instantiate(swatPrefab, figureRoot, false);
            swatModel.name = SwatZombieModelName;

            // Survivors face +Z; enemies turn around to stare and stumble toward the approaching squad.
            swatModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // Matching survivor scale keeps the converted enemy recognizably based on the same woman soldier.
            swatModel.transform.localScale = Vector3.one * SwatSurvivorScale;

            // Root-height compensation preserves the established road-contact plane for stationary enemies.
            swatModel.transform.localPosition = new Vector3(0f, SwatZombieYOffset, 0f);

            // Build the same explicit texture channels with an infected tint profile rather than flat green fallbacks.
            ApplySwatPbrMaterials(swatModel, true);

            // Zombies are unarmed; source firearm meshes stay disabled even though their helper bones remain retargetable.
            DisableSwatZombieWeaponRenderers(swatModel.transform);

            // The balaclava would conceal blood and drool, so every zombie exposes the underlying skinned face.
            SetNamedSwatRendererEnabled(swatModel.transform, "Balaclava_Mask", false);

            // Basic zombies lose the clean helmet, while armored enemies retain it as their gameplay durability tell.
            SetNamedSwatRendererEnabled(
                swatModel.transform,
                "AUG3M_Helmet_33393_Shape",
                enemyType == ZombieEnemyType.Armored);

            // The runtime override maps every survivor-controller state to the tracked in-place zombie walk clip.
            Animator animator = swatModel.GetComponent<Animator>() ?? swatModel.AddComponent<Animator>();
            animator.runtimeAnimatorController = GetOrCreateSwatZombieRuntimeController();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Rebind once so facial attachments are placed against the exact retargeted starting pose.
            animator.Rebind();
            animator.SetBool(SwatSurvivorLocomotionAnimator.MovingParameterName, true);
            animator.Update(0f);

            // Enlarged bloody eyes, wounds, and drool are reversible Unity geometry attached to live Humanoid bones.
            CreateSwatZombieFaceAndWoundDetails(swatModel.transform, animator);

            // Per-instance property blocks cut real suit holes, apply vivid palettes, and remove different tactical pieces.
            SwatZombieAppearance appearance = swatModel.AddComponent<SwatZombieAppearance>();
            appearance.Configure(enemyType, appearanceSeed);

            // Late-frame asymmetric motion turns the clean walk into a drunken, agitated near-stumble.
            SwatZombieAnimator zombieAnimator = swatModel.AddComponent<SwatZombieAnimator>();
            zombieAnimator.Configure(animator, figureRoot, enemyType);
            return true;
        }

        private static AnimatorOverrideController GetOrCreateSwatZombieRuntimeController()
        {
            if (swatZombieRuntimeController != null)
            {
                return swatZombieRuntimeController;
            }

            // Reusing the tested survivor graph preserves Humanoid setup and deterministic state evaluation.
            RuntimeAnimatorController survivorController = Resources.Load<RuntimeAnimatorController>(SwatSurvivorControllerResourcePath);
            if (survivorController == null)
            {
                throw new System.InvalidOperationException($"Missing SWAT controller at Resources/{SwatSurvivorControllerResourcePath}.");
            }

            // Animation-only Resources FBXs can include preview helpers, so select the one real authored take explicitly.
            AnimationClip[] zombieWalkClips = Resources.LoadAll<AnimationClip>(SwatZombieWalkResourcePath)
                .Where(clip => !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
                .ToArray();
            if (zombieWalkClips.Length != 1)
            {
                throw new System.InvalidOperationException(
                    $"Expected one zombie walk clip at Resources/{SwatZombieWalkResourcePath}, but found {zombieWalkClips.Length}.");
            }

            // Override both idle and moving source clips so an enemy always shambles while its gameplay root is stationary.
            AnimatorOverrideController controller = new(survivorController)
            {
                name = "SWAT Zombie Runtime Walk Controller",
                hideFlags = HideFlags.HideAndDontSave
            };
            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new(controller.overridesCount);
            controller.GetOverrides(overrides);
            for (int overrideIndex = 0; overrideIndex < overrides.Count; overrideIndex++)
            {
                // Keys must retain the original controller clip identity while values share the one zombie walk motion.
                overrides[overrideIndex] = new KeyValuePair<AnimationClip, AnimationClip>(
                    overrides[overrideIndex].Key,
                    zombieWalkClips[0]);
            }

            controller.ApplyOverrides(overrides);
            swatZombieRuntimeController = controller;
            return swatZombieRuntimeController;
        }

        private static void CreateSwatZombieFaceAndWoundDetails(Transform swatModel, Animator animator)
        {
            // Mapped Humanoid facial bones survive optimization and provide exact moving attachment anchors.
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform jaw = animator.GetBoneTransform(HumanBodyBones.Jaw);
            Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            Transform leftEye = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            Transform rightEye = animator.GetBoneTransform(HumanBodyBones.RightEye);
            if (head == null || jaw == null || chest == null || leftEye == null || rightEye == null)
            {
                throw new System.InvalidOperationException("The SWAT zombie requires mapped head, jaw, chest, and eye bones.");
            }

            // Model axes already include the 180-degree enemy turn and therefore point toward the approaching player.
            Vector3 forward = swatModel.forward;
            Vector3 right = swatModel.right;
            Vector3 up = swatModel.up;
            Quaternion faceRotation = Quaternion.LookRotation(forward, up);

            // A shared palette keeps all small infection cues saturated and readable without multiplying materials per eye.
            Material bloodMaterial = GetOrCreateSharedZombieDetailMaterial(
                ref swatZombieSharedBloodMaterial,
                SwatZombieBloodColor,
                "SWAT Zombie Blood Material");
            Material eyeMaterial = GetOrCreateSharedZombieDetailMaterial(
                ref swatZombieSharedEyeMaterial,
                SwatZombieBloodshotEyeColor,
                "SWAT Zombie Bloodshot Eye Material");
            Material pupilMaterial = GetOrCreateSharedZombieDetailMaterial(
                ref swatZombieSharedPupilMaterial,
                new Color(0.055f, 0.012f, 0.01f),
                "SWAT Zombie Dilated Pupil Material");
            Material droolMaterial = GetOrCreateSharedZombieDetailMaterial(
                ref swatZombieSharedDroolMaterial,
                SwatZombieDroolColor,
                "SWAT Zombie Drool Material");

            // Flattened socket patches frame the enlarged sclera with an unmistakably bloody rim.
            CreateAttachedWorldSphere(
                head,
                "Zombie Bloody Socket Left",
                leftEye.position + forward * 0.008f,
                new Vector3(0.034f, 0.028f, 0.008f),
                faceRotation,
                bloodMaterial);
            CreateAttachedWorldSphere(
                head,
                "Zombie Bloody Socket Right",
                rightEye.position + forward * 0.008f,
                new Vector3(0.034f, 0.028f, 0.008f),
                faceRotation,
                bloodMaterial);

            // Oversized bloodshot eyeballs protrude beyond the source corneas and pulse asynchronously at runtime.
            CreateAttachedWorldSphere(
                leftEye,
                SwatZombieBloodyEyeLeftName,
                leftEye.position + forward * 0.016f,
                new Vector3(0.034f, 0.029f, 0.022f),
                faceRotation,
                eyeMaterial);
            CreateAttachedWorldSphere(
                rightEye,
                SwatZombieBloodyEyeRightName,
                rightEye.position + forward * 0.016f,
                new Vector3(0.034f, 0.029f, 0.022f),
                faceRotation,
                eyeMaterial);

            // Dark dilated pupils make the red eyes look organic and preserve a clear gaze toward the player.
            CreateAttachedWorldSphere(
                leftEye,
                "Zombie Pupil Left",
                leftEye.position + forward * 0.028f,
                new Vector3(0.010f, 0.011f, 0.007f),
                faceRotation,
                pupilMaterial);
            CreateAttachedWorldSphere(
                rightEye,
                "Zombie Pupil Right",
                rightEye.position + forward * 0.028f,
                new Vector3(0.010f, 0.011f, 0.007f),
                faceRotation,
                pupilMaterial);

            // Thin downward streaks keep the eye infection readable when the pupils are only a few screen pixels wide.
            CreateAttachedWorldSphere(
                head,
                "Zombie Eye Blood Trail Left",
                leftEye.position + forward * 0.014f - up * 0.029f,
                new Vector3(0.006f, 0.038f, 0.006f),
                faceRotation,
                bloodMaterial);
            CreateAttachedWorldSphere(
                head,
                "Zombie Eye Blood Trail Right",
                rightEye.position + forward * 0.014f - up * 0.026f,
                new Vector3(0.006f, 0.034f, 0.006f),
                faceRotation,
                bloodMaterial);

            // Eye midpoint provides a stable mouth estimate even though the optimized source has no facial blendshapes.
            Vector3 eyeMidpoint = (leftEye.position + rightEye.position) * 0.5f;
            Vector3 mouthPosition = eyeMidpoint - up * 0.112f + forward * 0.026f;

            // A narrow strand and heavier terminal drop visibly hang from the open, animated jaw.
            CreateAttachedWorldCylinder(
                jaw,
                SwatZombieDroolStrandName,
                mouthPosition - up * 0.052f,
                new Vector3(0.013f, 0.105f, 0.013f),
                Quaternion.identity,
                droolMaterial);
            CreateAttachedWorldSphere(
                jaw,
                SwatZombieDroolDropName,
                mouthPosition - up * 0.115f,
                new Vector3(0.028f, 0.037f, 0.025f),
                Quaternion.identity,
                droolMaterial);

            // Flattened blood patches follow animated chest and head bones instead of floating in world space.
            CreateAttachedWorldSphere(
                chest,
                SwatZombieChestWoundName,
                chest.position + forward * 0.145f - right * 0.055f,
                new Vector3(0.052f, 0.125f, 0.014f),
                faceRotation,
                bloodMaterial);
            CreateAttachedWorldSphere(
                chest,
                "Zombie Chest Wound Smear",
                chest.position + forward * 0.147f - right * 0.020f + up * 0.018f,
                new Vector3(0.082f, 0.028f, 0.012f),
                faceRotation,
                bloodMaterial);
            CreateAttachedWorldSphere(
                head,
                SwatZombieHeadWoundName,
                head.position + forward * 0.103f + right * 0.075f + up * 0.042f,
                new Vector3(0.035f, 0.054f, 0.012f),
                faceRotation,
                bloodMaterial);
        }

        private static GameObject CreateAttachedWorldSphere(
            Transform parent,
            string name,
            Vector3 worldPosition,
            Vector3 worldScale,
            Quaternion worldRotation,
            Material material)
        {
            // Authoring in world units keeps facial proportions independent of Character Creator's centimetre bones.
            GameObject detail = PrototypeGeometryFactory.CreateSphere(name, worldPosition, worldScale, material);
            detail.transform.rotation = worldRotation;

            // World-position preservation converts the attachment into the exact parent-local transform automatically.
            detail.transform.SetParent(parent, true);
            return detail;
        }

        private static Material GetOrCreateSharedZombieDetailMaterial(
            ref Material cachedMaterial,
            Color color,
            string materialName)
        {
            if (cachedMaterial != null)
            {
                return cachedMaterial;
            }

            // Unity's fake-null check above also recreates a cache entry after editor play-mode teardown destroys it.
            cachedMaterial = CreateMaterial(color);
            cachedMaterial.name = materialName;
            return cachedMaterial;
        }

        private static GameObject CreateAttachedWorldCylinder(
            Transform parent,
            string name,
            Vector3 worldPosition,
            Vector3 worldScale,
            Quaternion worldRotation,
            Material material)
        {
            // Cylinder geometry gives drool a continuous strand instead of a chain of disconnected spheres.
            GameObject detail = PrototypeGeometryFactory.CreateCylinder(name, worldPosition, worldScale, material);
            detail.transform.rotation = worldRotation;

            // Keeping the initial world pose places the strand exactly under the jaw before animation begins.
            detail.transform.SetParent(parent, true);
            return detail;
        }

        private static void HideGeneratedZombieMeshRenderers(Transform figureRoot)
        {
            // This runs before importing the licensed child, so every current mesh renderer belongs to the fallback body.
            foreach (MeshRenderer renderer in figureRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.enabled = false;
            }
        }

        private static void DisableSwatZombieWeaponRenderers(Transform swatModel)
        {
            foreach (Renderer renderer in swatModel.GetComponentsInChildren<Renderer>(true))
            {
                // Renderer prefixes are stable across both original skinned weapons and any future mesh export variants.
                bool isWeapon = renderer.gameObject.name.StartsWith("SKM_WP_", System.StringComparison.Ordinal) ||
                                renderer.gameObject.name.StartsWith("SM_WP_", System.StringComparison.Ordinal);
                if (isWeapon)
                {
                    renderer.enabled = false;
                }
            }
        }

        private static void SetNamedSwatRendererEnabled(Transform swatModel, string rendererName, bool isEnabled)
        {
            foreach (Renderer renderer in swatModel.GetComponentsInChildren<Renderer>(true))
            {
                // Multiple imported branches can expose the same stable renderer name, so configure every exact match.
                if (renderer.gameObject.name == rendererName)
                {
                    renderer.enabled = isEnabled;
                }
            }
        }

        private static void CreateSurvivor(Transform squadRoot, string name, Vector3 localPosition, float scale, string weaponProfileName, Material bodyMaterial, Material skinMaterial, Material faceDetailMaterial, Material hairMaterial, Material pantsMaterial, Material bootMaterial, Material gearMaterial, Material armorMaterial, Material armorTrimMaterial, Material glowMaterial, Material weaponMaterial, Material referenceModelMaterial, Material referenceRearMaterial, bool useSwatTechnicalTrial)
        {
            // A per-survivor transform makes it cheap to scale and offset squad members as a formation.
            GameObject survivorRoot = new(name);
            survivorRoot.transform.SetParent(squadRoot, false);
            survivorRoot.transform.localPosition = localPosition;
            survivorRoot.transform.localRotation = Quaternion.identity;
            survivorRoot.transform.localScale = Vector3.one * scale;

            // Rounded torso and pelvis provide the 3D mass that can rotate toward a target.
            CreateSpherePart(survivorRoot.transform, "Human Torso", new Vector3(0f, 0.02f, 0f), new Vector3(0.34f, 0.50f, 0.20f), bodyMaterial, Quaternion.identity);
            CreateSpherePart(survivorRoot.transform, "Human Vest", new Vector3(0f, 0.06f, 0.11f), new Vector3(0.32f, 0.38f, 0.040f), armorTrimMaterial, Quaternion.identity);
            CreateSpherePart(survivorRoot.transform, "Human Pelvis", new Vector3(0f, -0.30f, 0f), new Vector3(0.34f, 0.18f, 0.19f), gearMaterial, Quaternion.identity);

            // Armor plates and light strips make the procedural 3D model read like the generated soldier concept art.
            CreateSurvivorTorsoArmor(survivorRoot.transform, bodyMaterial, armorMaterial, armorTrimMaterial, glowMaterial);

            // Head, helmet, and glow details make the player read as a tactical soldier instead of a prototype blob.
            CreateSpherePart(survivorRoot.transform, "Human Head", new Vector3(0f, 0.46f, -0.02f), new Vector3(0.19f, 0.21f, 0.18f), skinMaterial, Quaternion.identity);
            CreateSpherePart(survivorRoot.transform, "Human Hood Collar", new Vector3(0f, 0.36f, -0.05f), new Vector3(0.26f, 0.12f, 0.20f), bodyMaterial, Quaternion.identity);
            CreateSpherePart(survivorRoot.transform, "Human Helmet", new Vector3(0f, 0.56f, -0.01f), new Vector3(0.22f, 0.10f, 0.20f), armorTrimMaterial, Quaternion.identity);
            CreateSurvivorFaceDetails(survivorRoot.transform, skinMaterial, faceDetailMaterial, hairMaterial);
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

            // Procedural weapon profiles keep muzzle anchors and target direction stable for gameplay.
            Transform weaponRoot = CreateSurvivorWeapon(weaponHand, weaponProfileName, holdStyle, weaponMaterial, armorMaterial, glowMaterial);

            // The skinned soldier decals become the visible model, while the generated rig remains as its skeleton.
            CreateFemaleReferenceRig(survivorRoot.transform, weaponRoot, referenceModelMaterial, referenceRearMaterial);

            if (useSwatTechnicalTrial)
            {
                // Only the leader uses the licensed model during this trial, so the shared asset is measured in gameplay.
                CreateSwatSurvivorTechnicalTrial(survivorRoot.transform);
            }

            // Hide primitive meshes after the skinned model exists so the minigame does not show doubled soldiers.
            HideGeneratedSurvivorMeshRenderers(survivorRoot.transform);
        }

        private static void CreateSwatSurvivorTechnicalTrial(Transform survivorRoot)
        {
            // Load the optimized FBX as a prefab so Unity shares its meshes and textures across future instances.
            GameObject swatPrefab = Resources.Load<GameObject>(SwatSurvivorResourcePath);
            if (swatPrefab == null)
            {
                // The existing skinned cutout remains a safe visual fallback if the optional trial asset is absent.
                Debug.LogWarning($"SWAT technical-trial model was not found at Resources/{SwatSurvivorResourcePath}.");
                return;
            }

            // Instantiating beneath the survivor root keeps lanes, camera tracking, health, and shooting unchanged.
            GameObject swatModel = Object.Instantiate(swatPrefab, survivorRoot, false);
            swatModel.name = SwatSurvivorModelName;

            // The source faces Unity's down-lane direction after Blender's -Z/Y FBX axis conversion.
            swatModel.transform.localRotation = Quaternion.identity;

            // Scale the 1.8-metre source to the established gameplay silhouette.
            swatModel.transform.localScale = Vector3.one * SwatSurvivorScale;

            // Lower the feet to the same road contact point as the generated survivor rig.
            swatModel.transform.localPosition = new Vector3(0f, SwatSurvivorYOffset, 0f);

            // Build explicit lit materials from external diffuse/normal channels instead of unreliable FBX embedding.
            ApplySwatPbrMaterials(swatModel, false);

            // The imported weapon skin does not visually follow its helper-bone rotations, so render it rigidly under the aim pivot.
            BakeVisibleSwatWeaponUnderAimPivot(swatModel.transform);

            // Replace the invisible prototype origin with an anchor calculated from the rendered rifle's frontmost muzzle plane.
            ReplaceLeaderMuzzleWithVisibleSwatMuzzle(survivorRoot, swatModel.transform);

            // Configure the imported Animator before the procedural squad animator caches any survivor transforms.
            ConfigureSwatLocomotion(swatModel, survivorRoot);

            // Stop the legacy camera-facing component before it can re-enable either hidden flat card.
            Transform frontReference = survivorRoot.Find(FemaleReferenceUpperName);
            ReferenceModelFacingVisibility facingVisibility = frontReference != null ? frontReference.GetComponent<ReferenceModelFacingVisibility>() : null;
            if (facingVisibility != null)
            {
                facingVisibility.enabled = false;
            }

            // Disable both legacy decal views only after the real 3D model has loaded successfully.
            SetSkinnedRendererEnabled(survivorRoot, FemaleReferenceUpperName, false);
            SetSkinnedRendererEnabled(survivorRoot, FemaleReferenceRearName, false);
        }

        private static void ConfigureSwatLocomotion(GameObject swatModel, Transform survivorRoot)
        {
            // The controller retargets separate Mixamo Humanoid idle/run clips onto the SWAT Avatar stored with the model.
            RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>(SwatSurvivorControllerResourcePath);
            if (controller == null)
            {
                throw new System.InvalidOperationException($"Missing SWAT locomotion controller at Resources/{SwatSurvivorControllerResourcePath}.");
            }

            // Humanoid FBX prefabs normally include an Animator; hand-built imports receive one defensively.
            Animator animator = swatModel.GetComponent<Animator>() ?? swatModel.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Measure the Player Squad root itself; the leader child can be rewritten by animation evaluation.
            SwatSurvivorLocomotionAnimator locomotion = swatModel.AddComponent<SwatSurvivorLocomotionAnimator>();
            locomotion.Configure(animator, survivorRoot.parent);
        }

        private static void ReplaceLeaderMuzzleWithVisibleSwatMuzzle(Transform survivorRoot, Transform swatModel)
        {
            // The generated marker is behind invisible prototype geometry and must not remain eligible for live shots.
            Transform generatedMuzzle = RequireDescendant(
                survivorRoot,
                PlayerSquad.WeaponMuzzleAnchorName,
                "generated leader weapon muzzle");
            generatedMuzzle.name = HiddenGeneratedWeaponMuzzleName;

            // The MPX joint rigidly owns the imported weapon hierarchy and gives target aiming one stable pivot.
            Transform weaponAimPivot = RequireDescendant(
                swatModel,
                SwatWeaponAimPivotName,
                "imported SWAT weapon aim pivot");

            // The exact barrel-end bone moves with the visible rifle and is the only truthful effect origin.
            Transform sourceMuzzle = RequireDescendant(
                weaponAimPivot,
                SwatWeaponSourceMuzzleName,
                "imported SWAT visible muzzle");

            // The barrel-base joint sits on the visible bore; the MPX pivot is offset and must not define firing direction.
            Transform barrelBase = RequireDescendant(
                weaponAimPivot,
                SwatWeaponBarrelBaseName,
                "imported SWAT barrel base");

            // The barrel-base-to-muzzle line follows the rendered rifle independently of FBX-local axis conventions.
            Vector3 barrelDirection = sourceMuzzle.position - barrelBase.position;
            if (barrelDirection.sqrMagnitude <= 0.0001f)
            {
                throw new System.InvalidOperationException("Imported SWAT barrel base and muzzle cannot occupy the same point.");
            }

            // The imported muzzle bone may sit behind the mesh opening, so derive the true tip from rendered geometry.
            Vector3 renderedBarrelTip = FindRenderedSwatBarrelTip(weaponAimPivot, barrelDirection);

            // A named child lets PlayerSquad keep its existing exact-name registry while using the visible barrel.
            GameObject visibleMuzzle = new(PlayerSquad.WeaponMuzzleAnchorName);

            // Parent to the same rigid aim pivot as the rendered weapon so Animator helper-bone motion cannot cause drift.
            visibleMuzzle.transform.SetParent(weaponAimPivot, false);

            // Position the effect origin at the centre of the frontmost rendered muzzle plane, not at the helper bone.
            visibleMuzzle.transform.position = renderedBarrelTip;

            // World rotation makes local +Z follow the actual barrel; parenting preserves that alignment during animation.
            visibleMuzzle.transform.rotation = Quaternion.LookRotation(barrelDirection.normalized, swatModel.up);
            visibleMuzzle.transform.localScale = Vector3.one;
        }

        private static Vector3 FindRenderedSwatBarrelTip(Transform weaponAimPivot, Vector3 barrelDirection)
        {
            // The rigid proxy is the exact weapon geometry players see after the original skinned muzzle is disabled.
            Transform rigidMuzzle = RequireDescendant(
                weaponAimPivot,
                SwatWeaponMuzzleRendererName + SwatWeaponRigidRendererSuffix,
                "rendered SWAT rigid muzzle proxy");
            MeshFilter rigidMuzzleFilter = rigidMuzzle.GetComponent<MeshFilter>();
            Mesh rigidMuzzleMesh = rigidMuzzleFilter != null ? rigidMuzzleFilter.sharedMesh : null;
            if (rigidMuzzleMesh == null || rigidMuzzleMesh.vertexCount == 0)
            {
                throw new System.InvalidOperationException("Rendered SWAT muzzle proxy must contain mesh vertices.");
            }

            // Projection along the authored bore finds the frontmost geometric plane regardless of FBX helper axes.
            Vector3 normalizedBarrelDirection = barrelDirection.normalized;
            Vector3[] muzzleVertices = rigidMuzzleMesh.vertices;
            float furthestProjection = float.NegativeInfinity;
            foreach (Vector3 muzzleVertex in muzzleVertices)
            {
                // Rigid proxy vertices are pivot-local, so transform each one to the current world-space weapon pose.
                Vector3 worldVertex = rigidMuzzle.TransformPoint(muzzleVertex);
                furthestProjection = Mathf.Max(furthestProjection, Vector3.Dot(worldVertex, normalizedBarrelDirection));
            }

            // Averaging the foremost ring yields the bore centre instead of selecting one arbitrary rim vertex.
            Vector3 frontPlaneSum = Vector3.zero;
            int frontPlaneVertexCount = 0;
            foreach (Vector3 muzzleVertex in muzzleVertices)
            {
                // Include bevel-adjacent duplicates within a tiny world-space tolerance for stable imported topology.
                Vector3 worldVertex = rigidMuzzle.TransformPoint(muzzleVertex);
                float distanceBehindFrontPlane = furthestProjection - Vector3.Dot(worldVertex, normalizedBarrelDirection);
                if (distanceBehindFrontPlane <= SwatWeaponBarrelTipPlaneTolerance)
                {
                    frontPlaneSum += worldVertex;
                    frontPlaneVertexCount++;
                }
            }

            if (frontPlaneVertexCount == 0)
            {
                throw new System.InvalidOperationException("Rendered SWAT muzzle has no vertices on its front plane.");
            }

            // The averaged front-plane point is the precise world-space origin for both flash and tracer geometry.
            return frontPlaneSum / frontPlaneVertexCount;
        }

        private static void BakeVisibleSwatWeaponUnderAimPivot(Transform swatModel)
        {
            // The shared weapon-body bone is the stable transform used by target-facing runtime aim.
            Transform weaponAimPivot = RequireDescendant(
                swatModel,
                SwatWeaponAimPivotName,
                "imported SWAT weapon aim pivot");

            foreach (SkinnedMeshRenderer sourceRenderer in swatModel.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                // Weapon renderer names are stable even when FBX import duplicates helper-bone Transform instances.
                bool hasWeaponRendererName = sourceRenderer.gameObject.name.StartsWith("SKM_WP_", System.StringComparison.Ordinal) ||
                                             sourceRenderer.gameObject.name.StartsWith("SM_WP_", System.StringComparison.Ordinal);

                // Human body skins may also contain generically named body bones, so names are the safe weapon boundary.
                if (!hasWeaponRendererName)
                {
                    continue;
                }

                // Bake the authored rest skin once so every visible rifle vertex becomes rigid relative to the aim pivot.
                Mesh bakedSourceMesh = new Mesh
                {
                    name = sourceRenderer.gameObject.name + " Baked Weapon Source",
                };
                sourceRenderer.BakeMesh(bakedSourceMesh, true);

                // Transform baked renderer-local vertices into the aim pivot's local coordinate system.
                CombineInstance weaponPart = new CombineInstance
                {
                    mesh = bakedSourceMesh,
                    transform = weaponAimPivot.worldToLocalMatrix * sourceRenderer.transform.localToWorldMatrix,
                };

                // Keeping submeshes separate preserves every authored material slot on the imported weapon.
                Mesh rigidWeaponMesh = new Mesh
                {
                    name = sourceRenderer.gameObject.name + SwatWeaponRigidRendererSuffix,
                };
                rigidWeaponMesh.CombineMeshes(new[] { weaponPart }, false, true, false);
                rigidWeaponMesh.RecalculateBounds();

                // A pivot-local proxy follows target aim as one solid firearm without any unreliable skin weights.
                GameObject rigidWeaponObject = new(sourceRenderer.gameObject.name + SwatWeaponRigidRendererSuffix);
                rigidWeaponObject.transform.SetParent(weaponAimPivot, false);
                rigidWeaponObject.transform.localPosition = Vector3.zero;
                rigidWeaponObject.transform.localRotation = Quaternion.identity;
                rigidWeaponObject.transform.localScale = Vector3.one;

                // MeshFilter owns the runtime-baked geometry while MeshRenderer keeps the source PBR material slots.
                MeshFilter meshFilter = rigidWeaponObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = rigidWeaponMesh;

                MeshRenderer meshRenderer = rigidWeaponObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
                meshRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                meshRenderer.receiveShadows = sourceRenderer.receiveShadows;
                meshRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
                meshRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
                meshRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
                meshRenderer.sortingOrder = sourceRenderer.sortingOrder;

                // Disable only the original weapon skin so it cannot remain visibly frozen in the authored diagonal pose.
                sourceRenderer.enabled = false;

                // CombineMeshes copied all geometry, so the intermediate bake can be released safely in either test mode.
                if (Application.isPlaying)
                {
                    Object.Destroy(bakedSourceMesh);
                }
                else
                {
                    Object.DestroyImmediate(bakedSourceMesh);
                }
            }
        }

        private static void ApplySwatPbrMaterials(GameObject swatModel, bool useZombiePalette)
        {
            // Zombie instances share immutable materials globally; the one survivor keeps its isolated neutral palette.
            Dictionary<string, Material> resolvedMaterials = useZombiePalette
                ? SwatZombieSharedPbrMaterials
                : new Dictionary<string, Material>();

            foreach (Renderer renderer in swatModel.GetComponentsInChildren<Renderer>(true))
            {
                // Preserve the authored material-slot count because body, eyes, helmet, and belt use multiple UV sets.
                Material[] sourceMaterials = renderer.sharedMaterials;
                Material[] pbrMaterials = new Material[sourceMaterials.Length];
                for (int slotIndex = 0; slotIndex < sourceMaterials.Length; slotIndex++)
                {
                    // Imported material names match the packed texture prefixes listed in the source Blender file.
                    string sourceMaterialName = NormalizeImportedMaterialName(sourceMaterials[slotIndex]?.name);
                    // Renderer identity is part of the key because the same source slot can be metal on one mesh only.
                    string materialCacheKey = useZombiePalette
                        ? $"{sourceMaterialName}|{renderer.gameObject.name}"
                        : sourceMaterialName;
                    if (!resolvedMaterials.TryGetValue(materialCacheKey, out Material pbrMaterial) || pbrMaterial == null)
                    {
                        pbrMaterial = CreateSwatPbrMaterial(sourceMaterialName, renderer.gameObject.name, useZombiePalette);
                        resolvedMaterials[materialCacheKey] = pbrMaterial;
                    }

                    pbrMaterials[slotIndex] = pbrMaterial;
                }

                renderer.sharedMaterials = pbrMaterials;
            }
        }

        private static string NormalizeImportedMaterialName(string materialName)
        {
            // Unity appends this suffix to cloned/imported material slots, while texture filenames keep the source name.
            const string instanceSuffix = " (Instance)";
            if (!string.IsNullOrEmpty(materialName) && materialName.EndsWith(instanceSuffix, System.StringComparison.Ordinal))
            {
                return materialName.Substring(0, materialName.Length - instanceSuffix.Length);
            }

            return string.IsNullOrEmpty(materialName) ? "default" : materialName;
        }

        private static Material CreateSwatPbrMaterial(string sourceMaterialName, string rendererName, bool useZombiePalette)
        {
            // Resolve external channels before shader selection because available specular maps use Standard's spec workflow.
            Texture2D diffuseTexture = Resources.Load<Texture2D>($"Survivor3D/Textures/{sourceMaterialName}_Diffuse");
            Texture2D normalTexture = Resources.Load<Texture2D>($"Survivor3D/Textures/{sourceMaterialName}_Normal") ??
                                      Resources.Load<Texture2D>($"Survivor3D/Textures/{sourceMaterialName}_Bump");
            Texture2D specularTexture = Resources.Load<Texture2D>($"Survivor3D/Textures/{sourceMaterialName}_Specular");

            // Only the zombie Suit needs real alpha-tested gaps; survivors and rigid equipment retain normal Standard PBR.
            bool usesTatteredClothingShader = useZombiePalette &&
                                               rendererName == "Suit" &&
                                               sourceMaterialName.Contains("Outfit", System.StringComparison.OrdinalIgnoreCase);

            // A Resources reference guarantees inclusion even though no serialized scene material points at this shader.
            Shader tatteredClothingShader = usesTatteredClothingShader
                ? Resources.Load<Shader>(SwatZombieTatteredClothingShaderResourcePath)
                : null;
            if (usesTatteredClothingShader && tatteredClothingShader == null)
            {
                throw new System.InvalidOperationException(
                    $"Missing zombie clothing shader at Resources/{SwatZombieTatteredClothingShaderResourcePath}.");
            }

            // Built-in Standard variants are the active PBR path; fallbacks keep the method safe after pipeline changes.
            Shader shader = tatteredClothingShader ??
                            (specularTexture != null ? Shader.Find("Standard (Specular setup)") : null) ??
                            Shader.Find("Standard") ??
                            Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Mobile/Diffuse");
            if (shader == null)
            {
                throw new System.InvalidOperationException("No lit shader is available for the SWAT PBR material set.");
            }

            Material material = new(shader)
            {
                name = useZombiePalette
                    ? $"SWAT Zombie {sourceMaterialName} Runtime PBR"
                    : $"SWAT {sourceMaterialName} Runtime PBR"
            };

            // Shared suit materials can still batch because every random colour and tear lives in a property block.
            material.enableInstancing = usesTatteredClothingShader;

            // External Resources textures are deterministic and survive scene serialization and player builds.
            if (diffuseTexture != null)
            {
                material.mainTexture = diffuseTexture;
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", diffuseTexture);
                }
            }

            // Normal maps restore garment seams, hard-surface panels, and weapon machining lost in flat fallbacks.
            if (normalTexture != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normalTexture);
                material.SetFloat("_BumpScale", 1f);
                material.EnableKeyword("_NORMALMAP");
            }

            // Source-provided specular maps keep boots and other coated surfaces from using a fabricated metal value.
            if (specularTexture != null && material.HasProperty("_SpecGlossMap"))
            {
                material.SetTexture("_SpecGlossMap", specularTexture);
                material.EnableKeyword("_SPECGLOSSMAP");
            }

            // Survivors preserve neutral albedo, while zombies multiply the same texture detail by an infected palette.
            SetSwatMaterialColor(
                material,
                useZombiePalette ? ResolveSwatZombieMaterialTint(sourceMaterialName) : Color.white);

            // Weapons and metal hardware receive a modest metallic response; fabric and skin remain dielectric.
            bool isMetal = sourceMaterialName.StartsWith("M_WP_", System.StringComparison.Ordinal) ||
                           sourceMaterialName.Contains("Hardware", System.StringComparison.Ordinal) ||
                           rendererName.StartsWith("SKM_WP_", System.StringComparison.Ordinal) ||
                           rendererName.StartsWith("SM_WP_", System.StringComparison.Ordinal);
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", isMetal ? 0.58f : 0.02f);
            }

            // Moderate smoothness keeps readable highlights without recreating the overly plastic source suit.
            if (material.HasProperty("_Glossiness"))
            {
                // Torn cloth remains rougher than intact equipment so bright palettes do not look like glossy plastic.
                material.SetFloat("_Glossiness", usesTatteredClothingShader ? 0.18f : isMetal ? 0.52f : 0.28f);
            }

            if (material.HasProperty("_GlossMapScale"))
            {
                material.SetFloat("_GlossMapScale", isMetal ? 0.52f : 0.28f);
            }

            return material;
        }

        private static Color ResolveSwatZombieMaterialTint(string sourceMaterialName)
        {
            // Skin, nails, and face channels share the same cold flesh tint across exposed body regions.
            if (sourceMaterialName.Contains("Skin", System.StringComparison.OrdinalIgnoreCase) ||
                sourceMaterialName.Contains("Nails", System.StringComparison.OrdinalIgnoreCase))
            {
                return SwatZombieSkinTint;
            }

            // Source eye and cornea textures remain blood-red even behind the larger attached sclera geometry.
            if (sourceMaterialName.Contains("Eye", System.StringComparison.OrdinalIgnoreCase) ||
                sourceMaterialName.Contains("Cornea", System.StringComparison.OrdinalIgnoreCase))
            {
                return SwatZombieBloodshotEyeColor;
            }

            // Fabric uses a dirty olive cast that preserves seams and folds from the authored diffuse/normal maps.
            if (sourceMaterialName.Contains("Outfit", System.StringComparison.OrdinalIgnoreCase) ||
                sourceMaterialName.Contains("Suit", System.StringComparison.OrdinalIgnoreCase))
            {
                return SwatZombieClothingTint;
            }

            // Boots, gloves, belts, armor, and helmet hardware share a worn brown-gray equipment tint.
            return SwatZombieGearTint;
        }

        private static void SetSwatMaterialColor(Material material, Color color)
        {
            // Built-in Standard uses _Color, while URP Lit uses _BaseColor.
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
        }

        private static void SetSkinnedRendererEnabled(Transform root, string childName, bool isEnabled)
        {
            // Direct lookup is sufficient because both generated reference cards are survivor-root children.
            Transform child = root.Find(childName);
            if (child == null)
            {
                return;
            }

            // A missing renderer should not block the licensed model from appearing in hand-built test hierarchies.
            SkinnedMeshRenderer renderer = child.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = isEnabled;
            }
        }

        private static void HideGeneratedSurvivorMeshRenderers(Transform survivorRoot)
        {
            // A null root should never happen from factory construction, but this keeps test-built calls safe.
            if (survivorRoot == null)
            {
                return;
            }

            // Primitive MeshRenderers stay in the hierarchy as animated bones and muzzle carriers, not visuals.
            MeshRenderer[] generatedRenderers = survivorRoot.GetComponentsInChildren<MeshRenderer>(true);

            foreach (MeshRenderer generatedRenderer in generatedRenderers)
            {
                // Runtime-baked rifle meshes are licensed-model visuals, not procedural placeholders.
                if (generatedRenderer.gameObject.name.EndsWith(SwatWeaponRigidRendererSuffix, System.StringComparison.Ordinal))
                {
                    continue;
                }

                // Disabled renderers prevent primitive body and weapon anchors from floating over the model cutouts.
                generatedRenderer.enabled = false;
            }
        }

        private static Material CreateFemaleSurvivorReferenceMaterial()
        {
            // Load the approved woman soldier model once through the shared guard path.
            Texture2D texture = LoadFemaleSurvivorReferenceTexture();

            // One transparent material keeps every front decal matched to the same source art.
            return PrototypeMaterialFactory.CreateTexturedTransparent(texture, Color.white, "LaneSurvivor Female Survivor Reference Model Material", (int)RenderQueue.Transparent);
        }

        private static Material CreateFemaleSurvivorRearMaterial()
        {
            // Load the approved front art so the rear decal can preserve the exact source silhouette and detail rhythm.
            Texture2D frontReferenceTexture = LoadFemaleSurvivorReferenceTexture();

            // The rear decal is authored from the same source art plus rear-facing armor and backpack details.
            Texture2D texture = ProceduralSoldierRearTexture.Create(FemaleSurvivorRearReferenceTextureName, frontReferenceTexture);

            // A neutral tint preserves the generated rear art instead of collapsing it to a dark silhouette.
            return PrototypeMaterialFactory.CreateTexturedTransparent(texture, Color.white, "LaneSurvivor Female Survivor Rear Decal Material", (int)RenderQueue.Transparent - 1);
        }

        private static Texture2D LoadFemaleSurvivorReferenceTexture()
        {
            // The approved woman soldier model lives in Resources beside the earlier survivor cutouts.
            Texture2D texture = Resources.Load<Texture2D>(FemaleSurvivorReferenceResourcePath);

            // Missing model art would make the minigame fall back to the defective generated look.
            if (texture == null)
            {
                throw new System.InvalidOperationException($"Missing survivor reference texture at Resources/{FemaleSurvivorReferenceResourcePath}.");
            }

            return texture;
        }

        private static void CreateFemaleReferenceRig(Transform survivorRoot, Transform weaponRoot, Material referenceModelMaterial, Material referenceRearMaterial)
        {
            // The skinned texture needs both hips so the full model walks without chopped detached sprites.
            Transform leftLeg = survivorRoot.Find("Human Leg Left");
            Transform rightLeg = survivorRoot.Find("Human Leg Right");

            // The weapon root is still required because muzzle anchors and target-facing recoil use it.
            if (weaponRoot == null)
            {
                throw new System.InvalidOperationException("Female survivor reference model requires a generated weapon root.");
            }

            // A single full-texture skinned mesh keeps the minigame soldier visually identical to the approved model.
            CreateSkinnedFemaleReferenceModel(survivorRoot, leftLeg, rightLeg, referenceModelMaterial, referenceRearMaterial);
        }

        private static void CreateSurvivorTorsoArmor(Transform survivorRoot, Material jacketMaterial, Material armorMaterial, Material armorTrimMaterial, Material glowMaterial)
        {
            // Chest armor gives the 3D body a strong tactical front without covering the teal jacket entirely.
            CreateCubePart(survivorRoot, "Human Chest Armor", new Vector3(0f, 0.13f, 0.158f), new Vector3(0.40f, 0.24f, 0.046f), armorMaterial, Quaternion.identity);

            // A darker center strip echoes the reference jacket zipper and keeps the torso from reading as a shield.
            CreateCubePart(survivorRoot, "Human Chest Center Plate", new Vector3(0f, 0.02f, 0.188f), new Vector3(0.070f, 0.46f, 0.034f), armorTrimMaterial, Quaternion.identity);

            // Thin zipper rails break up the teal torso like the reference soldier's layered fabric panels.
            CreateCubePart(survivorRoot, "Human Chest Left Rail", new Vector3(-0.120f, 0.02f, 0.214f), new Vector3(0.018f, 0.42f, 0.018f), armorMaterial, Quaternion.Euler(0f, 0f, -2f));
            CreateCubePart(survivorRoot, "Human Chest Right Rail", new Vector3(0.120f, 0.02f, 0.214f), new Vector3(0.018f, 0.42f, 0.018f), armorMaterial, Quaternion.Euler(0f, 0f, 2f));

            // Cyan chest light makes the soldier readable at small scale and matches the concept glow language.
            CreateCubePart(survivorRoot, "Human Chest Glow", new Vector3(0f, 0.21f, 0.218f), new Vector3(0.16f, 0.032f, 0.020f), glowMaterial, Quaternion.identity);

            // Diagonal teal jacket folds add the same fabric read as the painted reference without reducing armor.
            CreateCubePart(survivorRoot, "Human Jacket Fold Left", new Vector3(-0.155f, -0.01f, 0.202f), new Vector3(0.018f, 0.34f, 0.016f), armorTrimMaterial, Quaternion.Euler(0f, 0f, -12f));
            CreateCubePart(survivorRoot, "Human Jacket Fold Right", new Vector3(0.155f, -0.01f, 0.202f), new Vector3(0.018f, 0.34f, 0.016f), armorTrimMaterial, Quaternion.Euler(0f, 0f, 12f));

            // The rear jacket panel keeps the chase camera from reading the soldier as only a dark backpack slab.
            CreateCubePart(survivorRoot, "Human Rear Jacket Panel", new Vector3(0f, 0.03f, -0.205f), new Vector3(0.30f, 0.43f, 0.034f), jacketMaterial, Quaternion.identity);

            // Rear shoulder armor mirrors the front tactical plating so the turned model still matches the concept.
            CreateCubePart(survivorRoot, "Human Rear Shoulder Plate", new Vector3(0f, 0.23f, -0.235f), new Vector3(0.34f, 0.10f, 0.036f), armorMaterial, Quaternion.identity);

            // Diagonal rear straps echo the reference harness and break up the large back-facing teal surface.
            CreateCubePart(survivorRoot, "Human Rear Strap Left", new Vector3(-0.095f, 0.02f, -0.245f), new Vector3(0.035f, 0.42f, 0.034f), armorTrimMaterial, Quaternion.Euler(0f, 0f, -18f));
            CreateCubePart(survivorRoot, "Human Rear Strap Right", new Vector3(0.095f, 0.02f, -0.245f), new Vector3(0.035f, 0.42f, 0.034f), armorTrimMaterial, Quaternion.Euler(0f, 0f, 18f));

            // A small rear spine light gives the back view the same cyan tech read as the approved front model.
            CreateCubePart(survivorRoot, "Human Rear Spine Glow", new Vector3(0f, 0.08f, -0.268f), new Vector3(0.050f, 0.28f, 0.022f), glowMaterial, Quaternion.identity);

            // A compact backpack gives the rear chase view equipment without hiding the jacket and limb motion.
            CreateCubePart(survivorRoot, "Human Backpack", new Vector3(0f, 0.05f, -0.315f), new Vector3(0.24f, 0.35f, 0.080f), armorTrimMaterial, Quaternion.identity);

            // Side pack pods reproduce the reference soldier's stacked utility gear without becoming one rectangle.
            CreateCubePart(survivorRoot, "Human Backpack Side Pod Left", new Vector3(-0.18f, -0.02f, -0.285f), new Vector3(0.085f, 0.30f, 0.070f), armorTrimMaterial, Quaternion.Euler(0f, 0f, 5f));
            CreateCubePart(survivorRoot, "Human Backpack Side Pod Right", new Vector3(0.18f, -0.02f, -0.285f), new Vector3(0.085f, 0.30f, 0.070f), armorTrimMaterial, Quaternion.Euler(0f, 0f, -5f));

            // A vertical backpack light keeps the rear view visually connected to the cyan suit highlights.
            CreateCubePart(survivorRoot, "Human Backpack Glow", new Vector3(0f, 0.07f, -0.365f), new Vector3(0.045f, 0.22f, 0.022f), glowMaterial, Quaternion.identity);

            // A raised antenna keeps the rotated back view from looking like the old plain body.
            CreateCylinderPart(survivorRoot, "Human Backpack Antenna", new Vector3(-0.10f, 0.43f, -0.350f), new Vector3(0.010f, 0.20f, 0.010f), armorTrimMaterial, Quaternion.identity);

            // Belt pouches build the chunky utility silhouette visible in the concept without adding colliders.
            CreateCubePart(survivorRoot, "Human Belt Pouch Left", new Vector3(-0.22f, -0.23f, 0.17f), new Vector3(0.10f, 0.13f, 0.07f), armorTrimMaterial, Quaternion.Euler(0f, 0f, 5f));
            CreateCubePart(survivorRoot, "Human Belt Pouch Right", new Vector3(0.22f, -0.23f, 0.17f), new Vector3(0.10f, 0.13f, 0.07f), armorTrimMaterial, Quaternion.Euler(0f, 0f, -5f));

            // The center buckle gives the front-facing model the same tactical belt focal point as the reference.
            CreateCubePart(survivorRoot, "Human Belt Buckle", new Vector3(0f, -0.225f, 0.205f), new Vector3(0.13f, 0.070f, 0.028f), armorMaterial, Quaternion.identity);
        }

        private static void CreateSurvivorFaceDetails(Transform survivorRoot, Material skinMaterial, Material faceDetailMaterial, Material hairMaterial)
        {
            // Dark brows and eyes give the head a forward-facing read when the 3D model rotates toward camera.
            CreateSpherePart(survivorRoot, "Human Eye Left", new Vector3(-0.060f, 0.490f, 0.190f), new Vector3(0.022f, 0.014f, 0.010f), faceDetailMaterial, Quaternion.identity);
            CreateSpherePart(survivorRoot, "Human Eye Right", new Vector3(0.060f, 0.490f, 0.190f), new Vector3(0.022f, 0.014f, 0.010f), faceDetailMaterial, Quaternion.identity);

            // A small raised nose keeps the face from flattening into the head ellipsoid on side rotations.
            CreateSpherePart(survivorRoot, "Human Nose", new Vector3(0f, 0.455f, 0.205f), new Vector3(0.030f, 0.040f, 0.020f), skinMaterial, Quaternion.Euler(0f, 0f, 2f));

            // Tucked hair under the helmet signals a woman soldier while keeping the full armor silhouette.
            CreateSpherePart(survivorRoot, "Human Hair Back", new Vector3(0f, 0.465f, -0.170f), new Vector3(0.145f, 0.145f, 0.050f), hairMaterial, Quaternion.identity);
            CreateSpherePart(survivorRoot, "Human Hair Left", new Vector3(-0.140f, 0.445f, 0.055f), new Vector3(0.035f, 0.105f, 0.026f), hairMaterial, Quaternion.Euler(0f, 0f, -8f));
            CreateSpherePart(survivorRoot, "Human Hair Right", new Vector3(0.140f, 0.445f, 0.055f), new Vector3(0.035f, 0.105f, 0.026f), hairMaterial, Quaternion.Euler(0f, 0f, 8f));

            // A segmented ponytail matches the reference silhouette and is visible even with the helmet intact.
            CreateSpherePart(survivorRoot, "Human Ponytail Base", new Vector3(0f, 0.565f, -0.245f), new Vector3(0.075f, 0.080f, 0.070f), hairMaterial, Quaternion.identity);
            CreateCylinderPart(survivorRoot, "Human Ponytail Upper", new Vector3(0f, 0.665f, -0.300f), new Vector3(0.045f, 0.18f, 0.045f), hairMaterial, Quaternion.Euler(-28f, 0f, 0f));
            CreateSpherePart(survivorRoot, "Human Ponytail Tip", new Vector3(0f, 0.755f, -0.355f), new Vector3(0.050f, 0.075f, 0.050f), hairMaterial, Quaternion.identity);

            // A small mouth and chin line gives the face expression without reading as facial hair.
            CreateSpherePart(survivorRoot, "Human Chin Shadow", new Vector3(0f, 0.390f, 0.185f), new Vector3(0.080f, 0.030f, 0.012f), faceDetailMaterial, Quaternion.identity);
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

            // A rear helmet glow keeps the chase view tied to the same cap-mounted cyan accent as the concept.
            CreateCubePart(survivorRoot, "Human Helmet Rear Glow", new Vector3(0f, 0.595f, -0.205f), new Vector3(0.11f, 0.022f, 0.018f), glowMaterial, Quaternion.identity);

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
            Transform shoulder = CreateJoint(survivorRoot, $"Human Arm {sideName}", new Vector3(sideSign * 0.24f, 0.36f, -0.01f), GetHumanShoulderRestRotation(sideSign, holdStyle));

            // The hand target separates shoulder-fired weapons from lower hip-fire weapons.
            Vector3 handLocalPosition = GetHumanHandLocalPosition(sideSign, holdStyle);

            // The visible upper arm spans from shoulder toward the raised hand instead of hanging at the side.
            CreateCylinderBetween(shoulder, $"Human Upper Arm {sideName} Mesh", Vector3.zero, handLocalPosition, 0.06f, sleeveMaterial);

            // A shoulder armor pad creates the bulky plated silhouette from the new soldier model.
            CreateSpherePart(shoulder, $"Human Shoulder Armor {sideName}", new Vector3(sideSign * 0.015f, 0.01f, 0.035f), new Vector3(0.16f, 0.090f, 0.11f), armorMaterial, Quaternion.identity);

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

        private static Transform CreateSurvivorWeapon(Transform hand, string weaponProfileName, SurvivorWeaponHoldStyle holdStyle, Material weaponMaterial, Material armorMaterial, Material glowMaterial)
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

            return weaponRoot.transform;
        }

        private static SurvivorWeaponHoldStyle GetWeaponHoldStyle(string weaponProfileName)
        {
            switch (weaponProfileName)
            {
                case LeaderRifleName:
                case LeftWingShotgunName:
                    // Rifle-like legacy profiles are shoulder-fired so their muzzles stay near the survivor's face.
                    return SurvivorWeaponHoldStyle.EyeLevel;
                case RightWingSmgName:
                    // Legacy compact profiles keep their old lower hold if an older scene still contains one.
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
                    // Eye-level hands stay close to the chest so the visible rifle reads as held, not floating up-lane.
                    return isLeftHand ? new Vector3(0.13f, 0.03f, 0.19f) : new Vector3(-0.04f, 0.09f, 0.17f);
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
                    // Shoulder-fired rifles shift back toward the centerline so the grip sits under the raised hands.
                    return new Vector3(-0.14f, -0.015f, 0.015f);
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
                    // A slight yaw keeps the rifle side readable while preserving a down-lane muzzle direction.
                    return Quaternion.Euler(0f, -8f, 0f);
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
            // The rifle body is compact so the chase camera reads it as held at the shoulder, not detached ahead.
            CreateCylinderPart(weaponRoot, "Leader Rifle Body", new Vector3(0f, 0f, 0.045f), new Vector3(0.055f, 0.13f, 0.055f), weaponMaterial, Quaternion.Euler(90f, 0f, 0f));

            // A short forward barrel keeps the muzzle attached to the soldier footprint while still pointing down-lane.
            CreateCylinderPart(weaponRoot, "Leader Rifle Barrel", new Vector3(0f, 0f, 0.19f), new Vector3(0.028f, 0.12f, 0.028f), weaponMaterial, Quaternion.Euler(90f, 0f, 0f));

            // A boxy receiver makes the weapon feel closer to the approved futuristic rifle art.
            CreateCubePart(weaponRoot, "Leader Rifle Receiver", new Vector3(0f, 0.018f, 0.075f), new Vector3(0.13f, 0.09f, 0.15f), armorMaterial, Quaternion.identity);

            // A broad side plate makes the yawed rifle read as a weapon instead of only an end-on barrel.
            CreateCubePart(weaponRoot, "Leader Rifle Side Plate", new Vector3(0.070f, 0.010f, 0.105f), new Vector3(0.030f, 0.115f, 0.14f), armorMaterial, Quaternion.identity);

            // A cyan side strip gives the rifle a readable sci-fi accent from front and three-quarter views.
            CreateCubePart(weaponRoot, "Leader Rifle Glow Strip", new Vector3(0.088f, 0.075f, 0.115f), new Vector3(0.018f, 0.024f, 0.080f), glowMaterial, Quaternion.identity);

            // A second top glow catches front-on combat screenshots where the side strip is partially hidden.
            CreateCubePart(weaponRoot, "Leader Rifle Top Glow", new Vector3(0f, 0.095f, 0.165f), new Vector3(0.095f, 0.018f, 0.070f), glowMaterial, Quaternion.identity);

            // The rear stock gives the rifle a shoulder-fired silhouette without imported art.
            CreateCylinderPart(weaponRoot, "Leader Rifle Stock", new Vector3(0f, 0f, -0.055f), new Vector3(0.050f, 0.10f, 0.050f), weaponMaterial, Quaternion.Euler(90f, 0f, 0f));

            // A small vertical grip visually connects the weapon to the hand joint.
            CreateCylinderPart(weaponRoot, "Leader Rifle Grip", new Vector3(0f, -0.09f, 0.02f), new Vector3(0.035f, 0.16f, 0.035f), weaponMaterial, Quaternion.identity);

            // The magazine is the reference rifle's strongest lower silhouette cue.
            CreateCubePart(weaponRoot, "Leader Rifle Magazine", new Vector3(0f, -0.135f, 0.075f), new Vector3(0.085f, 0.18f, 0.055f), weaponMaterial, Quaternion.Euler(7f, 0f, 0f));

            // The sight block creates a recognizable top-mounted optic that rotates with the 3D weapon.
            CreateCubePart(weaponRoot, "Leader Rifle Sight", new Vector3(0f, 0.105f, 0.130f), new Vector3(0.10f, 0.08f, 0.070f), armorMaterial, Quaternion.identity);

            // A glowing sight lens gives shots a clear forward aiming cue.
            CreateCubePart(weaponRoot, "Leader Rifle Sight Glow", new Vector3(0f, 0.155f, 0.130f), new Vector3(0.055f, 0.018f, 0.040f), glowMaterial, Quaternion.identity);

            // This generated marker is replaced by the imported leader's real visible muzzle during SWAT construction.
            CreateWeaponMuzzleAnchor(weaponRoot, 0.32f);
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

            // The muzzle anchor is centered between the two authored barrel tips.
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

            // Rear calf armor keeps the running-away view as armored as the front-facing approved model.
            CreateCylinderPart(shin, $"Human Rear Shin Armor {sideName}", new Vector3(0f, -lowerLegLength * 0.45f, -0.065f), new Vector3(0.076f, lowerLegLength * 0.50f, 0.045f), armorMaterial, Quaternion.identity);

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

            // Rear heel lights make the step cycle visible from the fixed chase camera.
            CreateCubePart(shin, $"Human Rear Boot Glow {sideName}", new Vector3(0f, -lowerLegLength + 0.010f, -0.155f), new Vector3(0.065f, 0.016f, 0.016f), glowMaterial, Quaternion.Euler(7f, 0f, 0f));
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

        private static GameObject CreateSkinnedFemaleReferenceModel(Transform survivorRoot, Transform leftLeg, Transform rightLeg, Material frontMaterial, Material rearMaterial)
        {
            // A missing survivor root would leave the skinned mesh without a stable body bone.
            if (survivorRoot == null)
            {
                throw new System.InvalidOperationException("Female survivor reference model requires a survivor root.");
            }

            // Missing leg bones would turn walking back into a whole-body bob, so fail instead of faking it.
            if (leftLeg == null || rightLeg == null)
            {
                throw new System.InvalidOperationException("Female survivor reference model requires both generated leg joints.");
            }

            // Knee bones let the visible decal bend through the same connected joint chain as the hidden rig.
            Transform leftKnee = RequireDescendant(leftLeg, "Human Knee Left", "left knee");

            // The right knee mirrors the left so both halves of the texture can stride independently.
            Transform rightKnee = RequireDescendant(rightLeg, "Human Knee Right", "right knee");

            // Boot bones make the lowest texture rows swing as feet instead of sliding with the thigh.
            Transform leftBoot = RequireDescendant(leftKnee, "Human Boot Left", "left boot");

            // The right boot is bound separately so the two feet can alternate in the capture.
            Transform rightBoot = RequireDescendant(rightKnee, "Human Boot Right", "right boot");

            // The model object is separate so the animator can recoil the front decal without moving gameplay roots.
            GameObject model = new(FemaleReferenceUpperName);
            model.transform.SetParent(survivorRoot, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            // Body, hip, knee, and boot joints make the exact model art walk without showing the hidden primitives.
            Transform[] bones =
            {
                survivorRoot,
                leftLeg,
                rightLeg,
                leftKnee,
                rightKnee,
                leftBoot,
                rightBoot
            };

            // Bindposes convert each animated transform back into the model object's local mesh space.
            Matrix4x4[] bindposes = CreateReferenceBindposes(model.transform, bones);

            // The front mesh samples the entire approved PNG on the zombie-facing side.
            Mesh frontMesh = CreateSkinnedFemaleReferenceMesh($"{FemaleReferenceUpperName} Mesh", bindposes, false);

            // The rear mesh uses the rear-view cutout on the chase-camera side of the same animated bones.
            Mesh rearMesh = CreateSkinnedFemaleReferenceMesh($"{FemaleReferenceRearName} Mesh", bindposes, true);

            // SkinnedMeshRenderer lets the texture rotate in 3D and bend through the generated leg joints.
            SkinnedMeshRenderer renderer = model.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = frontMesh;
            renderer.sharedMaterial = frontMaterial;
            renderer.rootBone = survivorRoot;
            renderer.bones = bones;
            renderer.localBounds = frontMesh.bounds;
            renderer.updateWhenOffscreen = true;

            // The rear decal is a child so aim/recoil offsets applied to the front decal move both silhouettes.
            GameObject rearModel = new(FemaleReferenceRearName);
            rearModel.transform.SetParent(model.transform, false);
            rearModel.transform.localPosition = Vector3.zero;
            rearModel.transform.localRotation = Quaternion.identity;
            rearModel.transform.localScale = Vector3.one;

            // The rear renderer has its own rear-side mesh so the chase camera sees the rear soldier cutout.
            SkinnedMeshRenderer rearRenderer = rearModel.AddComponent<SkinnedMeshRenderer>();
            rearRenderer.sharedMesh = rearMesh;
            rearRenderer.sharedMaterial = rearMaterial;
            rearRenderer.rootBone = survivorRoot;
            rearRenderer.bones = bones;
            rearRenderer.localBounds = rearMesh.bounds;
            rearRenderer.updateWhenOffscreen = true;
            rearRenderer.enabled = false;

            // Hide the front decal from behind so the minigame camera cannot show a backward-facing soldier.
            ReferenceModelFacingVisibility visibility = model.AddComponent<ReferenceModelFacingVisibility>();
            visibility.Configure(renderer, rearRenderer, survivorRoot);

            return model;
        }

        private static Transform RequireDescendant(Transform root, string childName, string diagnosticName)
        {
            // A missing root cannot be searched for required animation bones.
            if (root == null)
            {
                throw new System.InvalidOperationException($"Female survivor reference model requires a {diagnosticName} bone.");
            }

            // Direct children cover most leg joints and keep normal construction cheap.
            Transform directChild = root.Find(childName);
            if (directChild != null)
            {
                return directChild;
            }

            // Boot joints live below shin children, so recurse through the generated leg chain.
            foreach (Transform child in root)
            {
                // Search depth-first so generated hierarchy order remains deterministic.
                Transform nestedChild = RequireDescendantOrNull(child, childName);
                if (nestedChild != null)
                {
                    return nestedChild;
                }
            }

            // Failing here is better than silently returning to a bob-only visible soldier.
            throw new System.InvalidOperationException($"Female survivor reference model requires a {diagnosticName} bone named {childName}.");
        }

        private static Transform RequireDescendantOrNull(Transform root, string childName)
        {
            // A missing branch cannot contain the requested required child.
            if (root == null)
            {
                return null;
            }

            // Exact-name lookup keeps unrelated future attachment points out of the binding list.
            Transform directChild = root.Find(childName);
            if (directChild != null)
            {
                return directChild;
            }

            // Recursive search handles knee -> shin -> boot nesting without exposing another public helper.
            foreach (Transform child in root)
            {
                // Return the first matching descendant in authored hierarchy order.
                Transform nestedChild = RequireDescendantOrNull(child, childName);
                if (nestedChild != null)
                {
                    return nestedChild;
                }
            }

            // Null tells the required wrapper to keep searching sibling branches.
            return null;
        }

        private static Matrix4x4[] CreateReferenceBindposes(Transform modelTransform, Transform[] bones)
        {
            // The renderer transform is the local frame where the card vertices are authored.
            Matrix4x4 rendererLocalToWorld = modelTransform.localToWorldMatrix;

            // Each bindpose captures the inverse rest pose for one generated animation bone.
            Matrix4x4[] bindposes = new Matrix4x4[bones.Length];

            for (int i = 0; i < bones.Length; i++)
            {
                // A missing bone would make the mesh explode once Unity evaluates skinning.
                if (bones[i] == null)
                {
                    throw new System.InvalidOperationException("Female survivor reference model cannot bind a null bone.");
                }

                // Unity expects each bindpose in renderer-local space.
                bindposes[i] = bones[i].worldToLocalMatrix * rendererLocalToWorld;
            }

            return bindposes;
        }

        private static Mesh CreateSkinnedFemaleReferenceMesh(string name, Matrix4x4[] bindposes, bool isRearView)
        {
            // The grid stores one vertex at each row/column intersection across the full source image.
            int vertexCount = (FemaleReferenceMeshColumns + 1) * (FemaleReferenceMeshRows + 1);

            // Each grid cell contributes two triangles to the front-facing textured model mesh.
            int triangleIndexCount = FemaleReferenceMeshColumns * FemaleReferenceMeshRows * 6;

            // Vertex positions are authored directly in survivor-local dimensions.
            Vector3[] vertices = new Vector3[vertexCount];

            // Normals face along local Z while the card width stays readable from the matching side.
            Vector3[] normals = new Vector3[vertexCount];

            // UVs cover the full approved texture so rest pose remains an exact model match.
            Vector2[] uvs = new Vector2[vertexCount];

            // Bone weights decide which vertices walk with the left or right generated hip.
            BoneWeight[] boneWeights = new BoneWeight[vertexCount];

            // The model width is derived from the source texture aspect to avoid squashing the soldier.
            float modelWidth = FemaleReferenceModelHeight * FemaleReferenceModelAspect;

            // The front and rear decals are broad from their correct viewing sides instead of foreshortened cards.
            Vector3 modelNormal = isRearView ? Vector3.back : Vector3.forward;

            for (int row = 0; row <= FemaleReferenceMeshRows; row++)
            {
                // V runs bottom to top, matching Unity texture coordinate convention.
                float v = row / (float)FemaleReferenceMeshRows;

                for (int column = 0; column <= FemaleReferenceMeshColumns; column++)
                {
                    // U runs left to right across the entire approved PNG.
                    float u = column / (float)FemaleReferenceMeshColumns;

                    // Flatten the two-dimensional grid coordinate into the vertex arrays.
                    int vertexIndex = row * (FemaleReferenceMeshColumns + 1) + column;

                    // Width stays horizontal so the selected facing texture remains model-readable in screenshots.
                    Vector3 modelWidthOffset = Vector3.right * ((u - 0.5f) * modelWidth);

                    // Front and rear cutouts live on opposite sides of the 3D rig to avoid depth hiding.
                    float modelZOffset = isRearView ? FemaleRearReferenceModelZOffset : FemaleReferenceModelZOffset;

                    // Y preserves model height while X/Z place the card width in the soldier's facing direction.
                    vertices[vertexIndex] = new Vector3(modelWidthOffset.x, FemaleReferenceModelYOffset + (v - 0.5f) * FemaleReferenceModelHeight, modelZOffset + modelWidthOffset.z);

                    // The full PNG is sampled without cropping so no body part can detach from the reference art.
                    uvs[vertexIndex] = new Vector2(u, v);

                    // A consistent normal keeps lighting stable if the transparent shader uses scene lights.
                    normals[vertexIndex] = modelNormal;

                    // Weight lower vertices to generated leg joints while keeping the upper body on the root.
                    boneWeights[vertexIndex] = CreateFemaleReferenceBoneWeight(u, v);
                }
            }

            // Triangle winding is front-facing for local +Z; culling off makes the same card visible from behind.
            int[] triangles = new int[triangleIndexCount];

            // Fill the triangle list cell by cell so adjacent grid vertices share edges cleanly.
            int triangleCursor = 0;
            for (int row = 0; row < FemaleReferenceMeshRows; row++)
            {
                for (int column = 0; column < FemaleReferenceMeshColumns; column++)
                {
                    // Lower-left vertex for this grid cell.
                    int bottomLeft = row * (FemaleReferenceMeshColumns + 1) + column;

                    // Lower-right vertex for this grid cell.
                    int bottomRight = bottomLeft + 1;

                    // Upper-left vertex for this grid cell.
                    int topLeft = bottomLeft + FemaleReferenceMeshColumns + 1;

                    // Upper-right vertex for this grid cell.
                    int topRight = topLeft + 1;

                    // First triangle of the rectangular grid cell.
                    triangles[triangleCursor++] = bottomLeft;
                    triangles[triangleCursor++] = topLeft;
                    triangles[triangleCursor++] = bottomRight;

                    // Second triangle of the rectangular grid cell.
                    triangles[triangleCursor++] = bottomRight;
                    triangles[triangleCursor++] = topLeft;
                    triangles[triangleCursor++] = topRight;
                }
            }

            // The mesh carries positions, full-image UVs, and skinning data for the approved soldier model.
            Mesh mesh = new()
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices,
                normals = normals,
                uv = uvs,
                boneWeights = boneWeights,
                bindposes = bindposes,
                triangles = triangles
            };

            // Recalculated bounds keep screenshot crops tight around the skinned full-model rectangle.
            mesh.RecalculateBounds();

            return mesh;
        }

        private static BoneWeight CreateFemaleReferenceBoneWeight(float u, float v)
        {
            // Vertices above the hip blend stay locked to the survivor root for an exact upper model.
            if (v >= FemaleReferenceBodyFullWeightV)
            {
                return CreateSingleBoneWeight(FemaleReferenceBodyBoneIndex);
            }

            // Texture-space left and right halves are attached to the matching generated hip joints.
            bool isLeftSide = u < FemaleReferenceLegSplitU;

            // The hip bone drives the upper leg and the hip band below the belt.
            int hipBoneIndex = isLeftSide ? FemaleReferenceLeftHipBoneIndex : FemaleReferenceRightHipBoneIndex;

            // The knee bone drives the shin section so the visible decal bends with the generated joint chain.
            int kneeBoneIndex = isLeftSide ? FemaleReferenceLeftKneeBoneIndex : FemaleReferenceRightKneeBoneIndex;

            // The boot bone drives the lowest rows so foot placement changes clearly between stride frames.
            int bootBoneIndex = isLeftSide ? FemaleReferenceLeftBootBoneIndex : FemaleReferenceRightBootBoneIndex;

            // Boots need the strongest non-root weighting because foot motion is the clearest anti-bob cue.
            if (v <= FemaleReferenceBootFullWeightV)
            {
                return CreateRootTwoBoneBlendWeight(bootBoneIndex, 0.72f, kneeBoneIndex, 0.18f);
            }

            // Shin rows follow the knee first, with a little boot contribution for ankle/foot swing.
            if (v <= FemaleReferenceShinFullWeightV)
            {
                return CreateRootTwoBoneBlendWeight(kneeBoneIndex, 0.64f, bootBoneIndex, 0.18f);
            }

            // Vertices below the hip blend keep root support while receiving enough leg weight to walk.
            if (v <= FemaleReferenceLegFullWeightV)
            {
                return CreateRootTwoBoneBlendWeight(hipBoneIndex, FemaleReferenceMaximumLegWeight, kneeBoneIndex, 0.10f);
            }

            // The hip band blends root and leg weights so the full model bends instead of tearing at the belt.
            float legWeight = Mathf.InverseLerp(FemaleReferenceBodyFullWeightV, FemaleReferenceLegFullWeightV, v) * FemaleReferenceMaximumLegWeight;

            return CreateRootLegBlendWeight(hipBoneIndex, legWeight);
        }

        private static BoneWeight CreateRootLegBlendWeight(int legBoneIndex, float legWeight)
        {
            // Clamp protects the model from future tuning values that would let a leg detach from the torso.
            float clampedLegWeight = Mathf.Clamp01(legWeight);

            // Root weight preserves the complete approved silhouette while the leg contribution adds stride motion.
            return new BoneWeight
            {
                boneIndex0 = FemaleReferenceBodyBoneIndex,
                weight0 = 1f - clampedLegWeight,
                boneIndex1 = legBoneIndex,
                weight1 = clampedLegWeight
            };
        }

        private static BoneWeight CreateRootTwoBoneBlendWeight(int primaryBoneIndex, float primaryWeight, int secondaryBoneIndex, float secondaryWeight)
        {
            // Clamp each requested contribution so invalid future tuning cannot exceed Unity's normalized range.
            float clampedPrimaryWeight = Mathf.Clamp01(primaryWeight);

            // Clamp the secondary contribution independently before normalizing the total below.
            float clampedSecondaryWeight = Mathf.Clamp01(secondaryWeight);

            // If the two moving bones exceed one, scale them together instead of dropping either joint abruptly.
            float movingWeightTotal = clampedPrimaryWeight + clampedSecondaryWeight;
            if (movingWeightTotal > 1f)
            {
                // The scale preserves the relative hip/knee/boot mix while leaving no negative root weight.
                float scale = 1f / movingWeightTotal;

                // Apply the shared scale to the primary moving bone contribution.
                clampedPrimaryWeight *= scale;

                // Apply the shared scale to the secondary moving bone contribution.
                clampedSecondaryWeight *= scale;

                // The normalized moving contribution now fills the available weight budget.
                movingWeightTotal = 1f;
            }

            // Root weight keeps the approved full-model silhouette coherent while the lower joints walk.
            float rootWeight = 1f - movingWeightTotal;

            return new BoneWeight
            {
                boneIndex0 = FemaleReferenceBodyBoneIndex,
                weight0 = rootWeight,
                boneIndex1 = primaryBoneIndex,
                weight1 = clampedPrimaryWeight,
                boneIndex2 = secondaryBoneIndex,
                weight2 = clampedSecondaryWeight
            };
        }

        private static BoneWeight CreateSingleBoneWeight(int boneIndex)
        {
            // Unity treats unspecified weights as zero, so one filled slot gives an exact rigid attachment.
            return new BoneWeight
            {
                boneIndex0 = boneIndex,
                weight0 = 1f
            };
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
