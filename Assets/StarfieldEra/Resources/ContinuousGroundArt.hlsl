#ifndef STARFIELD_CONTINUOUS_GROUND_ART
#define STARFIELD_CONTINUOUS_GROUND_ART

float2 GroundHash22(float2 p)
{
 float3 q=frac(float3(p.x,p.y,p.x)*float3(.1031,.1030,.0973));
 q+=dot(q,q.yzx+33.33);
 return frac((q.xx+q.yz)*q.zy);
}
float GroundNoise(float2 p)
{
 float2 cell=floor(p),f=frac(p);
 // Quintic interpolation avoids a visible grid in the noise derivative and turf AO.
 float2 t=f*f*f*(f*(f*6-15)+10);
 return lerp(lerp(GroundHash22(cell).x,GroundHash22(cell+float2(1,0)).x,t.x),
             lerp(GroundHash22(cell+float2(0,1)).x,GroundHash22(cell+1).x,t.x),t.y);
}
float GroundLayerNoise(float2 worldXZ,float2 offset)
{
 float middle=GroundNoise(worldXZ*_BlendMidScale+offset);
 float fine=GroundNoise(worldXZ*_BlendFineScale+offset*3.17);
 // Fine detail dies away when smaller than a pixel instead of sparkling at distant zooms.
 float footprint=max(length(ddx(worldXZ*_BlendFineScale)),length(ddy(worldXZ*_BlendFineScale)));
 fine=lerp(fine,.5,smoothstep(.35,1.2,footprint));
 return (middle-.5)*1.3+(fine-.5)*.7;
}
float4 GroundHeightWeights(float4 inputWeights,float4 textureHeights,float2 worldXZ,out float turfField,out float turfPresence)
{
 float4 source=max(inputWeights,0);source/=max(dot(source,1),.0001);
 float4 support=smoothstep(0,.025,source);
 float4 perturb=float4(GroundLayerNoise(worldXZ,float2(11,3)),GroundLayerNoise(worldXZ,float2(43,19)),
                      GroundLayerNoise(worldXZ,float2(7,71)),GroundLayerNoise(worldXZ,float2(89,31)));
 // Perturb only existing layers: pure grass stays grass and absent sand cannot appear.
 float4 scores=source+perturb*_BlendNoiseStrength+(textureHeights-.5)*_TextureHeightStrength;
 scores=lerp(-4,scores,support);
 turfField=scores.x-max(scores.y,scores.z);
 turfPresence=saturate(min(source.x,source.y+source.z)*14)*(1-saturate(source.w*4));
 float highest=max(max(scores.x,scores.y),max(scores.z,scores.w));
 float4 width=max(_BlendWidth/max(_BlendSharpness,1),fwidth(scores)*.65+.0005);
 float4 coverage=max(0,(scores-highest+width)/width);
 coverage=coverage*coverage*support;
 return coverage/max(dot(coverage,1),.0001);
}
float GroundFieldDistance(float field,float2 worldXZ)
{
 // Recover the world-space gradient from screen derivatives, independent of camera
 // projection or zoom. The result gives the turf rim a width measured in metres.
 float2 dx=ddx(worldXZ),dy=ddy(worldXZ);float det=dx.x*dy.y-dx.y*dy.x;
 float2 d=float2(ddx(field),ddy(field));
 float safeDet=(det<0?-1:1)*max(abs(det),1e-8);
 float2 gradient=float2(d.x*dy.y-d.y*dx.y,dx.x*d.y-dy.x*d.x)/safeDet;
 return field/max(length(gradient),.025);
}
void GroundTurfMasks(float field,float presence,float2 worldXZ,float4 layers,out float shadow,out float highlight)
{
 float distance=GroundFieldDistance(field,worldXZ);
 float aa=max(fwidth(distance),.006);float width=max(_TurfEdgeWidth,.01);
 float grassSide=smoothstep(-aa,aa,distance);
 shadow=(1-smoothstep(0,width,max(-distance,0)))*(1-grassSide)*presence*saturate(layers.y+layers.z);
 highlight=(1-smoothstep(0,width*.65,max(distance,0)))*grassSide*presence*layers.x;
}
float GroundBrushBlade(float2 p,float2 root,float height,float bend,float width)
{
 float t=saturate((p.y-root.y)/max(height,.001));
 float2 centre=root+float2(bend*t*t,height*t);
 float distance=length(p-centre);
 float radius=width*lerp(1,.18,t);
 float aa=max(length(float2(ddx(distance),ddy(distance))),.003);
 return 1-smoothstep(max(0,radius-aa),radius+aa,distance);
}
half3 GroundGrassStrokes(half3 grass,float2 worldXZ)
{
 float2 uv=worldXZ*float2(_GrassStrokeScale*_GrassStrokeAspect,_GrassStrokeScale);
 float2 cell=floor(uv),p=frac(uv)-.5;
 float2 random=GroundHash22(cell+13.1);
 p-=(random-.5)*.32;
 float wind=sin(_Time.y*_GrassWindSpeed+worldXZ.y*.8+GroundNoise(worldXZ*.32)*3)*_GrassSwayStrength;
 float bend=(random.x-.5)*.18+wind;
 float central=GroundBrushBlade(p,float2(0,-.22),.46,bend,.035);
 float left=GroundBrushBlade(p,float2(-.04,-.22),.32,bend-.16,.029);
 float right=GroundBrushBlade(p,float2(.04,-.22),.35,bend+.15,.031);
 float strokes=max(central,max(left,right));
 float density=smoothstep(.25,.8,GroundNoise(worldXZ*.45+23));
 float enabled=step(1-_GrassStrokeDensity*lerp(.65,1.2,density),random.y);
 float footprint=max(length(ddx(uv)),length(ddy(uv)));
 float detailFade=1-smoothstep(.2,.65,footprint);
 float roots=(1-smoothstep(.03,.16,length((p-float2(0,-.22))*float2(1,1.7))))*enabled*detailFade;
 grass=lerp(grass,grass*_GrassRootColor.rgb,roots*_GrassRootStrength);
 return lerp(grass,_GrassStrokeColor.rgb,strokes*enabled*detailFade*_GrassStrokeStrength);
}
half3 GroundMacroVariation(half3 grass,float2 worldXZ)
{
 float patch=GroundNoise(worldXZ*_MacroScale+float2(13,7))*.7+GroundNoise(worldXZ*_MacroScale*2.1+31)*.3;
 half3 tint=lerp(_MacroCoolColor.rgb,_MacroWarmColor.rgb,smoothstep(.24,.76,patch));
 half3 neutral=max((_MacroCoolColor.rgb+_MacroWarmColor.rgb)*.5,.025);
 return grass*lerp(half3(1,1,1),tint/neutral,_MacroStrength)*lerp(1,lerp(.9,1.1,patch),_MacroStrength);
}
half3 GroundCloudShadows(half3 lit,float2 worldXZ)
{
 float2 direction=_CloudDirection.xy/max(length(_CloudDirection.xy),.001);
 float2 uv=(worldXZ-direction*(_Time.y*_CloudSpeed))*_CloudScale;
 float clouds=GroundNoise(uv+float2(31,17))*.72+GroundNoise(uv*1.93+7)*.28;
 float shadow=smoothstep(.32,.7,clouds)*_CloudShadowStrength;
 // Applied after native URP lighting; actual light shadows, SSAO and lamps remain intact.
 return lit*lerp(half3(1,1,1),_CloudShadowColor.rgb,shadow);
}
half3 GroundAtlas(float2 uv,float2 quadrant)
{
 return SAMPLE_TEXTURE2D_GRAD(_Atlas,sampler_Atlas,quadrant+(.006+frac(uv)*.488),ddx(uv)*.488,ddy(uv)*.488).rgb;
}
half3 GroundGrassAtlas(float2 uv,float2 quadrant)
{
 return SAMPLE_TEXTURE2D_GRAD(_GrassVariants,sampler_GrassVariants,quadrant+(.006+frac(uv)*.488),ddx(uv)*.488,ddy(uv)*.488).rgb;
}
#endif
