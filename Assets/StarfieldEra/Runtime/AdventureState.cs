using System;
using System.Linq;
using System.Collections.Generic;

namespace StarfieldEra
{
    [Serializable]
    public sealed class AdventureState
    {
        public VillageState village=new VillageState();
        public VillageTradeState trade=new VillageTradeState();
        public FrontierState frontier=new FrontierState();
        public int wood, stone, berries, fish, crystals, science;
        public int quest;
        public int truffles;
        public CampDefenseState defense=new CampDefenseState();
        public HomesteadState homestead=new HomesteadState();
        public ExpeditionState expedition=new ExpeditionState();
        public List<FenceRecord> fences = new List<FenceRecord>();
        public List<AnimalRecord> animals = new List<AnimalRecord>();
        public int constructionVersion;
        public bool[] legacyTown=new bool[4];
        public bool workshop, library, irrigation, lore, ruinsWon;
        public bool expandedFarm, mill, observatory, mountainWon, marshWon;
        public int toolLevel, meals, deliveries;
        public int deliveredDay=-1;
        public List<PlacedBuilding> buildings = new List<PlacedBuilding>();
        public int[] bossDay = {-1,-1,-1};
        public bool[] companions = new bool[3];
        public int activeCompanion;
        public int[] gatheredDay = new int[64];
        public bool[] discovered = new bool[8];
        public int victories;
        public int guardianDay = -1;
        public AdventureState() { for (int i=0;i<64;i++) gatheredDay[i]=-1; discovered[0]=true; }
        public void Upgrade()
        {
            if(constructionVersion==0)
            {legacyTown=new[]{workshop,library,mill,observatory};constructionVersion=1;}
            if(defense==null)defense=new CampDefenseState();defense.Upgrade();
            if(homestead==null)homestead=new HomesteadState();homestead.Upgrade();
            if(expedition==null)expedition=new ExpeditionState();
            if(expedition.equipment==null)expedition.equipment=new EquipmentState();expedition.equipment.Upgrade(expedition);
            if(fences==null)fences=new List<FenceRecord>();
            if(animals==null)animals=new List<AnimalRecord>();
            if(buildings==null)buildings=new List<PlacedBuilding>();
            foreach(var b in buildings)if(b!=null&&(b.kind<2||b.kind==6)&&(b.operation==null||b.operation.level==0))b.operation=new ResourceSiteState{level=1};
            if(gatheredDay!=null && (gatheredDay.Length==12||gatheredDay.Length==32))
            {var expanded=new int[64];for(int i=0;i<64;i++)expanded[i]=-1;Array.Copy(gatheredDay,expanded,gatheredDay.Length);gatheredDay=expanded;}
            if(discovered!=null && discovered.Length==4)
            {var expanded=new bool[8];Array.Copy(discovered,expanded,4);discovered=expanded;}
            if(bossDay==null || bossDay.Length!=3)bossDay=new[]{guardianDay,-1,-1};
            bossDay[0]=Math.Max(bossDay[0],guardianDay);
            ReconcileTown();
            if(village==null)village=new VillageState();if(trade==null)trade=new VillageTradeState();if(frontier==null)frontier=new FrontierState();
            village.Ensure(this);
        }
        public bool FixedTown(int kind)
        {
            int i=kind==6?0:kind==7?1:kind==8?2:kind==9?3:-1;
            if(i<0)return false;
            return constructionVersion==0?(i==0?workshop:i==1?library:i==2?mill:observatory):legacyTown!=null&&legacyTown.Length==4&&legacyTown[i];
        }
        public int TownCount(int kind)=>buildings.Count(b=>b.kind==kind)+(FixedTown(kind)?1:0);
        public void ReconcileTown()
        {workshop=TownCount(6)>0;library=TownCount(7)>0;mill=TownCount(8)>0;observatory=TownCount(9)>0;}
        public int CompanionCount => companions.Count(x=>x);
        public bool Valid() => village!=null&&village.Valid()&&trade!=null&&trade.Valid()&&frontier!=null&&frontier.Valid()&&constructionVersion>=0&&constructionVersion<=1&&legacyTown!=null&&legacyTown.Length==4&&wood>=0 && stone>=0 && berries>=0 && fish>=0 && crystals>=0 && science>=0 &&
            homestead!=null && homestead.Valid(this) && defense!=null && defense.Valid(this) && expedition!=null && expedition.Valid() && truffles>=0 && PastureFences.Valid(fences) && animals!=null && animals.Count<=3 && animals.All(a=>a!=null&&a.id>=0&&a.id<3&&a.fedDay>=-1&&a.homeColumn>=HexWorldGrid.MinColumn&&a.homeColumn<=HexWorldGrid.MaxColumn&&a.homeRow>=HexWorldGrid.MinRow&&a.homeRow<=HexWorldGrid.MaxRow&&!float.IsNaN(a.x)&&!float.IsInfinity(a.x)&&!float.IsNaN(a.z)&&!float.IsInfinity(a.z)&&Math.Abs(a.x)<=HexWorldGrid.HalfWidth&&Math.Abs(a.z)<=HexWorldGrid.HalfDepth) && animals.Select(a=>a.id).Distinct().Count()==animals.Count && quest>=0 && quest<=6 && companions!=null && companions.Length==3 && gatheredDay!=null && gatheredDay.Length==64 &&
            discovered!=null && discovered.Length==8 && activeCompanion>=0 && activeCompanion<3 && victories>=0 && toolLevel>=0 && toolLevel<=1 && meals>=0 && deliveries>=0 && bossDay!=null && bossDay.Length==3 && buildings!=null && buildings.Count<=80 && buildings.All(b=>b!=null&&b.kind>=0&&b.kind<HexConstruction.Names.Length&&(b.kind>=2||b.operation!=null)&&b.rotation>=0&&b.rotation<6&&b.row>=HexWorldGrid.MinRow&&b.row<=HexWorldGrid.MaxRow&&b.column>=HexWorldGrid.MinColumn&&b.column<=HexWorldGrid.MaxColumn&&Math.Abs(HexWorldGrid.Center(b.column,b.row).x)<=HexWorldGrid.HalfWidth-1&&Math.Abs(HexWorldGrid.Center(b.column,b.row).z)<=HexWorldGrid.HalfDepth-1) && buildings.Select(b=>b.column+":"+b.row).Distinct().Count()==buildings.Count && BuildingFootprints.Valid(buildings);
        public void NewDay(FarmState farm)
        {
            foreach(var a in animals)if(a.fedDay==farm.day-1)truffles++;
            village.BeginDay(farm);trade.Ensure(farm.day);
            homestead.NewDay(farm);
            foreach(var b in buildings)if(b.kind==2)science+=3;
            foreach(var b in buildings)if(b.kind==10)farm.coins+=10;else if(b.kind==3)meals+=2;else if(b.kind==11)homestead.carrots+=3;
            BuildingOperations.ProduceWorkshops(farm);
            village.EndDay(farm.day);
            science+=TownCount(7)*6+TownCount(9)*12;
            farm.coins+=TownCount(8)*18;
            int watered=irrigation?(expandedFarm?12:6):0;
            if(companions[0]) watered=Math.Max(watered,3);
            for(int i=0;i<Math.Min(watered,FarmState.PlotCount);i++)
                if(farm.plots[i].planted && !farm.plots[i].Ready) farm.plots[i].watered=true;
        }
    }

    public static class AdventureContent
    {
        public static readonly string[] Names={"苗芽","潮汐獭","焰晶狐"};
        public static readonly string[] Roles={"草系 · 每天照料前三块农田","水系 · 钓鱼额外获得一条鱼","火系 · 战斗攻击增加三点"};
        public static readonly string[] Regions={"晨风村","青苔森林","镜湖岸","星辉遗迹","金穗平原","白岩山地","幽光湿地","远行驿站"};
        public static readonly string[] Quests={
            "村长的委托：累计收获三次作物，回村领取奖励。",
            "聚落的伙伴：招募一位伙伴，建造工坊，再向村长汇报。",
            "失落的星辉：击败遗迹守卫，研究灌溉技术，再回村长身边。",
            "金穗拓荒：累计收获八次作物，扩建农场并建造磨坊，回村领取奖励。",
            "越过白岩：发现全部八个区域，击败白岩山地的守卫，回村汇报。",
            "星野纪元：招募全部伙伴，建设观星台并研究星辉学，再回村领取奖励。",
            "新的纪元开启了！继续经营农场、远行贸易并挑战每日守卫。"
        };
    }
}
