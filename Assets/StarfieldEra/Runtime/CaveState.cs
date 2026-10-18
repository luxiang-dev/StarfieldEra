using System;
using System.Linq;
namespace StarfieldEra
{
    [Serializable] public sealed class CaveState
    {
        public bool inside,firstBossReward;public float returnX,returnZ;public int copperOre,starIron,bossShards,toolLevel,clearedDay=-1;
        public bool[] mined=new bool[6],opened=new bool[2];public int[] enemyHealth={180,180,220,220,900};
        public void Upgrade(){if(mined==null)mined=new bool[6];if(opened==null)opened=new bool[2];if(enemyHealth==null)enemyHealth=new[]{180,180,220,220,900};}
        public bool Valid()=>copperOre>=0&&starIron>=0&&bossShards>=0&&toolLevel>=0&&toolLevel<=2&&clearedDay>=-1&&SurvivalState.Finite(returnX)&&SurvivalState.Finite(returnZ)&&mined!=null&&mined.Length==6&&opened!=null&&opened.Length==2&&enemyHealth!=null&&enemyHealth.Length==5&&enemyHealth.Select((h,i)=>h>=0&&h<=(i==4?900:i<2?180:220)).All(x=>x);
        public void Refresh(int day){if(clearedDay<0||day<clearedDay+3)return;mined=new bool[6];opened=new bool[2];enemyHealth=new[]{180,180,220,220,900};clearedDay=-1;}
        public bool Improve(FarmState farm)
        {int copper=toolLevel==0?8:16,iron=toolLevel==0?3:6;if(toolLevel>=2||copperOre<copper||starIron<iron||farm.coins<30||toolLevel==1&&bossShards<1)return false;copperOre-=copper;starIron-=iron;farm.coins-=30;if(toolLevel==1)bossShards--;toolLevel++;return true;}
    }
    public sealed class CaveEnemy:UnityEngine.MonoBehaviour
    {public int index;public CaveGame cave;public GardenActor actor;public float attackAt;}
}
