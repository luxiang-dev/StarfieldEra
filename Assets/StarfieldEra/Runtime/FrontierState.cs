using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace StarfieldEra
{
    [Serializable] public sealed class FrontierState
    {
        public bool bridgeCredit,towerCredit;public bool[] unlocked=new bool[2],visited=new bool[2],treasure=new bool[2];public int[] gatheredDay={-1,-1},observedDay={-1,-1};
        public List<VillageDecoration> decorations=new List<VillageDecoration>();
        public static readonly string[] Names={"雾松谷","赤晶矿谷"};public static readonly Vector3[] Centers={new Vector3(-68,0,54),new Vector3(68,0,-54)};
        public static Vector3 Entrance(int i)=>Centers[i]+Vector3.back*9;
        public static int Region(Vector3 p){for(int i=0;i<2;i++)if(WorldRelief.PlanarDistance(p,Centers[i])<8)return i;return -1;}
        public string Cost(int i)=>i==0?"木材8 · 石料4 · 木板"+(bridgeCredit?0:6)+" · 金币20":"木材4 · 石料12 · 石砖"+(towerCredit?0:6)+" · 金币30 · 声望3";
        public bool Repair(FarmState f,int i)
        {
            if(i<0||i>1||unlocked[i])return false;var a=f.adventure;var h=a.homestead;int wood=i==0?8:4,stone=i==0?4:12,gold=i==0?20:30,planks=i==0&&!bridgeCredit?6:0,bricks=i==1&&!towerCredit?6:0;
            if(a.wood<wood||a.stone<stone||f.coins<gold||h.planks<planks||h.bricks<bricks||i==1&&a.village.reputation<3)return false;
            a.wood-=wood;a.stone-=stone;f.coins-=gold;h.planks-=planks;h.bricks-=bricks;unlocked[i]=true;f.weather.survivalExperience+=10;return true;
        }
        public bool Gather(FarmState f,int i,int kind)
        {
            if(i<0||i>1||!unlocked[i])return false;var a=f.adventure;
            if(kind==0){if(gatheredDay[i]==f.day||f.energy<3)return false;f.SpendEnergy(3);gatheredDay[i]=f.day;if(i==0){a.wood+=6;a.expedition.fiber+=4;}else{a.stone+=8;a.crystals+=2;f.weather.starStones++;}}
            else if(kind==1){if(observedDay[i]==f.day)return false;observedDay[i]=f.day;a.expedition.fiber+=2;f.weather.survivalExperience+=3;}
            else if(kind==2){if(treasure[i])return false;treasure[i]=true;f.coins+=25;f.starSeeds+=2;a.village.reputation++;}else return false;return true;
        }
        public bool Valid()=>unlocked!=null&&unlocked.Length==2&&visited!=null&&visited.Length==2&&treasure!=null&&treasure.Length==2&&gatheredDay!=null&&gatheredDay.Length==2&&observedDay!=null&&observedDay.Length==2&&gatheredDay.Concat(observedDay).All(d=>d>=-1)&&Enumerable.Range(0,2).All(i=>unlocked[i]||!visited[i]&&!treasure[i]&&gatheredDay[i]==-1&&observedDay[i]==-1)&&decorations!=null&&decorations.Count<=16&&decorations.All(d=>d!=null&&d.rotation>=0&&d.rotation<6&&d.column>=HexWorldGrid.MinColumn&&d.column<=HexWorldGrid.MaxColumn&&d.row>=HexWorldGrid.MinRow&&d.row<=HexWorldGrid.MaxRow)&&decorations.Select(d=>d.column+":"+d.row).Distinct().Count()==decorations.Count;
    }
}
