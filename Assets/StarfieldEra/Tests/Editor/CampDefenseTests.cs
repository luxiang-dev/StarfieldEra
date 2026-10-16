using NUnit.Framework;
using UnityEngine;
namespace StarfieldEra.Tests
{
    public sealed class CampDefenseTests
    {
        [Test] public void HousesAndWarehousesExpandCapacityWithoutLosingOverflow()
        {
            var farm=new FarmState();var a=farm.adventure;var h=a.homestead;
            Assert.That(h.WorkerCapacity(a),Is.EqualTo(3));Assert.That(h.StorageCapacity(a),Is.EqualTo(30));
            a.buildings.Add(new PlacedBuilding{kind=4,column=10,row=-9});a.buildings.Add(new PlacedBuilding{kind=5,column=14,row=-9});
            Assert.That(h.WorkerCapacity(a),Is.EqualTo(5));Assert.That(h.StorageCapacity(a),Is.EqualTo(60));
            h.lumber.level=1;h.lumber.workers=1;h.lumber.stock=59;farm.Sleep();Assert.That(h.lumber.stock,Is.EqualTo(60));
            a.buildings.RemoveAll(b=>b.kind==5);farm.Sleep();Assert.That(h.lumber.stock,Is.EqualTo(60));
            h.Collect(a,h.lumber,0);Assert.That(a.wood,Is.EqualTo(60));Assert.That(h.lumber.stock,Is.Zero);
        }
        [Test] public void RaidForecastRequiresStaffAndResolvesOnlyOncePerDay()
        {
            var farm=new FarmState{day=3};var a=farm.adventure;var d=a.defense;
            Assert.That(d.Target(farm),Is.EqualTo(-1));a.homestead.lumber.level=1;a.homestead.lumber.workers=1;
            Assert.That(d.Target(farm),Is.Zero);Assert.That(d.Begin(farm),Is.True);Assert.That(d.Begin(farm),Is.False);
            Assert.That(d.Finish(farm,true),Is.True);Assert.That(d.Finish(farm,true),Is.False);Assert.That(farm.coins,Is.EqualTo(35));Assert.That(d.victories,Is.EqualTo(1));
            Assert.That(d.Begin(farm),Is.False);farm.day=4;Assert.That(d.Target(farm),Is.EqualTo(-1));farm.day=6;Assert.That(d.Target(farm),Is.Zero);
        }
        [Test] public void FailedDefenseLosesOnlyTargetStockAndSkipsOneProduction()
        {
            var farm=new FarmState{day=3};var h=farm.adventure.homestead;h.lumber.level=1;h.lumber.workers=1;h.lumber.stock=12;h.lumber.goods=4;
            h.quarry.level=1;h.quarry.workers=1;h.quarry.stock=10;
            farm.adventure.defense.Begin(farm);farm.adventure.defense.Finish(farm,false);
            Assert.That(h.lumber.stock,Is.EqualTo(6));Assert.That(h.lumber.goods,Is.EqualTo(2));Assert.That(h.quarry.stock,Is.EqualTo(10));
            farm.Sleep();Assert.That(h.lumber.stock,Is.EqualTo(6));Assert.That(h.lumber.downtime,Is.Zero);Assert.That(h.quarry.stock,Is.EqualTo(12));
            farm.Sleep();Assert.That(h.lumber.stock,Is.EqualTo(8));
        }
        [Test] public void TrapsArePaidOnceRejectDuplicatesAndCanReplaceSpentTraps()
        {
            var farm=new FarmState();var d=farm.adventure.defense;farm.adventure.wood=4;farm.adventure.stone=2;
            Assert.That(d.BuyTrap(farm,new Vector3(4,0,4)),Is.True);Assert.That(d.BuyTrap(farm,new Vector3(4,0,4)),Is.False);
            Assert.That(farm.adventure.wood,Is.EqualTo(2));d.traps[0].spent=true;Assert.That(d.BuyTrap(farm,new Vector3(4,0,4)),Is.True);
            Assert.That(d.traps.Count,Is.EqualTo(1));Assert.That(farm.adventure.wood,Is.Zero);
            Assert.That(d.BuyTrap(farm,new Vector3(8,0,4)),Is.False);
        }
        [Test] public void RepairsRequireMaterialsAndHonorCombatCooldown()
        {
            var farm=new FarmState{day=3};var a=farm.adventure;var d=a.defense;var site=a.homestead.lumber;
            site.level=1;site.workers=1;site.damage=50;Assert.That(d.Repair(farm,site),Is.False);Assert.That(site.damage,Is.EqualTo(50));
            a.wood=10;a.stone=10;d.Begin(farm);Assert.That(d.Repair(farm,site),Is.True);Assert.That(d.Repair(farm,site),Is.False);
            Assert.That(a.wood,Is.EqualTo(8));Assert.That(site.damage,Is.EqualTo(25));
        }
        [Test] public void MidRaidSaveKeepsThreatsProgressDamageAndTraps()
        {
            var farm=new FarmState{day=3};var a=farm.adventure;a.homestead.lumber.level=1;a.homestead.lumber.workers=1;a.homestead.lumber.damage=25;
            var d=a.defense;d.Begin(farm);d.elapsed=23;d.spawned=2;d.playerHP=5;d.threats.Add(new RaidThreat{id=0,hp=2,x=17,z=-11});d.traps.Add(new CampTrap{x=18,z=-12});
            var loaded=JsonUtility.FromJson<FarmState>(JsonUtility.ToJson(farm));loaded.Upgrade();
            Assert.That(loaded.IsValid(),Is.True);Assert.That(loaded.adventure.defense.active,Is.True);Assert.That(loaded.adventure.defense.elapsed,Is.EqualTo(23));
            Assert.That(loaded.adventure.defense.threats[0].hp,Is.EqualTo(2));Assert.That(loaded.adventure.homestead.lumber.damage,Is.EqualTo(25));
            Assert.That(loaded.adventure.defense.traps.Count,Is.EqualTo(1));
        }
        [Test] public void NewBuildingFootprintsValidateAcrossRotations()
        {
            for(int facing=0;facing<6;facing++)foreach(int kind in new[]{4,5})
            {var building=new PlacedBuilding{kind=kind,column=10,row=-8,rotation=facing,footprintVersion=1};Assert.That(BuildingFootprints.Cells(building).Count,Is.EqualTo(4));Assert.That(BuildingFootprints.Valid(new System.Collections.Generic.List<PlacedBuilding>{building}),Is.True);}
        }
        [Test] public void OldSavesMigrateToPeacefulDefenseState()
        {
            var farm=new FarmState();farm.adventure.defense=null;var loaded=JsonUtility.FromJson<FarmState>(JsonUtility.ToJson(farm));loaded.Upgrade();
            Assert.That(loaded.IsValid(),Is.True);Assert.That(loaded.adventure.defense.active,Is.False);Assert.That(loaded.adventure.homestead.StorageCapacity(loaded.adventure),Is.EqualTo(30));
        }
    }
}
