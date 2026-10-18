#ifndef STARFIELD_WEATHER_ATMOSPHERE
#define STARFIELD_WEATHER_ATMOSPHERE
// xyz: linear fog colour; w: density. Zero by default when no weather controller exists.
float4 _StarfieldWeatherFog;
float _StarfieldNight;
float4 _StarfieldExplorationVision;
half3 WeatherAtmosphere(half3 color, float3 world)
{
    color *= lerp(half3(1,1,1),half3(.30,.40,.66),saturate(_StarfieldNight));
    float distanceFromCamera = length(world - GetCameraPositionWS());
    float opticalDepth = distanceFromCamera * _StarfieldWeatherFog.w;
    float visibility = exp2(-opticalDepth * opticalDepth * 1.442695);
    color=lerp(_StarfieldWeatherFog.rgb, color, visibility);
    if(_StarfieldExplorationVision.w>0)color=lerp(color,_StarfieldWeatherFog.rgb,.75*smoothstep(_StarfieldExplorationVision.w,_StarfieldExplorationVision.w+10,length(world.xz-_StarfieldExplorationVision.xz)));
    return color;
}
#endif
