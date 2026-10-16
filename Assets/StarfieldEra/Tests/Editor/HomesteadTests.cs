using NUnit.Framework;
using UnityEngine;
namespace StarfieldEra.Tests
{
    public sealed class HomesteadTests
    {
        [Test] public void OldSaveGainsEmptySystemsAndKeepsBuildings()
        {
            var farm=new FarmState();farm.adventure.homestead=null;
            farm.adventure.buildings.Add(new PlacedBuilding{kind=0,column=12,row=-8});
            var restored=JsonUtility.FromJson<FarmState>(JsonUtility.ToJson(farm));restored.Upgrade();
            Assert.That(restored.IsValid(),Is.True);Assert.That(restored.adventure.buildings[0].operation.level,Is.EqualTo(1));
            restored.Sleep();Assert.That(restored.adventure.wood,Is.Zero);
        }
        [Test] public void FailedRepairDoesNotSpendAndWorkerPoolIsShared()
        {
            var farm=new FarmState();var h=farm.adventure.homestead;
            Assert.That(h.Improve(farm,h.lumber),Is.False);Assert.That(farm.coins,Is.EqualTo(20));Assert.That(h.lumber.level,Is.Zero);
            farm.adventure.wood=farm.adventure.stone=50;farm.coins=100;
            Assert.That(h.Improve(farm,h.lumber),Is.True);Assert.That(h.Improve(farm,h.quarry),Is.True);
            Assert.That(h.Assign(farm.adventure,h.lumber,1),Is.True);Assert.That(h.Assign(farm.adventure,h.lumber,1),Is.True);
            Assert.That(h.Assign(farm.adventure,h.quarry,1),Is.True);Assert.That(h.Assign(farm.adventure,h.quarry,1),Is.False);
            Assert.That(h.Assign(farm.adventure,h.lumber,-1),Is.True);Assert.That(h.Assign(farm.adventure,h.quarry,1),Is.True);
            Assert.That(h.UsedWorkers(farm.adventure),Is.EqualTo(3));
        }
        [Test] public void ProductionProcessingCapacityAndCollectionCannotDuplicateGoods()
        {
            var farm=new FarmState();var h=farm.adventure.homestead;
            h.lumber.level=2;h.lumber.workers=2;h.lumber.processing=true;
            farm.Sleep();Assert.That(h.lumber.stock,Is.EqualTo(4));Assert.That(h.lumber.goods,Is.EqualTo(4));Assert.That(farm.adventure.wood,Is.Zero);
            h.Collect(farm.adventure,h.lumber,0);h.Collect(farm.adventure,h.lumber,0);
            Assert.That(farm.adventure.wood,Is.EqualTo(4));Assert.That(h.planks,Is.EqualTo(4));
            h.lumber.stock=29;h.lumber.goods=30;farm.Sleep();Assert.That(h.lumber.stock,Is.EqualTo(30));Assert.That(h.lumber.goods,Is.EqualTo(30));
            h.lumber.workers=0;h.lumber.stock=0;h.lumber.goods=0;farm.Sleep();Assert.That(h.lumber.stock+h.lumber.goods,Is.Zero);
        }
        [Test] public void RabbitCareRequiresPreviousDayFeedingAndSurvivesReload()
        {
            var farm=new FarmState();var h=farm.adventure.homestead;
            h.rabbits.Add(new RabbitRecord{id=0,status=2,fedDay=1});
            farm.Sleep();Assert.That(h.rabbits[0].care,Is.EqualTo(1));Assert.That(farm.coins,Is.EqualTo(24));
            var restored=JsonUtility.FromJson<FarmState>(JsonUtility.ToJson(farm));restored.Upgrade();restored.Sleep();
            Assert.That(restored.IsValid(),Is.True);Assert.That(restored.coins,Is.EqualTo(24));Assert.That(restored.adventure.homestead.rabbits[0].care,Is.EqualTo(1));
        }
        [Test] public void CarrotHarvestAndPurchasesAreAtomic()
        {
            var farm=new FarmState();farm.plots[0]=new CropState{planted=true,growth=2};
            Assert.That(farm.Harvest(0),Is.True);Assert.That(farm.Harvest(0),Is.False);Assert.That(farm.adventure.homestead.carrots,Is.EqualTo(2));
            farm.coins=5;Assert.That(farm.adventure.homestead.BuyCarrots(farm),Is.False);Assert.That(farm.coins,Is.EqualTo(5));
            farm.coins=6;Assert.That(farm.adventure.homestead.BuyCarrots(farm),Is.True);Assert.That(farm.coins,Is.Zero);Assert.That(farm.adventure.homestead.carrots,Is.EqualTo(5));
        }
        [Test] public void RejectsImpossibleRabbitAndStaffStates()
        {
            var farm=new FarmState();var h=farm.adventure.homestead;h.rabbits.Add(new RabbitRecord{id=0,status=1});h.rabbits.Add(new RabbitRecord{id=1,status=1});
            Assert.That(farm.IsValid(),Is.False);h.rabbits.Clear();h.lumber.level=h.quarry.level=1;h.lumber.workers=h.quarry.workers=2;Assert.That(farm.IsValid(),Is.False);
        }
        [Test] public void TopUpgradeRequiresProcessedGoodsWithoutPartialPayment()
        {
            var farm=new FarmState();var h=farm.adventure.homestead;h.lumber.level=2;farm.coins=100;farm.adventure.wood=farm.adventure.stone=100;
            Assert.That(h.Improve(farm,h.lumber),Is.False);Assert.That(farm.coins,Is.EqualTo(100));Assert.That(farm.adventure.wood,Is.EqualTo(100));
            h.planks=h.bricks=2;Assert.That(h.Improve(farm,h.lumber),Is.True);Assert.That(h.planks+h.bricks,Is.Zero);Assert.That(h.lumber.level,Is.EqualTo(3));
        }
    }
}
