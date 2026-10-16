using System;
using System.Collections.Generic;
using System.Linq;
namespace StarfieldEra
{
    [Serializable] public sealed class WeatherState
    {
        // Index zero is today; six more entries are hidden future truth.
        public List<WeatherType> trueWeatherQueue=new List<WeatherType>();
        public int queueStartDay,seed,weatherSkillLevel,survivalExperience,starStones;
        public static readonly int[] ExperienceCosts={20,50,90},StarStoneCosts={1,3,5};
        public void Ensure(int day,bool migrate=false)
        {
            if(seed==0)seed=Guid.NewGuid().GetHashCode()&int.MaxValue;if(seed==0)seed=1;
            if(trueWeatherQueue==null)trueWeatherQueue=new List<WeatherType>();
            if(trueWeatherQueue.Count==0)
            {
                queueStartDay=day;for(int i=0;i<7;i++)trueWeatherQueue.Add(Roll(seed,day+i));
                if(migrate){trueWeatherQueue[0]=Legacy(day);trueWeatherQueue[1]=Legacy(day+1);}return;
            }
            if(trueWeatherQueue.Count!=7||queueStartDay<1||day<=queueStartDay)return;
            int shift=day-queueStartDay;
            if(shift>=7){trueWeatherQueue.Clear();for(int i=0;i<7;i++)trueWeatherQueue.Add(Roll(seed,day+i));}
            else{trueWeatherQueue.RemoveRange(0,shift);for(int i=7-shift;i<7;i++)trueWeatherQueue.Add(Roll(seed,day+i));}
            queueStartDay=day;
        }
        static WeatherType Legacy(int day)
        {
            var old=WeatherSchedule.ForDay(day);
            return old==WeatherKind.Clear?WeatherType.Sunny:old==WeatherKind.Cloudy||old==WeatherKind.Fog?WeatherType.Cloudy:WeatherType.LightRain;
        }
        static uint Hash(int seed,int day,int salt=0)
        {unchecked{uint v=(uint)seed^(uint)day*0x9e3779b9u^(uint)salt*0x85ebca6bu;v^=v>>16;v*=0x7feb352du;v^=v>>15;v*=0x846ca68bu;return v^(v>>16);}}
        public static WeatherType Roll(int seed,int day)
        {
            uint value=Hash(seed,day)%100;
            return value<30?WeatherType.Sunny:value<55?WeatherType.Cloudy:value<78?WeatherType.LightRain:value<93?WeatherType.HeavyRain:WeatherType.Thunderstorm;
        }
        public bool Valid(int day)=>seed!=0&&queueStartDay==day&&weatherSkillLevel>=0&&weatherSkillLevel<=3&&survivalExperience>=0&&starStones>=0&&trueWeatherQueue!=null&&trueWeatherQueue.Count==7&&trueWeatherQueue.All(t=>(int)t>=0&&(int)t<5);
        public bool CanPredict(int nthDay)=>nthDay>=3&&nthDay<=5&&weatherSkillLevel>=nthDay-2;
        public static double Accuracy(int level,int nthDay)=>Math.Max(.50,Math.Min(.90,.55+level*.10-(nthDay-3)*.08));
        public WeatherPrediction Predict(int nthDay)
        {
            if(!CanPredict(nthDay)||trueWeatherQueue==null||trueWeatherQueue.Count!=7)return null;
            var truth=trueWeatherQueue[nthDay-1];uint h=Hash(seed,queueStartDay+nthDay-1,weatherSkillLevel*11+nthDay);
            var similar=Similar(truth,h);double probability=Accuracy(weatherSkillLevel,nthDay);
            bool correct=Hash(seed,queueStartDay+nthDay-1,weatherSkillLevel*37+nthDay+100)/(double)uint.MaxValue<probability;
            int percent=(int)Math.Round(probability*20,MidpointRounding.AwayFromZero)*5;
            return new WeatherPrediction{main=correct?truth:similar,secondary=correct?similar:truth,mainPercent=percent};
        }
        static WeatherType Similar(WeatherType type,uint random)
        {
            int r=(int)(random%100);
            switch(type)
            {
                case WeatherType.Sunny:return r<80?WeatherType.Cloudy:WeatherType.LightRain;
                case WeatherType.Cloudy:return r<30?WeatherType.Sunny:r<80?WeatherType.LightRain:WeatherType.HeavyRain;
                case WeatherType.LightRain:return r<45?WeatherType.Cloudy:r<90?WeatherType.HeavyRain:WeatherType.Thunderstorm;
                case WeatherType.HeavyRain:return r<60?WeatherType.Thunderstorm:r<85?WeatherType.LightRain:WeatherType.Cloudy;
                default:return r<80?WeatherType.HeavyRain:WeatherType.Cloudy;
            }
        }
        public bool TryUpgrade()
        {
            if(weatherSkillLevel>=3||survivalExperience<ExperienceCosts[weatherSkillLevel]||starStones<StarStoneCosts[weatherSkillLevel])return false;
            survivalExperience-=ExperienceCosts[weatherSkillLevel];starStones-=StarStoneCosts[weatherSkillLevel];weatherSkillLevel++;return true;
        }
    }
    public sealed class WeatherPrediction
    {
        public WeatherType main,secondary;public int mainPercent;
        public int SecondaryPercent=>100-mainPercent;
    }
}
