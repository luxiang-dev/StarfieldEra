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
    }

    public sealed class HexConstruction : MonoBehaviour
    {
        public static readonly string[] Names={"林场","采石场","学舍","营地"};
        public static readonly int[] WoodCost={6,4,12,4}, StoneCost={4,8,6,2}, GoldCost={10,15,20,5};
        public FarmGame Game { get; private set; }
        public bool Active => selected>=0;
        public string Hint { get; private set; }="";
        readonly Dictionary<Vector2Int,PlacedBuilding> occupants=new Dictionary<Vector2Int,PlacedBuilding>();
        public BuildingInspectable Inspected {get;private set;}
        public bool CanDemolishInspected => Inspected && Inspected.record!=null;
        GameObject selectionOutline;
        Material selectionMaterial,platformMaterial;
        Mesh platformMesh;
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
                if(anchor.name.StartsWith("Store art - PT_Pine"))trees.Add(anchor.position);
            selectionMaterial=game.World.Mat("Building selected","E5C578");platformMaterial=game.World.Mat("Building foundation","B7AD8A");
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
            if(Game.Adventure.InBattle)return;
            Game.Fences?.Cancel();Cancel();Game.Adventure.Hud.Show("build");
        }
        public void Choose(int kind)
        {
            if(kind<0||kind>4||Game.Adventure.InBattle)return;
            Game.Fences?.Cancel();Cancel();selected=kind;rotation=0;
            Game.Adventure.Hud.Hide();
            if(kind<4)preview=CreateModel(new PlacedBuilding{kind=kind,footprintVersion=1},true);
            else
            {
                preview=new GameObject("Demolition preview",typeof(MeshFilter),typeof(MeshRenderer));
                preview.GetComponent<MeshFilter>().sharedMesh=Game.World.ConstructionOutline;
                preview.GetComponent<MeshRenderer>().sharedMaterial=invalidMaterial;
            }
            preview.SetActive(false);
            Game.ShowMessage(kind==4?"拆除模式：点击自己建造的建筑，返还一半木材、石料和金币。右键取消。":"已选择"+Names[kind]+"。鼠标选六边形地块，左键建造，R旋转，右键取消。");
        }
        public void Rotate() {if(Active&&selected<4)rotation=(rotation+1)%6;}
        public void Cancel()
        {
            if(preview)Destroy(preview);
            preview=null;selected=-1;Hint="";hasTile=false;
            ClearInspection();
        }
        void Update()
        {
            var mouse=Mouse.current;
            if(Game.Management&&Game.Management.IsOpen)return;
            if(Game.Expedition&&Game.Expedition.Fishing)return;
            if(Game.Adventure.InBattle||mouse==null){if(Active)Cancel();return;}
            Vector2 pixel=mouse.position.ReadValue();
            bool blocked=(EventSystem.current&&EventSystem.current.IsPointerOverGameObject())||pixel.y<Screen.height*.12f||pixel.y>Screen.height*.91f;
            var ray=Game.World.View.ScreenPointToRay(pixel);
            if(Game.Fences&&Game.Fences.Active)return;
            if(!Active)
            {
                if(!blocked&&mouse.leftButton.wasPressedThisFrame)
                {
                    if(Physics.Raycast(ray,out var hit,150,~0,QueryTriggerInteraction.Collide))
                    {
                        var landmark=hit.collider.GetComponentInParent<ExpeditionTarget>();if(landmark){Game.Expedition.Inspect(landmark.encounter);return;}
                        var animal=hit.collider.GetComponentInParent<AnimalActor>();if(animal){Game.Animals.Inspect(animal);return;}
                        var fence=hit.collider.GetComponentInParent<FenceTarget>();if(fence){Game.Fences.Inspect(fence.record);return;}
                        var info=hit.collider.GetComponentInParent<BuildingInspectable>();
                        if(info){Inspect(info);return;}
                    }
                    if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float d)&&Game.World.HexGrid.TryCell(ray.GetPoint(d),out var cell)&&occupants.TryGetValue(cell,out var record))
                        {Inspect(models[new Vector2Int(record.column,record.row)].GetComponent<BuildingInspectable>());return;}
                    ClearInspection();Game.Adventure.Hud.DismissBuildingDetails();
                }
                return;
            }
            if(mouse.rightButton.wasPressedThisFrame){Cancel();return;}
            float distance=0;hasTile=!blocked&&new Plane(Vector3.up,Vector3.zero).Raycast(ray,out distance);
            if(!hasTile){preview.SetActive(false);Hint="建造 · 移动鼠标到地图地块，右键取消";return;}
            hasTile=Game.World.HexGrid.TryCell(ray.GetPoint(distance),out hovered);
            if(!hasTile){preview.SetActive(false);return;}
            PlacedBuilding demolition=null;
            if(selected==4)occupants.TryGetValue(hovered,out demolition);
            canPlace=selected==4?demolition!=null:CanBuild(hovered,selected,out reason,rotation);
            if(selected==4)reason=canPlace?"拆除返还一半木材、石料和金币":"这里只能拆除自己建造的建筑";
            // Rebuild only after the anchor or rotation changes, so the footprint always matches the model.
            var stamp=new Vector3Int(hovered.x,hovered.y,rotation);
            if(!preview.activeSelf||stamp!=previewStamp)
            {
                Destroy(preview);
                preview=selected==4?CreateOutline(demolition!=null?BuildingFootprints.Cells(demolition):new List<Vector2Int>{hovered},invalidMaterial):CreateModel(new PlacedBuilding{kind=selected,column=hovered.x,row=hovered.y,rotation=rotation,footprintVersion=1},true);
                previewStamp=stamp;
            }
            preview.SetActive(true);
            foreach(var renderer in preview.GetComponentsInChildren<Renderer>())
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)materials[i]=canPlace?validMaterial:invalidMaterial;
                renderer.sharedMaterials=materials;
            }
            Hint=(selected==4?"拆除":Names[selected])+" ["+hovered.x+","+hovered.y+"] · "+(canPlace?(selected==4?"左键拆除，右键取消":"左键确认，R旋转，右键取消"):reason);
            if(mouse.leftButton.wasPressedThisFrame)
            {
                if(!canPlace){Game.ShowMessage(reason);return;}
                if(selected==4)Demolish(hovered);else Build(hovered,selected,rotation);
                preview.SetActive(false);
            }
        }
        Vector3Int previewStamp;
        public bool CanBuild(Vector2Int cell,int kind,out string explanation,int facing=0)
        {
            explanation="";
            if(kind<0||kind>=4||facing<0||facing>=6){explanation="无效的建筑类型或朝向";return false;}
            var state=Game.State.adventure;
            if(state.buildings.Count>=80){explanation="已达到80座建筑上限";return false;}
            var delta=HexWorldGrid.Center(cell.x,cell.y)-Game.World.Player.position;delta.y=0;
            if(delta.magnitude>10){explanation="请走到地块十步以内建造";return false;}
            foreach(var tile in BuildingFootprints.Cells(cell,kind,facing))
                if(!SiteAllowed(tile,null,false,out explanation))return false;
            if(state.wood<WoodCost[kind]||state.stone<StoneCost[kind]||Game.State.coins<GoldCost[kind]){explanation="木材、石料或金币不足";return false;}
            return true;
        }
        bool SiteAllowed(Vector2Int cell,PlacedBuilding ignore,bool migrating,out string explanation)
        {
            explanation="";
            if(!Game.World.HexGrid.Contains(cell)){explanation="建筑占地超出大陆边界";return false;}
            if(occupants.TryGetValue(cell,out var owner)&&owner!=ignore){explanation="占地与已有建筑重叠";return false;}
            if(!Game.World.HexGrid.CanBuildOn(cell)){explanation="建筑占地覆盖水面或沼泽，选择稳定陆地";return false;}
            var pos=HexWorldGrid.Center(cell.x,cell.y);
            foreach(var plot in Game.World.PlotPositions)
                if(Vector3.Distance(pos,new Vector3(plot.x,0,plot.z))<2.1f){explanation="占地覆盖农田或通道";return false;}
            foreach(var encounter in Game.World.Adventure.Encounters)
                if(Vector3.Distance(pos,encounter.Position)<2.6f){explanation="占地覆盖人物或资源预留地";return false;}
            var reserved=new[]{new Vector3(-5,0,5.3f),new Vector3(-10,0,8.5f),new Vector3(-1,0,11.5f),new Vector3(-10,0,13),new Vector3(-5,0,13.6f),new Vector3(-1,0,-12.5f),new Vector3(-14,0,13.5f),new Vector3(-28,0,-22)};
            foreach(var point in reserved)if(Vector3.Distance(pos,point)<3.4f){explanation="占地覆盖村庄建筑预留地";return false;}
            foreach(var collider in Physics.OverlapSphere(pos+Vector3.up*.65f,1.15f))
                if(collider!=Game.World.HexGrid.GetComponent<Collider>()&&!collider.isTrigger&&!(migrating&&collider==Game.World.Controller)){explanation="占地有障碍或角色";return false;}
            foreach(var tree in trees)if(Vector3.Distance(pos,tree)<1.4f){explanation="占地有树木，选择空地";return false;}
            return true;
        }
        public bool Build(Vector2Int cell,int kind,int facing)
        {
            if(Game.Adventure.InBattle || !CanBuild(cell,kind,out var message,facing)){Game.ShowMessage("该位置暂时不能建造。");return false;}
            var state=Game.State.adventure;
            state.wood-=WoodCost[kind];state.stone-=StoneCost[kind];Game.State.coins-=GoldCost[kind];
            var building=new PlacedBuilding{kind=kind,column=cell.x,row=cell.y,rotation=Mathf.Clamp(facing,0,5),footprintVersion=1,builtDay=Game.State.day};
            state.buildings.Add(building);Claim(building);Spawn(building);
            Game.Adventure.Commit("建成"+Names[kind]+"！林场每天木+2，采石场石+2，学舍知+1，营地可料理。");return true;
        }
        void Spawn(PlacedBuilding building)
        {
            var cell=new Vector2Int(building.column,building.row);
            var root=CreateModel(building,false);
            models[cell]=root;
            if(building.kind==3)Game.World.Adventure.Encounters.Add(new WorldEncounter{kind=EncounterKind.Camp,id=0,name="自建营地",root=root.transform});
        }
        public bool Demolish(Vector2Int cell)
        {
            if(Game.Adventure.InBattle||!occupants.TryGetValue(cell,out var building))return false;
            cell=new Vector2Int(building.column,building.row);
            if(!models.TryGetValue(cell,out var root))return false;
            var list=Game.State.adventure.buildings;

            Game.State.adventure.wood+=WoodCost[building.kind]/2;Game.State.adventure.stone+=StoneCost[building.kind]/2;Game.State.coins+=GoldCost[building.kind]/2;
            list.Remove(building);models.Remove(cell);Release(building);
            if(Inspected&&Inspected.record==building){Inspected=null;if(selectionOutline)Destroy(selectionOutline);Game.Adventure.Hud.Hide();}
            Game.World.Adventure.Encounters.RemoveAll(e=>e.root==root.transform);
            root.SetActive(false);Destroy(root);Game.Adventure.Commit("建筑已拆除，返还一半木材、石料和金币。");return true;
        }
        void Claim(PlacedBuilding building){foreach(var cell in BuildingFootprints.Cells(building))occupants[cell]=building;}
        void Release(PlacedBuilding building){foreach(var cell in BuildingFootprints.Cells(building))occupants.Remove(cell);}
        GameObject CreateModel(PlacedBuilding building,bool ghost)
        {
            int kind=building.kind;var cells=BuildingFootprints.Cells(building);
            var anchor=HexWorldGrid.Center(building.column,building.row);
            var center=Vector3.zero;foreach(var cell in cells)center+=HexWorldGrid.Center(cell.x,cell.y);center/=cells.Count;
            var root=new GameObject((ghost?"Preview ":"Built ")+Names[kind]);root.transform.SetParent(transform,false);root.transform.position=anchor;
            root.transform.rotation=Quaternion.Euler(0,-building.rotation*60,0);
            var art=StoreVisuals.Current;
            var prefab=art?(kind==0?art.workshop:kind==1?(art.quarry?art.quarry:art.rock):kind==2?art.library:art.market):null;
            float width=building.footprintVersion==0?1.8f:kind<2?5.2f:kind==2?3.9f:3.2f;
            float height=building.footprintVersion==0?2:kind==1?3.1f:kind==3?2.6f:3.7f;
            if(prefab)art.Place(prefab,root.transform,center,height,0,width);
            else
            {
                var piece=Game.World.Shape("Building body",PrimitiveType.Cube,center+Vector3.up*height*.35f,new Vector3(width*.7f,height*.7f,width*.6f),wallMaterial);piece.SetParent(root.transform,true);
                var roof=Game.World.Cone("Building roof",center+Vector3.up*height*.7f,new Vector3(width,height*.3f,width),roofMaterial);roof.SetParent(root.transform,true);
            }
            if(!ghost)
            {
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
                var pos=HexWorldGrid.Center(cell.x,cell.y);
                var pad=new GameObject("Building parcel",typeof(MeshFilter),typeof(MeshRenderer));pad.transform.SetParent(root.transform,true);pad.transform.position=pos+Vector3.up*.04f;pad.transform.rotation=Quaternion.identity;
                pad.GetComponent<MeshFilter>().sharedMesh=platformMesh;pad.GetComponent<MeshRenderer>().sharedMaterials=new[]{platformMaterial,platformMaterial};
                if(!ghost){var obstacle=root.AddComponent<BoxCollider>();obstacle.center=root.transform.InverseTransformPoint(pos+Vector3.up*1.1f);obstacle.size=new Vector3(1.65f,2.2f,1.65f);}
            }
            if(ghost)CreateOutline(cells,validMaterial).transform.SetParent(root.transform,true);
            return root;
        }
        GameObject CreateOutline(List<Vector2Int> cells,Material material)
        {
            var root=new GameObject("Building footprint outline");root.transform.SetParent(transform,false);
            foreach(var cell in cells)
            {
                var go=new GameObject("Occupied hex",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);go.transform.position=HexWorldGrid.Center(cell.x,cell.y)+Vector3.up*.095f;
                go.GetComponent<MeshFilter>().sharedMesh=Game.World.ConstructionOutline;go.GetComponent<MeshRenderer>().sharedMaterial=material;
            }
            return root;
        }
        public void Inspect(BuildingInspectable info)
        {
            if(!info||Game.Adventure.InBattle)return;
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
                else if(node.name=="Workshop")description="工坊：每天木材+2，解锁农场扩建、工具和书院。";
                else if(node.name=="Library")description="书院：每天知识+2，解锁灌溉与观星台。";
                else if(node.name=="Southern mill")description="磨坊：每天金币+6。";
                else if(node.name=="Observatory")description="观星台：每天知识+4。";
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
                var pos=HexWorldGrid.Center(c,r);bool blocked=false;
                foreach(var obstacle in Physics.OverlapSphere(pos+Vector3.up*.8f,.4f))if(obstacle!=Game.World.HexGrid.GetComponent<Collider>()&&obstacle!=Game.World.Controller&&!obstacle.isTrigger){blocked=true;break;}
                if(blocked)continue;
                Game.World.Controller.enabled=false;Game.World.Player.position=pos+Vector3.up*.2f;Game.World.Controller.enabled=true;return;
            }
        }
        void OnDestroy(){if(platformMesh)Destroy(platformMesh);}
    }
}
