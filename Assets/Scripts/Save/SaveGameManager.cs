using System.IO;
using UnityEngine;

namespace LaneSurvivor.Save
{
    public static class SaveGameManager
    {
        private const string SaveFileName = "lane_survivor_save.json";

        private static string customSavePath;

        public static string SavePath => string.IsNullOrEmpty(customSavePath)
            ? Path.Combine(Application.persistentDataPath, SaveFileName)
            : customSavePath;

        public static SaveGameData Load()
        {
            // Missing save files are expected for first launch, so create normalized default data.
            if (!File.Exists(SavePath))
            {
                return CreateFreshData();
            }

            try
            {
                // JsonUtility is enough for this small local-only save file and avoids extra dependencies.
                string json = File.ReadAllText(SavePath);
                SaveGameData loadedData = string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonUtility.FromJson<SaveGameData>(json);
                loadedData ??= new SaveGameData();
                loadedData.Normalize();
                return loadedData;
            }
            catch (System.Exception exception) when (exception is IOException || exception is System.ArgumentException)
            {
                // A corrupt or partially written save should repair to defaults instead of bricking scene startup.
                return CreateFreshData();
            }
        }

        public static void Save(SaveGameData data)
        {
            // Normalize before writing so persisted data stays inside the supported range.
            data ??= new SaveGameData();
            data.Normalize();

            // Ensure the persistent directory exists before writing a fresh install save file.
            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Pretty JSON makes manual inspection easier during prototype iteration.
            string json = JsonUtility.ToJson(data, true);
            string tempPath = $"{SavePath}.tmp";
            File.WriteAllText(tempPath, json);

            // Replace the save from a fully written temp file to reduce chances of a truncated save.
            if (File.Exists(SavePath))
            {
                File.Replace(tempPath, SavePath, null);
            }
            else
            {
                File.Move(tempPath, SavePath);
            }
        }

        public static SaveGameData ResetToFreshData()
        {
            // Create normalized defaults so reset uses the same first-launch values as a missing save.
            SaveGameData freshData = CreateFreshData();

            // Persist the defaults immediately so the next scene reads the reset state.
            Save(freshData);
            return freshData;
        }

        public static void UseCustomSavePathForTests(string path)
        {
            // Tests point the manager at a temp file so they never touch the developer's real save.
            customSavePath = path;
        }

        public static void ClearCustomSavePathForTests()
        {
            // Restore production behavior after a test fixture finishes.
            customSavePath = null;
        }

        private static SaveGameData CreateFreshData()
        {
            // Centralize default construction so missing and invalid files recover identically.
            SaveGameData freshData = new();
            freshData.Normalize();
            return freshData;
        }
    }
}
