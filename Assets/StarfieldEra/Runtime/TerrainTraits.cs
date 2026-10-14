namespace StarfieldEra
{
    public enum TerrainKind { Meadow, Forest, Marsh, Sand, Rock, Snow, Road, Water }
    public static class TerrainTraits
    {
        public static readonly string[] Names={"草地","森林","沼泽","沙地","岩地","雪地","道路","水面"};
        public static readonly float[] Speed={1,.8f,.45f,.8f,.7f,.65f,1.2f,0};
        public static readonly string[] Colors={"87A653","557B48","496B67","C7B780","A2ACA2","D8E5DC","C7B997","4597A8"};
        public static bool Walkable(TerrainKind kind)=>kind!=TerrainKind.Water;
        public static bool Buildable(TerrainKind kind)=>kind!=TerrainKind.Water&&kind!=TerrainKind.Marsh;
        public static string Hint(TerrainKind kind,float? speed=null)=>Names[(int)kind]+" · "+(kind==TerrainKind.Water?"不可通行、不可建造":"移动 "+UnityEngine.Mathf.RoundToInt((speed??Speed[(int)kind])*100)+"%"+(kind==TerrainKind.Marsh?" · 不可建造":""));
    }
}
