struct appdata
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
#pragma exclude_renderers gles
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float3 uv0 : TEXCOORD0;
};

struct v2g
{
    float4 vertex : POSITION;
    float3 uv0 : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float4 tangent : TEXCOORD2;                 
};

struct g2f
{
    float4 vertex : SV_POSITION;           
    float2 Tex0_uv : TEXCOORD0;
    float2 Tex1_uv : TEXCOORD1;
    float2 Tex2_uv : TEXCOORD2;
    int3 material : TEXCOORD3;
    float3 color : TEXCOORD4;
    float3 normal : TEXCOORD5;
    float3 wPos : TEXCOORD6;
    float4 tangent : TEXCOORD7;
    LIGHTING_COORDS (8,9)
    
};

            

float InverseLerp(float a, float b, float v)
{
    return (v - a) / (b - a);
}

float _Blend;
      
float _BlendStart;
float _BlendEnd;

float2 toTangentSpace(float3 tangent, float3 bitangent, float3 a)
{
    float2 b = float2(0, 0);
    float3 normal = cross(tangent, bitangent); // * tangent.w * unity_WorldTransformParams.w;
    float3x3 mtxTangToWorld = 
    {
        tangent.x, bitangent.x, normal.x,
        tangent.y, bitangent.y, normal.y,
        tangent.z, bitangent.z, normal.z,
    };
    return mul(a, mtxTangToWorld).xy;
                
}

float2 toTanSp(float3 tangent, float3 bitangent, float3 a)
{
    //float3 bitangent = cross (normal,tangent) * tangent.w * unity_WorldTransformParams.w;
    float2x3 mtx =
    {
        tangent.x, tangent.y, tangent.z,
        bitangent.x, bitangent.y, bitangent.z,
    };
    return normalize(mul(mtx, a));               
}

float2 CalcUV_InTangentPlane_UV(float3 u, float3 v, float3 x)
{
    return float2(
        dot(x, u) / dot(u, u),
        dot(x, v) / dot(v, v)
    );
}

v2g vert(appdata v)
{
    v2g Out;
    Out.vertex = v.vertex;
    Out.normal = v.normal;
    Out.tangent = v.tangent;
    Out.uv0 = v.uv0;
    return Out;
}

[maxvertexcount(3)]
void geom(triangle v2g In[3], inout TriangleStream<g2f> triStream)
{
    g2f Out[3];

    float4 color_table[3] =
    {
        float4(1, 0, 0, 1),
                    float4(0, 1, 0, 1),
                    float4(0, 0, 1, 1)
    };
    int3 matindex =
    {
        In[0].uv0.z,
                    In[1].uv0.z,
                    In[2].uv0.z
    };

    float3 bitangent[3] =
    {
        cross(In[0].normal, In[0].tangent.xyz), // * In[0].tangent.w,// * unity_WorldTransformParams.w,
        cross(In[1].normal, In[1].tangent.xyz), // * In[1].tangent.w,// * unity_WorldTransformParams.w,
        cross(In[2].normal, In[2].tangent.xyz), // * In[2].tangent.w,// * unity_WorldTransformParams.w
    };
    
    v2g v;
    for (int i = 0; i < 3; i++)
    { //bitangent[i] = normalize(bitangent[i]);
        v = In[i];
        Out[i].vertex = UnityObjectToClipPos(In[i].vertex);
        TRANSFER_VERTEX_TO_FRAGMENT(Out[i]);
        Out[i].wPos = mul(unity_ObjectToWorld, In[i].vertex);
        //UNITY_TRANSFER_FOG(Out, Out.vertex);

        Out[i].material = matindex;
        Out[i].normal = UnityObjectToWorldNormal(In[i].normal);
        Out[i].tangent = In[i].tangent;

    }
                
    Out[0].color = float3(1, 0, 0);
    Out[1].color = float3(0, 1, 0);
    Out[2].color = float3(0, 0, 1);

    Out[0].Tex0_uv = float3(0.5, 0.5, 0.5) + CalcUV_InTangentPlane_UV(In[0].tangent, bitangent[0], float3(0, 0, 0));
    Out[1].Tex0_uv = float3(0.5, 0.5, 0.5) + CalcUV_InTangentPlane_UV(In[0].tangent, bitangent[0], In[1].vertex - In[0].vertex);
    Out[2].Tex0_uv = float3(0.5, 0.5, 0.5) + CalcUV_InTangentPlane_UV(In[0].tangent, bitangent[0], In[2].vertex - In[0].vertex);

                
    Out[0].Tex1_uv = float3(0.5, 0.5, 0.5) + CalcUV_InTangentPlane_UV(In[1].tangent, bitangent[1], In[0].vertex - In[1].vertex);
    Out[1].Tex1_uv = float3(0.5, 0.5, 0.5) + CalcUV_InTangentPlane_UV(In[1].tangent, bitangent[1], float3(0, 0, 0));
    Out[2].Tex1_uv = float3(0.5, 0.5, 0.5) + CalcUV_InTangentPlane_UV(In[1].tangent, bitangent[1], In[2].vertex - In[1].vertex);


    Out[0].Tex2_uv = float3(0.5, 0.5, 0.5) + CalcUV_InTangentPlane_UV(In[2].tangent, bitangent[2], In[0].vertex - In[2].vertex);
    Out[1].Tex2_uv = float3(0.5, 0.5, 0.5) + CalcUV_InTangentPlane_UV(In[2].tangent, bitangent[2], In[1].vertex - In[2].vertex);
    Out[2].Tex2_uv = float3(0.5, 0.5, 0.5) + CalcUV_InTangentPlane_UV(In[2].tangent, bitangent[2], float3(0, 0, 0));

    triStream.Append(Out[0]);
    triStream.Append(Out[1]);
    triStream.Append(Out[2]);
    triStream.RestartStrip();
}

int Max3(float val[3])
{
    int m;
    if (val[0] > val[1])
        m = 0;
    else 
        m = 1;
    if (val[m] < val[2])
        m = 2;
    return m;
}

UNITY_DECLARE_TEX2DARRAY(_AlbedoTex);
UNITY_DECLARE_TEX2DARRAY(_NormalTex); 
UNITY_DECLARE_TEX2DARRAY(_DisplaTex); 

float4 frag(g2f In) : SV_Target
{
                // get all 3 Displacements
                //
    float4 dis[3];
    dis[0] = UNITY_SAMPLE_TEX2DARRAY(_DisplaTex, float3(In.Tex0_uv.xy, In.material[0]));
    dis[1] = UNITY_SAMPLE_TEX2DARRAY(_DisplaTex, float3(In.Tex1_uv.xy, In.material[1]));
    dis[2] = UNITY_SAMPLE_TEX2DARRAY(_DisplaTex, float3(In.Tex2_uv.xy, In.material[2]));
                // Add displacement to linear gradient
                //
    float hgt[3] =
    {
        hgt[0] = dis[0].x * In.color.r,
        hgt[1] = dis[1].x * In.color.g,
        hgt[2] = dis[2].x * In.color.b
    };
                // then determine which texture has the max height
                // this max Texture will be visible
                //
    int max_hgt = Max3(hgt);

    float2 uv_arr[3] =
    {
        In.Tex0_uv,
        In.Tex1_uv,
        In.Tex2_uv
    };

    float4 col;
    col = UNITY_SAMPLE_TEX2DARRAY(_AlbedoTex, float3(uv_arr[max_hgt].xy, In.material[max_hgt]));

                // now Apply Normals
                //
    float3 TS_normal = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalTex, float3(uv_arr[max_hgt].xy, In.material[max_hgt])))*.5 +.5;
    float3 bitangent = cross(In.normal, In.tangent.xyz) * In.tangent.w * unity_WorldTransformParams.w;
    float3x3 mtxTangToWorld =
    {
        In.tangent.x, bitangent.x, In.normal.x,
        In.tangent.y, bitangent.y, In.normal.y,
        In.tangent.z, bitangent.z, In.normal.z,
    };
    
    float3 N = mul(mtxTangToWorld , TS_normal);
    //N = -TS_normal;
    
    //return float4(N, 1);

    #ifdef USE_LIGHTING
        //diffuse lighing
        //float3 N = In.normal; // + nor;
        float3 L = normalize(UnityWorldSpaceLightDir(In.wPos));
        float attenuation = LIGHT_ATTENUATION(In);
        //_WorldSpaceLightPos0.xyz;
        float Lambert = saturate(dot(N, L));
        float3 diffuseLight = (Lambert * attenuation) * _LightColor0.xyz;

        //col *= float4(diffuseLight, 1);
    
        return float4 (diffuseLight * col ,1);
    #else
        #ifdef IN_BASE_PASS
            UNITY_APPLY_FOG(In.fogCoord, col);
            return col;
        #else
            return 0;
        #endif
    #endif
}