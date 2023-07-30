// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung
Shader "AxF2Unity/AxF CPA2 HDRP"
{
    Properties
    {
        // Versioning of material to help for upgrading
        // Indicates the version of the AxF2Unity plugin used during import. This is not supposed to change!
        [HideInInspector] _AxF2UnityImportVersionMajor("_AxF2UnityImportVersionMajor", Int) = 0
        [HideInInspector] _AxF2UnityImportVersionMinor("_AxF2UnityImportVersionMinor", Int) = 0
        [HideInInspector] _AxF2UnityImportVersionPatch("_AxF2UnityImportVersionPatch", Int) = 0
        // The current verion of the material properties. This can be upgraded via script
        [HideInInspector] _AxF2UnityVersionMajor("_AxF2UnityVersionMajor", Int) = 0
        [HideInInspector] _AxF2UnityVersionMinor("_AxF2UnityVersionMinor", Int) = 0
        [HideInInspector] _AxF2UnityVersionPatch("_AxF2UnityVersionPatch", Int) = 0


        //
        // BRDF Model
        //

        [Enum(AxF2Unity.AxFCPA2BRDFMode)]_BRDFMode("BRDF Mode", Float) = 0

        _GGXSmoothness("GGX Smoothness", Range(0, 1)) = 0.5

        _CTCoeffs("CT Coeffs", Vector) = (1,0,0,1)
        _CTF0s("CT F0s", Vector) = (1,1,1,0)
        _CTSpreads("CT Spreads", Vector) = (0.5,0.5,0.5,0.0)

        // FGD_CT, FGD_GGX, FGD_Flakes, iblRoughness
        _PreIntegratedFGDScale("Preintegrated FGD Scale", Vector) = (1,1,1,1)
        [NoScaleOffset]_PreIntegratedFGD("Preintegrated FGD", 2D) = "white" {}
        // NOTE: FGD_Diffuse is either 1 or 0.91428571*F(NdotV)


        //
        // BRDF Color
        //

        [Enum(AxF2Unity.AxFCPA2BRDFColorMode)]_BRDFColorMode("BRDF Color Mode", Float) = 0

        _DiffuseColor("Diffuse Color", Color) = (1,1,1,1)
        _SpecularColor("Specular Color", Color) = (1,1,1,1)

        [NoScaleOffset]_IntegratedColorLUT1D("1D Color LUT", 2D) = "white" {}

        [ToggleUI]_UseBRDFColorLUTForPointLights("Use BRDF Color LUT for Point Lights", Float) = 0
        [NoScaleOffset]_BRDFColorLUT("BRDF Color LUT", 2D) = "white" {}

        // NOTE: There should _not_ be any scale here, since the original _BRDFColorLUT is already scaled to [0, 1]
        _BRDFColorLUTxSkyDiffuseScale("Diffuse BRDF Color LUT Scale", Float) = 1.0
        _BRDFColorLUTxSkySpecularScale("Specular BRDF Color LUT Scale", Float) = 1.0

        [NoScaleOffset]_BRDFColorLUTxSkyDiffuse("Diffuse PreIntegrated BRDF Color LUT", CubeArray) = "white" {}
        [NoScaleOffset]_BRDFColorLUTxSkySpecular("Specular PreIntegrated BRDF Color LUT", CubeArray) = "white" {}
        _BRDFColorLUTxSky_ArraySize("PreIntegrated BRDF Color LUT", Int) = 1

        [NoScaleOffset]_BRDFColorLUTxSkyDiffuseSH("SH Diffuse PreIntegrated BRDF Color LUT", 2D) = "white" {}
        [NoScaleOffset]_BRDFColorLUTxSkySpecularSH("SH Specular PreIntegrated BRDF Color LUT", 2D) = "white" {}

        _BRDFColorLUTxSkyDiffuseMeanColor("SVD Diffuse Mean", Color) = (0, 0, 0, 0)
        [NoScaleOffset]_BRDFColorLUTxSkyDiffuseCubemap("SVD Diffuse PreIntegrated BRDF Color LUT Cubemap", Cube) = "white" {}
        [NoScaleOffset]_BRDFColorLUTxSkyDiffuseArray("SVD Diffuse PreIntegrated BRDF Color LUT Array", 2D) = "white" {}
        _BRDFColorLUTxSkySpecularMeanColor("SVD Diffuse Mean", Color) = (0, 0, 0, 0)
        [NoScaleOffset]_BRDFColorLUTxSkySpecularCubemap("SVD Specular PreIntegrated BRDF Color LUT Cubemap", Cube) = "white" {}
        [NoScaleOffset]_BRDFColorLUTxSkySpecularArray("SVD Specular PreIntegrated BRDF Color LUT Array", 2D) = "white" {}


        //
        // Flakes
        //

        [ToggleUI]_EnableFlakes("Enable Flakes", Float) = 0
        [Enum(AxF2Unity.AxFCPA2FlakesMode)]_FlakesMode("Flakes Mode", Float) = 0

        _SimpleFlakesColorMap("Simple Flakes Map", 2D) = "black" {}
        _SimpleFlakesColorOffset("Simple Flakes Offset", Float) = 0
        _SimpleFlakesColorScale("Simple Flakes Scale", Float) = 1

        _SimpleFlakesColorMap3D("Simple 3D Flakes Map", 2DArray) = "black" {}
        _SimpleFlakesColorOffset3D("Simple 3D Flakes Offset", Float) = 0
        _SimpleFlakesColorScale3D("Simple 3D Flakes Scale", Float) = 1

        _FullFlakesColorMap("Full Flakes Map", 2DArray) = "black" {}
        _FullFlakesColorScaleOffsetMap("Full Flakes Scale + Offset", 2D) = "red" {}
        _FullFlakesSliceLUT("Full Flakes Slice LUT", 2D) = "black" {}
        _FullFlakesInvStdDevMap("Full Flakes Inv Std Dev Map", 2D) = "white" {}
        _FullFlakesMaxThetaI("Full Flakes Max Theta I", Float) = 0
        _FullFlakesMaxThetaF("Full Flakes Max Theta F", Float) = 0
        _FullFlakesNumThetaI("Full Flakes Num Theta I", Float) = 0
        _FullFlakesNumThetaF("Full Flakes Num Theta F", Float) = 0

        [ToggleUI]_UseFlakesSmoothness("Use Flakes Smoothness", Float) = 0
        _FlakesSmoothness("Flakes Smoothness", Range(0, 1)) = 0.5


        //
        // Clear Coat
        //

        [ToggleUI]_EnableClearCoat("Enable Clear Coat", Float) = 0
        _ClearCoatIOR("Clear Coat IOR", Float) = 1.5
        _ClearCoatF0("Clear Coat F0", Float) = 0.04
        _ClearCoatF90("Clear Coat F90", Float) = 1

        //
        // Other
        //

        [Enum(AxF2Unity.UVTilingMode)]_UVTilingMode("UV Tiling Mode", Float) = 0
        _UVTilingGridSize("UV Tiling Grid Size", Float) = 1

        [Enum(UnityEngine.Rendering.CullMode)]_CullMode("Cull Mode", Float) = 2

        _SortPriority("Sort Priority", Float) = 0


        // Stencil state
        // Forward
        [HideInInspector] _StencilRef("_StencilRef", Int) = 0   // StencilUsage.Clear
        [HideInInspector] _StencilWriteMask("_StencilWriteMask", Int) = 3 // StencilUsage.RequiresDeferredLighting | StencilUsage.SubsurfaceScattering
        // Depth prepass
        [HideInInspector] _StencilRefDepth("_StencilRefDepth", Int) = 16 // DecalsForwardOutputNormalBuffer
        [HideInInspector] _StencilWriteMaskDepth("_StencilWriteMaskDepth", Int) = 48 // DoesntReceiveSSR | DecalsForwardOutputNormalBuffer
        // Motion vector pass
        [HideInInspector] _StencilRefMV("_StencilRefMV", Int) = 32 // StencilUsage.ObjectMotionVector
        [HideInInspector] _StencilWriteMaskMV("_StencilWriteMaskMV", Int) = 32 // StencilUsage.ObjectMotionVector

        // Blending state
        [HideInInspector] _BlendMode("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0


        [ToggleUI] _SupportDecals("Support Decals", Float) = 1.0
        [ToggleUI] _ReceivesSSR("Receives SSR", Float) = 1.0

        [ToggleUI] _AddPrecomputedVelocity("AddPrecomputedVelocity", Float) = 0.0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------

    #pragma shader_feature_local CLEAR_COAT

    #pragma shader_feature_local USE_GGXFIT_BRDF_MODEL USE_FULL_BRDF_MODEL
    #pragma shader_feature_local USE_INTEGRATED_COLOR USE_INTEGRATED_COLOR_LUT_1D USE_INTEGRATED_COLOR_X_SKY USE_INTEGRATED_COLOR_X_SKY_SH USE_INTEGRATED_COLOR_X_SKY_SVD
    #pragma shader_feature_local USE_FULL_COLOR_LUT_POINT_LIGHTS
    #pragma shader_feature_local _ USE_SIMPLE_FLAKES USE_SIMPLE_FLAKES_3D USE_FULL_FLAKES
    #pragma shader_feature_local _ USE_RANDOMIZED_UV_TILING USE_UV_MIRRORING


    #pragma shader_feature_local _DOUBLESIDED_ON

    #pragma shader_feature_local _DISABLE_DECALS
    #pragma shader_feature_local _DISABLE_SSR

    #pragma shader_feature_local _ADD_PRECOMPUTED_VELOCITY

    // enable dithering LOD crossfade
    #pragma multi_compile _ LOD_FADE_CROSSFADE

    //enable GPU instancing support
    #pragma multi_compile_instancing
    #pragma instancing_options renderinglayer

    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    #include "AxFCPA2HDRPProperties.hlsl"

    ENDHLSL

    SubShader
    {
        // This tags allow to use the shader replacement features
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "HDLitShader" }

        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }

            Cull Off

            HLSLPROGRAM

            // Note: Require _ObjectId and _PassValue variables

            // We reuse depth prepass for the scene selection, allow to handle alpha correctly as well as tessellation and vertex animation
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define SCENESELECTIONPASS // This will drive the output of the scene selection shader
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "AxFCPA2HDRP.hlsl"
            #include "ShaderPass/AxFHDRPDepthPass.hlsl"
            #include "AxFCPA2HDRPData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma editor_sync_compilation

            ENDHLSL
        }

        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags{ "LightMode" = "META" }


            HLSLPROGRAM

            // Lightmap memo
            // DYNAMICLIGHTMAP_ON is used when we have an "enlighten lightmap" ie a lightmap updated at runtime by enlighten.This lightmap contain indirect lighting from realtime lights and realtime emissive material.Offline baked lighting(from baked material / light,
            // both direct and indirect lighting) will hand up in the "regular" lightmap->LIGHTMAP_ON.

            #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "AxFCPA2HDRP.hlsl"
            #include "ShaderPass/AxFHDRPSharePass.hlsl"
            #include "AxFCPA2HDRPData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode" = "ShadowCaster" }

            Cull [_CullMode]

            ZClip [_ZClip]
            ZWrite On
            ZTest LEqual

            ColorMask 0

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_SHADOWS
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "AxFCPA2HDRP.hlsl"
            #include "ShaderPass/AxFHDRPDepthPass.hlsl"
            #include "AxFCPA2HDRPData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }

        Pass
        {
            Name "DepthForwardOnly"
            Tags{ "LightMode" = "DepthForwardOnly" }

            Cull [_CullMode]

            ZWrite On

            Stencil
            {
                WriteMask[_StencilWriteMaskDepth]
                Ref[_StencilRefDepth]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM

            #define WRITE_NORMAL_BUFFER
            #pragma multi_compile _ WRITE_MSAA_DEPTH

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "AxFCPA2HDRP.hlsl"
            #include "ShaderPass/AxFHDRPDepthPass.hlsl"
            #include "AxFCPA2HDRPData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }

        Pass
        {
            Name "MotionVectors"
            Tags{ "LightMode" = "MotionVectors" } // Caution, this need to be call like this to setup the correct parameters by C++ (legacy Unity)

            // If velocity pass (motion vectors) is enabled we tag the stencil so it don't perform CameraMotionVelocity
            Stencil
            {
                WriteMask [_StencilWriteMaskMV]
                Ref [_StencilRefMV]
                Comp Always
                Pass Replace
            }

            Cull [_CullMode]
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM

            #define WRITE_NORMAL_BUFFER
            #pragma multi_compile _ WRITE_MSAA_DEPTH

            #define SHADERPASS SHADERPASS_MOTION_VECTORS
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "AxFCPA2HDRP.hlsl"
            #include "ShaderPass/AxFHDRPSharePass.hlsl"
            #include "AxFCPA2HDRPData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            Stencil
            {
                WriteMask [_StencilWriteMask]
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            Blend [_SrcBlend] [_DstBlend]
            ZTest LEqual
            ZWrite [_ZWrite]
            Cull [_CullMode]
            ColorMask [_ColorMaskTransparentVel] 1

            HLSLPROGRAM

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // Setup DECALS_OFF so the shader stripper can remove variants
            #pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT

            // Supported shadow modes per light type
            #pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH
            #pragma multi_compile_fragment AREA_SHADOW_MEDIUM AREA_SHADOW_HIGH

            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

            #define SHADERPASS SHADERPASS_FORWARD
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

        #ifdef DEBUG_DISPLAY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
        #endif

            // The light loop (or lighting architecture) is in charge to:
            // - Define light list
            // - Define the light loop
            // - Setup the constant/data
            // - Do the reflection hierarchy
            // - Provide sampling function for shadowmap, ies, cookie and reflection (depends on the specific use with the light loops like index array or atlas or single and texture format (cubemap/latlong))

            #define HAS_LIGHTLOOP

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
            #include "AxFCPA2HDRP.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"

            #include "ShaderPass/AxFHDRPSharePass.hlsl"
            #include "AxFCPA2HDRPData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }

    }

    CustomEditor "AxF2Unity.AxFCPA2ShaderGUI"
}
