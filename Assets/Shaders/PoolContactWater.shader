Shader "ToduStudios/Pool Contact Water"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+8" "RenderType"="Transparent" }
        Pass
        {
            Name "Pool surface contacts"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 shape : TEXCOORD1;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                half2 uv : TEXCOORD0;
                half2 shape : TEXCOORD1;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                o.shape = v.shape;
                return o;
            }

            half FragRidge(half distance, half width, half soft)
            {
                return 1.0h - smoothstep(width, width + soft, abs(distance));
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half2 p = i.uv;
                half r = length(p);
                half age = i.shape.y;
                half feather = max(fwidth(r), 0.023h);
                half alpha;
                half3 color = i.color.rgb;
                if (i.shape.x > 2.5h)
                {
                    alpha = 1.0h - smoothstep(0.19h, 0.97h, r);
                    alpha *= alpha;
                }
                else if (i.shape.x > 1.5h)
                {
                    half patch = sin(p.x * 21.0h + p.y * 9.0h) * sin(p.x * 7.0h - p.y * 19.0h);
                    alpha = (1.0h - smoothstep(0.1h, 0.98h, r)) * (0.17h + 0.39h * smoothstep(0.08h, 0.7h, patch));
                }
                else if (i.shape.x > 0.5h)
                {
                    half sides = 0.26h + 0.30h * (0.5h - 0.5h * p.y) + 0.067h * sin(p.y * 7.0h + age * 3.0h);
                    half crest = FragRidge(abs(p.x) - sides, 0.038h, feather + 0.09h);
                    half inner = FragRidge(abs(p.x) - sides * 0.53h, 0.011h, feather + 0.07h);
                    half under = FragRidge(abs(p.x) - sides - 0.073h, 0.052h, 0.105h) * (1.0h - crest);
                    half breakup = 0.40h + 0.6h * smoothstep(-0.65h, 0.9h, sin(p.y * 21.0h + abs(p.x) * 13.0h + age * 6.0h));
                    half wash = (1.0h - smoothstep(0.04h, 0.54h, abs(p.x))) * smoothstep(-0.4h, 0.7h, p.y);
                    alpha = (crest + 0.44h * inner + 0.23h * wash + 0.45h * under) * breakup * (1.0h - smoothstep(0.26h, 0.99h, r));
                    color = lerp(color, half3(0.075h, 0.37h, 0.45h), under * 0.64h);
                }
                else
                {
                    half wobble = 0.037h * sin(p.x * 14.0h + p.y * 6.0h + age * 4.0h) + 0.018h * sin(p.y * 24.0h - p.x * 9.0h);
                    half outer = FragRidge(r + wobble - 0.78h, 0.024h, feather);
                    half inner = FragRidge(r - wobble - 0.52h, 0.018h, feather);
                    half broken = 0.45h + 0.55h * smoothstep(-0.75h, 0.72h, sin(p.x * 15.0h - p.y * 19.0h + age));
                    half foam = (1.0h - smoothstep(0.12h, 0.52h, r)) * (1.0h - smoothstep(0.0h, 0.47h, age));
                    alpha = (0.78h * outer + 0.48h * inner) * broken + 0.38h * foam * broken;
                }
                return half4(color, saturate(alpha * i.color.a));
            }
            ENDHLSL
        }
    }
}
