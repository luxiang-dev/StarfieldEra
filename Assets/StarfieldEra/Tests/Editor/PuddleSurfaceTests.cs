using NUnit.Framework;
using UnityEngine;
namespace StarfieldEra.Tests
{
    public class PuddleSurfaceTests
    {
        [Test] public void NeighbouringPuddlesBlendAcrossTheirSharedBoundary()
        {
            var a=Vector2Int.zero;var b=PastureFences.Neighbor(a,0);var storm=new StormWaterState();storm.puddles.Add(new RainPuddle{depth=.4f});storm.puddles.Add(new RainPuddle{column=b.x,row=b.y,depth=.4f});
            var field=new PuddleField(storm,_=>.2f);var mid=(HexWorldGrid.Center(a.x,a.y)+HexWorldGrid.Center(b.x,b.y))*.5f;
            Assert.That(field.Sample(mid-Vector3.right*.01f,out float left,out float l),Is.True);Assert.That(field.Sample(mid+Vector3.right*.01f,out float right,out float r),Is.True);
            Assert.That(left,Is.GreaterThan(.98f));Assert.That(right,Is.GreaterThan(.98f));Assert.That(l,Is.EqualTo(r).Within(.001));
        }
        [Test] public void DrainingWaterRecedesAndDisappearsWithoutChangingSavedGeometry()
        {
            var storm=new StormWaterState();storm.puddles.Add(new RainPuddle{depth=.45f});var field=new PuddleField(storm,_=>.2f);var p=HexWorldGrid.Center(0,0)+Vector3.right*1.15f;
            field.Sample(p,out float before,out _);storm.puddles[0].depth=.04f;field.Refresh(storm);field.Sample(p,out float after,out _);Assert.That(after,Is.LessThan(before));
            storm.puddles.Clear();storm.DiscardDryPuddles();field.Refresh(storm);Assert.That(field.Sample(HexWorldGrid.Center(0,0),out float dry,out _),Is.False);Assert.That(dry,Is.Zero);Assert.That(storm.Valid(),Is.True);
        }
        [Test] public void DistantDryGroundIsExcludedAndHeightBlendStaysFinite()
        {
            var storm=new StormWaterState();storm.puddles.Add(new RainPuddle{depth=.2f});var field=new PuddleField(storm,_=>3);
            Assert.That(field.Sample(HexWorldGrid.Center(0,0),out float coverage,out float level),Is.True);Assert.That(coverage,Is.EqualTo(1));Assert.That(level,Is.EqualTo(3.218f).Within(.001));
            Assert.That(field.Sample(new Vector3(60,0,60),out float dry,out _),Is.False);Assert.That(dry,Is.Zero);Assert.That(storm.puddles[0].depth,Is.EqualTo(.2f));
        }
    }
}
