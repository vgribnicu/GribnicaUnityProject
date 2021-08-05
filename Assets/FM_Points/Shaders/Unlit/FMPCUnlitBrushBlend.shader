Shader "FMPCD/FMPCUnlitBrushBlend"
{
    Properties     
    {         
        _Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
        
        _Blend("Blend", Range(0,1)) = 0
        _MainTex ("Texture", 2D) = "white" {}
    }       
    SubShader     
    {   
        Tags { "RenderType"="Transparent" "IgnoreProjector"="True" "Queue"="Transparent" }
        Lighting Off
        AlphaToMask On
        Cull Off

        Blend SrcAlpha OneMinusSrcAlpha
        

        //Tags{ "RenderType" = "TransparentCutout" "Queue" = "AlphaTest"}
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag  
            #define IF(a, b, c) lerp(b, c, step((float) (a), 0));

            uniform float4 _Color;
            uniform float _Blend;     

            sampler2D _MainTex;      

            struct appdata             
            {                 
                float4 vertex: POSITION;                
                float4 color: COLOR;      

                float2 uv : TEXCOORD0;  
                float4 tangent: TANGENT;    
            };               

            struct v2f          
            {
                float4 pos: SV_POSITION;                 
                float4 col: COLOR;     
                float2 uv : TEXCOORD0;  
            };               

            v2f vert(appdata v)             
            {                 
                v2f o;                 
                o.pos = UnityObjectToClipPos(v.vertex);
                
                float3 color1 = v.color;
                float3 color2 = float3(v.tangent.x, v.tangent.y, v.tangent.z);
                o.col = float4(lerp(color1, color2, _Blend), 1);

                o.col.rgb *= _Color.rgb;
                #if !UNITY_COLORSPACE_GAMMA
                o.col.rgb = pow(o.col.rgb, 2.2);
                #endif

                o.col.a = _Color.a;
                o.uv = v.uv;

                return o;             
            }               

            float3 ApplySaturation(float3 c, float _Saturation) { return lerp(dot(c, float3(0.299, 0.587, 0.114)), c, _Saturation); }

            float4 frag(v2f o) : COLOR             
            {     
                float2 uv = o.uv;
                float4 col = o.col;
                col.a *= tex2D(_MainTex, uv).r;
                if(uv.x>0.5) col.rgb = ApplySaturation(col.rgb, 1 +  (1-col.a) );

                float dist = sqrt(pow((0.5 - uv.x), 2) + pow((0.5 - uv.y), 2));
                col.a = IF(dist > 0.25, 0, col.a);

                clip(col.a-0.3);

                return col;       
            }       

            ENDCG         
          }     
      } 
}
