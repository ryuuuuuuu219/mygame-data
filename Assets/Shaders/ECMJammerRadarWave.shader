Shader "Custom/ECMJammerRadarWave"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0, 1, 0, 1)
        _TimeOffset ("Time Offset", Float) = 0
        _WaveSpeed ("Wave Speed", Float) = 0.75
        _WaveCount ("Wave Count", Float) = 8
        _ThinWaveCount ("Thin Wave Count", Float) = 28
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

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
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float _TimeOffset;
            float _WaveSpeed;
            float _WaveCount;
            float _ThinWaveCount;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centered = i.uv * 2.0 - 1.0;
                float dist = length(centered);
                if (dist > 1.0)
                    discard;

                float t = _Time.y * _WaveSpeed + _TimeOffset;
                float outward = frac(dist * _WaveCount - t);
                float mainWave = smoothstep(0.0, 0.08, outward) * (1.0 - smoothstep(0.12, 0.22, outward));

                float thin = abs(frac(dist * _ThinWaveCount - t * 2.2) - 0.5) * 2.0;
                float thinWave = 1.0 - smoothstep(0.0, 0.22, thin);

                float clearPulse = 1.0 - smoothstep(0.0, 0.16, abs(frac(dist * 2.2 - t * 0.7) - 0.5) * 2.0);
                float alpha = lerp(0.72, 0.9, saturate(mainWave + thinWave * 0.5));
                alpha = lerp(alpha, 0.2, clearPulse);
                alpha *= smoothstep(1.0, 0.82, dist);

                fixed4 color = tex2D(_MainTex, i.uv) * _Color * i.color;
                color.a *= alpha;
                return color;
            }
            ENDCG
        }
    }
}
