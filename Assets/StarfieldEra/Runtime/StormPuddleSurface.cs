using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace StarfieldEra
{
    /// <summary>Blends saved water depths into an organic surface; hexes remain gameplay records.</summary>
    public sealed class PuddleField
    {
        sealed class Node {public Vector3 center;public float depth,level;}
        readonly Dictionary<Vector2Int,Node> nodes=new Dictionary<Vector2Int,Node>();
        readonly System.Func<Vector3,float> ground;
        public PuddleField(StormWaterState storm,System.Func<Vector3,float> height)
        {ground=height;foreach(var p in storm.puddles){var c=HexWorldGrid.Center(p.column,p.row);nodes[p.Cell]=new Node{center=c,depth=p.depth,level=ground(c)+p.depth+.018f};}}
        public void Refresh(StormWaterState storm)
        {foreach(var pair in nodes){float depth=storm.Depth(pair.Key);if(Mathf.Abs(depth-pair.Value.depth)<.0001f)continue;pair.Value.depth=depth;pair.Value.level=ground(pair.Value.center)+depth+.018f;}}
        public bool Sample(Vector3 p,out float coverage,out float level)
        {
            var cell=StormWaterState.CellAt(p);float field=-10,total=0,sum=0;p.y=0;
            for(int r=cell.y-1;r<=cell.y+1;r++)for(int c=cell.x-1;c<=cell.x+1;c++)
            {
                if(!nodes.TryGetValue(new Vector2Int(c,r),out var n)||n.depth<=.025f)continue;
                float radius=Mathf.Lerp(.60f,1.20f,Mathf.Sqrt(Mathf.Clamp01(n.depth/.45f)));
                float distance=(p-n.center).magnitude;float d=radius-distance;
                // Smooth union removes internal cell borders without adding overlapping water faces.
                float h=Mathf.Max(.38f-Mathf.Abs(field-d),0)/.38f;field=Mathf.Max(field,d)+h*h*.095f;
                float w=Mathf.Max(0,1-distance/(radius+.55f));w=w*w*w;total+=w;sum+=n.level*w;
            }
            float noise=(Mathf.PerlinNoise(p.x*1.45f+21,p.z*1.45f+47)-.5f)*.26f;
            coverage=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.30f,.15f,field+noise));level=total>0?sum/total:0;
            return field>-.65f&&total>0;
        }
    }
    public sealed class StormPuddleSurface:MonoBehaviour
    {
        const float Step=.38f;
        PuddleField field;FarmWorld world;StormWaterState storm;Mesh mesh;Material material;
        readonly List<Vector3> points=new List<Vector3>();readonly List<Color> colors=new List<Color>();readonly List<float> ground=new List<float>();
        readonly List<int> changing=new List<int>();
        public int VertexCount=>points.Count;
        public void Build(FarmGame game)
        {
            world=game.World;storm=game.State.storm;if(storm.puddles.Count==0)return;
            field=new PuddleField(storm,world.HexGrid.GroundHeight);
            float minX=float.MaxValue,minZ=float.MaxValue,maxX=float.MinValue,maxZ=float.MinValue;
            foreach(var p in storm.puddles){var c=HexWorldGrid.Center(p.column,p.row);minX=Mathf.Min(minX,c.x-2.4f);maxX=Mathf.Max(maxX,c.x+2.4f);minZ=Mathf.Min(minZ,c.z-2.4f);maxZ=Mathf.Max(maxZ,c.z+2.4f);}
            minX=Mathf.Floor(minX/Step)*Step;minZ=Mathf.Floor(minZ/Step)*Step;int nx=Mathf.CeilToInt((maxX-minX)/Step),nz=Mathf.CeilToInt((maxZ-minZ)/Step);
            var indices=new int[(nx+1)*(nz+1)];for(int i=0;i<indices.Length;i++)indices[i]=-1;
            var network=world.HexGrid.GetComponent<ContinuousTerrain>().Drainage;
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
            {
                var p=new Vector3(minX+x*Step,0,minZ+z*Step);if(!field.Sample(p,out float coverage,out float level))continue;
                int index=points.Count;indices[z*(nx+1)+x]=index;float floor=world.HexGrid.GroundHeight(p);ground.Add(floor);p.y=Mathf.Max(floor+.018f,level);points.Add(p);colors.Add(new Color(coverage,Mathf.Max(0,level-floor),0,1));
                if(network.IntakeFloors.Count>0){var cell=StormWaterState.CellAt(p);bool affected=network.IntakeFloors.ContainsKey(cell);
                    for(int edge=0;!affected&&edge<6;edge++)affected=network.IntakeFloors.ContainsKey(PastureFences.Neighbor(cell,edge));if(affected)changing.Add(index);}
            }
            var triangles=new List<int>(points.Count*6);
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {int i=z*(nx+1)+x,a=indices[i],b=indices[i+1],c=indices[i+nx+1],d=indices[i+nx+2];if(a<0||b<0||c<0||d<0)continue;triangles.Add(a);triangles.Add(c);triangles.Add(b);triangles.Add(b);triangles.Add(c);triangles.Add(d);}
            mesh=new Mesh{name="Continuous rain pools with soft shores",indexFormat=IndexFormat.UInt32};mesh.SetVertices(points);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            material=new Material(Shader.Find("StarfieldEra/Rain Puddles"));material.SetFloat("_Rain",game.State.Weather==WeatherType.Thunderstorm||game.State.Weather==WeatherType.HeavyRain?1:game.State.Weather==WeatherType.LightRain?.45f:0);
            var filter=gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
        }
        void SetPoint(int i)
        {
            var p=points[i];field.Sample(p,out float coverage,out float level);float depth=Mathf.Max(0,level-ground[i]);p.y=Mathf.Max(ground[i]+.018f,level);points[i]=p;colors[i]=new Color(coverage,depth,0,1);
        }
        public void Refresh(bool groundChanged=false)
        {
            if(!mesh)return;field.Refresh(storm);
            foreach(int i in changing){if(groundChanged)ground[i]=world.HexGrid.GroundHeight(points[i]);SetPoint(i);}
            mesh.SetVertices(points);mesh.SetColors(colors);mesh.RecalculateBounds();
        }
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
