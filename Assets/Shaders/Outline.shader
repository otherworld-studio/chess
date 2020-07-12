Shader "Unlit/Outline"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth("Outline Width", Range(0.0, 1.0)) = 0.1
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    struct appdata {
        float4 vertex : POSITION;
        float4 normal : NORMAL;
    };
    
    uniform sampler2D _MainTex;
    uniform float4 _OutlineColor;
    uniform float _OutlineWidth;

    ENDCG

    SubShader
        {
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "ForceNoShadowCasting" = "True" "IgnoreProjector" = "True" }
            Blend SrcAlpha OneMinusSrcAlpha // TODO: this necessary?
            ZWrite Off
            
            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                };
                
                v2f vert(appdata v)
                {
                    v.vertex.xyz += normalize(v.normal.xyz) * _OutlineWidth;

                    v2f o; // Unity uses HLSL which doesn't support most struct constructors
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    return _OutlineColor;
                }

                ENDCG
            }
        }
}
