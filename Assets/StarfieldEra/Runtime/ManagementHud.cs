using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace StarfieldEra
{
    public sealed partial class ManagementHud : MonoBehaviour
    {
        FarmGame game;FarmHud worldHud;TMP_FontAsset font;RectTransform overlay,window,content;
        TextMeshProUGUI heading,wallet,feedback,pageIntro;
        UnityEngine.UI.Image pageEmblem;
        readonly Dictionary<string,UnityEngine.UI.Button> tabs=new Dictionary<string,UnityEngine.UI.Button>();
        string page="inventory",filter="全部",itemKey="wood",buildingKey="",signature="",openingMessage="";
        int selectedLandTool,equipmentSelection,equipmentPage,inventoryPage;
        bool equipmentView=true,blueprintView;
        int tutorialSection,selectedTask,selectedRecipe,selectedTech,selectedRegion,selectedCompanion,selectedTrade,buildingPage;
        bool expeditionTasks,confirmDemolition;
        readonly Color cream=new Color(.94f,.89f,.77f),gold=new Color(.83f,.69f,.41f),ink=new Color(.22f,.21f,.16f),green=new Color(.07f,.18f,.16f);
        public string Page=>page;
        public bool IsOpen=>IsHandbookOpen||IsOperationsOpen;
        static readonly string[] PageKeys={"inventory","weather","tasks","craft","town","companions","map","market","tutorial","camera","homestead"};
        static readonly string[] PageNames={"背包","天气","任务","制作","城镇","伙伴","地图","交易","操作教程","视角","牧场与营地"};
        static readonly string[] RecipeKeys={"bait","rod","boots","amulet","camp","meal"};
        static readonly string[] RecipeNames={"草编鱼饵","精制鱼竿","湿地长靴","星辉护符","野营补给","暖心料理"};
        static readonly string[] RecipeCosts={"纤维2 · 浆果1","木材8 · 纤维4 · 知识2","纤维8 · 木材4 · 金币20","碎片3 · 晶石2 · 知识6","纤维4 · 浆果4 · 鲜鱼1","浆果3 · 鲜鱼1"};
        static readonly string[] TechKeys={"workshop","library","irrigation","lore","farm","mill","tools","observatory"};
        static readonly string[] TechNames={"工坊","书院","灌溉技术","星辉学","农场扩建","磨坊","采集工具","观星台"};
        static readonly string[] TechCosts={"木6 石4 金15","木8 石6 金20","知4 木4 石4","知6 晶1","木8 石6 金30","木10 石8 金30","木6 石5 金25","木18 石14 晶3 知20"};
        AdventureState A=>game.State.adventure;ExpeditionState X=>A.expedition;
        public void Create(FarmGame owner,TMP_FontAsset asset)
        {
            game=owner;worldHud=GetComponent<FarmHud>();font=asset;
            overlay=Box(transform,"Management backdrop",0,0,0,0,new Color(.015f,.035f,.03f,.42f),true);
            overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;
            window=Box(overlay,"Traveler handbook",0,0,1440,820,new Color(.035f,.105f,.09f,.93f),true);window.anchorMin=window.anchorMax=window.pivot=new Vector2(.5f,.5f);window.anchoredPosition=Vector2.zero;UITheme.Frame(window);
            var header=Box(window,"Handbook header",12,12,1416,88,new Color(.085f,.19f,.15f,.86f));
            UITheme.Icon(header,9,16,8,64,64);Text(header,"旅人手册",92,7,236,43,29,cream);Text(header,"旅途 · 生活 · 营地",94,54,226,26,14,gold);
            pageEmblem=UITheme.Icon(header,0,326,14,52,52);
            heading=Text(header,"",392,16,628,44,27,cream);
            UITheme.Icon(header,14,1042,14,38,38);wallet=Text(header,"",1092,10,302,66,16,gold);wallet.alignment=TextAlignmentOptions.Right;
            var rail=Box(window,"Navigation rail",16,112,210,690,new Color(.025f,.08f,.07f,.70f));
            for(int i=0;i<PageKeys.Length;i++)
            {
                string key=PageKeys[i];var button=Button(rail,PageNames[i],8,10+i*48,194,42,()=>Open(key));tabs[key]=button;
                UITheme.Icon(button.transform,UITheme.PageIcon(key),9,4,34,34);var label=button.GetComponentInChildren<TextMeshProUGUI>();label.rectTransform.anchoredPosition=new Vector2(52,-6);label.rectTransform.sizeDelta=new Vector2(134,30);label.alignment=TextAlignmentOptions.MidlineLeft;label.fontSize=18;
            }
            Button(rail,"返回世界",10,634,190,42,Close);
            pageIntro=Text(window,"",246,112,1168,34,18,new Color(.76f,.84f,.74f));
            content=Box(window,"Management page",244,156,1168,598,new Color(0,0,0,0));
            feedback=Text(window,"",246,777,1168,28,15,new Color(.82f,.83f,.66f));
            overlay.gameObject.SetActive(false);CreateOperationsWindow();
        }
        public void Toggle(string target){if(IsOpen&&page==target)Close();else Open(target);}
        public void Open(string target)
        {
            if(game.Adventure.InBattle)return;
            if(game.Night&&game.Night.Active){game.ShowMessage("先完成夜间守卫，再打开旅人手册。");return;}
            game.Night?.CancelTrap();
            if(game.Expedition.Fishing)game.Expedition.CancelFishing();
            if(!IsOperationsPage(target)&&target!="homestead"&&!PageKeys.Contains(target))target="inventory";
            bool changed=page!=target;if(!IsOpen||changed)openingMessage=game.Message;
            game.Land?.Cancel();game.Construction.Cancel();game.Fences.Cancel();game.Adventure.Hud.HideContextOnly();
            SelectWindow(target);page=target;worldHud.SetWorldVisible(false);overlay.gameObject.SetActive(true);overlay.SetAsLastSibling();
            if(changed&&target=="tasks")selectedTask=Mathf.Clamp(expeditionTasks?X.quest:A.quest,0,expeditionTasks?2:5);
            confirmDemolition=false;signature="";Rebuild();
        }
        public void OpenFromMode(string mode)
        {
            if(mode=="journal"){expeditionTasks=false;selectedTask=Mathf.Min(A.quest,5);Open("tasks");}
            else if(mode=="expedition"){expeditionTasks=true;selectedTask=Mathf.Min(X.quest,2);Open("tasks");}
            else if(mode=="supplies")Open("inventory");
            else if(mode=="craft"||mode=="craft2"){equipmentView=true;if(mode=="craft2"){equipmentSelection=11;equipmentPage=1;}Open("craft");}
            else if(mode=="town"||mode=="town2"){if(mode=="town2")selectedTech=4;Open("town");}
            else if(mode=="map"||mode=="map2")Open("map");
            else if(mode=="market")Open("market");
            else Open("companions");
        }
        public static bool Handles(string mode)=>mode=="journal"||mode=="expedition"||mode=="supplies"||mode=="craft"||mode=="craft2"||mode=="town"||mode=="town2"||mode=="map"||mode=="map2"||mode=="companions"||mode=="market";
        public void Close(){if(!IsOpen)return;if(overlay)overlay.gameObject.SetActive(false);worldHud.SetWorldVisible(true);confirmDemolition=false;game.Persist();}
        void Update(){if(IsOpen){FitWindow();Refresh();}}
        void FitWindow(){var area=overlay.rect;if(area.width<1||area.height<1)return;float fit=Mathf.Min(1,Mathf.Min((area.width-32)/window.sizeDelta.x,(area.height-32)/window.sizeDelta.y));window.localScale=Vector3.one*Mathf.Max(.1f,fit);}
        public void Refresh()
        {
            if(!IsOpen)return;
            string next=game.Adventure.Revision+"/"+game.State.day+"/"+game.State.selectedCrop+"/"+game.State.energy+"/"+game.Message;
            if(next==signature)return;Rebuild();
        }
        void Rebuild()
        {
            signature=game.Adventure.Revision+"/"+game.State.day+"/"+game.State.selectedCrop+"/"+game.State.energy+"/"+game.Message;
            for(int i=content.childCount-1;i>=0;i--){content.GetChild(i).gameObject.SetActive(false);Destroy(content.GetChild(i).gameObject);}
            int tab=Array.IndexOf(PageKeys,page);heading.text=IsOperationsPage(page)?OperationsTitle(page):(page=="homestead"?"牧场与经营":(page=="building"?"建筑管理":page=="land"?"土地建造":PageNames[tab]))+" · "+(page=="building"?"生产与建设":page=="weather"?"天气与经验预判":page=="homestead"?"照料与生产":page=="inventory"?"物品与装备":page=="tasks"?"委托与进度":page=="craft"?"配方与准备":page=="town"?"建设与发展":page=="companions"?"同行与能力":page=="market"?"买卖与订单":page=="land"?"改造与生态":page=="tutorial"?"按键与玩法":page=="camera"?"观察与跟随":"大陆与旅途");
            pageEmblem.sprite=UITheme.GetIcon(IsOperationsPage(page)?page=="land"?8:17:UITheme.PageIcon(page));pageIntro.text=PageIntroduction();
            wallet.text="金币 "+game.State.coins+"   知识 "+A.science+"\n第 "+game.State.day+" 天 · "+game.State.Season;
            foreach(var pair in tabs){pair.Value.GetComponent<UnityEngine.UI.Image>().color=pair.Key==page?new Color(.24f,.34f,.24f,.98f):new Color(.06f,.17f,.14f,.72f);pair.Value.GetComponent<UnityEngine.UI.Outline>().effectColor=pair.Key==page?new Color(.85f,.72f,.42f,.80f):new Color(.50f,.58f,.39f,.20f);}
            feedback.text=(game.Message==openingMessage?"操作自动保存 · 返回游戏后继续探索":game.Message)+(game.SaveHealthy?"":" · 保存失败");
            if(page=="villagers"||page=="orders"||page=="caravan"||page=="frontier")SettlementWorkspace();else if(page=="production"||page=="buildings"||page=="camp")OperationsWorkspace();else if(page=="building")BuildingManagement();else if(page=="weather")WeatherPage();else if(page=="homestead")HomesteadPage();else if(page=="inventory")Inventory();else if(page=="tasks")Tasks();else if(page=="craft")Crafting();else if(page=="town")Town();else if(page=="companions")Companions();else if(page=="market")Market();else if(page=="land")LandTools();else if(page=="tutorial")Tutorial();else if(page=="camera")CameraSettings();else Map();
        }
        void CameraSettings()
        {
            var rig=game.World.Adventure.CameraRig;
            Text(content,"当前视角："+PlayerCameraRig.ModeNames[rig.Mode],0,4,1100,42,26,cream);
            string[] descriptions={"稳定跟随角色。\n适合种田、建造与日常探索。\n\n世界方向固定，走路时不随角色转向。","从高处观察地图。\n适合规划道路、围栏与城镇。\n\n保留正交地图的清晰比例。","近距离跟随角色。\n适合观察角色和村落细节。\n\n镜头会避开地形与建筑。","角色视线高度，跟随朝向。\n\n左键拖动转向与抬头低头。\nW/S 前后 · A/D 平移。"};
            for(int i=0;i<PlayerCameraRig.ModeNames.Length;i++)
            {
                int mode=i;float x=i*291;var card=Box(content,"View card",x,66,275,300,new Color(.09f,.22f,.19f));UITheme.Frame(card);
                UITheme.Icon(card,i<2?6:10,12,12,52,52);Text(card,PlayerCameraRig.ModeNames[i],78,22,179,36,22,gold);Text(card,descriptions[i],18,78,239,144,18,cream);
                Button(card,rig.Mode==i?"已启用":"使用此视角",18,238,239,44,()=>{rig.SetMode(mode);Rebuild();},rig.Mode!=i);
            }
            Text(content,"V 切换视角 · 左键拖动转向 · H 建造 · J 土地 · Shift 奔跑 · 空格跳跃",0,390,1120,38,19,cream);
            Button(content,"向左转动",0,452,270,48,()=>{rig.Turn(-45);Rebuild();});
            Button(content,"向右转动",292,452,270,48,()=>{rig.Turn(45);Rebuild();});
            Button(content,"拉近视野",584,452,270,48,()=>rig.AdjustZoom(-2));
            Button(content,"拉远视野",876,452,270,48,()=>rig.AdjustZoom(2));
            Button(content,"恢复默认方向与距离",0,524,562,46,()=>{rig.ResetView();Rebuild();});
            Text(content,"视角偏好自动保存。",590,531,540,36,18,gold);
        }
        void LandTools()
        {
            Text(content,"J · 土地建造工具",0,4,620,40,24,cream);
            for(int i=0;i<LandStewardship.Names.Length;i++)
            {int id=i;var button=Button(content,LandStewardship.Names[i]+"   "+LandStewardship.Costs[i],0,60+i*43,620,38,()=>{selectedLandTool=id;Rebuild();});UITheme.Icon(button.transform,i==0?18:i==1?26:i==4?7:24,10,4,35,35,true);button.GetComponent<UnityEngine.UI.Image>().color=selectedLandTool==i?new Color(.28f,.36f,.27f):new Color(.1f,.23f,.2f);}
            Text(content,"土方 "+game.State.land.soil+" · 木材 "+A.wood+" · 石料 "+A.stone+" · 纤维 "+X.fiber,0,475,620,36,18,cream);
            Text(content,"灌溉技术："+(A.irrigation?"已掌握，临水农田自动浇水":"未研究，临水可保湿；自动浇水需研究"),0,519,630,54,17,cream);
            var detail=Details(LandStewardship.Names[selectedLandTool],LandStewardship.Costs[selectedLandTool],LandStewardship.Descriptions[selectedLandTool]+"\n\n选择工具后点击六步内的地块。\n绿色可施工，红色不可施工。\n右键 / Esc 取消；施工自动保存。");
            string missing=EquipmentCatalog.LandMissing(game,selectedLandTool);
            DetailAction(detail,missing==""?"使用工具并返回地图":missing,0,()=>game.Land.Choose(selectedLandTool),missing=="");
            if(missing!="")DetailAction(detail,"查看所需工具与图纸",1,()=>{equipmentView=true;blueprintView=true;equipmentSelection=EquipmentCatalog.Index(!EquipmentCatalog.Has(game,"shovel")?"shovel":"forester");equipmentPage=equipmentSelection/6;Open("craft");});
            else DetailAction(detail,"查看当前位置的土壤",1,()=>game.ShowMessage(game.Land.Describe(CurrentCell())));
        }
        void Tutorial()
        {
            if(MobileControls.IsTouchMode)
            {
                TutorialCard(0,"手机操作 · 探索与农耕","左下摇杆 · 移动角色\n点击交互 · 采集、对话、种植或收获\n点击浇水 · 浇灌附近作物\n种子 · 切换当前种子\n购买 · 在小屋或商人旁购买种子\n休息 · 在小屋门前结束一天\n\n视野按钮调整地图远近。\n顶部旅人手册查看背包、任务、伙伴与地图。\n钓鱼时等待咬钩，点击交互收线。\n战斗时点击面板上的行动按钮。");
                TutorialCard(590,"手机操作 · 建造与土地","点击建造，选择建筑后点击地图放置。\n旋转 · 调整建筑方向\n取消 · 退出建造、围栏、土地工具或钓鱼\n\n点击土地，选择工具后点击目标地块。\n围栏模式点击起点，再点击终点铺设。\n绿色位置可用，红色位置不可用。\n\n点击世界中的建筑或动物查看详情。\n返回按钮关闭手册或普通面板。\n所有操作无需外接键盘或鼠标。");
                return;
            }
            string[] sections={"探索与手册","农耕与交互","建造与牧场","钓鱼与战斗"};
            for(int i=0;i<sections.Length;i++)
            {
                int section=i;
                var button=Button(content,sections[i],i*292,4,280,44,()=>{tutorialSection=section;Rebuild();});
                button.GetComponent<UnityEngine.UI.Image>().color=tutorialSection==i?new Color(.35f,.31f,.2f):new Color(.12f,.25f,.22f);
            }
            if(tutorialSection==0)
            {
                TutorialCard(0,"探索与视野","W / A / S / D 或方向键\n按画面方向移动，角色自动转向。\nShift 奔跑 · 空格跳跃 · X 砍树\nV 切换第一人称，按住鼠标左键拖动转身。\n\n鼠标滚轮\n拉近或拉远地图视野。\n\nC · 切换随行伙伴\n需要先招募伙伴。\n\n鼠标左键\n选中物件查看信息，E只操作该目标。");
                TutorialCard(590,"旅人手册","Tab · 打开手册；已打开时关闭\nI · 背包    Q · 任务\nL · 制作    M · 地图\n\n手册打开时，以上按键可切换页面；\n再次按当前页面的快捷键会关闭手册。\n\nEsc · 关闭手册或普通面板\n取消建造、围栏或钓鱼；战斗中不撤退。\n\n阅览手册时暂停角色移动。\n战斗中无法打开手册。");
            }
            else if(tutorialSection==1)
            {
                TutorialCard(0,"农田操作","E · 种植或收获\n走近空农田种下当前种子，成熟后收获。\n\nR · 浇水\n走近未浇水的作物；建造时改为旋转。\n\nT · 切换当前种子\n依次选择金萝卜、小麦、浆果。\n\nB · 购买当前种子\n需要在小屋或商人旁，且金币足够。");
                TutorialCard(590,"交互与休息","E · 与选中目标交互\n先点击物件查看信息并指定目标；\n未选中时与附近目标交互。\n\nF · 在小屋休息\n走到小屋门前，结束一天并推进生长。\n\n也可点击画面上的交互按钮。\n\n钓鱼中 E 改为收线；\n手册打开时先返回世界再进行操作。");
            }
            else if(tutorialSection==2)
            {
                TutorialCard(0,"H · 打开建造菜单","先关闭旅人手册，再按 H 进入独立建造菜单。\n\n1. 选择林场、采石场、学舍或营地。\n2. 移动鼠标选择地图上的位置。\n3. 按 R 旋转建筑，每次转动六十度。\n4. 鼠标左键确认放置。\n\n绿色表示可建，红色表示不可建。\n全部占地都须有效，且资源足够。\n可在地图的稳定陆地自由选址。\n\n鼠标右键或 Esc · 取消建造\nJ · 独立土地建造面板");
                TutorialCard(590,"围栏、牧场与拆除","在建造菜单中进入围栏与牧场。\n\n鼠标左键 · 放置围栏或牧场门\n每次点击放置一段，可连续点击铺设。\n鼠标右键或 Esc · 取消放置\n\n围住至少三格、关好门后，\n查看野猪详情并进行驯养。\n\n拆除模式：左键选择自建建筑或围栏。\n拆除建筑返还一半木材、石料和金币。\n点击建筑可查看占地、产出与拆除选项。");
            }
            else
            {
                TutorialCard(0,"钓鱼","走近湖岸钓点，按 E 开始钓鱼。\n\n等待鱼咬钩，观察收线光标。\nE 或点击收线 · 在绿色区域内收线\nEsc · 取消钓鱼\n\n三次成功即可捕获；三次失误会逃走。\n鱼饵会自动消耗并扩大收线窗口。\n\n钓鱼时不能移动或使用其他世界快捷键。");
                TutorialCard(590,"战斗","仅在遭遇战中使用数字行按键：\n\n1 · 普通攻击\n2 · 伙伴技能\n3 · 防御与回复\n4 · 撤退\n\n也可点击战斗面板中的对应按钮。\n\n战斗时无法移动或打开旅人手册。\nEsc 不会退出战斗，撤退请使用 4。");
            }
        }
        void TutorialCard(float x,string title,string description)
        {
            var panel=Box(content,"Tutorial card",x,66,570,496,cream);UITheme.Frame(panel);
            UITheme.Icon(panel,9,18,12,54,54);Text(panel,title,86,22,462,40,24,ink);
            Text(panel,description,22,78,526,396,19,ink);
        }
        sealed class Item
        {public string key,name,category,description;public int count,symbol;public Item(string k,string n,string cat,int value,int icon,string desc){key=k;name=n;category=cat;count=value;symbol=icon;description=desc;}}
        List<Item> Items(){var items=new List<Item>{
            new Item("starStones","星石","材料",game.State.weather.starStones,5,"开采晶石、开启远方宝箱或击败守卫获得，用于提升预知未来技能。"),
            new Item("carrots","胡萝卜","材料",A.homestead.carrots,3,"收获金萝卜时保留2份；小屋或商人处可6金币买3份。用于引诱、饲养兔子。"),new Item("planks","木板","材料",A.homestead.planks,1,"二级林场安排村民并开启加工后生产，用于三级资源点升级。"),new Item("bricks","石砖","材料",A.homestead.bricks,2,"二级采石场安排村民并开启加工后生产，用于三级资源点升级。"),
            new Item("wood","木材","材料",A.wood,1,"采集倒木获得，用于建造、围栏和制作。"),new Item("stone","石料","材料",A.stone,2,"开采矿石获得，用于建筑与城镇发展。"),
            new Item("berries","浆果","材料",A.berries,3,"采摘或种植获得，可招募伙伴、喂食野猪、制作鱼饵和料理。"),new Item("fish","鲜鱼","材料",A.fish,4,"在湖岸钓鱼获得，可交易、招募伙伴或制作补给。"),
            new Item("crystals","晶石","材料",A.crystals,5,"矿脉、宝箱与守卫奖励，用于研究和制作护符。"),new Item("fiber","纤维","材料",X.fiber,3,"采集野生草束或开启补给箱获得，用于远行装备。"),
            new Item("shards","遗迹碎片","材料",X.shards,5,"解读石碑或完美收线获得，星辉护符需要三份。"),new Item("truffles","松露","材料",A.truffles,3,"喂食牧场野猪后次日获得，可在动物详情中出售。"),
            new Item("seed0","金萝卜种子","种子",game.State.seeds,3,"两天成熟，收获后售出。选为当前种子，再回农田种植。"),new Item("seed1","小麦种子","种子",game.State.wheatSeeds,3,"三天成熟，每次收获售24金币。"),new Item("seed2","浆果种子","种子",game.State.berrySeeds,3,"四天成熟，可重复收获，产出金币和浆果。"),
            new Item("seed3","星辉南瓜种子","种子",game.State.starSeeds,14,"商队与秘境宝箱提供，五天成熟，收获60金币和村民口粮3。"),
            new Item("meal","暖心料理","补给",A.meals,0,"随时使用，体力回复12，最高300。小屋或营地可制作。"),new Item("bait","草编鱼饵","补给",X.bait,4,"抛竿时自动使用一份，扩大绿色收线窗口。"),new Item("camp","野营补给","补给",X.campSupplies,0,"在稳定陆地休整，体力回复15，不推进天数或刷新每日采集。"),
            };
            for(int i=0;i<EquipmentCatalog.Recipes.Length;i++){var r=EquipmentCatalog.Recipes[i];items.Add(new Item(r.key,r.name,"装备",EquipmentCatalog.Has(game,r.key)?1:0,0,r.effect+"\n\n图纸："+(EquipmentCatalog.State(game).blueprints[i]?"已学习":"未学习")+"\n持有工具即可自动用于操作，无需重复切换装备。"));}
            return items;
        }
        void Inventory()
        {
            string[] categories={"全部","材料","种子","补给","装备"};
            for(int i=0;i<categories.Length;i++){string category=categories[i];Chip(category,i*126,()=>{filter=category;inventoryPage=0;Rebuild();},filter==category);}
            var all=Items();var items=all.Where(x=>filter=="全部"||x.category==filter).ToList();
            if(!items.Any(x=>x.key==itemKey))itemKey=items[0].key;
            int pages=Mathf.Max(1,Mathf.CeilToInt(items.Count/16f));inventoryPage=Mathf.Clamp(inventoryPage,0,pages-1);
            for(int i=inventoryPage*16;i<Mathf.Min(items.Count,(inventoryPage+1)*16);i++)
            {
                var item=items[i];int slot=i-inventoryPage*16;float x=(slot%4)*155,y=60+(slot/4)*105;
                var button=Button(content,"",x,y,145,95,()=>{itemKey=item.key;Rebuild();});button.GetComponent<UnityEngine.UI.Image>().color=item.key==itemKey?new Color(.29f,.37f,.28f):new Color(.11f,.23f,.2f);
                UITheme.Icon(button.transform,UITheme.ItemIcon(item.key),8,5,50,50,true);
                var count=Text(button.transform,item.category=="装备"?(item.count>0?"已持有":"未拥有"):item.count.ToString(),55,10,78,36,item.category=="装备"?14:22,item.count>0?cream:new Color(.58f,.66f,.58f));count.alignment=TextAlignmentOptions.Right;
                Text(button.transform,item.name,10,57,125,30,17,cream);
            }
            Button(content,"上一页",0,514,116,38,()=>{inventoryPage--;Rebuild();},inventoryPage>0);Text(content,(inventoryPage+1)+" / "+pages,132,520,120,28,18,cream);Button(content,"下一页",280,514,116,38,()=>{inventoryPage++;Rebuild();},inventoryPage<pages-1);
            var selected=all.First(x=>x.key==itemKey);var detail=Details(selected.name,selected.category+" · "+(selected.category=="装备"?(selected.count>0?"已持有":"尚未制作"):"持有 "+selected.count),selected.description+"\n\n体力 "+game.State.energy+" / 300\n每5秒自动恢复1点体力\n当前种子："+FarmState.CropNames[game.State.selectedCrop]);
            if(selected.key=="meal")DetailAction(detail,"使用料理 · 回复体力12",0,()=>game.Adventure.Supply("eat"),selected.count>0&&game.State.energy<FarmState.MaxEnergy);
            else if(selected.key=="camp")DetailAction(detail,"野营休整 · 回复体力15",0,game.Expedition.UseCamp,selected.count>0&&game.State.energy<FarmState.MaxEnergy&&game.World.HexGrid.CanBuildOn(CurrentCell()));
            else if(selected.category=="种子")
            {
                int kind=int.Parse(selected.key.Substring(4));DetailAction(detail,kind==game.State.selectedCrop?"已选为当前种子":"选为当前种子",0,()=>game.SelectCrop(kind),kind!=game.State.selectedCrop);
                int cost=kind==0?5:kind==1?8:10;DetailAction(detail,"购买一份种子 · 金币"+cost,1,()=>{game.SelectCrop(kind);game.BuySeed();},(game.NearHouse||game.Adventure.Target?.kind==EncounterKind.Market)&&game.State.coins>=cost);
                Text(detail,"购买需在小屋或商人旁。",22,502,466,26,16,ink);
            }
            else if(selected.category=="装备")DetailAction(detail,"查看制作配方",0,()=>{equipmentView=true;blueprintView=false;equipmentSelection=EquipmentCatalog.Index(selected.key);equipmentPage=equipmentSelection/6;Open("craft");});
            if(selected.category=="材料")
            {
                if(selected.key=="truffles")DetailAction(detail,"出售全部松露 · 金币"+A.truffles*18,0,game.Animals.Sell,A.truffles>0);
                else DetailAction(detail,"查看制作配方",0,()=>Open("craft"));
                DetailAction(detail,"查看交易与订单",1,()=>Open("market"));
            }
            if(selected.key=="meal"||selected.key=="camp"||selected.key=="bait")DetailAction(detail,"查看补给配方",1,()=>{equipmentView=false;selectedRecipe=Array.IndexOf(RecipeKeys,selected.key);Open("craft");});
        }
        Vector2Int CurrentCell(){game.World.HexGrid.TryCell(game.World.Player.position,out var c);return c;}
        void Tasks()
        {
            Chip("寻兔委托",360,()=>OpenHomestead(true),false);
            Chip("村庄主线",0,()=>{expeditionTasks=false;selectedTask=Mathf.Min(A.quest,5);Rebuild();},!expeditionTasks);
            Chip("星图远征",180,()=>{expeditionTasks=true;selectedTask=Mathf.Min(X.quest,2);Rebuild();},expeditionTasks);
            string[] names=expeditionTasks?new[]{"远征入门","湿地行者","星图重现"}:new[]{"村长的委托","聚落的伙伴","失落的星辉","金穗拓荒","越过白岩","星野纪元"};
            int stage=expeditionTasks?X.quest:A.quest;selectedTask=Mathf.Clamp(selectedTask,0,names.Length-1);
            for(int i=0;i<names.Length;i++){int id=i;Row(names[i],i<stage?"已完成":i==stage?"进行中":"尚未解锁",i,()=>{selectedTask=id;Rebuild();},selectedTask==i);}
            string[] rewards=expeditionTasks?new[]{"金币25 · 纤维4 · 鱼饵3","晶石2 · 知识6 · 碎片1","金币150 · 知识15"}:new[]{"金币30 · 浆果3 · 木材5","金币40 · 石料5 · 知识4","金币80 · 知识8","金币60 · 浆果5 · 知识4","金币80 · 晶石3 · 知识8","金币200 · 知识20"};
            float progress=selectedTask<stage?1:TaskProgress(selectedTask);string description=(expeditionTasks?ExpeditionContent.Quests[selectedTask]:AdventureContent.Quests[selectedTask])+"\n\n进度 "+Mathf.RoundToInt(progress*100)+"%\n"+TaskCounts(selectedTask)+"\n\n奖励："+rewards[selectedTask]+"\n\n完成后回晨风村村长身边领取。";
            var detail=Details(names[selectedTask],selectedTask<stage?"已完成":selectedTask==stage?"当前委托":"后续委托",description);
            Progress(detail,progress);
            bool near=game.Adventure.Target?.kind==EncounterKind.Elder;
            DetailAction(detail,selectedTask<stage?"奖励已领取":progress<1?"目标尚未完成":near?"领取任务奖励":"请回村长身边领取",0,()=>{if(expeditionTasks)game.Expedition.ClaimQuest();else game.Adventure.ClaimMainQuest();},selectedTask==stage&&progress>=1&&near);
            DetailAction(detail,"返回晨风村",1,()=>game.Adventure.Travel(0));
        }
        float TaskProgress(int id)
        {
            if(expeditionTasks){if(id==0)return (Mathf.Clamp01(X.CacheCount/2f)+Mathf.Clamp01(X.fishCaught/3f))*.5f;if(id==1)return (Mathf.Clamp01(X.RuneCount/3f)+(X.marshBoots?1:0))*.5f;return (X.RuneCount/8f+(X.starAmulet?1:0)+(A.ruinsWon?1:0)+(A.mountainWon?1:0)+(A.marshWon?1:0))/5;}
            if(id==0)return Mathf.Clamp01(game.State.harvests/3f);if(id==1)return ((A.CompanionCount>0?1:0)+(A.workshop?1:0))*.5f;if(id==2)return ((A.ruinsWon?1:0)+(A.irrigation?1:0))*.5f;if(id==3)return (Mathf.Clamp01(game.State.harvests/8f)+(A.expandedFarm?1:0)+(A.mill?1:0))/3;if(id==4)return (A.discovered.Count(x=>x)/8f+(A.mountainWon?1:0))*.5f;return (A.CompanionCount/3f+(A.observatory?1:0)+(A.lore?1:0))/3;
        }
        string TaskCounts(int id)
        {
            if(expeditionTasks)return id==0?"补给箱 "+X.CacheCount+" / 2 · 钓获 "+X.fishCaught+" / 3":id==1?"石碑 "+X.RuneCount+" / 3 · 长靴 "+(X.marshBoots?"已制作":"未制作"):"石碑 "+X.RuneCount+" / 8 · 三地守卫 "+((A.ruinsWon?1:0)+(A.mountainWon?1:0)+(A.marshWon?1:0))+" / 3";
            return "收获 "+game.State.harvests+" 次 · 伙伴 "+A.CompanionCount+" / 3\n发现区域 "+A.discovered.Count(x=>x)+" / 8";
        }
        void Crafting()
        {
            Chip("装备制作",0,()=>{equipmentView=true;blueprintView=false;Rebuild();},equipmentView&&!blueprintView);
            Chip("装备图纸",180,()=>{equipmentView=true;blueprintView=true;Rebuild();},equipmentView&&blueprintView);
            Chip("料理与补给",360,()=>{equipmentView=false;Rebuild();},!equipmentView);
            if(equipmentView){EquipmentPage();return;}
            
            int row=0;foreach(int id in new[]{0,4,5})Row(RecipeNames[id],RecipeCosts[id],row++,()=>{selectedRecipe=id;Rebuild();},selectedRecipe==id);
            if(selectedRecipe!=0&&selectedRecipe!=4&&selectedRecipe!=5)selectedRecipe=0;int recipe=selectedRecipe;
            string[] effects={"每次制作鱼饵3。钓鱼抛竿时自动使用一份，扩大绿色收线窗口。","永久扩大钓鱼收线窗口，制作后自动生效。","沼泽移动速度提升至80%，雪地85%。不能进入水面。","伙伴战斗最大体力+10，攻击与技能伤害+3。","制作一份野营补给，在稳定陆地使用，体力回复15，不推进天数。","制作一份料理，体力回复12，最高300。需在小屋或营地制作。"};
            string condition=recipe==1||recipe==2?"前置：工坊 "+(A.workshop?"已建造":"未建造"):recipe==3?"前置：书院 "+(A.library?"已建造":"未建造"):"无需科技前置";
            bool location=recipe==5?game.NearHouse||game.Adventure.Target?.kind==EncounterKind.Camp:game.Expedition.CanCraftHere;
            var detail=Details(RecipeNames[recipe],RecipeOwned(recipe)?"已装备 · 永久生效":"制作配方",effects[recipe]+"\n\n材料："+RecipeCosts[recipe]+"\n"+condition+"\n\n地点："+(location?"当前可制作":"请走近制作地点")+"\n纤维 "+X.fiber+" · 碎片 "+X.shards+"\n浆果 "+A.berries+" · 鲜鱼 "+A.fish);
            DetailAction(detail,RecipeOwned(recipe)?"已经制作":!location?"请走近制作地点":CanAfford(recipe)?"制作物品":"材料或前置不足",0,()=>{if(recipe==5)game.Adventure.Supply("cook");else game.Expedition.Craft(RecipeKeys[recipe]);},!RecipeOwned(recipe)&&location&&CanAfford(recipe));
            DetailAction(detail,"查看背包",1,()=>Open("inventory"));
        }
        bool RecipeOwned(int id)=>id==1?X.fishingRod:id==2?X.marshBoots:id==3&&X.starAmulet;
        bool CanAfford(int id)
        {switch(id){case 0:return X.fiber>=2&&A.berries>=1;case 1:return A.workshop&&A.wood>=8&&X.fiber>=4&&A.science>=2;case 2:return A.workshop&&X.fiber>=8&&A.wood>=4&&game.State.coins>=20;case 3:return A.library&&X.shards>=3&&A.crystals>=2&&A.science>=6;case 4:return X.fiber>=4&&A.berries>=4&&A.fish>=1;default:return A.berries>=3&&A.fish>=1;}}
        void EquipmentPage()
        {
            var recipes=EquipmentCatalog.Recipes;var state=EquipmentCatalog.State(game);
            equipmentSelection=Mathf.Clamp(equipmentSelection,0,recipes.Length-1);equipmentPage=Mathf.Clamp(equipmentPage,0,(recipes.Length-1)/6);
            for(int i=equipmentPage*6;i<Mathf.Min(recipes.Length,equipmentPage*6+6);i++)
            {int id=i;var r=recipes[i];string status=state.owned[i]?"已持有":state.blueprints[i]?"已学图纸 · 待制作":"图纸未学习 · 金币"+r.plan;Row(r.name,status,i-equipmentPage*6,()=>{equipmentSelection=id;Rebuild();},equipmentSelection==i,74);}
            Button(content,"上一页",0,514,116,38,()=>{equipmentPage--;equipmentSelection=equipmentPage*6;Rebuild();},equipmentPage>0);Text(content,(equipmentPage+1)+" / 2",140,520,100,28,18,cream);Button(content,"下一页",280,514,116,38,()=>{equipmentPage++;equipmentSelection=equipmentPage*6;Rebuild();},equipmentPage<1);
            int chosen=equipmentSelection;var recipe=recipes[chosen];string block=EquipmentCatalog.Block(game,chosen);
            var detail=Details(recipe.name,blueprintView?"装备图纸":"工具与装备",recipe.effect+"\n图纸："+(state.blueprints[chosen]?"已学习":"未学习 · 金币"+recipe.plan)+"\n前置："+(recipe.tech==1?"工坊":recipe.tech==2?"书院":"无需建筑")+"\n制作："+recipe.Cost+"\n持有：木"+A.wood+" 石"+A.stone+" 纤维"+X.fiber+"\n"+(blueprintView?"图纸在小屋、商人或村长旁学习，学习后永久保存。":"持有即可自动使用。缺少工具时不会扣除体力和材料。"));
            if(blueprintView)DetailAction(detail,state.blueprints[chosen]?"图纸已学习":!EquipmentCatalog.PlanLocation(game)?"请走近小屋、商人或村长":"学习图纸 · 金币"+recipe.plan,0,()=>EquipmentCatalog.Learn(game,chosen),!state.blueprints[chosen]&&EquipmentCatalog.PlanLocation(game)&&game.State.coins>=recipe.plan);
            else DetailAction(detail,block==""?"制作装备":block,0,()=>EquipmentCatalog.Craft(game,chosen),block=="");
            DetailAction(detail,blueprintView?"查看装备制作":"查看此装备图纸",1,()=>{blueprintView=!blueprintView;Rebuild();});
        }
        public void OpenWorkshop(){game.Construction.Choose(6);}
        void Town()
        {
            Chip("牧场与经营",316,()=>OpenHomestead(true),false);
            Chip("营地与守卫",474,OpenCamp,false);
            Chip("城镇项目",0,()=>Open("town"),true);Chip("自建建筑",158,()=>Open("buildings"),false);
            for(int i=0;i<TechKeys.Length;i++){int id=i;Row(TechNames[i],TechOwned(i)?"已完成":TechCosts[i],i,()=>{selectedTech=id;Rebuild();},selectedTech==i,58);}
            string[] effects={"每天木材+6，解锁书院、农场扩建和采集工具。","每天知识+6，解锁灌溉与观星台。","每天照料前6块农田，农场扩建后照料12块。","伙伴战斗伤害增加3。","解锁南侧12块农田，总共24块。","每天金币+18。","采集木材、石料每次额外获得2份。","每天知识+12，需书院和白岩守卫胜利。"};
            string[] prerequisite={"无前置","工坊","书院","灌溉技术","工坊","工坊","工坊","书院、击败白岩守卫"};int tech=selectedTech;
            bool near=game.Adventure.Target?.kind==EncounterKind.Town;bool physical=tech==0||tech==1||tech==5||tech==7;
            var detail=Details(TechNames[tech],physical?"自由选址建筑":TechOwned(tech)?"已完成":"城镇项目",effects[tech]+"\n\n费用："+TechCosts[tech]+"\n前置："+prerequisite[tech]+"\n\n木材 "+A.wood+" · 石料 "+A.stone+"\n金币 "+game.State.coins+" · 知识 "+A.science+(physical?"\n\n点击后在地图自由选址，建成后解锁科技并逐座产出。":"\n\n研究需站在城镇议事牌旁。"));
            DetailAction(detail,physical?"选择位置建造"+TechNames[tech]:TechOwned(tech)?"项目已完成":near?"开始研究":"请走近城镇议事牌",0,()=>game.Adventure.BuildOrResearch(TechKeys[tech]),physical||!TechOwned(tech)&&near);
            DetailAction(detail,"进入自由建造",1,game.Construction.Open);
        }
        bool TechOwned(int id)=>id==0?A.workshop:id==1?A.library:id==2?A.irrigation:id==3?A.lore:id==4?A.expandedFarm:id==5?A.mill:id==6?A.toolLevel>0:A.observatory;
        void Buildings()
        {
            int pages=Mathf.Max(1,Mathf.CeilToInt(A.buildings.Count/8f));buildingPage=Mathf.Clamp(buildingPage,0,pages-1);
            var selected=A.buildings.FirstOrDefault(b=>b.column+":"+b.row==buildingKey);if(selected==null&&A.buildings.Count>0){selected=A.buildings[0];buildingKey=selected.column+":"+selected.row;}
            int start=buildingPage*8;
            for(int i=start;i<Mathf.Min(start+8,A.buildings.Count);i++){var b=A.buildings[i];string key=b.column+":"+b.row;Row(HexConstruction.Names[b.kind],"坐标 "+key+" · "+BuildingFootprints.Cells(b).Count+"格",i-start,()=>{buildingKey=key;confirmDemolition=false;Rebuild();},key==buildingKey,56);}
            Button(content,"上一页",0,528,116,38,()=>{buildingPage--;Rebuild();},buildingPage>0);Text(content,(buildingPage+1)+" / "+pages,128,536,140,28,17,cream);Button(content,"下一页",286,528,116,38,()=>{buildingPage++;Rebuild();},buildingPage<pages-1);
            if(selected==null){var empty=Details("尚无自建建筑","聚落档案","在地图上自由建造林场、采石场、学舍或营地。围栏沿六边形边缘布置。");DetailAction(empty,"开始建造",0,game.Construction.Open);return;}
            var chosen=selected;string output=BuildingEconomy.Description(chosen.kind);
            var detail=Details(HexConstruction.Names[chosen.kind],"占地 "+BuildingFootprints.Cells(chosen).Count+" 格",output+"\n\n坐标 ["+chosen.column+","+chosen.row+"]\n朝向 "+chosen.rotation*60+"度\n建造："+(chosen.builtDay>0?"第"+chosen.builtDay+"天":"旧存档建筑")+"\n\n拆除返还一半木材、石料和金币。"+(confirmDemolition?"\n\n确认拆除这座建筑？":""));
            DetailAction(detail,confirmDemolition?"确认拆除并返还资源":"拆除这座建筑",0,()=>{if(confirmDemolition){game.Construction.Demolish(new Vector2Int(chosen.column,chosen.row));confirmDemolition=false;buildingKey="";}else{confirmDemolition=true;Rebuild();}});
            DetailAction(detail,!confirmDemolition?"管理这座建筑":"取消拆除",1,()=>{if(!confirmDemolition){OpenBuilding(chosen);return;}if(confirmDemolition){confirmDemolition=false;Rebuild();}else game.Construction.Open();});
        }
        void Companions()
        {
            Text(content,"选择伙伴，查看能力和招募条件",0,6,620,34,19,cream);
            selectedCompanion=Mathf.Clamp(selectedCompanion,0,2);
            for(int i=0;i<3;i++){int id=i;Row(AdventureContent.Names[i],A.companions[i]?(A.activeCompanion==i?"正在随行":"已招募"):"尚未招募",i,()=>{selectedCompanion=id;Rebuild();},selectedCompanion==i,110);}
            int species=selectedCompanion;string[] conditions={"在青苔森林，用浆果3招募。","在镜湖岸，用鲜鱼2、浆果2招募。","击败遗迹守卫，再用晶石1招募。"};
            var detail=Details(AdventureContent.Names[species],A.companions[species]?"已成为伙伴":"野外伙伴",AdventureContent.Roles[species]+"\n\n"+conditions[species]+"\n\n草系技能削弱守卫反击；水系技能回复伙伴体力；火系技能克制幽光守卫。");
            DetailAction(detail,A.companions[species]&&A.activeCompanion==species?"正在随行":"设为随行伙伴",0,()=>game.Adventure.SelectCompanion(species),A.companions[species]&&A.activeCompanion!=species);
            DetailAction(detail,"查看所在区域",1,()=>{selectedRegion=species+1;Open("map");});
        }
        void Map()
        {
            var map=Box(content,"Continent chart",0,8,620,510,new Color(.2f,.37f,.36f));
            var graphic=new GameObject("Hex terrain chart",typeof(RectTransform),typeof(CanvasRenderer),typeof(TerrainMapGraphic)).GetComponent<TerrainMapGraphic>();graphic.transform.SetParent(map,false);graphic.rectTransform.anchorMin=Vector2.zero;graphic.rectTransform.anchorMax=Vector2.one;graphic.rectTransform.offsetMin=graphic.rectTransform.offsetMax=Vector2.zero;graphic.grid=game.World.HexGrid;graphic.raycastTarget=false;
            for(int i=0;i<8;i++)
            {
                int region=i;var p=game.World.Adventure.RegionCenters[i];var button=Button(map,AdventureContent.Regions[i]+"\n"+(A.discovered[i]?"已发现":"尚未发现"),0,0,78,42,()=>{selectedRegion=region;Rebuild();});
                var rect=button.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(p.x/(HexWorldGrid.HalfWidth*2)*620,p.z/(HexWorldGrid.HalfDepth*2)*510);button.GetComponent<UnityEngine.UI.Image>().color=selectedRegion==i?new Color(.38f,.32f,.18f):A.discovered[i]?green:new Color(.23f,.28f,.25f);
                button.GetComponentInChildren<TextMeshProUGUI>().fontSize=10;
            }
            FrontierMapPins(map);
            var den=ExpandedWilderness.DenPosition;var denButton=Button(map,"北岭狼窟",0,0,90,32,()=>game.ShowMessage("狼窟在东北山岭，坐标 58，39。向东北方向探索可抵达。"));var denRect=denButton.GetComponent<RectTransform>();denRect.anchorMin=denRect.anchorMax=denRect.pivot=new Vector2(.5f,.5f);denRect.anchoredPosition=new Vector2(den.x/(HexWorldGrid.HalfWidth*2)*620,den.z/(HexWorldGrid.HalfDepth*2)*510);denButton.GetComponentInChildren<TextMeshProUGUI>().fontSize=13;
            var player=game.World.Player.position;var marker=Box(map,"Player map marker",0,0,12,12,gold);marker.anchorMin=marker.anchorMax=marker.pivot=new Vector2(.5f,.5f);marker.anchoredPosition=new Vector2(player.x/(HexWorldGrid.HalfWidth*2)*620,player.z/(HexWorldGrid.HalfDepth*2)*510);
            Text(content,"草地 · 森林 · 沼泽 · 沙地 · 岩地 · 雪地 · 道路 · 水面",0,538,630,30,16,cream);
            string[] descriptions={"农场、小屋、村长与城镇议事牌。","倒木、浆果与森林伙伴。森林移动80%。","湖岸钓点与潮汐伙伴。水面禁止通行。","石碑、遗迹守卫与焰晶狐。","商人、营地和荒野补给。","矿石、晶石和白岩守卫。岩地70%，雪地65%。","湿地采集、水洼钓点与幽光守卫。沼泽移动45%。","远行商人、营地与旅人石碑。"};selectedRegion=Mathf.Clamp(selectedRegion,0,7);int regionId=selectedRegion;
            var detail=Details(AdventureContent.Regions[regionId],A.discovered[regionId]?"已发现 · 可快速旅行":"尚未发现",descriptions[regionId]+"\n\n补给箱："+(X.caches[regionId]?"已开启":"未开启")+"\n石碑："+(X.runes[regionId]?"已解读":"未解读")+"\n\n先步行发现区域，再解锁旅行。\n"+game.Expedition.NextLandmark);
            DetailAction(detail,"前往这个区域",0,()=>game.Adventure.Travel(regionId),A.discovered[regionId]);
            DetailAction(detail,"查看探索任务",1,()=>{expeditionTasks=true;selectedTask=Mathf.Min(X.quest,2);Open("tasks");});
        }
        void Market()
        {
            Text(content,"种子商店 · 资源收购 · 每日订单",0,6,620,34,19,cream);
            string[] names={"金萝卜种子","小麦种子","浆果种子","出售木材","出售鲜鱼","每日贸易订单"};
            string[] offers={"购买价5金币","购买价8金币","购买价10金币","木材5 → 金币12","鲜鱼2 → 金币10",A.deliveredDay==game.State.day?"今日订单已完成":"木6 石4 鲜鱼2 → 金35 知2"};
            for(int i=0;i<6;i++){int id=i;Row(names[i],offers[i],i,()=>{selectedTrade=id;Rebuild();},selectedTrade==i);}
            int trade=selectedTrade;bool merchant=game.Adventure.Target?.kind==EncounterKind.Market;bool seedShop=merchant||game.NearHouse;
            string description=trade<3?"在小屋门前或远行商人旁购买一份种子。\n\n购买后收入背包，再选择为当前种子。":trade==3?"商人收购木材5，支付金币12。":trade==4?"商人收购鲜鱼2，支付金币10。":"订单需求：木材6、石料4、鲜鱼2。\n奖励：金币35、知识2，每天限一次。";
            var detail=Details(names[trade],offers[trade],description+"\n\n当前范围："+(merchant?"附近有商人":game.NearHouse?"小屋种子商店":"需走近交易地点")+"\n金币 "+game.State.coins+" · 木材 "+A.wood+"\n石料 "+A.stone+" · 鲜鱼 "+A.fish+"\n\n资源交易与订单需在商人旁。\n商人在金穗平原和远行驿站。");
            int price=trade==0?5:trade==1?8:10;
            bool allowed=trade<3?seedShop&&game.State.coins>=price:trade==3?merchant&&A.wood>=5:trade==4?merchant&&A.fish>=2:merchant&&A.deliveredDay!=game.State.day&&A.wood>=6&&A.stone>=4&&A.fish>=2;
            DetailAction(detail,trade<3?"购买一份种子":trade==5?"提交每日订单":"确认出售",0,()=>{if(trade<3){game.SelectCrop(trade);game.BuySeed();}else game.Adventure.Supply(trade==3?"wood":trade==4?"fish":"delivery");},allowed);
            DetailAction(detail,"查看背包",1,()=>Open("inventory"));
        }
        RectTransform Details(string title,string subtitle,string description)
        {
            var panel=Box(content,"Selected detail",654,8,506,554,cream);UITheme.Frame(panel);
            DetailIcon(panel,title);Text(panel,title,96,20,386,48,25,ink);Text(panel,subtitle,22,82,462,30,16,new Color(.46f,.38f,.22f));Box(panel,"Detail divider",22,118,462,1,new Color(.63f,.53f,.30f,.40f));Text(panel,description,22,132,462,264,18,ink);return panel;
        }
        void Progress(Transform parent,float value)
        {var track=Box(parent,"Task progress",22,395,462,8,new Color(.64f,.65f,.51f));Box(track,"Task progress fill",0,0,462*Mathf.Clamp01(value),8,new Color(.36f,.54f,.32f));}
        void DetailAction(Transform parent,string label,int row,UnityEngine.Events.UnityAction callback,bool enabled=true)=>Button(parent,label,22,420+row*60,462,46,callback,enabled);
        void Chip(string label,float x,UnityEngine.Events.UnityAction callback,bool selected)
        {var button=Button(content,label,x,4,118,38,callback);button.GetComponent<UnityEngine.UI.Image>().color=selected?new Color(.36f,.32f,.2f):new Color(.12f,.25f,.22f);}
        void Row(string title,string description,int row,UnityEngine.Events.UnityAction callback,bool selected,float spacing=78)
        {
            var button=Button(content,"",0,60+row*spacing,620,spacing-8,callback);button.name="Row - "+title;button.GetComponent<UnityEngine.UI.Image>().color=selected?new Color(.23f,.34f,.25f,.96f):new Color(.075f,.18f,.155f,.78f);
            float size=Mathf.Min(50,spacing-18);RowIcon(button.transform,title,description,10,(spacing-8-size)/2,size);
            if(selected)Box(button.transform,"Selection accent",0,8,3,spacing-24,gold);
            Text(button.transform,title,76,spacing<78?3:7,522,spacing<78?30:36,spacing<78?18:20,cream);
            Text(button.transform,description,76,spacing<78?28:44,522,spacing<78?22:26,spacing<78?13:15,new Color(.74f,.82f,.73f));
        }
        RectTransform Box(Transform parent,string name,float x,float y,float width,float height,Color color,bool raycast=false)
        {var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);var image=go.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=raycast;if(width>12&&height>12)UITheme.Round(image);return rect;}
        TextMeshProUGUI Text(Transform parent,string text,float x,float y,float width,float height,float point,Color color)
        {var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);var label=go.GetComponent<TextMeshProUGUI>();label.font=font;label.fontSize=point;label.text=text;label.color=color;label.raycastTarget=false;label.enableAutoSizing=false;label.overflowMode=TextOverflowModes.Ellipsis;return label;}
        UnityEngine.UI.Button Button(Transform parent,string text,float x,float y,float width,float height,UnityEngine.Events.UnityAction callback,bool enabled=true)
        {var rect=Box(parent,text,x,y,width,height,new Color(.13f,.28f,.23f),true);UITheme.Frame(rect);var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=rect.GetComponent<UnityEngine.UI.Image>();button.onClick.AddListener(callback);button.interactable=enabled;UITheme.Button(button);var caption=Text(rect,text,8,4,width-16,height-8,18,cream);caption.alignment=TextAlignmentOptions.Center;return button;}
        void Icon(Transform parent,float x,float y,float size,int symbol,Color color)
        {var go=new GameObject("Item emblem",typeof(RectTransform),typeof(CanvasRenderer),typeof(HudOrnament));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=Vector2.one*size;var icon=go.GetComponent<HudOrnament>();icon.symbol=symbol;icon.color=color;icon.raycastTarget=false;}
    }
}
