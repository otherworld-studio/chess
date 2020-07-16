Shader "Chess/Outline"
{
    Properties
    {
        _Color("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _MainTex("Texture", 2D) = "white" {}
        _OutlineColor("Outline Color", Color) = (1.0, 1.0, 1.0, 1.0)
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
                Cull Front
                ZTest Always // Necessary when using Cull Front, to prevent clipping artefacts with the floor

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
                    //v.vertex.xyz += normalize(v.normal.xyz) * _OutlineWidth; This approach requires smoothed mesh normals (https://answers.unity.com/questions/625968/unitys-outline-shader-sharp-edges.html)
                    float norm = length(v.vertex.xyz);
                    v.vertex.xyz *= 1.0 + _OutlineWidth / norm;

                    v2f o; // Unity uses HLSL which doesn't support most struct constructors
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    return o;
                }

                ENDCG
            }

            Tags{ "Queue" = "Transparent" } // Without this, outline isn't rendered when the chess piece is behind another object

            CGPROGRAM
            #pragma surface surf Standard

            struct Input {
                float2 uv_MainTex;
                float4 color : COLOR;
            };

            void surf(Input IN, inout SurfaceOutputStandard o) {
                fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
                o.Albedo = c.rgb;
                o.Alpha = c.a;
            }

            ENDCG
        }
        Fallback "Standard" // Needed for shadows
}
