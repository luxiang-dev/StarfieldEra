using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace StarfieldEra
{
    /// <summary>Continuous sculpted beds with per-plot state and individual sowing rows.</summary>
    public sealed class FarmFieldArt:MonoBehaviour
    {
        FarmWorld world;FarmGame game;Mesh soilMesh;Material soilMaterial;Transform container;
        readonly List<int> owners=new List<int>();readonly List<float> fades=new List<float>();
        readonly List<Plant> plants=new List<Plant>();readonly List<Object> owned=new List<Object>();
        int signature=-1;bool expanded;
        readonly List<Vector2> outlineStart=new List<Vector2>(),outlineEnd=new List<Vector2>();
        const float RowWidth=.64f;
        sealed class Plant {public int plot;public Vector3 position;public Transform root,hole,seed,left,right,fruit;}
        public static float Bend(float z)=>Mathf.Sin(z*.68f)*.07f+Mathf.Sin(z*1.7f)*.022f;
        public static float Ridge(Vector3 p)
        {
            float row=.5f+.5f*Mathf.Cos((p.x-Bend(p.z))*Mathf.PI*2/RowWidth);
            return .012f+Mathf.Pow(row,1.8f)*(.085f+.015f*Mathf.PerlinNoise(p.x*1.4f+31,p.z*1.4f+14));
        }
        public void Build(FarmWorld source)
        {
            world=source;game=world.GetComponentInParent<FarmGame>();expanded=game.State.adventure.expandedFarm;
            container=new GameObject("Refined farm presentation").transform;container.SetParent(transform,false);
            foreach(var r in world.GetComponentsInChildren<Renderer>())
                if(r.name.StartsWith("Hex plot ")||r.name=="Farm edge weed"||r.name=="Farm edge pebble"||r.transform.parent&&r.transform.parent.name.StartsWith("Crop "))r.enabled=false;
            soilMaterial=new Material(Shader.Find("StarfieldEra/Cultivated Soil"));soilMaterial.SetTexture("_Atlas",Resources.Load<Texture2D>("TerrainArt/ReferenceAtlas"));soilMaterial.SetColor("_BaseColor",Color.white);owned.Add(soilMaterial);
            CacheOutline();
            foreach(var r in world.GetComponentsInChildren<Renderer>())
                if((r.name=="Daisy petal"||r.name=="Daisy pollen"||r.name=="Flower stem")&&(Boundary(r.transform.position,out _) > -.2f||(r.transform.position.x>-2&&r.transform.position.x<9&&r.transform.position.z> -5.8f&&r.transform.position.z< -4.8f)))r.enabled=false;
            BuildSoil();BuildPlants();DressEdge();Refresh();
        }
        int PlotCount=>expanded?FarmState.PlotCount:12;
        void CacheOutline()
        {
            var cells=new HashSet<Vector2Int>();for(int i=0;i<PlotCount;i++)if(world.HexGrid.TryCell(world.PlotPositions[i],out var cell))cells.Add(cell);
            foreach(var cell in cells)for(int edge=0;edge<6;edge++)
            {
                if(cells.Contains(PastureFences.Neighbor(cell,edge)))continue;
                var center=HexWorldGrid.Center(cell.x,cell.y);var a=center+HexWorldGrid.Corner(edge,HexWorldGrid.Radius);var b=center+HexWorldGrid.Corner((edge+1)%6,HexWorldGrid.Radius);
                outlineStart.Add(new Vector2(a.x,a.z));outlineEnd.Add(new Vector2(b.x,b.z));
            }
        }
        float Boundary(Vector3 p,out int nearest)
        {
            float closest=float.MaxValue;nearest=0;bool inside=false;
            for(int i=0;i<PlotCount;i++)
            {
                var delta=p-world.PlotPositions[i];delta.y=0;float distance=delta.sqrMagnitude;if(distance<closest){nearest=i;closest=distance;}
                float edge=-100;for(int side=0;side<6;side++){float a=side*Mathf.PI/3;edge=Mathf.Max(edge,delta.x*Mathf.Cos(a)+delta.z*Mathf.Sin(a));}
                if(edge<=HexWorldGrid.Radius*Mathf.Sqrt(3)*.5f)inside=true;
            }
            var point=new Vector2(p.x,p.z);float rim=float.MaxValue;
            for(int i=0;i<outlineStart.Count;i++)
            {var line=outlineEnd[i]-outlineStart[i];float t=Mathf.Clamp01(Vector2.Dot(point-outlineStart[i],line)/line.sqrMagnitude);rim=Mathf.Min(rim,(point-outlineStart[i]-line*t).magnitude);}
            return (inside?rim:-rim)+(Mathf.PerlinNoise(p.x*3.2f+12,p.z*3.2f+7)-.5f)*.16f;
        }
        void BuildSoil()
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            var bounds=new Bounds(world.PlotPositions[0],Vector3.zero);for(int i=1;i<PlotCount;i++)bounds.Encapsulate(world.PlotPositions[i]);
            float x0=bounds.min.x-1.5f,z0=bounds.min.z-1.5f;const float step=.12f;
            int nx=Mathf.CeilToInt((bounds.size.x+3)/step),nz=Mathf.CeilToInt((bounds.size.z+3)/step);
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
            {
                var p=new Vector3(x0+x*step,0,z0+z*step);float edge=Boundary(p,out int owner);float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.10f,.15f,edge));
                p.y=world.HexGrid.GroundHeight(p)+.008f+Ridge(p)*fade;
                vertices.Add(p);uv.Add(new Vector2(p.x-Bend(p.z),p.z));owners.Add(owner);fades.Add(fade);
            }
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {int a=z*(nx+1)+x,b=a+1,c=a+nx+1,d=c+1;if(fades[a]+fades[b]+fades[c]+fades[d]<.001f)continue;indices.AddRange(new[]{a,c,b,b,c,d});}
            soilMesh=new Mesh{name="Soft sculpted cultivation beds",indexFormat=IndexFormat.UInt32};soilMesh.SetVertices(vertices);soilMesh.SetUVs(0,uv);soilMesh.SetTriangles(indices,0);soilMesh.RecalculateNormals();soilMesh.RecalculateBounds();owned.Add(soilMesh);
            var go=new GameObject("Natural farm beds",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(container,false);go.GetComponent<MeshFilter>().sharedMesh=soilMesh;go.GetComponent<MeshRenderer>().sharedMaterial=soilMaterial;
        }
        Material Mat(string name,string hex){var m=world.Mat(name,hex);m.shader=Shader.Find("StarfieldEra/Storybook Models");return m;}
        Transform Piece(string title,Transform parent,Vector3 local,Vector3 scale,Material material)
        {var p=world.Shape(title,PrimitiveType.Sphere,Vector3.zero,scale,material);p.SetParent(parent,false);p.localPosition=local;return p;}
        void BuildPlants()
        {
            var leaf=Mat("Tender crop leaves","86B85C");var hole=Mat("Sowing pockets","513820");var seed=Mat("Warm seed kernels","C89D61");var fruit=Mat("Raised crop fruit","D9AA58");
            for(int i=0;i<FarmState.PlotCount;i++)
            {
                var center=world.PlotPositions[i];int first=Mathf.CeilToInt((center.x-.86f)/RowWidth),last=Mathf.FloorToInt((center.x+.86f)/RowWidth);
                for(int row=first;row<=last;row++)for(int s=-1;s<=1;s++)
                {
                    var p=new Vector3(row*RowWidth,0,center.z+s*.48f);p.x+=Bend(p.z);float border=Boundary(p,out int owner);if(owner!=i||border<.22f||WorldRelief.PlanarDistance(p,center)>.93f)continue;
                    p.y=world.HexGrid.GroundHeight(p)+Ridge(p)+.009f;
                    var root=new GameObject("Sown row plant").transform;root.SetParent(container,false);root.position=p;
                    var plant=new Plant{plot=i,position=p,root=root};
                    plant.hole=Piece("Seed pocket",root,Vector3.zero,new Vector3(.15f,.009f,.12f),hole);
                    plant.seed=Piece("Sown kernel",root,Vector3.up*.006f,new Vector3(.047f,.018f,.03f),seed);
                    plant.left=Piece("Seedling leaf",root,new Vector3(-.04f,.10f,0),new Vector3(.15f,.045f,.07f),leaf);plant.left.localRotation=Quaternion.Euler(0,12,-32);
                    plant.right=Piece("Seedling leaf",root,new Vector3(.04f,.10f,0),new Vector3(.15f,.045f,.07f),leaf);plant.right.localRotation=Quaternion.Euler(0,-15,32);
                    plant.fruit=Piece("Growing harvest",root,new Vector3(0,.035f,0),Vector3.one*.16f,fruit);plants.Add(plant);
                }
            }
        }
        void DressEdge()
        {
            var random=new System.Random(479);var grass=Mat("Soft field fringe","7E9E47");var stone=Mat("Field pebble variation","A99B7C");
            var bounds=soilMesh.bounds;
            for(int i=0;i<420;i++)
            {
                var p=new Vector3(Mathf.Lerp(bounds.min.x,bounds.max.x,(float)random.NextDouble()),0,Mathf.Lerp(bounds.min.z,bounds.max.z,(float)random.NextDouble()));
                float edge=Boundary(p,out _);if(edge<-.25f||edge>.12f)continue;p=world.HexGrid.SurfacePoint(p,.02f);
                if(i%3==0){var pebble=world.Shape("Soft edge pebble",PrimitiveType.Sphere,p,new Vector3(.07f,.035f,.055f),stone);pebble.SetParent(container,true);}
                else for(int j=0;j<3;j++)
                {var leaf=world.Shape("Field fringe leaf",PrimitiveType.Sphere,p+new Vector3((j-1)*.04f,.03f,0),new Vector3(.06f,.1f,.035f),grass);leaf.SetParent(container,true);leaf.rotation=Quaternion.Euler(0,i*31,(j-1)*24);}
            }
        }
        int Signature(){int key=game.State.adventure.expandedFarm?1:0;for(int i=0;i<FarmState.PlotCount;i++){var c=game.State.plots[i];key=unchecked(key*31+(c.planted?1:0)+(c.watered?2:0)+c.growth*4+c.kind*16);}return key;}
        void Refresh()
        {
            signature=Signature();var colors=new List<Color>(owners.Count);
            for(int i=0;i<owners.Count;i++){var c=game.State.plots[owners[i]];colors.Add(new Color(c.watered?.85f:0,c.planted?1:0,(owners[i]*13%7)/7f,fades[i]));}soilMesh.SetColors(colors);
            foreach(var plant in plants)
            {
                var crop=game.State.plots[plant.plot];plant.root.gameObject.SetActive(crop.planted&&(plant.plot<12||expanded));if(!crop.planted)continue;
                float growth=Mathf.Clamp01((float)crop.growth/crop.RequiredDays);float size=(.48f+growth*.9f)*(.94f+.08f*(.5f+.5f*Mathf.Sin(plant.position.x*17+plant.position.z*31)));
                plant.left.localScale=new Vector3(.15f,.045f,.07f)*size;plant.right.localScale=new Vector3(.15f,.045f,.07f)*size;
                plant.left.localPosition=new Vector3(-.04f*size,.025f+.07f*size,0);plant.right.localPosition=new Vector3(.04f*size,.025f+.07f*size,0);
                plant.seed.gameObject.SetActive(crop.growth==0);plant.fruit.gameObject.SetActive(crop.growth>0);
                plant.fruit.localScale=crop.kind==1?new Vector3(.055f,.12f+growth*.22f,.055f):Vector3.one*(.07f+growth*.15f);
                var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",crop.kind==2?new Color(.61f,.25f,.35f):crop.kind==1?new Color(.82f,.68f,.35f):new Color(.89f,.64f,.32f));plant.fruit.GetComponent<Renderer>().SetPropertyBlock(block);
            }
        }
        void LateUpdate()
        {
            if(!game)return;
            if(expanded!=game.State.adventure.expandedFarm){expanded=game.State.adventure.expandedFarm;Destroy(gameObject.GetComponent<FarmFieldArt>());var replacement=world.gameObject.AddComponent<FarmFieldArt>();replacement.Build(world);return;}
            if(signature!=Signature())Refresh();
        }
        void OnDestroy(){if(container)Destroy(container.gameObject);foreach(var o in owned)if(o)Destroy(o);}
    }
}
