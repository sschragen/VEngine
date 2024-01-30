Shader "NaiveSurfaceNets/3Way Texture Blend" {
        Properties {
            _Color ("Color", Color) = (1,1,1,1)
            _Blend ("Texture Blend", Range(0,1)) = 0.0
            _MainTex0 ("Albedo (RGB)", 2D) = "white" {}
            _MainTex1 ("Albedo (RGB)", 2D) = "white" {}
            _MainTex2 ("Albedo (RGB)", 2D) = "white" {}
            _Glossiness ("Smoothness", Range(0,1)) = 0.5
            _Metallic ("Metallic", Range(0,1)) = 0.0
        }
        SubShader {
            Tags { "RenderType"="Opaque" }
            LOD 200
           
            CGPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
            #pragma surface surf Standard fullforwardshadows vertex:vert
     
            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0


            void vert (inout appdata_full v) 
            {
                //v.vertex.xyz += v.normal * _Amount;
            }
     
            sampler2D _MainTex0;
            sampler2D _MainTex1;
            sampler2D _MainTex2;
     
            struct Input {
                float2 uv_MainTex0;
                float2 uv_MainTex1;      
                float2 uv_MainTex2; 
            };
     
            half _Blend;
            half _Glossiness;
            half _Metallic;
            fixed4 _Color;
     
            void surf (Input IN, inout SurfaceOutputStandard o) {
                // Albedo comes from a texture tinted by color
                //fixed4 c = lerp (tex2D (_MainTex, IN.uv_MainTex), tex2D (_MainTex2, IN.uv_MainTex2), _Blend) * _Color;

                fixed4 c = lerp (tex2D (_MainTex0, IN.uv_MainTex0), tex2D (_MainTex1, IN.uv_MainTex1), _Blend) * _Color ; 

                //fixed4 c = lerp (length(uv_MainTex0));

                o.Albedo = c.rgb;
                // Metallic and smoothness come from slider variables
                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
                o.Alpha = c.a;
            }
            ENDCG
        }
        FallBack "Diffuse"
    }