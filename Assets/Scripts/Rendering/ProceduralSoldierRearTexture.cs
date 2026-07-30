using UnityEngine;

namespace LaneSurvivor.Rendering
{
    internal static class ProceduralSoldierRearTexture
    {
        // Keep the authored rear cutout at the same aspect ratio as the approved front reference art.
        private const int TextureWidth = 1024;

        // Matching the front reference height lets the existing skinned mesh use identical UVs and proportions.
        private const int TextureHeight = 1536;

        // A tiny normalized softness removes jagged edges without requiring imported antialiasing assets.
        private const float EdgeSoftness = 0.004f;

        // Nearly transparent source pixels should remain invisible so the rear cutout keeps the approved PNG boundary.
        private const float ReferenceAlphaCutoff = 0.015f;

        // The texture is cached because every squad member can share the same deterministic rear model art.
        private static Texture2D cachedTexture;

        public static Texture2D Create(string textureName, Texture2D frontReferenceTexture)
        {
            // Return the existing generated texture when multiple soldiers or tests request the rear model.
            if (cachedTexture != null)
            {
                return cachedTexture;
            }

            // Start with a fully transparent image so the rear model behaves like the approved PNG cutout.
            Color[] pixels = new Color[TextureWidth * TextureHeight];

            // Draw from back-to-front so the reference-derived silhouette sits under rear armor and gear details.
            DrawRearSoldier(pixels, frontReferenceTexture);

            // RGBA32 preserves transparent edges and bright cyan detail in player builds and tests.
            cachedTexture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
            {
                name = textureName,
                hideFlags = HideFlags.HideAndDontSave
            };

            // Upload the procedurally authored pixels into the Unity texture object.
            cachedTexture.SetPixels(pixels);

            // The texture is only sampled by materials after creation, so CPU readback can be released.
            cachedTexture.Apply(false, true);

            return cachedTexture;
        }

        public static Texture2D Create(string textureName)
        {
            // Tests or legacy callers can still request the deterministic rear texture without a source silhouette.
            return Create(textureName, null);
        }

        private static void DrawRearSoldier(Color[] pixels, Texture2D frontReferenceTexture)
        {
            // The rear palette mirrors the approved front model: dark teal cloth, gray armor, black gear, cyan lights.
            Color outline = new(0.010f, 0.014f, 0.014f, 1f);
            Color deepGear = new(0.075f, 0.095f, 0.095f, 0.88f);
            Color armor = new(0.36f, 0.39f, 0.39f, 0.92f);
            Color armorDark = new(0.15f, 0.18f, 0.18f, 0.90f);
            Color suit = new(0.025f, 0.50f, 0.53f, 0.86f);
            Color suitDark = new(0.020f, 0.28f, 0.32f, 0.88f);
            Color pants = new(0.13f, 0.24f, 0.31f, 0.90f);
            Color boot = new(0.025f, 0.025f, 0.030f, 1f);
            Color glow = new(0.00f, 0.93f, 1.00f, 1f);
            Color hair = new(0.14f, 0.070f, 0.025f, 1f);

            // The approved front model supplies the exact alpha boundary and internal highlight noise for the rear.
            bool copiedReferenceBase = TryDrawReferenceMatchedBase(pixels, frontReferenceTexture);

            // If the source texture is unavailable or unreadable, keep a deterministic authored fallback silhouette.
            if (!copiedReferenceBase)
            {
                DrawFallbackRearSilhouette(pixels, outline);
            }

            // The ponytail sits behind helmet and backpack so the soldier reads feminine without reducing armor.
            DrawCapsule(pixels, new Vector2(0.500f, 0.835f), new Vector2(0.420f, 0.710f), 0.034f, outline);
            DrawCapsule(pixels, new Vector2(0.500f, 0.835f), new Vector2(0.425f, 0.715f), 0.026f, hair);
            DrawEllipse(pixels, new Vector2(0.515f, 0.858f), new Vector2(0.055f, 0.040f), hair);

            // Boots are drawn first because the running leg bones deform the lower texture region.
            DrawCapsule(pixels, new Vector2(0.355f, 0.080f), new Vector2(0.318f, 0.130f), 0.047f, outline);
            DrawCapsule(pixels, new Vector2(0.645f, 0.080f), new Vector2(0.682f, 0.130f), 0.047f, outline);
            DrawCapsule(pixels, new Vector2(0.355f, 0.085f), new Vector2(0.322f, 0.132f), 0.034f, boot);
            DrawCapsule(pixels, new Vector2(0.645f, 0.085f), new Vector2(0.678f, 0.132f), 0.034f, boot);
            DrawCapsule(pixels, new Vector2(0.324f, 0.138f), new Vector2(0.352f, 0.138f), 0.010f, glow);
            DrawCapsule(pixels, new Vector2(0.648f, 0.138f), new Vector2(0.676f, 0.138f), 0.010f, glow);

            // Rear legs use separate dark outlines, pants, calf armor, and heel lights so the run reads clearly.
            DrawCapsule(pixels, new Vector2(0.410f, 0.450f), new Vector2(0.350f, 0.250f), 0.053f, outline);
            DrawCapsule(pixels, new Vector2(0.590f, 0.450f), new Vector2(0.650f, 0.250f), 0.053f, outline);
            DrawCapsule(pixels, new Vector2(0.350f, 0.250f), new Vector2(0.330f, 0.130f), 0.046f, outline);
            DrawCapsule(pixels, new Vector2(0.650f, 0.250f), new Vector2(0.670f, 0.130f), 0.046f, outline);
            DrawCapsule(pixels, new Vector2(0.410f, 0.450f), new Vector2(0.352f, 0.250f), 0.038f, pants);
            DrawCapsule(pixels, new Vector2(0.590f, 0.450f), new Vector2(0.648f, 0.250f), 0.038f, pants);
            DrawCapsule(pixels, new Vector2(0.350f, 0.250f), new Vector2(0.332f, 0.138f), 0.032f, pants);
            DrawCapsule(pixels, new Vector2(0.650f, 0.250f), new Vector2(0.668f, 0.138f), 0.032f, pants);
            DrawCapsule(pixels, new Vector2(0.345f, 0.242f), new Vector2(0.327f, 0.150f), 0.022f, armor);
            DrawCapsule(pixels, new Vector2(0.655f, 0.242f), new Vector2(0.673f, 0.150f), 0.022f, armor);
            DrawCapsule(pixels, new Vector2(0.316f, 0.198f), new Vector2(0.342f, 0.198f), 0.008f, glow);
            DrawCapsule(pixels, new Vector2(0.658f, 0.198f), new Vector2(0.684f, 0.198f), 0.008f, glow);

            // The teal rear jacket is wide at the shoulders and narrow at the waist like the approved front pose.
            DrawPolygon(pixels, new[]
            {
                new Vector2(0.340f, 0.360f),
                new Vector2(0.370f, 0.630f),
                new Vector2(0.440f, 0.705f),
                new Vector2(0.560f, 0.705f),
                new Vector2(0.630f, 0.630f),
                new Vector2(0.660f, 0.360f),
                new Vector2(0.580f, 0.305f),
                new Vector2(0.420f, 0.305f)
            }, suit);

            // Dark side panels recreate the front model's layered tactical suit instead of a single flat color.
            DrawPolygon(pixels, new[]
            {
                new Vector2(0.340f, 0.355f),
                new Vector2(0.370f, 0.615f),
                new Vector2(0.425f, 0.660f),
                new Vector2(0.445f, 0.330f),
                new Vector2(0.405f, 0.305f)
            }, suitDark);
            DrawPolygon(pixels, new[]
            {
                new Vector2(0.660f, 0.355f),
                new Vector2(0.630f, 0.615f),
                new Vector2(0.575f, 0.660f),
                new Vector2(0.555f, 0.330f),
                new Vector2(0.595f, 0.305f)
            }, suitDark);

            // The waist belt and buckle preserve the full tactical armor level from the approved soldier.
            DrawCapsule(pixels, new Vector2(0.365f, 0.360f), new Vector2(0.635f, 0.360f), 0.023f, armorDark);
            DrawRotatedRectangle(pixels, new Vector2(0.500f, 0.362f), new Vector2(0.075f, 0.046f), 0f, armor);

            // Shoulder and upper-back plates make the rear read as the same armored model rather than a vest.
            DrawCapsule(pixels, new Vector2(0.335f, 0.655f), new Vector2(0.665f, 0.655f), 0.050f, armor);
            DrawRotatedRectangle(pixels, new Vector2(0.500f, 0.650f), new Vector2(0.240f, 0.055f), 0f, armorDark);
            DrawCapsule(pixels, new Vector2(0.405f, 0.675f), new Vector2(0.595f, 0.675f), 0.014f, glow);

            // The harness straps cross over the rear jacket like the reference soldier's visible gear.
            DrawCapsule(pixels, new Vector2(0.385f, 0.650f), new Vector2(0.515f, 0.350f), 0.020f, outline);
            DrawCapsule(pixels, new Vector2(0.615f, 0.650f), new Vector2(0.485f, 0.350f), 0.020f, outline);
            DrawCapsule(pixels, new Vector2(0.390f, 0.645f), new Vector2(0.515f, 0.355f), 0.012f, armorDark);
            DrawCapsule(pixels, new Vector2(0.610f, 0.645f), new Vector2(0.485f, 0.355f), 0.012f, armorDark);

            // The backpack is compact and segmented so it reads as the approved model's gear, not a plain slab.
            DrawRotatedRectangle(pixels, new Vector2(0.500f, 0.520f), new Vector2(0.195f, 0.290f), 0f, outline);
            DrawRotatedRectangle(pixels, new Vector2(0.500f, 0.520f), new Vector2(0.145f, 0.225f), 0f, deepGear);
            DrawRotatedRectangle(pixels, new Vector2(0.397f, 0.485f), new Vector2(0.056f, 0.210f), -5f, armorDark);
            DrawRotatedRectangle(pixels, new Vector2(0.603f, 0.485f), new Vector2(0.056f, 0.210f), 5f, armorDark);
            DrawCapsule(pixels, new Vector2(0.500f, 0.615f), new Vector2(0.500f, 0.430f), 0.014f, glow);
            DrawCapsule(pixels, new Vector2(0.500f, 0.660f), new Vector2(0.500f, 0.635f), 0.010f, armor);

            // The helmet, headset, and ponytail sit above the pack and match the approved woman's silhouette.
            DrawEllipse(pixels, new Vector2(0.500f, 0.765f), new Vector2(0.145f, 0.086f), outline);
            DrawEllipse(pixels, new Vector2(0.500f, 0.768f), new Vector2(0.120f, 0.064f), armorDark);
            DrawCapsule(pixels, new Vector2(0.395f, 0.748f), new Vector2(0.605f, 0.748f), 0.026f, armor);
            DrawCapsule(pixels, new Vector2(0.448f, 0.812f), new Vector2(0.552f, 0.812f), 0.014f, glow);
            DrawEllipse(pixels, new Vector2(0.355f, 0.760f), new Vector2(0.040f, 0.052f), armor);
            DrawEllipse(pixels, new Vector2(0.645f, 0.760f), new Vector2(0.040f, 0.052f), armor);
            DrawEllipse(pixels, new Vector2(0.350f, 0.760f), new Vector2(0.014f, 0.030f), glow);
            DrawEllipse(pixels, new Vector2(0.650f, 0.760f), new Vector2(0.014f, 0.030f), glow);

            // Shoulder pads widen the silhouette like the approved front model's large armored pauldrons.
            DrawEllipse(pixels, new Vector2(0.285f, 0.620f), new Vector2(0.085f, 0.060f), outline);
            DrawEllipse(pixels, new Vector2(0.715f, 0.620f), new Vector2(0.085f, 0.060f), outline);
            DrawEllipse(pixels, new Vector2(0.290f, 0.620f), new Vector2(0.066f, 0.043f), armor);
            DrawEllipse(pixels, new Vector2(0.710f, 0.620f), new Vector2(0.066f, 0.043f), armor);
            DrawCapsule(pixels, new Vector2(0.245f, 0.645f), new Vector2(0.305f, 0.645f), 0.010f, glow);
            DrawCapsule(pixels, new Vector2(0.695f, 0.645f), new Vector2(0.755f, 0.645f), 0.010f, glow);

            // Raised arms converge around the over-shoulder rifle instead of spreading sideways across the back.
            DrawCapsule(pixels, new Vector2(0.300f, 0.600f), new Vector2(0.455f, 0.682f), 0.038f, outline);
            DrawCapsule(pixels, new Vector2(0.700f, 0.600f), new Vector2(0.565f, 0.690f), 0.038f, outline);
            DrawCapsule(pixels, new Vector2(0.305f, 0.600f), new Vector2(0.450f, 0.675f), 0.025f, suit);
            DrawCapsule(pixels, new Vector2(0.695f, 0.600f), new Vector2(0.570f, 0.682f), 0.025f, suit);
            DrawCapsule(pixels, new Vector2(0.450f, 0.678f), new Vector2(0.505f, 0.730f), 0.030f, outline);
            DrawCapsule(pixels, new Vector2(0.570f, 0.682f), new Vector2(0.535f, 0.735f), 0.030f, outline);
            DrawCapsule(pixels, new Vector2(0.455f, 0.680f), new Vector2(0.505f, 0.724f), 0.020f, armor);
            DrawCapsule(pixels, new Vector2(0.565f, 0.685f), new Vector2(0.535f, 0.728f), 0.020f, armor);
            DrawCapsule(pixels, new Vector2(0.430f, 0.666f), new Vector2(0.482f, 0.710f), 0.008f, glow);
            DrawCapsule(pixels, new Vector2(0.545f, 0.710f), new Vector2(0.575f, 0.676f), 0.008f, glow);

            // A high-contrast over-shoulder rifle points up the texture, matching the chase camera's zombie direction.
            DrawCapsule(pixels, new Vector2(0.555f, 0.600f), new Vector2(0.595f, 0.955f), 0.045f, outline);
            DrawCapsule(pixels, new Vector2(0.555f, 0.620f), new Vector2(0.592f, 0.925f), 0.030f, armorDark);
            DrawRotatedRectangle(pixels, new Vector2(0.560f, 0.690f), new Vector2(0.160f, 0.078f), 83f, outline);
            DrawRotatedRectangle(pixels, new Vector2(0.560f, 0.690f), new Vector2(0.120f, 0.052f), 83f, armorDark);
            DrawRotatedRectangle(pixels, new Vector2(0.610f, 0.775f), new Vector2(0.105f, 0.050f), 83f, outline);
            DrawRotatedRectangle(pixels, new Vector2(0.610f, 0.775f), new Vector2(0.074f, 0.030f), 83f, armor);
            DrawCapsule(pixels, new Vector2(0.575f, 0.710f), new Vector2(0.590f, 0.890f), 0.014f, glow);
            DrawCapsule(pixels, new Vector2(0.590f, 0.920f), new Vector2(0.598f, 0.982f), 0.016f, outline);
        }

        private static bool TryDrawReferenceMatchedBase(Color[] pixels, Texture2D frontReferenceTexture)
        {
            // A null texture means the caller could not load the approved model art.
            if (frontReferenceTexture == null)
            {
                return false;
            }

            Color[] sourcePixels;

            try
            {
                // GetPixels requires the imported reference to stay readable in editor/player builds.
                sourcePixels = frontReferenceTexture.GetPixels();
            }
            catch (UnityException)
            {
                // Falling back preserves runtime construction instead of crashing if an import setting regresses.
                return false;
            }

            // Empty or malformed source data cannot provide a reliable model-matched silhouette.
            if (sourcePixels == null || sourcePixels.Length == 0 || frontReferenceTexture.width <= 0 || frontReferenceTexture.height <= 0)
            {
                return false;
            }

            // Cache dimensions locally so every pixel loop avoids property lookups.
            int sourceWidth = frontReferenceTexture.width;
            int sourceHeight = frontReferenceTexture.height;

            for (int y = 0; y < TextureHeight; y++)
            {
                // Normalized V lets the rear texture track the front texture even if import dimensions change.
                float v = (y + 0.5f) / TextureHeight;

                // Clamp the source row to the readable texture bounds.
                int sourceY = Mathf.Clamp(Mathf.FloorToInt(v * sourceHeight), 0, sourceHeight - 1);

                for (int x = 0; x < TextureWidth; x++)
                {
                    // Normalized U keeps the silhouette aligned with the skinned mesh UVs.
                    float u = (x + 0.5f) / TextureWidth;

                    // Clamp the source column to the readable texture bounds.
                    int sourceX = Mathf.Clamp(Mathf.FloorToInt(u * sourceWidth), 0, sourceWidth - 1);

                    // Sample the approved front art at the matching normalized point.
                    Color source = sourcePixels[sourceY * sourceWidth + sourceX];

                    // Transparent reference pixels should not create rear texture mass.
                    if (source.a <= ReferenceAlphaCutoff)
                    {
                        continue;
                    }

                    // Convert front-facing color detail into rear-facing suit, armor, gear, and glow colors.
                    Color rearColor = ConvertReferencePixelToRearPalette(source, u, v);

                    // Source alpha preserves the exact cutout silhouette, antialiasing, and transparent edge softness.
                    pixels[y * TextureWidth + x] = WithAlpha(rearColor, source.a);
                }
            }

            return true;
        }

        private static Color ConvertReferencePixelToRearPalette(Color source, float u, float v)
        {
            // Source luminance carries the approved model's painted highlight and shadow structure into the rear.
            float luminance = source.r * 0.2126f + source.g * 0.7152f + source.b * 0.0722f;

            // High-cyan pixels should remain bright cyan because they are a defining part of the soldier design.
            bool isGlow = source.g > 0.45f && source.b > 0.55f && source.b > source.r * 1.45f;
            if (isGlow)
            {
                return ShadeColor(new Color(0.00f, 0.90f, 1.00f, 1f), luminance, 0.90f, 1.25f);
            }

            // The front face area becomes rear helmet/neck armor so the chase view does not read as backward.
            bool isFrontFaceRegion = v > 0.58f && v < 0.80f && u > 0.38f && u < 0.73f;
            bool isSkinColored = source.r > source.g * 1.05f && source.g > source.b * 1.12f;
            if (isFrontFaceRegion && isSkinColored)
            {
                return ShadeColor(new Color(0.18f, 0.21f, 0.21f, 1f), luminance, 0.72f, 1.12f);
            }

            // Lower source pixels become boots and armored pant legs.
            if (v < 0.15f)
            {
                return ShadeColor(new Color(0.035f, 0.040f, 0.045f, 1f), luminance, 0.70f, 1.18f);
            }

            // Mid-lower source pixels keep the same dark teal tactical pants language as the approved model.
            if (v < 0.43f)
            {
                return ShadeColor(new Color(0.12f, 0.24f, 0.30f, 1f), luminance, 0.70f, 1.22f);
            }

            // Upper shoulder and helmet zones favor gray armor plates over cloth.
            bool isArmorBand = v > 0.64f || source.r > 0.34f && source.g > 0.34f && source.b > 0.32f;
            if (isArmorBand)
            {
                return ShadeColor(new Color(0.32f, 0.36f, 0.36f, 1f), luminance, 0.68f, 1.25f);
            }

            // The torso defaults to the approved teal suit, with source luminance preserving painted folds.
            return ShadeColor(new Color(0.025f, 0.48f, 0.52f, 1f), luminance, 0.70f, 1.20f);
        }

        private static Color ShadeColor(Color color, float luminance, float minShade, float maxShade)
        {
            // Normalize the source luminance into a useful paint-detail range.
            float shade = Mathf.Lerp(minShade, maxShade, Mathf.InverseLerp(0.08f, 0.82f, luminance));

            // Clamp RGB after shading so bright highlights do not exceed Unity's expected color range.
            return new Color(
                Mathf.Clamp01(color.r * shade),
                Mathf.Clamp01(color.g * shade),
                Mathf.Clamp01(color.b * shade),
                color.a);
        }

        private static void DrawFallbackRearSilhouette(Color[] pixels, Color outline)
        {
            // A broad dark underlay gives the fallback alpha silhouette the same heavy armored mass as the front model.
            DrawPolygon(pixels, new[]
            {
                new Vector2(0.315f, 0.280f),
                new Vector2(0.300f, 0.560f),
                new Vector2(0.365f, 0.710f),
                new Vector2(0.500f, 0.765f),
                new Vector2(0.635f, 0.710f),
                new Vector2(0.700f, 0.560f),
                new Vector2(0.685f, 0.280f),
                new Vector2(0.580f, 0.225f),
                new Vector2(0.420f, 0.225f)
            }, outline);
        }

        private static void DrawEllipse(Color[] pixels, Vector2 center, Vector2 radius, Color color)
        {
            // Clamp the pixel bounds so normalized art coordinates never write outside the texture.
            int minX = Mathf.Max(0, Mathf.FloorToInt((center.x - radius.x - EdgeSoftness) * TextureWidth));
            int maxX = Mathf.Min(TextureWidth - 1, Mathf.CeilToInt((center.x + radius.x + EdgeSoftness) * TextureWidth));
            int minY = Mathf.Max(0, Mathf.FloorToInt((center.y - radius.y - EdgeSoftness) * TextureHeight));
            int maxY = Mathf.Min(TextureHeight - 1, Mathf.CeilToInt((center.y + radius.y + EdgeSoftness) * TextureHeight));

            for (int y = minY; y <= maxY; y++)
            {
                // Pixel centers avoid a half-pixel bias when rasterizing symmetric ellipses.
                float normalizedY = (y + 0.5f) / TextureHeight;

                for (int x = minX; x <= maxX; x++)
                {
                    // Convert each candidate pixel center into ellipse-local normalized space.
                    float normalizedX = (x + 0.5f) / TextureWidth;
                    float dx = (normalizedX - center.x) / Mathf.Max(radius.x, 0.0001f);
                    float dy = (normalizedY - center.y) / Mathf.Max(radius.y, 0.0001f);
                    float distance = dx * dx + dy * dy;

                    // The soft boundary keeps the generated cutout from looking stair-stepped in the Game view.
                    float coverage = Mathf.Clamp01((1f + EdgeSoftness - distance) / EdgeSoftness);
                    if (coverage > 0f)
                    {
                        BlendPixel(pixels, x, y, WithAlpha(color, color.a * coverage));
                    }
                }
            }
        }

        private static void DrawCapsule(Color[] pixels, Vector2 start, Vector2 end, float radius, Color color)
        {
            // Determine a conservative box around the segment and its circular caps.
            int minX = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(start.x, end.x) - radius - EdgeSoftness) * TextureWidth));
            int maxX = Mathf.Min(TextureWidth - 1, Mathf.CeilToInt((Mathf.Max(start.x, end.x) + radius + EdgeSoftness) * TextureWidth));
            int minY = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(start.y, end.y) - radius - EdgeSoftness) * TextureHeight));
            int maxY = Mathf.Min(TextureHeight - 1, Mathf.CeilToInt((Mathf.Max(start.y, end.y) + radius + EdgeSoftness) * TextureHeight));

            for (int y = minY; y <= maxY; y++)
            {
                // Work in normalized coordinates so art dimensions remain independent of texture resolution.
                float normalizedY = (y + 0.5f) / TextureHeight;

                for (int x = minX; x <= maxX; x++)
                {
                    // The distance to the center segment defines both the body and the rounded caps.
                    Vector2 point = new((x + 0.5f) / TextureWidth, normalizedY);
                    float distance = DistanceToSegment(point, start, end);
                    float coverage = Mathf.Clamp01((radius + EdgeSoftness - distance) / EdgeSoftness);

                    if (coverage > 0f)
                    {
                        BlendPixel(pixels, x, y, WithAlpha(color, color.a * coverage));
                    }
                }
            }
        }

        private static void DrawRotatedRectangle(Color[] pixels, Vector2 center, Vector2 size, float angleDegrees, Color color)
        {
            // Convert the authored rectangle into a four-point polygon so the shared fill path can draw it.
            float radians = angleDegrees * Mathf.Deg2Rad;
            Vector2 xAxis = new(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 yAxis = new(-Mathf.Sin(radians), Mathf.Cos(radians));
            Vector2 halfX = xAxis * (size.x * 0.5f);
            Vector2 halfY = yAxis * (size.y * 0.5f);

            DrawPolygon(pixels, new[]
            {
                center - halfX - halfY,
                center + halfX - halfY,
                center + halfX + halfY,
                center - halfX + halfY
            }, color);
        }

        private static void DrawPolygon(Color[] pixels, Vector2[] points, Color color)
        {
            // Empty polygons should do nothing rather than fail a scene bootstrap.
            if (points == null || points.Length < 3)
            {
                return;
            }

            // Find normalized bounds first so only nearby pixels run the point-in-polygon test.
            float minXNormalized = points[0].x;
            float maxXNormalized = points[0].x;
            float minYNormalized = points[0].y;
            float maxYNormalized = points[0].y;

            for (int i = 1; i < points.Length; i++)
            {
                minXNormalized = Mathf.Min(minXNormalized, points[i].x);
                maxXNormalized = Mathf.Max(maxXNormalized, points[i].x);
                minYNormalized = Mathf.Min(minYNormalized, points[i].y);
                maxYNormalized = Mathf.Max(maxYNormalized, points[i].y);
            }

            // Clamp the raster bounds to the texture area.
            int minX = Mathf.Max(0, Mathf.FloorToInt(minXNormalized * TextureWidth));
            int maxX = Mathf.Min(TextureWidth - 1, Mathf.CeilToInt(maxXNormalized * TextureWidth));
            int minY = Mathf.Max(0, Mathf.FloorToInt(minYNormalized * TextureHeight));
            int maxY = Mathf.Min(TextureHeight - 1, Mathf.CeilToInt(maxYNormalized * TextureHeight));

            for (int y = minY; y <= maxY; y++)
            {
                // Normalized pixel centers match the other primitive rasterizers.
                float normalizedY = (y + 0.5f) / TextureHeight;

                for (int x = minX; x <= maxX; x++)
                {
                    // Ray-cast containment gives stable fills for both convex and concave authored shapes.
                    Vector2 point = new((x + 0.5f) / TextureWidth, normalizedY);
                    if (PointInPolygon(point, points))
                    {
                        BlendPixel(pixels, x, y, color);
                    }
                }
            }
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            // Segment projection lets capsules cover slanted limbs, straps, rifles, and lights with one helper.
            Vector2 segment = end - start;

            // Degenerate segments fall back to a circle around the start point.
            if (segment.sqrMagnitude < 0.000001f)
            {
                return Vector2.Distance(point, start);
            }

            // Clamp the projection to the segment ends so the capsule has round caps instead of infinite lines.
            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
            Vector2 closestPoint = start + segment * t;

            return Vector2.Distance(point, closestPoint);
        }

        private static bool PointInPolygon(Vector2 point, Vector2[] points)
        {
            // Standard odd-even ray casting keeps polygon filling deterministic and dependency-free.
            bool isInside = false;
            int previousIndex = points.Length - 1;

            for (int currentIndex = 0; currentIndex < points.Length; currentIndex++)
            {
                // Edge endpoints are named for readability in the crossing test.
                Vector2 current = points[currentIndex];
                Vector2 previous = points[previousIndex];

                // Count horizontal ray crossings while avoiding division by zero on horizontal edges.
                bool crossesY = (current.y > point.y) != (previous.y > point.y);
                if (crossesY)
                {
                    float xOnEdge = (previous.x - current.x) * (point.y - current.y) / (previous.y - current.y) + current.x;
                    if (point.x < xOnEdge)
                    {
                        isInside = !isInside;
                    }
                }

                previousIndex = currentIndex;
            }

            return isInside;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            // Preserve RGB while applying shape coverage to the source alpha.
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static void BlendPixel(Color[] pixels, int x, int y, Color source)
        {
            // Convert x/y into the flat Texture2D pixel array index.
            int index = y * TextureWidth + x;

            // Source-over alpha compositing lets dark outlines and bright lights layer predictably.
            Color destination = pixels[index];
            float inverseAlpha = 1f - source.a;

            float outputAlpha = source.a + destination.a * inverseAlpha;

            // Fully transparent output should stay transparent instead of dividing by zero.
            if (outputAlpha <= 0.0001f)
            {
                pixels[index] = Color.clear;
                return;
            }

            pixels[index] = new Color(
                (source.r * source.a + destination.r * destination.a * inverseAlpha) / outputAlpha,
                (source.g * source.a + destination.g * destination.a * inverseAlpha) / outputAlpha,
                (source.b * source.a + destination.b * destination.a * inverseAlpha) / outputAlpha,
                outputAlpha);
        }
    }
}
