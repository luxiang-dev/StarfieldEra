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
    }

    public sealed class HexConstruction : MonoBehaviour
    {
        public static readonly string[] Names={"林场","采石场","学舍","营地"};
        public static readonly int[] WoodCost={6,4,12,4}, StoneCost={4,8,6,2}, GoldCost={10,15,20,5};
        public FarmGame Game { get; private set; }
        public bool Active => selected>=0;
        public string Hint { get; private set; }="";
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
            foreach(var building in game.State.adventure.buildings)Spawn(building);
        }
        public void Open()
        {
            if(Game.Adventure.InBattle)return;
            Cancel();Game.Adventure.Hud.Show("build");
        }
        public void Choose(int kind)
        {
            if(kind<0||kind>4||Game.Adventure.InBattle)return;
            Cancel();selected=kind;rotation=0;
            Game.Adventure.Hud.Hide();
            if(kind<4)preview=CreateModel(kind,Vector3.zero,0,true);
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
        }
        void Update()
        {
            if(!Active)return;
            var mouse=Mouse.current;
            if(Game.Adventure.InBattle||mouse==null){Cancel();return;}
            if(mouse.rightButton.wasPressedThisFrame){Cancel();return;}
            Vector2 pixel=mouse.position.ReadValue();
            bool blocked= (EventSystem.current&&EventSystem.current.IsPointerOverGameObject()) || pixel.y<Screen.height*.2f || pixel.y>Screen.height*.87f;
            var ray=Game.World.View.ScreenPointToRay(pixel);
            float distance=0;
            hasTile=!blocked && new Plane(Vector3.up,Vector3.zero).Raycast(ray,out distance);
            if(!hasTile){preview.SetActive(false);Hint="建造 · 移动鼠标到地图地块，右键取消";return;}
            hasTile=Game.World.HexGrid.TryCell(ray.GetPoint(distance),out hovered);
            if(!hasTile){preview.SetActive(false);return;}
            var center=HexWorldGrid.Center(hovered.x,hovered.y);
            preview.SetActive(true);preview.transform.position=center+Vector3.up*.025f;preview.transform.rotation=Quaternion.Euler(0,rotation*60,0);
            canPlace=selected==4?models.ContainsKey(hovered):CanBuild(hovered,selected,out reason);
            if(selected==4)reason=canPlace?"拆除返还一半木材、石料和金币":"这里只能拆除自己建造的建筑";
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
            }
        }
        public bool CanBuild(Vector2Int cell,int kind,out string explanation)
        {
            explanation="";
            if(kind<0||kind>=4||!Game.World.HexGrid.Contains(cell)){explanation="地块不在大陆内";return false;}
            var state=Game.State.adventure;
            if(state.buildings.Count>=80){explanation="已达到80座建筑上限";return false;}
            if(models.ContainsKey(cell)){explanation="地块已有建筑";return false;}
            if(!Game.World.HexGrid.IsDry(cell)){explanation="水面不能建造";return false;}
            var pos=HexWorldGrid.Center(cell.x,cell.y);
            var delta=pos-Game.World.Player.position;delta.y=0;
            if(delta.magnitude>10){explanation="请走到地块十步以内建造";return false;}
            foreach(var plot in Game.World.PlotPositions)
                if(Vector3.Distance(pos,new Vector3(plot.x,0,plot.z))<2.1f){explanation="农田和周围通道需要保留";return false;}
            foreach(var encounter in Game.World.Adventure.Encounters)
                if(Vector3.Distance(pos,encounter.Position)<2.6f){explanation="资源、人物和营地附近需要保留";return false;}
            var reserved=new[]{new Vector3(-5,0,5.3f),new Vector3(-10,0,8.5f),new Vector3(-1,0,11.5f),new Vector3(-10,0,13),new Vector3(-5,0,13.6f),new Vector3(-1,0,-12.5f),new Vector3(-14,0,13.5f),new Vector3(-28,0,-22)};
            foreach(var point in reserved)if(Vector3.Distance(pos,point)<3.4f){explanation="村庄建筑预留地";return false;}
            foreach(var collider in Physics.OverlapSphere(pos+Vector3.up*.65f,.85f))
                if(collider!=Game.World.HexGrid.GetComponent<Collider>() && !collider.isTrigger){explanation="地块有障碍或角色，请先移开";return false;}
            foreach(var tree in trees)
                if(Vector3.Distance(pos,tree)<1.4f){explanation="这里有树木，选择空地";return false;}
            if(state.wood<WoodCost[kind]||state.stone<StoneCost[kind]||Game.State.coins<GoldCost[kind]){explanation="木材、石料或金币不足";return false;}
            return true;
        }
        public bool Build(Vector2Int cell,int kind,int facing)
        {
            if(Game.Adventure.InBattle || !CanBuild(cell,kind,out var message)){Game.ShowMessage("该位置暂时不能建造。");return false;}
            var state=Game.State.adventure;
            state.wood-=WoodCost[kind];state.stone-=StoneCost[kind];Game.State.coins-=GoldCost[kind];
            var building=new PlacedBuilding{kind=kind,column=cell.x,row=cell.y,rotation=Mathf.Clamp(facing,0,5)};
            state.buildings.Add(building);Spawn(building);
            Game.Adventure.Commit("建成"+Names[kind]+"！林场每天木+2，采石场石+2，学舍知+1，营地可料理。");return true;
        }
        void Spawn(PlacedBuilding building)
        {
            var cell=new Vector2Int(building.column,building.row);
            var root=CreateModel(building.kind,HexWorldGrid.Center(cell.x,cell.y),building.rotation,false);
            models[cell]=root;
            if(building.kind==3)Game.World.Adventure.Encounters.Add(new WorldEncounter{kind=EncounterKind.Camp,id=0,name="自建营地",root=root.transform});
        }
        public bool Demolish(Vector2Int cell)
        {
            if(Game.Adventure.InBattle||!models.TryGetValue(cell,out var root))return false;
            var list=Game.State.adventure.buildings;
            var building=list.Find(b=>b.column==cell.x&&b.row==cell.y);
            if(building==null)return false;
            Game.State.adventure.wood+=WoodCost[building.kind]/2;Game.State.adventure.stone+=StoneCost[building.kind]/2;Game.State.coins+=GoldCost[building.kind]/2;
            list.Remove(building);models.Remove(cell);
            Game.World.Adventure.Encounters.RemoveAll(e=>e.root==root.transform);
            root.SetActive(false);Destroy(root);Game.Adventure.Commit("建筑已拆除，返还一半木材、石料和金币。");return true;
        }
        GameObject CreateModel(int kind,Vector3 position,int facing,bool ghost)
        {
            var root=new GameObject((ghost?"Preview ":"Built ")+Names[kind]);root.transform.SetParent(transform,false);root.transform.position=position;
            var art=StoreVisuals.Current;
            var prefab=art?(kind==0?art.workshop:kind==1?art.rock:kind==2?art.library:art.market):null;
            if(prefab)art.Place(prefab,root.transform,position,kind==1?1:1.6f,0,1.6f);
            else
            {
                var piece=Game.World.Shape("Building body",PrimitiveType.Cube,position+Vector3.up*.55f,new Vector3(1.2f,1.1f,1.2f),wallMaterial);
                piece.SetParent(root.transform,true);
                var roof=Game.World.Cone("Building roof",position+Vector3.up*1.1f,new Vector3(1.6f,.7f,1.6f),roofMaterial);roof.SetParent(root.transform,true);
            }
            root.transform.rotation=Quaternion.Euler(0,facing*60,0);
            if(!ghost)
            {
                var obstacle=root.AddComponent<BoxCollider>();obstacle.center=Vector3.up*.65f;obstacle.size=new Vector3(1.2f,1.3f,1.2f);
            }
            return root;
        }
    }
}
