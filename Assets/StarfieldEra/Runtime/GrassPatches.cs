using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace StarfieldEra
{
    /// <summary>One combined decorative mesh of differently sized and colored grass clumps.</summary>
    public sealed class GrassPatches:MonoBehaviour
    {
        Mesh mesh;Material material;
        struct Patch {public Vector3 point;public int start,count;}
        readonly List<Patch> patches=new List<Patch>();
        public int ClumpCount {get;private set;}
        public int[] VarietyCounts {get;}=new int[4];
        public void Build(FarmWorld world)
        {
            if(mesh)Destroy(mesh);if(material)Destroy(material);ClumpCount=0;patches.Clear();System.Array.Clear(VarietyCounts,0,4);
            foreach(Transform child in transform)if(child.name=="Mixed grass sizes"){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var random=new System.Random(1947);var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
            var occupied=new HashSet<Vector2Int>();var owner=world.GetComponentInParent<FarmGame>();
            foreach(var building in owner.State.adventure.buildings)foreach(var cell in BuildingFootprints.Cells(building))occupied.Add(cell);
            var shades=new[]{new Color(.66f,.63f,.31f),new Color(.51f,.66f,.34f),new Color(.26f,.47f,.26f),new Color(.47f,.61f,.26f)};
            for(float z=-HexWorldGrid.HalfDepth+3;z<HexWorldGrid.HalfDepth-2;z+=1.3f)for(float x=-HexWorldGrid.HalfWidth+3;x<HexWorldGrid.HalfWidth-2;x+=1.3f)
            {
                var p=new Vector3(x+((float)random.NextDouble()-.5f)*.95f,0,z+((float)random.NextDouble()-.5f)*.95f);
                var kind=world.HexGrid.TerrainAt(p);if(kind!=TerrainKind.Meadow&&kind!=TerrainKind.Forest&&kind!=TerrainKind.Marsh)continue;
                if(!world.HexGrid.CanStand(p,.35f)||!world.HexGrid.TryCell(p,out var cell)||occupied.Contains(cell))continue;
                bool reserved=false;for(int i=0;i<owner.State.AvailablePlotCount;i++)if(WorldRelief.PlanarDistance(p,world.PlotPositions[i])<1.4f){reserved=true;break;}
                foreach(var encounter in world.Adventure.Encounters)if(WorldRelief.PlanarDistance(p,encounter.Position)<1.6f){reserved=true;break;}
                if(owner.State.land.Find(cell)?.tilled==true)reserved=true;
                if(reserved)continue;
                var weights=GrassEcology.Weights(p,world.HexGrid);if(random.NextDouble()>GrassEcology.Density(weights))continue;
                p=world.HexGrid.SurfacePoint(p,.01f);int variety=GrassEcology.Dominant(weights);ClumpCount++;VarietyCounts[variety]++;
                float size=variety==0?.16f:variety==1?.12f:variety==2?.3f:.48f;
                size*=.65f+(float)random.NextDouble()*.8f;int leaves=variety==0?2:variety==1?3:variety==2?7:5;
                var shade=shades[variety]*(.88f+(float)random.NextDouble()*.22f);shade.a=1;
                int start=vertices.Count;
                for(int leaf=0;leaf<leaves;leaf++)
                {
                    float angle=(float)random.NextDouble()*Mathf.PI*2;var width=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(size*.12f);
                    var lean=new Vector3(Mathf.Sin(angle),0,-Mathf.Cos(angle))*size*(.12f+(float)random.NextDouble()*.22f);
                    var basePoint=p+new Vector3(((float)random.NextDouble()-.5f)*size*.25f,0,((float)random.NextDouble()-.5f)*size*.25f);
                    int n=vertices.Count;vertices.Add(basePoint-width);vertices.Add(basePoint+width);vertices.Add(basePoint+lean*.65f+Vector3.up*size*.67f);vertices.Add(basePoint+lean+Vector3.up*size);
                    colors.Add((shade*.72f).linear);colors.Add((shade*.72f).linear);colors.Add(shade.linear);colors.Add((shade*1.1f).linear);
                    triangles.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});
                }
                patches.Add(new Patch{point=p,start=start,count=vertices.Count-start});
            }
            mesh=new Mesh{name="Diverse painted grass clumps",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject("Mixed grass sizes",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            material=new Material(Shader.Find("StarfieldEra/Painted Grass"));var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        public void RefreshHeights(FarmWorld world,Vector3 center,float radius=2.6f)
        {
            if(!mesh)return;var points=mesh.vertices;
            for(int i=0;i<patches.Count;i++)
            {
                var patch=patches[i];if(WorldRelief.PlanarDistance(patch.point,center)>radius)continue;
                float height=world.HexGrid.GroundHeight(patch.point)+.01f,delta=height-patch.point.y;
                for(int j=patch.start;j<patch.start+patch.count;j++)points[j].y+=delta;
                patch.point.y=height;patches[i]=patch;
            }
            mesh.vertices=points;mesh.RecalculateBounds();
        }
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
