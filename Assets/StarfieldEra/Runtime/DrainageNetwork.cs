using System;
using System.Collections.Generic;
using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Shared channel paths for terrain cuts, presentation and gravity drainage.</summary>
    public sealed class DrainageNetwork
    {
        public const float Depth=.32f,HalfWidth=.56f,Radius=4.2f,Rate=.055f;
        public struct Segment {public Vector3 a,b;public bool connected,outlet;}
        public readonly List<Segment> Segments=new List<Segment>();
        public readonly HashSet<Vector2Int> Connected;
        public readonly HashSet<Vector2Int> IsolatedCatchments=new HashSet<Vector2Int>();
        public readonly Dictionary<Vector2Int,float> IntakeFloors=new Dictionary<Vector2Int,float>();
        readonly Dictionary<Vector2Int,List<Segment>> nearby=new Dictionary<Vector2Int,List<Segment>>();
        public DrainageNetwork(LandState land,Func<Vector2Int,float> height,Func<Vector2Int,TerrainKind> kind)
        {
            var heights=new Dictionary<Vector2Int,float>();
            float H(Vector2Int c){if(!heights.TryGetValue(c,out float h)){h=height(c);heights[c]=h;}return h;}
            Connected=StormWaterState.ConnectedDrains(land,H,kind);
            var nodes=new HashSet<Vector2Int>();foreach(var edit in land.cells)if(edit.drainage)nodes.Add(edit.Cell);
            var distance=new Dictionary<Vector2Int,int>();var queue=new Queue<Vector2Int>();
            foreach(var c in nodes)for(int i=0;i<6;i++){var n=PastureFences.Neighbor(c,i);if(kind(n)==TerrainKind.Water||!nodes.Contains(n)&&H(n)<H(c)-.12f){distance[c]=0;queue.Enqueue(c);break;}}
            while(queue.Count>0){var c=queue.Dequeue();for(int i=0;i<6;i++){var n=PastureFences.Neighbor(c,i);if(nodes.Contains(n)&&H(n)>=H(c)-.08f&&!distance.ContainsKey(n)){distance[n]=distance[c]+1;queue.Enqueue(n);}}}
            foreach(var c in nodes)
            {
                var center=HexWorldGrid.Center(c.x,c.y);bool any=false;int outletEdge=-1;float outletScore=float.MinValue;
                for(int edge=0;edge<6;edge++){var n=PastureFences.Neighbor(c,edge);if(nodes.Contains(n))continue;float drop=H(c)-H(n);bool water=kind(n)==TerrainKind.Water;if((water||drop>.12f)&&(water?100+drop:drop)>outletScore){outletScore=water?100+drop:drop;outletEdge=edge;}}
                for(int i=0;i<6;i++)
                {
                    var n=PastureFences.Neighbor(c,i);bool neighbor=nodes.Contains(n),outlet=!neighbor&&i==outletEdge;
                    if(!neighbor&&!outlet)continue;any=true;
                    var end=Vector3.Lerp(center,HexWorldGrid.Center(n.x,n.y),outlet?.72f:.5f);
                    bool downstream=outlet||distance.TryGetValue(c,out int dc)&&distance.TryGetValue(n,out int dn)&&dc>dn;
                    Add(new Segment{a=downstream?center:end,b=downstream?end:center,connected=Connected.Contains(c),outlet=outlet},c);
                }
                if(!any)Add(new Segment{a=center+Vector3.back*.65f,b=center+Vector3.forward*.65f},c);
                bool draining=Connected.Contains(c);
                for(int row=c.y-3;row<=c.y+3;row++)for(int col=c.x-3;col<=c.x+3;col++)
                {
                    var p=new Vector2Int(col,row);if(kind(p)==TerrainKind.Water||(HexWorldGrid.Center(col,row)-center).sqrMagnitude>=Radius*Radius)continue;
                    if(!draining){IsolatedCatchments.Add(p);continue;}
                    // A lower basin cannot drain uphill below the inlet's excavated bed.
                    float floor=Mathf.Max(0,H(c)-Depth-H(p));
                    if(!IntakeFloors.TryGetValue(p,out float old)||floor<old)IntakeFloors[p]=floor;
                }
            }
        }
        void Add(Segment s,Vector2Int cell)
        {
            Segments.Add(s);
            for(int row=cell.y-2;row<=cell.y+2;row++)for(int col=cell.x-2;col<=cell.x+2;col++)
            {var c=new Vector2Int(col,row);if(!nearby.TryGetValue(c,out var list)){list=new List<Segment>();nearby[c]=list;}list.Add(s);}
        }
        float DistanceSquared(Vector3 p)
        {
            if(Segments.Count==0||!nearby.TryGetValue(StormWaterState.CellAt(p),out var segments))return float.MaxValue;
            float nearest=float.MaxValue;p.y=0;
            foreach(var s in segments){var ab=s.b-s.a;var q=s.a+ab*Mathf.Clamp01(Vector3.Dot(p-s.a,ab)/ab.sqrMagnitude);nearest=Mathf.Min(nearest,(p-q).sqrMagnitude);}
            return nearest;
        }
        public bool NearChannel(Vector3 p,float radius)=>DistanceSquared(p)<radius*radius;
        public float Cut(Vector3 p)=>Depth*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,HalfWidth,Mathf.Sqrt(DistanceSquared(p)))));
        public float Drain(StormWaterState storm,float amount)
        {
            if(IntakeFloors.Count==0||amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return 0;
            float removed=0;
            foreach(var p in storm.puddles)if(IntakeFloors.TryGetValue(p.Cell,out float floor))
            {float take=Mathf.Min(amount,Mathf.Max(0,p.depth-floor));p.depth-=take;removed+=take;}
            if(removed>0)storm.DiscardDryPuddles();return removed;
        }
    }
}
