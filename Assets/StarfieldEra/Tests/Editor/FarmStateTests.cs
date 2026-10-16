using System;
using System.IO;
using NUnit.Framework;

namespace StarfieldEra.Tests
{
    public class FarmStateTests
    {
        [Test]
        public void HarvestRequiresTwoWateredNightsAndPaysOnlyOnce()
        {
            var farm = new FarmState();
            for(int i=0;i<7;i++)farm.weather.trueWeatherQueue[i]=WeatherType.Cloudy;
            Assert.That(farm.Plant(0), Is.True);
            Assert.That(farm.Plant(0), Is.False);
            Assert.That(farm.seeds, Is.EqualTo(11));
            farm.Sleep();
            Assert.That(farm.plots[0].growth, Is.Zero, "Dry days must not advance growth.");
            Assert.That(farm.Water(0), Is.True);
            Assert.That(farm.Water(0), Is.False);
            farm.Sleep();
            Assert.That(farm.plots[0].watered, Is.False);
            Assert.That(farm.Harvest(0), Is.False);
            farm.Sleep();
            Assert.That(farm.plots[0].growth, Is.EqualTo(1));
            farm.Water(0);
            farm.Sleep();
            Assert.That(farm.Harvest(0), Is.True);
            Assert.That(farm.Harvest(0), Is.False);
            Assert.That(farm.coins, Is.EqualTo(32));
            Assert.That(farm.harvests, Is.EqualTo(1));
            Assert.That(farm.plots[0].planted, Is.False);
        }

        [Test]
        public void EmptyPlotsAndInsufficientResourcesCannotBeSpent()
        {
            var farm = new FarmState { seeds = 0, coins = 4 };
            Assert.That(farm.Plant(0), Is.False);
            Assert.That(farm.Water(0), Is.False);
            Assert.That(farm.BuySeed(), Is.False);
            Assert.That(farm.Plant(-1), Is.False);
            Assert.That(farm.Water(12), Is.False);
            Assert.That(farm.Harvest(99), Is.False);
            Assert.That(farm.coins, Is.EqualTo(4));
            farm.coins = 5;
            Assert.That(farm.BuySeed(), Is.True);
            Assert.That(farm.coins, Is.Zero);
            Assert.That(farm.seeds, Is.EqualTo(1));
        }

        [Test]
        public void SaveRoundTripPreservesGrowthAndRecoversCorruptedPrimary()
        {
            string folder = Path.Combine(Path.GetTempPath(), "StarfieldEra-Test-" + Guid.NewGuid());
            string path = Path.Combine(folder, "farm.json");
            try
            {
                var farm = new FarmState();
                farm.Plant(3); farm.Water(3); farm.Sleep();
                farm.playerX = 3.5f;
                FarmSave.Save(path, farm);
                Assert.That(FarmSave.TryLoad(path, out var restored), Is.True);
                Assert.That(restored.plots[3].growth, Is.EqualTo(1));
                Assert.That(restored.seeds, Is.EqualTo(11));
                Assert.That(restored.day, Is.EqualTo(2));
                Assert.That(restored.playerX, Is.EqualTo(3.5f));
                farm.Sleep(); FarmSave.Save(path, farm);
                File.WriteAllText(path, "{broken json");
                Assert.That(FarmSave.TryLoad(path, out restored), Is.True);
                Assert.That(restored.day, Is.EqualTo(2), "Recover last known-good backup.");
                File.WriteAllText(path + ".bak", "{\"version\":99}");
                Assert.That(FarmSave.TryLoad(path, out _), Is.False);
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
    }
}
