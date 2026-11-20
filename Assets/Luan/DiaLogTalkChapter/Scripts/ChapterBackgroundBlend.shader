Shader "Custom/ChapterBackgroundBlend"
{
    Properties
    {
        _MainTex ("No-Combo Video", 2D) = "black" {}
        _ComboTex ("Combo Video", 2D) = "black" {}
        _LoopTex ("Loop Snapshot", 2D) = "black" {}
        _Blend ("Combo Blend", Range(0, 1)) = 0
        _LoopBlend ("Loop Blend", Range(0, 1)) = 0
        _LoopPulse ("Loop Pulse", Range(0, 1)) = 0
        _Brightness ("Brightness", Range(0.5, 2.0)) = 1.0
        _Vignette ("Vignette", Range(0, 1)) = 0.12
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "Queue"="Background" 
            "RenderPipeline"="UniversalPipeline" 
        }
        LOD 100
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_ComboTex);
            SAMPLER(sampler_ComboTex);

            TEXTURE2D(_LoopTex);
            SAMPLER(sampler_LoopTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Blend;
                float _LoopBlend;
                float _LoopPulse;
                float _Brightness;
                float _Vignette;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 noCombo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 combo = SAMPLE_TEXTURE2D(_ComboTex, sampler_ComboTex, input.uv);

                if (_LoopBlend > 0.001)
                {
                    half4 loopSnap = SAMPLE_TEXTURE2D(_LoopTex, sampler_LoopTex, input.uv);
                    noCombo = lerp(noCombo, loopSnap, _LoopBlend);
                    combo = lerp(combo, loopSnap, _LoopBlend);
                }

                half4 col = lerp(noCombo, combo, saturate(_Blend));

                if (_LoopPulse > 0.001)
                {
                    half3 pulseTint = half3(0.15, 0.35, 0.6) * _LoopPulse;
                    col.rgb += pulseTint * (1.0 - length(input.uv - 0.5));
                    col.rgb *= (1.0 + _LoopPulse * 0.2);
                }

                if (_Vignette > 0.001)
                {
                    float2 d = input.uv - 0.5;
                    float dist = dot(d, d);
                    float vig = 1.0 - dist * (_Vignette * 1.5);
                    col.rgb *= saturate(vig);
                }

                col.rgb *= _Brightness;
                return col;
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        LOD 100
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _ComboTex;
            sampler2D _LoopTex;
            float4 _MainTex_ST;
            float _Blend;
            float _LoopBlend;
            float _LoopPulse;
            float _Brightness;
            float _Vignette;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 noCombo = tex2D(_MainTex, i.uv);
                fixed4 combo = tex2D(_ComboTex, i.uv);

                if (_LoopBlend > 0.001)
                {
                    fixed4 loopSnap = tex2D(_LoopTex, i.uv);
                    noCombo = lerp(noCombo, loopSnap, _LoopBlend);
                    combo = lerp(combo, loopSnap, _LoopBlend);
                }

                fixed4 col = lerp(noCombo, combo, saturate(_Blend));

                if (_LoopPulse > 0.001)
                {
                    fixed3 pulseTint = fixed3(0.15, 0.35, 0.6) * _LoopPulse;
                    col.rgb += pulseTint * (1.0 - length(i.uv - 0.5));
                    col.rgb *= (1.0 + _LoopPulse * 0.2);
                }

                if (_Vignette > 0.001)
                {
                    float2 d = i.uv - 0.5;
                    float dist = dot(d, d);
                    float vig = 1.0 - dist * (_Vignette * 1.5);
                    col.rgb *= saturate(vig);
                }

                col.rgb *= _Brightness;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
