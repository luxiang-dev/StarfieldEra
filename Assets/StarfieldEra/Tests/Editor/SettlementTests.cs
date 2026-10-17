using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace StarfieldEra.Tests
{
    public sealed class SettlementTests
    {
        static FarmState Farm(){var f=new FarmState();f.Upgrade();return f;}
        static void Weather(FarmState f,int day,WeatherType type){f.day=day;f.weather.Ensure(day);f.weather.trueWeatherQueue[0]=type;}
        [Test] public void MigrationKeepsLegacyCountsAndNamedAssignmentsAcrossReload()
        {
            var f=new FarmState();var h=f.adventure.homestead;h.lumber.level=h.quarry.level=1;h.lumber.workers=2;h.quarry.workers=1;f.Upgrade();
            Assert.That(f.adventure.village.residents.Count,Is.EqualTo(3));Assert.That(f.adventure.village.residents.Count(r=>r.workplace==h.lumber.workplaceId),Is.EqualTo(2));
            var copy=JsonUtility.FromJson<FarmState>(JsonUtility.ToJson(f));copy.Upgrade();Assert.That(copy.IsValid(),Is.True);CollectionAssert.AreEqual(f.adventure.village.residents.Select(r=>r.workplace),copy.adventure.village.residents.Select(r=>r.workplace));
        }
        [Test] public void SpecificResidentCanChangeJobsWithoutDuplicatingWorkers()
        {
            var f=Farm();var a=f.adventure;var h=a.homestead;h.lumber.level=h.quarry.level=1;var r=a.village.residents[0];
            Assert.That(a.village.Assign(a,r,h.lumber),Is.True);Assert.That(a.village.Assign(a,r,h.quarry),Is.True);Assert.That(h.lumber.workers,Is.Zero);Assert.That(h.quarry.workers,Is.EqualTo(1));
            Assert.That(a.village.Assign(a,r,new ResourceSiteState{level=1}),Is.False);Assert.That(h.quarry.workers,Is.EqualTo(1));Assert.That(a.village.Assign(a,r,null),Is.True);Assert.That(h.UsedWorkers(a),Is.Zero);
        }
        [Test] public void FoodAndDailyNeedsAreAtomicAndCannotBeChargedTwice()
        {
            var f=Farm();var v=f.adventure.village;int pantry=v.pantry;Assert.That(v.Donate(f,0),Is.False);Assert.That(v.pantry,Is.EqualTo(pantry));f.adventure.homestead.carrots=3;Assert.That(v.Donate(f,0),Is.True);Assert.That(f.adventure.homestead.carrots,Is.Zero);
            f.day=2;v.BeginDay(f);int left=v.pantry;v.BeginDay(f);Assert.That(v.pantry,Is.EqualTo(left));v.pantry=0;f.day=3;v.BeginDay(f);Assert.That(v.residents.All(r=>r.hunger==1),Is.True);
        }
        [Test] public void SpecialtyFoodAndHousingActuallyChangeProduction()
        {
            var f=Farm();var a=f.adventure;var h=a.homestead;h.lumber.level=1;var r=a.village.residents[0];a.village.Assign(a,r,h.lumber);r.workedDays=6;
            Assert.That(a.village.Productivity(a,h.lumber),Is.EqualTo(1.3f).Within(.001));r.hunger=1;Assert.That(a.village.Productivity(a,h.lumber),Is.EqualTo(.91f).Within(.001));
            h.lumber.NewDay(30,a.village.Productivity(a,h.lumber));Assert.That(h.lumber.stock,Is.EqualTo(3));
        }
        [Test] public void RecruitingRequiresBedsAndPaysOnlyOnSuccess()
        {
            var f=Farm();var a=f.adventure;f.coins=100;a.village.pantry=20;Assert.That(a.village.Recruit(f),Is.False);Assert.That(f.coins,Is.EqualTo(100));
            a.buildings.Add(new PlacedBuilding{kind=4,column=20,row=-20});Assert.That(a.village.Recruit(f),Is.True);Assert.That(a.village.residents.Count,Is.EqualTo(4));Assert.That(f.coins,Is.EqualTo(80));Assert.That(a.village.pantry,Is.EqualTo(14));
        }
        [Test] public void OrdersCannotChargePartialMaterialsOrRewardTwice()
        {
            var f=Farm();var t=f.adventure.trade;var order=t.orders.First(o=>o.template==0);Assert.That(t.Accept(f.day,order),Is.True);f.adventure.homestead.planks=5;int coins=f.coins;
            Assert.That(t.Deliver(f,order),Is.False);Assert.That(f.adventure.homestead.planks,Is.EqualTo(5));Assert.That(f.coins,Is.EqualTo(coins));f.adventure.homestead.planks=6;
            Assert.That(t.Deliver(f,order),Is.True);Assert.That(f.adventure.frontier.bridgeCredit,Is.True);Assert.That(f.adventure.village.reputation,Is.EqualTo(3));Assert.That(t.Deliver(f,order),Is.False);Assert.That(f.coins,Is.EqualTo(coins+45));
        }
        [Test] public void ExpiredOrdersAndForeignOrderObjectsCannotBeSubmitted()
        {
            var f=Farm();var t=f.adventure.trade;var old=t.orders[0];t.Accept(1,old);f.adventure.homestead.planks=10;Weather(f,5,WeatherType.Sunny);t.Ensure(5);
            Assert.That(old.status,Is.EqualTo(3));Assert.That(t.Deliver(f,old),Is.False);Assert.That(t.Accept(5,new VillageOrder{cycle=1,template=2}),Is.False);Assert.That(f.adventure.homestead.planks,Is.EqualTo(10));Assert.That(t.orders.Count(o=>o.cycle==1),Is.EqualTo(3));
        }
        [Test] public void CaravanLimitedStockPersistsAndRefreshesOnlyNextVisit()
        {
            var f=Farm();f.coins=300;var t=f.adventure.trade;Assert.That(t.Buy(f,0),Is.True);Assert.That(t.Buy(f,0),Is.True);Assert.That(t.Buy(f,0),Is.False);Assert.That(f.starSeeds,Is.EqualTo(6));
            var copy=JsonUtility.FromJson<FarmState>(JsonUtility.ToJson(f));copy.Upgrade();Assert.That(copy.adventure.trade.Buy(copy,0),Is.False);
            Weather(f,3,WeatherType.Sunny);Assert.That(t.Buy(f,5),Is.False);Weather(f,5,WeatherType.Sunny);Assert.That(t.Buy(f,0),Is.True);Assert.That(f.starSeeds,Is.EqualTo(9));
        }
        [Test] public void CaravanBlueprintCannotBeBoughtAgainAfterLearning()
        {
            var f=Farm();f.coins=100;Assert.That(f.adventure.trade.Buy(f,1),Is.True);Assert.That(f.adventure.expedition.equipment.blueprints[6],Is.True);int paid=f.coins;Assert.That(f.adventure.trade.Buy(f,1),Is.False);Assert.That(f.coins,Is.EqualTo(paid));
        }
        [Test] public void RareCropUsesRealSeedsAndProducesFoodAndCoinsOnce()
        {
            var f=Farm();f.starSeeds=1;f.selectedCrop=3;Assert.That(f.BuySeed(),Is.False);Assert.That(f.Plant(0),Is.True);Assert.That(f.starSeeds,Is.Zero);
            for(int i=0;i<5;i++){f.plots[0].watered=true;f.plots[0].Advance(1);}int coins=f.coins,food=f.adventure.village.pantry;Assert.That(f.Harvest(0),Is.True);Assert.That(f.coins,Is.EqualTo(coins+60));Assert.That(f.adventure.village.pantry,Is.EqualTo(food+3));Assert.That(f.Harvest(0),Is.False);
        }
        [Test] public void RouteRepairAndCommunityCreditsCannotDuplicateCosts()
        {
            var f=Farm();var a=f.adventure;var frontier=a.frontier;a.wood=8;a.stone=4;f.coins=20;
            Assert.That(frontier.Repair(f,0),Is.False);Assert.That(a.wood,Is.EqualTo(8));frontier.bridgeCredit=true;Assert.That(frontier.Repair(f,0),Is.True);Assert.That(frontier.Repair(f,0),Is.False);Assert.That(f.coins,Is.Zero);Assert.That(a.wood+a.stone,Is.Zero);
        }
        [Test] public void FrontierResourcesRequireUnlockAndHaveIndependentDailyLimits()
        {
            var f=Farm();var s=f.adventure.frontier;Assert.That(s.Gather(f,1,0),Is.False);s.unlocked[1]=true;Assert.That(s.Gather(f,1,0),Is.True);Assert.That(f.adventure.crystals,Is.EqualTo(2));Assert.That(s.Gather(f,1,0),Is.False);Assert.That(s.Gather(f,1,1),Is.True);Assert.That(s.Gather(f,1,1),Is.False);Assert.That(s.Gather(f,1,2),Is.True);Assert.That(s.Gather(f,1,2),Is.False);
        }
        [Test] public void HeavyRainPoolsInLowGroundAndSunnyWeatherDriesIt()
        {
            var f=Farm();var c=Vector2Int.zero;float Height(Vector2Int p)=>p==c?.12f:.35f;Weather(f,1,WeatherType.Thunderstorm);
            f.storm.Step(f,new[]{c},Height,_=>TerrainKind.Meadow,_=>false);float depth=f.storm.Depth(c);Assert.That(depth,Is.GreaterThan(.15f));Assert.That(f.storm.GroundDrop(HexWorldGrid.Center(0,0)),Is.GreaterThan(0));
            f.storm.Step(f,new[]{c},Height,_=>TerrainKind.Meadow,_=>false);Assert.That(f.storm.Depth(c),Is.EqualTo(depth));Weather(f,2,WeatherType.Sunny);f.storm.Step(f,new[]{c},Height,_=>TerrainKind.Meadow,_=>false);Assert.That(f.storm.Depth(c),Is.LessThan(depth));Weather(f,3,WeatherType.Sunny);f.storm.Step(f,new[]{c},Height,_=>TerrainKind.Meadow,_=>false);Assert.That(f.storm.puddles,Is.Empty);
        }
        [Test] public void ConnectedDrainRemovesWaterWithoutChangingPermanentHeight()
        {
            var f=Farm();var c=Vector2Int.zero;var drain=PastureFences.Neighbor(c,0);var water=PastureFences.Neighbor(drain,0);f.land.cells.Add(new LandCell{column=drain.x,row=drain.y,drainage=true});
            TerrainKind Kind(Vector2Int p)=>p==water?TerrainKind.Water:TerrainKind.Meadow;
            var connected=StormWaterState.ConnectedDrains(f.land,_=>.2f,Kind);Assert.That(connected,Does.Contain(drain));Weather(f,1,WeatherType.Thunderstorm);f.storm.Step(f,new[]{c},_=>.15f,Kind,_=>false);Assert.That(f.storm.puddles,Is.Empty);Assert.That(f.land.cells[0].elevationEdited,Is.False);
        }
        [Test] public void IsolatedDrainDoesNotPretendToHaveAnOutlet()
        {
            var f=Farm();var c=Vector2Int.zero;f.land.cells.Add(new LandCell{drainage=true});float Height(Vector2Int p)=>p==c?.1f:1f;
            Assert.That(StormWaterState.ConnectedDrains(f.land,Height,_=>TerrainKind.Meadow),Is.Empty);
            Weather(f,1,WeatherType.Thunderstorm);f.storm.Step(f,new[]{c},Height,_=>TerrainKind.Meadow,_=>false);Assert.That(f.storm.Depth(c),Is.GreaterThan(.2f));
        }
        [Test] public void PermanentWaterRoadsAndProtectedFoundationsDoNotFlood()
        {
            var f=Farm();Weather(f,1,WeatherType.Thunderstorm);var cells=new[]{new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(2,0)};
            f.storm.Step(f,cells,_=>.1f,c=>c.x==0?TerrainKind.Water:c.x==1?TerrainKind.Road:TerrainKind.Meadow,c=>c.x==2);Assert.That(f.storm.puddles,Is.Empty);
        }
        [Test] public void NewSystemsSurviveSaveAndRejectCorruptFloodDepth()
        {
            var f=Farm();f.adventure.frontier.unlocked[0]=true;f.adventure.frontier.visited[0]=true;f.land.cells.Add(new LandCell{column=10,row=10,drainage=true});f.storm.puddles.Add(new RainPuddle{column=2,row=3,depth=.22f});
            var copy=JsonUtility.FromJson<FarmState>(JsonUtility.ToJson(f));copy.Upgrade();Assert.That(copy.IsValid(),Is.True);Assert.That(copy.storm.Depth(new Vector2Int(2,3)),Is.EqualTo(.22f));Assert.That(copy.land.cells[0].drainage,Is.True);copy.storm.puddles[0].depth=float.NaN;Assert.That(copy.IsValid(),Is.False);
        }
        [Test] public void NewPanelsAreIndependentOperationsAndDrainToolHasDescription()
        {foreach(string page in new[]{"villagers","orders","caravan","frontier"})Assert.That(ManagementHud.IsOperationsPage(page),Is.True);Assert.That(LandStewardship.Names.Length,Is.EqualTo(LandStewardship.Descriptions.Length));Assert.That(EquipmentCatalog.LandKey(8),Is.EqualTo("shovel"));}
    }
}
