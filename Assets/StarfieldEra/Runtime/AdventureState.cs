using System;
using System.Linq;

namespace StarfieldEra
{
    [Serializable]
    public sealed class AdventureState
    {
        public int wood, stone, berries, fish, crystals, science;
        public int quest;
        public bool workshop, library, irrigation, lore, ruinsWon;
        public bool[] companions = new bool[3];
        public int activeCompanion;
        public int[] gatheredDay = new int[12];
        public bool[] discovered = new bool[4];
        public int victories;
        public int guardianDay = -1;
        public AdventureState() { for (int i=0;i<12;i++) gatheredDay[i]=-1; discovered[0]=true; }
        public int CompanionCount => companions.Count(x=>x);
        public bool Valid() => wood>=0 && stone>=0 && berries>=0 && fish>=0 && crystals>=0 && science>=0 &&
            quest>=0 && quest<=3 && companions!=null && companions.Length==3 && gatheredDay!=null && gatheredDay.Length==12 &&
            discovered!=null && discovered.Length==4 && activeCompanion>=0 && activeCompanion<3 && victories>=0;
        public void NewDay(FarmState farm)
        {
            if(workshop) wood+=2;
            if(library) science+=2;
            int watered=irrigation?6:0;
            if(companions[0]) watered=Math.Max(watered,3);
            for(int i=0;i<Math.Min(watered,FarmState.PlotCount);i++)
                if(farm.plots[i].planted && !farm.plots[i].Ready) farm.plots[i].watered=true;
        }
    }

    public static class AdventureContent
    {
        public static readonly string[] Names={"苗芽鹿","潮汐獭","焰晶狐"};
        public static readonly string[] Roles={"草系 · 每天照料前三块农田","水系 · 钓鱼额外获得一条鱼","火系 · 战斗攻击增加三点"};
        public static readonly string[] Regions={"晨风村","青苔森林","镜湖岸","星辉遗迹"};
        public static readonly string[] Quests={
            "村长的委托：累计收获三次作物，回村领取奖励。",
            "聚落的伙伴：招募一位伙伴，建造工坊，再向村长汇报。",
            "失落的星辉：击败遗迹守卫，研究灌溉技术，再回村长身边。",
            "晨风村复兴了！继续探索、培养伙伴，积累下一纪元的资源。"
        };
    }
}
