using System.Collections.Generic;
using UnityEngine;
namespace StarfieldEra
{
    public sealed class WeatherManager:MonoBehaviour
    {
        FarmGame game;
        public WeatherState State=>game.State.weather;
        public IReadOnlyList<WeatherType> trueWeatherQueue=>State.trueWeatherQueue.AsReadOnly();
        public int weatherSkillLevel=>State.weatherSkillLevel;
        public WeatherDataSO Today=>WeatherCatalog.Get(game.State.Weather);
        public WeatherDataSO Tomorrow=>WeatherCatalog.Get(game.State.TomorrowWeather);
        public void Initialize(FarmGame owner){game=owner;State.Ensure(game.State.day);}
        public void UpgradeSkill()
        {
            if(!State.TryUpgrade()){game.ShowMessage(weatherSkillLevel==3?"预知未来已达到最高等级。":"生存经验或星石不足。收获、采集与远行可积累经验，开采晶石与击败守卫可获得星石。");return;}
            game.Adventure.Commit("预知未来升至"+weatherSkillLevel+"级，可凭经验预判未来"+weatherSkillLevel+"天的天气。");
        }
    }
}
