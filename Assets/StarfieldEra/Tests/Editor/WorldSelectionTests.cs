using NUnit.Framework;
using UnityEngine;
namespace StarfieldEra.Tests
{
    public sealed class WorldSelectionTests
    {
        [Test] public void LockedExpansionCannotBePlantedWateredOrHarvested()
        {
            var state=new FarmState();state.plots[17].planted=true;
            Assert.That(state.IsPlotUnlocked(17),Is.False);Assert.That(state.Water(17),Is.False);
            state.plots[17].growth=FarmState.GrowthDays;Assert.That(state.Harvest(17),Is.False);
            state.plots[17]=new CropState();Assert.That(state.Plant(17),Is.False);
            state.adventure.expandedFarm=true;Assert.That(state.IsPlotUnlocked(17),Is.True);Assert.That(state.Plant(17),Is.True);
        }
        [Test] public void FarmGroundLookupUsesOnlyUnlockedHexes()
        {
            var root=new GameObject("farm lookup test");
            try
            {
                var world=root.AddComponent<FarmWorld>();var grid=new GameObject("grid").AddComponent<HexWorldGrid>();grid.transform.SetParent(root.transform);grid.Build(world,null);
                typeof(FarmWorld).GetProperty(nameof(FarmWorld.HexGrid)).SetValue(world,grid);
                var state=new FarmState();
                for(int i=0;i<FarmState.PlotCount;i++)
                {
                    var cell=FarmWorld.PlotCell(i);var point=HexWorldGrid.Center(cell.x,cell.y);
                    Assert.That(world.TryPlotAt(point,state,out int found),Is.EqualTo(i<12),"plot "+i);Assert.That(found,Is.EqualTo(i<12?i:-1));
                }
                var grass=HexWorldGrid.Center(8,-4);Assert.That(world.TryPlotAt(grass,state,out _),Is.False);
                state.adventure.expandedFarm=true;
                for(int i=0;i<FarmState.PlotCount;i++){var c=FarmWorld.PlotCell(i);Assert.That(world.TryPlotAt(HexWorldGrid.Center(c.x,c.y),state,out int found),Is.True);Assert.That(found,Is.EqualTo(i));}
                // A point inside a square's corner can already belong to the next hex.
                var origin=HexWorldGrid.Center(0,0);Assert.That(world.TryPlotAt(origin+new Vector3(-.9f,0,.9f),state,out _),Is.False);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void LandAndEveryConstructionManagementPageUseIndependentWindows()
        {
            foreach(var page in new[]{"land","building","production","buildings","camp"})Assert.That(ManagementHud.IsOperationsPage(page),Is.True,page);
            foreach(var page in new[]{"inventory","weather","town","homestead","tutorial"})Assert.That(ManagementHud.IsOperationsPage(page),Is.False,page);
        }
        [Test] public void RangeUsesGroundDistanceAndNeverAllowsObservationOnlyActions()
        {
            Assert.That(WorldInspection.InRange(Vector3.zero,new Vector3(2,80,0),2.7f),Is.True);
            Assert.That(WorldInspection.InRange(Vector3.zero,new Vector3(3,0,0),2.7f),Is.False);
            Assert.That(WorldInspection.InRange(Vector3.zero,Vector3.zero,-1),Is.False);
        }
        [Test] public void SelectedObjectBecomesUnavailableWhenItsParentIsHidden()
        {
            var parent=new GameObject("parent");var child=new GameObject("resource");child.transform.SetParent(parent.transform);
            try{var choice=new WorldChoice{root=child.transform};Assert.That(choice.Alive,Is.True);parent.SetActive(false);Assert.That(choice.Alive,Is.False);}
            finally{Object.DestroyImmediate(parent);}
        }
        [Test] public void HarvestedRockAndFelledTreeCannotRemainInteractive()
        {
            var go=new GameObject("rock");var treeObject=new GameObject("tree");
            try
            {
                var rock=go.AddComponent<HarvestableRock>();var choice=new WorldChoice{root=go.transform,rock=rock};Assert.That(choice.Alive,Is.True);rock.Hide();Assert.That(choice.Alive,Is.False);
                var tree=treeObject.AddComponent<HarvestableTree>();choice=new WorldChoice{root=treeObject.transform,tree=tree};tree.Felled=true;Assert.That(choice.Alive,Is.False);
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(treeObject);}
        }
        [Test] public void DefeatedSelectedWolfCannotBeAttackedAgain()
        {
            var go=new GameObject("wolf");try{var threat=new RaidThreat{hp=1};var choice=new WorldChoice{root=go.transform,threat=threat};Assert.That(choice.Alive,Is.True);threat.hp=0;Assert.That(choice.Alive,Is.False);}finally{Object.DestroyImmediate(go);}
        }
    }
}
