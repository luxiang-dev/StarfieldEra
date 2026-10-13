using UnityEngine;

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
                var delta=item.Position-Game.World.Player.position;delta.y=0;
                float distance=delta.magnitude;
                if(distance<best){best=distance;Target=item;}
            }
            int region=Game.World.Adventure.Region;
            if(!State.discovered[region])
            {
                State.discovered[region]=true;Revision++;
                Game.ShowMessage("发现新区域："+AdventureContent.Regions[region]+"。按 M 查看地图与旅行点。");
                Game.Persist();
            }
        }
        public string TargetHint
        {
            get
            {
                if(InBattle)return "遗迹遭遇战 · 选择攻击、技能、防御或撤退";
                if(Target==null)return "";
                if(Target.kind<=EncounterKind.Fish && State.gatheredDay[Target.id]==Game.State.day)return Target.name+" · 今天已采集，明天恢复";
                if(Target.kind==EncounterKind.Companion)
                    return Target.name+" · "+(Target.id==0?"招募需要三份浆果":Target.id==1?"招募需要两条鱼和两份浆果":"击败守卫后，用一枚晶石招募");
                return Target.name+" · E 交互";
            }
        }
        public void Interact()
        {
            if(InBattle)return;
            var item=Target;if(item==null)return;
            switch(item.kind)
            {
                case EncounterKind.Elder:Quest();break;
                case EncounterKind.Town:Hud.Show("town");break;
                case EncounterKind.Companion:Recruit(item.id);break;
                case EncounterKind.Guardian:StartBattle();break;
                default:
                    if(State.gatheredDay[item.id]==Game.State.day){Game.ShowMessage("今天这里已经采集过了，休息后再来。");return;}
                    State.gatheredDay[item.id]=Game.State.day;
                    if(item.kind==EncounterKind.Wood){State.wood+=3;Commit("采集木材 +3。工坊和研究需要木材。");}
                    if(item.kind==EncounterKind.Stone){State.stone+=3;Commit("开采石料 +3。石料用于建设与灌溉。");}
                    if(item.kind==EncounterKind.Berry){State.berries+=3;Commit("采摘浆果 +3。浆果可以招募森林伙伴。");}
                    if(item.kind==EncounterKind.Fish){int yield=State.companions[1]?3:2;State.fish+=yield;Commit("钓鱼成功！鲜鱼 +"+yield+"。");}
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
            Commit(AdventureContent.Names[species]+"成为了伙伴！"+AdventureContent.Roles[species]+"。按 C 切换随行伙伴。");
        }
        void Quest()
        {
            bool complete=State.quest==0?Game.State.harvests>=3:State.quest==1?State.CompanionCount>0&&State.workshop:State.quest==2?State.ruinsWon&&State.irrigation:false;
            if(!complete){Game.ShowMessage("阿岚："+AdventureContent.Quests[State.quest]);Hud.Show("journal");return;}
            if(State.quest==0){Game.State.coins+=30;State.berries+=3;State.wood+=5;}
            if(State.quest==1){Game.State.coins+=40;State.stone+=5;State.science+=4;}
            if(State.quest==2){Game.State.coins+=80;State.science+=8;}
            State.quest++;
            Commit("委托完成！阿岚送来了建设资源。"+AdventureContent.Quests[State.quest]);
            Hud.Show("journal");
        }
        public void BuildOrResearch(string action)
        {
            if(InBattle)return;
            if(Target==null||Target.kind!=EncounterKind.Town){Game.ShowMessage("请走到晨风村的城镇议事牌旁进行建设与研究。");return;}
            bool valid=false;
            switch(action)
            {
                case "workshop":
                    if(!State.workshop&&State.wood>=6&&State.stone>=4&&Game.State.coins>=15){State.wood-=6;State.stone-=4;Game.State.coins-=15;State.workshop=true;valid=true;}break;
                case "library":
                    if(!State.library&&State.workshop&&State.wood>=8&&State.stone>=6&&Game.State.coins>=20){State.wood-=8;State.stone-=6;Game.State.coins-=20;State.library=true;valid=true;}break;
                case "irrigation":
                    if(!State.irrigation&&State.library&&State.science>=4&&State.wood>=4&&State.stone>=4){State.science-=4;State.wood-=4;State.stone-=4;State.irrigation=true;valid=true;}break;
                case "lore":
                    if(!State.lore&&State.irrigation&&State.science>=6&&State.crystals>=1){State.science-=6;State.crystals--;State.lore=true;valid=true;}break;
            }
            if(valid)Commit("建设或研究完成！村庄的建筑和每日产出已更新。");
            else Game.ShowMessage("资源不足、前置尚未完成，或该项目已经完成。费用和前置见议事面板。");
            Hud.Refresh();
        }
        public void CycleCompanion()
        {
            if(InBattle){Game.ShowMessage("战斗中不能切换伙伴。");return;}
            if(State.CompanionCount==0){Game.ShowMessage("先在森林找到苗芽鹿，用三份浆果招募。");return;}
            for(int step=1;step<=3;step++)
            {
                int index=(State.activeCompanion+step)%3;
                if(State.companions[index]){State.activeCompanion=index;break;}
            }
            Commit("随行伙伴："+AdventureContent.Names[State.activeCompanion]+"。"+AdventureContent.Roles[State.activeCompanion]);
        }
        public void Travel(int region)
        {
            if(InBattle||region<0||region>=4||!State.discovered[region])return;
            Vector3[] destinations={new Vector3(-5,.2f,7),new Vector3(13,.2f,6),new Vector3(-8,.2f,-7),new Vector3(10,.2f,8)};
            Game.World.Controller.enabled=false;Game.World.Player.position=destinations[region];Game.World.Controller.enabled=true;
            Game.UpdateSelection();Hud.Hide();Commit("抵达："+AdventureContent.Regions[region]);
        }
        void StartBattle()
        {
            if(State.CompanionCount==0){Game.ShowMessage("遗迹危机四伏，先招募一位伙伴再来挑战。");return;}
            if(State.guardianDay==Game.State.day){Game.ShowMessage("守卫今日已被击退，明天可以再次挑战。");return;}
            InBattle=true;AllyMaxHP=40+Mathf.Min(State.victories,10)*2;AllyHP=AllyMaxHP;EnemyHP=42;
            Turn=1;Cooldown=0;BattleLog="岩甲兽挡住了遗迹入口。用技能和防御争取胜利。";
            Revision++;Hud.Show("battle");
        }
        public void BattleAction(int action)
        {
            if(!InBattle)return;
            if(action==3){InBattle=false;Hud.Hide();Commit("你和伙伴撤离了遗迹，随时可以再次挑战。");return;}
            if(action==1&&Cooldown>0){Game.ShowMessage("技能仍在冷却，先使用攻击或防御。");return;}
            int bonus=(State.companions[2]?3:0)+(State.lore?3:0)+Mathf.Min(State.victories,6);
            string[] skills={"藤蔓缠绕","潮汐冲击","星火爆裂"};
            if(action==0){EnemyHP=Mathf.Max(0,EnemyHP-9-bonus);BattleLog="伙伴发动攻击，造成 "+(9+bonus)+" 点伤害。";}
            if(action==1){EnemyHP=Mathf.Max(0,EnemyHP-17-bonus);Cooldown=3;BattleLog=skills[State.activeCompanion]+"！造成 "+(17+bonus)+" 点伤害。";}
            if(action==2){AllyHP=Mathf.Min(AllyMaxHP,AllyHP+5);BattleLog="伙伴摆出防御姿态，回复五点体力。";}
            if(EnemyHP==0)
            {
                InBattle=false;State.ruinsWon=true;State.victories++;State.crystals+=2;Game.State.coins+=25;State.science+=2;State.guardianDay=Game.State.day;
                Hud.Hide();Commit("战斗胜利！晶石 +2，金币 +25，知识 +2。遗迹中的焰晶狐愿意接近你了。");return;
            }
            int retaliation=Turn%3==0?13:8;
            if(action==2)retaliation=2;
            if(action==1&&State.activeCompanion==0)retaliation=Mathf.Max(2,retaliation-4);
            if(action==1&&State.activeCompanion==1)AllyHP=Mathf.Min(AllyMaxHP,AllyHP+6);
            AllyHP=Mathf.Max(0,AllyHP-retaliation);BattleLog+=" 守卫反击 "+retaliation+" 点。";
            if(AllyHP==0){InBattle=false;Hud.Hide();Commit("伙伴力竭，你们回到村庄休整。没有损失资源，再试一次吧！");Travel(0);return;}
            Turn++;Cooldown=Mathf.Max(0,Cooldown-1);Revision++;Hud.Refresh();
        }
        public void Commit(string message)
        {
            Revision++;Game.World.Adventure.Refresh(State);Game.ShowMessage(message);Game.Persist();Hud?.Refresh();
        }
    }
}
