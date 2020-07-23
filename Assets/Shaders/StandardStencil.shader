Shader "Chess/StandardStencil"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _StencilMask("Stencil Mask", Int) = 0
    }

    CGINCLUDE

    uniform sampler2D _MainTex;
    uniform int _StencilMask;

    ENDCG

    SubShader
        {
            // assumes the stencil buffer is clear at the start of each frame
            Stencil {
                Ref 255
                WriteMask [_StencilMask]
                Comp Always
                Pass Replace // always write
                ZFail Replace // ALWAYS write
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
        Fallback "Standard" // shadows
}
