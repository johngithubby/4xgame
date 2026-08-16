using UnityEngine;

namespace LaneSurvivor.Gameplay
{
    public static class GameplayVisuals
    {
        // The runtime track is lowered so prototype actors can sit above it without tall world coordinates.
        public const float TrackSurfaceY = -0.6f;

        // All visible gameplay objects use this as their road-contact reference height.
        public const float TrackTopY = TrackSurfaceY;

        // The road is wide enough for three portrait-safe gameplay lanes and a little visual margin.
        public const float TrackWidth = 6.4f;

        // Side lanes are intentionally tight so side gates enter the portrait view before the player reaches them.
        public const float SideLaneX = 0.95f;

        // The lane match tolerance is below half the lane spacing so adjacent lanes do not both resolve.
        public const float LaneMatchTolerance = 0.38f;

        // The player center is raised so humanoid squad feet stay above the road while the chase camera follows the group.
        public const float PlayerCenterY = TrackTopY + 1.10f;

        // The player height represents the visible soldier-shell formation rather than a single placeholder cube.
        public const float PlayerHeight = 1.35f;

        // The player footprint fits a three-person mini formation inside one portrait-safe lane.
        public const float PlayerFootprint = 0.68f;

        // The world-space humanoid squad is now the primary player visual; the HUD marker remains as an emergency fallback.
        public static readonly bool UseScreenSpacePlayerMarker = false;

        // The skinned survivor model renderers stay visible so the player is represented by people instead of UI rectangles.
        public static readonly bool WorldPlayerMeshRenderersEnabled = true;

        // The HUD marker is compact so it does not repeat the oversized-world-player regression.
        public const float ScreenPlayerMarkerWidth = 36f;

        // The marker is slightly taller than wide so lane motion still reads like a squad placeholder.
        public const float ScreenPlayerMarkerHeight = 46f;

        // The marker follows a point above the gameplay root so it sits on top of the road stripe.
        public const float ScreenPlayerMarkerWorldOffsetY = 0.78f;

        // The marker follows a slightly rearward point so it stays visually connected to the chase camera.
        public const float ScreenPlayerMarkerWorldOffsetZ = -0.10f;

        // The marker is clamped above the lane buttons to keep mobile controls readable.
        public const float ScreenPlayerMarkerBottomPadding = 112f;

        // The beacon footprint is smaller than the body so the high marker reads as a marker, not a giant player.
        public const float PlayerBeaconFootprint = 0.30f;

        // A small high beacon keeps the squad visible when road perspective would hide the body.
        public const float PlayerBeaconOffsetY = 0.58f;

        // The beacon sits slightly behind the squad center so forward motion remains visually clear.
        public const float PlayerBeaconBackOffsetZ = -0.16f;

        // The beacon is thin to avoid being mistaken for another gate.
        public const float PlayerBeaconHeight = 0.15f;

        // The mast shows the squad center line without reaching gate-card height.
        public const float PlayerMastOffsetY = 0.42f;

        // The mast is tall enough to read but low enough to stay below gate and zombie markers.
        public const float PlayerMastHeight = 0.42f;

        // The mast is narrow so it does not hide lane objects at distance.
        public const float PlayerMastWidth = 0.07f;

        // Perspective FOV keeps the lane shooter readable without flattening it into a top-down board.
        public const float CameraFieldOfView = 66f;

        // The camera was tuned against a modern portrait iPhone shape, so wider screens can keep the base view.
        public const float CameraReferenceAspect = 9f / 19.5f;

        // Ultra-tall phones need a wider vertical FOV, but this cap avoids a distorted action-camera look.
        public const float CameraMaximumFieldOfView = 74f;

        // A wider chase view keeps all gameplay lanes visible while leaving room below the squad.
        public static readonly Vector3 CameraOffset = new(0f, 9.5f, -9.2f);

        // A modest forward look keeps runner depth without pushing the squad off the bottom of the frame.
        public static readonly Vector3 CameraLookAtOffset = new(0f, 0.6f, 1.6f);

        // Gate logic stays rooted at the road contact point even though the visible marker is higher.
        public const float GateRootY = TrackTopY;

        // Footprints are disabled for now because low decals looked like gates rising from underground.
        public const bool GateFootprintEnabled = false;

        // The footprint is only a flat decal, slightly above the road to prevent z-fighting.
        public const float GateFootprintY = TrackTopY + 0.035f;

        // Gate footprints are narrower than the visible cards so they do not become the primary marker.
        public const float GateFootprintWidth = 0.9f;

        // Footprints stay shallow so they read as colored contact hints rather than obstacles.
        public const float GateFootprintDepth = 0.28f;

        // The card sits a hair toward the camera so labels are not hidden by the gate root.
        public const float GateFaceOffsetZ = -0.12f;

        // The card bottom is high enough that distant gates do not reveal only a top bar first.
        public const float GateCardBottomY = TrackTopY + 1.18f;

        // The card height gives modifier text room while avoiding a full wall silhouette.
        public const float GateCardHeight = 0.72f;

        // The card width communicates lane choice without spilling into neighboring lanes.
        public const float GateCardWidth = 1.18f;

        // The card is thin because it is a readable marker, not a physical obstacle.
        public const float GateCardDepth = 0.12f;

        // The center is derived from bottom and height so tests can guard against vertical drift.
        public const float GateCardCenterY = GateCardBottomY + GateCardHeight * 0.5f;

        // Zombie visual bounds start just above the road so enemies read as standing bodies instead of floating cards.
        public const float ZombieCardBottomY = TrackTopY + 0.10f;

        // Zombie visual height covers the full generated humanoid from feet to head.
        public const float ZombieCardHeight = 1.30f;

        // Zombie visual width leaves room for outstretched arms without crossing lane boundaries.
        public const float ZombieCardWidth = 0.82f;

        // Zombie visual depth includes the forward-reaching arms that sell the undead silhouette.
        public const float ZombieCardDepth = 0.58f;

        // The zombie center is derived from bottom and height so target points stay consistent with the visual body.
        public const float ZombieCardCenterY = ZombieCardBottomY + ZombieCardHeight * 0.5f;

        // The finish line is a flat decal so it cannot appear as a raised green wall.
        public const float FinishLineY = TrackTopY + 0.04f;

        // The finish line has enough depth to read while remaining clearly ground-painted.
        public const float FinishLineDepth = 0.34f;

        // Legacy zombie fields map to the generated humanoid bounds for existing gameplay code and tests.
        public const float ZombieCenterY = ZombieCardCenterY;

        // Legacy zombie height now represents the generated humanoid visual height.
        public const float ZombieHeight = ZombieCardHeight;

        // World labels sit above actor bases while remaining below the top UI.
        public const float WorldLabelY = 1.15f;

        // Floating combat text starts above gates and humanoid zombies so the road horizon cannot hide it.
        public const float FeedbackLabelOffsetY = 0.92f;

        // Feedback text stays small enough to read as an event label instead of another obstacle card.
        public const float FeedbackLabelScale = 0.30f;

        // The dark duplicate is slightly offset in text-local space to keep yellow/red labels readable on iOS.
        public static readonly Vector3 FeedbackTextShadowOffset = new(0.045f, -0.045f, 0.015f);

        // Shot tracers are camera-facing strips, narrow enough to read as beams instead of obstacle bars.
        public const float ShotTracerWidth = 0.16f;

        // Tracers remain readable through most of the 0.35-second firing interval while their muzzle parent prevents drift.
        public const float ShotTracerLifetimeSeconds = 0.34f;

        // Weapon-origin tracers are long enough to show aim direction while remaining inside the survivor footprint.
        public const float ShotTracerWeaponForwardLength = 0.64f;

        // Muzzle flashes are compact flares at the rifle tip, not gameplay-sized pickup markers.
        public const float MuzzleFlashSize = 0.22f;

        // The flash stretches only a tiny distance down-lane so it cannot read as an arrow over the squad.
        public const float MuzzleFlashForwardLength = 0.12f;

        // Only legacy no-muzzle fallback shots get a tiny readability lift; real muzzles stay exact.
        public const float MuzzleFlashLiftY = 0.04f;

        // The flash lifetime is short enough that it does not hang behind the running squad.
        public const float MuzzleFlashLifetimeSeconds = 0.14f;

        // Tracer endpoints are lifted to the readable actor band and kept away from the road surface.
        public const float ShotTracerMinimumY = TrackTopY + 1.24f;

        // Legacy root-origin fallback shots use this lane-local sideways offset to avoid the white lane stripe.
        public const float ShotTracerLaneOffsetX = 0.24f;

        // Legacy root-origin fallback shots move forward so they appear near the survivor formation.
        public const float ShotTracerMuzzleForwardOffsetZ = 1.35f;

        public static Vector3 WithVisualY(Vector3 source, float visualY)
        {
            // Level data owns lane and distance, while this helper owns prototype vertical staging.
            return new Vector3(source.x, visualY, source.z);
        }

        public static float GetCameraFieldOfViewForAspect(float screenAspect)
        {
            // Invalid or uninitialized camera aspects should fall back to the tuned reference view.
            float safeAspect = screenAspect > 0f ? screenAspect : CameraReferenceAspect;

            // Preserve the reference horizontal viewing angle when the screen gets narrower than the tuned phone.
            float referenceHalfFovRadians = CameraFieldOfView * 0.5f * Mathf.Deg2Rad;

            // Horizontal coverage is represented as a tangent so it can be converted back to vertical FOV.
            float referenceHorizontalTan = Mathf.Tan(referenceHalfFovRadians) * CameraReferenceAspect;

            // Wider screens already show more lane width, so only narrow screens request a larger vertical FOV.
            float requestedFieldOfView = Mathf.Atan(referenceHorizontalTan / safeAspect) * 2f * Mathf.Rad2Deg;

            // Clamp keeps the base framing stable on tablets while giving tall phones controlled extra coverage.
            return Mathf.Clamp(requestedFieldOfView, CameraFieldOfView, CameraMaximumFieldOfView);
        }
    }
}
