using NUnit.Framework;
using UnityEngine;
namespace StarfieldEra.Tests
{
    public class EnergyTests
    {
        [Test] public void NewGameAndSleepUseNewMaximum()
        {var s=new FarmState();Assert.That(s.energy,Is.EqualTo(300));s.energy=0;s.energyRecoverySeconds=4;s.Sleep();Assert.That(s.energy,Is.EqualTo(300));Assert.That(s.energyRecoverySeconds,Is.Zero);Assert.That(s.IsValid(),Is.True);}
        [Test] public void RecoveryAccumulatesFiveSecondsWithoutChangingDay()
        {
            var s=new FarmState{energy=20};int day=s.day;Assert.That(s.RecoverEnergy(2),Is.False);Assert.That(s.energy,Is.EqualTo(20));Assert.That(s.RecoverEnergy(3),Is.True);Assert.That(s.energy,Is.EqualTo(21));s.RecoverEnergy(11);Assert.That(s.energy,Is.EqualTo(23));Assert.That(s.energyRecoverySeconds,Is.EqualTo(1));Assert.That(s.day,Is.EqualTo(day));
        }
        [Test] public void FullEnergyCannotBankRecoveryTime()
        {
            var s=new FarmState{energy=299};s.RecoverEnergy(10000);Assert.That(s.energy,Is.EqualTo(300));Assert.That(s.energyRecoverySeconds,Is.Zero);s.RecoverEnergy(40);s.SpendEnergy(1);Assert.That(s.RecoverEnergy(4),Is.False);Assert.That(s.energy,Is.EqualTo(299));Assert.That(s.RecoverEnergy(1),Is.True);
        }
        [Test] public void FoodCanRestoreAboveOldCapAndStopsAtNewCap()
        {
            var s=new FarmState{energy=40};Assert.That(s.RestoreEnergy(12),Is.EqualTo(12));Assert.That(s.energy,Is.EqualTo(52));s.energy=295;s.energyRecoverySeconds=3;Assert.That(s.RestoreEnergy(15),Is.EqualTo(5));Assert.That(s.energy,Is.EqualTo(300));Assert.That(s.energyRecoverySeconds,Is.Zero);
        }
        [Test] public void SaveKeepsPartialRecoveryAndOldEnergyValue()
        {
            var s=new FarmState{energy=25};s.RecoverEnergy(4);var copy=JsonUtility.FromJson<FarmState>(JsonUtility.ToJson(s));copy.Upgrade();Assert.That(copy.energy,Is.EqualTo(25));Assert.That(copy.RecoverEnergy(1),Is.True);Assert.That(copy.energy,Is.EqualTo(26));Assert.That(copy.IsValid(),Is.True);
        }
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(-1f)]
        public void InvalidDeltaDoesNotCorruptEnergy(float seconds)
        {var s=new FarmState{energy=1};Assert.That(s.RecoverEnergy(seconds),Is.False);Assert.That(s.energy,Is.EqualTo(1));Assert.That(s.IsValid(),Is.True);}
        [Test] public void SaveValidationRejectsInvalidEnergyOrTimer()
        {var s=new FarmState{energy=301};Assert.That(s.IsValid(),Is.False);s.energy=300;s.energyRecoverySeconds=5;Assert.That(s.IsValid(),Is.False);s.energyRecoverySeconds=float.NaN;Assert.That(s.IsValid(),Is.False);}
    }
}
