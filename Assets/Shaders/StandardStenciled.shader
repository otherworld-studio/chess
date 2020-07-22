Shader "Chess/StandardStenciled"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    CGINCLUDE

    uniform sampler2D _MainTex;

    ENDCG

    SubShader
        {
            Stencil {
                Ref 1
                WriteMask 1
                Comp Always
                Pass Replace
            }
            
            CGPROGRAM
            #pragma surface surf Standard

            struct Input {
                float2 uv_MainTex;
                float4 color : COLOR;
            };

            void surf(Input i, inout SurfaceOutputStandard o) {
                fixed4 c = tex2D(_MainTex, i.uv_MainTex);
                o.Albedo = c.rgb;
                o.Alpha = c.a;
            }

            ENDCG
        }
        Fallback "Standard"
}
