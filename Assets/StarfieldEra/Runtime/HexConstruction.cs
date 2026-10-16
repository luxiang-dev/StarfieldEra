using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace StarfieldEra
{
    [Serializable]
    public sealed class PlacedBuilding
    {
        public int kind, column, row, rotation;
        public int footprintVersion, builtDay;
        public ResourceSiteState operation;
    }

    public sealed class HexConstruction : MonoBehaviour
    {
        public static readonly string[] Names={"林场","采石场","学舍","营地","居民房屋","仓库","工坊","书院","磨坊","观星台","市集","农场小屋"};
        public static readonly int[] WoodCost={6,4,12,4,10,8,6,8,10,18,8,12}, StoneCost={4,8,6,2,6,8,4,6,8,14,4,8}, GoldCost={10,15,20,5,15,12,15,20,30,0,15,20};
        public const int DemolitionKind=12;
        public BuildingMenu Menu {get;private set;}
        public void CreateMenu(Transform parent,TMPro.TMP_FontAsset font){Menu=parent.gameObject.AddComponent<BuildingMenu>();Menu.Initialize(Game,font);}
        public FarmGame Game { get; private set; }
        public bool Active => selected>=0;
        public string Hint { get; private set; }="";
        readonly Dictionary<Vector2Int,PlacedBuilding> occupants=new Dictionary<Vector2Int,PlacedBuilding>();
        public BuildingInspectable Inspected {get;private set;}
        public bool CanDemolishInspected => Inspected && Inspected.record!=null;
        GameObject selectionOutline;
        Material selectionMaterial,platformMaterial;
        Mesh platformMesh;
        readonly List<Mesh> footingMeshes=new List<Mesh>();
        Material footingMaterial;
        readonly Dictionary<Vector2Int,GameObject> models=new Dictionary<Vector2Int,GameObject>();
        readonly List<Vector3> trees=new List<Vector3>();
        int selected=-1,rotation;
        GameObject preview;
        Material validMaterial,invalidMaterial,wallMaterial,roofMaterial;
        Vector2Int hovered;
        bool hasTile,canPlace;
        string reason;

        public void Initialize(FarmGame game)
        {
            Game=game;
            validMaterial=game.World.Mat("Build preview allowed","89CC7A");
            invalidMaterial=game.World.Mat("Build preview blocked","D16B54");
            wallMaterial=game.World.Mat("Self built walls","D8C8A6");roofMaterial=game.World.Mat("Self built roof","8C6245");
            foreach(var anchor in game.World.GetComponentsInChildren<Transform>())
                if(anchor.name.StartsWith("Store art - PT_Pine")||anchor.name=="Local pine tree")trees.Add(anchor.position);
            selectionMaterial=game.World.Mat("Building selected","E5C578");platformMaterial=game.World.Mat("Building foundation","B7AD8A");
            footingMaterial=game.World.Mat("Terrace retaining stone","827A61");
            platformMesh=HexWorldGrid.Prism(HexWorldGrid.Radius-.025f,.07f,"Building footprint tile");
            foreach(var building in game.State.adventure.buildings)Claim(building);
            bool expanded=false;
            foreach(var building in game.State.adventure.buildings)
            {
                if(building.footprintVersion!=0)continue;
                var cells=BuildingFootprints.Cells(new Vector2Int(building.column,building.row),building.kind,building.rotation);
                bool fits=true;foreach(var cell in cells)if(!SiteAllowed(cell,building,true,out var ignored)){fits=false;break;}
                if(!fits)continue;
                Release(building);building.footprintVersion=1;Claim(building);expanded=true;
            }
            foreach(var building in game.State.adventure.buildings)Spawn(building);
            RegisterFixedBuildings();
            if(expanded){MovePlayerOutsideBuildings();game.Persist();}
        }
        public void Open()
        {
            if(Game.Adventure.InBattle||Game.Night&&Game.Night.Active)return;
            Game.Night?.CancelTrap();Game.Land?.Cancel();Game.Fences?.Cancel();Cancel();Menu.Show();
        }
        public void Choose(int kind)
        {
            if(kind<0||kind>DemolitionKind||Game.Adventure.InBattle||Game.Night&&Game.Night.Active)return;
            if(!EquipmentCatalog.Require(Game,"hammer"))return;
            Game.Night?.CancelTrap();Game.Land?.Cancel();Game.Fences?.Cancel();Cancel();selected=kind;rotation=0;
            Menu.Close();Game.Adventure.Hud.Hide();
            if(kind<Names.Length)preview=CreateModel(new PlacedBuilding{kind=kind,footprintVersion=1},true);
            else
            {
                preview=new GameObject("Demolition preview",typeof(MeshFilter),typeof(MeshRenderer));
                preview.GetComponent<MeshFilter>().sharedMesh=Game.World.ConstructionOutline;
                preview.GetComponent<MeshRenderer>().sharedMaterial=invalidMaterial;
            }
            preview.SetActive(false);
            Game.ShowMessage(kind==DemolitionKind?"拆除模式：点击自己建造的建筑，返还一半木材、石料和金币。":"已选择"+Names[kind]+"。"+BuildingEconomy.Description(kind));
        }
        public void Rotate() {if(Active&&selected<Names.Length)rotation=(rotation+1)%6;}
        public void Cancel()
        {
            Menu?.Close();
            if(preview)Destroy(preview);
            preview=null;selected=-1;Hint="";hasTile=false;
            ClearInspection();
        }
        void Update()
        {

            if(Game.Night&&(Game.Night.Active||Game.Night.TrapPlacing))return;
            if(Menu&&Menu.IsOpen||Game.Management&&Game.Management.IsOpen)return;
            if(Game.Expedition&&Game.Expedition.Fishing)return;
            if(Game.Adventure.InBattle||!WorldPointer.Available){if(Active)Cancel();return;}
            Vector2 pixel=WorldPointer.Position;
            bool blocked=(WorldPointer.Blocked)||pixel.y<Screen.height*.12f||pixel.y>Screen.height*.91f;
            var ray=Game.World.View.ScreenPointToRay(pixel);
            if(Game.Land&&Game.Land.Active)return;
            if(Game.Fences&&Game.Fences.Active)return;
            if(!Active)
            {
                if(!blocked&&WorldPointer.Clicked)
                {
                    if(Physics.Raycast(ray,out var hit,150,~0,QueryTriggerInteraction.Collide))
                    {
                        var landmark=hit.collider.GetComponentInParent<ExpeditionTarget>();if(landmark){Game.Expedition.Inspect(landmark.encounter);return;}
                        var rabbit=hit.collider.GetComponentInParent<RabbitActor>();if(rabbit){Game.Homestead.Inspect(rabbit);return;}
                        var site=hit.collider.GetComponentInParent<ResourceSiteTarget>();if(site){Game.Management.OpenHomestead(false,site.index);return;}
                        var animal=hit.collider.GetComponentInParent<AnimalActor>();if(animal){Game.Animals.Inspect(animal);return;}
                        var fence=hit.collider.GetComponentInParent<FenceTarget>();if(fence){Game.Fences.Inspect(fence.record);return;}
                        var info=hit.collider.GetComponentInParent<BuildingInspectable>();
                        if(info){Inspect(info);return;}
                    }
                    if(Game.World.HexGrid.RaycastGround(ray,out var groundPoint)&&Game.World.HexGrid.TryCell(groundPoint,out var cell)&&occupants.TryGetValue(cell,out var record))
                        {Inspect(models[new Vector2Int(record.column,record.row)].GetComponent<BuildingInspectable>());return;}
                    ClearInspection();Game.Adventure.Hud.DismissBuildingDetails();
                }
                return;
            }
            if(WorldPointer.Cancel){Cancel();return;}
            Vector3 groundHit=default;hasTile=!blocked&&Game.World.HexGrid.RaycastGround(ray,out groundHit);
            if(!hasTile){preview.SetActive(false);Hint="建造 · 等待选址";return;}
            hasTile=Game.World.HexGrid.TryCell(groundHit,out hovered);
            if(!hasTile){preview.SetActive(false);return;}
            PlacedBuilding demolition=null;
            if(selected==DemolitionKind)occupants.TryGetValue(hovered,out demolition);
            canPlace=selected==DemolitionKind?demolition!=null:CanBuild(hovered,selected,out reason,rotation);
            if(selected==DemolitionKind)reason=canPlace?"拆除返还一半木材、石料和金币":"这里只能拆除自己建造的建筑";
            // Rebuild only after the anchor or rotation changes, so the footprint always matches the model.
            var stamp=new Vector3Int(hovered.x,hovered.y,rotation);
            if(!preview.activeSelf||stamp!=previewStamp)
            {
                Destroy(preview);
                preview=selected==DemolitionKind?CreateOutline(demolition!=null?BuildingFootprints.Cells(demolition):new List<Vector2Int>{hovered},invalidMaterial):CreateModel(new PlacedBuilding{kind=selected,column=hovered.x,row=hovered.y,rotation=rotation,footprintVersion=1},true);
                previewStamp=stamp;
            }
            preview.SetActive(true);
            foreach(var renderer in preview.GetComponentsInChildren<Renderer>())
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)materials[i]=canPlace?validMaterial:invalidMaterial;
                renderer.sharedMaterials=materials;
            }
            Hint=(selected==DemolitionKind?"拆除":Names[selected])+" ["+hovered.x+","+hovered.y+"] · "+(canPlace?(selected==DemolitionKind?"可拆除":"可建造"):reason);
            if(WorldPointer.Pressed)
            {
                if(!canPlace){Game.ShowMessage(reason);return;}
                if(selected==DemolitionKind)Demolish(hovered);else Build(hovered,selected,rotation);
                preview.SetActive(false);
            }
        }
        Vector3Int previewStamp;
        public bool CanBuild(Vector2Int cell,int kind,out string explanation,int facing=0)
        {
            explanation="";
            if(Game.Night&&Game.Night.Active){explanation="守卫期间无法施工";return false;}
            if(!EquipmentCatalog.Has(Game,"hammer")){explanation="需要持有建造锤";return false;}
            if(kind<0||kind>=Names.Length||facing<0||facing>=6){explanation="无效的建筑类型或朝向";return false;}
            var state=Game.State.adventure;
            if((kind==7||kind==8)&&!state.workshop){explanation="需要先建造工坊";return false;}
            if(kind==9&&(!state.library||!state.mountainWon)){explanation="需要书院并击败白岩守卫";return false;}
            if(kind==9&&(state.crystals<3||state.science<20)){explanation="还需晶石3、知识20";return false;}
            if(state.buildings.Count>=80){explanation="已达到80座建筑上限";return false;}
            foreach(var tile in BuildingFootprints.Cells(cell,kind,facing))
                if(!SiteAllowed(tile,null,false,out explanation))return false;
            if(state.wood<WoodCost[kind]||state.stone<StoneCost[kind]||Game.State.coins<GoldCost[kind]){explanation="木材、石料或金币不足";return false;}
            return true;
        }
        bool SiteAllowed(Vector2Int cell,PlacedBuilding ignore,bool migrating,out string explanation)
        {
            var changed=Game.State.land?.Find(cell);if(changed!=null&&(changed.tree||changed.tilled||changed.channel)){explanation="请保留改造后的农田、树木和河道";return false;}

            explanation="";
            if(!Game.World.HexGrid.Contains(cell)){explanation="建筑占地超出大陆边界";return false;}
            if(occupants.TryGetValue(cell,out var owner)&&owner!=ignore){explanation="占地与已有建筑重叠";return false;}
            if(!Game.World.HexGrid.CanBuildOn(cell)){explanation="建筑占地覆盖水面或沼泽，选择稳定陆地";return false;}
            var pos=Game.World.HexGrid.SurfacePoint(HexWorldGrid.Center(cell.x,cell.y));
            if(Game.Homestead)for(int i=0;i<2;i++)if(WorldRelief.PlanarDistance(pos,Game.Homestead.SitePosition(i))<3.4f){explanation="占地覆盖经营资源点";return false;}
            foreach(var plot in Game.World.PlotPositions)
                if(WorldRelief.PlanarDistance(pos,plot)<2.1f){explanation="占地覆盖农田或通道";return false;}
            foreach(var encounter in Game.World.Adventure.Encounters)
                if(WorldRelief.PlanarDistance(pos,encounter.Position)<2.6f){explanation="占地覆盖人物或资源预留地";return false;}
            foreach(var collider in Physics.OverlapSphere(pos+Vector3.up*.65f,1.15f))
                if(!Game.World.HexGrid.IsGroundCollider(collider)&&!collider.isTrigger&&!(migrating&&collider==Game.World.Controller)){explanation="占地有障碍或角色";return false;}
            foreach(var tree in Game.World.GetComponentsInChildren<HarvestableTree>())if(!tree.Felled&&WorldRelief.PlanarDistance(pos,tree.transform.position)<1.4f){explanation="占地有树木，选择空地";return false;}
            return true;
        }
        public bool Build(Vector2Int cell,int kind,int facing)
        {
            if(Game.Adventure.InBattle || !CanBuild(cell,kind,out var message,facing)){Game.ShowMessage("该位置暂时不能建造。");return false;}
            var state=Game.State.adventure;
            state.wood-=WoodCost[kind];state.stone-=StoneCost[kind];Game.State.coins-=GoldCost[kind];
            var building=new PlacedBuilding{kind=kind,column=cell.x,row=cell.y,rotation=Mathf.Clamp(facing,0,5),footprintVersion=1,builtDay=Game.State.day,operation=kind<2?new ResourceSiteState{level=1}:null};
            if(kind==9){state.crystals-=3;state.science-=20;}
            state.buildings.Add(building);state.ReconcileTown();Claim(building);Spawn(building);
            Game.Adventure.Commit("建成"+Names[kind]+"！"+BuildingEconomy.Description(kind));return true;
        }
        void Spawn(PlacedBuilding building)
        {
            var cell=new Vector2Int(building.column,building.row);
            var root=CreateModel(building,false);
            models[cell]=root;
            if(building.kind==10)Game.World.Adventure.Encounters.Add(new WorldEncounter{kind=EncounterKind.Market,id=0,name="自建市集",root=root.transform});
            if(building.kind==3)Game.World.Adventure.Encounters.Add(new WorldEncounter{kind=EncounterKind.Camp,id=0,name="自建营地",root=root.transform});
        }
        public bool Demolish(Vector2Int cell)
        {
            if(!EquipmentCatalog.Require(Game,"hammer"))return false;
            if(Game.Adventure.InBattle||Game.Night&&Game.Night.Active||!occupants.TryGetValue(cell,out var building))return false;
            cell=new Vector2Int(building.column,building.row);
            if(!models.TryGetValue(cell,out var root))return false;
            var list=Game.State.adventure.buildings;
            if(building.kind==4&&Game.State.adventure.homestead.UsedWorkers(Game.State.adventure)>Mathf.Min(9,3+2*(list.FindAll(b=>b.kind==4).Count-1))){Game.ShowMessage("请先召回村民，再拆除房屋。");return false;}
            if(building.kind<2)Game.State.adventure.homestead.Collect(Game.State.adventure,building.operation,building.kind);

            Game.State.adventure.wood+=WoodCost[building.kind]/2;Game.State.adventure.stone+=StoneCost[building.kind]/2;Game.State.coins+=GoldCost[building.kind]/2;
            list.Remove(building);Game.State.adventure.ReconcileTown();models.Remove(cell);Release(building);
            if(Inspected&&Inspected.record==building){Inspected=null;if(selectionOutline)Destroy(selectionOutline);Game.Adventure.Hud.Hide();}
            Game.World.Adventure.Encounters.RemoveAll(e=>e.root==root.transform);
            root.SetActive(false);Destroy(root);Game.Adventure.Commit("建筑已拆除，返还一半木材、石料和金币。");return true;
        }
        void Claim(PlacedBuilding building){foreach(var cell in BuildingFootprints.Cells(building))occupants[cell]=building;}
        void Release(PlacedBuilding building){foreach(var cell in BuildingFootprints.Cells(building))occupants.Remove(cell);}
        GameObject CreateModel(PlacedBuilding building,bool ghost)
        {
            int kind=building.kind;var cells=BuildingFootprints.Cells(building);
            var anchor=Game.World.HexGrid.SurfacePoint(HexWorldGrid.Center(building.column,building.row));
            var center=Vector3.zero;float foundation=0;
            foreach(var cell in cells){var p=Game.World.HexGrid.SurfacePoint(HexWorldGrid.Center(cell.x,cell.y));center+=p;foundation=Mathf.Max(foundation,p.y);}center/=cells.Count;
            center.y=foundation;anchor.y=foundation;
            var root=new GameObject((ghost?"Preview ":"Built ")+Names[kind]);root.transform.SetParent(transform,false);root.transform.position=anchor;
            root.transform.rotation=Quaternion.Euler(0,-building.rotation*60,0);
            var art=StoreVisuals.Current;
            var prefab=art?(kind==0?art.workshop:kind==1?(art.quarry?art.quarry:art.rock):kind==2?art.library:art.market):null;
            float width=building.footprintVersion==0?1.8f:kind<2?5.2f:kind==6||kind==8?4.6f:kind==2?3.9f:3.2f;
            float height=building.footprintVersion==0?2:kind==1?3.1f:kind==3?2.6f:3.7f;
            if(kind==6&&LocalEnvironment.Workshop(root.transform,center)){}
            else if(kind==3&&LocalEnvironment.Camp(root.transform,center,height,width)){}
            else if((kind==4||kind==11)&&LocalEnvironment.House(root.transform,center,height,width)){}
            else if(LocalEnvironment.ProductionBuilding(kind,root.transform,center,height,width)){}
            else if(prefab){var visual=art.Place(prefab,root.transform,center,height,0,width);if(kind>=4&&visual)visual.rotation=root.transform.rotation;}
            else
            {
                var piece=Game.World.Shape("Building body",PrimitiveType.Cube,center+Vector3.up*height*.35f,new Vector3(width*.7f,height*.7f,width*.6f),wallMaterial);piece.SetParent(root.transform,true);
                var roof=Game.World.Cone("Building roof",center+Vector3.up*height*.7f,new Vector3(width,height*.3f,width),roofMaterial);roof.SetParent(root.transform,true);
            }
            if(!ghost)
            {
                if(kind==5)for(int i=0;i<3;i++){var crate=Game.World.Shape("Warehouse crate",PrimitiveType.Cube,center+new Vector3(-.7f+i*.7f,.35f,-1.6f),new Vector3(.6f,.7f,.65f),roofMaterial);crate.SetParent(root.transform,true);}
                var info=root.AddComponent<BuildingInspectable>();info.record=building;info.displayName=Names[kind];
                var visuals=root.GetComponentsInChildren<Renderer>();
                if(visuals.Length>0)
                {
                    var bounds=visuals[0].bounds;foreach(var visual in visuals)bounds.Encapsulate(visual.bounds);
                    var target=root.AddComponent<BoxCollider>();target.isTrigger=true;target.center=root.transform.InverseTransformPoint(bounds.center);target.size=bounds.size;
                }
            }
            foreach(var cell in cells)
            {
                var pos=Game.World.HexGrid.SurfacePoint(HexWorldGrid.Center(cell.x,cell.y));
                var pad=new GameObject("Building parcel",typeof(MeshFilter),typeof(MeshRenderer));pad.transform.SetParent(root.transform,true);pad.transform.position=new Vector3(pos.x,foundation+.04f,pos.z);pad.transform.rotation=Quaternion.identity;
                pad.GetComponent<MeshFilter>().sharedMesh=platformMesh;pad.GetComponent<MeshRenderer>().sharedMaterials=new[]{platformMaterial,platformMaterial};
                float depth=foundation-pos.y;
                if(depth>.12f)
                {
                    var footing=new GameObject("Terrace foundation",typeof(MeshFilter),typeof(MeshRenderer));footing.transform.SetParent(root.transform,true);footing.transform.position=new Vector3(pos.x,foundation,pos.z);
                    var mesh=HexWorldGrid.Prism(HexWorldGrid.Radius-.055f,depth,"Retaining foundation");footingMeshes.Add(mesh);
                    footing.GetComponent<MeshFilter>().sharedMesh=mesh;footing.GetComponent<MeshRenderer>().sharedMaterials=new[]{footingMaterial,footingMaterial};
                }
                if(!ghost){var obstacle=root.AddComponent<BoxCollider>();obstacle.center=root.transform.InverseTransformPoint(new Vector3(pos.x,foundation+1.1f,pos.z));obstacle.size=new Vector3(1.65f,2.2f,1.65f);}
            }
            if(ghost)CreateOutline(cells,validMaterial).transform.SetParent(root.transform,true);
            if(!ghost){var presentation=Game.World.GetComponent<FantasyPresentation>();if(presentation){presentation.Stylize(root.transform);if(kind<4||kind>=5)presentation.IllustrateBuilding(root.transform,kind,center,height);}}
            return root;
        }
        GameObject CreateOutline(List<Vector2Int> cells,Material material)
        {
            var root=new GameObject("Building footprint outline");root.transform.SetParent(transform,false);
            foreach(var cell in cells)
            {
                var go=new GameObject("Occupied hex",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);go.transform.position=Game.World.HexGrid.SurfacePoint(HexWorldGrid.Center(cell.x,cell.y),.095f);
                go.GetComponent<MeshFilter>().sharedMesh=Game.World.ConstructionOutline;go.GetComponent<MeshRenderer>().sharedMaterial=material;
            }
            return root;
        }
        public void Inspect(BuildingInspectable info)
        {
            if(!info||Game.Adventure.InBattle)return;
            if(info.record!=null&&info.record.kind<2){Game.Management.OpenHomestead(false,Game.Homestead.SiteIndex(info.record));return;}
            Cancel();Inspected=info;if(selectionOutline)Destroy(selectionOutline);
            if(info.record!=null)selectionOutline=CreateOutline(BuildingFootprints.Cells(info.record),selectionMaterial);
            Game.Adventure.Hud.Hide();Game.Adventure.Hud.Show("building");
        }
        public void ClearInspection()
        {
            Inspected=null;if(selectionOutline)Destroy(selectionOutline);selectionOutline=null;
        }
        public void DemolishInspected()
        {
            if(!CanDemolishInspected)return;
            Demolish(new Vector2Int(Inspected.record.column,Inspected.record.row));
        }
        void RegisterFixedBuildings()
        {
            foreach(var node in Game.World.GetComponentsInChildren<Transform>(true))
            {
                string description=null;
                if(node.name=="Farmhouse")description="小屋：休息恢复体力、购买种子、制作料理。";
                else if(node.name=="Workshop")description="工坊：每天木材+6，解锁农场扩建、工具和书院。";
                else if(node.name=="Library")description="书院：每天知识+6，解锁灌溉与观星台。";
                else if(node.name=="Southern mill")description="磨坊：每天金币+18。";
                else if(node.name=="Observatory")description="观星台：每天知识+12。";
                else if(node.name=="Seed market"||node.name=="Village bakery"||node.name=="Outpost market")description="村庄市集建筑，商人和议事项目提供交易与建设。";
                if(description==null)continue;
                var info=node.gameObject.AddComponent<BuildingInspectable>();info.displayName=node.name=="Farmhouse"?"农场小屋":node.name=="Workshop"?"工坊":node.name=="Library"?"书院":node.name=="Southern mill"?"磨坊":node.name=="Observatory"?"观星台":"村庄市集";info.description=description;
                var renderers=node.GetComponentsInChildren<Renderer>();
                Bounds bounds=new Bounds(node.position,Vector3.zero);bool found=false;
                foreach(var renderer in renderers){if(!renderer.enabled)continue;if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);}
                if(found)
                {
                    if(node.name=="Farmhouse"){float ground=bounds.min.y;bounds.SetMinMax(new Vector3(bounds.min.x,ground,bounds.min.z),new Vector3(bounds.max.x,ground+3.9f,bounds.max.z));}
                    var hitbox=node.gameObject.AddComponent<BoxCollider>();hitbox.isTrigger=true;hitbox.center=node.InverseTransformPoint(bounds.center);
                    var scale=node.lossyScale;hitbox.size=new Vector3(bounds.size.x/Mathf.Abs(scale.x),bounds.size.y/Mathf.Abs(scale.y),bounds.size.z/Mathf.Abs(scale.z));
                }
            }
        }
        void MovePlayerOutsideBuildings()
        {
            if(!Game.World.HexGrid.TryCell(Game.World.Player.position,out var current))return;
            bool inside=occupants.ContainsKey(current);
            foreach(var obstacle in Physics.OverlapSphere(Game.World.Player.position+Vector3.up*.8f,.3f))
                if(!obstacle.isTrigger&&obstacle.GetComponent<BuildingInspectable>()?.record!=null)inside=true;
            if(!inside)return;
            for(int radius=1;radius<8;radius++)for(int r=current.y-radius;r<=current.y+radius;r++)for(int c=current.x-radius;c<=current.x+radius;c++)
            {
                var cell=new Vector2Int(c,r);if(!Game.World.HexGrid.Contains(cell)||!Game.World.HexGrid.IsDry(cell)||occupants.ContainsKey(cell))continue;
                var pos=Game.World.HexGrid.SurfacePoint(HexWorldGrid.Center(c,r));bool blocked=false;
                foreach(var obstacle in Physics.OverlapSphere(pos+Vector3.up*.8f,.4f))if(!Game.World.HexGrid.IsGroundCollider(obstacle)&&obstacle!=Game.World.Controller&&!obstacle.isTrigger){blocked=true;break;}
                if(blocked)continue;
                Game.World.Controller.enabled=false;Game.World.Player.position=pos+Vector3.up*.2f;Game.World.Controller.enabled=true;return;
            }
        }
        void OnDestroy(){if(platformMesh)Destroy(platformMesh);foreach(var mesh in footingMeshes)if(mesh)Destroy(mesh);}
    }
}
