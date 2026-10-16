using UnityEngine;
namespace StarfieldEra
{
    public enum WeatherType { Sunny, Cloudy, LightRain, HeavyRain, Thunderstorm }

    [CreateAssetMenu(menuName="StarfieldEra/Weather data")]
    public sealed class WeatherDataSO:ScriptableObject
    {
        public WeatherType weatherType;
        public string displayName;
        public Sprite fullColorIcon,sketchIcon;
        public Vector2Int temperatureRange;
        public string windDescription;
        [TextArea] public string buffDescription,experienceProverb;
        [Range(1,2)] public float cropGrowthMultiplier=1;
        public int extraWood,extraStone;
        public bool watersSoil;
    }
    public static class WeatherCatalog
    {
        static readonly WeatherDataSO[] cache=new WeatherDataSO[5];
        public static WeatherDataSO Get(WeatherType type)
        {
            int index=(int)type;
            if(index<0||index>=5)type=WeatherType.Sunny;
            index=(int)type;
            if(!cache[index])
            {
                cache[index]=Resources.Load<WeatherDataSO>("Weather/"+type);
                if(!cache[index]){cache[index]=ScriptableObject.CreateInstance<WeatherDataSO>();Configure(cache[index],type);}
            }
            return cache[index];
        }
        public static void Configure(WeatherDataSO data,WeatherType type)
        {
            int i=(int)type;data.weatherType=type;data.displayName=new[]{"晴天","多云","小雨","大雨","雷暴"}[i];
            data.temperatureRange=new[]{new Vector2Int(18,27),new Vector2Int(15,23),new Vector2Int(12,19),new Vector2Int(10,17),new Vector2Int(9,16)}[i];
            data.windDescription=new[]{"微风 · 东南风","和风 · 偏南风","轻风 · 空气湿润","强风 · 偏东风","强阵风 · 云层翻涌"}[i];
            data.buffDescription=new[]{"阳光充足：湿润作物生长速度 +10%","适宜劳作：每次采集木材额外 +1","细雨润田：农田与幼树自动补水","雨水充足：农田补水，湿润作物生长速度 +10%","雷雨蓄水：农田补水，每次采石额外 +1"}[i];
            data.experienceProverb=new[]{"晚霞映天，风轻云薄，过两日多半还有好晴光。","晨雾贴林，云走得慢，往后两日天色易阴晴相间。","草叶挂珠，远山蒙纱，过两天要留心一阵细雨。","燕子低飞，空气湿重，后天大概率有一场大雨。","云头高耸，闷热无风，往后两日要防急雨和雷声。"}[i];
            data.cropGrowthMultiplier=type==WeatherType.Sunny||type==WeatherType.HeavyRain?1.1f:1;
            data.watersSoil=i>=2;data.extraWood=type==WeatherType.Cloudy?1:0;data.extraStone=type==WeatherType.Thunderstorm?1:0;
        }
    }
}
