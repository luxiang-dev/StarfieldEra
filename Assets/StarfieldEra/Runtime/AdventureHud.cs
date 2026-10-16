using TMPro;
using UnityEngine;

namespace StarfieldEra
{
    public sealed class AdventureHud : MonoBehaviour
    {
        AdventureGame game;
        TMP_FontAsset font;
        RectTransform panel;
        public bool IsContextOpen => panel && panel.gameObject.activeSelf;
        TextMeshProUGUI title,body;
        Transform actions;
        RectTransform fishingTrack,fishingZone,fishingCursor;
        string mode="";
        int previousRevision=-1,previousDay=-1;
        public void Create(AdventureGame adventure,TMP_FontAsset asset)
        {
            game=adventure;font=asset;game.Hud=this;
            var go=new GameObject("Adventure panel",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            panel=go.GetComponent<RectTransform>();panel.SetParent(transform,false);
            panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.one;
            panel.anchoredPosition=new Vector2(-24,-130);panel.sizeDelta=new Vector2(450,550);
            go.GetComponent<UnityEngine.UI.Image>().color=new Color(.06f,.16f,.15f,.98f);
            HudOrnament.Border(panel);
            title=Text("Panel title",panel,new Vector2(24,-20),new Vector2(390,40),27,new Color(1,.86f,.58f));
            body=Text("Panel content",panel,new Vector2(24,-80),new Vector2(400,210),17,new Color(.93f,.95f,.87f));
            actions=new GameObject("Panel actions",typeof(RectTransform)).transform;actions.SetParent(panel,false);
            var actionRect=(RectTransform)actions;actionRect.anchorMin=actionRect.anchorMax=actionRect.pivot=new Vector2(0,1);
            actionRect.anchoredPosition=Vector2.zero;actionRect.sizeDelta=new Vector2(450,550);
            AddButton(panel,"关闭",new Vector2(322,-492),new Vector2(100,38),()=>{if(mode=="building"||mode=="confirm-demolition")game.Game.Construction.ClearInspection();Hide();});
            CreateFishingTrack();
            panel.gameObject.SetActive(false);
        }
        public void Show(string next)
        {
            if(game.InBattle&&next!="battle")return;
            if(game.Game.Management&&ManagementHud.Handles(next)){game.Game.Management.OpenFromMode(next);return;}
            game.Game.Management?.Close();
            if(game.Game.Expedition.Fishing&&next!="fishing")game.Game.Expedition.CancelFishing();
            if(panel.gameObject.activeSelf&&mode==next&&next!="battle"){Hide();return;}
            mode=next;panel.gameObject.SetActive(true);Rebuild();
        }
        public void Hide(){if(game.InBattle)return;if(game.Game.Expedition.Fishing){game.Game.Expedition.CancelFishing();return;}HideContextOnly();game.Game.Management?.Close();}
        public void HideContextOnly(){panel.gameObject.SetActive(false);mode="";}
        public void Refresh()
        {
            if(!panel.gameObject.activeSelf)return;
            if(previousRevision==game.Revision&&previousDay==game.Game.State.day)return;
            Rebuild();
        }
        void Rebuild()
        {
            previousRevision=game.Revision;previousDay=game.Game.State.day;
            for(int i=actions.childCount-1;i>=0;i--) {actions.GetChild(i).gameObject.SetActive(false);Destroy(actions.GetChild(i).gameObject);}
            var s=game.State;
            fishingTrack.gameObject.SetActive(mode=="fishing");
            if(mode=="fishing")
            {
                title.text="湖岸垂钓 · 收线时机";body.text=game.Game.Expedition.FishingText;
                Action("收线",0,game.Game.Expedition.Reel);
                Action("收起鱼竿",1,game.Game.Expedition.CancelFishing);
            }
            else if(mode=="expedition")
            {
                var e=s.expedition;title.text="远征日志 · 星图重现";
                body.text=ExpeditionContent.Quests[e.quest]+"\n\n补给箱 "+e.CacheCount+" / 8   石碑 "+e.RuneCount+" / 8\n累计钓获 "+e.fishCaught+" 条   遗迹碎片 "+e.shards+"\n纤维 "+e.fiber+"   鱼饵 "+e.bait+"   野营补给 "+e.campSupplies+"\n\n"+game.Game.Expedition.NextLandmark;
                Action("制作装备、鱼饵与补给",0,()=>Show("craft"));
                Action("使用野营补给 · 回复体力15",1,game.Game.Expedition.UseCamp,e.campSupplies>0&&game.Game.State.energy<FarmState.MaxEnergy);
                Action("向村长领取远征奖励",2,game.Game.Expedition.ClaimQuest,e.quest<3);
                Action("打开旅途地图",3,()=>Show("map"));
            }
            else if(mode=="craft"||mode=="craft2")
            {
                var e=s.expedition;title.text="制作台 · 远行装备";
                body.text="纤维 "+e.fiber+"   遗迹碎片 "+e.shards+"\n木材 "+s.wood+"   浆果 "+s.berries+"   鲜鱼 "+s.fish+"\n晶石 "+s.crystals+"   知识 "+s.science+"\n\n在小屋、营地或已建工坊旁制作。\n鱼竿与长靴需先建工坊；护符需书院。\n长靴提升沼泽与雪地速度；护符体力+10、伤害+3。";
                if(mode=="craft")
                {
                    Action("鱼饵3 · 纤维2 浆果1",0,()=>game.Game.Expedition.Craft("bait"));
                    Action(e.fishingRod?"精制鱼竿 · 已装备":"精制鱼竿 · 木8 纤维4 知2",1,()=>game.Game.Expedition.Craft("rod"),!e.fishingRod);
                    Action(e.marshBoots?"湿地长靴 · 已装备":"湿地长靴 · 纤维8 木4 金20",2,()=>game.Game.Expedition.Craft("boots"),!e.marshBoots);
                    Action("返回远征日志",3,()=>Show("expedition"));
                }
                else
                {
                    Action(e.starAmulet?"星辉护符 · 已装备":"星辉护符 · 碎片3 晶2 知6",0,()=>game.Game.Expedition.Craft("amulet"),!e.starAmulet);
                    Action("野营补给 · 纤维4 浆果4 鲜鱼1",1,()=>game.Game.Expedition.Craft("camp"));
                    Action("使用野营补给 · 回复体力15",2,game.Game.Expedition.UseCamp,e.campSupplies>0&&game.Game.State.energy<FarmState.MaxEnergy);
                    Action("返回远征日志",3,()=>Show("expedition"));
                }
                Page(mode=="craft"?"下一页 · 护符与野营":"上一页 · 鱼竿与长靴",()=>Show(mode=="craft"?"craft2":"craft"));
            }
            else if(mode=="landmark")
            {
                var item=game.Game.Expedition.Inspected;if(item==null){Hide();return;}title.text=item.name;body.text=game.Game.Expedition.LandmarkDescription;
                bool claimed=item.kind==EncounterKind.Treasure?s.expedition.caches[item.id]:s.expedition.runes[item.id];
                Action(claimed?"已经探索":"走近领取或解读",0,game.Game.Expedition.ClaimLandmark,!claimed);
                Action("查看远征日志",1,()=>Show("expedition"));
                Action("查看装备制作",2,()=>Show("craft"));
            }
            else if(mode=="battle")
            {
                title.text=game.EnemyName+" · 回合 "+game.Turn;
                body.text=AdventureContent.Names[s.activeCompanion]+" 体力 "+game.AllyHP+" / "+game.AllyMaxHP+"\n\n"+game.EnemyName+" 体力 "+game.EnemyHP+" / "+game.EnemyMaxHP+"\n\n"+game.BattleLog+"\n\n技能冷却："+game.Cooldown+" 回合\n守卫每三回合会发动重击，适时防御。";
                Action("普通攻击",0,()=>game.BattleAction(0));
                Action("伙伴技能",1,()=>game.BattleAction(1));
                Action("防御与回复",2,()=>game.BattleAction(2));
                Action("撤退",3,()=>game.BattleAction(3));
            }
            else if(mode=="town" || mode=="town2")
            {
                title.text="晨风村 · 建设与科技";
                body.text="木材 "+s.wood+"   石料 "+s.stone+"   知识 "+s.science+"\n\n工坊每天木+2，书院知识+2。\n农场扩建解锁南侧12块农田；磨坊每天金+6。\n工具让采集木材和石料每次多两份。\n观星台每天知识+4，需先击败白岩守卫。\n\n建设须站在城镇议事牌旁。";
                if(mode=="town")
                {
                    Action(s.workshop?"工坊 · 已建造":"工坊 · 木6 石4 金15",0,()=>game.BuildOrResearch("workshop"),!s.workshop);
                    Action(s.library?"书院 · 已建造":"书院 · 木8 石6 金20",1,()=>game.BuildOrResearch("library"),!s.library);
                    Action(s.irrigation?"灌溉 · 已研究":"灌溉 · 知4 木4 石4",2,()=>game.BuildOrResearch("irrigation"),!s.irrigation);
                    Action(s.lore?"星辉学 · 已研究":"星辉学 · 知6 晶1",3,()=>game.BuildOrResearch("lore"),!s.lore);
                }
                else
                {
                    Action(s.expandedFarm?"农场 · 已扩建":"扩建农场 · 木8 石6 金30",0,()=>game.BuildOrResearch("farm"),!s.expandedFarm);
                    Action(s.mill?"磨坊 · 已建造":"磨坊 · 木10 石8 金30",1,()=>game.BuildOrResearch("mill"),!s.mill);
                    Action(s.toolLevel>0?"工具 · 已升级":"工具 · 木6 石5 金25",2,()=>game.BuildOrResearch("tools"),s.toolLevel==0);
                    Action(s.observatory?"观星台 · 已建造":"观星台 · 木18 石14 晶3 知20",3,()=>game.BuildOrResearch("observatory"),!s.observatory);
                }
                Page(mode=="town"?"下一页 · 开垦与观星":"上一页 · 村庄基础",()=>Show(mode=="town"?"town2":"town"));
            }
            else if(mode=="map" || mode=="map2")
            {
                title.text="星野大陆 · 旅途地图";
                body.text=mode=="map"?"中央：晨风村、24块农田与议事牌。\n东侧：青苔森林，木材与浆果。\n西南：镜湖岸，钓鱼与水系伙伴。\n东北：星辉遗迹，晶石与守卫。\n\n先步行发现，再解锁快速旅行。":"东南：金穗平原，营地与商人。\n东北远方：白岩山地，矿石与新守卫。\n西北：幽光湿地，浆果、钓鱼与守卫。\n西南远方：远行驿站，贸易与营地。\n\n沼泽移动45%，道路120%；水面不可通行。";
                int start=mode=="map"?0:4;
                for(int i=0;i<4;i++){int region=start+i;Action(AdventureContent.Regions[region]+(s.discovered[region]?" · 旅行":" · 尚未发现"),i,()=>game.Travel(region),s.discovered[region]);}
                Page(mode=="map"?"下一页 · 四个远方区域":"上一页 · 村庄周边",()=>Show(mode=="map"?"map2":"map"));
            }
            else if(mode=="build")
            {
                title.text="六边形建造 · 自由选址";
                body.text="木材 "+s.wood+"   石料 "+s.stone+"   金币 "+game.Game.State.coins+"\n\n林场和采石场需派驻村民；学舍每天知+1。\n林场、采石场7格；学舍4格，营地3格。\n\n绿色可建，红色不可建。\n全部占地须可建，点击建筑查看详情。";
                for(int i=0;i<4;i++){int kind=i;Action(HexConstruction.Names[i]+" "+BuildingFootprints.Counts[i]+"格 · 木"+HexConstruction.WoodCost[i]+" 石"+HexConstruction.StoneCost[i]+" 金"+HexConstruction.GoldCost[i],i,()=>game.Game.Construction.Choose(kind));}
                Page("下一页 · 房屋、仓库与守卫",()=>Show("campbuild"));
            }
            else if(mode=="campbuild")
            {
                title.text="营地建设 · 房屋与仓库";
                body.text="房屋木10石6金15：村民容量+2，上限9。\n仓库木8石8金12：各资源点库存+30，上限120。\n两类建筑均占4格，可旋转、拆除。\n\n夜袭日为第3、6、9…天。\n先修围栏、布置陷阱，再回房屋休息守卫。";
                Action("建造居民房屋",0,()=>game.Game.Construction.Choose(4));
                Action("建造仓库",1,()=>game.Game.Construction.Choose(5));
                Action("布置陷阱 · 木2石1",2,game.Game.Night.ChooseTrap);
                Action("查看营地与守卫",3,()=>game.Game.Management.OpenCamp());
                Page("下一页 · 围栏、牧场与拆除",()=>Show("pasture"));
            }
            else if(mode=="pasture")
            {
                title.text="牧场 · 围栏与动物";
                body.text="木材 "+s.wood+"   浆果 "+s.berries+"   松露 "+s.truffles+"\n\n围栏沿六边形边缘放置，格内可养动物。\n至少围住三格，关好门，再点击野猪驯养。\n点击牧场门可开关，点击动物查看状态。\n每只每天喂浆果1，次日松露1，可售18金币。";
                Action("木围栏 · 每段木2",0,()=>game.Game.Fences.Choose(0));
                Action("牧场门 · 每扇木4",1,()=>game.Game.Fences.Choose(1));
                Action("拆除围栏 · 返还一半木材",2,()=>game.Game.Fences.Choose(2));
                Action("拆除自建建筑",3,()=>game.Game.Construction.Choose(HexConstruction.DemolitionKind));
                Page("上一页 · 营地建设",()=>Show("campbuild"));
            }
            else if(mode=="fence")
            {
                var f=game.Game.Fences.Inspected;if(f==null){Hide();return;}
                title.text=f.gate?"牧场门":"木围栏";
                body.text=(f.free?"连续围栏 · 长度 "+UnityEngine.Vector2.Distance(new UnityEngine.Vector2(f.ax,f.az),new UnityEngine.Vector2(f.bx,f.bz)).ToString("0.00")+" 米":"坐标 ["+f.column+","+f.row+"]   边缘 "+(f.edge+1))+"\n\n"+(f.gate?(f.open?"状态：已打开。牧场未封闭。":"状态：已关闭，可围住牧场。") :"阻挡角色与动物，不占用整格。")+"\n围栏耐久："+(3-f.damage)+" / 3\n\n费用：木材 "+(f.gate?4:2)+"\n拆除返还一半木材。";
                if(f.gate)Action(f.open?"关闭牧场门":"打开牧场门",0,game.Game.Fences.ToggleGate);
                Action("拆除这段围栏",1,game.Game.Fences.RemoveInspected);
                if(f.damage>0)Action("修补受损围栏 · 木1",2,game.Game.Fences.RepairInspected);
                Action("继续建造围栏",2,()=>game.Game.Fences.Choose(0));
            }
            else if(mode=="animal")
            {
                var actor=game.Game.Animals.Inspected;if(!actor){Hide();return;}var a=actor.record;
                title.text=a==null?"林间野猪 · 野生动物":"牧场野猪 · 动物详情";
                body.text=a==null?"野猪在青苔森林与金穗平原活动。\n\n用三份浆果驯养，自动迁入附近封闭牧场。\n先在十步以内围出至少三格，关好牧场门。":
                    "状态：已驯养\n今日喂食："+(a.fedDay==game.Game.State.day?"已吃饱，次日松露+1":"尚未喂食")+"\n牧场："+(actor.pen==null?"未封闭，请补齐围栏或关门":"封闭 · "+actor.pen.Count+"格")+"\n\n每天喂浆果1，结束一天后产松露1。\n仓库松露 "+s.truffles+" · 每份售价18金币";
                if(a==null)Action("驯养到牧场 · 浆果3",0,game.Game.Animals.Tame,s.berries>=3);
                else Action(a.fedDay==game.Game.State.day?"今日已喂食":"喂食 · 浆果1",0,game.Game.Animals.Feed,a.fedDay!=game.Game.State.day&&s.berries>0);
                Action("出售全部松露 · 金"+s.truffles*18,1,game.Game.Animals.Sell,s.truffles>0);
                Action("围栏与牧场建造",2,()=>Show("pasture"));
            }
            else if(mode=="building" || mode=="confirm-demolition")
            {
                var info=game.Game.Construction.Inspected;
                if(!info){title.text="建筑详情";body.text="点击建筑或任意占地格查看详情。";Action("打开建造菜单",0,game.Game.Construction.Open);return;}
                title.text=info.displayName+" · 建筑详情";
                var b=info.record;
                if(b==null)
                {
                    body.text="状态：固定村庄建筑\n\n"+info.description+"\n\n点击自建建筑可查看占地、产出、花费与拆除。";
                    Action("查看城镇建设与研究",0,()=>Show("town"));
                    Action("查看行囊与料理",1,()=>Show("supplies"));
                }
                else
                {
                    string output=b.kind==0?"每天木材 +2":b.kind==1?"每天石料 +2":b.kind==2?"每天知识 +1":b.kind==4?"村民容量+2，可在房屋旁休息":b.kind==5?"各资源点库存容量+30":"可制作料理，需走近营地";
                    int count=BuildingFootprints.Cells(b).Count;
                    if(mode=="confirm-demolition")
                    {
                        body.text="确认拆除这座"+info.displayName+"？\n\n占地 "+count+" 格，拆除后释放全部地块。\n返还木材 "+HexConstruction.WoodCost[b.kind]/2+"、石料 "+HexConstruction.StoneCost[b.kind]/2+"、金币 "+HexConstruction.GoldCost[b.kind]/2+"。\n\n每日产出与营地功能将停止。";
                        Action("确认拆除并返还资源",0,game.Game.Construction.DemolishInspected);
                        Action("返回建筑详情",1,()=>Show("building"));
                    }
                    else
                    {
                        body.text="状态：运行中   占地 "+count+" 格\n坐标 ["+b.column+","+b.row+"]   朝向 "+b.rotation*60+"度\n\n"+output+"\n花费：木"+HexConstruction.WoodCost[b.kind]+" 石"+HexConstruction.StoneCost[b.kind]+" 金"+HexConstruction.GoldCost[b.kind]+"\n建造："+(b.builtDay>0?"第"+b.builtDay+"天":"原有存档建筑")+"\n\n"+(b.footprintVersion==0?"周围有障碍，旧占地保留；可拆除重建。":"黄色边框表示建筑完整占地。");
                        Action("拆除建筑 · 返还一半资源",0,()=>Show("confirm-demolition"));
                        Action("继续建造",1,game.Game.Construction.Open);
                        if(b.kind==3)Action("查看料理与补给",2,()=>Show("supplies"));
                    }
                }
            }
            else if(mode=="supplies")
            {
                title.text="行囊 · 料理与种子";
                var farm=game.Game.State;
                body.text="体力 "+farm.energy+" / 300   料理 "+s.meals+"\n\n金萝卜种子 "+farm.seeds+" · 两天成熟\n小麦种子 "+farm.wheatSeeds+" · 三天成熟，售24金币\n浆果种子 "+farm.berrySeeds+" · 四天成熟，可重复收获\n\n每四天有雨，自动浇水。种田耗体力1，采集2。\n营地与小屋可料理，使用料理回复12体力。";
                Action("制作料理 · 浆果3 鲜鱼1",0,()=>game.Supply("cook"));
                Action("使用料理 · 回复体力12",1,()=>game.Supply("eat"),s.meals>0&&farm.energy<FarmState.MaxEnergy);
                Action("切换种子 · 当前 "+FarmState.CropNames[farm.selectedCrop],2,game.Game.CycleCrop);
                Action("查看贸易订单",3,()=>Show("market"));
                Page("远征日志 · 装备与制作",()=>Show("expedition"));
            }
            else if(mode=="market")
            {
                title.text="远行贸易 · 每日订单";
                var farm=game.Game.State;
                body.text="金币 "+farm.coins+"   贸易完成 "+s.deliveries+" 次\n\n种子："+FarmState.CropNames[farm.selectedCrop]+" · "+farm.SeedCost+"金币\n\n订单：木6 石4 鲜鱼2，奖励金35、知2。\n每天限一次，必须站在平原或驿站商人身边。";
                Action("购买当前种子",0,()=>game.Supply("seeds"));
                Action("出售木材5 · 金币12",1,()=>game.Supply("wood"));
                Action("出售鲜鱼2 · 金币10",2,()=>game.Supply("fish"));
                Action(s.deliveredDay==farm.day?"今日订单已完成":"提交每日贸易订单",3,()=>game.Supply("delivery"),s.deliveredDay!=farm.day);
            }
            else
            {
                title.text="冒险日志 · 村庄复兴";
                string friends="";
                for(int i=0;i<3;i++)if(s.companions[i])friends+=AdventureContent.Names[i]+" · "+AdventureContent.Roles[i]+"\n";
                body.text=AdventureContent.Quests[s.quest]+"\n\n收获 "+game.Game.State.harvests+" / 3   伙伴 "+s.CompanionCount+" / 3\n\n"+(friends==""?"森林的苗芽喜欢浆果，去东侧找它吧！\n":friends)+"\n每日采集点会在休息后恢复。\n完成委托要回村长身边领取奖励。";
                Action("切换随行伙伴",0,game.CycleCompanion);
                Action("打开旅途地图",1,()=>Show("map"));
                Action("查看建设与研究",2,()=>Show("town"));
                Action("远征与制作",3,()=>Show("expedition"));
            }
        }
        void CreateFishingTrack()
        {
            fishingTrack=TrackPart("Fishing track",panel,new Color(.02f,.08f,.08f));
            fishingTrack.anchorMin=fishingTrack.anchorMax=fishingTrack.pivot=new Vector2(0,1);fishingTrack.anchoredPosition=new Vector2(24,-232);fishingTrack.sizeDelta=new Vector2(400,18);
            fishingZone=TrackPart("Green catch window",fishingTrack,new Color(.38f,.72f,.36f));
            fishingCursor=TrackPart("Fishing cursor",fishingTrack,new Color(1,.87f,.53f));
            fishingTrack.gameObject.SetActive(false);
        }
        RectTransform TrackPart(string name,Transform parent,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;go.GetComponent<UnityEngine.UI.Image>().color=color;go.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;return rect;
        }
        void Update()
        {
            if(mode!="fishing"||!game.Game.Expedition.Fishing)return;var fish=game.Game.Expedition;
            body.text=fish.FishingText;
            fishingZone.anchorMin=new Vector2(fish.ZoneCenter-fish.ZoneWidth*.5f,0);fishingZone.anchorMax=new Vector2(fish.ZoneCenter+fish.ZoneWidth*.5f,1);fishingZone.offsetMin=fishingZone.offsetMax=Vector2.zero;
            fishingCursor.anchorMin=new Vector2(fish.Cursor,0);fishingCursor.anchorMax=new Vector2(fish.Cursor,1);fishingCursor.offsetMin=new Vector2(-2,-4);fishingCursor.offsetMax=new Vector2(2,4);
        }
        public void DismissBuildingDetails(){if(mode=="building"||mode=="confirm-demolition")Hide();}
        void Page(string text,UnityEngine.Events.UnityAction callback) => AddButton(actions,text,new Vector2(24,-246),new Vector2(400,32),callback);
        void Action(string label,int row,UnityEngine.Events.UnityAction callback,bool enabled=true)
        {
            var button=AddButton(actions,label,new Vector2(24,-292-row*48),new Vector2(400,40),callback);
            button.interactable=enabled;
        }
        TextMeshProUGUI Text(string name,Transform parent,Vector2 pos,Vector2 size,float point,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));var rect=go.GetComponent<RectTransform>();
            rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=pos;rect.sizeDelta=size;
            var label=go.GetComponent<TextMeshProUGUI>();label.font=font;label.fontSize=point;label.color=color;label.raycastTarget=false;label.enableAutoSizing=false;
            return label;
        }
        UnityEngine.UI.Button AddButton(Transform parent,string label,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction callback)
        {
            var go=new GameObject(label,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));
            var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=pos;rect.sizeDelta=size;
            var image=go.GetComponent<UnityEngine.UI.Image>();image.color=new Color(.18f,.3f,.25f);image.raycastTarget=true;
            HudOrnament.Border(rect);
            var button=go.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.onClick.AddListener(callback);
            var text=Text("Caption",go.transform,new Vector2(6,-6),new Vector2(size.x-12,size.y-12),17,Color.white);text.text=label;text.alignment=TextAlignmentOptions.Center;
            return button;
        }
    }
}
