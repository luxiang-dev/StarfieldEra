using NUnit.Framework;
using UnityEngine;
namespace StarfieldEra.Tests
{
    public sealed class FreeBuildingTests
    {
        [Test] public void LegacyTownAndNewBuildingsSurviveRepeatedMigrationWithoutDoubleCounting()
        {
            var farm=new FarmState();farm.adventure.workshop=true;farm.Upgrade();
            farm.adventure.buildings.Add(new PlacedBuilding{kind=6,column=20,row=-20,footprintVersion=1});
            farm.adventure.ReconcileTown();
            var copy=JsonUtility.FromJson<FarmState>(JsonUtility.ToJson(farm));copy.Upgrade();copy.Upgrade();
            Assert.That(copy.IsValid(),Is.True);Assert.That(copy.adventure.TownCount(6),Is.EqualTo(2));
            copy.Sleep();Assert.That(copy.adventure.wood,Is.EqualTo(12));
            copy.adventure.buildings.Clear();copy.adventure.ReconcileTown();
            Assert.That(copy.adventure.workshop,Is.True);Assert.That(copy.adventure.TownCount(6),Is.EqualTo(1));
        }
        [Test] public void RemovingLastFreeWorkshopDoesNotSpawnAFixedWorkshopOrKeepProducing()
        {
            var farm=new FarmState();farm.Upgrade();var a=farm.adventure;
            a.buildings.Add(new PlacedBuilding{kind=6,column=20,row=-20,footprintVersion=1});a.ReconcileTown();
            Assert.That(a.workshop,Is.True);Assert.That(a.FixedTown(6),Is.False);
            a.buildings.Clear();a.ReconcileTown();farm.Sleep();
            Assert.That(a.workshop,Is.False);Assert.That(a.wood,Is.Zero);
        }
        [Test] public void BuildingsPayPerInstanceAndFunctionalBuildingsProduceSupplies()
        {
            var farm=new FarmState();farm.Upgrade();var a=farm.adventure;
            foreach(int kind in new[]{2,3,6,6,7,8,9,10,11})a.buildings.Add(new PlacedBuilding{kind=kind});
            a.ReconcileTown();a.NewDay(farm);
            Assert.That(a.wood,Is.EqualTo(12));Assert.That(a.science,Is.EqualTo(21));
            Assert.That(farm.coins,Is.EqualTo(48));Assert.That(a.meals,Is.EqualTo(2));Assert.That(a.homestead.carrots,Is.EqualTo(3));
        }
        [Test] public void AllNewBuildingKindsValidateAtAllRotationsAndRejectOverlap()
        {
            for(int kind=6;kind<12;kind++)for(int rotation=0;rotation<6;rotation++)
            {
                var farm=new FarmState();var b=new PlacedBuilding{kind=kind,column=20,row=-20,rotation=rotation,footprintVersion=1};farm.adventure.buildings.Add(b);
                Assert.That(BuildingFootprints.Cells(b).Count,Is.EqualTo(BuildingFootprints.Counts[kind]));Assert.That(farm.IsValid(),Is.True);
                farm.adventure.buildings.Add(new PlacedBuilding{kind=kind,column=20,row=-20,rotation=rotation,footprintVersion=1});Assert.That(farm.IsValid(),Is.False);
            }
        }
    }
}
