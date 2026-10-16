using UnityEngine;
using System.Linq;

namespace StarfieldEra
{
    public sealed class AdventureGame : MonoBehaviour
    {
        public FarmGame Game { get; private set; }
        public WorldEncounter Target { get; private set; }
        public AdventureState State => Game.State.adventure;
        public bool InBattle { get; private set; }
        public int AllyHP { get; private set; }
        public int AllyMaxHP { get; private set; }
        public int EnemyHP { get; private set; }
        public int EnemyMaxHP { get; private set; }
        public string EnemyName { get; private set; }
        int enemyId;
        public int Turn { get; private set; }
        public int Cooldown { get; private set; }
        public string BattleLog { get; private set; }
        public AdventureHud Hud { get; set; }
        public int Revision { get; private set; }
        public void Initialize(FarmGame game) { Game=game; Game.World.Adventure.Refresh(State); }
        public void UpdateTarget()
        {
            Target=null;float best=2.25f;
            foreach(var item in Game.World.Adventure.Encounters)
            {
                if(!item.root.gameObject.activeSelf)continue;
                if(item.kind==EncounterKind.Stone&&Game.Mining&&!Game.Mining.Reachable(item.root.GetComponentInChildren<HarvestableRock>()))continue;
                var delta=item.Position-Game.World.Player.position;delta.y=0;
                float distance=delta.magnitude;
                if(distance<best){best=distance;Target=item;}
            }
            int region=Game.World.Adventure.Region;
            if(!State.discovered[region])
            {
                State.discovered[region]=true;Revision++;
                Game.ShowMessage("发现新区域："+AdventureContent.Regions[region]+"。");
                Game.Persist();
            }
        }
        public string TargetHint
        {
            get
            {
                if(InBattle)return "遗迹遭遇战 · 选择攻击、技能、防御或撤退";
                if(Target==null)return "";
                if(Target.kind==EncounterKind.Stone)return "石料 · E 开采 · 体力2 · 采完碎裂消失";
                if(Target.kind==EncounterKind.Fiber||Target.kind==EncounterKind.Treasure||Target.kind==EncounterKind.Rune)return Game.Expedition.TargetHint(Target);
                if((Target.kind<=EncounterKind.Fish || Target.kind==EncounterKind.Crystal) && State.gatheredDay[Target.id]==Game.State.day)return Target.name+" · 今天已采集，明天恢复";
                if(Target.kind==EncounterKind.Companion)
                    return Target.name+" · "+(Target.id==0?"招募需要三份浆果":Target.id==1?"招募需要两条鱼和两份浆果":"击败守卫后，用一枚晶石招募");
                return Target.name+" · 可交互";
            }
        }
        public void Interact()
        {
            if(InBattle)return;
            var item=Target;if(item==null)return;
            switch(item.kind)
            {
                case EncounterKind.Landmark:Game.ShowMessage("北岭狼窟：荒野狼群的栖息地。位置在东北山岭，营地守卫时留意狼群。此处暂用于探索与观察。");break;
                case EncounterKind.Stone:Game.Mining.Mine(item.root.GetComponentInChildren<HarvestableRock>());break;
                case EncounterKind.Fish:Game.Expedition.BeginFishing(item);break;
                case EncounterKind.Fiber:case EncounterKind.Treasure:case EncounterKind.Rune:Game.Expedition.Interact(item);break;
                case EncounterKind.Elder:Quest();break;
                case EncounterKind.Town:Hud.Show("town");break;
                case EncounterKind.Companion:Recruit(item.id);break;
                case EncounterKind.Guardian:StartBattle(item.id);break;
                case EncounterKind.Market:Hud.Show("market");break;
                case EncounterKind.Camp:Hud.Show("supplies");break;
                default:
                    if(State.gatheredDay[item.id]==Game.State.day){Game.ShowMessage("今天这里已经采集过了，休息后再来。");return;}
                    if(item.kind==EncounterKind.Wood&&!EquipmentCatalog.Require(Game,"axe"))return;
                    if(item.kind==EncounterKind.Crystal&&!EquipmentCatalog.Require(Game,"pick"))return;
                    if(!Game.State.SpendEnergy(2)){Game.ShowMessage("体力不足，使用料理或回小屋休息。");return;}
                    State.gatheredDay[item.id]=Game.State.day;Game.State.weather.survivalExperience++;
                    if(item.kind==EncounterKind.Wood){int amount=3+State.toolLevel*2+WeatherCatalog.Get(Game.State.Weather).extraWood;State.wood+=amount;Commit("采集木材 +"+amount+"，体力 -2。");}
                    if(item.kind==EncounterKind.Berry){State.berries+=3;Commit("采摘浆果 +3。浆果可以招募森林伙伴。");}
                    if(item.kind==EncounterKind.Crystal){State.crystals++;Game.State.weather.starStones++;Commit("开采晶石与星石 +1，体力 -2。");}
                    break;
            }
        }
        void Recruit(int species)
        {
            if(State.companions[species])return;
            bool allowed=species==0?State.berries>=3:species==1?State.fish>=2&&State.berries>=2:State.ruinsWon&&State.crystals>=1;
            if(!allowed){Game.ShowMessage(TargetHint);return;}
            if(species==0)State.berries-=3;
            if(species==1){State.fish-=2;State.berries-=2;}
            if(species==2)State.crystals--;
            State.companions[species]=true;State.activeCompanion=species;
            Commit(AdventureContent.Names[species]+"成为了伙伴！"+AdventureContent.Roles[species]+"。");
        }
        public float QuestProgress
        {
            get
            {
                if(State.quest==0)return Mathf.Clamp01(Game.State.harvests/3f);
                if(State.quest==1)return ((State.CompanionCount>0?1:0)+(State.workshop?1:0))*.5f;
                if(State.quest==2)return ((State.ruinsWon?1:0)+(State.irrigation?1:0))*.5f;
                if(State.quest==3)return (Mathf.Clamp01(Game.State.harvests/8f)+(State.expandedFarm?1:0)+(State.mill?1:0))/3;
                if(State.quest==4)return (State.discovered.Count(x=>x)/8f+(State.mountainWon?1:0))*.5f;
                if(State.quest==5)return (State.CompanionCount/3f+(State.observatory?1:0)+(State.lore?1:0))/3;
                return 1;
            }
        }
        public void ClaimMainQuest()
        {
            if(InBattle||Target==null||Target.kind!=EncounterKind.Elder){Game.ShowMessage("回村长身边领取任务奖励。");return;}Quest();
        }
        void Quest()
        {
            if(State.quest>=6){Game.ShowMessage(AdventureContent.Quests[6]);Hud.Show("journal");return;}
            bool complete=QuestProgress>=1;
            if(!complete){Game.ShowMessage("阿岚："+AdventureContent.Quests[State.quest]);Hud.Show("journal");return;}
            if(State.quest==0){Game.State.coins+=30;State.berries+=3;State.wood+=5;}
            if(State.quest==1){Game.State.coins+=40;State.stone+=5;State.science+=4;}
            if(State.quest==2){Game.State.coins+=80;State.science+=8;}
            if(State.quest==3){Game.State.coins+=60;State.berries+=5;State.science+=4;}
            if(State.quest==4){Game.State.coins+=80;State.crystals+=3;State.science+=8;}
            if(State.quest==5){Game.State.coins+=200;State.science+=20;}
            State.quest++;
            Commit("委托完成！阿岚送来了建设资源。"+AdventureContent.Quests[State.quest]);
            Hud.Show("journal");
        }
        public void BuildOrResearch(string action)
        {
            if(InBattle)return;
            int building=action=="workshop"?6:action=="library"?7:action=="mill"?8:action=="observatory"?9:-1;
            if(building>=0){Game.Construction.Choose(building);return;}
            if(Target==null||Target.kind!=EncounterKind.Town){Game.ShowMessage("请走到晨风村的城镇议事牌旁进行建设与研究。");return;}
            if(action!="lore"&&action!="tools"&&!EquipmentCatalog.Require(Game,"hammer"))return;
            bool valid=false;
            switch(action)
            {
                case "irrigation":
                    if(!State.irrigation&&State.library&&State.science>=4&&State.wood>=4&&State.stone>=4){State.science-=4;State.wood-=4;State.stone-=4;State.irrigation=true;valid=true;}break;
                case "farm":
                    if(!State.expandedFarm&&State.workshop&&State.wood>=8&&State.stone>=6&&Game.State.coins>=30){State.wood-=8;State.stone-=6;Game.State.coins-=30;State.expandedFarm=true;valid=true;}break;
                case "tools":
                    if(State.toolLevel==0&&State.workshop&&State.wood>=6&&State.stone>=5&&Game.State.coins>=25){State.wood-=6;State.stone-=5;Game.State.coins-=25;State.toolLevel=1;valid=true;}break;
                case "lore":
                    if(!State.lore&&State.irrigation&&State.science>=6&&State.crystals>=1){State.science-=6;State.crystals--;State.lore=true;valid=true;}break;
            }
            if(valid){Game.World.RefreshCrops(Game.State);Commit("建设或研究完成！农田、建筑与每日产出已更新。");}
            else Game.ShowMessage("资源不足、前置尚未完成，或该项目已经完成。费用和前置见议事面板。");
            Hud.Refresh();
        }
        public void SelectCompanion(int species)
        {
            if(InBattle||species<0||species>=3||!State.companions[species])return;
            State.activeCompanion=species;Commit("随行伙伴："+AdventureContent.Names[species]+"。");
        }
        public void CycleCompanion()
        {
            if(InBattle){Game.ShowMessage("战斗中不能切换伙伴。");return;}
            if(State.CompanionCount==0){Game.ShowMessage("先在森林找到苗芽，用三份浆果招募。");return;}
            for(int step=1;step<=3;step++)
            {
                int index=(State.activeCompanion+step)%3;
                if(State.companions[index]){State.activeCompanion=index;break;}
            }
            Commit("随行伙伴："+AdventureContent.Names[State.activeCompanion]+"。"+AdventureContent.Roles[State.activeCompanion]);
        }
        public void Travel(int region)
        {
            if(Game.Night&&Game.Night.Active)return;
            Game.Night?.CancelTrap();
            if(InBattle||region<0||region>=8||!State.discovered[region])return;
            Vector3[] destinations={new Vector3(-5,.2f,7),new Vector3(13,.2f,6),new Vector3(-8,.2f,-7),new Vector3(10,.2f,8),new Vector3(24,.2f,-22),new Vector3(25,.2f,24),new Vector3(-27,.2f,20),new Vector3(-28,.2f,-18)};
            Game.World.Controller.enabled=false;Game.World.Player.position=Game.World.HexGrid.SafeLand(destinations[region],Game.World.Controller.radius);Game.World.Controller.enabled=true;
            Game.UpdateSelection();Hud.Hide();Commit("抵达："+AdventureContent.Regions[region]);
        }
        void StartBattle(int id)
        {
            if(Game.Night&&Game.Night.Active)return;
            if(State.CompanionCount==0){Game.ShowMessage("遗迹危机四伏，先招募一位伙伴再来挑战。");return;}
            if(State.bossDay[id]==Game.State.day){Game.ShowMessage("守卫今日已被击退，明天可以再次挑战。");return;}
            enemyId=id;EnemyName=id==0?"岩甲兽":id==1?"白岩首领 · 野蛮人":"幽光守卫";
            InBattle=true;AllyMaxHP=40+Mathf.Min(State.victories,10)*2+(State.expedition.starAmulet?10:0);AllyHP=AllyMaxHP;EnemyMaxHP=id==0?42:id==1?68:58;EnemyHP=EnemyMaxHP;
            Turn=1;Cooldown=0;BattleLog=EnemyName+"挡住了道路。每三回合重击，水系克制白岩，火系克制幽光。";
            Revision++;Hud.Show("battle");
        }
        public void BattleAction(int action)
        {
            if(!InBattle || action<0 || action>3)return;
            if(action==3){InBattle=false;Hud.Hide();Commit("你和伙伴撤离了遗迹，随时可以再次挑战。");return;}
            if(action==1&&Cooldown>0){Game.ShowMessage("技能仍在冷却，先使用攻击或防御。");return;}
            int bonus=(State.expedition.starAmulet?3:0)+(State.companions[2]?3:0)+(State.lore?3:0)+Mathf.Min(State.victories,6);
            if(action==1&&((enemyId==1&&State.activeCompanion==1)||(enemyId==2&&State.activeCompanion==2)))bonus+=6;
            string[] skills={"藤蔓缠绕","潮汐冲击","星火爆裂"};
            if(action==0){EnemyHP=Mathf.Max(0,EnemyHP-9-bonus);BattleLog="伙伴发动攻击，造成 "+(9+bonus)+" 点伤害。";}
            if(action==1){EnemyHP=Mathf.Max(0,EnemyHP-17-bonus);Cooldown=3;BattleLog=skills[State.activeCompanion]+"！造成 "+(17+bonus)+" 点伤害。";}
            if(action==2){AllyHP=Mathf.Min(AllyMaxHP,AllyHP+5);BattleLog="伙伴摆出防御姿态，回复五点体力。";}
            if(EnemyHP==0)
            {
                InBattle=false;if(enemyId==0){State.ruinsWon=true;State.guardianDay=Game.State.day;}else if(enemyId==1)State.mountainWon=true;else State.marshWon=true;
                State.victories++;Game.State.weather.survivalExperience+=5;Game.State.weather.starStones++;State.crystals+=enemyId==0?2:3;Game.State.coins+=enemyId==0?25:40;State.science+=enemyId==0?2:4;State.bossDay[enemyId]=Game.State.day;
                Hud.Hide();Commit("击败"+EnemyName+"！获得晶石、金币和知识。守卫将在明天重新出现。");return;
            }
            int retaliation=Turn%3==0?13+enemyId*2:8+enemyId;
            if(action==2)retaliation=2;
            if(action==1&&State.activeCompanion==0)retaliation=Mathf.Max(2,retaliation-4);
            if(action==1&&State.activeCompanion==1)AllyHP=Mathf.Min(AllyMaxHP,AllyHP+6);
            AllyHP=Mathf.Max(0,AllyHP-retaliation);BattleLog+=" 守卫反击 "+retaliation+" 点。";
            if(AllyHP==0){InBattle=false;Hud.Hide();Commit("伙伴力竭，你们回到村庄休整。没有损失资源，再试一次吧！");Travel(0);return;}
            Turn++;Cooldown=Mathf.Max(0,Cooldown-1);Revision++;Hud.Refresh();
        }
        public void Supply(string action)
        {
            if(InBattle)return;
            bool merchant=Target!=null&&Target.kind==EncounterKind.Market;
            bool kitchen=Game.NearHouse||(Target!=null&&Target.kind==EncounterKind.Camp);
            if(action=="eat")
            {
                if(State.meals<1||Game.State.energy>=FarmState.MaxEnergy){Game.ShowMessage("没有料理，或体力已经充足。");return;}
                State.meals--;Game.State.RestoreEnergy(12);Commit("使用料理，体力回复12。");return;
            }
            if(action=="cook")
            {
                if(!kitchen||State.berries<3||State.fish<1){Game.ShowMessage("在小屋或营地，用浆果3、鲜鱼1制作料理。");return;}
                State.berries-=3;State.fish--;State.meals++;Commit("制作料理 +1。可在背包中使用。");return;
            }
            if(!merchant){Game.ShowMessage("请走近远行驿站或金穗平原的商人交易。");return;}
            if(action=="seeds"){Game.BuySeed();return;}
            if(action=="delivery")
            {
                if(State.deliveredDay==Game.State.day||State.wood<6||State.stone<4||State.fish<2){Game.ShowMessage("每日订单：木材6、石料4、鲜鱼2，每天限一次。");return;}
                State.wood-=6;State.stone-=4;State.fish-=2;State.deliveredDay=Game.State.day;State.deliveries++;Game.State.coins+=35;State.science+=2;Commit("完成贸易订单！金币 +35，知识 +2。");return;
            }
            if(action=="wood"&&State.wood>=5){State.wood-=5;Game.State.coins+=12;Commit("出售木材5，金币 +12。");}
            else if(action=="fish"&&State.fish>=2){State.fish-=2;Game.State.coins+=10;Commit("出售鲜鱼2，金币 +10。");}
            else Game.ShowMessage("交易资源不足。");
        }
        public void Commit(string message)
        {
            Revision++;Game.World.Adventure.Refresh(State);Game.Expedition?.RefreshVisuals();Game.ShowMessage(message);Game.Persist();Hud?.Refresh();
        }
    }
}
