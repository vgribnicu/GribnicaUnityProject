Shader "FMPCD/FMPCUnlitBlend"
{    
    Properties     
    {         
        _Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _PointSize("Point size", Range(0.00001, 100)) = 0.02

        _Blend("Blend", Range(0,1)) = 0
        [Toggle] _ApplyDistance("Apply Distance", Float) = 1
    }           
    SubShader     
    {   
        Tags { "RenderType"="Transparent" "IgnoreProjector"="True" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag  
            #include "UnityCG.cginc" 
            #define IF(a, b, c) lerp(b, c, step((float) (a), 0));

            uniform float4 _Color;
            uniform float _PointSize; 
            uniform float _Blend;  
            uniform float _ApplyDistance;           

            struct appdata             
            {                 
                float4 vertex: POSITION;                 
                float4 color: COLOR; 

                float3 normal   : NORMAL;
                float2 uv: TEXCOORD0;
                float4 tangent: TANGENT;          
            };               

            struct v2f          
            {
                float4 pos: SV_POSITION;                 
                float4 col: COLOR;                 
                float size: PSIZE;     
 
                float2 uv: TEXCOORD0;      
            };               

            v2f vert(appdata v)             
            {                 
                v2f o;                 
                o.pos = UnityObjectToClipPos(v.vertex); 
                
                float3 color1 = v.color;
                float3 color2 = float3(v.tangent.x, v.tangent.y, v.tangent.z);
                o.col = float4(lerp(color1, color2, _Blend), 1);

                o.col *= _Color;
                #if !UNITY_COLORSPACE_GAMMA
                o.col = pow(o.col, 2.2);
                #endif

                o.col.a = _Color.a;

                o.size = IF(_ApplyDistance > 0, _PointSize / o.pos.w * _ScreenParams.y, _PointSize);
                #ifdef SHADER_API_MOBILE
                //o.size *= 2;
                #endif

                if(v.tangent.w > 0) o.size *= 0.5 + 0.5 * pow((abs(0.5 - _Blend) * 2), 2);
                

                return o;             
            }               

            float4 frag(v2f i) : COLOR             
            {   
                return i.col;  
            }             
            ENDCG         
          }     
      } 
}