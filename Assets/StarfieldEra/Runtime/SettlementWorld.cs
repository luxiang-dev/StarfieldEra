using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace StarfieldEra
{
    public sealed partial class SettlementGame
    {
        Transform villageRoot,caravanRoot,decorationRoot,weatherRoot;Transform[] barriers=new Transform[2],contents=new Transform[2],repairs=new Transform[2];
        readonly Dictionary<int,Transform> people=new Dictionary<int,Transform>();readonly List<Object> weatherAssets=new List<Object>();
        int renderedDecorationsCount=-1;Material drainageSoil;
        readonly List<WorldEncounter> decorationTargets=new List<WorldEncounter>();
        WorldEncounter Anchor(string name,Vector3 point,EncounterKind kind,int id,Transform parent)
        {
            var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=Game.World.HexGrid.SurfacePoint(point);
            var e=new WorldEncounter{name=name,root=t,kind=kind,id=id};Game.World.Adventure.Encounters.Add(e);return e;
        }
        void BuildWorld()
        {
            villageRoot=new GameObject("Village residents and public works").transform;villageRoot.SetParent(transform,false);
            var caravan=Anchor("晨风商队 · 种子与图纸",CaravanPosition,EncounterKind.Caravan,0,villageRoot);caravanRoot=caravan.root;
            LocalEnvironment.Camp(caravanRoot,caravanRoot.position,2.1f,3);
            var art=StoreVisuals.Current;if(art&&art.villager){var merchant=new GameObject("Caravan trader").transform;merchant.SetParent(caravanRoot,false);merchant.position=caravanRoot.position+Vector3.left*1.8f;art.Replace(art.villager,merchant,1.7f,true);}
            LocalEnvironment.Chest(caravanRoot,caravanRoot.position+new Vector3(1.4f,0,-1));WorldNameplate.Create(caravanRoot,"行脚商队",Game.interfaceFont,Game.World.View,2.5f);
            for(int i=0;i<2;i++)
            {
                var entry=Anchor(i==0?"断桥 · 雾松谷入口":"塌方矿道 · 赤晶矿谷入口",FrontierState.Entrance(i),EncounterKind.Route,i,villageRoot);
                LocalEnvironment.TownBoard(entry.root,entry.Position);WorldNameplate.Create(entry.root,FrontierState.Names[i]+" · 路线入口",Game.interfaceFont,Game.World.View,2.6f);
                var wall=new GameObject("Unrepaired route boundary "+i).transform;wall.SetParent(villageRoot,false);barriers[i]=wall;
                for(int n=0;n<16;n++)
                {
                    float angle=n*Mathf.PI*2/16;var p=FrontierState.Centers[i]+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*8.3f;p=Game.World.HexGrid.SurfacePoint(p);
                    var rock=Game.World.Shape("Route cliff rubble",PrimitiveType.Sphere,p+Vector3.up*1.1f,new Vector3(3.5f,3.2f,2.7f),Game.World.Mat("Frontier cliff stone","75827D"));rock.SetParent(wall,true);
                }
                var repaired=new GameObject("Repaired route approach "+i).transform;repaired.SetParent(villageRoot,false);repairs[i]=repaired;
                var approach=FrontierState.Entrance(i)+Vector3.forward*2;
                for(int n=0;n<7;n++){var p=Game.World.HexGrid.SurfacePoint(approach+Vector3.forward*(n*.48f),.1f);var plank=Game.World.Shape(i==0?"Rebuilt bridge deck":"Cleared mine path",PrimitiveType.Cube,p,new Vector3(2.5f,.13f,.42f),Game.World.Mat("Repaired route timber","A38258"));plank.SetParent(repaired,true);}
                var inside=new GameObject("Unlocked frontier content "+i).transform;inside.SetParent(villageRoot,false);contents[i]=inside;
                var resource=Anchor(i==0?"雾松古林 · 木材与纤维":"赤晶矿脉 · 晶石与石料",FrontierState.Centers[i]+Vector3.left*2,EncounterKind.FrontierResource,i*3,inside);
                if(i==0)LocalEnvironment.Tree(resource.root,resource.Position,3.8f);else LocalEnvironment.Pillar(resource.root,resource.Position,2.1f);
                // This tree belongs to a renewable daily resource encounter, not the permanent felling list.
                foreach(var tree in resource.root.GetComponentsInChildren<HarvestableTree>()){tree.enabled=false;Destroy(tree);}
                var animal=Anchor("银耳兔 · 稀有动物观察",FrontierState.Centers[i]+new Vector3(2,0,2),EncounterKind.FrontierResource,i*3+1,inside);LocalEnvironment.Rabbit(animal.root,animal.Position);
                var tint=new MaterialPropertyBlock();tint.SetColor("_BaseColor",new Color(.8f,.94f,1));foreach(var r in animal.root.GetComponentsInChildren<Renderer>())r.SetPropertyBlock(tint);
                var cache=Anchor("废弃营地 · 隐藏宝箱",FrontierState.Centers[i]+Vector3.forward*4,EncounterKind.FrontierResource,i*3+2,inside);LocalEnvironment.Camp(cache.root,cache.Position,1.7f,2.4f);LocalEnvironment.Chest(cache.root,cache.Position+Vector3.right*1.6f);
            }
            decorationRoot=new GameObject("Placed village decorations").transform;decorationRoot.SetParent(villageRoot,false);
        }
        void RefreshSettlementWorld()
        {
            caravanRoot.gameObject.SetActive(VillageTradeState.CaravanPresent(Game.State.day));for(int i=0;i<2;i++){barriers[i].gameObject.SetActive(!Frontier.unlocked[i]);contents[i].gameObject.SetActive(Frontier.unlocked[i]);repairs[i].gameObject.SetActive(Frontier.unlocked[i]);}
            foreach(var r in Village.residents)if(!people.ContainsKey(r.id))
            {
                var entry=Anchor(VillageState.Names[r.id]+" · "+VillageState.Specialties[r.specialty]+"专长",new Vector3(-2+r.id*.8f,0,10),EncounterKind.Resident,r.id,villageRoot);people[r.id]=entry.root;
                var art=StoreVisuals.Current;if(art&&art.villager)art.Replace(art.villager,entry.root,1.65f,true);
                else{var body=Game.World.Shape("Resident",PrimitiveType.Capsule,entry.Position+Vector3.up*.8f,new Vector3(.45f,.8f,.45f),Game.World.Mat("Resident coat","8B986F"));body.SetParent(entry.root,true);}
                WorldNameplate.Create(entry.root,VillageState.Names[r.id]+" · "+VillageState.Specialties[r.specialty],Game.interfaceFont,Game.World.View,2.1f);WorldInspection.Volume(entry.root);
            }
            Game.World.GetComponent<FantasyPresentation>()?.Stylize(villageRoot);
            if(renderedDecorationsCount==Frontier.decorations.Count)return;renderedDecorationsCount=Frontier.decorations.Count;
            foreach(var e in decorationTargets){Game.World.Adventure.Encounters.Remove(e);if(e.root){e.root.gameObject.SetActive(false);Destroy(e.root.gameObject);}}decorationTargets.Clear();
            for(int i=0;i<Frontier.decorations.Count;i++)
            {
                var d=Frontier.decorations[i];var e=Anchor("村庄旗帜",HexWorldGrid.Center(d.column,d.row),EncounterKind.Decoration,i,decorationRoot);decorationTargets.Add(e);e.root.rotation=Quaternion.Euler(0,d.rotation*60,0);
                var pole=Game.World.Shape("Banner pole",PrimitiveType.Cylinder,e.Position+Vector3.up*1.2f,new Vector3(.065f,1.2f,.065f),Game.World.Mat("Banner pole wood","785F3F"));pole.SetParent(e.root,true);
                var cloth=Game.World.Shape("Emerald village banner",PrimitiveType.Cube,e.Position+new Vector3(.42f,1.85f,0),new Vector3(.8f,.7f,.035f),Game.World.Mat("Village banner emerald","28584A"));cloth.SetParent(e.root,true);
                var emblem=Game.World.Shape("Banner gold star",PrimitiveType.Cube,e.Position+new Vector3(.42f,1.85f,.03f),new Vector3(.22f,.22f,.025f),Game.World.Mat("Village banner gold","E9C57B"));emblem.rotation=Quaternion.Euler(0,0,45);emblem.SetParent(e.root,true);
                foreach(var c in e.root.GetComponentsInChildren<Collider>())c.enabled=false;WorldInspection.Volume(e.root);
            }
        }
        readonly Dictionary<int,Vector3> destinations=new Dictionary<int,Vector3>(),destinationInputs=new Dictionary<int,Vector3>();
        List<VillageWorkplace> residentWorkplaces;float workplacesAt;
        static readonly float[] AvoidanceAngles={0f,60f,-60f};
        void UpdateResidents()
        {
            if(residentWorkplaces==null||Time.time>=workplacesAt){residentWorkplaces=VillageState.Workplaces(Game.State.adventure);workplacesAt=Time.time+1;}
            var places=residentWorkplaces;float phase=Time.time%90;
            foreach(var r in Village.residents)if(people.TryGetValue(r.id,out var root)&&root)
            {
                Vector3 target=new Vector3(-3+r.id*.65f,0,8.5f);
                VillageWorkplace place=null;foreach(var candidate in places)if(candidate.site.workplaceId==r.workplace){place=candidate;break;}if(phase<55&&place!=null)target=place.position+new Vector3(2.8f,0,-2.5f);
                else if(phase>75)
                {
                    target=new Vector3(-4+r.id*.6f,0,2.6f);
                    if(r.id>=3){int houseIndex=(r.id-3)/2;foreach(var b in Game.State.adventure.buildings)if(b.kind==4&&houseIndex--==0){target=HexWorldGrid.Center(b.column,b.row)+Vector3.back*3;break;}}
                }
                var grid=Game.World.HexGrid;
                if(!destinationInputs.TryGetValue(r.id,out var old)||(old-target).sqrMagnitude>.01f){destinationInputs[r.id]=target;destinations[r.id]=grid.SafeLand(target,.25f);}
                target=destinations[r.id];var delta=target-root.position;delta.y=0;
                if(delta.magnitude>.4f){var step=Vector3.ClampMagnitude(delta,1.15f*Mathf.Min(Time.deltaTime,.1f));
                    foreach(float angle in AvoidanceAngles){var next=root.position+grid.ConstrainMovement(root.position,Quaternion.Euler(0,angle,0)*step,.2f);next.y=grid.GroundHeight(next);if(!Physics.CheckCapsule(next+Vector3.up*.35f,next+Vector3.up*1.35f,.22f,~0,QueryTriggerInteraction.Ignore)){root.position=next;break;}}root.rotation=Quaternion.LookRotation(delta.normalized);}
            }
        }
        public void AdvanceWeather()
        {
            var grid=Game.World.HexGrid;var protectedCells=new HashSet<Vector2Int>();
            foreach(var b in Game.State.adventure.buildings)foreach(var c in BuildingFootprints.Cells(b))protectedCells.Add(c);
            var positions=Game.GetComponentsInChildren<BuildingInspectable>().Select(b=>b.transform.position).Concat(new[]{new Vector3(-5,0,5),CaravanPosition}).ToArray();
            foreach(var c in grid.Cells)if(positions.Any(p=>WorldRelief.PlanarDistance(p,HexWorldGrid.Center(c.x,c.y))<3))protectedCells.Add(c);
            Game.State.storm.Step(Game.State,grid.Cells,c=>LandElevation.Height(HexWorldGrid.Center(c.x,c.y),Game.State.land),grid.TerrainAt,c=>protectedCells.Contains(c));
        }
        public HashSet<Vector2Int> ConnectedDrains()=>StormWaterState.ConnectedDrains(Game.State.land,c=>LandElevation.Height(HexWorldGrid.Center(c.x,c.y),Game.State.land),Game.World.HexGrid.TerrainAt);
        public void OnDrainChanged(Vector2Int c){if(Game.State.land.Find(c)?.drainage==true)Game.State.storm.DrainNow(c,ConnectedDrains().Contains(c));}
        public void RefreshEnvironment()
        {
            destinations.Clear();destinationInputs.Clear();
            if(weatherRoot){weatherRoot.gameObject.SetActive(false);Destroy(weatherRoot.gameObject);}foreach(var a in weatherAssets)if(a)Destroy(a);weatherAssets.Clear();
            weatherRoot=new GameObject("Rain puddles and drainage channels").transform;weatherRoot.SetParent(transform,false);var grid=Game.World.HexGrid;
            var water=new Material(Shader.Find("StarfieldEra/Painted Water"));water.SetColor("_ShallowColor",new Color(.24f,.49f,.43f,.57f));water.SetFloat("_DepthMaxDistance",.45f);water.SetColor("_FoamColor",new Color(.8f,.9f,.83f,.15f));water.SetFloat("_FoamWidth",.08f);water.SetFloat("_FoamDistortion",.015f);water.SetFloat("_CausticsStrength",.05f);weatherAssets.Add(water);
            var vertices=new List<Vector3>();var colors=new List<Color>();var indices=new List<int>();
            foreach(var puddle in Game.State.storm.puddles)
            {
                var center=grid.SurfacePoint(HexWorldGrid.Center(puddle.column,puddle.row));float level=center.y+puddle.depth+.015f;int start=vertices.Count;vertices.Add(new Vector3(center.x,level,center.z));colors.Add(new Color(puddle.depth,1,0,1));
                for(int i=0;i<6;i++){var p=center+HexWorldGrid.Corner(i,HexWorldGrid.Radius*.87f);p.y=level;vertices.Add(p);colors.Add(new Color(0,0,0,1));}
                for(int i=0;i<6;i++){indices.Add(start);indices.Add(start+(i+1)%6+1);indices.Add(start+i+1);}
            }
            if(vertices.Count>0){var mesh=new Mesh{name="Saved storm puddle surfaces"};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();weatherAssets.Add(mesh);var go=new GameObject("Visible storm accumulation",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(weatherRoot,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=water;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
            if(!drainageSoil)drainageSoil=Game.World.Mat("Drainage cut dark soil","4D4832");var mat=drainageSoil;
            foreach(var cell in Game.State.land.cells)if(cell.drainage)
            {
                var center=HexWorldGrid.Center(cell.column,cell.row);var line=new GameObject("Drainage trench",typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(weatherRoot,false);line.sharedMaterial=mat;line.widthMultiplier=.18f;var neighbors=new List<Vector2Int>();for(int i=0;i<6;i++){var n=PastureFences.Neighbor(cell.Cell,i);if(Game.State.land.Find(n)?.drainage==true||grid.TerrainAt(n)==TerrainKind.Water)neighbors.Add(n);}
                var points=new List<Vector3>{grid.SurfacePoint(center+Vector3.back*.6f,.012f),grid.SurfacePoint(center,.012f)};if(neighbors.Count==0)points.Add(grid.SurfacePoint(center+Vector3.forward*.6f,.012f));else foreach(var n in neighbors){points.Add(grid.SurfacePoint((center+HexWorldGrid.Center(n.x,n.y))*.5f,.012f));points.Add(grid.SurfacePoint(center,.012f));}
                line.positionCount=points.Count;line.SetPositions(points.ToArray());line.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        void OnDestroy(){foreach(var a in weatherAssets)if(a)Destroy(a);}
    }
}
