Shader "Water Polo/Championship Ribbon"
{
 Properties
 {
  _BaseMap("Replaceable ribbon artwork",2D)="white"{}
  _BaseColor("Midnight LED",Color)=(.015,.045,.13,1)
  _AccentColor("Cyan graphics",Color)=(.04,.65,.95,1)
  _ArtworkWeight("Artwork blend",Range(0,1))=0
  _Phase("Animation phase",Float)=0
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass
  {
   Tags {"LightMode"="SRPDefaultUnlit"}
   Cull Back ZWrite On
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST,_BaseColor,_AccentColor;
   float _ArtworkWeight,_Phase;
   CBUFFER_END
   Varyings vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;return o;}
   half4 frag(Varyings i):SV_Target
   {
    float u=i.uv.x-_Phase,y=i.uv.y;
    float wave=.46+.22*sin(u*4.2)+.07*sin(u*9.1);
    float stroke=1-smoothstep(.035,.075,abs(y-wave));
    float streak=pow(saturate(.5+.5*sin(u*2.7+y*2)),20)*.16;
    half3 c=_BaseColor.rgb+_AccentColor.rgb*(stroke*.64+streak);
    float edge=1-smoothstep(.005,.04,min(y,1-y));c+=_AccentColor.rgb*edge*.45;
    half3 artwork=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv*_BaseMap_ST.xy+_BaseMap_ST.zw+float2(-_Phase*.1,0)).rgb;
    return half4(lerp(c,artwork,_ArtworkWeight),1);
   }
   ENDHLSL
  }
 }
}
