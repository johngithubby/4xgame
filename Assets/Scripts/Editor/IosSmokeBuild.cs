using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace LaneSurvivor.Editor
{
    public static class IosSmokeBuild
    {
        public static void Run()
        {
            // Allow CI or local callers to choose a disposable build output folder.
            string outputPath = Environment.GetEnvironmentVariable("LANE_SURVIVOR_IOS_BUILD_PATH");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = "/private/tmp/4xgame-ios-smoke-build";
            }

            // A local environment flag lets deployment scripts choose the simulator SDK without a second build method.
            bool useSimulatorSdk = string.Equals(Environment.GetEnvironmentVariable("LANE_SURVIVOR_IOS_SIMULATOR"), "1", StringComparison.Ordinal);

            // A second local flag lets Apple Silicon simulator deploys request the architecture iOS 26 accepts.
            string simulatorArchitectureName = Environment.GetEnvironmentVariable("LANE_SURVIVOR_IOS_SIMULATOR_ARCH");

            // Preserve the editor's SDK preference so smoke builds do not permanently alter local project state.
            iOSSdkVersion previousSdkVersion = PlayerSettings.iOS.sdkVersion;

            // Preserve the simulator CPU preference for the same reason as the SDK setting.
            AppleMobileArchitectureSimulator previousSimulatorArchitecture = PlayerSettings.iOS.simulatorSdkArchitecture;

            try
            {
                // Simulator exports are required for local deploys to CoreSimulator devices.
                if (useSimulatorSdk)
                {
                    PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
                }

                // Only override simulator architecture when callers explicitly request one.
                if (!string.IsNullOrWhiteSpace(simulatorArchitectureName))
                {
                    // Unity's enum parser accepts ARM64, Universal, and X86_64, matching the editor setting names.
                    PlayerSettings.iOS.simulatorSdkArchitecture = Enum.Parse<AppleMobileArchitectureSimulator>(simulatorArchitectureName, true);
                }

                // Build only enabled scenes so the smoke build follows project build settings.
                string[] scenePaths = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray();

                // Visual verification exports can start directly in Minigame while normal builds keep Base first.
                scenePaths = ApplyRequestedStartScene(scenePaths, Environment.GetEnvironmentVariable("LANE_SURVIVOR_IOS_START_SCENE"));

                // Ensure Unity can create the output path's parent directory.
                string parentDirectory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(parentDirectory))
                {
                    Directory.CreateDirectory(parentDirectory);
                }

                // Development builds are enough for compile/link validation in this prototype phase.
                BuildPlayerOptions options = new()
                {
                    scenes = scenePaths,
                    locationPathName = outputPath,
                    target = BuildTarget.iOS,
                    options = BuildOptions.Development
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException($"iOS smoke build failed: {report.summary.result}");
                }
            }
            finally
            {
                // Restore the previous SDK selection even when BuildPipeline throws.
                PlayerSettings.iOS.sdkVersion = previousSdkVersion;

                // Restore the previous architecture selection even when BuildPipeline throws.
                PlayerSettings.iOS.simulatorSdkArchitecture = previousSimulatorArchitecture;
            }
        }

        private static string[] ApplyRequestedStartScene(string[] scenePaths, string requestedStartScene)
        {
            if (string.IsNullOrWhiteSpace(requestedStartScene))
            {
                return scenePaths;
            }

            // Accept either a scene asset path or a bare scene name so local scripts stay readable.
            int requestedSceneIndex = Array.FindIndex(scenePaths, scenePath => IsRequestedScene(scenePath, requestedStartScene));
            if (requestedSceneIndex < 0)
            {
                throw new InvalidOperationException($"Requested iOS start scene was not enabled in build settings: {requestedStartScene}");
            }

            // Unity launches the first scene in the build list, so move only the requested scene to the front.
            return scenePaths
                .Skip(requestedSceneIndex)
                .Take(1)
                .Concat(scenePaths.Where((_, index) => index != requestedSceneIndex))
                .ToArray();
        }

        private static bool IsRequestedScene(string scenePath, string requestedStartScene)
        {
            // Full path matching supports exact EditorBuildSettings scene values.
            if (string.Equals(scenePath, requestedStartScene, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Bare scene names keep command-line verification exports concise.
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            return string.Equals(sceneName, requestedStartScene, StringComparison.OrdinalIgnoreCase);
        }
    }
}
