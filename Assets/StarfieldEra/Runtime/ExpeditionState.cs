using System;
using System.Linq;
namespace StarfieldEra
{
    [Serializable]
    public sealed class ExpeditionState
    {
        public int fiber,shards,bait,campSupplies,fishCaught,quest;
        public bool fishingRod,marshBoots,starAmulet;
        public bool[] caches=new bool[8],runes=new bool[8];
        public int[] forageDay={-1,-1,-1,-1,-1,-1,-1,-1};
        public int CacheCount=>caches.Count(x=>x);
        public int RuneCount=>runes.Count(x=>x);
        public bool Valid()=>fiber>=0&&shards>=0&&bait>=0&&campSupplies>=0&&fishCaught>=0&&quest>=0&&quest<=3&&caches!=null&&caches.Length==8&&runes!=null&&runes.Length==8&&forageDay!=null&&forageDay.Length==8&&forageDay.All(d=>d>=-1);
    }
    public static class ExpeditionContent
    {
        public static readonly string[] RuneNames={"晨风起源碑","青苔共生碑","镜湖潮汐碑","星辉古门碑","金穗耕作碑","白岩群山碑","幽光迁徙碑","远行航路碑"};
        public static readonly string[] Lore={
            "先民在晨风里播下第一颗种子。村落的光，来自彼此照料。",
            "森林伙伴曾为幼苗引来雨水，人类以果实回赠它们。",
            "镜湖记录每一场春雨。潮汐伙伴守护湖岸，等待友善的来客。",
            "古门不是征服的武器，而是连接八片土地的星图。守卫仍履行古老的承诺。",
            "金穗的收成需要土地休养。先民把食物和种子留在路旁，赠予后来者。",
            "群山封存着星光结晶。守卫认可勇气，却更尊重与伙伴并肩的人。",
            "迁徙者借幽光寻找道路。湿地的脚印，记载了人与生灵共同的旅程。",
            "八座石碑共同指向新的纪元：耕作、探索、友谊，让大陆再次相连。"
        };
        public static readonly string[] Quests={"远征入门：开启2个补给箱，累计钓获3条鱼。","湿地行者：解读3座石碑，并制作湿地长靴。","星图重现：解读全部8座石碑，制作星辉护符，击败三地守卫。","星图已重现！继续探索、经营与照料伙伴。"};
    }
    public sealed class ExpeditionTarget : UnityEngine.MonoBehaviour {public WorldEncounter encounter;}
}
