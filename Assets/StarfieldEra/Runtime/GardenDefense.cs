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
        float spawnClock;GardenNavigation navigation;GardenCombatFeedback feedback;
        public FarmGame Game {get;private set;}
        public GardenDefenseState State=>Game.State.garden;
        public bool Placing=>placing>=0;
        public string Hint {get;private set;}="";
        readonly Dictionary<int,GardenActor> actors=new Dictionary<int,GardenActor>();
        readonly Dictionary<int,float> cooldowns=new Dictionary<int,float>();
        readonly List<GardenActor> tickActors=new List<GardenActor>();
        readonly List<Shot> shots=new List<Shot>();readonly Stack<GameObject> pool=new Stack<GameObject>();
        readonly RaycastHit[] hits=new RaycastHit[32];
        GardenArtLibrary art;Material peaMaterial,iceMaterial,previewMaterial;Transform bars;GameObject preview;int placing=-1;float yaw,playerCooldown;Vector3 placement;
        sealed class Shot {public GameObject root;public int target,source,damage;public float age;public bool ice;}
        public void Initialize(FarmGame owner)
        {
            Game=owner;navigation=new GardenNavigation(owner);var effects=new GameObject("Garden combat feedback");effects.transform.SetParent(transform,false);feedback=effects.AddComponent<GardenCombatFeedback>();feedback.Initialize(owner);art=Resources.Load<GardenArtLibrary>("GardenDefenseArt");
            peaMaterial=Game.World.Mat("Flying garden peas","81C928");iceMaterial=Game.World.Mat("Flying ice peas","83DEFF");previewMaterial=Game.World.Mat("Garden build preview","80B58A");
            var canvas=new GameObject("Garden health bars",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));canvas.transform.SetParent(transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=2;
            var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;bars=canvas.transform;
            if(!State.initialized)SeedOutpost();
            EnsureCemeteries();
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
        void EnsureCemeteries()
        {
            if(State.cemeteryVersion>=1)return;
            var home=Game.World.Door;var grid=Game.World.HexGrid;
            for(int tier=1;tier<=2&&State.units.Count<64;tier++)
            {
                var preferred=tier==1?new Vector3(-42,0,28):new Vector3(59,0,43);
                foreach(var cell in grid.Cells.OrderBy(c=>(HexWorldGrid.Center(c.x,c.y)-preferred).sqrMagnitude))
                {
                    var p=HexWorldGrid.Center(cell.x,cell.y);if(!grid.IsDry(cell)||WorldRelief.PlanarDistance(p,home)<(tier==1?35:55)||State.units.Any(u=>WorldRelief.PlanarDistance(u.Position,p)<8))continue;
                    bool blocked=false;foreach(var c in Physics.OverlapSphere(grid.SurfacePoint(p,.8f),.8f,~0,QueryTriggerInteraction.Ignore))if(!grid.IsGroundCollider(c)){blocked=true;break;}if(blocked)continue;
                    var tomb=State.Add(2,p);tomb.tier=tier;tomb.health=tomb.MaxHealth;break;
                }
            }
            State.cemeteryVersion=1;
        }
        GardenActor Spawn(GardenUnit unit,GardenZombie zombie)
        {
            var root=new GameObject(zombie!=null?"Garden zombie "+zombie.id:GardenDefenseState.Names[unit.kind]+" "+unit.id);root.transform.SetParent(transform,false);
            root.transform.position=Game.World.HexGrid.SurfacePoint(unit!=null?unit.Position:zombie.Position);root.transform.rotation=Quaternion.Euler(0,unit?.yaw??0,0);
            var actor=root.AddComponent<GardenActor>();actor.owner=this;actor.unit=unit;actor.zombie=zombie;
            var prefab=zombie!=null?art?.Zombie(zombie.variant):art?.Unit(unit.kind);
            if(prefab)Instantiate(prefab,root.transform,false);
            else Debug.LogError("Missing supplied garden art: "+root.name);
            var facing=root.transform.rotation;root.transform.rotation=Quaternion.identity;
            var bounds=new Bounds(root.transform.position+Vector3.up*.8f,Vector3.one);foreach(var r in root.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
            var box=root.AddComponent<BoxCollider>();box.center=root.transform.InverseTransformPoint(bounds.center);box.size=bounds.size;box.isTrigger=zombie!=null;root.transform.rotation=facing;
            if(zombie!=null)actor.SetAnimation(root.GetComponentInChildren<Animator>(),art?.Walk(zombie.variant),art?.Dismay(zombie.variant));
            if(unit!=null&&unit.kind==6){var glow=new GameObject("Warm lantern light");glow.transform.SetParent(root.transform,false);glow.transform.localPosition=Vector3.up*2.2f;var light=glow.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.73f,.35f);light.range=8;light.intensity=2;light.shadows=LightShadows.None;light.enabled=Game.State.clock.IsNight;}
            root.AddComponent<HealthBars>().Initialize(Game,actor,bars);actors[unit?.id??zombie.id]=actor;return actor;
        }
        public void BeginPlacement(int kind)
        {
            if(Game.InCave||kind<0||kind>6||kind==2||!art||!art.Unit(kind)||Game.Night.Active||Game.Adventure.InBattle)return;
            if(!EquipmentCatalog.Require(Game,"hammer"))return;
            Game.Management.Close();Game.Construction.Cancel();Game.Fences.Cancel();Game.Land.Cancel();Game.Night.CancelTrap();Game.Inspection.Clear();Cancel();placing=kind;
            preview=Instantiate(art.Unit(kind));preview.name="Garden defense placement";
            foreach(var collider in preview.GetComponentsInChildren<Collider>())collider.enabled=false;
            foreach(var r in preview.GetComponentsInChildren<Renderer>())r.sharedMaterial=previewMaterial;
            Game.ShowMessage("选择"+GardenDefenseState.Names[kind]+"位置 · R旋转 · 左键放置 · 右键取消。");
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
            Hint=GardenDefenseState.Names[placing]+" · "+(allowed?"R旋转 · 左键放置":reason);
            if(!WorldPointer.Pressed)return;
            if(!allowed){Game.ShowMessage(reason);return;}
            var unit=State.Build(Game.State,placing,placement,yaw);if(unit==null){Game.ShowMessage("材料不足，或防御设施已达64个。需要木"+GardenDefenseState.WoodCost[placing]+" 石"+GardenDefenseState.StoneCost[placing]+" 金"+GardenDefenseState.GoldCost[placing]);return;}
            Spawn(unit,null);Game.Adventure.Commit("已建成"+GardenDefenseState.Names[placing]+"："+GardenDefenseState.Details[placing]);
        }
        void Update()
        {
            if(!Game||Game.InCave)return;if(Placing)PlacementUpdate();if(Game.SimulationPaused)return;
            float dt=Mathf.Min(Time.deltaTime,.1f);playerCooldown=Mathf.Max(0,playerCooldown-dt);
            if(Game.State.clock.IsNight)
            {
                bool firstWave=State.lastWaveDay!=Game.State.day;spawnClock+=dt;
                if(firstWave||spawnClock>=SpawnInterval)
                {spawnClock=0;State.lastWaveDay=Game.State.day;foreach(var tomb in State.units.Where(u=>u.kind==2).ToArray())Raise(tomb,(firstWave?WaveSize:2)+tomb.tier);Game.Persist();}
            }
            else spawnClock=0;
            tickActors.Clear();foreach(var current in actors.Values)tickActors.Add(current);
            foreach(var a in tickActors)
            {
                if(!a||a.Health<=0)continue;int id=a.unit?.id??a.zombie.id;
                float cd=cooldowns.TryGetValue(id,out var t)?Mathf.Max(0,t-dt):0;cooldowns[id]=cd;
                if(a.zombie!=null)
                {
                    a.thinkTime+=dt;float distance=WorldRelief.PlanarDistance(a.transform.position,Game.World.Player.position);
                    if(Time.time>=a.thinkAt){float elapsed=Mathf.Min(a.thinkTime,.5f);a.thinkTime=0;a.thinkAt=Time.time+(distance<28?.04f:distance<45?.2f:.6f);MoveZombie(a,elapsed);}continue;
                }
                if(a.unit.kind==2)
                {if(a.unit.tier>0&&!a.unit.guardsAwake&&GardenDefenseState.InRange(a.transform.position,Game.World.Player.position,24)){a.unit.guardsAwake=true;Raise(a.unit,3+a.unit.tier*2);Game.Persist();}continue;}
                if(a.unit.kind==6){var glow=a.GetComponentInChildren<Light>();if(glow)glow.enabled=Game.State.clock.IsNight&&GardenDefenseState.InRange(a.transform.position,Game.World.Player.position,30);continue;}
                if((a.unit.kind!=0&&a.unit.kind!=3&&a.unit.kind!=4&&a.unit.kind!=5)||cd>0)continue;
                GardenZombie target=null;float nearest=a.unit.kind==5?2.8f*2.8f:ShootRange*ShootRange;
                foreach(var candidate in State.zombies)
                {float distance=(candidate.Position-new Vector3(a.transform.position.x,0,a.transform.position.z)).sqrMagnitude;if(distance>nearest||!actors.TryGetValue(candidate.id,out var enemy))continue;if(!ClearShot(a.transform.position+Vector3.up*1.1f,enemy.transform.position+Vector3.up,a))continue;nearest=distance;target=candidate;}
                if(target==null)continue;var towards=actors[target.id].transform.position-a.transform.position;towards.y=0;if(towards.sqrMagnitude>.01f)a.transform.rotation=Quaternion.LookRotation(towards);
                if(a.unit.kind==5){Damage(actors[target.id],150,a.transform.position);cooldowns[a.unit.id]=6;}
                else{Fire(a,target,a.unit.kind==3?-.22f:0);if(a.unit.kind==3)Fire(a,target,.22f);cooldowns[a.unit.id]=a.unit.kind==4?1.5f:1.15f;}
            }
            UpdateShots(dt);
        }
        bool ClearShot(Vector3 from,Vector3 to,GardenActor source)
        {
            var d=to-from;int count=Physics.RaycastNonAlloc(from,d.normalized,hits,d.magnitude,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++){var c=hits[i].collider;if(c.transform.IsChildOf(source.transform)||c.transform.IsChildOf(Game.World.Player))continue;return false;}return count<hits.Length;
        }
        public GardenActor NearestEnemy(Vector3 p,float range)
        {GardenActor target=null;foreach(var a in actors.Values)if(a&&a.zombie!=null&&a.Health>0){float d=WorldRelief.PlanarDistance(p,a.transform.position);if(d<range){range=d;target=a;}}return target;}
        public bool CanGuardAttack(Vector3 p,GardenActor enemy)=>enemy&&navigation.Clear(p,enemy.transform.position,null,out _,.1f);
        public void Raise(GardenUnit tomb,int count=WaveSize)
        {
            if(tomb==null||tomb.kind!=2||!State.units.Contains(tomb))return;
            if(GardenDefenseState.InRange(tomb.Position,Game.World.Player.position,35))
            {
                while(State.zombies.Count+count>GardenDefenseState.MaxZombies)
                {
                    GardenZombie distant=null;float far=40;foreach(var z in State.zombies){float d=WorldRelief.PlanarDistance(z.Position,Game.World.Player.position);if(d>far&&z.sourceTomb!=tomb.id){far=d;distant=z;}}if(distant==null)break;
                    State.zombies.Remove(distant);if(actors.TryGetValue(distant.id,out var old)){actors.Remove(distant.id);cooldowns.Remove(distant.id);Destroy(old.gameObject);}
                }
            }
            for(int i=0;i<count&&State.zombies.Count<GardenDefenseState.MaxZombies;i++)
            {float angle=(i*137.508f+State.nextId*17)*Mathf.Deg2Rad;float radius=1.8f+(i%3)*.35f;if(!navigation.SpawnPoint(tomb.Position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius),out var p))continue;var z=new GardenZombie{id=State.nextId++,x=p.x,z=p.z,tier=tomb.tier,sourceTomb=tomb.id};int roll=z.id%10;z.variant=roll<4?0:roll<7?1:roll<9?2:3;if(!art||!art.Zombie(z.variant))z.variant=0;z.health=z.MaxHealth;State.zombies.Add(z);Spawn(null,z);}
        }
        void Fire(GardenActor source,GardenZombie target,float offset=0)
        {
            if(shots.Count>=96)return;GameObject pea;
            if(pool.Count>0){pea=pool.Pop();pea.SetActive(true);}else{pea=GameObject.CreatePrimitive(PrimitiveType.Sphere);pea.name="Flying pea";pea.GetComponent<Collider>().enabled=false;Destroy(pea.GetComponent<Collider>());pea.GetComponent<Renderer>().sharedMaterial=peaMaterial;pea.transform.SetParent(transform,false);pea.transform.localScale=Vector3.one*.22f;}
            bool ice=source.unit.kind==4;pea.GetComponent<Renderer>().sharedMaterial=ice?iceMaterial:peaMaterial;pea.transform.position=source.transform.position+Vector3.up*1.1f+source.transform.forward*.65f+source.transform.right*offset;shots.Add(new Shot{root=pea,target=target.id,source=source.unit.id,damage=ice?12:15,ice=ice});
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
                    int count=Physics.SphereCastNonAlloc(s.root.transform.position,.08f,delta.normalized,hits,Mathf.Min(step,delta.magnitude),~0,QueryTriggerInteraction.Ignore);bool blocked=count==hits.Length;
                    for(int h=0;h<count;h++){var collider=hits[h].collider;var owner=collider.GetComponentInParent<GardenActor>();if((owner&&owner.unit!=null&&owner.unit.id==s.source)||collider.transform.IsChildOf(Game.World.Player))continue;blocked=true;break;}
                    if(blocked)done=true;
                    else if(delta.magnitude<=step+.15f){if(s.ice)target.slowedUntil=Time.time+2.5f;Damage(target,s.damage,s.root.transform.position);done=true;}
                    else s.root.transform.position+=delta.normalized*step;
                }
                if(done){s.root.SetActive(false);pool.Push(s.root);shots.RemoveAt(i);}
            }
        }
        bool SpaceFree(GardenActor actor,Vector3 next)
        {
            foreach(var other in tickActors)if(other&&other!=actor&&other.zombie!=null&&other.Health>0)
            {var delta=next-other.transform.position;delta.y=0;if(delta.sqrMagnitude<.25f&&WorldRelief.PlanarDistance(next,other.transform.position)<WorldRelief.PlanarDistance(actor.transform.position,other.transform.position))return false;}
            return true;
        }
        bool MeleeClear(GardenActor attacker,Vector3 target,GardenActor victim=null)
        {
            if(Mathf.Abs(attacker.transform.position.y-target.y)>1.3f)return false;
            bool clear=navigation.Clear(attacker.transform.position,target,attacker.transform,out var obstacle,.1f);
            return clear||victim&&obstacle&&obstacle.GetComponentInParent<GardenActor>()==victim;
        }
        void MoveZombie(GardenActor actor,float dt)
        {
            var p=actor.transform.position;var z=actor.zombie;var player=Game.World.Player.position;
            float playerDistance=WorldRelief.PlanarDistance(p,player);
            bool seesPlayer=playerDistance<=26&&navigation.Clear(p,player,actor.transform,out _,.08f);
            if(actor.chasing)
            {actor.lostSight=seesPlayer?0:actor.lostSight+dt;if(playerDistance>26||actor.lostSight>6){actor.chasing=false;actor.lostSight=0;actor.path.Clear();}}
            else if(playerDistance<=ChaseRange&&seesPlayer)
            {actor.chasing=true;actor.alertUntil=Time.time+1.5f;feedback.Sound(p,true);actor.repathAt=0;}
            float speed=(actor.chasing?ChaseSpeed:WanderSpeed)*(1+z.tier*.12f)*(z.variant==3?.75f:1)*(Time.time<actor.slowedUntil?.55f:1);
            GardenUnit goal=null;float nearest=float.MaxValue;
            if(!actor.chasing)foreach(var unit in State.units)if(unit.kind==0||unit.kind==3||unit.kind==4||unit.kind==5){float d=(unit.Position-new Vector3(p.x,0,p.z)).sqrMagnitude;if(d<nearest){nearest=d;goal=unit;}}
            Vector3 target=actor.chasing?player:goal!=null?actors[goal.id].transform.position:p;GardenActor victim=goal!=null?actors[goal.id]:null;
            float distance=WorldRelief.PlanarDistance(p,target);bool playerNear=actor.chasing&&playerDistance<1.4f&&MeleeClear(actor,player);
            bool stopped=Time.time<actor.stunnedUntil||playerNear||!actor.chasing&&(goal==null||distance<1.2f&&MeleeClear(actor,target,victim));
            Vector3 direction=target-p;direction.y=0;direction.Normalize();
            if(!stopped&&distance>1.2f)
            {
                bool direct=navigation.Clear(p,target,actor.transform,out var obstacle);
                if(!direct)
                {
                    if(Time.time>=actor.repathAt||(actor.pathGoal-target).sqrMagnitude>4)navigation.Route(actor,target);
                    while(actor.pathIndex<actor.path.Count&&WorldRelief.PlanarDistance(p,actor.path[actor.pathIndex])<.4f)actor.pathIndex++;
                    if(actor.pathIndex<actor.path.Count){direction=actor.path[actor.pathIndex]-p;direction.y=0;direction.Normalize();}
                    else if(obstacle)
                    {
                        var barrier=obstacle.GetComponentInParent<GardenActor>();var fence=obstacle.GetComponentInParent<FenceTarget>();
                        float reach=Vector3.Distance(obstacle.ClosestPoint(p+Vector3.up*.65f),p+Vector3.up*.65f);
                        if(reach<1.1f&&barrier&&barrier.unit!=null&&barrier.unit.kind!=2){victim=barrier;stopped=true;}
                        else if(reach<1.1f&&fence&&cooldowns[z.id]<=0){Game.Fences.Damage(fence.record);cooldowns[z.id]=1.5f;stopped=true;}
                    }
                }
                // Local separation keeps bodies from converging on a single point.
                Vector3 separation=Vector3.zero;
                foreach(var other in tickActors)if(other&&other!=actor&&other.zombie!=null&&other.Health>0)
                {var delta=p-other.transform.position;delta.y=0;float sq=delta.sqrMagnitude;if(sq<.64f){if(sq<.0001f)delta=new Vector3(z.id%2==0?1:-1,0,0);separation+=delta.normalized*(.8f-Mathf.Sqrt(sq));}}
                if(separation.sqrMagnitude>.001f)direction=(direction+Vector3.ClampMagnitude(separation,1)*1.4f).normalized;
            }
            var move=Game.World.HexGrid.ConstrainMovement(p,direction*(dt*speed),.28f);
            if(!stopped&&move.sqrMagnitude>.000001f)
            {
                var next=Game.World.HexGrid.SurfacePoint(p+move);
                if(navigation.Clear(p,next,actor.transform,out _)&&SpaceFree(actor,next))actor.transform.position=next;
                else
                {
                    actor.repathAt=0;
                    bool found=false;for(int i=0;i<4;i++){float angle=(i/2+1)*45*(i%2==0?1:-1);var side=Quaternion.Euler(0,angle,0)*direction;var step=Game.World.HexGrid.ConstrainMovement(p,side*(dt*speed),.28f);var q=Game.World.HexGrid.SurfacePoint(p+step);if(step.sqrMagnitude>.000001f&&navigation.Clear(p,q,actor.transform,out _)&&SpaceFree(actor,q)){actor.transform.position=q;direction=side;found=true;break;}}if(!found)stopped=true;
                }
            }
            if(stopped&&Time.time>=actor.stunnedUntil&&cooldowns[z.id]<=0)
            {if(playerNear&&MeleeClear(actor,player))Game.DamagePlayer(z.AttackDamage);else if(victim&&WorldRelief.PlanarDistance(p,victim.transform.position)<2.8f&&MeleeClear(actor,victim.transform.position,victim))Damage(victim,z.AttackDamage,p);cooldowns[z.id]=1.5f;}
            if(direction.sqrMagnitude>.01f)actor.transform.rotation=Quaternion.Slerp(actor.transform.rotation,Quaternion.LookRotation(direction),dt*6);
            actor.Animate(dt*Mathf.Min(1.6f,speed/.85f),!stopped&&(actor.transform.position-p).sqrMagnitude>.000001f);z.x=actor.transform.position.x;z.z=actor.transform.position.z;
        }
        public void Damage(GardenActor actor,int amount,Vector3? from=null)
        {
            if(!actor||actor.dying||actor.Health<=0||amount<=0)return;
            if(actor.caveEnemy){Game.Cave.Damage(actor,amount);return;}
            actor.HitFlash();feedback.Emit(actor.transform.position+Vector3.up*.9f);
            if(actor.zombie!=null)
            {actor.stunnedUntil=Time.time+.18f;var away=actor.transform.position-(from??Game.World.Player.position);away.y=0;var step=Game.World.HexGrid.ConstrainMovement(actor.transform.position,away.normalized*.55f,.28f);var target=Game.World.HexGrid.SurfacePoint(actor.transform.position+step);if(navigation.Clear(actor.transform.position,target,actor.transform,out _))actor.transform.position=target;actor.zombie.x=actor.transform.position.x;actor.zombie.z=actor.transform.position.z;actor.repathAt=0;}
            if(actor.zombie!=null){actor.zombie.health=Mathf.Max(0,actor.zombie.health-amount);if(actor.zombie.health==0){State.zombies.Remove(actor.zombie);State.kills++;Game.State.coins+=2;Remove(actor);Game.Persist();}}
            else{actor.unit.health=Mathf.Max(0,actor.unit.health-amount);if(actor.unit.health==0){if(actor.unit.kind==2){ClearTomb(actor);return;}State.units.Remove(actor.unit);Remove(actor);Game.Persist();}}
        }
        void Remove(GardenActor actor){int id=actor.unit?.id??actor.zombie.id;actors.Remove(id);cooldowns.Remove(id);navigation.Invalidate();if(actor.zombie!=null){actor.zombie.health=0;feedback.Emit(actor.transform.position,true);actor.Fall();}else Destroy(actor.gameObject);}
        void ClearTomb(GardenActor actor)
        {
            var tomb=actor.unit;State.units.Remove(tomb);
            foreach(var z in State.zombies.ToArray())if(z.sourceTomb==tomb.id||GardenDefenseState.InRange(z.Position,tomb.Position,10))
            {State.zombies.Remove(z);if(actors.TryGetValue(z.id,out var guard))Remove(guard);}
            State.clearedTombs++;Game.State.adventure.crystals+=2+tomb.tier*2;Game.State.weather.starStones+=1+tomb.tier;Game.State.coins+=15+tomb.tier*15;
            var equipment=Game.State.adventure.expedition.equipment;int recipe=-1;for(int i=6;i<equipment.blueprints.Length;i++)if(!equipment.blueprints[i]){equipment.blueprints[i]=true;recipe=i;break;}
            if(recipe<0)Game.State.adventure.crystals+=2;
            feedback.Emit(actor.transform.position,true);Remove(actor);Game.Persist();Game.Inspection.Clear();
            Game.ShowMessage("墓地已清除，停止生成僵尸。获得晶石"+(2+tomb.tier*2)+"、星石"+(1+tomb.tier)+"、金币"+(15+tomb.tier*15)+(recipe>=0?"，工具图纸："+EquipmentCatalog.Recipes[recipe].name:"，图纸已齐，额外晶石2")+"。");
        }
        public void Interact(GardenActor actor)
        {
            if(!actor)return;
            if(actor.caveEnemy){Game.Cave.Melee(actor);return;}
            if(actor.zombie!=null){if(playerCooldown>0)return;if(!GardenDefenseState.InRange(Game.World.Player.position,actor.transform.position,3)){Game.ShowMessage("走近这只僵尸三米以内攻击。");return;}if(!navigation.Clear(Game.World.Player.position,actor.transform.position,Game.World.Player,out _,.1f)){Game.ShowMessage("攻击被障碍挡住。");return;}if(!Game.State.SpendEnergy(1)){Game.ShowMessage("体力不足。");return;}playerCooldown=.6f;Damage(actor,20);Game.Persist();return;}
            if(actor.unit.kind==2)
            {
                if(playerCooldown>0||!GardenDefenseState.InRange(Game.World.Player.position,actor.transform.position,3))return;
                if(!navigation.Clear(Game.World.Player.position,actor.transform.position,Game.World.Player,out var wall,.1f)&&(!wall||wall.GetComponentInParent<GardenActor>()!=actor)){Game.ShowMessage("攻击被障碍挡住，请绕到墓碑旁边。");return;}
                if(!Game.State.SpendEnergy(1)){Game.ShowMessage("体力不足。");return;}
                playerCooldown=.6f;if(!actor.unit.guardsAwake){actor.unit.guardsAwake=true;Raise(actor.unit,3+actor.unit.tier*2);Game.ShowMessage("墓地守卫被惊动了！击碎墓碑可清除威胁并获得奖励。");}Damage(actor,25);Game.Persist();return;
            }
            if(actor.Health>=actor.MaxHealth){Game.ShowMessage(actor.DisplayName+"状态良好。"+GardenDefenseState.Details[actor.unit.kind]);return;}
            if(Game.State.adventure.wood<2||Game.State.adventure.stone<1){Game.ShowMessage("修复需要木材2、石料1。");return;}Game.State.adventure.wood-=2;Game.State.adventure.stone--;actor.unit.health=actor.unit.MaxHealth;Game.ShowMessage("已修复"+actor.DisplayName);Game.Persist();
        }
        public void Dismantle(GardenActor actor)
        {
            if(!actor||actor.unit==null||actor.unit.kind==2)return;int kind=actor.unit.kind;State.units.Remove(actor.unit);Game.State.adventure.wood+=GardenDefenseState.WoodCost[kind]/2;Game.State.adventure.stone+=GardenDefenseState.StoneCost[kind]/2;Remove(actor);Game.Persist();Game.Inspection.Clear();Game.ShowMessage("防御设施已拆除，返还一半木材与石料。");
        }
        void OnDestroy(){if(peaMaterial)Destroy(peaMaterial);if(iceMaterial)Destroy(iceMaterial);if(previewMaterial)Destroy(previewMaterial);if(preview)Destroy(preview);}
    }
}
