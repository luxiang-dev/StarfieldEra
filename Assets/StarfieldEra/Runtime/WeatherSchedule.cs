namespace StarfieldEra
{
    public enum WeatherKind { Clear, Cloudy, Rain, Fog, Snow }

    /// <summary>Calendar-based weather is stable across saves and never consumes gameplay randomness.</summary>
    public static class WeatherSchedule
    {
        public static WeatherKind ForDay(int day)
        {
            day = System.Math.Max(1, day);
            int season = ((day - 1) / 14) % 4;
            if (day % 4 == 0) return season == 3 ? WeatherKind.Snow : WeatherKind.Rain;
            if (day % 7 == 0) return WeatherKind.Fog;
            if (day % 3 == 0) return WeatherKind.Cloudy;
            return WeatherKind.Clear;
        }

        public static bool WatersSoil(WeatherKind kind) => kind == WeatherKind.Rain || kind == WeatherKind.Snow;
        public static string Name(WeatherKind kind)
        {
            switch (kind)
            {
                case WeatherKind.Cloudy: return "多云";
                case WeatherKind.Rain: return "雨天";
                case WeatherKind.Fog: return "雾天";
                case WeatherKind.Snow: return "雪天";
                default: return "晴天";
            }
        }
    }
}
