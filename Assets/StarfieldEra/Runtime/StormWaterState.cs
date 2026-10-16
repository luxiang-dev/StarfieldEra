using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace StarfieldEra
{
    [Serializable] public sealed class RainPuddle {public int column,row;public float depth;public Vector2Int Cell=>new Vector2Int(column,row);}
    [Serializable] public sealed class StormWaterState
    {
        public int lastDay=-1;public List<RainPuddle> puddles=new List<RainPuddle>();
        [NonSerialized] Dictionary<Vector2Int,RainPuddle> lookup;[NonSerialized]int indexed=-1;
        public float Depth(Vector2Int c){if(lookup==null||indexed!=puddles.Count){lookup=puddles.ToDictionary(p=>p.Cell);indexed=puddles.Count;}return lookup.TryGetValue(c,out var p)?p.depth:0;}
        public static Vector2Int CellAt(Vector3 p){int row=Mathf.RoundToInt((p.z-.15f)/HexWorldGrid.RowSpacing);return new Vector2Int(Mathf.RoundToInt((p.x-.5f)/HexWorldGrid.ColumnSpacing-.5f*(row&1)),row);}
        public float DepthAt(Vector3 p)=>Depth(CellAt(p));
        public float GroundDrop(Vector3 p)
        {
            var c=CellAt(p);float depth=Depth(c);if(depth<=0)return 0;var delta=p-HexWorldGrid.Center(c.x,c.y);float edge=-10;
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;edge=Mathf.Max(edge,delta.x*Mathf.Cos(a)+delta.z*Mathf.Sin(a));}
            return depth*.28f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.01f,.4f,edge));
        }
        public static HashSet<Vector2Int> ConnectedDrains(LandState land,Func<Vector2Int,float> height,Func<Vector2Int,TerrainKind> kind)
        {
            var drains=new HashSet<Vector2Int>(land.cells.Where(c=>c.drainage).Select(c=>c.Cell));var connected=new HashSet<Vector2Int>();var queue=new Queue<Vector2Int>();
            foreach(var c in drains)for(int i=0;i<6;i++){var n=PastureFences.Neighbor(c,i);if(kind(n)==TerrainKind.Water||!drains.Contains(n)&&height(n)<height(c)-.12f){if(connected.Add(c))queue.Enqueue(c);break;}}
            while(queue.Count>0){var c=queue.Dequeue();for(int i=0;i<6;i++){var n=PastureFences.Neighbor(c,i);if(drains.Contains(n)&&height(n)>=height(c)-.08f&&connected.Add(n))queue.Enqueue(n);}}
            return connected;
        }
        public void Step(FarmState farm,IEnumerable<Vector2Int> cells,Func<Vector2Int,float> height,Func<Vector2Int,TerrainKind> kind,Func<Vector2Int,bool> protectedCell)
        {
            if(lastDay==farm.day)return;lastDay=farm.day;var weather=farm.Weather;
            float rain=weather==WeatherType.Thunderstorm?.30f:weather==WeatherType.HeavyRain?.18f:0;
            float evaporation=weather==WeatherType.Sunny?.18f:weather==WeatherType.Cloudy?.09f:weather==WeatherType.LightRain?.045f:.025f;
            var drains=ConnectedDrains(farm.land,height,kind);var allDrains=new HashSet<Vector2Int>(farm.land.cells.Where(c=>c.drainage).Select(c=>c.Cell));var next=new List<RainPuddle>();
            foreach(var c in cells)
            {
                var terrain=kind(c);if(terrain==TerrainKind.Water||terrain==TerrainKind.Road||terrain==TerrainKind.Snow||farm.land.Find(c)?.channel==true||protectedCell(c))continue;
                float h=height(c),rim=0;for(int i=0;i<6;i++)rim+=height(PastureFences.Neighbor(c,i));rim/=6;
                float basin=Mathf.Max(0,rim-h);bool low=h<.28f||basin>.025f;
                float depth=Depth(c)-evaporation;if(low&&rain>0)depth+=rain*Mathf.Clamp(.4f+basin*3,.4f,1.5f);
                float drain=0;foreach(var n in allDrains)if(WorldRelief.PlanarDistance(HexWorldGrid.Center(c.x,c.y),HexWorldGrid.Center(n.x,n.y))<4.2f)drain=Mathf.Max(drain,drains.Contains(n)?.38f:.065f);
                depth-=drain;if(depth>.025f)next.Add(new RainPuddle{column=c.x,row=c.y,depth=Mathf.Min(.6f,depth)});
            }
            puddles=next.OrderByDescending(p=>p.depth).ThenBy(p=>p.row).ThenBy(p=>p.column).Take(4096).ToList();lookup=null;indexed=-1;
        }
        public void DrainNow(Vector2Int c,bool connected)
        {foreach(var p in puddles)if(WorldRelief.PlanarDistance(HexWorldGrid.Center(c.x,c.y),HexWorldGrid.Center(p.column,p.row))<4.2f)p.depth=Mathf.Max(0,p.depth-(connected?.38f:.065f));puddles.RemoveAll(p=>p.depth<=.025f);lookup=null;indexed=-1;}
        public bool Valid()=>lastDay>=-1&&puddles!=null&&puddles.Count<=4096&&puddles.All(p=>p!=null&&p.column>=HexWorldGrid.MinColumn&&p.column<=HexWorldGrid.MaxColumn&&p.row>=HexWorldGrid.MinRow&&p.row<=HexWorldGrid.MaxRow&&!float.IsNaN(p.depth)&&!float.IsInfinity(p.depth)&&p.depth>0&&p.depth<=.6f)&&puddles.Select(p=>p.Cell).Distinct().Count()==puddles.Count;
    }
}
