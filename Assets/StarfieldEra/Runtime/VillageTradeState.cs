using System;
using System.Collections.Generic;
using System.Linq;
namespace StarfieldEra
{
    [Serializable] public sealed class VillageOrder {public int cycle,template,status;}
    public sealed class OrderRecipe
    {
        public string name;public int wood,stone,planks,bricks,carrots,fish,fiber,berries,crystals,gold,reputation;
        public string Needs=>"木材 "+wood+" · 石料 "+stone+"\n木板 "+planks+" · 石砖 "+bricks+"\n胡萝卜 "+carrots+" · 鲜鱼 "+fish+"\n纤维 "+fiber+" · 浆果 "+berries+" · 晶石 "+crystals;
        public bool CanPay(FarmState f){var a=f.adventure;var h=a.homestead;return a.wood>=wood&&a.stone>=stone&&h.planks>=planks&&h.bricks>=bricks&&h.carrots>=carrots&&a.fish>=fish&&a.expedition.fiber>=fiber&&a.berries>=berries&&a.crystals>=crystals;}
        public void Pay(FarmState f){var a=f.adventure;var h=a.homestead;a.wood-=wood;a.stone-=stone;h.planks-=planks;h.bricks-=bricks;h.carrots-=carrots;a.fish-=fish;a.expedition.fiber-=fiber;a.berries-=berries;a.crystals-=crystals;}
    }
    [Serializable] public sealed class VillageTradeState
    {
        public int cycle=-1,completed,expired,caravanCycle=-1;
        public int[] purchases=new int[6];public List<VillageOrder> orders=new List<VillageOrder>();
        public static readonly OrderRecipe[] Recipes={
            new OrderRecipe{name="修桥木板",planks=6,gold=45,reputation=3},new OrderRecipe{name="矿道哨塔石砖",bricks=6,gold=60,reputation=4},new OrderRecipe{name="居民房屋备料",wood=12,stone=8,gold=40,reputation=3},
            new OrderRecipe{name="村民餐桌补给",carrots=8,fish=3,gold=40,reputation=3},new OrderRecipe{name="驿站纤维与浆果",fiber=10,berries=6,gold=35,reputation=2},new OrderRecipe{name="观星台晶石",crystals=2,planks=4,gold=70,reputation=5}};
        public static readonly string[] Goods={"星辉南瓜种子 ×3","工程铁铲图纸","育林手套图纸","湿地长靴图纸","村庄旗帜 ×1","村民口粮 ×6"};
        public static readonly int[] Prices={35,16,18,30,20,12},Limits={2,1,1,1,3,3};
        public static bool CaravanPresent(int day)=>(day-1)%4<2;
        public static int NextCaravan(int day)=>CaravanPresent(day)?day:day+(4-(day-1)%4);
        public void Ensure(int day)
        {
            if(orders==null)orders=new List<VillageOrder>();if(orders.Any(o=>o==null))return;if(purchases==null||purchases.Length!=6)purchases=new int[6];int now=(day-1)/4;
            if(cycle!=now){foreach(var o in orders)if(o.status<2){o.status=3;expired++;}cycle=now;for(int i=0;i<3;i++)orders.Add(new VillageOrder{cycle=now,template=(now*2+i)%6});while(orders.Count>18)orders.RemoveAt(0);}
            if(caravanCycle!=now){caravanCycle=now;Array.Clear(purchases,0,6);}
        }
        public bool Accept(int day,VillageOrder order)
        {Ensure(day);if(order==null||!orders.Contains(order)||order.cycle!=cycle||order.status!=0)return false;order.status=1;return true;}
        public bool Deliver(FarmState f,VillageOrder o)
        {
            Ensure(f.day);if(o==null||!orders.Contains(o)||o.cycle!=cycle||o.status!=1)return false;var recipe=Recipes[o.template];if(!recipe.CanPay(f))return false;
            recipe.Pay(f);o.status=2;completed++;f.coins+=recipe.gold;f.adventure.village.reputation+=recipe.reputation;f.weather.survivalExperience+=5;
            if(o.template==0)f.adventure.frontier.bridgeCredit=true;if(o.template==1)f.adventure.frontier.towerCredit=true;return true;
        }
        public string BuyBlock(FarmState f,int offer)
        {
            if(offer<0||offer>=6)return "无效商品";if(!CaravanPresent(f.day))return "商队尚未抵达";
            if(purchases[offer]>=Limits[offer])return "本期已经售罄";if(f.coins<Prices[offer])return "金币不足";
            int plan=offer==1?6:offer==2?8:10;if(offer>=1&&offer<=3&&f.adventure.expedition.equipment.blueprints[plan])return "已经掌握这张图纸";
            if(offer==4&&f.adventure.village.banners>=99||offer==5&&f.adventure.village.pantry>99993)return "库存已满";return "";
        }
        public bool Buy(FarmState f,int offer)
        {
            Ensure(f.day);if(BuyBlock(f,offer)!="")return false;f.coins-=Prices[offer];purchases[offer]++;
            if(offer==0)f.starSeeds+=3;else if(offer>=1&&offer<=3)f.adventure.expedition.equipment.blueprints[offer==1?6:offer==2?8:10]=true;
            else if(offer==4)f.adventure.village.banners++;else f.adventure.village.pantry+=6;return true;
        }
        public bool Valid()=>cycle>=-1&&completed>=0&&expired>=0&&caravanCycle>=-1&&purchases!=null&&purchases.Length==6&&purchases.Select((p,i)=>p>=0&&p<=Limits[i]).All(v=>v)&&orders!=null&&orders.Count<=18&&orders.All(o=>o!=null&&o.cycle>=0&&o.template>=0&&o.template<6&&o.status>=0&&o.status<=3)&&orders.Select(o=>o.cycle+":"+o.template).Distinct().Count()==orders.Count;
    }
    [Serializable] public sealed class VillageDecoration {public int column,row,rotation;}
}
