using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>Lifetime progress. Survives losing a run.</summary>
    [Serializable]
    public class SaveData
    {
        public int miles;
        public List<string> passports = new List<string>();
        public int countriesVisited;
        public int photosTaken;
        public int flightsTaken;
        public int flightsMissed;
        public float secondsPlayed;

        public bool Owns(string countryId) => passports.Contains(countryId);
    }

    /// <summary>JSON save at Application.persistentDataPath/passportfrenzy_save.json.</summary>
    public static class SaveSystem
    {
        const string FileName = "passportfrenzy_save.json";

        public static SaveData Data { get; private set; } = new SaveData();
        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Load()
        {
            Data = LoadFrom(FilePath) ?? new SaveData();
        }

        public static void Save() => SaveTo(FilePath, Data);

        public static void Clear()
        {
            Data = new SaveData();
            Save();
        }

        public static void AddMiles(int amount)
        {
            Data.miles = Mathf.Max(0, Data.miles + amount);
            Save();
            GameEvents.RaiseMilesChanged();
        }

        public static void GrantPassport(string countryId)
        {
            if (Data.Owns(countryId)) return;
            Data.passports.Add(countryId);
            Save();
            GameEvents.RaisePassportGranted(countryId);
        }

        public static string ToJson(SaveData d) => JsonUtility.ToJson(d, true);
        public static SaveData FromJson(string json) => string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<SaveData>(json);

        public static void SaveTo(string path, SaveData d)
        {
            try { File.WriteAllText(path, ToJson(d)); }
            catch (Exception e) { Debug.LogWarning($"[Save] Could not write {path}: {e.Message}"); }
        }

        public static SaveData LoadFrom(string path)
        {
            try { return File.Exists(path) ? FromJson(File.ReadAllText(path)) : null; }
            catch (Exception e) { Debug.LogWarning($"[Save] Could not read {path}: {e.Message}"); return null; }
        }
    }
}
