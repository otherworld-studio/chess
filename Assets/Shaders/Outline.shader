Shader "Chess/Outline"
{
    Properties
    {
        _Color("Outline Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Width("Outline Width", Range(0.0, 10.0)) = 5.0
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    uniform float4 _Color;
    uniform float _Width;

    ENDCG

    SubShader
        {
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "ForceNoShadowCasting" = "True" "IgnoreProjector" = "True"}

            Pass
            {
                ZWrite Off
                Blend SrcAlpha OneMinusSrcAlpha
                Cull Front

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
                  o.vertex.xy += TransformViewToProjection(viewNormal.xy) * (_Width * o.vertex.w * 2.0 / _ScreenParams.xy); // TODO: precalculate _OutlineWidth * 2.0 / _ScreenParams.xy
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
