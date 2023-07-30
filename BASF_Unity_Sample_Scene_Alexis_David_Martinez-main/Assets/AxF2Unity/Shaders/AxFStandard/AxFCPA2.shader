// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

Shader "AxF2Unity/AxF CPA2"
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


        // Blending state
        [HideInInspector] _BlendMode ("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 300

        Cull [_CullMode]


        // ------------------------------------------------------------------
        //  Base forward pass (directional light, emission, lightmaps, ...)
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            CGPROGRAM
            #pragma target 3.0

            // -------------------------------------

            #pragma shader_feature CLEAR_COAT

            #pragma shader_feature USE_GGXFIT_BRDF_MODEL USE_FULL_BRDF_MODEL
            #pragma shader_feature USE_INTEGRATED_COLOR USE_INTEGRATED_COLOR_LUT_1D USE_INTEGRATED_COLOR_X_SKY USE_INTEGRATED_COLOR_X_SKY_SH USE_INTEGRATED_COLOR_X_SKY_SVD

            #pragma shader_feature USE_FULL_COLOR_LUT_POINT_LIGHTS
            #pragma shader_feature _ USE_SIMPLE_FLAKES USE_SIMPLE_FLAKES_3D USE_FULL_FLAKES
            #pragma shader_feature USE_FLAKES_SMOOTHNESS
            #pragma shader_feature _ USE_RANDOMIZED_UV_TILING USE_UV_MIRRORING

            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertForwardBase
            #pragma fragment fragForwardBase
            #include "AxFCPA2Core.cginc"

            ENDCG
        }
        // ------------------------------------------------------------------
        //  Additive forward pass (one light per pass)
        Pass
        {
            Name "FORWARD_DELTA"
            Tags { "LightMode" = "ForwardAdd" }
            Blend [_SrcBlend] One
            Fog { Color (0,0,0,0) } // in additive pass fog should be black
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma target 3.0

            // -------------------------------------

            #pragma shader_feature CLEAR_COAT

            #pragma shader_feature USE_GGXFIT_BRDF_MODEL USE_FULL_BRDF_MODEL
            #pragma shader_feature USE_INTEGRATED_COLOR USE_INTEGRATED_COLOR_LUT_1D USE_INTEGRATED_COLOR_X_SKY USE_INTEGRATED_COLOR_X_SKY_SH USE_INTEGRATED_COLOR_X_SKY_SVD
            #pragma shader_feature USE_FULL_COLOR_LUT_POINT_LIGHTS
            #pragma shader_feature _ USE_SIMPLE_FLAKES USE_SIMPLE_FLAKES_3D USE_FULL_FLAKES
            #pragma shader_feature USE_FLAKES_SMOOTHNESS
            #pragma shader_feature _ USE_RANDOMIZED_UV_TILING USE_UV_MIRRORING

            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertForwardAdd
            #pragma fragment fragForwardAdd
            #include "AxFCPA2Core.cginc"

            ENDCG
        }
        // ------------------------------------------------------------------
        //  Shadow rendering pass
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On ZTest LEqual

            CGPROGRAM
            #pragma target 3.0

            // -------------------------------------

            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster

            #include "UnityStandardShadow.cginc"

            ENDCG
        }

        // ------------------------------------------------------------------
        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags { "LightMode"="Meta" }

            CGPROGRAM

            // -------------------------------------

            #pragma shader_feature CLEAR_COAT

            #pragma vertex vert_meta
            #pragma fragment frag_meta

            #pragma shader_feature EDITOR_VISUALIZATION

            #include "AxFCPA2Meta.cginc"
            ENDCG
        }
    }

    FallBack "VertexLit"
    CustomEditor "AxF2Unity.AxFCPA2ShaderGUI"
}
