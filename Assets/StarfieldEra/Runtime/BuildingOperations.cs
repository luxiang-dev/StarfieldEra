using System;
using System.Linq;
namespace StarfieldEra
{
    // All forecasts and buttons use the same daily production and affordability rules.
    public static class BuildingOperations
    {
        public static bool Produces(int kind)=>kind>=0&&kind<2||kind==6;
        public static string UpgradeBlock(FarmState farm,ResourceSiteState site)
        {
            if(site==null)return "这座建筑无需升级";
            if(site.level>=3)return "已达到三级上限";
            Costs(site.level,out var wood,out var stone,out var gold);
            string missing="";
            if(farm.adventure.wood<wood)missing+="木材差"+(wood-farm.adventure.wood)+" ";
            if(farm.adventure.stone<stone)missing+="石料差"+(stone-farm.adventure.stone)+" ";
            if(farm.coins<gold)missing+="金币差"+(gold-farm.coins)+" ";
            if(site.level==2){if(farm.adventure.homestead.planks<2)missing+="木板差"+(2-farm.adventure.homestead.planks)+" ";if(farm.adventure.homestead.bricks<2)missing+="石砖差"+(2-farm.adventure.homestead.bricks);}
            return missing.Trim();
        }
        public static void Costs(int level,out int wood,out int stone,out int gold)
        {wood=level==0?4:level==1?8:12;stone=level==0?4:level==1?6:10;gold=level==0?10:level==1?20:35;}
        public static string UpgradeCost(ResourceSiteState s)
        {if(s==null||s.level>=3)return "已达到等级上限";Costs(s.level,out var w,out var st,out var g);return "木材 "+w+" · 石料 "+st+" · 金币 "+g+(s.level==2?" · 木板 2 · 石砖 2":"");}
        public static ResourceSiteState Forecast(ResourceSiteState site,int capacity,float productivity=1)
        {var copy=new ResourceSiteState{level=site.level,workers=site.workers,stock=site.stock,goods=site.goods,processing=site.processing,damage=site.damage,downtime=site.downtime};copy.NewDay(capacity,productivity);return copy;}
        public static int WorkshopOutput(ResourceSiteState s,int materials,int capacity,float productivity=1)
        {if(s==null||s.level==0||s.damage>=100||s.downtime>0)return 0;return Math.Max(0,Math.Min((int)Math.Floor(s.workers*s.level*2*productivity+.0001f),Math.Min(materials/2,capacity-(s.processing?s.goods:s.stock))));}
        public static void ProduceWorkshops(FarmState farm)
        {
            var a=farm.adventure;var h=a.homestead;int capacity=h.StorageCapacity(a);
            // Preserve the existing automatic workshop income; staffed processing is additional.
            foreach(var s in h.Workshops(a))
            {
                if(s.downtime>0){s.downtime--;continue;}if(s.damage>=100)continue;
                a.wood+=s.level*6;
                int amount=WorkshopOutput(s,s.processing?a.stone:a.wood,capacity,a.village.Productivity(a,s));
                if(s.processing){a.stone-=amount*2;s.goods+=amount;}else{a.wood-=amount*2;s.stock+=amount;}
            }
        }
        public static bool OwnsWorkshop(AdventureState a,ResourceSiteState s)=>s!=null&&a.homestead.Workshops(a).Contains(s);
        public static bool ImproveWorkshop(FarmState farm,ResourceSiteState s)
        {
            if(!OwnsWorkshop(farm.adventure,s)||UpgradeBlock(farm,s)!="")return false;
            Costs(s.level,out var wood,out var stone,out var gold);farm.adventure.wood-=wood;farm.adventure.stone-=stone;farm.coins-=gold;
            if(s.level==2){farm.adventure.homestead.planks-=2;farm.adventure.homestead.bricks-=2;}s.level++;return true;
        }
        public static bool AssignWorkshop(AdventureState a,ResourceSiteState s,int delta)
        {
            if(!OwnsWorkshop(a,s)||Math.Abs(delta)!=1||s.workers+delta<0||s.workers+delta>2||(delta>0&&a.homestead.UsedWorkers(a)>=a.homestead.WorkerCapacity(a)))return false;
            return a.village.Adjust(a,s,delta);
        }
        public static void CollectWorkshop(AdventureState a,ResourceSiteState s)
        {if(!OwnsWorkshop(a,s))return;a.homestead.planks+=s.stock;a.homestead.bricks+=s.goods;s.stock=s.goods=0;}
    }
}
