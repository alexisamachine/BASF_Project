// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using System;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace AxF2Unity
{
    /// <summary>
    /// Inspector GUI for AxF2Unity/AxFCPA2 shader used with Unity's built-in render pipeline
    /// </summary>
    public class AxFCPA2ShaderGUI : ShaderGUI
    {
        private static class Styles
        {
            public static string surfaceOptionsText = "Surface Options";

            public static GUIContent sortPriorityText = new GUIContent("Sort Priority");

            public static GUIContent cullModeText = new GUIContent("Cull Mode");

            public static GUIContent supportDecalsText = new GUIContent("Support Decals");
            public static GUIContent receivesSSRText = new GUIContent("Receive SSR");
            public static GUIContent addPrecomputedVelocityText = new GUIContent("Add Precomputed Velocity");

            public static string surfaceInputText = "Surface Input";

            // BRDF

            public static GUIContent surfaceBRDFText = new GUIContent("BRDF");

            public static GUIContent brdfModeText = new GUIContent("BRDF Mode");

            // GGX Fit
            public static GUIContent ggxSmoothnessText = new GUIContent("GGX Smoothness");

            // Original BRDF
            public static GUIContent ctCoeffsText = new GUIContent("CT Coeffs");
            public static GUIContent ctF0sText = new GUIContent("CT F0s");
            public static GUIContent ctSpreadsText = new GUIContent("CT Spreads");

            // 1D LUT only! cosTheta -> specular FGD, iblRoughness
            public static GUIContent preIntegratedFGDScaleText = new GUIContent("Pre Integrated FGD Scale");
            public static GUIContent preIntegratedFGDText = new GUIContent("Pre Integrated FGD");
            public static GUIContent computePreIntegratedFGDText = new GUIContent("Compute Pre Integrated FGD");

            // BRDF Color
            public static GUIContent brdfColorModeText = new GUIContent("BRDF Color Mode");

            public static GUIContent diffuseColorText = new GUIContent("Diffuse Color");
            public static GUIContent specularColorText = new GUIContent("Specular Color");

            public static GUIContent integratedColorLUT1DText = new GUIContent("Integrated 1D Color LUT");

            public static GUIContent useBrdfColorLUTForPointLightsText = new GUIContent("Use BRDF Color LUT for Point Lights");

            public static GUIContent brdfColorLUTText = new GUIContent("BRDF Color LUT");

            public static GUIContent brdfColorLUTxSkyDiffuseText = new GUIContent("Integraed Diffuse Color");
            public static GUIContent brdfColorLUTxSkySpecularText = new GUIContent("Integrated Specular Color");
            public static GUIContent brdfColorLUTxSkyArraySizeText = new GUIContent("Cubemap Array Size");

            public static GUIContent brdfColorLUTxSkyDiffuseSHText = new GUIContent("Integrated Diffuse SH Coeffs");
            public static GUIContent brdfColorLUTxSkySpecularSHText = new GUIContent("Integrated Specular SH Coeffs");

            public static GUIContent brdfColorLUTxSkyDiffuseMeanColorText = new GUIContent("Diffuse Mean Color");
            public static GUIContent brdfColorLUTxSkySpecularMeanColorText = new GUIContent("Specular Mean Color");

            public static GUIContent brdfColorLUTxSkyDiffuseSVDCubemapText = new GUIContent("Integrated Diffuse Cubemap");
            public static GUIContent brdfColorLUTxSkySpecularSVDCubemapText = new GUIContent("Integrated Specular Cubemap");
            public static GUIContent brdfColorLUTxSkyDiffuseSVDArrayText = new GUIContent("Integrated Diffuse Array");
            public static GUIContent brdfColorLUTxSkySpecularSVDArrayText = new GUIContent("Integrated Specular Array");


            // Flakes

            public static GUIContent flakesText = new GUIContent("Flakes");
            public static GUIContent flakesModeText = new GUIContent("Flakes Mode");

            public static GUIContent simpleFlakesColorMapText = new GUIContent("Simple Flakes Map");
            public static GUIContent simpleFlakesColorOffsetText = new GUIContent("Simple Flakes Offset");
            public static GUIContent simpleFlakesColorScaleText = new GUIContent("Simple Flakes Scale");

            public static GUIContent simpleFlakesColorMap3DText = new GUIContent("Simple 3D Flakes Map");
            public static GUIContent simpleFlakesColorOffset3DText = new GUIContent("Simple 3D Flakes Offset");
            public static GUIContent simpleFlakesColorScale3DText = new GUIContent("Simple 3D Flakes Scale");

            public static GUIContent fullFlakesMapText = new GUIContent("Full Flakes Map");
            public static GUIContent fullFlakesColorScaleOffsetMapText = new GUIContent("Full Flakes Scale + Offset");
            public static GUIContent fullFlakesSliceLUTText = new GUIContent("Full Flakes Slice LUT");
            public static GUIContent fullFlakesInvStdDevMapText = new GUIContent("Full Flakes Inv Std Dev");
            public static GUIContent fullFlakesMaxThetaIText = new GUIContent("Full Flakes Max Theta I");
            public static GUIContent fullFlakesMaxThetaFText = new GUIContent("Full Flakes Max Theta F");
            public static GUIContent fullFlakesNumThetaIText = new GUIContent("Full Flakes Num Theta I");
            public static GUIContent fullFlakesNumThetaFText = new GUIContent("Full Flakes Num Theta F");

            public static GUIContent useFlakesSmoothnessText = new GUIContent("Use Flakes Smoothness");
            public static GUIContent flakesSmoothnessText = new GUIContent("Flakes Smoothness");

            // Clear Coat

            public static GUIContent clearCoatText = new GUIContent("Clear Coat");
            public static GUIContent clearCoatIORText = new GUIContent("Clear Coat IOR");
            public static GUIContent clearCoatF0Text = new GUIContent("Clear Coat F0");
            public static GUIContent clearCoatF90Text = new GUIContent("Clear Coat F90");


            public static string tilingText = "Tiling Options";

            public static GUIContent uvTilingModeText = new GUIContent("UV Tiling Mode");
            public static GUIContent uvTilingGridSizeText = new GUIContent("UV Tiling Grid Size");


            public static string advancedText = "Advanced Options";
        }

        // BRDF

        static string kBRDFMode = "_BRDFMode";
        MaterialProperty m_BRDFMode = null;

        static string kGGXSmoothness = "_GGXSmoothness";
        MaterialProperty m_GGXSmoothness = null;

        static string kCTCoeffs = "_CTCoeffs";
        MaterialProperty m_CTCoeffs = null;
        static string kCTF0s = "_CTF0s";
        MaterialProperty m_CTF0s = null;
        static string kCTSpreads = "_CTSpreads";
        MaterialProperty m_CTSpreads = null;

        static string kPreIntegratedFGDScale = "_PreIntegratedFGDScale";
        MaterialProperty m_PreIntegratedFGDScale = null;
        static string kPreIntegratedFGD = "_PreIntegratedFGD";
        MaterialProperty m_PreIntegratedFGD = null;

        static string kBRDFColorMode = "_BRDFColorMode";
        MaterialProperty m_BRDFColorMode = null;

        static string kDiffuseColor = "_DiffuseColor";
        MaterialProperty m_DiffuseColor = null;
        static string kSpecularColor = "_SpecularColor";
        MaterialProperty m_SpecularColor = null;

        static string kIntegratedColorLUT1D = "_IntegratedColorLUT1D";
        MaterialProperty m_IntegratedColorLUT1D = null;

        static string kUseBRDFColorLUTForPointLights = "_UseBRDFColorLUTForPointLights";
        MaterialProperty m_UseBRDFColorLUTForPointLights = null;

        static string kBRDFColorLUT = "_BRDFColorLUT";
        MaterialProperty m_BRDFColorLUT = null;

        static string kBRDFColorLUTxSkyDiffuseScale = "_BRDFColorLUTxSkyDiffuseScale";
        MaterialProperty m_BRDFColorLUTxSkyDiffuseScale = null;
        static string kBRDFColorLUTxSkySpecularScale = "_BRDFColorLUTxSkySpecularScale";
        MaterialProperty m_BRDFColorLUTxSkySpecularScale = null;

        static string kBRDFColorLUTxSkyDiffuse = "_BRDFColorLUTxSkyDiffuse";
        MaterialProperty m_BRDFColorLUTxSkyDiffuse = null;
        static string kBRDFColorLUTxSkySpecular = "_BRDFColorLUTxSkySpecular";
        MaterialProperty m_BRDFColorLUTxSkySpecular = null;
        static string kBRDFColorLUTxSky_ArraySize = "_BRDFColorLUTxSky_ArraySize";
        MaterialProperty m_BRDFColorLUTxSky_ArraySize = null;

        static string kBRDFColorLUTxSkyDiffuseSH = "_BRDFColorLUTxSkyDiffuseSH";
        MaterialProperty m_BRDFColorLUTxSkyDiffuseSH = null;
        static string kBRDFColorLUTxSkySpecularSH = "_BRDFColorLUTxSkySpecularSH";
        MaterialProperty m_BRDFColorLUTxSkySpecularSH = null;

        static string kBRDFColorLUTxSkyDiffuseMeanColor = "_BRDFColorLUTxSkyDiffuseMeanColor";
        MaterialProperty m_BRDFColorLUTxSkyDiffuseMeanColor = null;
        static string kBRDFColorLUTxSkyDiffuseCubemap = "_BRDFColorLUTxSkyDiffuseCubemap";
        MaterialProperty m_BRDFColorLUTxSkyDiffuseCubemap = null;
        static string kBRDFColorLUTxSkyDiffuseArray = "_BRDFColorLUTxSkyDiffuseArray";
        MaterialProperty m_BRDFColorLUTxSkyDiffuseArray = null;
        static string kBRDFColorLUTxSkySpecularMeanColor = "_BRDFColorLUTxSkySpecularMeanColor";
        MaterialProperty m_BRDFColorLUTxSkySpecularMeanColor = null;
        static string kBRDFColorLUTxSkySpecularCubemap = "_BRDFColorLUTxSkySpecularCubemap";
        MaterialProperty m_BRDFColorLUTxSkySpecularCubemap = null;
        static string kBRDFColorLUTxSkySpecularArray = "_BRDFColorLUTxSkySpecularArray";
        MaterialProperty m_BRDFColorLUTxSkySpecularArray = null;


        // Flakes

        static string kEnableFlakes = "_EnableFlakes";
        MaterialProperty m_EnableFlakes = null;
        static string kFlakesMode = "_FlakesMode";
        MaterialProperty m_FlakesMode = null;

        static string kSimpleFlakesColorMap = "_SimpleFlakesColorMap";
        MaterialProperty m_SimpleFlakesColorMap = null;
        static string kSimpleFlakesColorOffset = "_SimpleFlakesColorOffset";
        MaterialProperty m_SimpleFlakesColorOffset = null;
        static string kSimpleFlakesColorScale = "_SimpleFlakesColorScale";
        MaterialProperty m_SimpleFlakesColorScale = null;

        static string kSimpleFlakesColorMap3D = "_SimpleFlakesColorMap3D";
        MaterialProperty m_SimpleFlakesColorMap3D = null;
        static string kSimpleFlakesColorOffset3D = "_SimpleFlakesColorOffset3D";
        MaterialProperty m_SimpleFlakesColorOffset3D = null;
        static string kSimpleFlakesColorScale3D = "_SimpleFlakesColorScale3D";
        MaterialProperty m_SimpleFlakesColorScale3D = null;

        static string kFullFlakesColorMap = "_FullFlakesColorMap";
        MaterialProperty m_FullFlakesColorMap = null;
        static string kFullFlakesColorScaleOffsetMap = "_FullFlakesColorScaleOffsetMap";
        MaterialProperty m_FullFlakesColorScaleOffsetMap = null;
        static string kFullFlakesSliceLUT = "_FullFlakesSliceLUT";
        MaterialProperty m_FullFlakesSliceLUT = null;
        static string kFullFlakesInvStdDevMap = "_FullFlakesInvStdDevMap";
        MaterialProperty m_FullFlakesInvStdDevMap = null;
        static string kFullFlakesMaxThetaI = "_FullFlakesMaxThetaI";
        MaterialProperty m_FullFlakesMaxThetaI = null;
        static string kFullFlakesMaxThetaF = "_FullFlakesMaxThetaF";
        MaterialProperty m_FullFlakesMaxThetaF = null;
        static string kFullFlakesNumThetaI = "_FullFlakesNumThetaI";
        MaterialProperty m_FullFlakesNumThetaI = null;
        static string kFullFlakesNumThetaF = "_FullFlakesNumThetaF";
        MaterialProperty m_FullFlakesNumThetaF = null;

        static string kUseFlakesSmoothness = "_UseFlakesSmoothness";
        MaterialProperty m_UseFlakesSmoothness = null;
        static string kFlakesSmoothness = "_FlakesSmoothness";
        MaterialProperty m_FlakesSmoothness = null;

        // Clear Coat

        static string kEnableClearCoat = "_EnableClearCoat";
        MaterialProperty m_EnableClearCoat = null;
        static string kClearCoatIOR = "_ClearCoatIOR";
        MaterialProperty m_ClearCoatIOR = null;
        static string kClearCoatF0 = "_ClearCoatF0";
        MaterialProperty m_ClearCoatF0 = null;
        static string kClearCoatF90 = "_ClearCoatF90";
        MaterialProperty m_ClearCoatF90 = null;

        // Tiling

        static string kUVTilingMode = "_UVTilingMode";
        MaterialProperty m_UVTilingMode = null;
        static string kUVTilingGridSize = "_UVTilingGridSize";
        MaterialProperty m_UVTilingGridSize = null;

        // Other options

        const string kSortPriority = "_SortPriority";
        MaterialProperty m_SortPriority;

        static string kCullMode = "_CullMode";
        MaterialProperty m_CullMode = null;


        static string kStencilRef = "_StencilRef";
        MaterialProperty m_StencilRef = null;
        static string kStencilWriteMask = "_StencilWriteMask";
        MaterialProperty m_StencilWriteMask = null;
        static string kStencilRefDepth = "_StencilRefDepth";
        MaterialProperty m_StencilRefDepth = null;
        static string kStencilWriteMaskDepth = "_StencilWriteMaskDepth";
        MaterialProperty m_StencilWriteMaskDepth = null;
        static string kStencilRefMV = "_StencilRefMV";
        MaterialProperty m_StencilRefMV = null;
        static string kStencilWriteMaskMV = "_StencilWriteMaskMV";
        MaterialProperty m_StencilWriteMaskMV = null;

        static string kBlendMode = "_BlendMode";
        MaterialProperty m_BlendMode = null;
        static string kSrcBlend = "_SrcBlend";
        MaterialProperty m_SrcBlend = null;
        static string kDstBlend = "_DstBlend";
        MaterialProperty m_DstBlend = null;
        static string kZWrite = "_ZWrite";
        MaterialProperty m_ZWrite = null;


        static string kSupportDecals = "_SupportDecals";
        MaterialProperty m_SupportDecals = null;
        static string kReceivesSSR = "_ReceivesSSR";
        MaterialProperty m_ReceivesSSR = null;
        static string kAddPrecomputedVelocity = "_AddPrecomputedVelocity";
        MaterialProperty m_AddPrecomputedVelocity = null;


        MaterialEditor m_MaterialEditor;

        public void FindProperties(MaterialProperty[] props)
        {
            // BRDF

            m_BRDFMode = FindProperty(kBRDFMode, props);

            m_GGXSmoothness = FindProperty(kGGXSmoothness, props);

            m_CTCoeffs = FindProperty(kCTCoeffs, props);
            m_CTF0s = FindProperty(kCTF0s, props);
            m_CTSpreads = FindProperty(kCTSpreads, props);

            m_PreIntegratedFGDScale = FindProperty(kPreIntegratedFGDScale, props);
            m_PreIntegratedFGD = FindProperty(kPreIntegratedFGD, props);

            m_BRDFColorMode = FindProperty(kBRDFColorMode, props);

            m_DiffuseColor = FindProperty(kDiffuseColor, props);
            m_SpecularColor = FindProperty(kSpecularColor, props);

            m_IntegratedColorLUT1D = FindProperty(kIntegratedColorLUT1D, props);

            m_UseBRDFColorLUTForPointLights = FindProperty(kUseBRDFColorLUTForPointLights, props);
            m_BRDFColorLUT = FindProperty(kBRDFColorLUT, props);


            m_BRDFColorLUTxSkyDiffuseScale = FindProperty(kBRDFColorLUTxSkyDiffuseScale, props);
            m_BRDFColorLUTxSkySpecularScale = FindProperty(kBRDFColorLUTxSkySpecularScale, props);

            m_BRDFColorLUTxSkyDiffuse = FindProperty(kBRDFColorLUTxSkyDiffuse, props);
            m_BRDFColorLUTxSkySpecular = FindProperty(kBRDFColorLUTxSkySpecular, props);
            m_BRDFColorLUTxSky_ArraySize = FindProperty(kBRDFColorLUTxSky_ArraySize, props);

            m_BRDFColorLUTxSkyDiffuseSH = FindProperty(kBRDFColorLUTxSkyDiffuseSH, props);
            m_BRDFColorLUTxSkySpecularSH = FindProperty(kBRDFColorLUTxSkySpecularSH, props);

            m_BRDFColorLUTxSkyDiffuseMeanColor = FindProperty(kBRDFColorLUTxSkyDiffuseMeanColor, props);
            m_BRDFColorLUTxSkyDiffuseCubemap = FindProperty(kBRDFColorLUTxSkyDiffuseCubemap, props);
            m_BRDFColorLUTxSkyDiffuseArray = FindProperty(kBRDFColorLUTxSkyDiffuseArray, props);
            m_BRDFColorLUTxSkySpecularMeanColor = FindProperty(kBRDFColorLUTxSkySpecularMeanColor, props);
            m_BRDFColorLUTxSkySpecularCubemap = FindProperty(kBRDFColorLUTxSkySpecularCubemap, props);
            m_BRDFColorLUTxSkySpecularArray = FindProperty(kBRDFColorLUTxSkySpecularArray, props);


            // Flakes

            m_EnableFlakes = FindProperty(kEnableFlakes, props);
            m_FlakesMode = FindProperty(kFlakesMode, props);

            m_SimpleFlakesColorMap = FindProperty(kSimpleFlakesColorMap, props);
            m_SimpleFlakesColorOffset = FindProperty(kSimpleFlakesColorOffset, props);
            m_SimpleFlakesColorScale = FindProperty(kSimpleFlakesColorScale, props);

            m_SimpleFlakesColorMap3D = FindProperty(kSimpleFlakesColorMap3D, props);
            m_SimpleFlakesColorOffset3D = FindProperty(kSimpleFlakesColorOffset3D, props);
            m_SimpleFlakesColorScale3D = FindProperty(kSimpleFlakesColorScale3D, props);

            m_FullFlakesColorMap = FindProperty(kFullFlakesColorMap, props);
            m_FullFlakesColorScaleOffsetMap = FindProperty(kFullFlakesColorScaleOffsetMap, props);
            m_FullFlakesSliceLUT = FindProperty(kFullFlakesSliceLUT, props);
            m_FullFlakesInvStdDevMap = FindProperty(kFullFlakesInvStdDevMap, props);
            m_FullFlakesMaxThetaI = FindProperty(kFullFlakesMaxThetaI, props);
            m_FullFlakesMaxThetaF = FindProperty(kFullFlakesMaxThetaF, props);
            m_FullFlakesNumThetaI = FindProperty(kFullFlakesNumThetaI, props);
            m_FullFlakesNumThetaF = FindProperty(kFullFlakesNumThetaF, props);

            m_UseFlakesSmoothness = FindProperty(kUseFlakesSmoothness, props);
            m_FlakesSmoothness = FindProperty(kFlakesSmoothness, props);

            // Clear Coat

            m_EnableClearCoat = FindProperty(kEnableClearCoat, props);
            m_ClearCoatIOR = FindProperty(kClearCoatIOR, props);
            m_ClearCoatF0 = FindProperty(kClearCoatF0, props);
            m_ClearCoatF90 = FindProperty(kClearCoatF90, props);

            // Tiling

            m_UVTilingMode = FindProperty(kUVTilingMode, props);
            m_UVTilingGridSize = FindProperty(kUVTilingGridSize, props);

            // Other Properties

            m_SortPriority = FindProperty(kSortPriority, props);

            m_CullMode = FindProperty(kCullMode, props);

            m_SupportDecals = FindProperty(kSupportDecals, props, false);
            m_ReceivesSSR = FindProperty(kReceivesSSR, props, false);
            m_AddPrecomputedVelocity = FindProperty(kAddPrecomputedVelocity, props, false);
        }

        private void SurfaceOptionsGUI()
        {
            GUILayout.Label(Styles.surfaceOptionsText, EditorStyles.boldLabel);

            m_MaterialEditor.ShaderProperty(m_SortPriority, Styles.sortPriorityText);

            if (m_SupportDecals != null)
                m_MaterialEditor.ShaderProperty(m_SupportDecals, Styles.supportDecalsText);

            if (m_ReceivesSSR != null)
                m_MaterialEditor.ShaderProperty(m_ReceivesSSR, Styles.receivesSSRText);

            if (m_AddPrecomputedVelocity != null)
                m_MaterialEditor.ShaderProperty(m_AddPrecomputedVelocity, Styles.addPrecomputedVelocityText);

            m_MaterialEditor.ShaderProperty(m_CullMode, Styles.cullModeText);
        }

        private void SurfaceBRDFGUI()
        {
            GUILayout.Label(Styles.surfaceBRDFText, EditorStyles.boldLabel);

            m_MaterialEditor.ShaderProperty(m_BRDFMode, Styles.brdfModeText);

            // GGX Fit

            m_MaterialEditor.ShaderProperty(m_GGXSmoothness, Styles.ggxSmoothnessText);

            // CT Lobes

            m_MaterialEditor.ShaderProperty(m_CTCoeffs, Styles.ctCoeffsText);
            m_MaterialEditor.ShaderProperty(m_CTF0s, Styles.ctF0sText);
            m_MaterialEditor.ShaderProperty(m_CTSpreads, Styles.ctSpreadsText);

            m_MaterialEditor.ShaderProperty(m_PreIntegratedFGDScale, Styles.preIntegratedFGDScaleText);
            m_MaterialEditor.TexturePropertySingleLine(Styles.preIntegratedFGDText, m_PreIntegratedFGD);
            if (GUILayout.Button(Styles.computePreIntegratedFGDText))
            {
                ComputePreIntegratedFGD();
            }

            // Color

            m_MaterialEditor.ShaderProperty(m_BRDFColorMode, Styles.brdfColorModeText);

            ++EditorGUI.indentLevel;
            if (m_BRDFColorMode.floatValue == (float)AxFCPA2BRDFColorMode.Integrated ||
                m_BRDFColorMode.floatValue == (float)AxFCPA2BRDFColorMode.Integrated1D)
            {
                m_MaterialEditor.ShaderProperty(m_DiffuseColor, Styles.diffuseColorText);
                m_MaterialEditor.ShaderProperty(m_SpecularColor, Styles.specularColorText);

                if (m_BRDFColorMode.floatValue == (float)AxFCPA2BRDFColorMode.Integrated1D)
                {
                    m_MaterialEditor.TexturePropertySingleLine(Styles.integratedColorLUT1DText, m_IntegratedColorLUT1D);
                }
            }
            else if (m_BRDFColorMode.floatValue == (float)AxFCPA2BRDFColorMode.IntegratedSky)
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.brdfColorLUTxSkyDiffuseText, m_BRDFColorLUTxSkyDiffuse, m_BRDFColorLUTxSkyDiffuseScale);
                m_MaterialEditor.TexturePropertySingleLine(Styles.brdfColorLUTxSkySpecularText, m_BRDFColorLUTxSkySpecular, m_BRDFColorLUTxSkySpecularScale);
                m_MaterialEditor.ShaderProperty(m_BRDFColorLUTxSky_ArraySize, Styles.brdfColorLUTxSkyArraySizeText);
            }
            else if (m_BRDFColorMode.floatValue == (float)AxFCPA2BRDFColorMode.IntegratedSkySH)
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.brdfColorLUTxSkyDiffuseSHText, m_BRDFColorLUTxSkyDiffuseSH, m_BRDFColorLUTxSkyDiffuseScale);
                m_MaterialEditor.TexturePropertySingleLine(Styles.brdfColorLUTxSkySpecularSHText, m_BRDFColorLUTxSkySpecularSH, m_BRDFColorLUTxSkySpecularScale);
            }
            else if (m_BRDFColorMode.floatValue == (float)AxFCPA2BRDFColorMode.IntegratedSkySVD)
            {
                m_MaterialEditor.ShaderProperty(m_BRDFColorLUTxSkyDiffuseMeanColor, Styles.brdfColorLUTxSkyDiffuseMeanColorText);
                m_MaterialEditor.ShaderProperty(m_BRDFColorLUTxSkySpecularMeanColor, Styles.brdfColorLUTxSkySpecularMeanColorText);

                m_MaterialEditor.TexturePropertySingleLine(Styles.brdfColorLUTxSkyDiffuseSVDCubemapText, m_BRDFColorLUTxSkyDiffuseCubemap, m_BRDFColorLUTxSkyDiffuseScale);
                m_MaterialEditor.TexturePropertySingleLine(Styles.brdfColorLUTxSkyDiffuseSVDArrayText, m_BRDFColorLUTxSkyDiffuseArray);

                m_MaterialEditor.TexturePropertySingleLine(Styles.brdfColorLUTxSkySpecularSVDCubemapText, m_BRDFColorLUTxSkySpecularCubemap, m_BRDFColorLUTxSkySpecularScale);
                m_MaterialEditor.TexturePropertySingleLine(Styles.brdfColorLUTxSkySpecularSVDArrayText, m_BRDFColorLUTxSkySpecularArray);
            }
            --EditorGUI.indentLevel;

            m_MaterialEditor.ShaderProperty(m_UseBRDFColorLUTForPointLights, Styles.useBrdfColorLUTForPointLightsText);
            {
                ++EditorGUI.indentLevel;
                m_MaterialEditor.TexturePropertySingleLine(Styles.brdfColorLUTText, m_BRDFColorLUT);
                --EditorGUI.indentLevel;
            }
        }

        private void SurfaceFlakesGUI()
        {
            // m_MaterialEditor.ShaderProperty(m_EnableFlakes, Styles.flakesText);
            EditorGUI.BeginChangeCheck();
            bool enableFlakes = EditorGUILayout.ToggleLeft(Styles.flakesText, m_EnableFlakes.floatValue > 0, EditorStyles.boldLabel);
            if (EditorGUI.EndChangeCheck())
                m_EnableFlakes.floatValue = enableFlakes ? 1.0f : 0.0f;

            if (enableFlakes)
            {
                ++EditorGUI.indentLevel;
                m_MaterialEditor.ShaderProperty(m_FlakesMode, Styles.flakesModeText);

                if (m_FlakesMode.floatValue == (float)AxFCPA2FlakesMode.Simple2D)
                {
                    m_MaterialEditor.TexturePropertySingleLine(Styles.simpleFlakesColorMapText, m_SimpleFlakesColorMap);
                    m_MaterialEditor.ShaderProperty(m_SimpleFlakesColorOffset, Styles.simpleFlakesColorOffsetText);
                    m_MaterialEditor.ShaderProperty(m_SimpleFlakesColorScale, Styles.simpleFlakesColorScaleText);
                }
                if (m_FlakesMode.floatValue == (float)AxFCPA2FlakesMode.Simple3D)
                {
                    m_MaterialEditor.TexturePropertySingleLine(Styles.simpleFlakesColorMap3DText, m_SimpleFlakesColorMap3D);
                    m_MaterialEditor.ShaderProperty(m_SimpleFlakesColorOffset3D, Styles.simpleFlakesColorOffset3DText);
                    m_MaterialEditor.ShaderProperty(m_SimpleFlakesColorScale3D, Styles.simpleFlakesColorScale3DText);
                    m_MaterialEditor.ShaderProperty(m_FullFlakesMaxThetaI, Styles.fullFlakesMaxThetaIText);
                    m_MaterialEditor.ShaderProperty(m_FullFlakesNumThetaI, Styles.fullFlakesNumThetaIText);
                }
                if (m_FlakesMode.floatValue == (float)AxFCPA2FlakesMode.Full4D)
                {
                    m_MaterialEditor.TexturePropertySingleLine(Styles.fullFlakesMapText, m_FullFlakesColorMap);
                    m_MaterialEditor.TexturePropertySingleLine(Styles.fullFlakesColorScaleOffsetMapText, m_FullFlakesColorScaleOffsetMap);
                    m_MaterialEditor.TexturePropertySingleLine(Styles.fullFlakesSliceLUTText, m_FullFlakesSliceLUT);
                    m_MaterialEditor.TexturePropertySingleLine(Styles.fullFlakesInvStdDevMapText, m_FullFlakesInvStdDevMap);
                    m_MaterialEditor.ShaderProperty(m_FullFlakesMaxThetaI, Styles.fullFlakesMaxThetaIText);
                    m_MaterialEditor.ShaderProperty(m_FullFlakesNumThetaI, Styles.fullFlakesNumThetaIText);
                    m_MaterialEditor.ShaderProperty(m_FullFlakesMaxThetaF, Styles.fullFlakesMaxThetaFText);
                    m_MaterialEditor.ShaderProperty(m_FullFlakesNumThetaF, Styles.fullFlakesNumThetaFText);
                }

                m_MaterialEditor.ShaderProperty(m_UseFlakesSmoothness, Styles.useFlakesSmoothnessText);
                if (m_UseFlakesSmoothness.floatValue > 0)
                {
                    ++EditorGUI.indentLevel;
                    m_MaterialEditor.ShaderProperty(m_FlakesSmoothness, Styles.flakesSmoothnessText);
                    --EditorGUI.indentLevel;
                }

                --EditorGUI.indentLevel;
            }
        }

        private void SurfaceClearCoatGUI()
        {
            // m_MaterialEditor.ShaderProperty(m_EnableClearCoat, Styles.clearCoatText);
            EditorGUI.BeginChangeCheck();
            bool enableClearCoat = EditorGUILayout.ToggleLeft(Styles.clearCoatText, m_EnableClearCoat.floatValue > 0, EditorStyles.boldLabel);
            if (EditorGUI.EndChangeCheck())
                m_EnableClearCoat.floatValue = enableClearCoat ? 1.0f : 0.0f;

            if (enableClearCoat)
            {
                ++EditorGUI.indentLevel;
                m_MaterialEditor.ShaderProperty(m_ClearCoatIOR, Styles.clearCoatIORText);
                m_MaterialEditor.ShaderProperty(m_ClearCoatF0, Styles.clearCoatF0Text);
                m_MaterialEditor.ShaderProperty(m_ClearCoatF90, Styles.clearCoatF90Text);
                --EditorGUI.indentLevel;
            }
        }

        private void SurfaceInputGUI()
        {
            // GUILayout.Label(Styles.surfaceInputText, EditorStyles.boldLabel);

            SurfaceBRDFGUI();
            EditorGUILayout.Space();
            SurfaceFlakesGUI();
            EditorGUILayout.Space();
            SurfaceClearCoatGUI();
        }

        private void TilingOptionsGUI()
        {
            GUILayout.Label(Styles.tilingText, EditorStyles.boldLabel);

            m_MaterialEditor.TextureScaleOffsetProperty(m_SimpleFlakesColorMap);

            m_MaterialEditor.ShaderProperty(m_UVTilingMode, Styles.uvTilingModeText);
            if (m_UVTilingMode.floatValue == (float)UVTilingMode.Randomized)
            {
                ++EditorGUI.indentLevel;
                m_MaterialEditor.ShaderProperty(m_UVTilingGridSize, Styles.uvTilingGridSizeText);
                --EditorGUI.indentLevel;
            }
        }

        private void AdvancedGUI()
        {
            GUILayout.Label(Styles.advancedText, EditorStyles.boldLabel);

            // m_MaterialEditor.RenderQueueField();
            m_MaterialEditor.EnableInstancingField();
            m_MaterialEditor.DoubleSidedGIField();
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            FindProperties(props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
            m_MaterialEditor = materialEditor;

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                // Use default labelWidth
                EditorGUIUtility.labelWidth = 0f;

                SurfaceOptionsGUI();
                EditorGUILayout.Space();
                SurfaceInputGUI();
                EditorGUILayout.Space();
                TilingOptionsGUI();
                EditorGUILayout.Space();
                AdvancedGUI();

                if (changed.changed)
                {
                    foreach (var material in m_MaterialEditor.targets.Select(target => target as Material))
                        SetupMaterialKeywordsAndUniforms(material);
                }
            }
        }

        private void ComputePreIntegratedFGD()
        {
            var instance = AxFCPA2ComputePreIntegratedFGD.instance;
            foreach (var material in m_MaterialEditor.targets.Select(target => target as Material))
                instance.ComputeFGDForMaterial(material);
        }

        static void SetMaterialKeywords(Material material)
        {
            // BRDF

            SetKeyword(material, "USE_GGXFIT_BRDF_MODEL", material.GetFloat(kBRDFMode) == (float)AxFCPA2BRDFMode.GGXFit);
            SetKeyword(material, "USE_FULL_BRDF_MODEL", material.GetFloat(kBRDFMode) == (float)AxFCPA2BRDFMode.MultiLobeCT);

            SetKeyword(material, "USE_INTEGRATED_COLOR", material.GetFloat(kBRDFColorMode) == (float)AxFCPA2BRDFColorMode.Integrated);
            SetKeyword(material, "USE_INTEGRATED_COLOR_LUT_1D", material.GetFloat(kBRDFColorMode) == (float)AxFCPA2BRDFColorMode.Integrated1D);
            SetKeyword(material, "USE_INTEGRATED_COLOR_X_SKY", material.GetFloat(kBRDFColorMode) == (float)AxFCPA2BRDFColorMode.IntegratedSky);
            SetKeyword(material, "USE_INTEGRATED_COLOR_X_SKY_SH", material.GetFloat(kBRDFColorMode) == (float)AxFCPA2BRDFColorMode.IntegratedSkySH);
            SetKeyword(material, "USE_INTEGRATED_COLOR_X_SKY_SVD", material.GetFloat(kBRDFColorMode) == (float)AxFCPA2BRDFColorMode.IntegratedSkySVD);

            SetKeyword(material, "USE_FULL_COLOR_LUT_POINT_LIGHTS", material.GetFloat(kUseBRDFColorLUTForPointLights) > 0);

            // Flakes

            bool enableFlakes = material.GetFloat(kEnableFlakes) > 0;

            SetKeyword(material, "USE_SIMPLE_FLAKES", enableFlakes && material.GetFloat(kFlakesMode) == (float)AxFCPA2FlakesMode.Simple2D);
            SetKeyword(material, "USE_SIMPLE_FLAKES_3D", enableFlakes && material.GetFloat(kFlakesMode) == (float)AxFCPA2FlakesMode.Simple3D);
            SetKeyword(material, "USE_FULL_FLAKES", enableFlakes && material.GetFloat(kFlakesMode) == (float)AxFCPA2FlakesMode.Full4D);

            SetKeyword(material, "USE_FLAKES_SMOOTHNESS", material.GetFloat(kUseFlakesSmoothness) > 0);

            // Clear Coat

            SetKeyword(material, "CLEAR_COAT", material.GetFloat(kEnableClearCoat) > 0);

            // Misc

            SetKeyword(material, "USE_UV_MIRRORING", material.GetFloat(kUVTilingMode) == (float)UVTilingMode.Mirroring);
            SetKeyword(material, "USE_RANDOMIZED_UV_TILING", material.GetFloat(kUVTilingMode) == (float)UVTilingMode.Randomized);


            // Set Render Queue

            float sortPriority = 0;
            if (material.HasProperty(kSortPriority))
                sortPriority = material.GetFloat(kSortPriority);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry + (int)sortPriority;

            // HDRP specific:

            bool doubleSidedEnable = material.GetFloat(kCullMode) != (float)UnityEngine.Rendering.CullMode.Back;
            SetKeyword(material, "_DOUBLESIDED_ON", doubleSidedEnable);

            // Keywords for opt-out of decals and SSR:
            bool ssrEnabled = material.HasProperty(kReceivesSSR) && material.GetFloat(kReceivesSSR) > 0.0f;
            SetKeyword(material, "_DISABLE_SSR", ssrEnabled == false);

            bool addPrecomputedVelocity = material.HasProperty(kAddPrecomputedVelocity) && material.GetFloat(kAddPrecomputedVelocity) > 0.0f;
            SetKeyword(material, "_ADD_PRECOMPUTED_VELOCITY", addPrecomputedVelocity);


            // Set Stencil State

            int stencilRef = 0; // (int)StencilUsage.Clear; // Forward case
            int stencilWriteMask = 2+4; // (int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.SubsurfaceScattering;
            int stencilRefDepth = 0;
            int stencilWriteMaskDepth = 2+4;
            int stencilRefMV = 32; // (int)StencilUsage.ObjectMotionVector;
            int stencilWriteMaskMV = 32; // (int)StencilUsage.ObjectMotionVector;

            if (ssrEnabled)
            {
                stencilRefDepth |= 8; // (int)StencilUsage.TraceReflectionRay;
                stencilRefMV |= 8; // (int)StencilUsage.TraceReflectionRay;
            }

            stencilWriteMaskDepth |= 8; // (int)StencilUsage.TraceReflectionRay;
            stencilWriteMaskMV |= 8; // (int)StencilUsage.TraceReflectionRay;

            // As we tag both during motion vector pass and Gbuffer pass we need a separate state and we need to use the write mask
            if (material.HasProperty(kStencilRef))
                material.SetInt(kStencilRef, stencilRef);
            if (material.HasProperty(kStencilWriteMask))
                material.SetInt(kStencilWriteMask, stencilWriteMask);
            if (material.HasProperty(kStencilRefDepth))
                material.SetInt(kStencilRefDepth, stencilRefDepth);
            if (material.HasProperty(kStencilWriteMaskDepth))
                material.SetInt(kStencilWriteMaskDepth, stencilWriteMaskDepth);
            if (material.HasProperty(kStencilRefMV))
                material.SetInt(kStencilRefMV, stencilRefMV);
            if (material.HasProperty(kStencilWriteMaskMV))
                material.SetInt(kStencilWriteMaskMV, stencilWriteMaskMV);
        }

        public static void SetupMaterialKeywordsAndUniforms(Material material)
        {
            // SetupMaterialUniforms(material);

            SetMaterialKeywords(material);
        }

        static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }
    }
}
