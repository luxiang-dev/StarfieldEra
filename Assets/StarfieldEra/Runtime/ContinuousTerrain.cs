using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StarfieldEra
{
    /// <summary>A continuous visual surface; hex records remain the authority for gameplay.</summary>
    public sealed class ContinuousTerrain : MonoBehaviour
    {
        readonly List<Object> owned=new List<Object>();
        HexWorldGrid grid;
        FarmGame owner;
        Texture2D atlas;
        readonly List<Vector3> shoreEdges=new List<Vector3>();
        readonly Dictionary<Vector2Int,float> waterLevels=new Dictionary<Vector2Int,float>();
        MeshCollider groundCollider;
        public DrainageNetwork Drainage {get;private set;}
        public bool IsGround(Collider collider)=>collider==groundCollider;
        public bool Raycast(Ray ray,out RaycastHit hit)=>groundCollider.Raycast(ray,out hit,300);
        public float HeightAt(Vector3 p)
        {
            p.y=0;Sample(p,out _,out float water,out _);
            return HeightAt(p,water);
        }
        float HeightAt(Vector3 p,float water)
        {
            float h=LandElevation.Height(p,owner?.State?.land)-(owner?owner.State.storm.GroundDrop(p):0);
            if(owner&&owner.State.land!=null&&grid.TryCell(p,out var cell))
            {
                var edit=owner.State.land.Find(cell);
                if(edit!=null&&edit.channel)
                {
                    var delta=p-HexWorldGrid.Center(cell.x,cell.y);float edge=-10;
                    for(int i=0;i<6;i++){float angle=i*Mathf.PI/3;edge=Mathf.Max(edge,delta.x*Mathf.Cos(angle)+delta.z*Mathf.Sin(angle));}
                    float cut=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.38f,HexWorldGrid.Radius*Mathf.Sqrt(3)*.5f-edge));
                    float bed=waterLevels.TryGetValue(cell,out float level)?Mathf.Min(h,level-CanalHydrology.BedDepth):h-.45f;
                    h=Mathf.Lerp(h,bed,cut);
                }

            }
            if(Drainage!=null)h-=Drainage.Cut(p);
            if(water<=0)return h;
            return Mathf.Lerp(h,WaterLevelAt(p)-.6f,Mathf.SmoothStep(0,1,Mathf.Clamp01(water*1.35f)));
        }
        // The blended shoreline depresses neighbouring dry hexes too. Terrain enums alone
        // cannot describe the safe bank: keep feet above the actual water surface.
        public bool SupportsWalking(Vector3 p)
        {
            if(WaterDistance(p)<-.8f)return true;
            return HeightAt(p)>=WaterLevelAt(p)+.12f;
        }
        public bool HasWaterAt(Vector3 p)=>WaterDistance(p)>=0&&HeightAt(p)<WaterLevelAt(p);
        public float WaterLevelAt(Vector3 p)
        {
            float nearest=float.MaxValue,level=.015f;p.y=0;
            foreach(var pair in waterLevels){float d=(HexWorldGrid.Center(pair.Key.x,pair.Key.y)-p).sqrMagnitude;if(d<nearest){nearest=d;level=pair.Value;}}
            return level;
        }
        void BuildWaterLevels()
        {
            Drainage=new DrainageNetwork(owner?owner.State.land:new LandState(),c=>LandElevation.Height(HexWorldGrid.Center(c.x,c.y),owner?owner.State.land:null),grid.TerrainAt);
            shoreEdges.Clear();
            foreach(var cell in grid.Cells)if(grid.TerrainAt(cell)==TerrainKind.Water)
                for(int edge=0;edge<6;edge++)if(grid.TerrainAt(PastureFences.Neighbor(cell,edge))!=TerrainKind.Water)
                {var center=HexWorldGrid.Center(cell.x,cell.y);shoreEdges.Add(center+HexWorldGrid.Corner(edge,HexWorldGrid.Radius));shoreEdges.Add(center+HexWorldGrid.Corner((edge+1)%6,HexWorldGrid.Radius));}
            waterLevels.Clear();
            foreach(var pair in CanalHydrology.Resolve(grid,owner?owner.State.land:null))waterLevels[pair.Key]=pair.Value;
        }
        const float Step=.6f;
        public void Build(HexWorldGrid source,FarmWorld world)
        {
            grid=source;owner=GetComponentInParent<FarmGame>();atlas=Resources.Load<Texture2D>("TerrainArt/ReferenceAtlas");
            RebuildSurface();
            var cameraData=world.View.GetComponent<UniversalAdditionalCameraData>();if(cameraData)cameraData.requiresDepthTexture=true;
            DecorateBanks(world);GroundWorld(world);
            world.gameObject.AddComponent<FantasyPresentation>().Build(world);
            world.gameObject.AddComponent<GrassPatches>().Build(world);
            world.gameObject.AddComponent<FarmFieldArt>().Build(world);
        }
        public void RebuildSurface()
        {
            BuildWaterLevels();
            foreach(Transform child in transform)if(child.name=="Continuous land and lakebed"||child.name=="Animated translucent water")
            {child.gameObject.SetActive(false);Destroy(child.gameObject);}
            foreach(var asset in owned)if(asset)Destroy(asset);owned.Clear();
            int nx=Mathf.CeilToInt(HexWorldGrid.HalfWidth*2/Step),nz=Mathf.CeilToInt(HexWorldGrid.HalfDepth*2/Step);
            int count=(nx+1)*(nz+1);
            var vertices=new List<Vector3>(count);var colors=new List<Color>(count);var uv=new List<Vector2>(count);var varieties=new List<Vector4>(count);var triangles=new List<int>(nx*nz*6);
            var waterDistances=new float[count];
            int AddGround(Vector3 p)
            {
                Sample(p,out var weights,out float water,out float tint);p.y=HeightAt(p,water);
                int index=vertices.Count;vertices.Add(p);colors.Add(weights);uv.Add(new Vector2(tint,water));varieties.Add(GrassEcology.Weights(p,grid));return index;
            }
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
            {
                var p=new Vector3(-HexWorldGrid.HalfWidth+x*Step,0,-HexWorldGrid.HalfDepth+z*Step);
                waterDistances[vertices.Count]=WaterDistance(p);AddGround(p);
            }
            var waterVertices=new List<Vector3>();var waterColors=new List<Color>();var waterTriangles=new List<int>();
            var polygon=new List<Vector3>(4);
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                int a=z*(nx+1)+x,b=a+1,c=a+nx+1,d=c+1;
                var middle=(vertices[a]+vertices[d])*.5f;
                if(Drainage.NearChannel(middle,1.1f))
                {
                    // Only the strip around trenches needs a fine collision/render surface.
                    // The outer patch edge lies beyond the excavation, avoiding cracks at its rim.
                    int start=vertices.Count;
                    for(int rz=0;rz<=3;rz++)for(int rx=0;rx<=3;rx++)AddGround(new Vector3(vertices[a].x+rx*Step/3,0,vertices[a].z+rz*Step/3));
                    for(int rz=0;rz<3;rz++)for(int rx=0;rx<3;rx++){int i=start+rz*4+rx;triangles.Add(i);triangles.Add(i+4);triangles.Add(i+1);triangles.Add(i+1);triangles.Add(i+4);triangles.Add(i+5);}
                }
                else {triangles.Add(a);triangles.Add(c);triangles.Add(b);triangles.Add(b);triangles.Add(c);triangles.Add(d);}
                WaterTriangle(vertices[a],vertices[c],vertices[b],waterDistances[a],waterDistances[c],waterDistances[b],polygon,waterVertices,waterColors,waterTriangles);
                WaterTriangle(vertices[b],vertices[c],vertices[d],waterDistances[b],waterDistances[c],waterDistances[d],polygon,waterVertices,waterColors,waterTriangles);
            }
            var mesh=new Mesh{name="Continuous painted island",indexFormat=IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetUVs(1,uv);mesh.SetUVs(2,varieties);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
            var land=Draw("Continuous land and lakebed",mesh,Material("StarfieldEra/Continuous Ground"));
            groundCollider=land.gameObject.AddComponent<MeshCollider>();groundCollider.sharedMesh=mesh;
            grid.GetComponent<BoxCollider>().enabled=false;
            var waterMesh=new Mesh{name="Connected shoreline water",indexFormat=IndexFormat.UInt32};
            waterMesh.SetVertices(waterVertices);waterMesh.SetColors(waterColors);waterMesh.SetTriangles(waterTriangles,0);waterMesh.RecalculateNormals();waterMesh.RecalculateBounds();owned.Add(waterMesh);
            var waterRenderer=Draw("Animated translucent water",waterMesh,Material("StarfieldEra/Painted Water"));waterRenderer.shadowCastingMode=ShadowCastingMode.Off;
            foreach(Transform child in transform)if(child.name=="Floating lily pad"||child.name=="Submerged pebble")child.gameObject.SetActive(grid.TerrainAt(child.position)==TerrainKind.Water);
            Physics.SyncTransforms();
        }
        Material Material(string shaderName)
        {
            var material=new Material(Shader.Find(shaderName)){name=shaderName};
            if(material.HasProperty("_Atlas"))material.SetTexture("_Atlas",atlas);
            if(material.HasProperty("_GrassVariants"))material.SetTexture("_GrassVariants",Resources.Load<Texture2D>("TerrainArt/GrassVariants"));
            owned.Add(material);return material;
        }
        MeshRenderer Draw(string title,Mesh mesh,Material material)
        {
            var go=new GameObject(title,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;return r;
        }
        void Sample(Vector3 p,out Color weights,out float water,out float tint)
        {
            weights=Color.clear;water=0;tint=0;float total=0;
            int row=Mathf.RoundToInt((p.z-.15f)/HexWorldGrid.RowSpacing);
            for(int r=row-2;r<=row+2;r++)
            {
                int col=Mathf.RoundToInt((p.x-.5f)/HexWorldGrid.ColumnSpacing-.5f*(r&1));
                for(int c=col-2;c<=col+2;c++)
                {
                    var cell=new Vector2Int(c,r);if(!grid.Contains(cell))continue;
                    float d=WorldRelief.PlanarDistance(p,HexWorldGrid.Center(c,r));float w=Mathf.Pow(Mathf.Max(0,1-d/2.65f),3);if(w<=0)continue;
                    var kind=grid.TerrainAt(cell);total+=w;
                    if(kind==TerrainKind.Water){water+=w;weights.b+=w;}
                    else if(kind==TerrainKind.Sand)weights.b+=w;
                    else if(kind==TerrainKind.Snow)weights.a+=w;
                    else if(kind==TerrainKind.Road||kind==TerrainKind.Rock){weights.g+=w;tint+=w*(kind==TerrainKind.Rock?1:0);}
                    else {weights.r+=w;tint-=w*(kind==TerrainKind.Forest?.14f:kind==TerrainKind.Marsh?.3f:0);}
                }
            }
            if(total>0){weights/=total;water/=total;tint/=total;}else{weights=new Color(0,0,1,0);water=1;}
        }
        // Signed distance to the nearest authoritative water hex. Union contours are clipped
        // into one mesh, so all lakes keep the same boundary used by movement.
        float WaterDistance(Vector3 p)
        {
            float best=-10;int row=Mathf.RoundToInt((p.z-.15f)/HexWorldGrid.RowSpacing);
            for(int r=row-1;r<=row+1;r++)
            {
                int col=Mathf.RoundToInt((p.x-.5f)/HexWorldGrid.ColumnSpacing-.5f*(r&1));
                for(int c=col-1;c<=col+1;c++)
                {
                    var cell=new Vector2Int(c,r);if(!grid.Contains(cell)||grid.TerrainAt(cell)!=TerrainKind.Water)continue;
                    var q=p-HexWorldGrid.Center(c,r);float edge=float.MinValue;
                    for(int i=0;i<6;i++){float angle=i*Mathf.PI/3;edge=Mathf.Max(edge,q.x*Mathf.Cos(angle)+q.z*Mathf.Sin(angle));}
                    best=Mathf.Max(best,HexWorldGrid.Radius*Mathf.Sqrt(3)*.5f-edge);
                }
            }
            return best;
        }
        // Distance only to exposed shore edges; internal hex borders must never fade.
        float ShoreDistance(Vector3 p)
        {
            p.y=0;float best=100;
            for(int n=0;n<shoreEdges.Count;n+=2)
            {var a=shoreEdges[n];var ab=shoreEdges[n+1]-a;var q=a+ab*Mathf.Clamp01(Vector3.Dot(p-a,ab)/ab.sqrMagnitude);best=Mathf.Min(best,(p-q).sqrMagnitude);}
            return Mathf.Sqrt(best);
        }
        void WaterTriangle(Vector3 a,Vector3 b,Vector3 c,float da,float db,float dc,List<Vector3> polygon,List<Vector3> vertices,List<Color> colors,List<int> triangles)
        {
            if(da<0&&db<0&&dc<0)return;
            polygon.Clear();
            ClipWaterEdge(a,b,da,db,polygon);ClipWaterEdge(b,c,db,dc,polygon);ClipWaterEdge(c,a,dc,da,polygon);
            if(polygon.Count<3)return;int offset=vertices.Count;
            foreach(var p0 in polygon)
            {var p=p0;p.y=WaterLevelAt(p)+.015f;vertices.Add(p);Sample(p,out _,out float depth,out _);colors.Add(new Color(depth,ShoreDistance(p),0,1));}
            for(int i=1;i<polygon.Count-1;i++){triangles.Add(offset);triangles.Add(offset+i);triangles.Add(offset+i+1);}
        }
        static void ClipWaterEdge(Vector3 start,Vector3 end,float ds,float de,List<Vector3> polygon)
        {
            if(ds>=0)polygon.Add(start);
            if((ds>=0)!=(de>=0))polygon.Add(Vector3.Lerp(start,end,ds/(ds-de)));
        }
        void DecorateBanks(FarmWorld world)
        {
            var stone=world.Mat("Riverbed pebbles","8E9B88");var leaf=world.Mat("Lily pads","65934C");var random=new System.Random(491);
            for(int r=HexWorldGrid.MinRow;r<=HexWorldGrid.MaxRow;r++)for(int c=HexWorldGrid.MinColumn;c<=HexWorldGrid.MaxColumn;c++)
            {
                var cell=new Vector2Int(c,r);if(!grid.Contains(cell)||grid.TerrainAt(cell)!=TerrainKind.Water)continue;
                var p=HexWorldGrid.Center(c,r);p.y=WaterLevelAt(p);
                if((c*19+r*31)%3==0)
                {var pad=world.Shape("Floating lily pad",PrimitiveType.Cylinder,p+new Vector3(.28f,.035f,.18f),new Vector3(.38f,.01f,.32f),leaf);pad.SetParent(transform,true);}
                for(int i=0;i<2;i++)
                {var pebble=world.Shape("Submerged pebble",PrimitiveType.Sphere,p+new Vector3((float)random.NextDouble()-.5f,-.23f,(float)random.NextDouble()-.5f),new Vector3(.24f,.1f,.18f),stone);pebble.SetParent(transform,true);}
            }

        }
        void GroundWorld(FarmWorld world)
        {
            var roots=new List<Transform>();foreach(Transform child in world.transform)roots.Add(child);
            foreach(var root in roots)GroundRoot(root,world);
            for(int i=0;i<world.PlotPositions.Length;i++){var p=world.PlotPositions[i];p.y+=HeightAt(p);world.PlotPositions[i]=p;}
        }
        void GroundRoot(Transform root,FarmWorld world)
        {
            if(root==grid.transform||root==world.Player||root==world.View.transform||root.name=="Island sea")return;
            if(root.GetComponent<AdventureWorld>()||root.GetComponent<PainterlyPresentation>())
            {var list=new List<Transform>();foreach(Transform child in root)list.Add(child);foreach(var child in list)GroundRoot(child,world);return;}
            if(root.GetComponent<Light>())return;
            if(root.name=="Jetty plank"){var jetty=root.position;jetty.y=WaterLevelAt(jetty)+.24f;root.position=jetty;return;}
            var position=root.position;position.y+=HeightAt(position);root.position=position;
        }
        public void RefreshHeights(Vector3 center,float radius=2.6f)
        {
            Drainage=new DrainageNetwork(owner.State.land,c=>LandElevation.Height(HexWorldGrid.Center(c.x,c.y),owner.State.land),grid.TerrainAt);
            // Dry-land sculpting leaves shoreline topology intact; update existing geometry/collider.
            var mesh=groundCollider.sharedMesh;var points=mesh.vertices;
            for(int i=0;i<points.Length;i++)if(WorldRelief.PlanarDistance(points[i],center)<=radius)points[i].y=HeightAt(points[i]);
            mesh.vertices=points;mesh.RecalculateNormals();mesh.RecalculateBounds();
            groundCollider.sharedMesh=null;groundCollider.sharedMesh=mesh;Physics.SyncTransforms();
        }
        public void RefreshDrainedGround()
        {
            if(Drainage==null||Drainage.IntakeFloors.Count==0)return;
            var mesh=groundCollider.sharedMesh;var points=mesh.vertices;
            for(int i=0;i<points.Length;i++)if(Drainage.IntakeFloors.ContainsKey(StormWaterState.CellAt(points[i])))points[i].y=HeightAt(points[i]);
            mesh.SetVertices(points);mesh.RecalculateNormals();mesh.RecalculateBounds();groundCollider.sharedMesh=null;groundCollider.sharedMesh=mesh;
            Physics.SyncTransforms();
        }
        void OnDestroy(){foreach(var asset in owned)if(asset)Destroy(asset);}
    }
}
