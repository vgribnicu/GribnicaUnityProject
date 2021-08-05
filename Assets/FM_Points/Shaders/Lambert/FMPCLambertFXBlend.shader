Shader "FMPCD/FMPCLambertFXBlend" 
{
    Properties 
    {
        _Color ("Diffuse Material Color", Color) = (1,1,1,1) 
        _PointSize("Point size", Range(0.00001, 100)) = 5.0
        [Toggle] _ApplyDistance("Apply Distance", Float) = 1
        
        _Blend("Blend", Range(0,1)) = 0
    }
    SubShader 
    {
        Pass 
        {   
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
            #pragma multi_compile_fwdbase 
            #pragma vertex vert
            #pragma fragment frag
 
            #include "UnityCG.cginc" 
            uniform float4 _LightColor0;
            uniform float4 _Color; 

            #define IF(a, b, c) lerp(b, c, step((float) (a), 0));
            uniform float _PointSize; 
            uniform float _ApplyDistance;
            
            uniform float _Blend;

            uniform float4 _WindDirection;
            uniform float _Duration;      
            uniform float _WindPower;
            inline float GetLuma(float3 c) { return sqrt(dot(c, float3(0.299, 0.587, 0.114))); }  
 
            struct appdata 
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;

                float4 color: COLOR;
                float4 tangent: TANGENT;   
            };

            struct v2f 
            {
                float4 pos : SV_POSITION;
                float4 posWorld : TEXCOORD0;
                float3 normalDir : TEXCOORD1;
                float3 vertexLighting : TEXCOORD2;

                float4 col: COLOR;  
                float size: PSIZE;
            };
 
            v2f vert(appdata v)
            {     
                v2f o;
 
                float4x4 modelMatrix = unity_ObjectToWorld;
                float4x4 modelMatrixInverse = unity_WorldToObject; 
         
                o.posWorld = mul(modelMatrix, v.vertex);
                o.normalDir = normalize(mul(float4(v.normal, 0.0), modelMatrixInverse).xyz);
                o.pos = UnityObjectToClipPos(v.vertex);
 
                float3 color1 = v.color;
                float3 color2 = float3(v.tangent.x, v.tangent.y, v.tangent.z);
                o.col = float4(lerp(color1, color2, _Blend) * _Color.rgb, 1);
                #if !UNITY_COLORSPACE_GAMMA
                o.col.rgb = pow(o.col.rgb, 2.2);
                #endif
                o.size = IF(_ApplyDistance > 0, _PointSize / o.pos.w * _ScreenParams.y, _PointSize);
                if(v.tangent.w > 0) o.size *= 0.5 + 0.5 * pow((abs(0.5 - _Blend) * 2), 2);

                if(_WindPower > 0 && length(_WindDirection.xyz) > 0 && _WindDirection.w > 0)
                {
                    //==============Position=================
                    //float4 WPos = mul(unity_ObjectToWorld, v.vertex);
                    //float3 WNorm = mul((float3x3)unity_ObjectToWorld, v.normal);
                    float4 WPos = o.posWorld;
                    float3 WNorm = o.normalDir;
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
                                    
                                o.normalDir = mul(unity_WorldToObject, saturate(WDir));
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
                               
                                o.normalDir = mul(unity_WorldToObject, saturate(WDir));
                            }
                        }  
                        float size_value = 1 - (abs(0.5-explode_value)*2);  
                        o.size *= explode_value < 0.5 ? pow(size_value, 1.0/5.0) : size_value;                              
                    }

                    float4 OPos = mul(unity_WorldToObject, WPos);
                    o.pos = UnityObjectToClipPos(OPos);
                    o.posWorld = WPos;
                    //==============Position=================
                }
    
                // Diffuse reflection by four "vertex lights"      
                o.vertexLighting = float3(0.0, 0.0, 0.0);
                #ifdef VERTEXLIGHT_ON
                for (int index = 0; index < 4; index++)
                {  
                    float4 lightPosition = float4(unity_4LightPosX0[index], unity_4LightPosY0[index], unity_4LightPosZ0[index], 1.0);
                    float3 vertexToLightSource = lightPosition.xyz - o.posWorld.xyz;    
                    float3 lightDirection = normalize(vertexToLightSource);
                    float squaredDistance = dot(vertexToLightSource, vertexToLightSource);
                    float attenuation = 1.0 / (1.0 + unity_4LightAtten0[index] * squaredDistance);
                    float3 diffuseReflection = attenuation * unity_LightColor[index].rgb * o.col.rgb * max(0.0, dot(o.normalDir, lightDirection));     
         
                    o.vertexLighting = o.vertexLighting + diffuseReflection;
                }
                #endif
                return o;
            }
 
            float4 frag(v2f v) : COLOR
            {
                float3 normalDirection = normalize(v.normalDir); 
                float3 viewDirection = normalize(_WorldSpaceCameraPos - v.posWorld.xyz);
                float3 lightDirection;
                float attenuation;
 
                if (0.0 == _WorldSpaceLightPos0.w) // directional light?
                {
                    attenuation = 1.0; // no attenuation
                    lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                } 
                else // point or spot light
                {
                    float3 vertexToLightSource = _WorldSpaceLightPos0.xyz - v.posWorld.xyz;
                    float distance = length(vertexToLightSource);
                    attenuation = 1.0 / distance; // linear attenuation 
                    lightDirection = normalize(vertexToLightSource);
                }
 
                float3 ambientLighting = UNITY_LIGHTMODEL_AMBIENT.rgb * v.col.rgb;
                float3 diffuseReflection = attenuation * _LightColor0.rgb * v.col.rgb * max(0.0, dot(normalDirection, lightDirection));
                return float4(v.vertexLighting + ambientLighting + diffuseReflection, 1.0);
            }
            ENDCG
        }
 
        Pass 
        {  
            Tags { "LightMode" = "ForwardAdd" } 
            Blend One One
 
            CGPROGRAM
 
            #pragma vertex vert 
            #pragma fragment frag 
 
            #include "UnityCG.cginc" 
            uniform float4 _LightColor0;
            uniform float4 _Color; 
            
            #define IF(a, b, c) lerp(b, c, step((float) (a), 0));
            uniform float _PointSize;  
            uniform float _ApplyDistance;

            uniform float _Blend;

            uniform float4 _WindDirection;
            uniform float _Duration;      
            uniform float _WindPower;
            inline float GetLuma(float3 c) { return sqrt(dot(c, float3(0.299, 0.587, 0.114))); }  

            struct appdata 
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color: COLOR;  
                float4 tangent: TANGENT;
            };
            struct v2f 
            {
                float4 pos : SV_POSITION;
                float4 posWorld : TEXCOORD0;
                float3 normalDir : TEXCOORD1;

                float4 col: COLOR;  
                float size: PSIZE;
            };
 
            v2f vert(appdata v) 
            {
                v2f o;
 
                float4x4 modelMatrix = unity_ObjectToWorld;
                float4x4 modelMatrixInverse = unity_WorldToObject; 
 
                o.posWorld = mul(modelMatrix, v.vertex);
                o.normalDir = normalize(mul(float4(v.normal, 0.0), modelMatrixInverse).xyz);
                o.pos = UnityObjectToClipPos(v.vertex);

                float3 color1 = v.color;
                float3 color2 = float3(v.tangent.x, v.tangent.y, v.tangent.z);
                o.col = float4(lerp(color1, color2, _Blend) * _Color.rgb, 1);
                #if !UNITY_COLORSPACE_GAMMA
                o.col.rgb = pow(o.col.rgb, 2.2);
                #endif
                o.size = IF(_ApplyDistance > 0, _PointSize / o.pos.w * _ScreenParams.y, _PointSize);
                if(v.tangent.w > 0) o.size *= 0.5 + 0.5 * pow((abs(0.5 - _Blend) * 2), 2);

                if(_WindPower > 0 && length(_WindDirection.xyz) > 0 && _WindDirection.w > 0)
                {
                    //==============Position=================
                    //float4 WPos = mul(unity_ObjectToWorld, v.vertex);
                    //float3 WNorm = mul((float3x3)unity_ObjectToWorld, v.normal);
                    float4 WPos = o.posWorld;
                    float3 WNorm = o.normalDir;
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
                                    
                                o.normalDir = mul(unity_WorldToObject, saturate(WDir));
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
                               
                                o.normalDir = mul(unity_WorldToObject, saturate(WDir));
                            }
                        }  
                        float size_value = 1 - (abs(0.5-explode_value)*2);  
                        o.size *= explode_value < 0.5 ? pow(size_value, 1.0/5.0) : size_value;                              
                    }

                    float4 OPos = mul(unity_WorldToObject, WPos);
                    o.pos = UnityObjectToClipPos(OPos);
                    o.posWorld = WPos;
                    //==============Position=================
                }

                return o;
            }
 
            float4 frag(v2f v) : COLOR
            {
                float3 normalDirection = normalize(v.normalDir);
 
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - v.posWorld.xyz);
                float3 lightDirection;
                float attenuation;
 
                if (0.0 == _WorldSpaceLightPos0.w) // directional light?
                {
                    attenuation = 1.0; // no attenuation
                    lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                } 
                else // point or spot light
                {
                    float3 vertexToLightSource = _WorldSpaceLightPos0.xyz - v.posWorld.xyz;
                    float distance = length(vertexToLightSource);
                    attenuation = 1.0 / distance; // linear attenuation 
                    lightDirection = normalize(vertexToLightSource);
                }
 
                float3 diffuseReflection = attenuation * _LightColor0.rgb * v.col.rgb * max(0.0, dot(normalDirection, lightDirection));
                // no ambient lighting in this passs
                return float4(diffuseReflection, 1.0);
            }
            ENDCG
        }
    } 
}