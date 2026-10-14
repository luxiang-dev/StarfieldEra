Shader "StarfieldEra/Storybook Sprite"
{
 Properties {[PerRendererData]_MainTex("Painted sprite",2D)="white"{} _Color("Tint",Color)=(1,1,1,1)}
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" "CanUseSpriteAtlas"="True"}
  Pass
  {
   Tags {"LightMode"="UniversalForwardOnly"}
   Cull Off
   ZWrite On
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   half4 _Color;
   CBUFFER_END
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.color=i.color*_Color;return o;}
   half4 frag(V i):SV_Target {half4 paint=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;clip(paint.a-.12);return half4(paint.rgb,1);}
   ENDHLSL
  }
 }
}
