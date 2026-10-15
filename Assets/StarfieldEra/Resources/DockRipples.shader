Shader "StarfieldEra/Dock Ripples"
{
 Properties {_Phase("Phase",Float)=0}
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+8"}
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial) float _Phase; CBUFFER_END
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;return o;}
   half4 frag(V i):SV_Target
   {
    float radius=length(i.uv*2-1);float phase=frac(_Time.y*.28+_Phase*.17);float distance=abs(radius-(.22+phase*.70));float ring=(1-smoothstep(.015,.055,distance))*(1-phase)*smoothstep(.15,.3,radius);
    return half4(.77,.93,.84,ring*.45);
   }
   ENDHLSL
  }
 }
}
