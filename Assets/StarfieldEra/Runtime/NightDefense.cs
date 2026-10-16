using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace StarfieldEra
{
    public sealed class NightDefense:MonoBehaviour
    {
        public FarmGame Game {get;private set;}
        public CampDefenseState State=>Game.State.adventure.defense;
        public bool Active=>State.active;
        public bool TrapPlacing {get;private set;}
        readonly Dictionary<int,Transform> enemies=new Dictionary<int,Transform>();
        readonly Dictionary<CampTrap,GameObject> trapModels=new Dictionary<CampTrap,GameObject>();
        GameObject trapPreview,raidHud;TextMeshProUGUI status;
        Material trapMaterial,spentMaterial;float saveClock;
        public string Forecast
        {
            get{int target=State.Target(Game.State);return target<0?"今夜平静，下次警戒日为第"+(Game.State.day+(3-Game.State.day%3))+"天":"今夜野兽将袭击："+Game.Homestead.SiteName(target)+"。休息前布置围栏与陷阱。";}
        }
        public void Initialize(FarmGame game)
        {
            Game=game;trapMaterial=game.World.Mat("Camp trap timber","B48C51");spentMaterial=game.World.Mat("Spent trap","5D5548");
            RefreshTraps();CreateHud();if(Active)foreach(var threat in State.threats)if(threat.hp>0)SpawnVisual(threat);
        }
        public bool TryBegin()
        {
            if(Active){Game.ShowMessage("先完成夜间守卫。");return true;}
            if(!State.Begin(Game.State))return false;
            State.returnX=Game.World.Player.position.x;State.returnZ=Game.World.Player.position.z;
            Game.Management.Close();Game.Construction.Cancel();Game.Fences.Cancel();Game.Land.Cancel();CancelTrap();Game.Adventure.Hud.Hide();
            MovePlayer(Game.Homestead.SitePosition(State.siteIndex)+new Vector3(-4,0,-3));
            Game.Adventure.Commit("夜袭开始！E或驱赶按钮攻击身旁野兽，R或修补按钮修复设施。守住三波袭击。");return true;
        }
        void MovePlayer(Vector3 position)
        {var p=Game.World.HexGrid.SafeLand(position,Game.World.Controller.radius);Game.World.Controller.enabled=false;Game.World.Player.position=p;Game.World.Controller.enabled=true;}
        public void ChooseTrap()
        {
            if(Active)return;Game.Management.Close();Game.Construction.Cancel();Game.Fences.Cancel();Game.Land.Cancel();Game.Adventure.Hud.Hide();
            TrapPlacing=true;Game.ShowMessage("陷阱：点击六步内、资源点八步内的空地放置。费用木2石1。右键或Esc取消。");
        }
        public void CancelTrap(){TrapPlacing=false;if(trapPreview)Destroy(trapPreview);trapPreview=null;}
        public bool PlaceTrap(Vector3 point)
        {
            var grid=Game.World.HexGrid;
            bool nearSite=Game.Homestead.Sites().Select((s,i)=>new{s,i}).Any(x=>x.s.level>0&&WorldRelief.PlanarDistance(point,Game.Homestead.SitePosition(x.i))<=8);
            if(!nearSite||!Game.Homestead.Near(point,6)||!grid.CanStand(point,.3f)||Physics.OverlapSphere(grid.SurfacePoint(point,.45f),.3f,~0,QueryTriggerInteraction.Ignore).Any(c=>!grid.IsGroundCollider(c)))
            {Game.ShowMessage("在六步内的空地布置，且需靠近已修复资源点八步以内。");return false;}
            if(!State.BuyTrap(Game.State,point)){Game.ShowMessage("陷阱需要木2石1，间距至少一米，最多24个。");return false;}
            RefreshTraps();Game.Adventure.Commit("陷阱已布置，可困住并击退一只野兽；触发后消耗。");return true;
        }
        void RefreshTraps()
        {
            foreach(var pair in trapModels)if(pair.Value)Destroy(pair.Value);trapModels.Clear();
            foreach(var trap in State.traps)
            {
                var p=Game.World.HexGrid.SurfacePoint(new Vector3(trap.x,0,trap.z),.06f);var root=new GameObject(trap.spent?"Spent camp trap":"Armed camp trap");root.transform.SetParent(transform,false);root.transform.position=p;
                for(int i=0;i<4;i++){var part=Game.World.Shape("Trap stakes",PrimitiveType.Cube,p+new Vector3(i<2?(i==0?-.4f:.4f):0,.12f,i>=2?(i==2?-.4f:.4f):0),i<2?new Vector3(.1f,.2f,.9f):new Vector3(.9f,.2f,.1f),trap.spent?spentMaterial:trapMaterial);part.SetParent(root.transform,true);}
                if(!trap.spent){var rope=Game.World.Shape("Trap rope",PrimitiveType.Cylinder,p,new Vector3(.65f,.025f,.65f),trapMaterial);rope.SetParent(root.transform,true);}
                var selectable=root.AddComponent<WorldPickTarget>();selectable.trap=trap;WorldInspection.Volume(root.transform);
                trapModels[trap]=root;
            }
        }
        void Update()
        {
            if(TrapPlacing)
            {
                if(WorldPointer.Cancel||Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){CancelTrap();return;}
                if(!WorldPointer.Available||WorldPointer.Blocked)return;
                var ray=Game.World.View.ScreenPointToRay(WorldPointer.Position);
                if(Game.World.HexGrid.RaycastGround(ray,out var point))
                {
                    if(!trapPreview){trapPreview=GameObject.CreatePrimitive(PrimitiveType.Cylinder);trapPreview.name="Trap placement preview";trapPreview.transform.localScale=new Vector3(.9f,.025f,.9f);trapPreview.GetComponent<Collider>().enabled=false;trapPreview.GetComponent<Renderer>().sharedMaterial=trapMaterial;}
                    trapPreview.transform.position=Game.World.HexGrid.SurfacePoint(point,.09f);
                    if(WorldPointer.Pressed)PlaceTrap(point);
                }
            }
            raidHud.SetActive(Active);if(!Active)return;
            float dt=Mathf.Min(Time.deltaTime,.1f);State.elapsed=Mathf.Min(60,State.elapsed+dt);State.attackCooldown=Mathf.Max(0,State.attackCooldown-dt);State.repairCooldown=Mathf.Max(0,State.repairCooldown-dt);
            int desired=State.elapsed<18?2:State.elapsed<36?4:6;
            while(State.spawned<desired)SpawnThreat(State.spawned++);
            var site=Game.Homestead.Sites()[State.siteIndex];var target=Game.Homestead.SitePosition(State.siteIndex);
            foreach(var threat in State.threats)
            {
                if(threat.hp<=0)continue;if(!enemies.TryGetValue(threat.id,out var root)||!root)continue;
                threat.cooldown=Mathf.Max(0,threat.cooldown-dt);
                var trap=State.traps.Find(t=>!t.spent&&Vector2.Distance(new Vector2(t.x,t.z),new Vector2(threat.x,threat.z))<.85f);
                if(trap!=null){trap.spent=true;Defeat(threat);RefreshTraps();continue;}
                bool playerNear=Game.Homestead.Near(root.position,1.15f);
                if(playerNear&&threat.cooldown<=0){State.playerHP=Mathf.Max(0,State.playerHP-1);threat.cooldown=2;}
                else if(WorldRelief.PlanarDistance(root.position,target)<2.65f)
                {if(threat.cooldown<=0){site.damage=Mathf.Min(100,site.damage+5);threat.cooldown=2;}}
                else MoveThreat(threat,root,target,dt);
                threat.x=root.position.x;threat.z=root.position.z;
            }
            status.text="夜间守卫 · "+Game.Homestead.SiteName(State.siteIndex)+"\n设施 "+(100-site.damage)+" / 100    守卫体力 "+State.playerHP+" / 8    波次 "+Mathf.Min(3,1+(int)(State.elapsed/18))+" / 3    剩余 "+Mathf.CeilToInt(60-State.elapsed)+"秒\nE 驱赶身旁野兽 · R 修补设施（木2石1） · 陷阱与围栏自动防御";
            saveClock+=dt;if(saveClock>=5){saveClock=0;Game.Persist();}
            if(site.damage>=100||State.playerHP<=0)Finish(false);
            else if(State.elapsed>=60||State.spawned==6&&State.threats.All(t=>t.hp<=0))Finish(true);
        }
        void SpawnThreat(int id)
        {
            var target=Game.Homestead.SitePosition(State.siteIndex);float angle=(id*61+Game.State.day*17)*Mathf.Deg2Rad;
            var p=Game.World.HexGrid.SafeLand(target+new Vector3(Mathf.Cos(angle)*7,0,Mathf.Sin(angle)*7),.3f);
            var threat=new RaidThreat{id=id,x=p.x,z=p.z};State.threats.Add(threat);SpawnVisual(threat);
        }
        void SpawnVisual(RaidThreat threat)
        {
            var root=new GameObject("Night raider "+threat.id).transform;root.SetParent(transform,false);root.position=Game.World.HexGrid.SurfacePoint(new Vector3(threat.x,0,threat.z));
            if(LocalEnvironment.Wolf(root,root.position)){}
            else{var body=Game.World.Shape("Wild beast",PrimitiveType.Capsule,root.position+Vector3.up*.4f,new Vector3(.6f,.5f,1),spentMaterial);body.SetParent(root,true);}
            var selectable=root.gameObject.AddComponent<WorldPickTarget>();selectable.threat=threat;WorldInspection.Volume(root);
            enemies[threat.id]=root;
        }
        void MoveThreat(RaidThreat threat,Transform root,Vector3 target,float dt)
        {
            var direction=target-root.position;direction.y=0;direction.Normalize();float step=dt*.95f;
            var hits=Physics.SphereCastAll(root.position+Vector3.up*.45f,.22f,direction,step+.25f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
            foreach(var hit in hits)
            {
                if(Game.World.HexGrid.IsGroundCollider(hit.collider)||hit.collider is CharacterController)continue;
                var fence=hit.collider.GetComponentInParent<FenceTarget>();
                if(fence&&(!fence.record.gate||!fence.record.open)){if(threat.cooldown<=0){Game.Fences.Damage(fence.record);threat.cooldown=2;}return;}
                // Steer around buildings, tree trunks and rocks on stable ground.
                direction=Quaternion.Euler(0,threat.id%2==0?70:-70,0)*direction;break;
            }
            var movement=Game.World.HexGrid.ConstrainMovement(root.position,direction*step,.25f);var next=Game.World.HexGrid.SurfacePoint(root.position+movement);
            if(Physics.OverlapSphere(next+Vector3.up*.45f,.2f,~0,QueryTriggerInteraction.Ignore).Any(c=>!Game.World.HexGrid.IsGroundCollider(c)&&!(c is CharacterController)))return;
            root.position=next;root.rotation=Quaternion.Slerp(root.rotation,Quaternion.LookRotation(direction),dt*7);
        }
        public void Attack(RaidThreat requested=null)
        {
            if(!Active||State.attackCooldown>0)return;
            if(requested==null&&Game.Inspection&&Game.Inspection.HasSelection){requested=Game.Inspection.Current.threat;if(requested==null){Game.ShowMessage("请先选中要驱赶的野狼。");return;}}
            var nearest=requested??State.threats.Where(t=>t.hp>0&&Game.Homestead.Near(new Vector3(t.x,0,t.z),3)).OrderBy(t=>WorldRelief.PlanarDistance(Game.World.Player.position,new Vector3(t.x,0,t.z))).FirstOrDefault();
            if(nearest==null||!State.threats.Contains(nearest)||nearest.hp<=0||!Game.Homestead.Near(new Vector3(nearest.x,0,nearest.z),3)){Game.ShowMessage("走近选中的野兽三步以内驱赶。");return;}
            State.attackCooldown=.55f;nearest.hp--;if(nearest.hp==0)Defeat(nearest);Game.Persist();
        }
        void Defeat(RaidThreat threat){threat.hp=0;if(enemies.TryGetValue(threat.id,out var root)&&root)Destroy(root.gameObject);enemies.Remove(threat.id);}
        public void Repair(int index=-1)
        {
            int chosen=Active?State.siteIndex:index;if(chosen<0||chosen>=Game.Homestead.Sites().Count)return;
            if(!Game.Homestead.Near(Game.Homestead.SitePosition(chosen),6)){Game.ShowMessage("走近设施六步以内修补。");return;}
            if(!State.Repair(Game.State,Game.Homestead.Sites()[chosen])){Game.ShowMessage("修补需要木2石1，设施有损伤；守卫中间隔3秒。");return;}
            Game.Adventure.Commit("设施已修补25点耐久。");
        }
        void Finish(bool won)
        {
            var returnPoint=new Vector3(State.returnX,0,State.returnZ);if(!State.Finish(Game.State,won))return;
            foreach(var root in enemies.Values)if(root)Destroy(root.gameObject);enemies.Clear();raidHud.SetActive(false);MovePlayer(returnPoint);
            Game.CompleteSleep();Game.Adventure.Commit(won?"守卫成功！获得15金币，设施完成生产，新的一天开始。":"野兽撤走了。受袭库存损失一半，停产一天；可用木石修复设施。");
        }
        void CreateHud()
        {
            raidHud=new GameObject("Night defense HUD",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));raidHud.transform.SetParent(transform,false);
            raidHud.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;raidHud.GetComponent<Canvas>().sortingOrder=10;
            var scaler=raidHud.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            var bar=Panel(raidHud.transform,"Night status",new Vector2(.5f,1),new Vector2(0,-90),new Vector2(980,110));status=Label(bar,"",24,new Vector2(932,96));
            var controls=Panel(raidHud.transform,"Defense actions",new Vector2(.5f,0),new Vector2(0,152),new Vector2(700,58));
            MakeButton(controls,"驱赶野兽 · E",new Vector2(-171,0),()=>Attack());MakeButton(controls,"修补设施 · R",new Vector2(171,0),()=>Repair());raidHud.SetActive(Active);
        }
        RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
        {var root=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();root.SetParent(parent,false);root.anchorMin=root.anchorMax=anchor;root.pivot=new Vector2(.5f,anchor.y);root.anchoredPosition=position;root.sizeDelta=size;root.GetComponent<UnityEngine.UI.Image>().color=new Color(.05f,.1f,.16f,.94f);root.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;return root;}
        TextMeshProUGUI Label(Transform parent,string text,float fontSize,Vector2 size)
        {var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();label.transform.SetParent(parent,false);label.rectTransform.sizeDelta=size;label.font=Game.interfaceFont;label.fontSize=fontSize;label.text=text;label.color=new Color(1,.9f,.7f);label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;}
        void MakeButton(Transform parent,string text,Vector2 position,UnityEngine.Events.UnityAction action)
        {var root=Panel(parent,text,new Vector2(.5f,.5f),position,new Vector2(326,48));root.pivot=Vector2.one*.5f;root.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;root.GetComponent<UnityEngine.UI.Image>().color=new Color(.22f,.32f,.28f);var button=root.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=root.GetComponent<UnityEngine.UI.Image>();button.onClick.AddListener(action);Label(root,text,21,new Vector2(310,44));}
    }
}
