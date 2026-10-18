using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace StarfieldEra
{
    public sealed class CaveGame:MonoBehaviour
    {
        public const float Floor=-40;
        Light[] lanternLights=System.Array.Empty<Light>();
        FarmGame game;Transform interior;Vector3 entrance;float meleeAt;
        readonly List<CaveEnemy> enemies=new List<CaveEnemy>();readonly List<WorldEncounter> targets=new List<WorldEncounter>();
        public CaveState State=>game.State.cave;public bool Inside=>State.inside;
        public static bool CaveKind(EncounterKind kind)=>kind>=EncounterKind.CaveEntrance&&kind<=EncounterKind.CaveExit;
        public void Initialize(FarmGame owner)
        {
            game=owner;entrance=game.World.HexGrid.SafeLand(game.World.Door+new Vector3(-18,0,13),1,true);
            var entry=Target("旧矿洞入口",entrance,EncounterKind.CaveEntrance,0,transform);Portal(entry.root,entrance);
            if(Inside)Build();
            if(Inside)Teleport(new Vector3(Mathf.Clamp(game.State.playerX,-12,12),Floor+.1f,Mathf.Clamp(game.State.playerZ,-10,24)));
        }
        WorldEncounter Target(string name,Vector3 p,EncounterKind kind,int id,Transform parent)
        {
            var root=new GameObject(name).transform;root.SetParent(parent,false);root.position=p;var e=new WorldEncounter{name=name,root=root,kind=kind,id=id};targets.Add(e);game.World.Adventure.Encounters.Add(e);
            var pick=root.gameObject.AddComponent<ExpeditionTarget>();pick.encounter=e;return e;
        }
        void Shape(Transform parent,string name,PrimitiveType type,Vector3 p,Vector3 size,Material material)
        {var obj=game.World.Shape(name,type,p,size,material,true);obj.SetParent(parent,true);}
        void Portal(Transform parent,Vector3 p)
        {
            var rock=game.World.Mat("Mine entrance rock","59636A");for(int i=-1;i<=1;i+=2)Shape(parent,"Mine stone arch",PrimitiveType.Cube,p+new Vector3(i*1.35f,1.4f,0),new Vector3(.8f,2.8f,1.4f),rock);
            Shape(parent,"Mine arch lintel",PrimitiveType.Cube,p+Vector3.up*2.9f,new Vector3(3.5f,.65f,1.5f),rock);
            Shape(parent,"Dark mine opening",PrimitiveType.Cube,p+new Vector3(0,1.25f,.45f),new Vector3(2,2.5f,.08f),game.World.Mat("Mine darkness","101B23"));
            WorldNameplate.Create(parent,"旧矿洞 · 探索",game.interfaceFont,game.World.View,3.6f);
        }
        void Build()
        {
            interior=new GameObject("Underground mine").transform;interior.SetParent(transform,false);
            var floor=game.World.Mat("Mine floor","A0AFB8");var wall=game.World.Mat("Mine wall","6B7D88");
            var stone=Resources.Load<GameObject>("Environment/FieldRock");var stoneRenderer=stone?stone.GetComponentInChildren<Renderer>():null;
            if(stoneRenderer&&stoneRenderer.sharedMaterial&&stoneRenderer.sharedMaterial.HasProperty("_BaseMap"))foreach(var material in new[]{floor,wall})
            {material.shader=Shader.Find("StarfieldEra/Storybook Models");material.SetTexture("_BaseMap",stoneRenderer.sharedMaterial.GetTexture("_BaseMap"));material.SetTextureScale("_BaseMap",material==floor?new Vector2(7,10):new Vector2(2,2));}
            Shape(interior,"Underground floor",PrimitiveType.Cube,new Vector3(0,Floor-.3f,7),new Vector3(28,.6f,40),floor);
            foreach(int side in new[]{-1,1})for(int i=0;i<10;i++)Shape(interior,"Rough mine wall",PrimitiveType.Cube,new Vector3(side*14,Floor+2.8f,-11+i*4),new Vector3(1.5f,5.6f,4.2f),wall);
            Shape(interior,"Mine end wall",PrimitiveType.Cube,new Vector3(0,Floor+3,27),new Vector3(29,6,1),wall);
            Shape(interior,"Mine entry wall",PrimitiveType.Cube,new Vector3(0,Floor+3,-13),new Vector3(29,6,1),wall);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<5;i++)
            {
                var rock=LocalEnvironment.Rock(interior,new Vector3(side*15.5f,Floor,-9+i*8),2.2f,i*73);
                if(rock){foreach(var c in rock.GetComponentsInChildren<Collider>())c.enabled=false;foreach(var harvest in rock.GetComponentsInChildren<HarvestableRock>())Destroy(harvest);}
            }
            var exit=Target("返回营地",new Vector3(0,Floor,-10),EncounterKind.CaveExit,0,interior);LocalEnvironment.Camp(exit.root,exit.Position,1.2f,1.5f);WorldNameplate.Create(exit.root,"出口 · 返回营地",game.interfaceFont,game.World.View,2.5f);
            for(int i=0;i<6;i++)
            {
                var p=new Vector3(i%2==0?-10:10,Floor,-3+(i/2)*10);var e=Target(i<4?"赤铜矿脉":"星铁矿脉",p,EncounterKind.CaveOre,i,interior);
                LocalEnvironment.Pillar(e.root,p,1.5f);foreach(var rock in e.root.GetComponentsInChildren<HarvestableRock>())Destroy(rock);
                var material=game.World.Mat(i<4?"Copper ore glow":"Star iron glow",i<4?"D39354":"67CAE3");
                for(int n=0;n<4;n++){var crystal=game.World.Shape("Ore crystal",PrimitiveType.Cube,p+new Vector3((n%2-.5f)*.7f,.8f+n*.22f,0),new Vector3(.25f,.8f,.25f),material);crystal.rotation=Quaternion.Euler(15,n*45,25);crystal.SetParent(e.root,true);}
                WorldInspection.Volume(e.root);e.root.gameObject.SetActive(!State.mined[i]);
            }
            for(int i=0;i<2;i++){var e=Target(i==0?"矿工遗留宝箱":"矿主密藏",new Vector3(i==0?-7:7,Floor,i==0?8:22),EncounterKind.CaveChest,i,interior);LocalEnvironment.Chest(e.root,e.Position);WorldInspection.Volume(e.root);e.root.gameObject.SetActive(!State.opened[i]);}
            for(int i=0;i<5;i++)SpawnEnemy(i);
            for(int i=0;i<6;i++){var go=new GameObject("Mine lantern");go.transform.SetParent(interior,false);go.transform.position=new Vector3(i%2==0?-8:8,Floor+3,-7+i*6);var art=Resources.Load<GardenArtLibrary>("GardenDefenseArt");if(art&&art.lantern)Instantiate(art.lantern,go.transform.position-Vector3.up*3,Quaternion.identity,interior);var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=13;light.intensity=2.5f;light.color=i<4?new Color(1,.72f,.4f):new Color(.3f,.75f,1);light.shadows=LightShadows.None;}
            lanternLights=interior.GetComponentsInChildren<Light>();
        }
        void SpawnEnemy(int i)
        {
            if(State.enemyHealth[i]<=0)return;var root=new GameObject(i==4?"矿主 · 首领":"矿洞精英守卫");root.transform.SetParent(interior,false);root.transform.position=new Vector3(i==4?0:i%2==0?-4:4,Floor,i==4?21:1+(i/2)*10);
            var art=Resources.Load<GardenArtLibrary>("GardenDefenseArt");int variant=i==4?3:i<2?1:2;Instantiate(art.Zombie(variant),root.transform,false);if(i==4)root.transform.localScale=Vector3.one*1.2f;
            var enemy=root.AddComponent<CaveEnemy>();enemy.index=i;enemy.cave=this;var actor=root.AddComponent<GardenActor>();actor.owner=game.Garden;actor.caveEnemy=enemy;actor.zombie=new GardenZombie{id=100000+i,variant=variant,health=State.enemyHealth[i]};enemy.actor=actor;enemies.Add(enemy);actor.SetAnimation(root.GetComponentInChildren<Animator>(),art.Walk(variant),art.Dismay(variant));
            var collider=root.AddComponent<CapsuleCollider>();collider.center=Vector3.up*.9f;collider.height=1.8f;collider.radius=i==4?.55f:.35f;collider.isTrigger=true;
            root.AddComponent<HealthBars>().Initialize(game,actor,game.Garden.GetComponentInChildren<Canvas>().transform);
        }
        void Teleport(Vector3 p){var c=game.World.Controller;c.enabled=false;game.World.Player.position=p;Physics.SyncTransforms();c.enabled=true;game.Inspection.Clear();}
        public void Enter()
        {
            if(Inside||game.Night.Active||game.Adventure.InBattle)return;bool reset=State.clearedDay>=0&&game.State.day>=State.clearedDay+3;State.Refresh(game.State.day);if(reset){foreach(var enemy in enemies)if(enemy)Destroy(enemy.gameObject);enemies.Clear();}
            var p=game.World.Player.position;State.returnX=p.x;State.returnZ=p.z;State.inside=true;if(!interior)Build();interior.gameObject.SetActive(true);
            foreach(var e in targets)if(e.kind==EncounterKind.CaveOre)e.root.gameObject.SetActive(!State.mined[e.id]);else if(e.kind==EncounterKind.CaveChest)e.root.gameObject.SetActive(!State.opened[e.id]);
            for(int i=0;i<5;i++)if(State.enemyHealth[i]>0&&!enemies.Any(e=>e&&e.index==i&&e.actor.Health>0))SpawnEnemy(i);
            Teleport(new Vector3(0,Floor+.1f,-8));game.ShowMessage("进入旧矿洞。点击矿脉、宝箱或守卫，再按E交互。入口可返回营地。");game.Persist();
        }
        public void Exit(bool persist=true)
        {if(!Inside)return;State.inside=false;interior.gameObject.SetActive(false);Teleport(game.World.HexGrid.SafeLand(new Vector3(State.returnX,0,State.returnZ),game.World.Controller.radius));if(persist)game.Persist();}
        public bool Illuminated(Vector3 p){if(!interior)return false;foreach(var light in lanternLights)if(light.enabled&&WorldRelief.PlanarDistance(p,light.transform.position)<7)return true;return false;}
        public Vector3 Constrain(Vector3 start,Vector3 step){var p=start+step;p.x=Mathf.Clamp(p.x,-12.7f,12.7f);p.z=Mathf.Clamp(p.z,-11.7f,25.7f);p.y=start.y;return p-start;}
        public void InteractNearest()
        {
            var enemy=enemies.Where(e=>e&&e.actor.Health>0).OrderBy(e=>Vector3.Distance(e.transform.position,game.World.Player.position)).FirstOrDefault();
            if(enemy&&Vector3.Distance(enemy.transform.position,game.World.Player.position)<3){Melee(enemy.actor);return;}
            var target=targets.Where(t=>t.root&&t.root.gameObject.activeInHierarchy&&t.kind!=EncounterKind.CaveEntrance).OrderBy(t=>Vector3.Distance(t.Position,game.World.Player.position)).FirstOrDefault();if(target!=null&&Vector3.Distance(target.Position,game.World.Player.position)<3)Interact(target);
        }
        public string Describe(WorldEncounter e)=>e.kind==EncounterKind.CaveEntrance?"矿洞专属矿物：赤铜矿 "+State.copperOre+" · 星铁矿 "+State.starIron+"\n矿镐等级 "+State.toolLevel+" / 2，升级提高开采收益与矿洞近战伤害。\n升级需要赤铜"+(State.toolLevel==0?8:16)+"、星铁"+(State.toolLevel==0?3:6)+"、金币30；二级另需矿主碎片1。":e.kind==EncounterKind.CaveOre?"消耗体力3，获得"+(e.id<4?"赤铜矿":"星铁矿")+"。仅在矿洞获得，采石场不会生产。":e.kind==EncounterKind.CaveChest?(e.id==1&&State.enemyHealth[4]>0?"击败矿主后才能开启密藏。":"一次性宝箱：金币、矿物及工具图纸。清除矿主三天后矿洞重置。") :"离开矿洞，返回进入前的位置。战利品保留。";
        public void ImproveTool(){if(!State.Improve(game.State)){game.ShowMessage("矿镐已满级，或矿物、金币、矿主碎片不足。");return;}game.Persist();game.ShowMessage("矿镐升级完成，开采收益与矿洞近战伤害提高。");}
        public void Interact(WorldEncounter e)
        {
            if(e==null||!CaveKind(e.kind)||Vector3.Distance(e.Position,game.World.Player.position)>3)return;
            if(e.kind==EncounterKind.CaveEntrance){Enter();return;}if(!Inside)return;if(e.kind==EncounterKind.CaveExit){Exit();return;}
            if(e.kind==EncounterKind.CaveOre){if(State.mined[e.id]||!EquipmentCatalog.Require(game,"pick")||!game.State.SpendEnergy(3))return;State.mined[e.id]=true;if(e.id<4)State.copperOre+=4+State.toolLevel*2;else State.starIron+=3+State.toolLevel;e.root.gameObject.SetActive(false);game.ShowMessage("矿脉已采尽，特殊矿物收入背包。");}
            if(e.kind==EncounterKind.CaveChest){if(State.opened[e.id])return;if(e.id==1&&State.enemyHealth[4]>0){game.ShowMessage("先击败矿主，才能开启密藏。");return;}State.opened[e.id]=true;game.State.coins+=e.id==0?60:150;State.copperOre+=4;State.starIron+=2;var blueprints=game.State.adventure.expedition.equipment.blueprints;for(int i=6;i<blueprints.Length;i++)if(!blueprints[i]){blueprints[i]=true;break;}e.root.gameObject.SetActive(false);game.ShowMessage("宝箱已开启：获得金币、稀有矿物与可用工具图纸。");}
            game.Persist();
        }
        public void Melee(GardenActor actor)
        {if(!Inside||!actor||Time.time<meleeAt||Vector3.Distance(game.World.Player.position,actor.transform.position)>3)return;if(!ClearLine(game.World.Player.position,actor.transform.position)||!game.State.SpendEnergy(1))return;meleeAt=Time.time+.55f;Damage(actor,25+State.toolLevel*10);}
        public void Damage(GardenActor actor,int amount)
        {
            var e=actor.caveEnemy;if(!e||actor.Health<=0)return;actor.HitFlash();actor.zombie.health=Mathf.Max(0,actor.Health-amount);State.enemyHealth[e.index]=actor.Health;
            if(actor.Health==0){game.State.coins+=e.index==4?100:15;if(e.index==4){State.bossShards++;State.starIron+=6;State.clearedDay=game.State.day;if(!State.firstBossReward){State.firstBossReward=true;game.State.weather.starStones+=3;}game.ShowMessage("矿主倒下！获得矿主碎片与星铁，密藏已经解锁。");}actor.Fall();}game.Persist();
        }
        bool ClearLine(Vector3 a,Vector3 b)
        {return !Physics.Linecast(a+Vector3.up*.9f,b+Vector3.up*.9f,out var hit,~0,QueryTriggerInteraction.Ignore)||hit.collider.transform.IsChildOf(game.World.Player);}
        void Update()
        {
            if(!Inside||game.SimulationPaused)return;var player=game.World.Player.position;float dt=Mathf.Min(Time.deltaTime,.1f);
            foreach(var e in enemies)if(e&&e.actor.Health>0)
            {
                var delta=player-e.transform.position;delta.y=0;float distance=delta.magnitude;bool chase=distance<(e.index==4?12:9);if(chase&&!e.actor.chasing)e.actor.alertUntil=Time.time+1.5f;e.actor.chasing=chase;
                if(chase){if(distance>1.5f){var step=delta.normalized*dt*(e.index==4?1.2f:1.8f);bool free=true;foreach(var other in enemies)if(other&&other!=e&&other.actor.Health>0&&Vector3.Distance(e.transform.position+step,other.transform.position)<.8f){free=false;break;}if(free)e.transform.position+=step;e.transform.rotation=Quaternion.LookRotation(delta);}else if(Time.time>=e.attackAt&&ClearLine(e.transform.position,player)){e.attackAt=Time.time+1.5f;game.DamagePlayer(e.index==4?22:12);}}
                e.actor.Animate(dt,chase&&distance>1.5f);
            }
        }
    }
}
