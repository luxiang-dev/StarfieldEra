using System.Collections.Generic;
using UnityEngine;
namespace StarfieldEra
{
    /// <summary>One water-level authority for saved canal connectivity and rendered riverbeds.</summary>
    public static class CanalHydrology
    {
        public const float BedDepth=.6f,MaxExcavation=1.8f;
        public static Dictionary<Vector2Int,float> Resolve(HexWorldGrid grid,LandState state,Vector2Int? extra=null)
        {
            var channels=new HashSet<Vector2Int>();if(state!=null)foreach(var c in state.cells)if(c.channel)channels.Add(c.Cell);if(extra.HasValue)channels.Add(extra.Value);
            var edits=new Dictionary<Vector2Int,TerrainKind>();if(state!=null)foreach(var c in state.cells)edits[c.Cell]=(TerrainKind)c.kind;
            TerrainKind Kind(Vector2Int cell)=>edits.TryGetValue(cell,out var kind)?kind:grid.TerrainAt(cell);
            var levels=new Dictionary<Vector2Int,float>();var seen=new HashSet<Vector2Int>();var flow=new Queue<Vector2Int>();
            // Find actual lake levels from the natural shoreline, never from submerged land at a lake's center.
            foreach(var source in grid.Cells)
            {
                if(channels.Contains(source)||Kind(source)!=TerrainKind.Water||!seen.Add(source))continue;
                var queue=new Queue<Vector2Int>();var lake=new List<Vector2Int>();queue.Enqueue(source);float shore=float.MaxValue;
                while(queue.Count>0)
                {
                    var cell=queue.Dequeue();lake.Add(cell);
                    for(int edge=0;edge<6;edge++)
                    {
                        var n=PastureFences.Neighbor(cell,edge);
                        if(grid.Contains(n)&&!channels.Contains(n)&&Kind(n)==TerrainKind.Water){if(seen.Add(n))queue.Enqueue(n);}
                        else shore=Mathf.Min(shore,WorldRelief.LandHeight((HexWorldGrid.Center(cell.x,cell.y)+HexWorldGrid.Center(n.x,n.y))*.5f));
                    }
                }
                float level=Mathf.Max(.03f,shore-.1f);foreach(var cell in lake){levels[cell]=level;flow.Enqueue(cell);}
            }
            // All connected canal cells inherit their source's surface height. Cut the bed to that height.
            while(flow.Count>0)
            {
                var cell=flow.Dequeue();float level=levels[cell];
                for(int edge=0;edge<6;edge++)
                {
                    var n=PastureFences.Neighbor(cell,edge);if(!channels.Contains(n)||!grid.Contains(n))continue;
                    float required=LandElevation.Height(HexWorldGrid.Center(n.x,n.y),state)-(level-BedDepth);
                    if(required>MaxExcavation+.001f)continue;
                    if(levels.TryGetValue(n,out var existing)&&existing>=level-.001f)continue;
                    levels[n]=level;flow.Enqueue(n);
                }
            }
            return levels;
        }
        public static void Apply(HexWorldGrid grid,LandState state)
        {
            grid.SetExcavated(state?.cells);
            if(state==null)return;var levels=Resolve(grid,state);
            foreach(var c in state.cells)if(c.channel){c.kind=(int)(levels.ContainsKey(c.Cell)?TerrainKind.Water:TerrainKind.Rock);grid.SetTerrain(c.Cell,(TerrainKind)c.kind);}
        }
    }
}
