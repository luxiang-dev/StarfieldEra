using System;
using System.Linq;
namespace StarfieldEra
{
    [Serializable] public sealed class EquipmentState
    {
        public bool[] owned=new bool[15],blueprints=new bool[15];
        public int version;
        public EquipmentState(){for(int i=0;i<6;i++)owned[i]=blueprints[i]=true;version=1;}
        public void Upgrade(ExpeditionState old)
        {
            if(owned==null)owned=new bool[15];if(blueprints==null)blueprints=new bool[15];
            Array.Resize(ref owned,15);Array.Resize(ref blueprints,15);
            if(version==0){for(int i=0;i<6;i++)owned[i]=blueprints[i]=true;version=1;}
            if(old.fishingRod){owned[7]=owned[9]=true;}if(old.marshBoots)owned[10]=true;if(old.starAmulet)owned[11]=true;
            for(int i=0;i<15;i++)if(owned[i])blueprints[i]=true;
        }
        public bool Valid()=>version==1&&owned!=null&&owned.Length==15&&blueprints!=null&&blueprints.Length==15&&Enumerable.Range(0,15).All(i=>!owned[i]||blueprints[i]);
    }
    public sealed class EquipmentRecipe
    {
        public string key,name,effect;public int wood,stone,fiber,gold,science,shards,crystals,plan,tech;
        public EquipmentRecipe(string k,string n,string e,int w,int s,int f,int g,int bp,int t=0,int sc=0,int sh=0,int c=0)
        {key=k;name=n;effect=e;wood=w;stone=s;fiber=f;gold=g;plan=bp;tech=t;science=sc;shards=sh;crystals=c;}
        public string Cost=>"木材"+wood+" · 石料"+stone+" · 纤维"+fiber+"\n金币"+gold+" · 知识"+science+(shards>0?" · 碎片"+shards:"")+(crystals>0?" · 晶石"+crystals:"");
    }
    public static class EquipmentCatalog
    {
        public static readonly EquipmentRecipe[] Recipes={
            new EquipmentRecipe("hoe","农用锄头","开垦农田、播种与覆草复绿。初始农具。",3,2,0,0,0),
            new EquipmentRecipe("can","浇水壶","为农田和幼树浇水。初始农具。",2,2,0,0,0),
            new EquipmentRecipe("axe","伐木斧","采集木材。初始农具。",3,3,0,0,0),
            new EquipmentRecipe("pick","采矿镐","开采石料与晶石。初始农具。",3,4,0,0,0),
            new EquipmentRecipe("hammer","建造锤","建造和拆除建筑、围栏与牧场门。初始农具。",4,2,0,0,0),
            new EquipmentRecipe("sickle","收割镰刀","收获农作物与采集野生纤维。初始农具。",2,3,0,0,0),
            new EquipmentRecipe("shovel","工程铁铲","挖河、引水、填埋和种植树苗。",6,5,2,8,12),
            new EquipmentRecipe("basicRod","木制鱼竿","解锁湖岸钓鱼，精制鱼竿可进一步升级。",4,0,2,0,6),
            new EquipmentRecipe("forester","育林手套","保护幼苗根系，与铁铲配合植树造林。",0,0,4,5,8),
            new EquipmentRecipe("rod","精制鱼竿","替代木制鱼竿并扩大收线窗口。",8,0,4,0,18,1,2),
            new EquipmentRecipe("boots","湿地长靴","沼泽速度80%，雪地85%。不能踏水。",4,0,8,20,20,1),
            new EquipmentRecipe("amulet","星辉护符","伙伴体力+10，伤害+3。",0,0,0,0,30,2,6,3,2),
            new EquipmentRecipe("lantern","远行提灯","夜间或矿洞自动照明8米，解除缺光时25%的移动惩罚。",6,3,2,15,10,1),
            new EquipmentRecipe("raincoat","防雨斗篷","阻止雨天淋湿积累。湿透会降低移动速度并消耗体力；下水仍会浸湿。",2,0,10,20,12,1),
            new EquipmentRecipe("coat","保暖外套","雪地与夜间山地防寒，缓慢消除寒冷。寒冷会降低移动速度，极寒持续损失生命。",0,0,12,25,15,1)};
        public static EquipmentState State(FarmGame g)=>g.State.adventure.expedition.equipment;
        public static int Index(string key)=>Array.FindIndex(Recipes,r=>r.key==key);
        public static bool Has(FarmGame g,string key){int i=Index(key);return i<0||State(g).owned[i]||(key=="basicRod"&&g.Expedition.State.fishingRod);}
        public static bool Require(FarmGame g,string key)
        {if(Has(g,key))return true;g.ShowMessage("需要持有"+Recipes[Index(key)].name+"。打开制作 → 装备图纸，学习图纸后制作。");return false;}
        public static string LandKey(int action)=>action==0||action==4?"hoe":(action>=1&&action<=3)||action>=6?"shovel":"";
        public static string LandMissing(FarmGame g,int action)
        {string k=LandKey(action);if(k!=""&&!Has(g,k))return "需要持有"+Recipes[Index(k)].name;if(action==1&&!Has(g,"forester"))return "需要持有育林手套";return "";}
        public static string Block(FarmGame g,int i)
        {
            if(i<0||i>=Recipes.Length)return "无效配方";var r=Recipes[i];var a=g.State.adventure;var x=a.expedition;
            if(State(g).owned[i])return "已经持有";
            if(!State(g).blueprints[i])return "尚未学习图纸";
            if(!g.Expedition.CanCraftHere)return "请走近小屋、营地或工坊";
            if(r.tech==1&&!a.workshop)return "需要先建造工坊";if(r.tech==2&&!a.library)return "需要先建造书院";
            if(r.key=="rod"&&!Has(g,"basicRod"))return "需要先制作木制鱼竿";
            if(a.wood<r.wood||a.stone<r.stone||x.fiber<r.fiber||g.State.coins<r.gold||a.science<r.science||x.shards<r.shards||a.crystals<r.crystals)return "制作材料不足";
            return "";
        }
        public static bool PlanLocation(FarmGame g)=>g.NearHouse||g.Adventure.Target?.kind==EncounterKind.Market||g.Adventure.Target?.kind==EncounterKind.Elder;
        public static void Learn(FarmGame g,int i)
        {
            if(g.Adventure.InBattle||g.Expedition.Fishing||i<0||i>=Recipes.Length)return;
            var s=State(g);var r=Recipes[i];if(s.blueprints[i]){g.ShowMessage("已经学习这张图纸。");return;}
            if(!PlanLocation(g)){g.ShowMessage("到小屋、商人或村长旁获取图纸。");return;}
            if(g.State.coins<r.plan){g.ShowMessage("学习图纸需要金币"+r.plan+"。");return;}
            g.State.coins-=r.plan;s.blueprints[i]=true;g.Adventure.Commit("已学习"+r.name+"图纸，现在可收集材料制作。");
        }
        public static void Craft(FarmGame g,int i)
        {
            if(g.Adventure.InBattle||g.Expedition.Fishing)return;string why=Block(g,i);if(why!=""){g.ShowMessage(why);return;}
            var r=Recipes[i];var a=g.State.adventure;var x=a.expedition;a.wood-=r.wood;a.stone-=r.stone;x.fiber-=r.fiber;g.State.coins-=r.gold;a.science-=r.science;x.shards-=r.shards;a.crystals-=r.crystals;State(g).owned[i]=true;
            if(r.key=="rod")x.fishingRod=true;if(r.key=="boots")x.marshBoots=true;if(r.key=="amulet")x.starAmulet=true;
            g.Adventure.Commit("制作完成："+r.name+"。持有即可用于对应操作。");
        }
    }
}
