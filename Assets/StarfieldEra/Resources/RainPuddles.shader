Shader "StarfieldEra/Rain Puddles"
{
 Properties {_Rain("Rain intensity",Range(0,1))=1}
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+1"}
  Pass
  {
   Tags {"LightMode"="UniversalForwardOnly"}
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "WeatherAtmosphere.hlsl"
   CBUFFER_START(UnityPerMaterial) float _Rain; CBUFFER_END
   struct A {float4 p:POSITION;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;float2 water:TEXCOORD1;};
   V vert(A i){V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.water=i.color.rg;return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float ripple(float2 p,float rate,float seed)
   {
    float2 cell=floor(p),local=frac(p)-.5;float h=hash(cell+seed);local-=(float2(h,hash(cell+13+seed))-.5)*.23;
    float phase=frac(_Time.y*rate+h*7);float radius=phase*.29;float d=abs(length(local)-radius);float width=max(fwidth(d)*1.1,.008);
    return (1-smoothstep(width,width+.012,d))*(1-phase)*smoothstep(0,.12,phase);
   }
   half4 frag(V i):SV_Target
   {
    float mask=smoothstep(.015,.94,i.water.x);clip(mask-.003);
    float depth=i.water.y;float wet=smoothstep(.015,.11,depth);float deep=smoothstep(.08,.45,depth);
    float3 view=SafeNormalize(GetWorldSpaceViewDir(i.world));float fresnel=pow(1-saturate(view.y),3);
    float wave=sin(i.world.x*3.1+i.world.z*2.3+_Time.y*.75)*sin(i.world.x*1.7-i.world.z*3.7-_Time.y*.55)*.5+.5;
    half3 water=lerp(half3(.16,.29,.27),half3(.08,.20,.24),deep);
    water=lerp(water,half3(.58,.66,.65),(.06+fresnel*.30)*wet+wave*.06);
    float rings=(ripple(i.world.xz*.80,1.15,0)+ripple(i.world.xz*1.13+.43,.88,31))*.35*_Rain*wet;
    water+=half3(.40,.47,.43)*rings;
    half3 color=lerp(half3(.18,.23,.13),water,wet);
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));color*=lerp(.72,1,sun.shadowAttenuation);
    float alpha=mask*lerp(.22,lerp(.38,.68,deep),wet);
    return half4(WeatherAtmosphere(color,i.world),alpha);
   }
   ENDHLSL
  }
 }
}
