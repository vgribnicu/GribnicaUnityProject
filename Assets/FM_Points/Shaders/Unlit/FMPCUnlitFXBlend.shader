Shader "FMPCD/FMPCUnlitFXBlend"
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
            uniform float4 _WindDirection;  
            uniform float _Duration;  
            uniform float _WindPower;
            inline float GetLuma(float3 c) { return sqrt(dot(c, float3(0.299, 0.587, 0.114))); }

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

                if(_WindPower > 0 && length(_WindDirection.xyz) > 0 && _WindDirection.w > 0)
                {
                    //==============Position=================
                    float4 WPos = mul(unity_ObjectToWorld, v.vertex);
                    //float3 WNorm = mul(unity_ObjectToWorld, float4(v.normal, 0.0 ) ).xyz;
                    float3 WNorm = mul((float3x3)unity_ObjectToWorld, SCALED_NORMAL);

                    float3 ForceDir = _WindDirection.xyz;

                    float AngThresh = _WindDirection.w;
                    AngThresh *= _WindPower;
                    
                    float grad = acos(dot(-normalize(_WindDirection.xyz), WNorm));
                    grad = degrees(grad);    

                    //reflective direction
                    float3 RDir = normalize(reflect(normalize(-_WindDirection.xyz), WNorm));
                    float Duration = _Duration;
                    if( (length(WPos.xyz)%0.001/0.001) < saturate(_WindPower))
                    {
                        float offset_by_pos = (length(WPos.xyz)%0.001/0.001) * Duration;
                        float offset_by_luma = GetLuma(o.col.rgb) * Duration;
                        float explode_value = (_Time.y + offset_by_pos + offset_by_luma) % Duration;
                        explode_value /= Duration;

                        if(grad < 90)
                        {
                            if(grad > (90 - AngThresh))
                            { 
                                float effect = (grad - (90 -AngThresh) ) / AngThresh;
                                float3 WDir = (lerp(lerp(WNorm, -RDir, pow(effect, 1.0/_WindPower)), ForceDir, effect * pow(explode_value, 1.0/_WindPower))) * explode_value;
                                WDir *= effect;
                                WPos.xyz += WDir;
                            }
                        }        
                        else
                        {
                            grad -= 90.0;
                            if(grad < AngThresh)
                            { 
                                float effect = (1 - (grad / AngThresh));
                                float3 WDir = (lerp(lerp(WNorm, ForceDir, pow(effect, 1.0/_WindPower)), ForceDir, effect * pow(explode_value, 1.0/_WindPower))) * explode_value;
                                WDir *= effect;
                                WPos.xyz += WDir;
                            }
                        }  
                        float alpha_value = 1 - (abs(0.5-explode_value)*2);  
                        o.col.a *= explode_value < 0.5 ? pow(alpha_value, 1.0/5.0) : alpha_value;                           
                    }

                    float4 OPos = mul(unity_WorldToObject, WPos);
                    o.pos = UnityObjectToClipPos(OPos);
                    //==============Position=================
                }

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