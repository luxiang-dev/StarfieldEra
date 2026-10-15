#ifndef STARFIELD_CONTINUOUS_GROUND_INPUT
#define STARFIELD_CONTINUOUS_GROUND_INPUT
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
TEXTURE2D(_Atlas);SAMPLER(sampler_Atlas);
TEXTURE2D(_GrassVariants);SAMPLER(sampler_GrassVariants);
// All passes include this exact layout, including depth/normals/shadow passes.
CBUFFER_START(UnityPerMaterial)
 float4 _Atlas_ST;
 float4 _TurfShadowColor, _TurfHighlightColor;
 float4 _GrassStrokeColor, _GrassRootColor;
 float4 _MacroWarmColor, _MacroCoolColor;
 float4 _CloudDirection, _CloudShadowColor;
 float _TileScale, _BlendWidth, _BlendSharpness, _BlendNoiseStrength;
 float _BlendMidScale, _BlendFineScale, _TextureHeightStrength;
 float _TurfEdgeWidth, _TurfShadowStrength, _TurfHighlightStrength;
 float _GrassStrokeScale, _GrassStrokeAspect, _GrassStrokeDensity, _GrassStrokeStrength;
 float _GrassRootStrength, _GrassWindSpeed, _GrassSwayStrength;
 float _MacroScale, _MacroStrength;
 float _CloudScale, _CloudSpeed, _CloudShadowStrength;
CBUFFER_END
#endif
