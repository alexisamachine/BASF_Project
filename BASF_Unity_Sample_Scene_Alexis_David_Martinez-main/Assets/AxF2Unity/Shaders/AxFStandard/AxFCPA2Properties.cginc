// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

#ifndef AXFCPA2PROPERTIES_CGINC
#define AXFCPA2PROPERTIES_CGINC

//#ifdef USE_GGXFIT_BRDF_MODEL
    half _GGXSmoothness;
//#endif // USE_GGXFIT_BRDF_MODEL
//#ifdef USE_FULL_BRDF_MODEL
    half4 _CTCoeffs;
    half4 _CTF0s;
    half4 _CTSpreads;
//#endif // USE_FULL_BRDF_MODEL

half4 _PreIntegratedFGDScale;
UNITY_DECLARE_TEX2D(_PreIntegratedFGD);
half4 _PreIntegratedFGD_TexelSize;

half4 _DiffuseColor;
half4 _SpecularColor;

#ifdef USE_INTEGRATED_COLOR_LUT_1D
    // UNITY_DECLARE_TEX2D(_DiffuseColorLUT1D);
    // UNITY_DECLARE_TEX2D(_SpecularColorLUT1D);
    UNITY_DECLARE_TEX2D(_IntegratedColorLUT1D);
    float4 _IntegratedColorLUT1D_TexelSize; // (1 / width, 1 / height, width, height)
    // Reuse _Smoothness as scale!
    // Reuse _SpecularColor as scale!
#endif // USE_INTEGRATED_COLOR_LUT_1D
#ifdef USE_INTEGRATED_COLOR_X_SKY
    UNITY_DECLARE_TEXCUBEARRAY(_BRDFColorLUTxSkyDiffuse);
    UNITY_DECLARE_TEXCUBEARRAY(_BRDFColorLUTxSkySpecular);
    int _BRDFColorLUTxSky_ArraySize;
    float _BRDFColorLUTxSkyDiffuseScale;
    float _BRDFColorLUTxSkySpecularScale;
#endif // USE_INTEGRATED_COLOR_X_SKY
#ifdef USE_INTEGRATED_COLOR_X_SKY_SH
    UNITY_DECLARE_TEX2D(_BRDFColorLUTxSkyDiffuseSH);
    UNITY_DECLARE_TEX2D(_BRDFColorLUTxSkySpecularSH);
    float4 _BRDFColorLUTxSkyDiffuseSH_TexelSize; // float4(1/width, 1/height, width, height)
    float4 _BRDFColorLUTxSkySpecularSH_TexelSize;
    float _BRDFColorLUTxSkyDiffuseScale;
    float _BRDFColorLUTxSkySpecularScale;
#endif // USE_INTEGRATED_COLOR_X_SKY_SH
#ifdef USE_INTEGRATED_COLOR_X_SKY_SVD
    UNITY_DECLARE_TEXCUBE(_BRDFColorLUTxSkyDiffuseCubemap);
    UNITY_DECLARE_TEX2D(_BRDFColorLUTxSkyDiffuseArray);
    UNITY_DECLARE_TEXCUBE(_BRDFColorLUTxSkySpecularCubemap);
    UNITY_DECLARE_TEX2D(_BRDFColorLUTxSkySpecularArray);
    float4 _BRDFColorLUTxSkyDiffuseArray_TexelSize; // float4(1/width, 1/height, width, height)
    float4 _BRDFColorLUTxSkySpecularArray_TexelSize;
    float _BRDFColorLUTxSkyDiffuseScale;
    float _BRDFColorLUTxSkySpecularScale;
    float3 _BRDFColorLUTxSkyDiffuseMeanColor;
    float3 _BRDFColorLUTxSkySpecularMeanColor;
#endif // USE_INTEGRATED_COLOR_X_SKY_SVD
#ifdef USE_FULL_COLOR_LUT_POINT_LIGHTS
    // half _BRDFColorLUTScale; // Bake into CTCoeffs!
    UNITY_DECLARE_TEX2D(_BRDFColorLUT);
    float4 _BRDFColorLUT_TexelSize; // (1 / width, 1 / height, width, height)
#endif // USE_FULL_COLOR_LUT_POINT_LIGHTS


float4 _SimpleFlakesColorMap_ST;
#ifdef USE_SIMPLE_FLAKES
    UNITY_DECLARE_TEX2D(_SimpleFlakesColorMap);
    float _SimpleFlakesColorOffset;
    float _SimpleFlakesColorScale;
#endif // USE_SIMPLE_FLAKES
#ifdef USE_SIMPLE_FLAKES_3D
    UNITY_DECLARE_TEX2DARRAY(_SimpleFlakesColorMap3D);
    float _SimpleFlakesColorOffset3D;
    float _SimpleFlakesColorScale3D;
    float _FullFlakesMaxThetaI;
    float _FullFlakesNumThetaI;
#endif // USE_SIMPLE_FLAKES_3D
#ifdef USE_FULL_FLAKES
    UNITY_DECLARE_TEX2DARRAY(_FullFlakesColorMap);
    UNITY_DECLARE_TEX2D(_FullFlakesColorScaleOffsetMap);
    UNITY_DECLARE_TEX2D(_FullFlakesSliceLUT);
    UNITY_DECLARE_TEX2D(_FullFlakesInvStdDevMap);
    float _FullFlakesMaxThetaI;
    float _FullFlakesMaxThetaF;
    float _FullFlakesNumThetaI;
    float _FullFlakesNumThetaF;
#endif // USE_FULL_FLAKES
#ifdef USE_FLAKES_SMOOTHNESS
    half _FlakesSmoothness;
#endif // USE_FLAKES_SMOOTHNESS

#ifdef CLEAR_COAT
    half _ClearCoatIOR;
    half _ClearCoatF0;
    half _ClearCoatF90;
#endif // CLEAR_COAT

#endif // AXFCPA2PROPERTIES_CGINC
