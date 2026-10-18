using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
namespace StarfieldEra
{
    public sealed class WorldPickTarget:MonoBehaviour
    {public WorldEncounter encounter;public int plot=-1;public bool land;public Vector2Int cell;public RaidThreat threat;public CampTrap trap;}
    public sealed class WorldChoice
    {
        public Transform root;public HarvestableTree tree;public HarvestableRock rock;public RabbitActor rabbit;public AnimalActor animal;
        public GardenActor garden;public BuildingInspectable building;public ResourceSiteTarget site;public FenceTarget fence;public WorldEncounter encounter;public RaidThreat threat;public CampTrap trap;
        public int plot=-1;public bool land;public Vector2Int cell;
        public bool Alive=>(!garden||garden.Health>0)&&root&&root.gameObject.activeInHierarchy&&(!tree||!tree.Felled)&&(!rock||!rock.Depleted)&&(threat==null||threat.hp>0);
        public Vector3 Position=>land?HexWorldGrid.Center(cell.x,cell.y):root?root.position:Vector3.zero;
        public string Name=>garden?garden.DisplayName:tree?tree.planted?"种植的树木":"野外树木":rock?"石头":rabbit?"兔子 "+(rabbit.id+1):animal?"野猪":building?building.displayName:site?site.index==0?"青苔林场":"白岩采石场":fence?fence.record.gate?"牧场门":"木围栏":encounter!=null?encounter.name:threat!=null?"野狼":trap!=null?"捕兽陷阱":plot>=0?"农田 "+(plot+1):land?"土地 ["+cell.x+","+cell.y+"]":"环境物件";
    }
    public sealed class WorldInspection:MonoBehaviour
    {
        FarmGame game;TMP_FontAsset font;RectTransform panel;TextMeshProUGUI title,description,selectionCount;UnityEngine.UI.Button primary,secondary,previous,next,clearButton;
        WorldChoice selected;readonly List<WorldChoice> choices=new List<WorldChoice>();float refreshAt;
        public bool HasSelection=>selected!=null;
        public Transform SelectedRoot=>HasSelection?selected.root:null;
        public string SelectedName=>HasSelection?selected.Name:"";
        public float SelectedDistance=>HasSelection?WorldRelief.PlanarDistance(game.World.Player.position,Position(selected)):0;
        public string Hint=>!HasSelection?"":!selected.Alive?"选中的目标已消失，请重新选择":"已选中 "+selected.Name+" · E "+ActionName()+" · "+SelectedDistance.ToString("0.0")+"米";
        public void Initialize(FarmGame owner,TMP_FontAsset asset)
        {
            game=owner;font=asset;
            panel=Box(transform,"Selected world object",440,480);panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.one;panel.anchoredPosition=new Vector2(-24,-106);UITheme.Frame(panel);
            UITheme.Icon(panel,15,16,12,44,44);title=Label(panel,"",72,12,344,44,24);selectionCount=Label(panel,"",20,66,398,36,17);
            description=Label(panel,"",20,112,398,192,17);description.overflowMode=TextOverflowModes.Ellipsis;
            primary=Button(panel,"",20,316,400,42,InteractSelected);secondary=Button(panel,"",20,368,400,42,SecondaryAction);
            previous=Button(panel,"上一目标",20,426,124,38,()=>Cycle(-1));next=Button(panel,"下一目标",158,426,124,38,()=>Cycle(1));clearButton=Button(panel,"取消选择",296,426,124,38,Clear);
            gameObject.AddComponent<WorldSelectionVisual>().Initialize(game,this,font);
            RefreshPickVolumes();panel.gameObject.SetActive(false);
        }
        public void Clear(){selected=null;choices.Clear();if(panel)panel.gameObject.SetActive(false);}
        public void Select(GameObject target)
        {if(!target){Clear();return;}var choice=Resolve(target.transform)??new WorldChoice{root=target.transform};choices.Clear();choices.Add(choice);selected=choice;RefreshPanel();}
        public void RebindBuilding(PlacedBuilding record,BuildingInspectable info)
        {if(selected!=null&&selected.building&&selected.building.record==record){selected.building=info;selected.root=info.transform;}}
        public void RebindPlantedTree(Vector2Int cell,HarvestableTree tree)
        {if(selected!=null&&selected.tree&&selected.tree.planted&&selected.tree.cell==cell){selected.tree=tree;selected.root=tree.transform;}}
        public void SelectPlot(int index)
        {if(!game.State.IsPlotUnlocked(index))return;selected=new WorldChoice{root=game.World.transform.Find("Farm plot click targets/Farm plot "+index)??game.World.transform,plot=index};choices.Clear();choices.Add(selected);RefreshPanel();}
        public void SelectLand(Vector2Int cell)
        {selected=new WorldChoice{root=game.World.HexGrid.transform,land=true,cell=cell};choices.Clear();choices.Add(selected);RefreshPanel();}
        public WorldChoice Current=>HasSelection?selected:null;
        public static bool InRange(Vector3 player,Vector3 target,float range)=>range>=0&&WorldRelief.PlanarDistance(player,target)<=range;
        Vector3 Position(WorldChoice choice)=>choice.plot>=0?game.World.PlotPositions[choice.plot]:choice.Position;
        float Range()
        {if(selected.encounter!=null&&CaveGame.CaveKind(selected.encounter.kind))return 3;if(selected.garden)return selected.garden.zombie!=null||selected.garden.unit.kind==2?3:6;if(selected.building||selected.site)return 0;if(selected.plot>=0||selected.land&&game.State.land.Find(selected.cell)?.tilled==true)return 1.7f;if(selected.tree||selected.rock)return 2.7f;if(selected.encounter!=null||selected.threat!=null)return selected.threat!=null?3:2.75f;if(selected.rabbit||selected.animal)return 6;if(selected.fence)return 6;return selected.land?0:-1;}
        public bool CanInteract=>Ready&&Range()>=0&&(!game.Night.Active||selected.threat!=null||selected.garden);
        bool Ready=>HasSelection&&selected.Alive&&(Range()==0||InRange(game.World.Player.position,Position(selected),Range()));
        void Update()
        {
            if(Time.unscaledTime>=refreshAt){refreshAt=Time.unscaledTime+1;RefreshPickVolumes();}
            bool busy=game.Management.IsOpen||game.Construction.Menu.IsOpen||game.Construction.Active||game.Fences.Active||game.Land.Active||game.Night.TrapPlacing||game.Garden&&game.Garden.Placing||game.Adventure.InBattle||game.Expedition.Fishing||game.Adventure.Hud.IsContextOpen;
            if(!busy&&WorldPointer.Available&&!WorldPointer.Blocked&&WorldPointer.Clicked)Pick(game.World.View.ScreenPointToRay(WorldPointer.Position));
            if(panel)panel.gameObject.SetActive(!busy&&HasSelection);
            if(!busy&&HasSelection)RefreshPanel();
        }
        void RefreshPickVolumes()
        {
            if(!game||!game.World)return;
            foreach(var rock in game.World.GetComponentsInChildren<HarvestableRock>())Volume(rock.transform);
            foreach(var tree in game.World.GetComponentsInChildren<HarvestableTree>())Volume(tree.transform);
            foreach(var e in game.World.Adventure.Encounters)if(e.root&&!e.root.GetComponent<WorldPickTarget>()){var marker=e.root.gameObject.AddComponent<WorldPickTarget>();marker.encounter=e;Volume(e.root);}
            if(!game.World.transform.Find("Farm plot click targets"))
            {
                var root=new GameObject("Farm plot click targets").transform;root.SetParent(game.World.transform,false);
                for(int i=0;i<FarmState.PlotCount;i++){var p=new GameObject("Farm plot "+i).transform;p.SetParent(root,false);p.position=game.World.PlotPositions[i];var marker=p.gameObject.AddComponent<WorldPickTarget>();marker.plot=i;}
            }
        }
        public static void Volume(Transform root)
        {
            if(root.Find("Object selection volume"))return;
            var renderers=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!r.GetComponent<TMP_Text>()&&!r.GetComponent<TMP_SubMesh>()).ToArray();
            if(renderers.Length==0)return;var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            var child=new GameObject("Object selection volume");child.transform.SetParent(root,false);child.transform.position=bounds.center;child.transform.rotation=Quaternion.identity;child.transform.localScale=new Vector3(1/Mathf.Abs(root.lossyScale.x),1/Mathf.Abs(root.lossyScale.y),1/Mathf.Abs(root.lossyScale.z));var box=child.AddComponent<BoxCollider>();box.isTrigger=true;box.size=Vector3.Max(bounds.size,Vector3.one*.25f);
        }
        WorldChoice Resolve(Transform t)
        {
            var garden=t.GetComponentInParent<GardenActor>();if(garden)return new WorldChoice{root=garden.transform,garden=garden};
            var tree=t.GetComponentInParent<HarvestableTree>();if(tree)return new WorldChoice{root=tree.transform,tree=tree};
            var rock=t.GetComponentInParent<HarvestableRock>();if(rock)return new WorldChoice{root=rock.transform,rock=rock};
            var rabbit=t.GetComponentInParent<RabbitActor>();if(rabbit)return new WorldChoice{root=rabbit.transform,rabbit=rabbit};
            var animal=t.GetComponentInParent<AnimalActor>();if(animal)return new WorldChoice{root=animal.transform,animal=animal};
            var building=t.GetComponentInParent<BuildingInspectable>();if(building)return new WorldChoice{root=building.transform,building=building};
            var site=t.GetComponentInParent<ResourceSiteTarget>();if(site)return new WorldChoice{root=site.transform,site=site};
            var fence=t.GetComponentInParent<FenceTarget>();if(fence)return new WorldChoice{root=fence.transform,fence=fence};
            var pick=t.GetComponentInParent<WorldPickTarget>();if(pick&&pick.plot>=0&&!game.State.IsPlotUnlocked(pick.plot))return null;if(pick)return new WorldChoice{root=pick.land?game.World.HexGrid.transform:pick.plot>=0?(game.World.transform.Find("Farm plot click targets/Farm plot "+pick.plot)??pick.transform):pick.transform,land=pick.land,cell=pick.cell,encounter=pick.encounter,plot=pick.plot,threat=pick.threat,trap=pick.trap};
            var expedition=t.GetComponentInParent<ExpeditionTarget>();if(expedition)return new WorldChoice{root=expedition.transform,encounter=expedition.encounter};return null;
        }
        static string ChoiceKey(WorldChoice choice)=>choice.land?"land:"+choice.cell:choice.plot>=0?"plot:"+choice.plot:choice.root.GetInstanceID().ToString();
        bool SameLayer(Transform root)=>root&&(game.InCave?root.position.y<-25:root.position.y>-25);
        public void Pick(Ray ray)
        {
            choices.Clear();var seen=new HashSet<string>();var candidates=new List<KeyValuePair<float,WorldChoice>>();float limit=150;
            bool hasGround=game.World.HexGrid.RaycastGround(ray,out var ground);int groundPlot=-1;
            if(hasGround){limit=Mathf.Min(150,Vector3.Distance(ray.origin,ground)+.5f);game.World.TryPlotAt(ground,game.State,out groundPlot);}
            foreach(var hit in Physics.RaycastAll(ray,limit,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance))
            {
                if(hit.transform.IsChildOf(game.World.Player)||hit.collider.GetComponentInParent<LineRenderer>())continue;
                var choice=Resolve(hit.transform);if(choice!=null&&SameLayer(choice.root)&&(choice.plot<0||choice.plot==groundPlot)&&choice.Alive&&seen.Add(ChoiceKey(choice)))candidates.Add(new KeyValuePair<float,WorldChoice>(hit.distance,choice));
            }
            // Decorative art can have no collider. Include its visible bounds as an observation target,
            // sorted with gameplay targets so foreground objects can also be inspected.
            foreach(var renderer in game.GetComponentsInChildren<Renderer>())
            {
                if(renderer.GetComponentInParent<GardenCombatFeedback>()||renderer.GetComponentInParent<DrainageArt>()||renderer.name=="Natural farm beds"||!renderer.enabled||renderer is LineRenderer||renderer is ParticleSystemRenderer||renderer.GetComponent<TMP_Text>()||renderer.GetComponent<TMP_SubMesh>()||renderer.transform.IsChildOf(game.World.Player)||renderer.GetComponentInParent<ContinuousTerrain>()||renderer.transform.IsChildOf(game.World.HexGrid.transform))continue;
                if(renderer.bounds.size.x>30&&renderer.bounds.size.z>30)continue;
                if(!renderer.bounds.IntersectRay(ray,out float d)||d>limit)continue;
                var choice=Resolve(renderer.transform)??new WorldChoice{root=renderer.transform};
                if(SameLayer(choice.root)&&(choice.plot<0||choice.plot==groundPlot)&&choice.Alive&&seen.Add(ChoiceKey(choice)))candidates.Add(new KeyValuePair<float,WorldChoice>(d,choice));
            }
            // Farm targets follow the actual ground hex, never a rectangular proxy or the
            // bounds of the combined field mesh. Locked expansion cells remain ordinary land.
            if(groundPlot>=0&&seen.Add("plot:"+groundPlot))
            {
                var root=game.World.transform.Find("Farm plot click targets/Farm plot "+groundPlot);
                candidates.Add(new KeyValuePair<float,WorldChoice>(Vector3.Distance(ray.origin,ground),new WorldChoice{root=root?root:game.World.transform,plot=groundPlot}));
            }
            choices.AddRange(candidates.OrderBy(x=>x.Key).Take(24).Select(x=>x.Value));
            if(choices.Count>0){selected=choices[0];RefreshPanel();return;}
            if(game.World.HexGrid.RaycastGround(ray,out ground)&&game.World.HexGrid.TryCell(ground,out var cell)){SelectLand(cell);return;}
            Clear();
        }
        public void Cycle(int delta)
        {
            choices.RemoveAll(c=>!c.Alive);if(choices.Count==0){Clear();return;}
            int index=choices.IndexOf(selected);selected=choices[(index+delta+choices.Count)%choices.Count];RefreshPanel();
        }
        string ActionName()
        {
            if(selected.encounter!=null&&CaveGame.CaveKind(selected.encounter.kind))return selected.encounter.kind==EncounterKind.CaveOre?"开采矿脉 · 体力3":selected.encounter.kind==EncounterKind.CaveEntrance?"进入矿洞":selected.encounter.kind==EncounterKind.CaveExit?"返回营地":"开启宝箱";
            if(selected.garden)return selected.garden.zombie!=null?"攻击僵尸":selected.garden.unit.kind==2?"攻击墓碑 · 体力1":"查看 / 修复";
            if(selected.tree)return selected.tree.planted&&game.State.land.Find(selected.tree.cell)?.treeGrowth<3?"照料树苗":"砍树";
            if(selected.rock)return "开采";if(selected.building||selected.site)return "建筑管理";if(selected.fence)return selected.fence.record.gate?"开关门":"围栏管理";
            if(selected.rabbit)return "兔子操作";if(selected.animal)return "动物操作";if(selected.threat!=null)return "驱赶野狼";
            if(selected.encounter!=null)return selected.encounter.kind==EncounterKind.Fish?"钓鱼":selected.encounter.kind==EncounterKind.Elder||selected.encounter.kind==EncounterKind.Town||selected.encounter.kind==EncounterKind.Companion?"对话":selected.encounter.kind==EncounterKind.Market?"交易":"交互";
            if(selected.plot>=0||selected.land&&game.State.land.Find(selected.cell)?.tilled==true)return "种植或收获";return selected.land?"土地工具 · J":"仅供观察";
        }
        string Description()
        {
            if(selected.encounter!=null&&CaveGame.CaveKind(selected.encounter.kind))return game.Cave.Describe(selected.encounter);
            if(selected.garden&&selected.garden.caveEnemy)return "生命 "+selected.garden.Health+" / "+selected.garden.MaxHealth+"\n矿洞敌人 · 近战伤害 "+(selected.garden.caveEnemy.index==4?22:12)+"\nE攻击，消耗体力1。矿镐升级提高矿洞伤害。";
            if(selected.garden)return "生命 "+selected.garden.Health+" / "+selected.garden.MaxHealth+"\n"+(selected.garden.zombie!=null?"僵尸会靠近并攻击射手、城墙和玩家。\n攻击伤害 "+selected.garden.zombie.AttackDamage+"。\nE近战攻击，消耗体力1。":selected.garden.unit.kind!=2?GardenDefenseState.Details[selected.garden.unit.kind]+"\n受损修复需要木2石1。":"守卫等级 "+(selected.garden.unit.tier+1)+" / 3\nE攻击墓碑，每次伤害25、消耗体力1。\n摧毁后停止生成并清除关联僵尸，\n奖励工具图纸、晶石与星石。" );
            if(selected.tree&&selected.tree.planted&&game.State.land.Find(selected.tree.cell)?.treeGrowth<3){var c=game.State.land.Find(selected.tree.cell);return "树苗 · 成长 "+c.treeGrowth+" / 3\n"+(c.treeWatered?"今天已浇水":"今天尚未浇水")+"\n靠近后可为选中的树苗浇水，\n休息后继续生长，长成后可以砍伐。";}
            if(selected.tree)return "树木 · 挥斧 "+selected.tree.hits+" / 3\n需要伐木斧；每斧消耗体力1。\n第一人称需要面向选中的树木。";
            if(selected.rock)return "石料资源 · 需要采矿镐\n开采消耗体力2，采完碎裂消失。\n第一人称需要面向选中的石头。";
            if(selected.building)return "建筑 · "+(selected.building.record!=null?BuildingEconomy.Description(selected.building.record.kind):selected.building.description)+"\n打开独立面板管理生产、升级和位置。";
            if(selected.site)return "生产设施 · 可安排村民、领取库存、升级和修补。\n点击建筑管理，打开独立操作面板。";
            if(selected.fence)return (selected.fence.record.gate?"牧场门 · "+(selected.fence.record.open?"已打开":"已关闭"):"木围栏")+"\n受损 "+selected.fence.record.damage+" / 3 · 可在附近修补或拆除。";
            if(selected.rabbit)return "兔子 · "+(selected.rabbit.Record==null?"野外，可用胡萝卜引诱后捕捉":selected.rabbit.Record.status==1?"正在携带，可带回兔舍安置":"已安置，可喂养和照料")+"\n打开兔子操作只会操作这一只。";
            if(selected.animal)return "野猪 · "+(selected.animal.record==null?"野外动物，需要封闭牧场和浆果驯养":"牧场动物，可喂养并产出松露");
            if(selected.threat!=null)return "夜袭野狼 · 生命 "+selected.threat.hp+"\n三步内可驱赶，攻击只作用于选中的狼。";
            if(selected.trap!=null)return "捕兽陷阱 · "+(selected.trap.spent?"已经触发":"等待野兽触发");
            if(selected.encounter!=null)return "世界目标 · "+selected.encounter.name+"\n点击交互只会操作这个目标。\n对话、采集或钓鱼需要走近。";
            if(selected.plot>=0){var crop=game.State.plots[selected.plot];return "农田 · "+(!crop.planted?"空地，可播种":crop.Ready?"作物已成熟":crop.watered?"今天已浇水":"需要浇水")+"\nE 种植或收获；R 为这块农田浇水。";}
            if(selected.land)return game.Land.Describe(selected.cell)+"\nJ 打开独立土地建造面板。";
            return "环境物件 · 可查看外观。\n此物件暂无采集或使用动作。";
        }
        void RefreshPanel()
        {
            if(!HasSelection)return;
            if(!selected.Alive){title.text="目标已消失";selectionCount.text="点击新的目标，或取消选择";description.text="选中的物件已采集、离开或被移除。\n交互不会自动转向旁边其他物件。";primary.interactable=false;secondary.gameObject.SetActive(false);FitActions(false);return;}
            title.text=selected.Name;float distance=WorldRelief.PlanarDistance(game.World.Player.position,Position(selected));
            selectionCount.text="目标 "+(Math.Max(0,choices.IndexOf(selected))+1)+" / "+Math.Max(1,choices.Count)+" · 距离 "+distance.ToString("0.0")+"米";
            description.text=Description()+"\n\n"+(Ready?"已选定目标，E或按钮交互。":"请靠近选中的目标，不会改选附近物件。");
            primary.GetComponentInChildren<TextMeshProUGUI>().text=ActionName();primary.interactable=CanInteract;
            bool forge=selected.encounter!=null&&selected.encounter.kind==EncounterKind.CaveEntrance;
            bool water=selected.plot>=0||selected.land&&game.State.land.Find(selected.cell)?.tilled==true||selected.tree&&selected.tree.planted;
            bool dismantle=selected.garden&&selected.garden.unit!=null&&selected.garden.unit.kind!=2;
            secondary.gameObject.SetActive(water||dismantle||forge);secondary.GetComponentInChildren<TextMeshProUGUI>().text=forge?"升级矿镐":dismantle?"拆除 · 返还一半材料":"为选中目标浇水 · R";secondary.interactable=Ready;
            previous.interactable=next.interactable=choices.Count>1;FitActions(water||dismantle||forge);

        }
        void FitActions(bool water)
        {
            panel.sizeDelta=new Vector2(440,water?480:428);float y=water?-426:-374;
            foreach(var button in new[]{previous,next,clearButton}){var r=button.GetComponent<RectTransform>();r.anchoredPosition=new Vector2(r.anchoredPosition.x,y);}
        }
        public void InteractSelected()
        {
            if(!HasSelection)return;
            if(game.Management.IsOpen||game.Construction.Active||game.Fences.Active||game.Land.Active||game.Adventure.InBattle||game.Expedition.Fishing||game.Night.TrapPlacing)return;
            if(!selected.Alive){game.ShowMessage("选中的目标已消失，请重新选择。");return;}
            if(!Ready){game.ShowMessage("请靠近选中的"+selected.Name+"再交互。");return;}
            if(selected.garden){game.Garden.Interact(selected.garden);return;}
            if(selected.threat!=null){game.Night.Attack(selected.threat);return;}
            if(game.Night.Active){game.ShowMessage("先完成夜间守卫。");return;}
            if(selected.tree){if(selected.tree.planted&&game.State.land.Find(selected.tree.cell)?.treeGrowth<3)game.Land.InteractCell(selected.tree.cell,true);else game.Forestry.Chop(selected.tree);return;}
            if(selected.rock){game.Mining.Mine(selected.rock);return;}
            if(selected.building){game.Management.OpenBuilding(selected.building);return;}
            if(selected.site){game.Management.OpenResourceBuilding(selected.site.index);return;}
            if(selected.fence){var record=selected.fence.record;game.Fences.Inspect(record);if(record.gate){game.Fences.ToggleGate();game.Adventure.Hud.HideContextOnly();var target=game.GetComponentsInChildren<FenceTarget>().FirstOrDefault(f=>f.record==record);if(target){selected.root=target.transform;selected.fence=target;}}return;}
            if(selected.rabbit){game.Homestead.Inspect(selected.rabbit);return;}
            if(selected.animal){game.Animals.Inspect(selected.animal);return;}
            if(selected.encounter!=null){game.Adventure.Interact(selected.encounter);return;}
            if(selected.plot>=0){game.InteractPlot(selected.plot);return;}
            if(selected.land){if(game.State.land.Find(selected.cell)?.tilled==true)game.Land.InteractCell(selected.cell,false);else game.Management.Open("land");return;}
            game.ShowMessage("这个物件暂无交互动作。");
        }
        void SecondaryAction(){if(selected?.encounter!=null&&selected.encounter.kind==EncounterKind.CaveEntrance){if(Ready)game.Cave.ImproveTool();return;}if(selected?.garden&&selected.garden.unit!=null){if(Ready)game.Garden.Dismantle(selected.garden);return;}WaterSelected();}
        public bool WaterSelected()
        {
            if(!HasSelection)return false;
            if(game.Management.IsOpen||game.Construction.Active||game.Fences.Active||game.Land.Active||game.Adventure.InBattle||game.Night.Active)return true;
            if(!Ready){game.ShowMessage("请靠近选中的目标再浇水。");return true;}
            if(!EquipmentCatalog.Require(game,"can"))return true;
            if(selected.plot>=0){if(game.State.Water(selected.plot))game.Adventure.Commit("已为选中的农田浇水。");else game.ShowMessage("这块农田无需浇水，或体力不足。");return true;}
            if(selected.tree&&selected.tree.planted){game.Land.InteractCell(selected.tree.cell,true);return true;}
            if(selected.land&&game.State.land.Find(selected.cell)?.tilled==true){game.Land.InteractCell(selected.cell,true);return true;}
            game.ShowMessage("选中的物件不需要浇水。");return true;
        }
        RectTransform Box(Transform parent,string name,float w,float h)
        {var r=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.sizeDelta=new Vector2(w,h);var image=r.GetComponent<UnityEngine.UI.Image>();image.color=new Color(.035f,.11f,.095f,.94f);UITheme.Round(image);return r;}
        TextMeshProUGUI Label(Transform parent,string text,float x,float y,float w,float h,float size)
        {var t=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();t.rectTransform.SetParent(parent,false);t.rectTransform.anchorMin=t.rectTransform.anchorMax=t.rectTransform.pivot=new Vector2(0,1);t.rectTransform.anchoredPosition=new Vector2(x,-y);t.rectTransform.sizeDelta=new Vector2(w,h);t.font=font;t.fontSize=size;t.color=UITheme.Paper;t.text=text;t.raycastTarget=false;return t;}
        UnityEngine.UI.Button Button(Transform parent,string name,float x,float y,float w,float h,UnityEngine.Events.UnityAction callback)
        {var r=Box(parent,name,w,h);r.anchoredPosition=new Vector2(x,-y);r.GetComponent<UnityEngine.UI.Image>().color=UITheme.Card;var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=r.GetComponent<UnityEngine.UI.Image>();b.onClick.AddListener(callback);UITheme.Button(b);Label(r,name,4,3,w-8,h-6,17).alignment=TextAlignmentOptions.Center;return b;}
    }
}
