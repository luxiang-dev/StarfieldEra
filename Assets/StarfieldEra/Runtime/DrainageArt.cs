using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace StarfieldEra
{
    /// <summary>Combined earth banks, stone coping and animated channel water. No per-cell renderers.</summary>
    public sealed class DrainageArt:MonoBehaviour
    {
        readonly List<Object> owned=new List<Object>();
        Material flow;float wetUntil;FarmWorld world;
        sealed class Geometry
        {
            public readonly List<Vector3> vertices=new List<Vector3>();public readonly List<Vector2> uv=new List<Vector2>();public readonly List<int> triangles=new List<int>();
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float length=1,float start=0)
            {if(Vector3.Cross(c-a,b-a).y<0){var t=a;a=b;b=t;t=c;c=d;d=t;}int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);uv.Add(new Vector2(0,start));uv.Add(new Vector2(1,start));uv.Add(new Vector2(0,start+length));uv.Add(new Vector2(1,start+length));triangles.Add(n);triangles.Add(n+2);triangles.Add(n+1);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n+3);}
        }
        public void Build(FarmWorld source,DrainageNetwork channels)
        {
            world=source;if(channels.Segments.Count==0)return;
            var earth=new Geometry();var bed=new Geometry();var stone=new Geometry();var water=new Geometry();
            foreach(var s in channels.Segments)
            {
                var axis=(s.b-s.a).normalized;var side=new Vector3(axis.z,0,-axis.x);float length=Vector3.Distance(s.a,s.b);int steps=Mathf.Max(2,Mathf.CeilToInt(length/.18f));
                for(int i=0;i<steps;i++)
                {
                    var a=Vector3.Lerp(s.a,s.b,i/(float)steps);var b=Vector3.Lerp(s.a,s.b,(i+1f)/steps);
                    bed.Quad(Ground(a-side*.22f),Ground(a+side*.22f),Ground(b-side*.22f),Ground(b+side*.22f),.35f);
                    for(int sign=-1;sign<=1;sign+=2)for(int bank=0;bank<5;bank++)
                    {
                        float inner=.22f+bank*.07f,outer=inner+.07f;
                        earth.Quad(Ground(a+side*(sign*inner)),Ground(a+side*(sign*outer)),Ground(b+side*(sign*inner)),Ground(b+side*(sign*outer)),.35f);
                    }
                    if(s.connected)water.Quad(Ground(a-side*.18f,.07f),Ground(a+side*.18f,.07f),Ground(b-side*.18f,.07f),Ground(b+side*.18f,.07f),length/steps,i*length/steps);
                }
                // Low, irregular stone coping sits on both banks, leaving the channel open.
                int blocks=Mathf.Max(1,Mathf.CeilToInt(length/.38f));
                for(int i=0;i<blocks;i++)for(int sign=-1;sign<=1;sign+=2)
                {
                    float t=(i+.5f)/blocks;var center=Vector3.Lerp(s.a,s.b,t)+side*(sign*.51f);
                    if(channels.Cut(center)>.08f)continue; // Open junctions: coping must not cross another channel.
                    float variation=.5f+.5f*Mathf.Sin(center.x*71+center.z*43);float half=length/blocks*.46f;
                    var a=Ground(center-axis*half-side*.07f,.045f);var b=Ground(center-axis*half+side*.07f,.045f);var c=Ground(center+axis*half-side*.07f,.045f);var d=Ground(center+axis*half+side*.07f,.045f);
                    var up=Vector3.up*(.065f+variation*.055f);stone.Quad(a+up,b+up,c+up,d+up);stone.Quad(a,b,a+up,b+up);stone.Quad(c,d,c+up,d+up);stone.Quad(a,c,a+up,c+up);stone.Quad(b,d,b+up,d+up);
                }
            }
            Draw("Excavated earth channel banks",earth,Paint("Drain bank earth",new Color(.36f,.25f,.14f),false));
            Draw("Dark gravel channel bed",bed,Paint("Damp channel gravel",new Color(.28f,.23f,.14f),true));
            Draw("Hand laid stone coping",stone,Paint("Weathered drain stones",new Color(.56f,.53f,.40f),true));
            flow=new Material(Shader.Find("StarfieldEra/Drainage Flow"));owned.Add(flow);flow.SetFloat("_Flow",0);Draw("Water flowing to outlet",water,flow);
        }
        Vector3 Ground(Vector3 p,float offset=.035f)
        {
            // Follow the cooked terrain triangles, including local trench refinement.
            p.y=world.HexGrid.RaycastGround(new Ray(p+Vector3.up*25,Vector3.down),out var hit)?hit.y+offset:world.HexGrid.GroundHeight(p)+offset;return p;
        }
        Material Paint(string title,Color color,bool grain)
        {
            var m=new Material(Shader.Find("StarfieldEra/Storybook Models")){name=title};m.SetColor("_BaseColor",color);owned.Add(m);
            var tex=new Texture2D(32,32,TextureFormat.RGBA32,false){name=title+" painted grain",wrapMode=TextureWrapMode.Repeat};var pixels=new Color[1024];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++){float n=Mathf.PerlinNoise(x*.32f+4,y*.32f+7);float fleck=grain&&((x*17+y*31)%23<3)?.72f:1;pixels[y*32+x]=Color.white*(Mathf.Lerp(.78f,1.15f,n)*fleck);pixels[y*32+x].a=1;}tex.SetPixels(pixels);tex.Apply(false,true);m.SetTexture("_BaseMap",tex);owned.Add(tex);return m;
        }
        void Draw(string title,Geometry data,Material mat)
        {
            if(data.vertices.Count==0)return;var mesh=new Mesh{name=title,indexFormat=IndexFormat.UInt32};mesh.SetVertices(data.vertices);mesh.SetUVs(0,data.uv);mesh.SetTriangles(data.triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
            var go=new GameObject(title,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;
        }
        public void SetFlowing(bool flowing){if(flowing)wetUntil=Time.time+1.5f;if(flow)flow.SetFloat("_Flow",Time.time<wetUntil?1:0);}
        void OnDestroy(){foreach(var asset in owned)if(asset)Destroy(asset);}
    }
}
