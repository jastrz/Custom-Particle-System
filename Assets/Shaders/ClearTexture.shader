Shader "Hidden/ClearTexture"
{
    Properties
    {
        _ClearColor ("Clear Color", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        CGINCLUDE
        #include "UnityCG.cginc"

        float4 _ClearColor;

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        v2f vert(appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }
        ENDCG

        Pass
        {
            Name "ClearFloat"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 frag(v2f i) : SV_Target
            {
                return _ClearColor;
            }
            ENDCG
        }

        Pass
        {
            Name "ClearInt"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            int frag(v2f i) : SV_Target
            {
                return (int)_ClearColor.r;
            }
            ENDCG
        }
    }
}