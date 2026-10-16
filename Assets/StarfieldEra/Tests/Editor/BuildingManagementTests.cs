using NUnit.Framework;
using UnityEngine;
namespace StarfieldEra.Tests
{
    public sealed class BuildingManagementTests
    {
        static PlacedBuilding Workshop(FarmState farm,int x=20)
        {var b=new PlacedBuilding{kind=6,column=x,row=-20,footprintVersion=1,operation=new ResourceSiteState{level=1}};farm.adventure.buildings.Add(b);farm.adventure.ReconcileTown();return b;}
        [Test] public void WorkshopKeepsAutomaticIncomeAndConservesProcessingMaterials()
        {
            var farm=new FarmState();farm.Upgrade();var b=Workshop(farm);farm.adventure.wood=20;b.operation.workers=2;
            farm.Sleep();Assert.That(farm.adventure.wood,Is.EqualTo(18));Assert.That(b.operation.stock,Is.EqualTo(4));
            BuildingOperations.CollectWorkshop(farm.adventure,b.operation);Assert.That(farm.adventure.homestead.planks,Is.EqualTo(4));Assert.That(b.operation.stock,Is.Zero);
        }
        [Test] public void WorkshopCannotProduceWithoutInputOrOverflowStorage()
        {
            var farm=new FarmState();farm.Upgrade();var b=Workshop(farm);b.operation.level=2;b.operation.workers=2;b.operation.processing=true;farm.adventure.stone=3;
            farm.Sleep();Assert.That(b.operation.goods,Is.EqualTo(1));Assert.That(farm.adventure.stone,Is.EqualTo(1));
            b.operation.goods=30;farm.adventure.stone=100;farm.Sleep();Assert.That(b.operation.goods,Is.EqualTo(30));Assert.That(farm.adventure.stone,Is.EqualTo(100));
        }
        [Test] public void WorkshopAndQuarryShareWorkerCapacityAndAssignmentsCannotOverbook()
        {
            var farm=new FarmState();farm.Upgrade();var b=Workshop(farm);var h=farm.adventure.homestead;h.quarry.level=1;
            Assert.That(h.Assign(farm.adventure,h.quarry,1),Is.True);Assert.That(h.Assign(farm.adventure,h.quarry,1),Is.True);
            Assert.That(BuildingOperations.AssignWorkshop(farm.adventure,b.operation,1),Is.True);
            Assert.That(BuildingOperations.AssignWorkshop(farm.adventure,b.operation,1),Is.False);Assert.That(h.UsedWorkers(farm.adventure),Is.EqualTo(3));
        }
        [Test] public void WorkshopUpgradeIsAtomicAndReportsExactMaterialShortage()
        {
            var farm=new FarmState();farm.Upgrade();var b=Workshop(farm);farm.adventure.wood=7;farm.adventure.stone=6;farm.coins=20;
            Assert.That(BuildingOperations.UpgradeBlock(farm,b.operation),Does.Contain("木材差1"));Assert.That(BuildingOperations.ImproveWorkshop(farm,b.operation),Is.False);
            Assert.That(farm.adventure.stone,Is.EqualTo(6));farm.adventure.wood=8;Assert.That(BuildingOperations.ImproveWorkshop(farm,b.operation),Is.True);
            Assert.That(b.operation.level,Is.EqualTo(2));Assert.That(farm.adventure.wood,Is.Zero);Assert.That(farm.adventure.stone,Is.Zero);Assert.That(farm.coins,Is.Zero);
        }
        [Test] public void ProductionForecastMatchesNextDayIncludingFullStorageAndDowntime()
        {
            foreach(bool processing in new[]{true,false})foreach(int stock in new[]{0,29,30})foreach(int downtime in new[]{0,1})
            {var s=new ResourceSiteState{level=2,workers=2,stock=stock,goods=29,processing=processing,downtime=downtime};var prediction=BuildingOperations.Forecast(s,30);s.NewDay(30);Assert.That(prediction.stock,Is.EqualTo(s.stock));Assert.That(prediction.goods,Is.EqualTo(s.goods));Assert.That(prediction.downtime,Is.EqualTo(s.downtime));}
        }
        [Test] public void LegacyWorkshopsUpgradeOnceAndKeepWorkersAndStockAcrossReload()
        {
            var farm=new FarmState();farm.Upgrade();var b=Workshop(farm);b.operation=null;farm.Upgrade();b.operation.workers=1;b.operation.stock=7;
            var copy=JsonUtility.FromJson<FarmState>(JsonUtility.ToJson(farm));copy.Upgrade();copy.Upgrade();
            Assert.That(copy.IsValid(),Is.True);Assert.That(copy.adventure.buildings[0].operation.stock,Is.EqualTo(7));Assert.That(copy.adventure.homestead.UsedWorkers(copy.adventure),Is.EqualTo(1));
        }
        [Test] public void NoWorkerOrMaterialsCanBeSpentOnAnotherBuildingsOperation()
        {
            var farm=new FarmState();farm.Upgrade();Workshop(farm);var foreign=new ResourceSiteState{level=1};farm.adventure.wood=farm.adventure.stone=100;farm.coins=100;
            Assert.That(BuildingOperations.AssignWorkshop(farm.adventure,foreign,1),Is.False);Assert.That(BuildingOperations.ImproveWorkshop(farm,foreign),Is.False);Assert.That(farm.coins,Is.EqualTo(100));
        }
    }
}
