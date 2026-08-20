Shader "Custom/AlwaysOnTopParticle"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 0.85)
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent+500" 
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "IgnoreProjector"="True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _BaseColor;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return input.color;
            }
            ENDHLSL
        }
    }
    Fallback "Sprites/Default"
}
