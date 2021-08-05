// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/FMPCRTFXRender"
{
    Properties     
    {
        [HideInInspector]_MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HideInInspector] _PointSize("Point size", Range(0.00001, 100)) = 0.02
        [HideInInspector][Toggle] _ApplyDistance("Apply Distance", Float) = 1

        [HideInInspector][Toggle] _OrthographicProjection("Orthographic Projection", Float) = 0

        [HideInInspector] _OrthographicSize("OrthographicSize", Range(0.00001, 100)) = 1
        [HideInInspector] _Aspect("Aspect", Range(0.00001, 100)) = 1

        [HideInInspector] _NearClipPlane("NearClipPlane", Range(0.00001, 100)) = 0.3
        [HideInInspector] _FarClipPlane("FarClipPlane", Range(0.00001, 100)) = 10
        [HideInInspector] _VerticalFOV("VerticalFOV", Range(0.00001, 180)) = 60
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
            uniform float _ApplyDistance;

            uniform float _OrthographicSize;
            uniform float _Aspect;

            uniform float _NearClipPlane;
            uniform float _FarClipPlane;

            uniform float _VerticalFOV;
            uniform float _OrthographicProjection;

            struct appdata             
            {                 
                float4 vertex: POSITION;                 
                float4 color: COLOR;       
                float2 uv : TEXCOORD0;  
            };               

            struct v2f          
            {
                float4 pos: SV_POSITION;                 
                float4 col: COLOR;                 
                float size: PSIZE;     
                float2 uv : TEXCOORD0;
            };               


            sampler2D _MainTex;
            float4 _MainTex_ST;

            float mod(float x, float y){ return x - y * floor(x/y); }
            float RGBToFloat(float3 rgb, float scale)
            {
                return rgb.r + (rgb.g/scale)+ (rgb.b/(scale*scale));
            }
            float3 FloatToRGB(float v, float scale)
            {
                float r = v;
                float g = mod(v*scale,1.0);
                r-= g/scale;
                float b = mod(v*scale*scale,1.0);
                g-=b/scale;
                return float3(r,g,b);
            }

            v2f vert(appdata v)             
            {                 
                v2f o;

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                float4 pos = v.vertex;

                float2 uvDepth = pos.xy;
                float2 uvColor = pos.xy;
                uvDepth.x /= 2.0;
                
                float4 sampleDepth = tex2Dlod(_MainTex, float4(uvDepth, 0.0, 0.0));
                float decodeDepth = RGBToFloat(sampleDepth.rgb, 1.0);


                if(_OrthographicProjection > 0)
                {
                    //=========== orth ==============
                    float depthClip = _FarClipPlane - _NearClipPlane;
                    pos.z = _NearClipPlane + (decodeDepth * depthClip);
                    pos.z = decodeDepth * _FarClipPlane;

                    pos.y -= 0.5;
                    pos.y *= _OrthographicSize * 2;

                    pos.x -= 0.5;
                    pos.x *= _OrthographicSize * 2 * _Aspect;
                }
                else
                {
                    //=========== fov ==============
                    float vfov = ((_VerticalFOV)/180) * 3.14159265359;
                    float hfov = 2.0 * atan(tan(vfov / 2) * _Aspect);

                    float3 dir = float3(1,1,1);
                    float d = _FarClipPlane;
                    dir.z = d;

                    pos.y -= 0.5;
                    pos.y *= 2.0;
                    pos.y *= tan(vfov * 0.5) * d;
                    float angY = atan(pos.y/d);
                    dir.y = tan(angY) * d;

                    pos.x -= 0.5;
                    pos.x *= 2.0;
                    pos.x *= tan(hfov * 0.5) * d;

                    //pos.x /= _Aspect;
                    float angX = atan(pos.x/d);
                    dir.x = tan(angX) * d;
                    pos.xyz = decodeDepth * dir;
                }

                o.pos = UnityObjectToClipPos(pos);

                o.col = v.color;

                //new method
                uvColor.x /= 2.0;
                uvColor.x += 0.5;
                float4 sampleColor = tex2Dlod(_MainTex, float4(uvColor, 0.0, 0.0));
                o.col = sampleColor;
                    
                o.col.rgb *= _Color.rgb;
                #if !UNITY_COLORSPACE_GAMMA
                o.col.rgb = pow(o.col.rgb, 2.2);
                #endif

                o.col.a = _Color.a;

                o.size = IF(_ApplyDistance > 0, _PointSize / o.pos.w * _ScreenParams.y, _PointSize);
                #ifdef SHADER_API_MOBILE
                //o.size *= 2;
                #endif

                
                if(decodeDepth > 0.96) o.col.a = 0;
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
