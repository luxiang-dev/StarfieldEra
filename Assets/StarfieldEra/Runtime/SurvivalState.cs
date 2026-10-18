using System;
using UnityEngine;
namespace StarfieldEra
{
    [Serializable] public sealed class SurvivalState
    {
        public const int MaxHealth=100;
        public int health=MaxHealth;
        public float wetness,cold;
        public float invulnerable,recoveryDelay,recoverySeconds;
        public bool Damage(int amount)
        {
            if(amount<=0||health==0||invulnerable>0)return false;
            health=Math.Max(0,health-amount);invulnerable=.8f;recoveryDelay=15;recoverySeconds=0;return true;
        }
        public void Tick(float seconds)
        {
            if(!Finite(seconds)||seconds<=0)return;
            invulnerable=Math.Max(0,invulnerable-seconds);
            float healingTime=Math.Max(0,seconds-recoveryDelay);recoveryDelay=Math.Max(0,recoveryDelay-seconds);
            if(health==0||health==MaxHealth){recoverySeconds=0;return;}
            recoverySeconds+=healingTime;int heal=(int)(recoverySeconds/2);
            if(heal>0){health=Math.Min(MaxHealth,health+heal);recoverySeconds-=heal*2;}
        }
        public void Rest(){health=MaxHealth;invulnerable=recoveryDelay=recoverySeconds=wetness=cold=0;}
        public bool Valid()=>Finite(wetness)&&wetness>=0&&wetness<=100&&Finite(cold)&&cold>=0&&cold<=100&&health>=0&&health<=MaxHealth&&Finite(invulnerable)&&invulnerable>=0&&invulnerable<=.8f&&Finite(recoveryDelay)&&recoveryDelay>=0&&recoveryDelay<=15&&Finite(recoverySeconds)&&recoverySeconds>=0&&recoverySeconds<2;
        public static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
    }
    [Serializable] public sealed class WorldClockState
    {
        public const float DaySeconds=1200;
        public float elapsed;
        public int MinuteOfDay=>(360+(int)(elapsed/DaySeconds*1440))%1440;
        public bool IsNight=>MinuteOfDay>=1080||MinuteOfDay<360;
        public string Display=>string.Format("{0:00}:{1:00}",MinuteOfDay/60,MinuteOfDay%60);
        public int Tick(float seconds)
        {
            if(!SurvivalState.Finite(seconds)||seconds<=0)return 0;
            elapsed+=seconds;int days=(int)(elapsed/DaySeconds);elapsed-=days*DaySeconds;return days;
        }
        public bool Valid()=>SurvivalState.Finite(elapsed)&&elapsed>=0&&elapsed<DaySeconds;
    }
}
