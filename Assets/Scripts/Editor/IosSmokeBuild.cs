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

            // Build only enabled scenes so the smoke build follows project build settings.
            string[] scenePaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

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
    }
}
