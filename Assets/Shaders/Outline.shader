Shader "Chess/Outline"
{
    Properties
    {
        _Color("Main Color", Color) = (0.5, 0.5, 0.5, 1)
        _MainTex("Texture", 2D) = "white" {}
        _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth("Outline Width", Range(0.0, 1.0)) = 0.1
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    uniform float4 _Color;
    uniform sampler2D _MainTex;
    uniform float4 _OutlineColor;
    uniform float _OutlineWidth;

    ENDCG

    SubShader
        {
            Pass // Outline
            {
                Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "ForceNoShadowCasting" = "True" "IgnoreProjector" = "True" }
                Blend SrcAlpha OneMinusSrcAlpha // TODO: this necessary?
                ZWrite Off

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                struct appdata {
                    float4 vertex : POSITION;
                    float4 normal : NORMAL;
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                };

                fixed4 frag(v2f i) : SV_Target
                {
                    return _OutlineColor;
                }

                v2f vert(appdata v) {
                    v.vertex.xyz += normalize(v.normal.xyz) * _OutlineWidth;

                    v2f o; // Unity uses HLSL which doesn't support most struct constructors
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    return o;
                }

                ENDCG
            }

            // Surface shader (borrowed from Shrimpey)
            CGPROGRAM
            #pragma surface surf Lambert noshadow

            struct Input {
                float2 uv_MainTex;
                float4 color : COLOR;
            };

            void surf(Input IN, inout SurfaceOutput  o) {
                fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
                o.Albedo = c.rgb;
                o.Alpha = c.a;
            }

            ENDCG
        }
}
