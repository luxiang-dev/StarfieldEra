using System;
using System.IO;
using UnityEngine;

namespace StarfieldEra
{
    [Serializable]
    public sealed class CropState
    {
        public bool planted;
        public bool watered;
        public int growth;
        public bool Ready => planted && growth >= FarmState.GrowthDays;
    }

    [Serializable]
    public sealed class FarmState
    {
        public const int PlotCount = 12;
        public const int GrowthDays = 2;
        public const int SeedPrice = 5;
        public const int HarvestPrice = 12;
        public int version = 1;
        public int day = 1;
        public int coins = 20;
        public int seeds = 12;
        public int harvests;
        public AdventureState adventure = new AdventureState();
        public float playerX = -1;
        public float playerZ = -3;
        public CropState[] plots = new CropState[PlotCount];

        public FarmState()
        {
            for (int i = 0; i < plots.Length; i++) plots[i] = new CropState();
        }

        public bool Plant(int index)
        {
            if (!ValidIndex(index) || plots[index].planted || seeds <= 0) return false;
            plots[index].planted = true;
            seeds--;
            return true;
        }

        public bool Water(int index)
        {
            if (!ValidIndex(index) || !plots[index].planted || plots[index].watered || plots[index].Ready) return false;
            plots[index].watered = true;
            return true;
        }

        public bool Harvest(int index)
        {
            if (!ValidIndex(index) || !plots[index].Ready) return false;
            plots[index] = new CropState();
            coins += HarvestPrice;
            harvests++;
            return true;
        }

        public bool BuySeed()
        {
            if (coins < SeedPrice) return false;
            coins -= SeedPrice;
            seeds++;
            return true;
        }

        public void Sleep()
        {
            foreach (var crop in plots)
            {
                if (crop.planted && crop.watered) crop.growth = Math.Min(GrowthDays, crop.growth + 1);
                crop.watered = false;
            }
            day++;
            adventure?.NewDay(this);
        }

        bool ValidIndex(int index) => index >= 0 && index < PlotCount;

        public bool IsValid()
        {
            if (adventure != null && !adventure.Valid()) return false;
            if (version != 1 || day < 1 || coins < 0 || seeds < 0 || harvests < 0 ||
                plots == null || plots.Length != PlotCount || float.IsNaN(playerX) || float.IsNaN(playerZ) ||
                float.IsInfinity(playerX) || float.IsInfinity(playerZ)) return false;
            foreach (var crop in plots)
                if (crop == null || crop.growth < 0 || crop.growth > GrowthDays ||
                    (!crop.planted && (crop.watered || crop.growth != 0))) return false;
            return true;
        }
    }

    public static class FarmSave
    {
        public static bool TryLoad(string path, out FarmState state)
        {
            return Read(path, out state) || Read(path + ".bak", out state);
        }

        static bool Read(string path, out FarmState state)
        {
            state = null;
            try
            {
                if (!File.Exists(path)) return false;
                var candidate = JsonUtility.FromJson<FarmState>(File.ReadAllText(path));
                if (candidate == null || !candidate.IsValid()) return false;
                if (candidate.adventure == null) candidate.adventure = new AdventureState();
                state = candidate;
                return true;
            }
            catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException)
            {
                return false;
            }
        }

        public static void Save(string path, FarmState state)
        {
            if (state == null || !state.IsValid()) throw new ArgumentException("Invalid farm state.");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(state, true));
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }
    }
}
