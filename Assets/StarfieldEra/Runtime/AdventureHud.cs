using TMPro;
using UnityEngine;

namespace StarfieldEra
{
    public sealed class AdventureHud : MonoBehaviour
    {
        AdventureGame game;
        TMP_FontAsset font;
        RectTransform panel;
        TextMeshProUGUI title,body;
        Transform actions;
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
            AddButton(panel,"关闭",new Vector2(322,-492),new Vector2(100,38),Hide);
            panel.gameObject.SetActive(false);
        }
        public void Show(string next)
        {
            if(game.InBattle&&next!="battle")return;
            if(panel.gameObject.activeSelf&&mode==next&&next!="battle"){Hide();return;}
            mode=next;panel.gameObject.SetActive(true);Rebuild();
        }
        public void Hide(){if(game.InBattle)return;panel.gameObject.SetActive(false);mode="";}
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
            if(mode=="battle")
            {
                title.text=game.EnemyName+" · 回合 "+game.Turn;
                body.text=AdventureContent.Names[s.activeCompanion]+" 体力 "+game.AllyHP+" / "+game.AllyMaxHP+"\n\n"+game.EnemyName+" 体力 "+game.EnemyHP+" / "+game.EnemyMaxHP+"\n\n"+game.BattleLog+"\n\n技能冷却："+game.Cooldown+" 回合\n守卫每三回合会发动重击，适时防御。";
                Action("1  普通攻击",0,()=>game.BattleAction(0));
                Action("2  伙伴技能",1,()=>game.BattleAction(1));
                Action("3  防御与回复",2,()=>game.BattleAction(2));
                Action("4  撤退",3,()=>game.BattleAction(3));
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
                body.text=mode=="map"?"中央：晨风村、24块农田与议事牌。\n东侧：青苔森林，木材与浆果。\n西南：镜湖岸，钓鱼与水系伙伴。\n东北：星辉遗迹，晶石与守卫。\n\n先步行发现，再解锁快速旅行。":"东南：金穗平原，营地与商人。\n东北远方：白岩山地，矿石与新守卫。\n西北：幽光湿地，浆果、钓鱼与守卫。\n西南远方：远行驿站，贸易与营地。\n\n鼠标滚轮可缩放到大陆视野。";
                int start=mode=="map"?0:4;
                for(int i=0;i<4;i++){int region=start+i;Action(AdventureContent.Regions[region]+(s.discovered[region]?" · 旅行":" · 尚未发现"),i,()=>game.Travel(region),s.discovered[region]);}
                Page(mode=="map"?"下一页 · 四个远方区域":"上一页 · 村庄周边",()=>Show(mode=="map"?"map2":"map"));
            }
            else if(mode=="build")
            {
                title.text="六边形建造 · 自由选址";
                body.text="木材 "+s.wood+"   石料 "+s.stone+"   金币 "+game.Game.State.coins+"\n\n林场木+2，采石场石+2，学舍知+1。\n营地可料理，选择后在空地放置。\n\n左键建造，R旋转；右键或Esc取消。\n绿色可建，红色不可建。\n水面、农田、人物旁不能建。";
                for(int i=0;i<4;i++){int kind=i;Action(HexConstruction.Names[i]+" · 木"+HexConstruction.WoodCost[i]+" 石"+HexConstruction.StoneCost[i]+" 金"+HexConstruction.GoldCost[i],i,()=>game.Game.Construction.Choose(kind));}
                Page("拆除自建建筑 · 返还一半资源",()=>game.Game.Construction.Choose(4));
            }
            else if(mode=="supplies")
            {
                title.text="行囊 · 料理与种子";
                var farm=game.Game.State;
                body.text="体力 "+farm.energy+" / 30   料理 "+s.meals+"\n\n金萝卜种子 "+farm.seeds+" · 两天成熟\n小麦种子 "+farm.wheatSeeds+" · 三天成熟，售24金币\n浆果种子 "+farm.berrySeeds+" · 四天成熟，可重复收获\n\n每四天有雨，自动浇水。种田耗体力1，采集2。\n营地与小屋可料理，使用料理回复12体力。";
                Action("制作料理 · 浆果3 鲜鱼1",0,()=>game.Supply("cook"));
                Action("使用料理 · 回复体力12",1,()=>game.Supply("eat"),s.meals>0&&farm.energy<30);
                Action("T 切换种子 · 当前 "+FarmState.CropNames[farm.selectedCrop],2,game.Game.CycleCrop);
                Action("查看贸易订单",3,()=>Show("market"));
            }
            else if(mode=="market")
            {
                title.text="远行贸易 · 每日订单";
                var farm=game.Game.State;
                body.text="金币 "+farm.coins+"   贸易完成 "+s.deliveries+" 次\n\n种子："+FarmState.CropNames[farm.selectedCrop]+" · "+farm.SeedCost+"金币\n按 T 切换种子。\n\n订单：木6 石4 鲜鱼2，奖励金35、知2。\n每天限一次，必须站在平原或驿站商人身边。";
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
                body.text=AdventureContent.Quests[s.quest]+"\n\n收获 "+game.Game.State.harvests+" / 3   伙伴 "+s.CompanionCount+" / 3\n\n"+(friends==""?"森林的苗芽鹿喜欢浆果，去东侧找它吧！\n":friends)+"\n每日采集点会在休息后恢复。\n完成委托要回村长身边领取奖励。";
                Action("C  切换随行伙伴",0,game.CycleCompanion);
                Action("M  打开旅途地图",1,()=>Show("map"));
                Action("查看建设与研究",2,()=>Show("town"));
            }
        }
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
