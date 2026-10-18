using UnityEngine;
namespace StarfieldEra
{
    public sealed class ExpeditionGame : MonoBehaviour
    {
        public FarmGame Game {get;private set;}
        public ExpeditionState State=>Game.State.adventure.expedition;
        public WorldEncounter Inspected {get;private set;}
        public bool Fishing {get;private set;}
        public float Cursor=>Mathf.PingPong((Time.unscaledTime-fishingStarted)*.7f,1);
        public float ZoneCenter {get;private set;}
        public float ZoneWidth {get;private set;}
        public int FishHits {get;private set;}
        public int FishMisses {get;private set;}
        public bool ReelReady=>Fishing&&Time.unscaledTime>=readyAt;
        public float FishingSeconds=>Mathf.Max(0,deadline-Time.unscaledTime);
        float fishingStarted,readyAt,deadline;int fishingId;bool usingBait;
        ExpeditionWorld expeditionWorld;
        Material bobberMaterial,lineMaterial;
        Transform bobber;LineRenderer fishingLine;Vector3 bobberWater;
        public void Initialize(FarmGame game)
        {Game=game;Physics.SyncTransforms();bobberMaterial=game.World.Mat("Fishing float red","D17557");lineMaterial=game.World.Mat("Fishing line","DDD6B5");expeditionWorld=new GameObject("Expedition landmarks").AddComponent<ExpeditionWorld>();expeditionWorld.transform.SetParent(transform,false);expeditionWorld.Build(this);}
        public float MovementSpeed(TerrainKind kind)
        {float speed=TerrainTraits.Speed[(int)kind];if(State.marshBoots){if(kind==TerrainKind.Marsh)speed=.8f;if(kind==TerrainKind.Snow)speed=.85f;}return speed;}
        public string TargetHint(WorldEncounter item)
        {
            if(item.kind==EncounterKind.Treasure)return item.name+(State.caches[item.id]?" · 已开启":" · 查看补给箱");
            if(item.kind==EncounterKind.Rune)return item.name+(State.runes[item.id]?" · 重读记录":" · 解读石碑");
            return item.name+(State.forageDay[item.id]==Game.State.day?" · 今天已采集":" · 采集纤维，体力1");
        }
        public void Inspect(WorldEncounter item)
        {if(Game.Adventure.InBattle||Fishing)return;Inspected=item;Game.Construction.Cancel();Game.Fences.Cancel();Game.Adventure.Hud.Hide();Game.Adventure.Hud.Show("landmark");}
        public void Interact(WorldEncounter item)
        {if(item.kind==EncounterKind.Fiber)CollectFiber(item);else Inspect(item);}
        bool Nearby(WorldEncounter item)=>item!=null&&Vector3.Distance(item.Position,Game.World.Player.position)<2.75f;
        public string LandmarkDescription
        {
            get
            {
                var item=Inspected;if(item==null)return "走近地图上的补给箱或石碑。";
                if(item.kind==EncounterKind.Treasure)return AdventureContent.Regions[item.id]+"的旅人补给。\n\n"+(State.caches[item.id]?"箱子已开启，奖励已经收入行囊。":"奖励：金币12、木材4、石料3、纤维3，另有种子或远方晶石。每个箱子只能领取一次。\n\n开启消耗体力1，须走到附近。");
                return State.runes[item.id]?ExpeditionContent.Lore[item.id]+"\n\n记录已存入远征日志，可重复阅读。": "古老石碑留有星图的片段。\n\n解读消耗体力3，获得遗迹碎片1、知识2。\n遗迹、白岩、幽光的石碑需要先击败当地守卫。\n\n解读全部八座石碑，拼出大陆的故事。";
            }
        }
        public void ClaimLandmark()
        {
            var item=Inspected;if(Game.Adventure.InBattle||Fishing||!Nearby(item)){Game.ShowMessage("走近这处地标三步以内再交互。");return;}
            if(item.kind==EncounterKind.Treasure)
            {
                if(State.caches[item.id])return;if(!Game.State.SpendEnergy(1)){Game.ShowMessage("开启补给箱需要体力1。");return;}
                State.caches[item.id]=true;State.fiber+=3;Game.State.coins+=12;Game.State.adventure.wood+=4;Game.State.adventure.stone+=3;
                if(item.id<4)Game.State.wheatSeeds+=2;else{Game.State.adventure.crystals++;Game.State.weather.starStones++;Game.State.berrySeeds++;}
                Game.Adventure.Commit("打开补给箱！获得金币、材料和种子。");
            }
            else if(item.kind==EncounterKind.Rune)
            {
                if(State.runes[item.id])return;var a=Game.State.adventure;
                if((item.id==3&&!a.ruinsWon)||(item.id==5&&!a.mountainWon)||(item.id==6&&!a.marshWon)){Game.ShowMessage("石碑被守卫的力量封锁，先击败这个区域的守卫。");return;}
                if(!Game.State.SpendEnergy(3)){Game.ShowMessage("解读石碑需要体力3。");return;}
                Game.State.weather.survivalExperience+=2;State.runes[item.id]=true;State.shards++;a.science+=2;Game.Adventure.Commit("解读"+item.name+"：遗迹碎片+1，知识+2。");
            }
        }
        void CollectFiber(WorldEncounter item)
        {
            if(!Nearby(item)||Game.Adventure.InBattle||Fishing)return;
            if(State.forageDay[item.id]==Game.State.day){Game.ShowMessage("这里的纤维今天已采集，明天恢复。");return;}
            if(!EquipmentCatalog.Require(Game,"sickle"))return;
            if(!Game.State.SpendEnergy(1)){Game.ShowMessage("采集纤维需要体力1。");return;}
            State.forageDay[item.id]=Game.State.day;State.fiber+=3;Game.Adventure.Commit("采集纤维+3。纤维可制作鱼饵、长靴与野营补给。");
        }
        public bool CanCraftHere=>Game.NearHouse||(Game.Adventure.Target!=null&&Game.Adventure.Target.kind==EncounterKind.Camp)||
            (Game.State.adventure.FixedTown(6)&&Vector3.Distance(Game.World.Player.position,new Vector3(-10,0,13))<4)||
            Game.State.adventure.buildings.Exists(b=>b.kind==6&&WorldRelief.PlanarDistance(Game.World.Player.position,HexWorldGrid.Center(b.column,b.row))<5);
        public void Craft(string recipe)
        {
            if(Game.Adventure.InBattle||Fishing)return;if(!CanCraftHere){Game.ShowMessage("走到小屋、营地或已建工坊旁制作。");return;}
            int equipment=EquipmentCatalog.Index(recipe);if(equipment>=0){EquipmentCatalog.Craft(Game,equipment);return;}
            var a=Game.State.adventure;bool made=false;string result="";
            switch(recipe)
            {
                case "bait":if(State.fiber>=2&&a.berries>=1){State.fiber-=2;a.berries--;State.bait+=3;made=true;result="鱼饵+3，抛竿时自动使用一份，扩大收线窗口。";}break;
                case "rod":if(!State.fishingRod&&a.workshop&&a.wood>=8&&State.fiber>=4&&a.science>=2){a.wood-=8;State.fiber-=4;a.science-=2;State.fishingRod=true;made=true;result="精制鱼竿完成，收线窗口永久扩大。";}break;
                case "boots":if(!State.marshBoots&&a.workshop&&State.fiber>=8&&a.wood>=4&&Game.State.coins>=20){State.fiber-=8;a.wood-=4;Game.State.coins-=20;State.marshBoots=true;made=true;result="湿地长靴完成：沼泽移动80%，雪地85%。";}break;
                case "amulet":if(!State.starAmulet&&a.library&&State.shards>=3&&a.crystals>=2&&a.science>=6){State.shards-=3;a.crystals-=2;a.science-=6;State.starAmulet=true;made=true;result="星辉护符完成：伙伴战斗体力+10、伤害+3。";}break;
                case "camp":if(State.fiber>=4&&a.berries>=4&&a.fish>=1){State.fiber-=4;a.berries-=4;a.fish--;State.campSupplies++;made=true;result="野营补给+1，可在稳定陆地上休整回复体力15。";}break;
            }
            if(made)Game.Adventure.Commit(result);else Game.ShowMessage("材料或前置建筑不足，或装备已经制作。费用见制作面板。");
        }
        public void UseCamp()
        {
            if(Game.InCave){Game.ShowMessage("请先从矿洞出口返回营地。");return;}
            if(Game.Adventure.InBattle||Fishing)return;
            if(State.campSupplies==0||Game.State.energy>=FarmState.MaxEnergy){Game.ShowMessage("没有野营补给，或体力已充足。");return;}
            if(!Game.World.HexGrid.TryCell(Game.World.Player.position,out var cell)||!Game.World.HexGrid.CanBuildOn(cell)){Game.ShowMessage("选择稳定陆地休整，水面和沼泽不适合野营。");return;}
            State.campSupplies--;Game.State.RestoreEnergy(15);Game.Adventure.Commit("野营休整：体力回复15。时间与每日采集次数保持不变。");
        }
        public void ClaimQuest()
        {
            if(Game.Adventure.InBattle||Fishing||State.quest>=3)return;
            if(Game.Adventure.Target==null||Game.Adventure.Target.kind!=EncounterKind.Elder){Game.ShowMessage("回晨风村村长身边领取远征奖励。");return;}
            var a=Game.State.adventure;
            bool complete=State.quest==0?State.CacheCount>=2&&State.fishCaught>=3:State.quest==1?State.RuneCount>=3&&State.marshBoots:State.RuneCount==8&&State.starAmulet&&a.ruinsWon&&a.mountainWon&&a.marshWon;
            if(!complete){Game.ShowMessage(ExpeditionContent.Quests[State.quest]);return;}
            if(State.quest==0){Game.State.coins+=25;State.fiber+=4;State.bait+=3;}
            else if(State.quest==1){a.crystals+=2;a.science+=6;State.shards++;}
            else{Game.State.coins+=150;a.science+=15;}
            State.quest++;Game.Adventure.Commit("远征委托完成！村长送来了奖励。"+ExpeditionContent.Quests[State.quest]);
        }
        public void BeginFishing(WorldEncounter point)
        {
            if(Fishing||Game.Adventure.InBattle||!Nearby(point))return;
            if(!EquipmentCatalog.Require(Game,"basicRod"))return;
            if(Game.State.adventure.gatheredDay[point.id]==Game.State.day){Game.ShowMessage("这个钓点今天已钓获，明天再来。");return;}
            if(!Game.State.SpendEnergy(2)){Game.ShowMessage("抛竿需要体力2。");return;}
            Game.Construction.Cancel();Game.Fences.Cancel();fishingId=point.id;usingBait=State.bait>0;if(usingBait)State.bait--;
            FishHits=FishMisses=0;ZoneWidth=.24f+(State.fishingRod?.1f:0)+(usingBait?.1f:0);ZoneCenter=UnityEngine.Random.Range(.3f,.7f);
            fishingStarted=Time.unscaledTime;readyAt=fishingStarted+1.2f;deadline=fishingStarted+18;Fishing=true;
            CreateBobber(point);
            Game.Adventure.Commit("抛竿！等待鱼咬钩。"+(usingBait?"已使用鱼饵1。":""));Game.Adventure.Hud.Show("fishing");
        }
        public void Reel()
        {
            if(!ReelReady)return;
            if(Mathf.Abs(Cursor-ZoneCenter)<=ZoneWidth*.5f)FishHits++;else FishMisses++;
            if(FishHits>=3)
            {
                int amount=(Game.State.adventure.companions[1]?3:2)+(FishMisses==0?1:0);Game.State.adventure.fish+=amount;State.fishCaught+=amount;Game.State.weather.survivalExperience+=2;
                if(FishMisses==0)State.shards++;Game.State.adventure.gatheredDay[fishingId]=Game.State.day;Fishing=false;ClearBobber();Game.Adventure.Hud.Hide();
                Game.Adventure.Commit("钓鱼成功！鲜鱼+"+amount+(FishMisses==0?"，完美收线发现遗迹碎片+1。":"。"));return;
            }
            if(FishMisses>=3){FinishFailed("鱼挣脱了鱼线。制作鱼饵或精制鱼竿可扩大绿色窗口。");return;}
            ZoneCenter=UnityEngine.Random.Range(.3f,.7f);readyAt=Time.unscaledTime+.6f;
        }
        void Update()
        {
            if(!Fishing)return;
            if(bobber){bobber.position=bobberWater+Vector3.up*(.16f+Mathf.Sin(Time.unscaledTime*5)*.025f);fishingLine.SetPosition(0,Game.World.Player.position+Vector3.up*.95f);fishingLine.SetPosition(1,bobber.position);}
            if(Time.unscaledTime>=deadline)FinishFailed("等待太久，鱼已经游走。今天仍可再次尝试。");
        }
        void CreateBobber(WorldEncounter point)
        {
            float distance=float.MaxValue;bobberWater=point.Position;
            for(int r=-20;r<=20;r++)for(int c=-23;c<=22;c++)
            {var cell=new Vector2Int(c,r);if(!Game.World.HexGrid.Contains(cell)||Game.World.HexGrid.TerrainAt(cell)!=TerrainKind.Water)continue;var p=HexWorldGrid.Center(c,r);float d=Mathf.Pow(WorldRelief.PlanarDistance(p,point.Position),2);if(d<distance){distance=d;bobberWater=p;bobberWater.y=Game.World.HexGrid.GetComponent<ContinuousTerrain>().WaterLevelAt(p);}}
            var root=GameObject.CreatePrimitive(PrimitiveType.Sphere);root.name="Fishing float";root.transform.SetParent(transform,false);root.transform.position=bobberWater+Vector3.up*.16f;root.transform.localScale=Vector3.one*.18f;root.GetComponent<Collider>().enabled=false;root.GetComponent<Renderer>().sharedMaterial=bobberMaterial;bobber=root.transform;
            fishingLine=root.AddComponent<LineRenderer>();fishingLine.positionCount=2;fishingLine.startWidth=fishingLine.endWidth=.012f;fishingLine.useWorldSpace=true;fishingLine.sharedMaterial=lineMaterial;
        }
        void ClearBobber(){if(bobber){bobber.gameObject.SetActive(false);Destroy(bobber.gameObject);}bobber=null;fishingLine=null;}

        void FinishFailed(string text){Fishing=false;ClearBobber();Game.Adventure.Hud.Hide();Game.Adventure.Commit(text);}
        public void CancelFishing(){if(!Fishing)return;Fishing=false;ClearBobber();Game.Adventure.Hud.Hide();Game.Adventure.Commit("收起鱼竿。已消耗的体力和鱼饵不返还。");}
        public string FishingText=>"成功收线 "+FishHits+" / 3   失误 "+FishMisses+" / 3\n剩余 "+Mathf.CeilToInt(FishingSeconds)+" 秒\n\n"+(ReelReady?"鱼已咬钩，观察收线时机。":"等待鱼咬钩……")+"\n\n三次成功捕获；三次失误鱼会逃走。\n完美收线额外获得鲜鱼和遗迹碎片。";
        public string NextLandmark=>expeditionWorld?expeditionWorld.NextLandmark():"";
        public void RefreshVisuals(){if(expeditionWorld)expeditionWorld.Refresh();}
    }
}
