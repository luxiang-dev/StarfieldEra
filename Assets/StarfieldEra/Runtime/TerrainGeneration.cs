using System.Collections.Generic;
using UnityEngine;
namespace StarfieldEra
{
    public sealed partial class HexWorldGrid
    {
        public void CompleteTerrain(FarmWorld world,FarmState state)
        {
            // Older decorative patches now receive explicit gameplay data.
            MarkBox(new Vector3(-2,0,2.6f),new Vector3(14,0,1.9f),TerrainKind.Road);
            MarkBox(new Vector3(-5.6f,0,-4),new Vector3(4.4f,0,4),TerrainKind.Sand);
            MarkBox(new Vector3(-5.6f,0,-4),new Vector3(3.4f,0,2.8f),TerrainKind.Water);
            var protectedCells=new HashSet<Vector2Int>();
            foreach(var b in state.adventure.buildings)foreach(var c in BuildingFootprints.Cells(b))protectedCells.Add(c);
            foreach(var f in state.adventure.fences){var c=new Vector2Int(f.column,f.row);protectedCells.Add(c);protectedCells.Add(PastureFences.Neighbor(c,f.edge));}
            foreach(var a in state.adventure.animals){protectedCells.Add(new Vector2Int(a.homeColumn,a.homeRow));if(TryCell(new Vector3(a.x,0,a.z),out var c))protectedCells.Add(c);}
            var palettes=new Material[8,2];
            for(int k=0;k<8;k++)for(int v=0;v<2;v++)
            {var source=world.Mat("Terrain "+(TerrainKind)k+" "+v,TerrainTraits.Colors[k]);if(v==1)source.color*=.95f;palettes[k,v]=Surface(source);}
            var reed=world.Mat("Marsh reeds","A6AD6D");var ripple=world.Mat("Water ripples","83BFBB");
            var decoration=new GameObject("Terrain details");decoration.transform.SetParent(transform,false);
            foreach(var pair in cells)
            {
                var c=pair.Key;var p=Center(c.x,c.y);var kind=terrain[c];
                bool protectedSite=protectedCells.Contains(c);
                foreach(var e in world.Adventure.Encounters)if(Vector3.Distance(p,e.Position)<3)protectedSite=true;
                if(kind==TerrainKind.Rock&&p.x>29&&p.z>27)kind=TerrainKind.Snow;
                if(kind==TerrainKind.Meadow&&(Mathf.Abs(p.x)>10||Mathf.Abs(p.z)>15))
                {float noise=Mathf.PerlinNoise((p.x+80)*.12f,(p.z+60)*.12f);if(noise>.66f)kind=TerrainKind.Forest;else if(noise<.29f)kind=TerrainKind.Sand;}
                bool water=false,shore=false;
                if(p.z>-13&&p.z<12){float d=Mathf.Abs(p.x-(32+Mathf.Sin(p.z*.22f)*2));water=d<1.25f;shore=d<2.7f;}
                float lake=Mathf.Pow((p.x+26)/3.7f,2)+Mathf.Pow((p.z-5)/2.7f,2);water|=lake<1;shore|=lake<1.7f;
                float wetlandPool=Mathf.Pow((p.x+33)/3.4f,2)+Mathf.Pow((p.z-29)/2.5f,2);water|=wetlandPool<1;
                if(kind!=TerrainKind.Road&&!protectedSite)
                {if(water)kind=TerrainKind.Water;else if(shore)kind=TerrainKind.Sand;}
                terrain[c]=kind;var colors=pair.Value.sharedMaterials;colors[0]=palettes[(int)kind,Mathf.Abs(c.x*31+c.y*17)%2];pair.Value.sharedMaterials=colors;
                int hash=Mathf.Abs(c.x*19+c.y*43);
                if(kind==TerrainKind.Marsh&&hash%4==0)
                {
                    for(int i=0;i<3;i++)
                    {var stem=world.Shape("Marsh reeds",PrimitiveType.Cube,p+new Vector3((i-1)*.13f,.28f,.2f),new Vector3(.025f,.55f,.025f),reed);stem.SetParent(decoration.transform,true);}
                }
                if(kind==TerrainKind.Water&&hash%2==0)
                {var wave=world.Shape("Water ripple",PrimitiveType.Cube,p+Vector3.up*.03f,new Vector3(.6f,.015f,.035f),ripple);wave.SetParent(decoration.transform,true);wave.rotation=Quaternion.Euler(0,hash%40-20,0);}
            }
            // Hex data provides the shoreline boundary for both movement and construction.
            foreach(var col in world.GetComponentsInChildren<Collider>())if(col.name=="Lake collision"||col.name=="Pond obstacle")col.enabled=false;
        }
        void MarkBox(Vector3 center,Vector3 size,TerrainKind kind)
        {foreach(var c in cells.Keys){var p=Center(c.x,c.y);if(Mathf.Abs(p.x-center.x)<=size.x*.5f&&Mathf.Abs(p.z-center.z)<=size.z*.5f)terrain[c]=kind;}}
    }
}
