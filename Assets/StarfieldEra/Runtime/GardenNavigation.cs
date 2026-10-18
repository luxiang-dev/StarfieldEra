using System.Collections.Generic;
using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Bounded hex routing around current physical obstacles; shared short-lived caches.</summary>
    public sealed class GardenNavigation
    {
        readonly FarmGame game;readonly Collider[] overlaps=new Collider[24];readonly RaycastHit[] hits=new RaycastHit[32];
        readonly Dictionary<Vector2Int,bool> passable=new Dictionary<Vector2Int,bool>();
        readonly Dictionary<(Vector2Int,Vector2Int),bool> edges=new Dictionary<(Vector2Int,Vector2Int),bool>();
        readonly List<Node> open=new List<Node>(256);readonly Dictionary<Vector2Int,Node> nodes=new Dictionary<Vector2Int,Node>(256);
        int frame=-1,budget;float expires;
        sealed class Node {public Vector2Int cell;public float g,f;public Node parent;public bool closed;}
        public GardenNavigation(FarmGame owner){game=owner;}
        public void Invalidate(){passable.Clear();edges.Clear();expires=Time.time+2;}
        public bool SpawnPoint(Vector3 desired,out Vector3 point)
        {
            var grid=game.World.HexGrid;point=desired;
            for(int i=0;i<13;i++)
            {
                float angle=i*2.39996f;var p=desired+(i==0?Vector3.zero:new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(.6f+(i%3)*.6f));
                if(!grid.CanStand(p,.3f)||WorldRelief.PlanarDistance(p,game.World.Player.position)<1.6f)continue;p=grid.SurfacePoint(p);
                int count=Physics.OverlapSphereNonAlloc(p+Vector3.up*.65f,.33f,overlaps,~0,QueryTriggerInteraction.Ignore);bool clear=count<overlaps.Length;
                for(int n=0;n<count&&clear;n++)if(!grid.IsGroundCollider(overlaps[n]))clear=false;
                if(clear){point=p;return true;}
            }
            return false;
        }
        bool Walkable(Vector2Int c)
        {
            if(passable.TryGetValue(c,out bool cached))return cached;
            var grid=game.World.HexGrid;var p=HexWorldGrid.Center(c.x,c.y);bool ok=grid.Contains(c)&&grid.CanStand(p,.3f);
            if(ok){int count=Physics.OverlapSphereNonAlloc(grid.SurfacePoint(p,.65f),.33f,overlaps,~0,QueryTriggerInteraction.Ignore);if(count==overlaps.Length)ok=false;
                for(int i=0;i<count&&ok;i++)if(!grid.IsGroundCollider(overlaps[i])&&!overlaps[i].transform.IsChildOf(game.World.Player))ok=false;}
            passable[c]=ok;return ok;
        }
        public bool Clear(Vector3 from,Vector3 to,Transform ignore,out Collider obstacle,float radius=.28f)
        {
            var delta=to-from;float distance=delta.magnitude;obstacle=null;if(distance<.001f)return true;
            int count=Physics.SphereCastNonAlloc(from+Vector3.up*.65f,radius,delta.normalized,hits,distance,~0,QueryTriggerInteraction.Ignore);float nearest=float.MaxValue;
            for(int i=0;i<count;i++){var c=hits[i].collider;if(game.World.HexGrid.IsGroundCollider(c)||c.transform.IsChildOf(game.World.Player)||ignore&&c.transform.IsChildOf(ignore))continue;if(hits[i].distance<nearest){nearest=hits[i].distance;obstacle=c;}}
            return !obstacle&&count<hits.Length;
        }
        bool Edge(Vector2Int a,Vector2Int b)
        {
            var key=(a,b);if(edges.TryGetValue(key,out bool value))return value;
            var grid=game.World.HexGrid;var p=grid.SurfacePoint(HexWorldGrid.Center(a.x,a.y));var q=grid.SurfacePoint(HexWorldGrid.Center(b.x,b.y));
            value=Mathf.Abs(p.y-q.y)<1&&Clear(p,q,null,out _);edges[key]=value;edges[(b,a)]=value;return value;
        }
        public bool Route(GardenActor actor,Vector3 target)
        {
            if(frame!=Time.frameCount){frame=Time.frameCount;budget=2;}if(budget<=0)return false;budget--;
            if(Time.time>=expires)Invalidate();var grid=game.World.HexGrid;if(!grid.TryCell(actor.transform.position,out var start))return false;
            open.Clear();nodes.Clear();var first=new Node{cell=start};open.Add(first);nodes[start]=first;Node best=first;float bestDistance=float.MaxValue;
            for(int iteration=0;iteration<192&&open.Count>0;iteration++)
            {
                int at=0;for(int i=1;i<open.Count;i++)if(open[i].f<open[at].f)at=i;var n=open[at];open.RemoveAt(at);n.closed=true;
                var p=HexWorldGrid.Center(n.cell.x,n.cell.y);float distance=WorldRelief.PlanarDistance(p,target);if(distance<bestDistance){best=n;bestDistance=distance;}
                if(distance<2.2f)break;
                for(int e=0;e<6;e++)
                {
                    var c=PastureFences.Neighbor(n.cell,e);if(!Walkable(c)||!Edge(n.cell,c))continue;float cost=n.g+2;
                    if(!nodes.TryGetValue(c,out var child)){child=new Node{cell=c,g=float.MaxValue};nodes[c]=child;}
                    if(child.closed||cost>=child.g)continue;child.g=cost;child.f=cost+WorldRelief.PlanarDistance(HexWorldGrid.Center(c.x,c.y),target);child.parent=n;if(!open.Contains(child))open.Add(child);
                }
            }
            actor.path.Clear();actor.pathIndex=0;actor.pathGoal=target;actor.repathAt=Time.time+1.2f;
            for(var n=best;n!=null&&n.parent!=null;n=n.parent)actor.path.Add(grid.SurfacePoint(HexWorldGrid.Center(n.cell.x,n.cell.y)));actor.path.Reverse();return actor.path.Count>0;
        }
    }
}
