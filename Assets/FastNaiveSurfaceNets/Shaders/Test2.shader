Shader "Unlit/Test2"
{
    Properties
    {
        _MainTex0 ("Texture", 2D) = "white" {}
        _MainTex1 ("Texture", 2D) = "white" {}
        _MainTex2 ("Texture", 2D) = "white" {}

        _AlbedoTex ("Albedo", 2DArray) = "white" {}
        
        _Blend ("Blend",Range(0,1)) = 1.0

        _BlendStart ("Blend Range Start", Range(0,1)) = .2
        _BlendEnd   ("Blend Range End  ", Range(0,1)) = .8
    }
    SubShader
    {
        Tags { 
            "RenderType"="Opaque" 
           
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            // Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma exclude_renderers gles
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma require 2darray
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            

            struct appdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;
                float3 uv0     : TEXCOORD0;
            };

            struct v2g
            {
                float4 vertex  : POSITION;
                float3 uv0     : TEXCOORD0;
                float3 normal  : TEXCOORD1;
                float4 tangent : TANGENT;                
                LIGHTING_COORDS (2,3)
                
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                
                float2 Tex0_uv : TEXCOORD0;
                float2 Tex1_uv : TEXCOORD1;
                float2 Tex2_uv : TEXCOORD2;
                int3   material : TEXCOORD3;
                float3  color    : TEXCOORD4;
                
                UNITY_FOG_COORDS(1)
            };

            

            float InverseLerp (float a, float b, float v)
            {
                return (v-a)/(b-a);
            }

            float _Blend;

            sampler2D _MainTex0;
            sampler2D _MainTex1;
            sampler2D _MainTex2;

        

            float _BlendStart;
            float _BlendEnd;

            float2 toTangentSpace (float3 tangent,float3 bitangent, float3 a)
            {
                float2 b = float2 (0,0);
                float3 normal = cross (tangent,bitangent);// * tangent.w * unity_WorldTransformParams.w;
                float3x3 mtxTangToWorld = {
                    tangent.x, bitangent.x, normal.x,
                    tangent.y, bitangent.y, normal.y,
                    tangent.z, bitangent.z, normal.z,
                    };
                return mul (a,mtxTangToWorld).xy;
                
            }

            float2 toTanSp (float3 tangent, float3 bitangent, float3 a)
            {
                
                //float3 bitangent = cross (normal,tangent) * tangent.w * unity_WorldTransformParams.w;
                float2x3 mtx = 
                {
                    tangent.x, tangent.y, tangent.z,
                    bitangent.x, bitangent.y, bitangent.z,                    
                };
                return normalize( mul (mtx,a) );
               
            }

            float2 CalcUV_InTangentPlane_UV (float3 u, float3 v, float3 x)
            {
                return float2(
                    dot(x, u) / dot(u, u),
                    dot(x, v) / dot(v, v)
                );
            }

            v2g vert (appdata In)
            {
                v2g Out;

                Out.vertex = (In.vertex);
                Out.normal = In.normal;
                Out.tangent = In.tangent;
                //Out.normal = UnityObjectToWorldNormal( In.normal);
                //Out.tangent = UnityObjectToWorldDir (In.tangent.xyz);
                Out.uv0 = In.uv0;
                UNITY_TRANSFER_FOG(Out,Out.vertex);
                
                return Out;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g In[3], inout TriangleStream<g2f> triStream)
            {
                g2f Out[3];

                float4 color_table[3] = 
                {
                    float4 (1,0,0,1),
                    float4 (0,1,0,1),
                    float4 (0,0,1,1)
                };
                int3 matindex =
                {
                    In[0].uv0.z,
                    In[1].uv0.z,
                    In[2].uv0.z
                };

                float3 bitangent[3] =
                {
                    cross (In[0].normal,In[0].tangent.xyz),// * In[0].tangent.w,// * unity_WorldTransformParams.w,
                    cross (In[1].normal,In[1].tangent.xyz),// * In[1].tangent.w,// * unity_WorldTransformParams.w,
                    cross (In[2].normal,In[2].tangent.xyz),// * In[2].tangent.w,// * unity_WorldTransformParams.w
                };

                for(int i = 0; i < 3; i++)
                {   //bitangent[i] = normalize(bitangent[i]);
                    Out[i].vertex = UnityObjectToClipPos(In[i].vertex);
                    UNITY_TRANSFER_FOG(Out,Out.vertex);
                    //Out[i].uv0 = float3 ( TRANSFORM_TEX(In[i].uv0, _MainTex0), In[i];
                    //matindex = In[i].uv0.z;
                    //Out[i].color = color_table[matindex[i]];  
                    Out[i].material = matindex;
                }   
                
                Out[0].color = float3 (1,0,0);  
                Out[1].color = float3 (0,1,0); 
                Out[2].color = float3 (0,0,1);

                Out[0].Tex0_uv = float3(0.5,0.5,0.5)+CalcUV_InTangentPlane_UV (In[0].tangent,bitangent[0], float3(0,0,0));
                Out[1].Tex0_uv = float3(0.5,0.5,0.5)+CalcUV_InTangentPlane_UV (In[0].tangent,bitangent[0], In[1].vertex-In[0].vertex);
                Out[2].Tex0_uv = float3(0.5,0.5,0.5)+CalcUV_InTangentPlane_UV (In[0].tangent,bitangent[0], In[2].vertex-In[0].vertex);

                
                Out[0].Tex1_uv = float3(0.5,0.5,0.5)+CalcUV_InTangentPlane_UV (In[1].tangent,bitangent[1], In[0].vertex-In[1].vertex);
                Out[1].Tex1_uv = float3(0.5,0.5,0.5)+CalcUV_InTangentPlane_UV (In[1].tangent,bitangent[1], float3(0,0,0));
                Out[2].Tex1_uv = float3(0.5,0.5,0.5)+CalcUV_InTangentPlane_UV (In[1].tangent,bitangent[1], In[2].vertex-In[1].vertex);


                Out[0].Tex2_uv = float3(0.5,0.5,0.5)+CalcUV_InTangentPlane_UV (In[2].tangent,bitangent[2], In[0].vertex-In[2].vertex);
                Out[1].Tex2_uv = float3(0.5,0.5,0.5)+CalcUV_InTangentPlane_UV (In[2].tangent,bitangent[2], In[1].vertex-In[2].vertex);
                Out[2].Tex2_uv = float3(0.5,0.5,0.5)+CalcUV_InTangentPlane_UV (In[2].tangent,bitangent[2], float3(0,0,0));

                triStream.Append(Out[0]);
                triStream.Append(Out[1]);
                triStream.Append(Out[2]);
                triStream.RestartStrip();
            }

            UNITY_DECLARE_TEX2DARRAY(_AlbedoTex);

            float4 frag (g2f In) : SV_Target
            {
                float4 col[3];
                col[0] = UNITY_SAMPLE_TEX2DARRAY(_AlbedoTex, float3 (In.Tex0_uv.xy, In.material[0])) * In.color.x;
                col[1] = UNITY_SAMPLE_TEX2DARRAY(_AlbedoTex, float3 (In.Tex1_uv.xy, In.material[1])) * In.color.y;
                col[2] = UNITY_SAMPLE_TEX2DARRAY(_AlbedoTex, float3 (In.Tex2_uv.xy, In.material[2])) * In.color.z;
       
                // sample the texture
                //col[0] = tex2D(_MainTex0, In.Tex0_uv) * In.color.x;  
                //col[1] = tex2D(_MainTex1, In.Tex1_uv) * In.color.y; 
                //col[2] = tex2D(_MainTex2, In.Tex2_uv) * In.color.z;

                float4 outCol = col[0] + col[1] + col[2] ;

                // apply fog
                UNITY_APPLY_FOG(In.fogCoord, outCol);
                
                return outCol;
            }
            ENDCG
        }
    }
}
