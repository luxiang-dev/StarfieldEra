using NUnit.Framework;
using UnityEngine;
namespace StarfieldEra.Tests
{
    public class SurvivalAndClockTests
    {
        [Test] public void ClockPassesSunsetAndWrapsIntoNextDay()
        {
            var c=new WorldClockState();Assert.That(c.Display,Is.EqualTo("06:00"));Assert.That(c.Tick(600),Is.Zero);Assert.That(c.IsNight,Is.True);
            Assert.That(c.Tick(600),Is.EqualTo(1));Assert.That(c.Display,Is.EqualTo("06:00"));Assert.That(c.Valid(),Is.True);
        }
        [Test] public void AutomaticDayRunsDailyEconomyWithoutRestoringHealthOrStamina()
        {
            var f=new FarmState();f.energy=20;f.survival.health=40;f.plots[0].planted=true;f.plots[0].watered=true;
            f.AdvanceDay(false);Assert.That(f.day,Is.EqualTo(2));Assert.That(f.energy,Is.EqualTo(20));Assert.That(f.survival.health,Is.EqualTo(40));Assert.That(f.plots[0].growth,Is.GreaterThan(0));
            f.Sleep();Assert.That(f.day,Is.EqualTo(3));Assert.That(f.energy,Is.EqualTo(300));Assert.That(f.survival.health,Is.EqualTo(100));Assert.That(f.IsValid(),Is.True);
        }
        [Test] public void DamageHasGracePeriodAndHealingWaitsUntilOutOfCombat()
        {
            var h=new SurvivalState();Assert.That(h.Damage(20),Is.True);Assert.That(h.Damage(20),Is.False);h.Tick(14);Assert.That(h.health,Is.EqualTo(80));h.Tick(3);Assert.That(h.health,Is.EqualTo(81));h.Tick(1);Assert.That(h.Damage(200),Is.True);h.Tick(20);Assert.That(h.health,Is.Zero);Assert.That(h.Valid(),Is.True);
        }
        [Test] public void ShooterSelectsOnlyLivingZombiesInRangeAndBuildingSpendsOnce()
        {
            var f=new FarmState();f.adventure.wood=100;f.adventure.stone=100;f.coins=100;
            var a=f.garden.Build(f,0,Vector3.zero);Assert.That(a,Is.Not.Null);Assert.That(f.adventure.wood,Is.EqualTo(92));Assert.That(f.garden.Build(f,0,Vector3.zero),Is.Null);Assert.That(f.adventure.wood,Is.EqualTo(92));
            var near=new GardenZombie{id=1,x=5};var far=new GardenZombie{id=2,x=20};f.garden.zombies.AddRange(new[]{near,far});f.garden.nextId=3;
            Assert.That(GardenDefenseState.Target(Vector3.zero,f.garden.zombies,12),Is.SameAs(near));near.health=0;Assert.That(GardenDefenseState.Target(Vector3.zero,f.garden.zombies,12),Is.Null);
        }
    }
}
