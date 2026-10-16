using NUnit.Framework;
using UnityEngine;
namespace StarfieldEra.Tests
{
    public class RockAndFenceTests
    {
        static FenceRecord Line(float ax,float az,float bx,float bz)=>new FenceRecord{free=true,ax=ax,az=az,bx=bx,bz=bz};
        [Test] public void MiningPersistsAcrossSaveAndTerrainHeightChanges()
        {
            var state=new FarmState();var p=new Vector3(42.12f,3,38.17f);
            Assert.That(state.mining.Mine(p),Is.True);
            Assert.That(state.mining.Mine(p+Vector3.up*7),Is.False);
            var loaded=JsonUtility.FromJson<FarmState>(JsonUtility.ToJson(state));loaded.Upgrade();
            Assert.That(loaded.mining.Contains(p),Is.True);Assert.That(loaded.IsValid(),Is.True);
        }
        [Test] public void MiningRejectsInvalidAndDuplicateSaveEntries()
        {
            var s=new MiningState();Assert.That(s.Mine(new Vector3(1000,0,0)),Is.False);
            s.depleted.Add(new Vector2Int(int.MinValue,0));Assert.That(s.Valid(),Is.False);
            s.depleted.Clear();s.depleted.Add(Vector2Int.zero);s.depleted.Add(Vector2Int.zero);Assert.That(s.Valid(),Is.False);
        }
        [Test] public void OldSaveUpgradesWithNoDepletedRocks()
        {
            var state=new FarmState();state.mining=null;state.Upgrade();Assert.That(state.mining.depleted,Is.Empty);Assert.That(state.IsValid(),Is.True);
        }
        [Test] public void RestartedFenceWithinSameCellAttachesToExistingPost()
        {
            var fences=new[]{Line(0,0,1.15f,0)};
            Assert.That(PastureFences.TrySnapEndpoint(fences,new Vector3(1.3f,5,.8f),null,HexWorldGrid.Radius,out var snapped),Is.True);
            Assert.That(snapped,Is.EqualTo(new Vector3(1.15f,0,0)));
        }
        [Test] public void ClosingFenceIgnoresOwnAnchorEvenWhenItIsCloser()
        {
            var fences=new[]{Line(0,0,1.15f,0),Line(0,.6f,1.15f,.6f)};
            Assert.That(PastureFences.TrySnapEndpoint(fences,new Vector3(.02f,0,.1f),Vector3.zero,.7f,out var snapped),Is.True);
            Assert.That(snapped,Is.EqualTo(new Vector3(0,0,.6f)));
        }
        [Test] public void SnappedGateEndpointSurvivesReloadWithoutGap()
        {
            var first=Line(0,0,1.15f,0);first.gate=true;
            var loaded=JsonUtility.FromJson<FenceRecord>(JsonUtility.ToJson(first));
            Assert.That(PastureFences.TrySnapEndpoint(new[]{loaded},new Vector3(1.3f,0,.25f),null,HexWorldGrid.Radius,out var p),Is.True);
            var second=Line(p.x,p.z,p.x,p.z+1.15f);
            PastureFences.Endpoints(loaded,out _,out var end);PastureFences.Endpoints(second,out var start,out _);
            Assert.That(start,Is.EqualTo(end));Assert.That(PastureFences.Valid(new System.Collections.Generic.List<FenceRecord>{loaded,second}),Is.True);
        }
        [Test] public void DistantFenceDoesNotPullPlacementAway()
        {
            var cursor=new Vector3(3,0,3);
            Assert.That(PastureFences.TrySnapEndpoint(new[]{Line(0,0,1.15f,0)},cursor,null,HexWorldGrid.Radius,out var snapped),Is.False);
            Assert.That(snapped,Is.EqualTo(cursor));
        }
    }
}
