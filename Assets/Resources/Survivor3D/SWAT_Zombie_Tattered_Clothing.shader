Shader "LaneSurvivor/SWAT Zombie Tattered Clothing"
{
    Properties
    {
        // The authored outfit alpha preserves the original UV-island silhouette around every garment panel.
        _MainTex ("Outfit Coverage", 2D) = "white" {}

        // The original normal map keeps seams and folds readable after replacing the nearly black source albedo.
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 1

        // Each zombie supplies three deliberately saturated colours through one per-renderer property block.
        _UpperColor ("Upper Clothing", Color) = (1, 0.1, 0.35, 1)
        _LowerColor ("Lower Clothing", Color) = (0.1, 0.8, 1, 1)
        _AccentColor ("Clothing Accent", Color) = (0.9, 1, 0.05, 1)

        // Five guaranteed atlas-space wounds create torso, leg, arm, and silhouette-breaking gaps.
        _Hole0 ("Tear Hole 0", Vector) = (0.57, 0.82, 0.12, 0.12)
        _Hole1 ("Tear Hole 1", Vector) = (0.48, 0.42, 0.06, 0.12)
        _Hole2 ("Tear Hole 2", Vector) = (0.67, 0.38, 0.06, 0.12)
        _Hole3 ("Tear Hole 3", Vector) = (0.90, 0.80, 0.06, 0.09)
        _Hole4 ("Tear Hole 4", Vector) = (0.44, 0.74, 0.06, 0.10)

        // One deterministic seed rotates and roughens every ellipse differently on each spawned zombie.
        _RaggedSeed ("Ragged Edge Seed", Float) = 1
        _Cutoff ("Source Alpha Cutoff", Range(0, 1)) = 0.2
        _Glossiness ("Fabric Smoothness", Range(0, 1)) = 0.18
    }

    SubShader
    {
        // Alpha testing writes normal depth and shadows, unlike transparent blending that would reveal sorted shells.
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" }
        LOD 300
        Cull Off
        ZWrite On

        CGPROGRAM
        // The generated shadow caster runs the same clip logic so garment shadows contain the real holes too.
        #pragma surface surf StandardSpecular fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _UpperColor;
        fixed4 _LowerColor;
        fixed4 _AccentColor;
        float4 _Hole0;
        float4 _Hole1;
        float4 _Hole2;
        float4 _Hole3;
        float4 _Hole4;
        float _RaggedSeed;
        half _BumpScale;
        half _Cutoff;
        half _Glossiness;

        struct Input
        {
            // Every cut and colour boundary stays fixed to the authored garment UVs while the Humanoid deforms.
            float2 uv_MainTex;
        };

        float HashSeed(float value)
        {
            // This inexpensive deterministic hash only shapes visuals and does not touch gameplay random state.
            return frac(sin(value * 12.9898 + _RaggedSeed * 0.071) * 43758.5453);
        }

        float2 RotateUv(float2 value, float radians)
        {
            // Rotating the ellipse independently prevents the five tears from reading as stamped axis-aligned ovals.
            float sineValue = sin(radians);
            float cosineValue = cos(radians);
            return float2(
                value.x * cosineValue - value.y * sineValue,
                value.x * sineValue + value.y * cosineValue);
        }

        float EvaluateRaggedHole(float2 uv, float4 hole, float holeIndex)
        {
            // Hole vectors contain centre XY and radius ZW in normalized outfit-atlas coordinates.
            float2 safeRadius = max(hole.zw, float2(0.001, 0.001));

            // Each seed selects a stable rotation of roughly plus or minus thirty-five degrees.
            float angle = (HashSeed(holeIndex * 7.13 + 0.41) - 0.5) * 1.22;
            float2 ellipsePoint = RotateUv(uv - hole.xy, -angle) / safeRadius;
            float radialDistance = length(ellipsePoint);
            float edgeAngle = atan2(ellipsePoint.y, ellipsePoint.x);

            // Two angular waves make coarse ripped fibres instead of a mathematically smooth ellipse boundary.
            float primaryTeeth = sin(
                edgeAngle * (6.0 + floor(HashSeed(holeIndex * 3.17 + 1.9) * 5.0)) +
                _RaggedSeed * 0.13 + holeIndex) * 0.13;
            float secondaryTeeth = sin(edgeAngle * 17.0 - _RaggedSeed * 0.07 + holeIndex * 2.3) * 0.045;
            float raggedBoundary = 0.92 + primaryTeeth + secondaryTeeth;

            // The small transition avoids unstable single-pixel teeth while retaining a hard alpha-tested edge.
            return 1.0 - smoothstep(raggedBoundary - 0.035, raggedBoundary + 0.035, radialDistance);
        }

        void surf(Input input, inout SurfaceOutputStandardSpecular output)
        {
            // Source RGB is almost uniformly navy; only alpha and the separate normal map contain useful garment detail.
            fixed4 sourceCoverage = tex2D(_MainTex, input.uv_MainTex);
            clip(sourceCoverage.a - _Cutoff);

            // Every zombie receives a guaranteed chest tear, two leg tears, one arm tear, and one edge-breaking tear.
            float largestHole = EvaluateRaggedHole(input.uv_MainTex, _Hole0, 0.0);
            largestHole = max(largestHole, EvaluateRaggedHole(input.uv_MainTex, _Hole1, 1.0));
            largestHole = max(largestHole, EvaluateRaggedHole(input.uv_MainTex, _Hole2, 2.0));
            largestHole = max(largestHole, EvaluateRaggedHole(input.uv_MainTex, _Hole3, 3.0));
            largestHole = max(largestHole, EvaluateRaggedHole(input.uv_MainTex, _Hole4, 4.0));
            clip(0.5 - largestHole);

            // UV height separates the trousers from torso panels consistently across front, back, and sleeve islands.
            float brokenWaist = input.uv_MainTex.y + sin(input.uv_MainTex.x * 21.0 + _RaggedSeed) * 0.025;
            float upperBlend = smoothstep(0.57, 0.66, brokenWaist);
            fixed3 garmentColor = lerp(_LowerColor.rgb, _UpperColor.rgb, upperBlend);

            // Sparse irregular accent patches add a third colour without looking like a clean military uniform pattern.
            float accentWave = sin(
                input.uv_MainTex.x * 31.0 +
                input.uv_MainTex.y * 19.0 +
                _RaggedSeed * 0.11) * 0.5 + 0.5;
            float accentMask = smoothstep(0.86, 0.98, accentWave) * 0.72;
            garmentColor = lerp(garmentColor, _AccentColor.rgb, accentMask);

            // Broad grime variation preserves an undead read while leaving the selected colours unmistakably saturated.
            float grimeWave = sin(
                input.uv_MainTex.x * 11.0 -
                input.uv_MainTex.y * 15.0 +
                _RaggedSeed * 0.037) * 0.5 + 0.5;
            float grime = lerp(0.68, 1.02, grimeWave);
            output.Albedo = saturate(garmentColor * grime);

            // The imported normal map retains fabric folds and seams even though the flat source albedo is replaced.
            fixed3 sampledNormal = UnpackNormal(tex2D(_BumpMap, input.uv_MainTex));
            sampledNormal.xy *= _BumpScale;
            sampledNormal.z = sqrt(saturate(1.0 - dot(sampledNormal.xy, sampledNormal.xy)));
            output.Normal = sampledNormal;

            // Cloth remains dielectric and comparatively rough under the game's built-in Standard lighting.
            output.Specular = fixed3(0.035, 0.035, 0.035);
            output.Smoothness = _Glossiness;
            output.Occlusion = 1.0;
            output.Alpha = 1.0;
        }
        ENDCG
    }

    // The fallback preserves source alpha if a legacy platform cannot compile the full Standard surface shader.
    Fallback "Legacy Shaders/Transparent/Cutout/Diffuse"
}
