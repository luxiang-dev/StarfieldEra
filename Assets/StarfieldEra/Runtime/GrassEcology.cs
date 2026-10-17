using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Visual grass varieties. They do not change terrain rules or saved gameplay.</summary>
    public static class GrassEcology
    {
        public static Vector4 Weights(Vector3 p,HexWorldGrid grid)
        {
            var kind=grid.TerrainAt(p);float path=0;
            if(grid.TryCell(p,out var cell))for(int edge=0;edge<6;edge++)
            {
                var neighbor=PastureFences.Neighbor(cell,edge);var terrain=grid.TerrainAt(neighbor);
                if(terrain!=TerrainKind.Road&&terrain!=TerrainKind.Sand)continue;
                float d=WorldRelief.PlanarDistance(p,HexWorldGrid.Center(neighbor.x,neighbor.y));path=Mathf.Max(path,Mathf.Clamp01(1-d/3.4f));
            }
            float exposure=Mathf.PerlinNoise(p.x*.105f+71,p.z*.105f+29);
            float moisture=Mathf.PerlinNoise(p.x*.09f+23,p.z*.09f+87);
            float meadow=Mathf.PerlinNoise(p.x*.075f+44,p.z*.075f+11);
            float dry=Mathf.Clamp01((exposure-.56f)*4)+path*.18f;
            float shortGrass=.3f+path*.75f;
            float dense=.10f+Mathf.Clamp01((moisture-.42f)*2)*.45f+(kind==TerrainKind.Forest?.65f:kind==TerrainKind.Marsh?.8f:0);
            float tall=Mathf.Clamp01((meadow-.5f)*2.7f)*.9f*(1-path);
            var weights=new Vector4(Mathf.Pow(dry,3),Mathf.Pow(shortGrass,3),Mathf.Pow(dense,3),Mathf.Pow(tall,3));
            return weights/(weights.x+weights.y+weights.z+weights.w);
        }
        public static int Dominant(Vector4 w)=>w.x>w.y&&w.x>w.z&&w.x>w.w?0:w.y>w.z&&w.y>w.w?1:w.z>w.w?2:3;
        public static float Density(Vector4 w)=>Vector4.Dot(w,new Vector4(.10f,.23f,.68f,.47f));
    }
}
