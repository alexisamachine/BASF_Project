// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung
#ifndef AXFCPA2HDRPPROPERTIES_HLSL
#define AXFCPA2HDRPPROPERTIES_HLSL

//
// Textures & Samplers
//

TEXTURE2D(_PreIntegratedFGD);
SAMPLER(sampler_PreIntegratedFGD);

#ifdef USE_INTEGRATED_COLOR_LUT_1D
    TEXTURE2D(_IntegratedColorLUT1D);
    SAMPLER(sampler_IntegratedColorLUT1D);
#endif // USE_INTEGRATED_COLOR_LUT_1D
#ifdef USE_INTEGRATED_COLOR_X_SKY
    TEXTURECUBE_ARRAY(_BRDFColorLUTxSkyDiffuse);
    SAMPLER(sampler_BRDFColorLUTxSkyDiffuse);
    TEXTURECUBE_ARRAY(_BRDFColorLUTxSkySpecular);
    SAMPLER(sampler_BRDFColorLUTxSkySpecular);
#endif // USE_INTEGRATED_COLOR_X_SKY
#ifdef USE_INTEGRATED_COLOR_X_SKY_SH
    TEXTURE2D(_BRDFColorLUTxSkyDiffuseSH);
    SAMPLER(sampler_BRDFColorLUTxSkyDiffuseSH);
    TEXTURE2D(_BRDFColorLUTxSkySpecularSH);
    SAMPLER(sampler_BRDFColorLUTxSkySpecularSH);
#endif // USE_INTEGRATED_COLOR_X_SKY_SH
#ifdef USE_INTEGRATED_COLOR_X_SKY_SVD
    TEXTURECUBE(_BRDFColorLUTxSkyDiffuseCubemap);
    SAMPLER(sampler_BRDFColorLUTxSkyDiffuseCubemap);
    TEXTURE2D(_BRDFColorLUTxSkyDiffuseArray);
    SAMPLER(sampler_BRDFColorLUTxSkyDiffuseArray);
    TEXTURECUBE(_BRDFColorLUTxSkySpecularCubemap);
    SAMPLER(sampler_BRDFColorLUTxSkySpecularCubemap);
    TEXTURE2D(_BRDFColorLUTxSkySpecularArray);
    SAMPLER(sampler_BRDFColorLUTxSkySpecularArray);
#endif // USE_INTEGRATED_COLOR_X_SKY_SVD

#ifdef USE_FULL_COLOR_LUT_POINT_LIGHTS
    TEXTURE2D(_BRDFColorLUT);
    SAMPLER(sampler_BRDFColorLUT);
#endif // USE_FULL_COLOR_LUT_POINT_LIGHTS

#ifdef USE_SIMPLE_FLAKES_3D
    TEXTURE2D_ARRAY(_SimpleFlakesColorMap3D);
    SAMPLER(sampler_SimpleFlakesColorMap3D);
#endif // USE_SIMPLE_FLAKES_3D
#ifdef USE_SIMPLE_FLAKES
    TEXTURE2D(_SimpleFlakesColorMap);
    SAMPLER(sampler_SimpleFlakesColorMap);
#endif // USE_SIMPLE_FLAKES
#ifdef USE_FULL_FLAKES
    TEXTURE2D_ARRAY(_FullFlakesColorMap);
    TEXTURE2D(_FullFlakesColorScaleOffsetMap);
    TEXTURE2D(_FullFlakesSliceLUT);
    TEXTURE2D(_FullFlakesInvStdDevMap);

    SAMPLER(sampler_FullFlakesColorMap);
    SAMPLER(sampler_FullFlakesColorScaleOffsetMap);
    SAMPLER(sampler_FullFlakesSliceLUT);
    SAMPLER(sampler_FullFlakesInvStdDevMap);
#endif // USE_FULL_FLAKES


//
// Uniform Parameters
//

CBUFFER_START(UnityPerMaterial)

    float _GGXSmoothness;
    float4 _CTCoeffs;
    float4 _CTF0s;
    float4 _CTSpreads;

    float4 _PreIntegratedFGDScale;
    float4 _PreIntegratedFGD_TexelSize;

    float4 _DiffuseColor;
    float4 _SpecularColor;

#ifdef USE_INTEGRATED_COLOR_LUT_1D
    float4 _IntegratedColorLUT1D_TexelSize; // (1 / width, 1 / height, width, height)
#endif // USE_INTEGRATED_COLOR_LUT_1D
#ifdef USE_INTEGRATED_COLOR_X_SKY
    int _BRDFColorLUTxSky_ArraySize;
    float _BRDFColorLUTxSkyDiffuseScale;
    float _BRDFColorLUTxSkySpecularScale;
#endif // USE_INTEGRATED_COLOR_X_SKY
#ifdef USE_INTEGRATED_COLOR_X_SKY_SH
    float4 _BRDFColorLUTxSkyDiffuseSH_TexelSize; // float4(1/width, 1/height, width, height)
    float4 _BRDFColorLUTxSkySpecularSH_TexelSize;
    float _BRDFColorLUTxSkyDiffuseScale;
    float _BRDFColorLUTxSkySpecularScale;
#endif // USE_INTEGRATED_COLOR_X_SKY_SH
#ifdef USE_INTEGRATED_COLOR_X_SKY_SVD
    float4 _BRDFColorLUTxSkyDiffuseArray_TexelSize; // float4(1/width, 1/height, width, height)
    float4 _BRDFColorLUTxSkySpecularArray_TexelSize;
    float _BRDFColorLUTxSkyDiffuseScale;
    float _BRDFColorLUTxSkySpecularScale;
    float3 _BRDFColorLUTxSkyDiffuseMeanColor;
    float3 _BRDFColorLUTxSkySpecularMeanColor;
#endif // USE_INTEGRATED_COLOR_X_SKY_SVD

#ifdef USE_FULL_COLOR_LUT_POINT_LIGHTS
    float4 _BRDFColorLUT_TexelSize; // (1 / width, 1 / height, width, height)
#endif // USE_FULL_COLOR_LUT_POINT_LIGHTS


float4 _SimpleFlakesColorMap_ST;
#ifdef USE_SIMPLE_FLAKES
    float _SimpleFlakesColorOffset;
    float _SimpleFlakesColorScale;
#endif // USE_SIMPLE_FLAKES
#ifdef USE_SIMPLE_FLAKES_3D
    float _SimpleFlakesColorOffset3D;
    float _SimpleFlakesColorScale3D;
    float _FullFlakesMaxThetaI;
    float _FullFlakesNumThetaI;
#endif // USE_SIMPLE_FLAKES_3D
#ifdef USE_FULL_FLAKES
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


// UVTiling properties:
float _UVTilingGridSize;

// Following two variables are feeded by the C++ Editor for Scene selection
int _ObjectId;
int _PassValue;

CBUFFER_END

#endif // AXFCPA2HDRPPROPERTIES_HLSL
