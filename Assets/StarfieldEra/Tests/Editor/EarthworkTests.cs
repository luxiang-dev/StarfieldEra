using NUnit.Framework;
using UnityEngine;
namespace StarfieldEra.Tests
{
    public class EarthworkTests
    {
        [Test] public void SavedElevationChangesSurfaceAndLeavesDistantTerrainIntact()
        {
            var s=new LandState();var cell=new Vector2Int(28,22);var p=HexWorldGrid.Center(cell.x,cell.y);
            s.cells.Add(new LandCell{column=cell.x,row=cell.y,elevationEdited=true,elevation=8});
            Assert.That(LandElevation.Height(p,s),Is.EqualTo(8).Within(.001));
            var far=p+Vector3.right*5;Assert.That(LandElevation.Height(far,s),Is.EqualTo(WorldRelief.LandHeight(far)).Within(.001));
            s.cells[0].elevation=.2f;Assert.That(LandElevation.Height(p,s),Is.EqualTo(.2f).Within(.001));
            Assert.That(s.Valid(),Is.True);
        }
        [Test] public void LandHeightsRoundTripAndLegacyCellsStayNatural()
        {
            var s=new LandState();s.cells.Add(new LandCell{column=30,row=-30,elevationEdited=true,elevation=4.2f});
            var loaded=JsonUtility.FromJson<LandState>(JsonUtility.ToJson(s));loaded.Upgrade();
            Assert.That(loaded.Valid(),Is.True);Assert.That(LandElevation.Height(HexWorldGrid.Center(30,-30),loaded),Is.EqualTo(4.2f).Within(.001));
            var old=JsonUtility.FromJson<LandState>("{\"soil\":0,\"cells\":[{\"column\":1,\"row\":1}]}");old.Upgrade();
            var p=HexWorldGrid.Center(1,1);Assert.That(LandElevation.Height(p,old),Is.EqualTo(WorldRelief.LandHeight(p)));
        }
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(-1f)][TestCase(12.1f)]
        public void InvalidHeightsRejected(float value)
        {var s=new LandState();s.cells.Add(new LandCell{elevationEdited=true,elevation=value});Assert.That(s.Valid(),Is.False);}
        [Test] public void ExpandedGatherHistoryMigratesWithoutResettingOldSites()
        {
            var s=new AdventureState();s.gatheredDay=new int[32];s.gatheredDay[9]=17;s.Upgrade();
            Assert.That(s.gatheredDay.Length,Is.EqualTo(64));Assert.That(s.gatheredDay[9],Is.EqualTo(17));Assert.That(s.gatheredDay[32],Is.EqualTo(-1));
        }
        [Test] public void ExpandedMapAdmitsOuterConstructionAndLandEdits()
        {
            Assert.That(HexWorldGrid.HalfWidth*HexWorldGrid.HalfDepth,Is.EqualTo(42*36*4));
            var s=new FarmState();s.playerX=60;s.playerZ=50;s.land.cells.Add(new LandCell{column=30,row=29,elevationEdited=true,elevation=3});
            Assert.That(s.IsValid(),Is.True);
        }
    }
}
