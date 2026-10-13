using System;
using System.Linq;
using System.Collections.Generic;

namespace StarfieldEra
{
    [Serializable]
    public sealed class AdventureState
    {
        public int wood, stone, berries, fish, crystals, science;
        public int quest;
        public bool workshop, library, irrigation, lore, ruinsWon;
        public bool expandedFarm, mill, observatory, mountainWon, marshWon;
        public int toolLevel, meals, deliveries;
        public int deliveredDay=-1;
        public List<PlacedBuilding> buildings = new List<PlacedBuilding>();
        public int[] bossDay = {-1,-1,-1};
        public bool[] companions = new bool[3];
        public int activeCompanion;
        public int[] gatheredDay = new int[32];
        public bool[] discovered = new bool[8];
        public int victories;
        public int guardianDay = -1;
        public AdventureState() { for (int i=0;i<32;i++) gatheredDay[i]=-1; discovered[0]=true; }
        public void Upgrade()
        {
            if(buildings==null)buildings=new List<PlacedBuilding>();
            if(gatheredDay!=null && gatheredDay.Length==12)
            {var expanded=new int[32];for(int i=0;i<32;i++)expanded[i]=-1;Array.Copy(gatheredDay,expanded,12);gatheredDay=expanded;}
            if(discovered!=null && discovered.Length==4)
            {var expanded=new bool[8];Array.Copy(discovered,expanded,4);discovered=expanded;}
            if(bossDay==null || bossDay.Length!=3)bossDay=new[]{guardianDay,-1,-1};
            bossDay[0]=Math.Max(bossDay[0],guardianDay);
        }
        public int CompanionCount => companions.Count(x=>x);
        public bool Valid() => wood>=0 && stone>=0 && berries>=0 && fish>=0 && crystals>=0 && science>=0 &&
            quest>=0 && quest<=6 && companions!=null && companions.Length==3 && gatheredDay!=null && gatheredDay.Length==32 &&
            discovered!=null && discovered.Length==8 && activeCompanion>=0 && activeCompanion<3 && victories>=0 && toolLevel>=0 && toolLevel<=1 && meals>=0 && deliveries>=0 && bossDay!=null && bossDay.Length==3 && buildings!=null && buildings.Count<=80 && buildings.All(b=>b!=null&&b.kind>=0&&b.kind<4&&b.rotation>=0&&b.rotation<6&&b.row>=-20&&b.row<=20&&b.column>=-23&&b.column<=22&&Math.Abs(HexWorldGrid.Center(b.column,b.row).x)<=HexWorldGrid.HalfWidth-1&&Math.Abs(HexWorldGrid.Center(b.column,b.row).z)<=HexWorldGrid.HalfDepth-1) && buildings.Select(b=>b.column+":"+b.row).Distinct().Count()==buildings.Count && BuildingFootprints.Valid(buildings);
        public void NewDay(FarmState farm)
        {
            foreach(var b in buildings){if(b.kind==0)wood+=2;else if(b.kind==1)stone+=2;else if(b.kind==2)science++;}
            if(workshop) wood+=2;
            if(library) science+=2;
            if(mill)farm.coins+=6;
            if(observatory)science+=4;
            int watered=irrigation?(expandedFarm?12:6):0;
            if(companions[0]) watered=Math.Max(watered,3);
            for(int i=0;i<Math.Min(watered,FarmState.PlotCount);i++)
                if(farm.plots[i].planted && !farm.plots[i].Ready) farm.plots[i].watered=true;
        }
    }

    public static class AdventureContent
    {
        public static readonly string[] Names={"苗芽鹿","潮汐獭","焰晶狐"};
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
