using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace StarfieldEra
{
    public sealed class GardenDefense:MonoBehaviour
    {
        public const float ShootRange=12,ChaseRange=18,ChaseSpeed=2.2f,WanderSpeed=1.3f;
        const int WaveSize=6;const float SpawnInterval=20;
        float spawnClock;
        public FarmGame Game {get;private set;}
        public GardenDefenseState State=>Game.State.garden;
        public bool Placing=>placing>=0;
        public string Hint {get;private set;}="";
        readonly Dictionary<int,GardenActor> actors=new Dictionary<int,GardenActor>();
        readonly Dictionary<int,float> cooldowns=new Dictionary<int,float>();
        readonly List<GardenActor> tickActors=new List<GardenActor>();
        readonly List<Shot> shots=new List<Shot>();readonly Stack<GameObject> pool=new Stack<GameObject>();
        readonly RaycastHit[] hits=new RaycastHit[32];
        GardenArtLibrary art;Material peaMaterial,previewMaterial;Transform bars;GameObject preview;int placing=-1;float yaw,playerCooldown;Vector3 placement;
        sealed class Shot {public GameObject root;public int target;public float age;}
        public void Initialize(FarmGame owner)
        {
            Game=owner;art=Resources.Load<GardenArtLibrary>("GardenDefenseArt");
            peaMaterial=Game.World.Mat("Flying garden peas","81C928");previewMaterial=Game.World.Mat("Garden build preview","80B58A");
            var canvas=new GameObject("Garden health bars",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));canvas.transform.SetParent(transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=2;
            var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;bars=canvas.transform;
            if(!State.initialized)SeedOutpost();
            foreach(var u in State.units)Spawn(u,null);foreach(var z in State.zombies)Spawn(null,z);
        }
        void SeedOutpost()
        {
            // A starter plant by the home and hostile tombs outside the settled area.
            var home=Game.World.Door;
            foreach(var spec in new[]{(kind:0,point:home+new Vector3(7,0,-5)),(kind:2,point:home+new Vector3(20,0,-9)),(kind:2,point:home+new Vector3(-20,0,-12))})
            {
                var candidates=Game.World.HexGrid.Cells.Select(c=>HexWorldGrid.Center(c.x,c.y)).OrderBy(p=>(p-spec.point).sqrMagnitude);
                foreach(var p in candidates){if(!PlaceAllowed(p,spec.kind,out _,false))continue;State.Add(spec.kind,p);break;}
            }
            State.initialized=true;
        }
        GardenActor Spawn(GardenUnit unit,GardenZombie zombie)
        {
            var root=new GameObject(zombie!=null?"Garden zombie "+zombie.id:unit.kind==0?"Peashooter "+unit.id:unit.kind==1?"Garden wall "+unit.id:"Zombie tomb "+unit.id);root.transform.SetParent(transform,false);
            root.transform.position=Game.World.HexGrid.SurfacePoint(unit!=null?unit.Position:zombie.Position);root.transform.rotation=Quaternion.Euler(0,unit?.yaw??0,0);
            var actor=root.AddComponent<GardenActor>();actor.owner=this;actor.unit=unit;actor.zombie=zombie;
            var prefab=zombie!=null?art?.zombie:unit.kind==0?art?.peashooter:unit.kind==1?art?.wall:art?.tomb;
            if(prefab)Instantiate(prefab,root.transform,false);
            else Debug.LogError("Missing supplied garden art: "+root.name);
            var facing=root.transform.rotation;root.transform.rotation=Quaternion.identity;
            var bounds=new Bounds(root.transform.position+Vector3.up*.8f,Vector3.one);foreach(var r in root.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
            var box=root.AddComponent<BoxCollider>();box.center=root.transform.InverseTransformPoint(bounds.center);box.size=bounds.size;box.isTrigger=zombie!=null;root.transform.rotation=facing;
            if(zombie!=null)actor.SetAnimation(root.GetComponentInChildren<Animator>(),art?.walk,art?.dismay);
            root.AddComponent<HealthBars>().Initialize(Game,actor,bars);actors[unit?.id??zombie.id]=actor;return actor;
        }
        public void BeginPlacement(int kind)
        {
            if(kind<0||kind>1||Game.Night.Active||Game.Adventure.InBattle)return;
            if(!EquipmentCatalog.Require(Game,"hammer"))return;
            Game.Management.Close();Game.Construction.Cancel();Game.Fences.Cancel();Game.Land.Cancel();Game.Night.CancelTrap();Game.Inspection.Clear();Cancel();placing=kind;
            preview=Instantiate(kind==0?art.peashooter:art.wall);preview.name="Garden defense placement";
            foreach(var collider in preview.GetComponentsInChildren<Collider>())collider.enabled=false;
            foreach(var r in preview.GetComponentsInChildren<Renderer>())r.sharedMaterial=previewMaterial;
            Game.ShowMessage("选择"+(kind==0?"豌豆射手":"防御城墙")+"位置 · R旋转 · 左键放置 · 右键取消。");
        }
        public void Cancel(){placing=-1;Hint="";if(preview)Destroy(preview);preview=null;}
        public bool PlaceAllowed(Vector3 p,int kind,out string reason,bool playerRange=true)
        {
            reason="";var grid=Game.World.HexGrid;float radius=kind==1?1.6f:.65f;
            if(playerRange&&WorldRelief.PlanarDistance(Game.World.Player.position,p)>18){reason="请在角色18米内建造";return false;}
            if(!grid.CanStand(p,radius)||Game.State.storm.DepthAt(p)>.15f){reason="需要干燥且可以站立的土地";return false;}
            for(int i=0;i<Game.State.AvailablePlotCount;i++)if(WorldRelief.PlanarDistance(Game.World.PlotPositions[i],p)<radius+1){reason="请避开已有农田";return false;}
            if(Game.State.land.cells.Any(c=>c.tilled&&WorldRelief.PlanarDistance(HexWorldGrid.Center(c.column,c.row),p)<radius+1)){reason="请避开已开垦土地";return false;}
            if(State.units.Any(u=>WorldRelief.PlanarDistance(u.Position,p)<(kind==1||u.kind==1?2:1.4f))){reason="与已有防御设施重叠";return false;}
            if(Game.State.adventure.buildings.Any(b=>BuildingFootprints.Cells(b).Any(c=>WorldRelief.PlanarDistance(HexWorldGrid.Center(c.x,c.y),p)<radius+1))){reason="占用了建筑地基";return false;}
            var point=grid.SurfacePoint(p,.65f);
            foreach(var c in Physics.OverlapSphere(point,radius,~0,QueryTriggerInteraction.Ignore))if(!grid.IsGroundCollider(c)&&!c.transform.IsChildOf(Game.World.Player)){reason="与建筑、树木或岩石重叠";return false;}
            if(WorldRelief.PlanarDistance(Game.World.Player.position,p)<radius+.8f){reason="请避开角色站立的位置";return false;}
            return true;
        }
        void PlacementUpdate()
        {
            if(WorldPointer.Cancel||Keyboard.current?.escapeKey.wasPressedThisFrame==true){Cancel();return;}
            if(Game.Management.IsOpen||Game.Construction.Menu.IsOpen){Cancel();return;}
            if(Keyboard.current?.rKey.wasPressedThisFrame==true)yaw=(yaw+60)%360;
            if(!WorldPointer.Available||WorldPointer.Blocked||!Game.World.HexGrid.RaycastGround(Game.World.View.ScreenPointToRay(WorldPointer.Position),out placement)){preview.SetActive(false);return;}
            bool allowed=PlaceAllowed(placement,placing,out var reason);preview.SetActive(true);preview.transform.position=Game.World.HexGrid.SurfacePoint(placement);preview.transform.rotation=Quaternion.Euler(0,yaw,0);previewMaterial.SetColor("_BaseColor",allowed?new Color(.5f,.8f,.4f):new Color(.9f,.25f,.2f));
            Hint=(placing==0?"豌豆射手 · 索敌12米":"防御城墙 · 300耐久")+" · "+(allowed?"R旋转 · 左键放置":reason);
            if(!WorldPointer.Pressed)return;
            if(!allowed){Game.ShowMessage(reason);return;}
            var unit=State.Build(Game.State,placing,placement,yaw);if(unit==null){Game.ShowMessage("材料不足，或防御设施已达64个。需要木"+GardenDefenseState.WoodCost[placing]+" 石"+GardenDefenseState.StoneCost[placing]+" 金"+GardenDefenseState.GoldCost[placing]);return;}
            Spawn(unit,null);Game.Adventure.Commit("已建成"+(placing==0?"豌豆射手：自动攻击12米内可见僵尸。":"防御城墙：阻挡僵尸，受损后可选中修复。"));
        }
        void Update()
        {
            if(!Game)return;if(Placing)PlacementUpdate();if(Game.SimulationPaused)return;
            float dt=Mathf.Min(Time.deltaTime,.1f);playerCooldown=Mathf.Max(0,playerCooldown-dt);
            if(Game.State.clock.IsNight)
            {
                bool firstWave=State.lastWaveDay!=Game.State.day;spawnClock+=dt;
                if(firstWave||spawnClock>=SpawnInterval)
                {spawnClock=0;State.lastWaveDay=Game.State.day;foreach(var tomb in State.units.Where(u=>u.kind==2).ToArray())Raise(tomb,firstWave?WaveSize:2);Game.Persist();}
            }
            else spawnClock=0;
            tickActors.Clear();foreach(var current in actors.Values)tickActors.Add(current);
            foreach(var a in tickActors)
            {
                if(!a||a.Health<=0)continue;int id=a.unit?.id??a.zombie.id;
                float cd=cooldowns.TryGetValue(id,out var t)?Mathf.Max(0,t-dt):0;cooldowns[id]=cd;
                if(a.zombie!=null){MoveZombie(a,dt);continue;}
                if(a.unit.kind!=0||cd>0)continue;
                GardenZombie target=null;float nearest=ShootRange*ShootRange;
                foreach(var candidate in State.zombies)
                {float distance=(candidate.Position-new Vector3(a.transform.position.x,0,a.transform.position.z)).sqrMagnitude;if(distance>nearest||!actors.TryGetValue(candidate.id,out var enemy))continue;if(!ClearShot(a.transform.position+Vector3.up*1.1f,enemy.transform.position+Vector3.up,a))continue;nearest=distance;target=candidate;}
                if(target==null)continue;var towards=actors[target.id].transform.position-a.transform.position;towards.y=0;if(towards.sqrMagnitude>.01f)a.transform.rotation=Quaternion.LookRotation(towards);
                Fire(a,target);cooldowns[a.unit.id]=1.15f;
            }
            UpdateShots(dt);
        }
        bool ClearShot(Vector3 from,Vector3 to,GardenActor source)
        {
            var d=to-from;int count=Physics.RaycastNonAlloc(from,d.normalized,hits,d.magnitude,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++){var c=hits[i].collider;if(c.transform.IsChildOf(source.transform)||c.transform.IsChildOf(Game.World.Player))continue;return false;}return count<hits.Length;
        }
        public void Raise(GardenUnit tomb,int count=WaveSize)
        {
            if(tomb==null||tomb.kind!=2||!State.units.Contains(tomb))return;
            for(int i=0;i<count&&State.zombies.Count<GardenDefenseState.MaxZombies;i++)
            {float angle=(i*137.508f+State.nextId*17)*Mathf.Deg2Rad;float radius=1.8f+(i%3)*.35f;var p=Game.World.HexGrid.SafeLand(tomb.Position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius),.3f);var z=new GardenZombie{id=State.nextId++,x=p.x,z=p.z};State.zombies.Add(z);Spawn(null,z);}
        }
        void Fire(GardenActor source,GardenZombie target)
        {
            if(shots.Count>=96)return;GameObject pea;
            if(pool.Count>0){pea=pool.Pop();pea.SetActive(true);}else{pea=GameObject.CreatePrimitive(PrimitiveType.Sphere);pea.name="Flying pea";Destroy(pea.GetComponent<Collider>());pea.GetComponent<Renderer>().sharedMaterial=peaMaterial;pea.transform.SetParent(transform,false);pea.transform.localScale=Vector3.one*.22f;}
            pea.transform.position=source.transform.position+Vector3.up*1.1f+source.transform.forward*.65f;shots.Add(new Shot{root=pea,target=target.id});
        }
        void UpdateShots(float dt)
        {
            for(int i=shots.Count-1;i>=0;i--)
            {
                var s=shots[i];s.age+=dt;bool done=s.age>2.5f;
                if(!actors.TryGetValue(s.target,out var target)||!target||target.zombie==null)done=true;
                if(!done)
                {
                    var delta=target.transform.position+Vector3.up-s.root.transform.position;float step=dt*14;
                    if(Physics.SphereCast(s.root.transform.position,.08f,delta.normalized,out var hit,Mathf.Min(step,delta.magnitude),~0,QueryTriggerInteraction.Ignore))done=true;
                    else if(delta.magnitude<=step+.15f){Damage(target,15);done=true;}
                    else s.root.transform.position+=delta.normalized*step;
                }
                if(done){s.root.SetActive(false);pool.Push(s.root);shots.RemoveAt(i);}
            }
        }
        void MoveZombie(GardenActor actor,float dt)
        {
            var p=actor.transform.position;var z=actor.zombie;
            bool chasing=GardenDefenseState.InRange(p,Game.World.Player.position,ChaseRange);
            float speed=chasing?ChaseSpeed:WanderSpeed;
            GardenUnit goal=null;float nearest=float.MaxValue;
            foreach(var unit in State.units)if(!chasing&&unit.kind==0){float d=(unit.Position-new Vector3(p.x,0,p.z)).sqrMagnitude;if(d<nearest){nearest=d;goal=unit;}}
            Vector3 target=chasing?Game.World.Player.position:goal!=null?actors[goal.id].transform.position:p;GardenActor victim=goal!=null?actors[goal.id]:null;
            bool playerNear=GardenDefenseState.InRange(p,Game.World.Player.position,1.4f);var direction=target-p;direction.y=0;float distance=direction.magnitude;direction=direction.normalized;
            var move=Game.World.HexGrid.ConstrainMovement(p,direction*(dt*speed),.28f);
            bool stopped=distance<1.2f||playerNear;Collider obstacle=null;
            int count=Physics.SphereCastNonAlloc(p+Vector3.up*.65f,.22f,direction,hits,.8f,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++){var c=hits[i].collider;if(Game.World.HexGrid.IsGroundCollider(c)||c.transform.IsChildOf(actor.transform)||c is CharacterController)continue;obstacle=c;break;}
            if(obstacle){var other=obstacle.GetComponentInParent<GardenActor>();if(other&&other.unit!=null&&other.unit.kind!=2){victim=other;stopped=true;}
                else{var fence=obstacle.GetComponentInParent<FenceTarget>();if(fence){stopped=true;if(cooldowns[z.id]<=0){Game.Fences.Damage(fence.record);cooldowns[z.id]=1.5f;}}
                    else{direction=Quaternion.Euler(0,(z.id%2==0?65:-65),0)*direction;move=Game.World.HexGrid.ConstrainMovement(p,direction*(dt*speed),.28f);var ray=new Ray(p+Vector3.up*.65f,direction);if(Physics.SphereCast(ray,.22f,.65f,~0,QueryTriggerInteraction.Ignore))stopped=true;}}}
            if(stopped&&cooldowns[z.id]<=0){if(playerNear)Game.DamagePlayer(10);else if(victim)Damage(victim,10);cooldowns[z.id]=1.5f;}
            if(!stopped){var next=p+move;actor.transform.position=Game.World.HexGrid.SurfacePoint(next);}
            if(direction.sqrMagnitude>.01f)actor.transform.rotation=Quaternion.Slerp(actor.transform.rotation,Quaternion.LookRotation(direction),dt*6);
            actor.Animate(dt*Mathf.Min(1.6f,speed/.85f),!stopped&&move.sqrMagnitude>.000001f);z.x=actor.transform.position.x;z.z=actor.transform.position.z;
        }
        public void Damage(GardenActor actor,int amount)
        {
            if(!actor||amount<=0)return;
            if(actor.zombie!=null){actor.zombie.health=Mathf.Max(0,actor.zombie.health-amount);if(actor.zombie.health==0){State.zombies.Remove(actor.zombie);State.kills++;Game.State.coins+=2;Remove(actor);Game.Persist();}}
            else{actor.unit.health=Mathf.Max(0,actor.unit.health-amount);if(actor.unit.health==0){State.units.Remove(actor.unit);Remove(actor);Game.Persist();}}
        }
        void Remove(GardenActor actor){int id=actor.unit?.id??actor.zombie.id;actors.Remove(id);cooldowns.Remove(id);Destroy(actor.gameObject);}
        public void Interact(GardenActor actor)
        {
            if(!actor)return;
            if(actor.zombie!=null){if(playerCooldown>0)return;if(!GardenDefenseState.InRange(Game.World.Player.position,actor.transform.position,3)){Game.ShowMessage("走近这只僵尸三米以内攻击。");return;}if(!Game.State.SpendEnergy(1)){Game.ShowMessage("体力不足。");return;}playerCooldown=.6f;Damage(actor,20);Game.Persist();return;}
            if(actor.unit.kind==2){if(!GardenDefenseState.InRange(Game.World.Player.position,actor.transform.position,6))return;Raise(actor.unit);Game.ShowMessage("墓碑被惊动了！僵尸正在靠近，退到豌豆射手身后。");Game.Persist();return;}
            if(actor.Health>=actor.MaxHealth){Game.ShowMessage(actor.DisplayName+"状态良好。豌豆射手自动攻击12米内可见的僵尸，城墙可以阻挡它们。");return;}
            if(Game.State.adventure.wood<2||Game.State.adventure.stone<1){Game.ShowMessage("修复需要木材2、石料1。");return;}Game.State.adventure.wood-=2;Game.State.adventure.stone--;actor.unit.health=actor.unit.MaxHealth;Game.ShowMessage("已修复"+actor.DisplayName);Game.Persist();
        }
        public void Dismantle(GardenActor actor)
        {
            if(!actor||actor.unit==null||actor.unit.kind==2)return;int kind=actor.unit.kind;State.units.Remove(actor.unit);Game.State.adventure.wood+=GardenDefenseState.WoodCost[kind]/2;Game.State.adventure.stone+=GardenDefenseState.StoneCost[kind]/2;Remove(actor);Game.Persist();Game.Inspection.Clear();Game.ShowMessage("防御设施已拆除，返还一半木材与石料。");
        }
        void OnDestroy(){if(peaMaterial)Destroy(peaMaterial);if(previewMaterial)Destroy(previewMaterial);if(preview)Destroy(preview);}
    }
}
