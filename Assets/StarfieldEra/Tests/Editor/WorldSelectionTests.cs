using NUnit.Framework;
using UnityEngine;
namespace StarfieldEra.Tests
{
    public sealed class WorldSelectionTests
    {
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
