Shader "Hidden/FMPCDepth"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Debug ("Debug", float) = 0
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

            uniform sampler2D _CameraDepthTexture;
            uniform sampler2D _CameraDepthNormalsTexture;
            uniform float4 _CameraDepthNormalsTexture_TexelSize;

            uniform float _Debug;

            uniform float4x4 UNITY_MATRIX_IV;

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
                float4 depthnormal = tex2D(_CameraDepthNormalsTexture, i.uv);
                
                //decode depthnormal
                float3 normal;
                float RawDepth;
                DecodeDepthNormal(depthnormal, RawDepth, normal);
                col.a = RawDepth;

                float3 worldNormal = mul(UNITY_MATRIX_IV, float4(normal.xyz, 0)).xyz;
                //float3 worldPoint = mul(UNITY_MATRIX_IV, float4(i.pos.xyz, 1)).xyz;
                col.rgb = worldNormal;
                //col.rgb = worldPoint;
                //return col;
                 
                #if !UNITY_COLORSPACE_GAMMA
                //col.rgb = pow(col.rgb, 1.0/2.2);
                #endif
                        
                if(_Debug == 1)
                {
                    col.rgb = worldNormal;
                    if(col.a >= 1) col.rgb = float3(0,0,0);
                }
                if(_Debug == 2) col.r = col.g = col.b = col.a;
                return col;
            }
            ENDCG
        }
    }
}
