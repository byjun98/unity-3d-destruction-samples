Shader "Slicing/CapUnlit"
{
    // Tiny double-sided unlit shader for slice caps.
    // Why: the centroid-fan triangulation can emit triangles with mixed winding,
    // which under standard backface culling shows as missing wedges + a star/stripe
    // pattern as adjacent surfaces fight for the same depth. Cull Off removes that
    // artifact entirely so the cut surface reads as one clean disc.
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _EmissionColor ("Emission Color", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _BaseColor;
            float4 _EmissionColor;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return half4(_BaseColor.rgb + _EmissionColor.rgb, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
