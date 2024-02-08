Shader "Unlit/3WayTangential Blend"
{
    Properties
    {
        _AlbedoTex ("Albedo", 2DArray) = "white" {}
        _NormalTex ("Normal", 2DArray) = "black" {}
        _DisplaTex ("Displacement", 2DArray) = "white" {}

        
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

        // Base Pass
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
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
                   
            #define IN_BASE_PASS
            #define USE_LIGHTING

            #include "3WayTangential.cginc"
            ENDCG
        }

        // Add Pass
        Pass
        {
            Tags { "LightMode" = "ForwardAdd" }
            Blend One One
            CGPROGRAM
            // Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma exclude_renderers gles
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile_fwdadd
            #pragma require 2darray
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"    
            
            #define USE_LIGHTING

            #include "3WayTangential.cginc"
            ENDCG
        }
    }
}
