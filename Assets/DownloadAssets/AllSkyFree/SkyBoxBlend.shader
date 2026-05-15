Shader "Custom/SkyBoxBlend"
{
    Properties
    {
        _CubemapA("Cubemap A", CUBE) = "" {}
        _CubemapB("Cubemap B", CUBE) = "" {}
        _Blend("Blend", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 dir         : TEXCOORD0;
            };

            TEXTURECUBE(_CubemapA);
            TEXTURECUBE(_CubemapB);
            SAMPLER(sampler_CubemapA);
            SAMPLER(sampler_CubemapB);

            CBUFFER_START(UnityPerMaterial)
                float _Blend;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.dir         = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 a = SAMPLE_TEXTURECUBE(_CubemapA, sampler_CubemapA, IN.dir);
                half4 b = SAMPLE_TEXTURECUBE(_CubemapB, sampler_CubemapB, IN.dir);
                return lerp(a, b, _Blend);
            }
            ENDHLSL
        }
    }
}