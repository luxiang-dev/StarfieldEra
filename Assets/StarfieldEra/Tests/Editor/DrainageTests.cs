using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace StarfieldEra.Tests
{
    public class DrainageTests
    {
        static LandState Line(params Vector2Int[] cells){var land=new LandState();foreach(var c in cells)land.cells.Add(new LandCell{column=c.x,row=c.y,drainage=true});return land;}
        [Test] public void JoinedChannelsCutAlongTheirActualDirectionAndLeaveDistantGroundAlone()
        {
            var a=Vector2Int.zero;var b=PastureFences.Neighbor(a,1);var land=Line(a,b);var n=new DrainageNetwork(land,_=>1,_=>TerrainKind.Meadow);
            var center=HexWorldGrid.Center(a.x,a.y);var other=HexWorldGrid.Center(b.x,b.y);
            Assert.That(n.Cut((center+other)*.5f),Is.EqualTo(DrainageNetwork.Depth).Within(.001));Assert.That(n.Cut(center+Vector3.right*10),Is.Zero);
            Assert.That(land.cells.All(c=>!c.elevationEdited),Is.True);
        }
        [Test] public void ConnectedNetworkVisiblyDrainsOverSecondsAndCannotProduceNegativeDepth()
        {
            var a=Vector2Int.zero;var b=PastureFences.Neighbor(a,0);var outlet=PastureFences.Neighbor(b,0);var land=Line(a,b);
            var n=new DrainageNetwork(land,_=>.2f,c=>c==outlet?TerrainKind.Water:TerrainKind.Meadow);
            var storm=new StormWaterState();storm.puddles.Add(new RainPuddle{depth=.45f});
            Assert.That(n.Connected,Does.Contain(a));n.Drain(storm,DrainageNetwork.Rate*.5f);Assert.That(storm.Depth(a),Is.EqualTo(.4225f).Within(.001));
            for(int i=0;i<20;i++)n.Drain(storm,DrainageNetwork.Rate*.5f);
            Assert.That(storm.puddles,Is.Empty);Assert.That(storm.Valid(),Is.True);
        }
        [Test] public void IsolatedTrenchAndConstructionTogglingCannotErasePuddles()
        {
            var c=Vector2Int.zero;var land=Line(c);var n=new DrainageNetwork(land,_=>.2f,_=>TerrainKind.Meadow);var storm=new StormWaterState();storm.puddles.Add(new RainPuddle{depth=.4f});
            Assert.That(n.Drain(storm,10),Is.Zero);for(int i=0;i<10;i++){land.cells[0].drainage=false;new DrainageNetwork(land,_=>.2f,_=>TerrainKind.Meadow).Drain(storm,10);land.cells[0].drainage=true;new DrainageNetwork(land,_=>.2f,_=>TerrainKind.Meadow).Drain(storm,10);}Assert.That(storm.Depth(c),Is.EqualTo(.4f));
        }
        [Test] public void ConnectingAnExistingUpstreamTrenchEnablesDrainageAcrossWholeNetwork()
        {
            var a=Vector2Int.zero;var b=PastureFences.Neighbor(a,0);var water=PastureFences.Neighbor(b,0);var land=Line(a);
            TerrainKind Kind(Vector2Int c)=>c==water?TerrainKind.Water:TerrainKind.Meadow;
            Assert.That(new DrainageNetwork(land,_=>.2f,Kind).Connected,Is.Empty);
            land.cells.Add(new LandCell{column=b.x,row=b.y,drainage=true});var network=new DrainageNetwork(land,_=>.2f,Kind);var storm=new StormWaterState();storm.puddles.Add(new RainPuddle{depth=.5f});network.Drain(storm,.55f);Assert.That(storm.puddles,Is.Empty);
        }
        [Test] public void HighDrainCannotPumpWaterOutOfLowerBasin()
        {
            var c=Vector2Int.zero;var wet=PastureFences.Neighbor(c,3);var water=PastureFences.Neighbor(c,0);var land=Line(c);
            var n=new DrainageNetwork(land,p=>p==wet?0:2,p=>p==water?TerrainKind.Water:TerrainKind.Meadow);
            var storm=new StormWaterState();storm.puddles.Add(new RainPuddle{column=wet.x,row=wet.y,depth=.5f});n.Drain(storm,10);Assert.That(storm.Depth(wet),Is.EqualTo(.5f));
        }
        [Test] public void RemovingOutletConnectionStopsDrainageAndDistantPuddlesStayWet()
        {
            var c=Vector2Int.zero;var b=PastureFences.Neighbor(c,0);var water=PastureFences.Neighbor(b,0);var land=Line(c,b);TerrainKind Kind(Vector2Int p)=>p==water?TerrainKind.Water:TerrainKind.Meadow;
            var storm=new StormWaterState();storm.puddles.Add(new RainPuddle{depth=.4f});storm.puddles.Add(new RainPuddle{column=20,row=20,depth=.4f});new DrainageNetwork(land,_=>.2f,Kind).Drain(storm,.1f);
            Assert.That(storm.Depth(new Vector2Int(20,20)),Is.EqualTo(.4f));land.cells[1].drainage=false;new DrainageNetwork(land,_=>.2f,Kind).Drain(storm,.1f);Assert.That(storm.Depth(c),Is.EqualTo(.3f).Within(.001));
        }
    }
}
