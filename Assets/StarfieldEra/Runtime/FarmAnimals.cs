using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarfieldEra
{
    [Serializable] public sealed class AnimalRecord
    {public int id,homeColumn,homeRow,fedDay=-1;public float x,z;}
    public sealed class FarmAnimals : MonoBehaviour
    {
        public FarmGame Game {get;private set;}public AnimalActor Inspected {get;private set;}
        readonly List<AnimalActor> actors=new List<AnimalActor>();
        public void Initialize(FarmGame game)
        {
            Game=game;foreach(var saved in game.State.adventure.animals)Spawn(saved,saved.id,new Vector3(saved.x,0,saved.z));
            var positions=new[]{new Vector3(14,0,7),new Vector3(18,0,-4),new Vector3(24,0,-22)};
            for(int i=0;i<3;i++)if(!game.State.adventure.animals.Exists(a=>a.id==i))Spawn(null,i,positions[i]);
            RefreshPens();
        }
        void Spawn(AnimalRecord record,int id,Vector3 position)
        {
            var root=new GameObject(record==null?"Wild boar":"Pasture boar");root.transform.SetParent(transform,false);position=Game.World.HexGrid.SafeLand(position,.28f);root.transform.position=position;
            var actor=root.AddComponent<AnimalActor>();actor.owner=this;actor.record=record;actor.id=id;actor.origin=position;
            if(record==null&&actor.Obstructed(position))
            {
                bool found=false;for(int radius=1;radius<5&&!found;radius++)for(int x=-radius;x<=radius&&!found;x++)for(int z=-radius;z<=radius;z++)
                {var p=position+new Vector3(x,0,z);if(!gameCell(p)||actor.Obstructed(p))continue;position=p;root.transform.position=p;actor.origin=p;found=true;break;}
            }
            var art=StoreVisuals.Current;
            if(art&&art.boar){var visual=art.Place(art.boar,root.transform,position,.9f,0,1.8f);actor.animator=visual.GetComponentInChildren<Animator>();}
            else
            {
                var mat=Game.World.Mat("Boar placeholder","806249");var body=Game.World.Shape("Boar body",PrimitiveType.Sphere,position+Vector3.up*.45f,new Vector3(.65f,.7f,1),mat);body.SetParent(root.transform,true);
                var head=Game.World.Shape("Boar head",PrimitiveType.Sphere,position+new Vector3(0,.4f,.55f),new Vector3(.45f,.4f,.45f),mat);head.SetParent(root.transform,true);
            }
            var hitbox=root.AddComponent<BoxCollider>();hitbox.isTrigger=true;hitbox.center=Vector3.up*.5f;hitbox.size=new Vector3(.8f,1,1.4f);actors.Add(actor);
        }
        bool gameCell(Vector3 p)=>Game.World.HexGrid.TryCell(p,out var c)&&Game.World.HexGrid.IsDry(c);
        public void Inspect(AnimalActor actor)
        {if(Game.Adventure.InBattle)return;Inspected=actor;Game.Construction.Cancel();Game.Fences.Cancel();Game.Adventure.Hud.Hide();Game.Adventure.Hud.Show("animal");}
        public bool InteractNearby()
        {if(Game.Construction.Active||Game.Fences.Active)return false;foreach(var a in actors)if(Vector3.Distance(a.transform.position,Game.World.Player.position)<2.2f){Inspect(a);return true;}return false;}
        public void Tame()
        {
            if(!Inspected||Inspected.record!=null)return;var s=Game.State.adventure;
            var pen=Game.Fences.FindPasture();if(pen==null){Game.ShowMessage("先在十步以内围出至少三格的封闭牧场，关好牧场门。");return;}
            if(s.berries<3){Game.ShowMessage("驯养需要三份浆果。");return;}
            // Use a free cell in the enclosed pasture, away from solid obstacles.
            Vector2Int home=default;bool found=false;
            foreach(var c in pen){var pos=HexWorldGrid.Center(c.x,c.y);if(actors.Exists(a=>a.record!=null&&Vector3.Distance(a.transform.position,pos)<1))continue;if(!Inspected.Obstructed(pos)){home=c;found=true;break;}}
            if(!found){Game.ShowMessage("牧场缺少空地，请扩大围栏。");return;}
            s.berries-=3;var p=HexWorldGrid.Center(home.x,home.y);
            var record=new AnimalRecord{id=Inspected.id,homeColumn=home.x,homeRow=home.y,x=p.x,z=p.z,fedDay=-1};s.animals.Add(record);Inspected.record=record;Inspected.origin=p;Inspected.transform.position=p;RefreshPens();Game.Adventure.Commit("野猪已加入牧场。每天喂一份浆果，次日获得松露。");
        }
        public void Feed()
        {
            if(!Inspected||Inspected.record==null)return;var a=Inspected.record;var s=Game.State.adventure;
            if(a.fedDay==Game.State.day){Game.ShowMessage("这只野猪今天已经吃饱了。");return;}
            if(Vector3.Distance(Inspected.transform.position,Game.World.Player.position)>3){Game.ShowMessage("走近动物三步以内喂食。");return;}
            if(s.berries<1){Game.ShowMessage("喂食需要一份浆果。");return;}s.berries--;a.fedDay=Game.State.day;Game.Adventure.Commit("野猪吃饱了，结束一天后产出一份松露。");
        }
        public void Sell()
        {var s=Game.State.adventure;if(s.truffles==0)return;int value=s.truffles*18;s.truffles=0;Game.State.coins+=value;Game.Adventure.Commit("出售牧场松露，获得金币 "+value+"。");}
        public void RefreshPens(){foreach(var a in actors)a.pen=a.record!=null?Game.Fences.Enclosure(new Vector2Int(a.record.homeColumn,a.record.homeRow)):null;}
        public void SavePositions(){foreach(var a in actors)if(a.record!=null){a.record.x=a.transform.position.x;a.record.z=a.transform.position.z;}}
    }
    public sealed class AnimalActor : MonoBehaviour
    {
        public FarmAnimals owner;public AnimalRecord record;public int id;public Vector3 origin;public List<Vector2Int> pen;public Animator animator;
        Vector3 destination;float nextMove;bool walking,previousWalking;int walkHash;bool hasSpeed,hasWalking;
        void Start()
        {
            destination=transform.position;if(animator&&animator.runtimeAnimatorController)animator.Play("Idle",0,0);nextMove=Time.time+UnityEngine.Random.Range(1,3);
            if(animator&&animator.runtimeAnimatorController)foreach(var p in animator.parameters){if(p.type==AnimatorControllerParameterType.Float&&(p.name.ToLower()=="speed"||p.name.ToLower()=="velocity")){walkHash=p.nameHash;hasSpeed=true;}if(p.type==AnimatorControllerParameterType.Bool&&p.name.ToLower().Contains("walk")){walkHash=p.nameHash;hasWalking=true;}}
        }
        public bool Obstructed(Vector3 p)
        {
            foreach(var col in Physics.OverlapSphere(p+Vector3.up*.5f,.28f,~0,QueryTriggerInteraction.Ignore))if(col!=owner.Game.World.HexGrid.GetComponent<Collider>())return true;return false;
        }
        void Update()
        {
            if(!owner||owner.Game.Adventure.InBattle)return;
            if(Time.time>=nextMove)
            {
                nextMove=Time.time+UnityEngine.Random.Range(3,7);destination=transform.position;
                for(int i=0;i<8;i++)
                {
                    Vector3 p;if(record!=null){if(pen==null||pen.Count==0)break;var c=pen[UnityEngine.Random.Range(0,pen.Count)];p=HexWorldGrid.Center(c.x,c.y)+new Vector3(UnityEngine.Random.Range(-.25f,.25f),0,UnityEngine.Random.Range(-.25f,.25f));}
                    else{var offset=UnityEngine.Random.insideUnitCircle*3;p=origin+new Vector3(offset.x,0,offset.y);}
                    if(!owner.Game.World.HexGrid.TryCell(p,out var cell)||!owner.Game.World.HexGrid.IsDry(cell)||Obstructed(p))continue;destination=p;break;
                }
            }
            var delta=destination-transform.position;delta.y=0;walking=delta.magnitude>.12f;
            if(walking)
            {
                var step=Vector3.MoveTowards(transform.position,destination,Mathf.Min(Time.deltaTime,.1f)*.7f*TerrainTraits.Speed[(int)owner.Game.World.HexGrid.TerrainAt(transform.position)]);
                if(owner.Game.World.HexGrid.TryCell(transform.position,out var from)&&owner.Game.World.HexGrid.TryCell(step,out var to)&&from!=to)
                {
                    bool crossing=false;for(int e=0;e<6;e++)if(PastureFences.Neighbor(from,e)==to&&owner.Game.Fences.Blocks(from,e))crossing=true;
                    if(record!=null&&(pen==null||!pen.Contains(to)))crossing=true;
                    if(crossing){destination=transform.position;walking=false;}
                }
                if(walking&&owner.Game.World.HexGrid.CanStand(step,.28f)&&!Obstructed(step)){transform.position=step;transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),Time.deltaTime*4);}else{destination=transform.position;walking=false;}
            }
            if(animator&&animator.runtimeAnimatorController){if(hasSpeed)animator.SetFloat(walkHash,walking?.7f:0);else if(hasWalking)animator.SetBool(walkHash,walking);else if(walking!=previousWalking){animator.CrossFadeInFixedTime(walking?"Walking":"Idle",.2f,0);previousWalking=walking;}}
        }
    }
}
