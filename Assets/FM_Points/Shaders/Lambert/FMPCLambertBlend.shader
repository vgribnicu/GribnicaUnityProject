Shader "FMPCD/FMPCLambertBlend" 
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
 
            float4 frag(v2f i) : COLOR
            {
                float3 normalDirection = normalize(i.normalDir); 
                float3 viewDirection = normalize(_WorldSpaceCameraPos - i.posWorld.xyz);
                float3 lightDirection;
                float attenuation;
 
                if (0.0 == _WorldSpaceLightPos0.w) // directional light?
                {
                    attenuation = 1.0; // no attenuation
                    lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                } 
                else // point or spot light
                {
                    float3 vertexToLightSource = _WorldSpaceLightPos0.xyz - i.posWorld.xyz;
                    float distance = length(vertexToLightSource);
                    attenuation = 1.0 / distance; // linear attenuation 
                    lightDirection = normalize(vertexToLightSource);
                }
 
                float3 ambientLighting = UNITY_LIGHTMODEL_AMBIENT.rgb * i.col.rgb;
                float3 diffuseReflection = attenuation * _LightColor0.rgb * i.col.rgb * max(0.0, dot(normalDirection, lightDirection));
                return float4(i.vertexLighting + ambientLighting + diffuseReflection, 1.0);
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

                return o;
            }
 
            float4 frag(v2f i) : COLOR
            {
                float3 normalDirection = normalize(i.normalDir);
 
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 lightDirection;
                float attenuation;
 
                if (0.0 == _WorldSpaceLightPos0.w) // directional light?
                {
                    attenuation = 1.0; // no attenuation
                    lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                } 
                else // point or spot light
                {
                    float3 vertexToLightSource = _WorldSpaceLightPos0.xyz - i.posWorld.xyz;
                    float distance = length(vertexToLightSource);
                    attenuation = 1.0 / distance; // linear attenuation 
                    lightDirection = normalize(vertexToLightSource);
                }
 
                float3 diffuseReflection = attenuation * _LightColor0.rgb * i.col.rgb * max(0.0, dot(normalDirection, lightDirection));
                // no ambient lighting in this passs
                return float4(diffuseReflection, 1.0);
            }
            ENDCG
        }
    } 
}