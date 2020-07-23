Shader "Chess/Outline"
{
    Properties
    {
        _Color("Outline Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Width("Outline Width", Range(0.0, 8.0)) = 4.0
        _StencilMask("Stencil Mask", Int) = 0
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    uniform float4 _Color;
    uniform float _Width;
    uniform int _StencilMask;

    ENDCG

    SubShader
        {
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "ForceNoShadowCasting" = "True" "IgnoreProjector" = "True"}

            Pass
            {
                Tags { "LightMode" = "Always" }
                ZWrite Off
                ZTest Always // render above other geometry
                Blend SrcAlpha OneMinusSrcAlpha
                Cull Front

                Stencil {
                    Ref 0
                    ReadMask [_StencilMask]
                    Comp Equal // discard if the value read is nonzero
                }

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                struct appdata {
                    float4 vertex : POSITION;
                    float3 color : COLOR;
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                };

                v2f vert(appdata v) {
                  v2f o;
                  o.vertex = UnityObjectToClipPos(v.vertex);
                  float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.color);
                  float3 clipNormal = TransformViewToProjection(viewNormal);
                  float norm = length(clipNormal.xy);
                  if (norm != 0.0)
                    o.vertex.xy += clipNormal.xy * (_Width * o.vertex.w * 2.0 / (norm * _ScreenParams.xy)); // TODO: precalculate _Width * 2.0 / _ScreenParams.xy
                  
                  return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    return _Color;
                }

                ENDCG
            }
        }
}
