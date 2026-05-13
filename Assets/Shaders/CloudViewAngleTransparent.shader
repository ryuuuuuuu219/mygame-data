Shader "Custom/CloudViewAngleTransparent"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 0.3)
        _AngleFadePower ("Angle Fade Power", Range(0.25, 8)) = 2
        _MinAlphaFactor ("Min Alpha Factor", Range(0, 1)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent-100"
            "RenderType" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "CloudViewAngleTransparent"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            half _AngleFadePower;
            half _MinAlphaFactor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.positionWS = mul(unity_ObjectToWorld, input.positionOS).xyz;
                output.normalWS = UnityObjectToWorldNormal(input.normalOS);
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);
                half viewFacing = saturate(abs(dot(normalWS, viewDirWS)));
                half angleAlpha = pow(viewFacing, _AngleFadePower);

                half4 color = _BaseColor;
                color.a *= lerp(_MinAlphaFactor, 1.0h, angleAlpha);
                return color;
            }
            ENDCG
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
