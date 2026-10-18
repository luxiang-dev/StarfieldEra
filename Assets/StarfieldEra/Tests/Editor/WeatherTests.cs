using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace StarfieldEra.Tests
{
    public sealed class WeatherTests
    {
        [Test] public void QueueKeepsSevenDaysAndTomorrowBecomesTodayAcrossSave()
        {
            var farm=new FarmState();
            for(int day=0;day<15;day++)
            {
                var expected=farm.TomorrowWeather;var old=farm.weather.trueWeatherQueue.Skip(1).ToArray();farm.Sleep();
                Assert.That(farm.Weather,Is.EqualTo(expected));CollectionAssert.AreEqual(old,farm.weather.trueWeatherQueue.Take(6));Assert.That(farm.weather.trueWeatherQueue.Count,Is.EqualTo(7));
                farm=JsonUtility.FromJson<FarmState>(JsonUtility.ToJson(farm));farm.Upgrade();Assert.That(farm.IsValid(),Is.True);
            }
        }
        [TestCase(0,0)][TestCase(1,1)][TestCase(2,2)][TestCase(3,3)]
        public void SkillUnlocksExactlyTheRequestedSlots(int level,int count)
        {
            var w=new WeatherState{weatherSkillLevel=level,seed=45};w.Ensure(1);
            Assert.That(Enumerable.Range(3,3).Count(n=>w.CanPredict(n)),Is.EqualTo(count));
            foreach(int n in new[]{3,4,5})Assert.That(w.Predict(n)!=null,Is.EqualTo(level>=n-2));
        }
        [TestCase(1,3,65)][TestCase(2,3,75)][TestCase(2,4,65)][TestCase(3,3,85)][TestCase(3,4,75)][TestCase(3,5,70)]
        public void PredictionsRoundToFiveAndStayStable(int level,int day,int percent)
        {
            var w=new WeatherState{weatherSkillLevel=level,seed=74};w.Ensure(17);var p=w.Predict(day);var again=w.Predict(day);
            Assert.That(p.mainPercent,Is.EqualTo(percent));Assert.That(p.mainPercent+p.SecondaryPercent,Is.EqualTo(100));Assert.That(p.mainPercent%5,Is.Zero);
            Assert.That(p.main,Is.Not.EqualTo(p.secondary));Assert.That(new[]{p.main,p.secondary},Does.Contain(w.trueWeatherQueue[day-1]));
            Assert.That(again.main,Is.EqualTo(p.main));Assert.That(again.secondary,Is.EqualTo(p.secondary));
            var loaded=JsonUtility.FromJson<WeatherState>(JsonUtility.ToJson(w));Assert.That(loaded.Predict(day).main,Is.EqualTo(p.main));
        }
        [Test] public void MainPredictionCanBeWrongWithExpectedFrequency()
        {
            int correct=0;
            for(int seed=1;seed<=4000;seed++)
            {var w=new WeatherState{seed=seed,weatherSkillLevel=1};w.Ensure(1);w.trueWeatherQueue[2]=WeatherType.HeavyRain;if(w.Predict(3).main==WeatherType.HeavyRain)correct++;}
            Assert.That(correct/4000f,Is.InRange(.62f,.68f));
            Assert.That(WeatherState.Accuracy(0,8),Is.EqualTo(.5));Assert.That(WeatherState.Accuracy(7,3),Is.EqualTo(.9));
        }
        [Test] public void UpgradeIsAtomicAndCappedAtThree()
        {
            var w=new WeatherState{survivalExperience=20,starStones=0};Assert.That(w.TryUpgrade(),Is.False);Assert.That(w.survivalExperience,Is.EqualTo(20));
            w.starStones=1;Assert.That(w.TryUpgrade(),Is.True);Assert.That(w.survivalExperience+w.starStones,Is.Zero);Assert.That(w.weatherSkillLevel,Is.EqualTo(1));
            w.survivalExperience=1000;w.starStones=100;w.TryUpgrade();w.TryUpgrade();int xp=w.survivalExperience,stones=w.starStones;Assert.That(w.TryUpgrade(),Is.False);Assert.That(w.survivalExperience,Is.EqualTo(xp));Assert.That(w.starStones,Is.EqualTo(stones));
        }
        [TestCase(WeatherType.LightRain)][TestCase(WeatherType.HeavyRain)][TestCase(WeatherType.Thunderstorm)]
        public void RainWatersExistingAndNewCrops(WeatherType rain)
        {CheckRain(rain);}
        void CheckRain(WeatherType type)
        {
            var farm=new FarmState();farm.weather.trueWeatherQueue[0]=WeatherType.Cloudy;farm.weather.trueWeatherQueue[1]=type;farm.Plant(0);farm.Sleep();
            Assert.That(farm.plots[0].watered,Is.True);Assert.That(farm.energy,Is.EqualTo(FarmState.MaxEnergy));farm.Plant(1);Assert.That(farm.plots[1].watered,Is.True);
            farm.Sleep();Assert.That(farm.plots[0].growth,Is.EqualTo(1));Assert.That(farm.plots[1].growth,Is.EqualTo(1));
        }
        [Test] public void GrowthBuffPreservesFractionalProgressAndSaveValidity()
        {
            var farm=new FarmState();farm.weather.trueWeatherQueue[0]=WeatherType.Sunny;farm.Plant(0);farm.Water(0);farm.Sleep();
            Assert.That(farm.plots[0].growth,Is.EqualTo(1));Assert.That(farm.plots[0].growthBonus,Is.EqualTo(.1f).Within(.0001f));Assert.That(farm.IsValid(),Is.True);
        }
        [Test] public void MigrationPreservesFarmAndInitialKnownWeather()
        {
            var farm=new FarmState{day=28,coins=71,harvests=8};var json=JsonUtility.ToJson(farm);
            json=System.Text.RegularExpressions.Regex.Replace(json,"\"weather\":\\{[^{}]*\\},?","");
            var path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"weather-migration-"+Guid.NewGuid()+".json");
            try
            {
                System.IO.File.WriteAllText(path,json);Assert.That(FarmSave.TryLoad(path,out var loaded),Is.True);Assert.That(loaded.IsValid(),Is.True);Assert.That(loaded.coins,Is.EqualTo(71));Assert.That(loaded.day,Is.EqualTo(28));Assert.That(loaded.weather.weatherSkillLevel,Is.Zero);
                Assert.That(loaded.Weather,Is.EqualTo(WeatherType.LightRain));Assert.That(loaded.TomorrowWeather,Is.EqualTo(WeatherType.Sunny));
            }
            finally{System.IO.File.Delete(path);}
        }
        [Test] public void InvalidQueueAndSkillAreRejected()
        {
            var farm=new FarmState();farm.weather.trueWeatherQueue[2]=(WeatherType)99;Assert.That(farm.IsValid(),Is.False);
            farm.weather.trueWeatherQueue[2]=WeatherType.Sunny;farm.weather.weatherSkillLevel=4;Assert.That(farm.IsValid(),Is.False);
        }
    }
}
