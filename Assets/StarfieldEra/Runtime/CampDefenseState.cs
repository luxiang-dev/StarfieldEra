using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace StarfieldEra
{
    [Serializable] public sealed class CampTrap
    {
        public float x,z;public bool spent;
        public bool Valid()=>!float.IsNaN(x)&&!float.IsInfinity(x)&&!float.IsNaN(z)&&!float.IsInfinity(z)&&Math.Abs(x)<=HexWorldGrid.HalfWidth&&Math.Abs(z)<=HexWorldGrid.HalfDepth;
    }
    [Serializable] public sealed class RaidThreat
    {
        public int id,hp=3;public float x,z,cooldown;
        public bool Valid()=>id>=0&&id<6&&hp>=0&&hp<=3&&cooldown>=0&&cooldown<=3&&new CampTrap{x=x,z=z}.Valid();
    }
    [Serializable] public sealed class CampDefenseState
    {
        public int lastNight=-1,victories,losses,siteIndex,spawned,playerHP=8;
        public bool active,sleepAfterRaid;
        public float elapsed,attackCooldown,repairCooldown,returnX,returnZ;
        public List<CampTrap> traps=new List<CampTrap>();
        public List<RaidThreat> threats=new List<RaidThreat>();
        public void Upgrade(){if(traps==null)traps=new List<CampTrap>();if(threats==null)threats=new List<RaidThreat>();}
        public bool Valid(AdventureState a)=>new CampTrap{x=returnX,z=returnZ}.Valid()&&lastNight>=-1&&victories>=0&&losses>=0&&siteIndex>=0&&(!active||siteIndex<a.homestead.Sites(a).Count())&&spawned>=0&&spawned<=6&&playerHP>=0&&playerHP<=8&&Finite(elapsed)&&elapsed>=0&&elapsed<=60&&Finite(attackCooldown)&&attackCooldown>=0&&attackCooldown<=1&&Finite(repairCooldown)&&repairCooldown>=0&&repairCooldown<=3&&traps!=null&&traps.Count<=24&&traps.All(t=>t!=null&&t.Valid())&&threats!=null&&threats.Count<=6&&threats.All(t=>t!=null&&t.Valid())&&threats.Select(t=>t.id).Distinct().Count()==threats.Count;
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        public static bool RaidDue(int day)=>day>=3&&day%3==0;
        public int Target(FarmState farm)
        {
            if(active)return siteIndex;
            if(!RaidDue(farm.day)||lastNight==farm.day)return -1;
            var sites=farm.adventure.homestead.Sites(farm.adventure).ToList();
            var candidates=Enumerable.Range(0,sites.Count).Where(i=>sites[i].level>0&&sites[i].workers>0).ToArray();
            return candidates.Length==0?-1:candidates[(farm.day/3-1)%candidates.Length];
        }
        public bool Begin(FarmState farm)
        {
            if(active)return false;int target=Target(farm);if(target<0)return false;
            siteIndex=target;elapsed=0;spawned=0;playerHP=8;attackCooldown=repairCooldown=0;threats.Clear();active=true;return true;
        }
        public bool BuyTrap(FarmState farm,Vector3 point)
        {
            if(active||traps.Count(t=>!t.spent)>=24||farm.adventure.wood<2||farm.adventure.stone<1)return false;
            var trap=new CampTrap{x=point.x,z=point.z};if(!trap.Valid()||traps.Any(t=>!t.spent&&Vector2.Distance(new Vector2(t.x,t.z),new Vector2(point.x,point.z))<.9f))return false;
            farm.adventure.wood-=2;farm.adventure.stone--;traps.RemoveAll(t=>t.spent);traps.Add(trap);return true;
        }
        public bool Repair(FarmState farm,ResourceSiteState site)
        {
            if(site==null||site.damage==0||farm.adventure.wood<2||farm.adventure.stone<1||repairCooldown>0)return false;
            farm.adventure.wood-=2;farm.adventure.stone--;site.damage=Math.Max(0,site.damage-25);if(site.damage==0)site.downtime=0;repairCooldown=active?3:0;return true;
        }
        public bool Finish(FarmState farm,bool won)
        {
            if(!active)return false;
            var site=farm.adventure.homestead.Sites(farm.adventure).ElementAt(siteIndex);
            active=false;lastNight=farm.day;threats.Clear();spawned=0;attackCooldown=repairCooldown=0;
            if(won){victories++;farm.coins+=15;}
            else{losses++;site.stock/=2;site.goods/=2;site.damage=Math.Max(site.damage,75);site.downtime=1;}
            return true;
        }
    }
}
