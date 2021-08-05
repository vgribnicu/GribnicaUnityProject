Shader "FMPCD/FMPCUnlit" 
{    
    Properties     
    {         
        _Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _PointSize("Point size", Range(0.00001, 100)) = 0.02
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
            #define IF(a, b, c) lerp(b, c, step((float) (a), 0));

            uniform float4 _Color;
            uniform float _PointSize;  
            uniform float _ApplyDistance;            

            struct appdata             
            {                 
                float4 vertex: POSITION;                 
                float4 color: COLOR;         
            };               

            struct v2f          
            {
                float4 pos: SV_POSITION;                 
                float4 col: COLOR;                 
                float size: PSIZE;     
            };               

            v2f vert(appdata v)             
            {                 
                v2f o;                 
                o.pos = UnityObjectToClipPos(v.vertex);
                o.col = v.color;    
                o.col.rgb *= _Color.rgb;
                #if !UNITY_COLORSPACE_GAMMA
                o.col.rgb = pow(o.col.rgb, 2.2);
                #endif

                o.col.a = _Color.a;

                o.size = IF(_ApplyDistance > 0, _PointSize / o.pos.w * _ScreenParams.y, _PointSize);
                #ifdef SHADER_API_MOBILE
                //o.size *= 2;
                #endif

                return o;             
            }               

            float4 frag(v2f o) : COLOR             
            {            
                return o.col;             
            }             
            ENDCG         
          }     
      } 
}