using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace StarfieldEra
{
    [Serializable] public sealed class GardenUnit
    {
        public int id,kind,health,tier;public bool guardsAwake;public float x,z,yaw;
        public Vector3 Position=>new Vector3(x,0,z);
        public int MaxHealth=>kind==0?100:kind==1?300:200+tier*100;
        public bool Valid()=>id>=0&&kind>=0&&kind<=2&&tier>=0&&tier<=2&&health>0&&health<=MaxHealth&&new CampTrap{x=x,z=z}.Valid()&&SurvivalState.Finite(yaw);
    }
    [Serializable] public sealed class GardenZombie
    {
        public int id,health=60,tier,sourceTomb=-1;public float x,z;
        public int MaxHealth=>60+tier*30;
        public int AttackDamage=>10+tier*4;
        public Vector3 Position=>new Vector3(x,0,z);
        public bool Valid()=>id>=0&&health>0&&health<=MaxHealth&&tier>=0&&tier<=2&&sourceTomb>=-1&&new CampTrap{x=x,z=z}.Valid();
    }
    [Serializable] public sealed class GardenDefenseState
    {
        public const int MaxZombies=48;
        public bool initialized;public int nextId,lastWaveDay=-1,kills,cemeteryVersion,clearedTombs;
        public List<GardenUnit> units=new List<GardenUnit>();public List<GardenZombie> zombies=new List<GardenZombie>();
        public static readonly int[] WoodCost={8,2},StoneCost={2,12},GoldCost={15,5};
        public void Upgrade(){if(units==null)units=new List<GardenUnit>();if(zombies==null)zombies=new List<GardenZombie>();}
        public bool Valid()=>nextId>=0&&cemeteryVersion>=0&&cemeteryVersion<=1&&clearedTombs>=0&&lastWaveDay>=-1&&kills>=0&&units!=null&&units.Count<=64&&units.All(u=>u!=null&&u.Valid())&&zombies!=null&&zombies.Count<=MaxZombies&&zombies.All(z=>z!=null&&z.Valid())&&units.Select(u=>u.id).Concat(zombies.Select(z=>z.id)).Distinct().Count()==units.Count+zombies.Count;
        public GardenUnit Add(int kind,Vector3 point,float yaw=0)
        {var u=new GardenUnit{id=nextId++,kind=kind,x=point.x,z=point.z,yaw=yaw};u.health=u.MaxHealth;units.Add(u);return u;}
        public GardenUnit Build(FarmState farm,int kind,Vector3 point,float yaw=0)
        {
            if(kind<0||kind>1||units.Count>=64||farm.adventure.wood<WoodCost[kind]||farm.adventure.stone<StoneCost[kind]||farm.coins<GoldCost[kind]||units.Any(u=>WorldRelief.PlanarDistance(u.Position,point)<(u.kind==1||kind==1?2:1.4f)))return null;
            if(!new CampTrap{x=point.x,z=point.z}.Valid())return null;
            farm.adventure.wood-=WoodCost[kind];farm.adventure.stone-=StoneCost[kind];farm.coins-=GoldCost[kind];return Add(kind,point,yaw);
        }
        public static bool InRange(Vector3 a,Vector3 b,float range)=>WorldRelief.PlanarDistance(a,b)<=range;
        public static GardenZombie Target(Vector3 origin,IEnumerable<GardenZombie> zombies,float range)=>zombies.Where(z=>z.health>0&&InRange(origin,z.Position,range)).OrderBy(z=>(z.Position-origin).sqrMagnitude).FirstOrDefault();
    }
}
