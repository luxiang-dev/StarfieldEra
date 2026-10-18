using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Low frequency environmental simulation. Crafted gear is used automatically.</summary>
    public sealed class ExpeditionReadiness:MonoBehaviour
    {
        FarmGame game;float tick,exposureClock;Light lamp;
        public string Status {get;private set;}="出征准备 · 天气、照明与保暖";
        public bool IsLit {get;private set;}
        public bool InDarkness {get;private set;}
        public float SpeedMultiplier {get;private set;}=1;
        public void Initialize(FarmGame owner)
        {
            game=owner;var root=new GameObject("Traveler portable lantern");root.transform.SetParent(game.World.Player,false);root.transform.localPosition=new Vector3(.3f,1.1f,.2f);
            lamp=root.AddComponent<Light>();lamp.type=LightType.Point;lamp.range=8;lamp.intensity=1.8f;lamp.color=new Color(1,.78f,.46f);lamp.shadows=LightShadows.None;lamp.enabled=false;
            Evaluate(0);
        }
        public static string ForecastAdvice(WeatherType weather)=>weather>=WeatherType.LightRain?"出征建议：雨衣防止淋湿，雪地带保暖外套，夜行带提灯。":"出征建议：雪地带保暖外套，夜行带提灯；路灯可建立安全光照路线。";
        void Update()
        {
            if(!game||game.SimulationPaused)return;tick+=Mathf.Min(Time.deltaTime,.25f);if(tick<1)return;float dt=tick;tick=0;Evaluate(dt);
        }
        void Evaluate(float dt)
        {
            var s=game.State.survival;var p=game.World.Player.position;var terrain=game.World.HexGrid.TerrainAt(p);
            bool shelter=!game.InCave&&game.NearHouse;
            bool lantern=EquipmentCatalog.Has(game,"lantern"),raincoat=EquipmentCatalog.Has(game,"raincoat"),coat=EquipmentCatalog.Has(game,"coat");
            bool dark=game.InCave||game.State.clock.IsNight||game.Night.Active;
            IsLit=!dark||lantern||shelter||(game.InCave?game.Cave.Illuminated(p):game.Garden.Illuminated(p));InDarkness=dark&&!IsLit;lamp.enabled=dark&&lantern;
            bool raining=!game.InCave&&!shelter&&game.State.Rainy;
            bool freezing=!game.InCave&&!shelter&&(terrain==TerrainKind.Snow||terrain==TerrainKind.Rock&&game.State.clock.IsNight);
            s.wetness=Mathf.Clamp(s.wetness+dt*(game.IsInWater?4:raining&&!raincoat?(game.State.Weather>=WeatherType.HeavyRain?1.4f:.7f):shelter?-5:raincoat?-2:-.7f),0,100);
            s.cold=Mathf.Clamp(s.cold+dt*(freezing&&!coat?(.65f+s.wetness*.012f):shelter?-5:coat?-3:-1.5f),0,100);
            SpeedMultiplier=(InDarkness?.75f:1)*(s.wetness>=65?.9f:1)*(s.cold>=65?.85f:1);
            exposureClock+=dt;
            if(exposureClock>=10){exposureClock=0;if(s.wetness>=65||s.cold>=65){game.State.SpendEnergy(Mathf.Min(game.State.energy,s.cold>=65?5:3));if(s.cold>=95)game.DamagePlayer(2);game.Persist();}}
            Status=(InDarkness?"缺少照明 · 移动75%":dark?"光照充足":"日间出征")+" · "+(freezing?(coat?"外套保暖":"寒冷 "+Mathf.RoundToInt(s.cold)+"%"):s.cold>0?"回暖 "+Mathf.RoundToInt(s.cold)+"%":"温暖")+" · "+(raining?(raincoat?"雨衣防雨":"淋湿 "+Mathf.RoundToInt(s.wetness)+"%"):s.wetness>0?"潮湿 "+Mathf.RoundToInt(s.wetness)+"%":"干燥");
        }
    }
}
