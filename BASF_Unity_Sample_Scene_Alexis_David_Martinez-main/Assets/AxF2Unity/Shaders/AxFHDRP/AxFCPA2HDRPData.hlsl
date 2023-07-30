// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung
#ifndef AXFCPA2HDRPDATA_HLSL
#define AXFCPA2HDRPDATA_HLSL

//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl"
#include "UVTiling.hlsl"

void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
{
    // TODO support decals
}

void GetSurfaceAndBuiltinData(FragInputs input, float3 viewDirWS, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef _DOUBLESIDED_ON
    // 'doubleSidedConstants' is float3(-1, -1, -1) in flip mode and float3(1, 1, -1) in mirror mode.
    float3 doubleSidedConstants = float3(-1.0, -1.0, -1.0);
#else
    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
#endif

    ApplyDoubleSidedFlipOrMirror(input, doubleSidedConstants); // Apply double sided flip on the vertex normal

    float alpha = 1.0;

    surfaceData.uv = TRANSFORM_TEX(input.texCoord0.xy, _SimpleFlakesColorMap);
    surfaceData.duvdx = ddx(surfaceData.uv);
    surfaceData.duvdy = ddy(surfaceData.uv);

    ApplyUVTiling(surfaceData.uv, surfaceData.duvdx, surfaceData.duvdy);


    surfaceData.normalWS = input.tangentToWorld[2].xyz;


    float NdotV = dot(surfaceData.normalWS, viewDirWS);

    surfaceData.diffuseColor = _DiffuseColor.rgb;
    surfaceData.specularColor = _SpecularColor.rgb;
#ifdef USE_INTEGRATED_COLOR_LUT_1D
    {
        // Multiply to SpecularColor and Smoothness...

        // _IntegratedColorLUT1D is N x 2 texture!
        // NdotV maps to last pixel
        float lutCoord = NdotV - 0.5 * _IntegratedColorLUT1D_TexelSize.x; // 1/width
        float4 diffuseLUTValue = SAMPLE_TEXTURE2D_LOD(_IntegratedColorLUT1D, sampler_IntegratedColorLUT1D, float2(lutCoord, 0.25), 0);
        float4 specularLUTValue = SAMPLE_TEXTURE2D_LOD(_IntegratedColorLUT1D, sampler_IntegratedColorLUT1D, float2(lutCoord, 0.75), 0);

        surfaceData.diffuseColor *= diffuseLUTValue.rgb;
        surfaceData.specularColor *= specularLUTValue.rgb;
    }
#endif // USE_INTEGRATED_COLOR_LUT_1D
#ifdef USE_INTEGRATED_COLOR_X_SKY
    {
        float3 reflDirWS = reflect(-viewDirWS, surfaceData.normalWS);

        float arraySlice = (1 - NdotV) * _BRDFColorLUTxSky_ArraySize;
        float alpha = frac(arraySlice);
        int arraySlice_low = floor(arraySlice);
        int arraySlice_high = min(arraySlice_low+1, _BRDFColorLUTxSky_ArraySize-1);

        float3 diffuseColorLow = SAMPLE_TEXTURECUBE_ARRAY_LOD(_BRDFColorLUTxSkyDiffuse, sampler_BRDFColorLUTxSkyDiffuse, reflDirWS, arraySlice_low, 0).xyz;
        float3 diffuseColorHigh = SAMPLE_TEXTURECUBE_ARRAY_LOD(_BRDFColorLUTxSkyDiffuse, sampler_BRDFColorLUTxSkyDiffuse, reflDirWS, arraySlice_high, 0).xyz;

        float3 specularColorLow = SAMPLE_TEXTURECUBE_ARRAY_LOD(_BRDFColorLUTxSkySpecular, sampler_BRDFColorLUTxSkySpecular, reflDirWS, arraySlice_low, 0).xyz;
        float3 specularColorHigh = SAMPLE_TEXTURECUBE_ARRAY_LOD(_BRDFColorLUTxSkySpecular, sampler_BRDFColorLUTxSkySpecular, reflDirWS, arraySlice_high, 0).xyz;

        surfaceData.diffuseColor = lerp(diffuseColorLow, diffuseColorHigh, alpha) * _BRDFColorLUTxSkyDiffuseScale;
        surfaceData.specularColor = lerp(specularColorLow, specularColorHigh, alpha) * _BRDFColorLUTxSkySpecularScale;
    }
#endif // USE_INTEGRATED_COLOR_X_SKY
#ifdef USE_INTEGRATED_COLOR_X_SKY_SH
    {
        float3 reflDirWS = reflect(-viewDirWS, surfaceData.normalWS);

        // diffuse SH coeffs
        float4 diffuseSHCoeffs[7];
        for (int i = 0; i < 7; ++i)
        {
            // TODO verify UV
            // assert(_BRDFColorLUTxSkyDiffuseSH_TexelSize.w == 7)
            float2 uv = float2(1 - NdotV + 0.5*_BRDFColorLUTxSkyDiffuseSH_TexelSize.x, (i+0.5)/7.0);
            diffuseSHCoeffs[i] = SAMPLE_TEXTURE2D_LOD(_BRDFColorLUTxSkyDiffuseSH, sampler_BRDFColorLUTxSkyDiffuseSH, uv, 0);
        }
        float3 diffuseColor = SampleSH9(diffuseSHCoeffs, reflDirWS);

        // specular SH coeffs
        float4 specularSHCoeffs[7];
        for (int i = 0; i < 7; ++i)
        {
            // TODO verify UV
            // assert(_BRDFColorLUTxSkySpecularSH_TexelSize.w == 7)
            float2 uv = float2(1 - NdotV + 0.5*_BRDFColorLUTxSkySpecularSH_TexelSize.x, (i+0.5)/7.0);
            specularSHCoeffs[i] = SAMPLE_TEXTURE2D_LOD(_BRDFColorLUTxSkySpecularSH, sampler_BRDFColorLUTxSkySpecularSH, uv, 0);
        }
        float3 specularColor = SampleSH9(specularSHCoeffs, reflDirWS);

        surfaceData.diffuseColor = diffuseColor * _BRDFColorLUTxSkyDiffuseScale;
        surfaceData.specularColor = specularColor * _BRDFColorLUTxSkySpecularScale;
    }
#endif // USE_INTEGRATED_COLOR_X_SKY_SH
#ifdef USE_INTEGRATED_COLOR_X_SKY_SVD
    {
        float3 reflDirWS = reflect(-viewDirWS, surfaceData.normalWS);

        float4 diffuseVector = SAMPLE_TEXTURECUBE_LOD(_BRDFColorLUTxSkyDiffuseCubemap, sampler_BRDFColorLUTxSkyDiffuseCubemap, reflDirWS, 0);
        // diffuseVector.yzw = 0;
        float3 diffuseColor = _BRDFColorLUTxSkyDiffuseMeanColor;
        for (int i = 0; i < 3; ++i)
        {
            float2 uv = float2( 1 - NdotV + 0.5*_BRDFColorLUTxSkyDiffuseArray_TexelSize.x, (i + 0.5) / 3 );
            diffuseColor[i] += dot(diffuseVector, SAMPLE_TEXTURE2D_LOD(_BRDFColorLUTxSkyDiffuseArray, sampler_BRDFColorLUTxSkyDiffuseArray, uv, 0));
        }

        float4 specularVector = SAMPLE_TEXTURECUBE_LOD(_BRDFColorLUTxSkySpecularCubemap, sampler_BRDFColorLUTxSkySpecularCubemap, reflDirWS, 0);
        // specularVector.yzw = 0;
        float3 specularColor = _BRDFColorLUTxSkySpecularMeanColor;
        for (int i = 0; i < 3; ++i)
        {
            float2 uv = float2( 1 - NdotV + 0.5*_BRDFColorLUTxSkySpecularArray_TexelSize.x, (i + 0.5) / 3 );
            specularColor[i] += dot(specularVector, SAMPLE_TEXTURE2D_LOD(_BRDFColorLUTxSkySpecularArray, sampler_BRDFColorLUTxSkySpecularArray, uv, 0));
        }

        surfaceData.diffuseColor = diffuseColor * _BRDFColorLUTxSkyDiffuseScale;
        surfaceData.specularColor = specularColor * _BRDFColorLUTxSkySpecularScale;
    }
#endif // USE_INTEGRATED_COLOR_X_SKY_SVD




    // GGX Fit
    surfaceData.ggxSmoothness = _GGXSmoothness;

    // CT Lobes
    surfaceData.ctCoeffs = _CTCoeffs;
    surfaceData.ctF0s = _CTF0s;
    surfaceData.ctSpreads = _CTSpreads;


    // Pre integrated FGD and friends
    float4 preIntegratedFGDValues = _PreIntegratedFGDScale * SAMPLE_TEXTURE2D_LOD(_PreIntegratedFGD, sampler_PreIntegratedFGD, float2(NdotV - 0.5*_PreIntegratedFGD_TexelSize.x, 0), 0);
    surfaceData.ctFGD = preIntegratedFGDValues.x;
    surfaceData.ggxFGD = preIntegratedFGDValues.y;
    surfaceData.flakesFGD = preIntegratedFGDValues.z;
    surfaceData.iblSmoothness = _GGXSmoothness; // preIntegratedFGDValues.w;
    surfaceData.diffuseFGD = _CTCoeffs.w;


    surfaceData.flakesColor = float3(0,0,0);
#ifdef USE_SIMPLE_FLAKES
    surfaceData.flakesColor = _SimpleFlakesColorOffset.rrr + _SimpleFlakesColorScale * SAMPLE_TEXTURE2D_GRAD(_SimpleFlakesColorMap, sampler_SimpleFlakesColorMap, surfaceData.uv, surfaceData.duvdx, surfaceData.duvdy).rgb;
#endif // USE_SIMPLE_FLAKES
#ifdef USE_SIMPLE_FLAKES_3D
    float NdotV_below = NdotV;
#ifdef CLEAR_COAT
    NdotV_below = sqrt(1 - 1.0/Sq(_ClearCoatIOR) * (1 - Sq(NdotV)));
#endif // CLEAR_COAT
    float f_thetaI = acos(clamp(NdotV_below, 0, 1));
    float sliceIndex = (f_thetaI / (0.5 * PI) * _FullFlakesNumThetaI) + 0.5;
    float sliceIndex_low = floor(sliceIndex);
    float sliceIndex_high = min(sliceIndex_low+1, _FullFlakesMaxThetaI-1);
    float rawFlakesLerpFactor = sliceIndex - sliceIndex_low;
    float3 rawFlakesValue_low = SAMPLE_TEXTURE2D_ARRAY_GRAD(_SimpleFlakesColorMap3D, sampler_SimpleFlakesColorMap3D, surfaceData.uv, sliceIndex_low, surfaceData.duvdx, surfaceData.duvdy).rgb;
    float3 rawFlakesValue_high = SAMPLE_TEXTURE2D_ARRAY_GRAD(_SimpleFlakesColorMap3D, sampler_SimpleFlakesColorMap3D, surfaceData.uv, sliceIndex_high, surfaceData.duvdx, surfaceData.duvdy).rgb;
    float3 rawFlakesValue = lerp(rawFlakesValue_low, rawFlakesValue_high, rawFlakesLerpFactor);
    surfaceData.flakesColor = _SimpleFlakesColorOffset3D.rrr + _SimpleFlakesColorScale3D * rawFlakesValue;
#endif // USE_SIMPLE_FLAKES_3D
#ifdef USE_FULL_FLAKES
    float NdotV_below = NdotV;
#ifdef CLEAR_COAT
    NdotV_below = sqrt(1 - 1.0/Sq(_ClearCoatIOR) * (1 - Sq(NdotV)));
#endif // CLEAR_COAT
    float f_thetaI = acos(clamp(NdotV_below, 0, 1));
    float sliceIndex = (f_thetaI / (0.5 * PI) * _FullFlakesNumThetaI) + 0.5;
    float sliceIndex_low = floor(sliceIndex);
    float sliceIndex_high = min(sliceIndex_low+1, _FullFlakesMaxThetaI-1);
    float flakesLerpFactor = sliceIndex - sliceIndex_low;
    float3 flakesValue_low = SampleFlakesSlice(SampleFlakesSliceLUT(sliceIndex_low), 0, surfaceData.uv, surfaceData.duvdx, surfaceData.duvdy).rgb;
    float3 flakesValue_high = SampleFlakesSlice(SampleFlakesSliceLUT(sliceIndex_high), 0, surfaceData.uv, surfaceData.duvdx, surfaceData.duvdy).rgb;
    float3 flakesValue = lerp(flakesValue_low, flakesValue_high, flakesLerpFactor);
    float flakesInvStdDev = SAMPLE_TEXTURE2D_LOD(_FullFlakesInvStdDevMap, sampler_FullFlakesInvStdDevMap, float2((sliceIndex+0.5) / _FullFlakesMaxThetaI, 0), 0).x;
    surfaceData.flakesColor = flakesValue * flakesInvStdDev;
#endif // USE_FULL_FLAKES

#ifdef CLEAR_COAT
    surfaceData.clearCoatF0 = _ClearCoatF0;
    surfaceData.clearCoatF90 = _ClearCoatF90;
    surfaceData.clearCoatIOR = _ClearCoatIOR;
    surfaceData.clearCoatReflectance = F_Schlick(surfaceData.clearCoatF0, surfaceData.clearCoatF90, NdotV);

    surfaceData.diffuseFGD *= 0.91428571 * (1.0 - surfaceData.clearCoatReflectance);
    // Attenuate base material for view direction
    //o.CTFGD *= (1.0 - o.ClearCoatReflectance);
    //o.FlakesFGD *= (1.0 - o.ClearCoatReflectance);
#else
    surfaceData.clearCoatF0 = 0;
    surfaceData.clearCoatF90 = 0;
    surfaceData.clearCoatIOR = 1;
    surfaceData.clearCoatReflectance = 0;
#endif // CLEAR_COAT


    surfaceData.occlusion = 1;



    // Propagate the geometry normal
    surfaceData.geomNormalWS = input.tangentToWorld[2];

    #if HAVE_DECALS
        if (_EnableDecals)
        {
            // Both uses and modifies 'surfaceData.normalWS'.
            DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, input, alpha);
            ApplyDecalToSurfaceData(decalSurfaceData, surfaceData);
        }
    #endif

    // -------------------------------------------------------------
    // Builtin Data:
    // -------------------------------------------------------------

    // No back lighting with AxF
    InitBuiltinData(posInput, alpha, surfaceData.normalWS, -surfaceData.normalWS, input.texCoord1, input.texCoord2, builtinData);
    builtinData.emissiveColor = float3(0,0,0);

    PostInitBuiltinData(viewDirWS, posInput, surfaceData, builtinData);
}

#endif // AXFCPA2HDRPDATA_HLSL
