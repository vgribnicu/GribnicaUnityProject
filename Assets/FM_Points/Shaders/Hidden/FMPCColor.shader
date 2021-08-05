Shader "Hidden/FMPCColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define IF(a, b, c) lerp(b, c, step((float) (a), 0));
            #include "UnityCG.cginc"

            uniform sampler2D _MainTex;
            uniform float4 _MainTex_TexelSize;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
       
            v2f vert (appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
               
                #if UNITY_UV_STARTS_AT_TOP
                o.uv.y = IF(_MainTex_TexelSize.y<0, 1-o.uv.y, o.uv.y);
                #endif              
               
                return o;
            }
   
            float4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                 
                #if !UNITY_COLORSPACE_GAMMA
                col.rgb = pow(col.rgb, 1.0/2.2);
                #endif
                return col;
            }
            ENDCG
        }
    }
}
