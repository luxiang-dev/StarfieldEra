using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarfieldEra
{
    public sealed class ResourceSiteTarget:MonoBehaviour {public int index;}
    public sealed class HomesteadGame:MonoBehaviour
    {
        public FarmGame Game {get;private set;}
        public readonly List<RabbitActor> Rabbits=new List<RabbitActor>();
        readonly List<Transform> fixedSites=new List<Transform>();
        public RabbitActor Inspected {get;private set;}
        public HomesteadState State=>Game.State.adventure.homestead;
        public Vector3 Home {get;private set;}
        public void Initialize(FarmGame game)
        {
            Game=game;Home=game.World.HexGrid.SafeLand(new Vector3(-7,0,-4),.25f);
            for(int i=0;i<5;i++)
            {
                var record=State.rabbits.Find(r=>r.id==i);if(record?.status==3)continue;
                var root=new GameObject("Meadow rabbit "+(i+1));root.transform.SetParent(transform,false);
                var actor=root.AddComponent<RabbitActor>();actor.owner=this;actor.id=i;
                actor.origin=game.World.HexGrid.SafeLand(new Vector3(8+i*2.5f,0,-8-i*1.6f),.25f);
                root.transform.position=record?.status==2?Home+Vector3.right*(i%3)*.5f:actor.origin;
                var visual=LocalEnvironment.Rabbit(root.transform,root.transform.position);
                if(!visual){var body=game.World.Shape("Rabbit",PrimitiveType.Sphere,root.transform.position+Vector3.up*.3f,new Vector3(.4f,.6f,.55f),game.World.Mat("Rabbit fur","E7DCC7"));body.SetParent(root.transform,true);}
                var hit=root.AddComponent<BoxCollider>();hit.isTrigger=true;hit.center=Vector3.up*.4f;hit.size=new Vector3(.7f,.9f,.9f);Rabbits.Add(actor);
            }
            WorldNameplate.Create(new GameObject("Rabbit home").transform,"兔舍 · 回小屋安置兔子",game.interfaceFont,game.World.View,1.3f).transform.parent.SetParent(transform,false);
            var homeSign=transform.Find("Rabbit home");homeSign.position=Home;
            // Reuse the local painted fence, leaving a wide opening for the player.
            for(int i=0;i<3;i++){var a=Home+new Vector3(-1.5f+i,0,1.2f);LocalEnvironment.Fence(homeSign,a,a+Vector3.right);}
            CreateSite(0,new Vector3(19,0,-10));CreateSite(1,new Vector3(20,0,18));
        }
        void CreateSite(int kind,Vector3 position)
        {
            var root=new GameObject(kind==0?"Repairable lumberyard":"Repairable quarry");root.transform.SetParent(transform,false);
            root.transform.position=Game.World.HexGrid.SafeLand(position,.3f);
            LocalEnvironment.ProductionBuilding(kind,root.transform,root.transform.position,2.7f,3.8f);
            var target=root.AddComponent<ResourceSiteTarget>();target.index=kind;
            var solid=root.AddComponent<BoxCollider>();solid.center=Vector3.up;solid.size=new Vector3(2.4f,2,2.4f);
            var hit=root.AddComponent<BoxCollider>();hit.isTrigger=true;hit.center=Vector3.up;hit.size=new Vector3(4,2.8f,4);
            WorldNameplate.Create(root.transform,kind==0?"废弃林场 · 可修复":"废弃采石场 · 可修复",Game.interfaceFont,Game.World.View,3.1f);
            fixedSites.Add(root.transform);
        }
        public List<ResourceSiteState> Sites()=>State.Sites(Game.State.adventure).ToList();
        public int SiteIndex(PlacedBuilding b)=>2+Game.State.adventure.buildings.Where(x=>x.kind<2).ToList().IndexOf(b);
        public int SiteKind(int index)=>index<2?index:Game.State.adventure.buildings.Where(x=>x.kind<2).ElementAt(index-2).kind;
        public string SiteName(int index)=>index<2?(index==0?"青苔林场":"白岩采石场"):HexConstruction.Names[SiteKind(index)]+" ["+SitePosition(index).x.ToString("F0")+","+SitePosition(index).z.ToString("F0")+"]";
        public Vector3 SitePosition(int index)
        {
            if(index<2)return fixedSites[index].position;
            var b=Game.State.adventure.buildings.Where(x=>x.kind<2).ElementAt(index-2);return HexWorldGrid.Center(b.column,b.row);
        }
        public bool Near(Vector3 p,float distance=5)=>WorldRelief.PlanarDistance(Game.World.Player.position,p)<=distance;
        public void InspectForHandbook(RabbitActor rabbit)=>Inspected=rabbit;
        public void Inspect(RabbitActor rabbit){Inspected=rabbit;Game.Management.OpenHomestead(true);}
        public bool InteractNearby()
        {
            if(Game.Construction.Active||Game.Fences.Active)return false;
            var rabbit=Rabbits.Where(r=>r&&r.Record?.status!=3&&Near(r.transform.position,2.3f)).OrderBy(r=>WorldRelief.PlanarDistance(r.transform.position,Game.World.Player.position)).FirstOrDefault();
            if(rabbit){Inspect(rabbit);return true;}
            for(int i=0;i<fixedSites.Count;i++)if(Near(fixedSites[i].position,3)){Game.Management.OpenResourceBuilding(i);return true;}
            return false;
        }
        public void BuyCarrots()
        {
            if(!Game.NearHouse&&Game.Adventure.Target?.kind!=EncounterKind.Market){Game.ShowMessage("走近小屋或商人，6金币购买3份胡萝卜。");return;}
            if(!State.BuyCarrots(Game.State)){Game.ShowMessage("金币不足，需要6金币。");return;}Commit("购入胡萝卜3份。收获金萝卜也会额外保留2份用于喂养。");
        }
        public void Lure()
        {
            if(!Inspected||Inspected.Record!=null||!Near(Inspected.transform.position,6)){Game.ShowMessage("走近野兔六步以内再投喂胡萝卜。");return;}
            if(State.carrots<1){Game.ShowMessage("需要1份胡萝卜，可收获金萝卜或在小屋购买。");return;}
            State.carrots--;Inspected.lureSeconds=25;Inspected.bait=Game.World.HexGrid.SafeLand(Game.World.Player.position+Game.World.Player.forward*.9f,.2f,false);
            Commit("胡萝卜已放下！返回世界，让兔子靠近，再按E捕捉。诱饵持续25秒。");Game.Management.Close();
        }
        public void Capture()
        {
            if(!Inspected||Inspected.Record!=null||Inspected.lureSeconds<=0||!Near(Inspected.transform.position,1.8f)){Game.ShowMessage("先用胡萝卜引诱，等兔子走到两步以内再捕捉。");return;}
            if(State.rabbits.Any(r=>r.status==1)){Game.ShowMessage("一次只能携带一只兔子，先回小屋安置。");return;}
            State.rabbits.Add(new RabbitRecord{id=Inspected.id,status=1});Commit("成功捕捉兔子！带回小屋门前，在牧场页安置。");
        }
        public void House()
        {
            var carried=State.rabbits.Find(r=>r.status==1);
            if(carried==null||!Game.NearHouse){Game.ShowMessage("携带兔子走到小屋门前再安置。");return;}
            if(State.rabbits.Count(r=>r.status==2)>=3){Game.ShowMessage("兔舍最多饲养3只，可将寻回的兔子交还村民。");return;}
            carried.status=2;var actor=Rabbits.Find(r=>r.id==carried.id);if(actor)actor.transform.position=Home;Commit("兔子已入住兔舍。每天喂1份胡萝卜，次日照料收益4金币。");
        }
        public void Feed()
        {
            var record=Inspected?Inspected.Record:null;
            if(record==null||record.status!=2||!Near(Home,4)){Game.ShowMessage("走近兔舍并选择已安置的兔子。");return;}
            if(record.fedDay==Game.State.day){Game.ShowMessage("这只兔子今天已经吃饱。");return;}
            if(State.carrots<1){Game.ShowMessage("喂养需要1份胡萝卜。");return;}
            State.carrots--;record.fedDay=Game.State.day;Commit("兔子吃饱了，休息后增加照料天数，并获得4金币照料收益。");
        }
        public void Release()
        {
            var rabbit=Inspected;var record=rabbit?rabbit.Record:null;
            if(record==null||record.status==3||!Near(rabbit.transform.position,4)){Game.ShowMessage("请走近要放回的兔子。");return;}
            State.rabbits.Remove(record);rabbit.lureSeconds=0;rabbit.origin=Game.World.HexGrid.SafeLand(Game.World.Player.position+Game.World.Player.right*2,.25f);rabbit.transform.position=rabbit.origin;
            Commit("兔子已放回野外，兔舍位置已空出。");
        }
        public void Quest()
        {
            if(Game.Adventure.Target?.kind!=EncounterKind.Elder){Game.ShowMessage("请走近村长阿岚，接取或交付寻兔委托。");return;}
            if(State.rabbitQuest==0){State.rabbitQuest=1;Commit("寻兔委托：带一只捕获的兔子回来，或交还已照料一天的兔子。奖励30金币和2份胡萝卜。");return;}
            if(State.rabbitQuest!=1)return;
            var rabbit=State.rabbits.Find(r=>r.status==1||(r.status==2&&r.care>=1));
            if(rabbit==null){Game.ShowMessage("请携带一只捕获的兔子，或先照料兔舍里的兔子一天。");return;}
            rabbit.status=3;State.rabbitQuest=2;Game.State.coins+=30;State.carrots+=2;
            var actor=Rabbits.Find(r=>r.id==rabbit.id);if(actor)actor.gameObject.SetActive(false);
            Commit("已将兔子交还村民，获得30金币和2份胡萝卜。");
        }
        void Commit(string message)=>Game.Adventure.Commit(message);
        public void Improve(int index){var site=Sites()[index];if(!Near(SitePosition(index),6)){Game.ShowMessage("请走近资源点六步以内修复或升级。");return;}if(!State.Improve(Game.State,site)){Game.ShowMessage("材料不足，或建筑已经满级。");return;}Commit("资源点已升至"+site.level+"级。安排村民后，每次休息结算生产。");RefreshSiteSigns();}
        public void Assign(int index,int delta){if(!State.Assign(Game.State.adventure,Sites()[index],delta)){Game.ShowMessage("需先修复；每处最多2人，可建房屋增加村民容量。");return;}Commit(delta>0?"村民已派驻，下次休息开始生产。":"村民已召回。");}
        public void Collect(int index){if(!Near(SitePosition(index),6)){Game.ShowMessage("请走近资源点六步以内领取库存。");return;}State.Collect(Game.State.adventure,Sites()[index],SiteKind(index));Commit("已领取原料与加工品，库存已清空。");}
        public void ToggleProcessing(int index){var site=Sites()[index];if(site.level<2)return;site.processing=!site.processing;Commit(site.processing?"加工已开启：每名工人每天用4份原料加工2份成品。":"加工已关闭，保存原料。");}
        public void RefreshSiteSigns(){for(int i=0;i<2;i++){var label=fixedSites[i].GetComponentInChildren<TMPro.TMP_Text>();if(label)label.text=SiteName(i)+(Sites()[i].level==0?" · 待修复":" · "+Sites()[i].level+"级");}}
        void Start()=>RefreshSiteSigns();
    }
    public sealed class RabbitActor:MonoBehaviour
    {
        public HomesteadGame owner;public int id;public Vector3 origin,bait;public float lureSeconds;
        Vector3 destination;float wander;
        public RabbitRecord Record=>owner.State.rabbits.Find(r=>r.id==id);
        void Update()
        {
            var game=owner.Game;if(game.Adventure.InBattle||game.Management&&game.Management.IsOpen)return;
            var record=Record;if(record?.status==3)return;
            if(record?.status==1){transform.position=game.World.HexGrid.SurfacePoint(game.World.Player.position-game.World.Player.forward*.6f+game.World.Player.right*.5f,.65f);return;}
            lureSeconds=Mathf.Max(0,lureSeconds-Time.deltaTime);float speed=.7f;
            if(record==null&&lureSeconds>0){destination=bait;speed=1.1f;}
            else if(record==null&&owner.Near(transform.position,3.5f))
            {var away=transform.position-game.World.Player.position;away.y=0;if(away.sqrMagnitude<.01f)away=Vector3.right;destination=transform.position+away.normalized*3;speed=2.6f;}
            else if(Time.time>=wander){wander=Time.time+Random.Range(2f,5f);var offset=Random.insideUnitCircle*(record==null?2:1);destination=(record==null?origin:owner.Home)+new Vector3(offset.x,0,offset.y);}
            var delta=destination-transform.position;delta.y=0;if(delta.magnitude<.12f)return;
            var step=delta.normalized*Mathf.Min(delta.magnitude,speed*Mathf.Min(Time.deltaTime,.08f));
            var movement=game.World.HexGrid.ConstrainMovement(transform.position,step,.22f);
            var next=game.World.HexGrid.SurfacePoint(transform.position+movement);
            bool blocked=Physics.OverlapSphere(next+Vector3.up*.4f,.18f,~0,QueryTriggerInteraction.Ignore).Any(c=>!game.World.HexGrid.IsGroundCollider(c)&&!(c is CharacterController));
            if(blocked){wander=0;return;}
            if(game.World.HexGrid.CanStand(next,.22f)){transform.position=next;transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),Time.deltaTime*8);}
        }
    }
}
