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
        public int kind;
        public int RequiredDays => kind==1?3:kind==2?4:FarmState.GrowthDays;
        public bool Ready => planted && growth >= RequiredDays;
    }

    [Serializable]
    public sealed class FarmState
    {
        public const int PlotCount = 24;
        public const int GrowthDays = 2;
        public const int SeedPrice = 5;
        public const int HarvestPrice = 12;
        public int version = 1;
        public int day = 1;
        public int coins = 20;
        public int seeds = 12;
        public int harvests;
        public int schema = 2;
        public int energy = 30;
        public int selectedCrop;
        public int wheatSeeds, berrySeeds;
        public bool Rainy => day % 4 == 0;
        public string Season => new[]{"春季","夏季","秋季","冬季"}[((day-1)/14)%4];
        public int SeedCost => selectedCrop==0?5:selectedCrop==1?8:10;
        public int SelectedSeeds => selectedCrop==0?seeds:selectedCrop==1?wheatSeeds:berrySeeds;
        public static readonly string[] CropNames = {"金萝卜","小麦","浆果"};
        public bool SpendEnergy(int cost) { if(energy<cost)return false;energy-=cost;return true; }
        public void Upgrade()
        {
            if(schema<2){schema=2;energy=30;}
            if(plots!=null && plots.Length==12)
            {var expanded=new CropState[PlotCount];Array.Copy(plots,expanded,12);for(int i=12;i<PlotCount;i++)expanded[i]=new CropState();plots=expanded;}
            if(adventure==null)adventure=new AdventureState();
            adventure.Upgrade();
            if(land==null)land=new LandState();land.Upgrade();
        }
        public LandState land = new LandState();
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
            if (!ValidIndex(index) || plots[index].planted || SelectedSeeds <= 0 || (index>=12 && !adventure.expandedFarm) || energy<1) return false;
            plots[index].planted = true;
            plots[index].kind = selectedCrop;
            plots[index].watered = Rainy;
            SpendEnergy(1);
            if(selectedCrop==0)seeds--;else if(selectedCrop==1)wheatSeeds--;else berrySeeds--;
            return true;
        }

        public bool Water(int index)
        {
            if (!ValidIndex(index) || !plots[index].planted || plots[index].watered || plots[index].Ready || energy<1) return false;
            SpendEnergy(1);
            plots[index].watered = true;
            return true;
        }

        public bool Harvest(int index)
        {
            if (!ValidIndex(index) || !plots[index].Ready) return false;
            int kind=plots[index].kind;
            if(!SpendEnergy(1))return false;
            if(kind==2){plots[index].growth=0;plots[index].watered=Rainy;adventure.berries+=2;}
            else plots[index] = new CropState();
            coins += kind==0?HarvestPrice:kind==1?24:18;
            harvests++;
            return true;
        }

        public bool BuySeed()
        {
            if (coins < SeedCost) return false;
            coins -= SeedCost;
            if(selectedCrop==0)seeds++;else if(selectedCrop==1)wheatSeeds++;else berrySeeds++;
            return true;
        }

        public void Sleep()
        {
            foreach (var crop in plots)
            {
                if (crop.planted && crop.watered) crop.growth = Math.Min(crop.RequiredDays, crop.growth + 1);
                crop.watered = false;
            }
            day++; energy=30;
            adventure?.NewDay(this);
            if(Rainy)foreach(var crop in plots)if(crop.planted&&!crop.Ready)crop.watered=true;
        }

        bool ValidIndex(int index) => index >= 0 && index < PlotCount;

        public bool IsValid()
        {
            if(land!=null&&!land.Valid())return false;
            if (adventure != null && !adventure.Valid()) return false;
            if (schema!=2 || energy<0 || energy>30 || selectedCrop<0 || selectedCrop>2 || wheatSeeds<0 || berrySeeds<0 || version != 1 || day < 1 || coins < 0 || seeds < 0 || harvests < 0 ||
                plots == null || plots.Length != PlotCount || float.IsNaN(playerX) || float.IsNaN(playerZ) ||
                float.IsInfinity(playerX) || float.IsInfinity(playerZ)) return false;
            foreach (var crop in plots)
                if (crop == null || crop.growth < 0 || crop.growth > crop.RequiredDays || crop.kind<0 || crop.kind>2 ||
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
                if (candidate == null) return false;
                candidate.Upgrade();
                if (!candidate.IsValid()) return false;
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
