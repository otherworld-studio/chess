Shader "Chess/Outline"
{
    Properties
    {
        _Color("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _MainTex("Texture", 2D) = "white" {}
        _OutlineColor("Outline Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _OutlineWidth("Outline Width", Range(0.0, 10.0)) = 5.0
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    uniform float4 _Color;
    uniform sampler2D _MainTex;
    uniform float4 _OutlineColor;
    uniform float _OutlineWidth;

    ENDCG

    // TODO: stencil buffer to block outline with object (https://forum.unity.com/threads/render-an-object-only-if-the-object-is-behind-a-specific-object.429525/)

    SubShader
        {
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

            Pass // Outline
            {
                Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "ForceNoShadowCasting" = "True" "IgnoreProjector" = "True" }
                ZWrite Off
                Blend SrcAlpha OneMinusSrcAlpha
                Cull Front

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                struct appdata {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
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
                    // This approach requires smoothed mesh normals (https://answers.unity.com/questions/625968/unitys-outline-shader-sharp-edges.html)
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                    float2 clipNormal = TransformViewToProjection(viewNormal.xy);
                    float norm = length(clipNormal);
                    // TODO: division by zero
                    o.vertex.xy += clipNormal * (_OutlineWidth * o.vertex.w * 2.0 / (_ScreenParams.xy * norm)); // TODO: precalculate _OutlineWidth * 2.0 / _ScreenParams.xy
                    return o;
                }

                ENDCG
            }
        }
        Fallback "Standard" // Shadows
}
