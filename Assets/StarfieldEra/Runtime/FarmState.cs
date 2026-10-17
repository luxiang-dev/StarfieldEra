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
        public float growthBonus;
        public void Advance(float multiplier){if(!planted||!watered||Ready)return;float progress=growth+growthBonus+multiplier;growth=Math.Min(RequiredDays,(int)Math.Floor(progress+0.00001f));growthBonus=growth>=RequiredDays?0:Math.Max(0,progress-growth);}
        public int RequiredDays => kind==3?5:kind==1?3:kind==2?4:FarmState.GrowthDays;
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
        public const int MaxEnergy=300;
        public const float EnergyRecoveryInterval=5;
        public int energy = MaxEnergy;
        public float energyRecoverySeconds;
        public int selectedCrop;
        public int wheatSeeds, berrySeeds, starSeeds;
        public StormWaterState storm=new StormWaterState();
        public WeatherState weather=new WeatherState();
        public WeatherType Weather {get{weather.Ensure(day);return weather.trueWeatherQueue[0];}}
        public WeatherType TomorrowWeather {get{weather.Ensure(day);return weather.trueWeatherQueue[1];}}
        public bool Rainy => WeatherCatalog.Get(Weather).watersSoil;
        public string Season => new[]{"春季","夏季","秋季","冬季"}[((day-1)/14)%4];
        public int SeedCost => selectedCrop==0?5:selectedCrop==1?8:selectedCrop==2?10:25;
        public int SelectedSeeds => selectedCrop==0?seeds:selectedCrop==1?wheatSeeds:selectedCrop==2?berrySeeds:starSeeds;
        public static readonly string[] CropNames = {"金萝卜","小麦","浆果","星辉南瓜"};
        public bool SpendEnergy(int cost) { if(energy<cost)return false;energy-=cost;return true; }
        public int RestoreEnergy(int amount)
        {
            int restored=Math.Min(MaxEnergy-energy,Math.Max(0,amount));energy+=restored;
            if(energy>=MaxEnergy)energyRecoverySeconds=0;return restored;
        }
        public bool RecoverEnergy(float seconds)
        {
            if(energy>=MaxEnergy){energyRecoverySeconds=0;return false;}
            if(seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return false;
            energyRecoverySeconds+=Math.Min(seconds,MaxEnergy*EnergyRecoveryInterval);
            int points=(int)(energyRecoverySeconds/EnergyRecoveryInterval);
            if(points==0)return false;
            energyRecoverySeconds-=points*EnergyRecoveryInterval;RestoreEnergy(points);return true;
        }
        public void Upgrade()
        {
            if(storm==null)storm=new StormWaterState();
            if(weather==null)weather=new WeatherState();weather.Ensure(day,true);
            if(schema<2){schema=2;energy=MaxEnergy;}
            if(plots!=null && plots.Length==12)
            {var expanded=new CropState[PlotCount];Array.Copy(plots,expanded,12);for(int i=12;i<PlotCount;i++)expanded[i]=new CropState();plots=expanded;}
            if(adventure==null)adventure=new AdventureState();
            adventure.Upgrade();adventure.trade.Ensure(day);
            if(land==null)land=new LandState();land.Upgrade();
            if(forestry==null)forestry=new ForestryState();
            if(mining==null)mining=new MiningState();
        }
        public ForestryState forestry=new ForestryState();
        public MiningState mining=new MiningState();
        public LandState land = new LandState();
        public AdventureState adventure = new AdventureState();
        public float playerX = -1;
        public float playerZ = -3;
        public CropState[] plots = new CropState[PlotCount];

        public FarmState()
        {
            for (int i = 0; i < plots.Length; i++) plots[i] = new CropState();
            weather.Ensure(day);
        }

        public int AvailablePlotCount=>adventure.expandedFarm?PlotCount:12;
        public bool IsPlotUnlocked(int index)=>index>=0&&index<AvailablePlotCount;

        public bool Plant(int index)
        {
            if (!IsPlotUnlocked(index) || plots[index].planted || SelectedSeeds <= 0 || energy<1) return false;
            plots[index].planted = true;
            plots[index].kind = selectedCrop;
            plots[index].watered = Rainy;
            SpendEnergy(1);
            if(selectedCrop==0)seeds--;else if(selectedCrop==1)wheatSeeds--;else if(selectedCrop==2)berrySeeds--;else starSeeds--;
            return true;
        }

        public bool Water(int index)
        {
            if (!IsPlotUnlocked(index) || !plots[index].planted || plots[index].watered || plots[index].Ready || energy<1) return false;
            SpendEnergy(1);
            plots[index].watered = true;
            return true;
        }

        public bool Harvest(int index)
        {
            if (!IsPlotUnlocked(index) || !plots[index].Ready) return false;
            int kind=plots[index].kind;
            if(!SpendEnergy(1))return false;
            if(kind==2){plots[index].growth=0;plots[index].growthBonus=0;plots[index].watered=Rainy;adventure.berries+=2;}
            else plots[index] = new CropState();
            coins += kind==0?HarvestPrice:kind==1?24:kind==2?18:60;
            if(kind==3)adventure.village.pantry+=3;
            if(kind==0)adventure.homestead.carrots+=2;
            harvests++;weather.survivalExperience+=2;
            return true;
        }

        public bool BuySeed()
        {
            if (selectedCrop==3||coins < SeedCost) return false;
            coins -= SeedCost;
            if(selectedCrop==0)seeds++;else if(selectedCrop==1)wheatSeeds++;else berrySeeds++;
            return true;
        }

        public void Sleep()
        {
            foreach (var crop in plots)
            {
                crop.Advance(WeatherCatalog.Get(Weather).cropGrowthMultiplier);
                crop.watered = false;
            }
            day++; energy=MaxEnergy;energyRecoverySeconds=0;weather.Ensure(day);weather.survivalExperience++;
            adventure?.NewDay(this);
            if(Rainy)foreach(var crop in plots)if(crop.planted&&!crop.Ready)crop.watered=true;
        }

        bool ValidIndex(int index) => index >= 0 && index < PlotCount;

        public bool IsValid()
        {
            if(storm==null||!storm.Valid()||weather==null||!weather.Valid(day)||forestry==null||!forestry.Valid()||mining==null||!mining.Valid())return false;
            if(float.IsNaN(energyRecoverySeconds)||float.IsInfinity(energyRecoverySeconds)||energyRecoverySeconds<0||energyRecoverySeconds>=EnergyRecoveryInterval)return false;
            if(land!=null&&!land.Valid())return false;
            if (adventure != null && !adventure.Valid()) return false;
            if (schema!=2 || energy<0 || energy>MaxEnergy || selectedCrop<0 || selectedCrop>3 || wheatSeeds<0 || berrySeeds<0 || starSeeds<0 || version != 1 || day < 1 || coins < 0 || seeds < 0 || harvests < 0 ||
                plots == null || plots.Length != PlotCount || float.IsNaN(playerX) || float.IsNaN(playerZ) ||
                float.IsInfinity(playerX) || float.IsInfinity(playerZ)) return false;
            foreach (var crop in plots)
                if (crop == null || crop.growth < 0 || crop.growth > crop.RequiredDays || crop.kind<0 || crop.kind>3 || float.IsNaN(crop.growthBonus)||float.IsInfinity(crop.growthBonus)||crop.growthBonus<0||crop.growthBonus>=1 ||
                    (!crop.planted && (crop.watered || crop.growth != 0 || crop.growthBonus!=0))) return false;
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
                var json=File.ReadAllText(path);
                var candidate = JsonUtility.FromJson<FarmState>(json);
                if(candidate!=null&&!System.Text.RegularExpressions.Regex.IsMatch(json,@"""weather""\s*:"))candidate.weather=null;
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
