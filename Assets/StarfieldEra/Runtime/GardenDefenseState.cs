using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace StarfieldEra
{
    [Serializable] public sealed class GardenUnit
    {
        public int id,kind,health,tier;public bool guardsAwake;public float x,z,yaw,armSeconds;
        public Vector3 Position=>new Vector3(x,0,z);
        public int MaxHealth=>kind==8?800:kind==7?60:kind==0?100:kind==1?300:kind==3?140:kind==4?110:kind==5?180:kind==6?150:200+tier*100;
        public bool Valid()=>id>=0&&kind>=0&&kind<=8&&tier>=0&&tier<=2&&health>0&&health<=MaxHealth&&new CampTrap{x=x,z=z}.Valid()&&SurvivalState.Finite(yaw)&&SurvivalState.Finite(armSeconds)&&armSeconds>=0&&armSeconds<=12;
    }
    [Serializable] public sealed class GardenZombie
    {
        public int id,health=60,tier,variant,sourceTomb=-1;public float x,z;
        public int MaxHealth=>(variant==1?120:variant==2?210:variant==3?360:60)+tier*30;
        public int AttackDamage=>(variant==3?24:10)+tier*4;
        public Vector3 Position=>new Vector3(x,0,z);
        public bool Valid()=>id>=0&&health>0&&health<=MaxHealth&&tier>=0&&tier<=2&&variant>=0&&variant<=3&&sourceTomb>=-1&&new CampTrap{x=x,z=z}.Valid();
    }
    [Serializable] public sealed class GardenDefenseState
    {
        public const int MaxZombies=48;
        public bool initialized;public int nextId,lastWaveDay=-1,kills,cemeteryVersion,clearedTombs;
        public List<GardenUnit> units=new List<GardenUnit>();public List<GardenZombie> zombies=new List<GardenZombie>();
        public static readonly int[] WoodCost={8,2,0,16,12,18,4,2,10},StoneCost={2,12,0,4,6,8,6,3,4},GoldCost={15,5,0,30,35,40,10,12,15};
        public void Upgrade(){if(units==null)units=new List<GardenUnit>();if(zombies==null)zombies=new List<GardenZombie>();}
        public bool Valid()=>nextId>=0&&cemeteryVersion>=0&&cemeteryVersion<=1&&clearedTombs>=0&&lastWaveDay>=-1&&kills>=0&&units!=null&&units.Count<=64&&units.All(u=>u!=null&&u.Valid())&&zombies!=null&&zombies.Count<=MaxZombies&&zombies.All(z=>z!=null&&z.Valid())&&units.Select(u=>u.id).Concat(zombies.Select(z=>z.id)).Distinct().Count()==units.Count+zombies.Count;
        public GardenUnit Add(int kind,Vector3 point,float yaw=0)
        {var u=new GardenUnit{id=nextId++,kind=kind,x=point.x,z=point.z,yaw=yaw};u.health=u.MaxHealth;units.Add(u);return u;}
        public GardenUnit Build(FarmState farm,int kind,Vector3 point,float yaw=0)
        {
            if(kind<0||kind>8||kind==2||units.Count>=64||farm.adventure.wood<WoodCost[kind]||farm.adventure.stone<StoneCost[kind]||farm.coins<GoldCost[kind]||units.Any(u=>WorldRelief.PlanarDistance(u.Position,point)<(u.kind==1||kind==1?2:1.4f)))return null;
            if(!new CampTrap{x=point.x,z=point.z}.Valid())return null;
            farm.adventure.wood-=WoodCost[kind];farm.adventure.stone-=StoneCost[kind];farm.coins-=GoldCost[kind];return Add(kind,point,yaw);
        }
        public static readonly string[] Names={"豌豆射手","防御城墙","墓碑","双发射手","寒冰射手","食人花","营地路灯","土豆地雷","坚果护卫"};
        public static readonly string[] Details={"每1.15秒发射豌豆，射程12米，每发伤害15。","城墙可以阻挡僵尸与豌豆。","","每1.15秒发射两颗豌豆，每颗伤害15，射程12米。","每1.5秒发射寒冰豌豆，伤害12，减速45%持续2.5秒。","吞咬2.8米内可见僵尸，伤害150，消化冷却6秒。","入夜自动点亮，照亮周围10米。光照范围内解除夜间探索减速。","埋设后12秒就绪，敌人进入1.6米触发，爆炸范围3.4米、伤害250；一次性，不伤玩家和友军，障碍可挡爆炸。","耐久800，阻挡僵尸。豌豆可以从坚果上方通过，适合保护后排射手。"};
        public static bool InRange(Vector3 a,Vector3 b,float range)=>WorldRelief.PlanarDistance(a,b)<=range;
        public static GardenZombie Target(Vector3 origin,IEnumerable<GardenZombie> zombies,float range)=>zombies.Where(z=>z.health>0&&InRange(origin,z.Position,range)).OrderBy(z=>(z.Position-origin).sqrMagnitude).FirstOrDefault();
    }
}
