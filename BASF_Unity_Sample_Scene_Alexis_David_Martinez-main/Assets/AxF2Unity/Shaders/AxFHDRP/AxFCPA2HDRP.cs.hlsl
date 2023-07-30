// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung
//
// This file would usually be generated from AxFCPA2HDRP.cs, but this does not work if we are not "within" the HDRP....
//

#ifndef AXFCPA2HDRP_CS_HLSL
#define AXFCPA2HDRP_CS_HLSL

// Generated from AxF2Unity.AxFCPA2HDRP+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    float2 uv;
    float2 duvdx;
    float2 duvdy;
    float3  normalWS;
    float3 diffuseColor;
    float3 specularColor;
    float diffuseFGD;
    float ggxSmoothness;
    float ggxFGD;
    float4 ctCoeffs;
    float4 ctF0s;
    float4 ctSpreads;
    float  ctFGD;
    float iblSmoothness;
    float3 flakesColor;
    float flakesFGD;
    float clearCoatF0;
    float clearCoatF90;
    float clearCoatIOR;
    float clearCoatReflectance;
    float occlusion;
    float3 geomNormalWS;
};

// Generated from AxF2Unity.AxFCPA2HDRP+BSDFData
// PackingRules = Exact
struct BSDFData
{
    float2 uv;
    float2 duvdx;
    float2 duvdy;
    float3  normalWS;
    float3 diffuseColor;
    float3 specularColor;
    float diffuseFGD;
    float ggxPerceptualRoughness;
    float ggxFGD;
    float4 ctCoeffs;
    float4 ctF0s;
    float4 ctSpreads;
    float ctFGD;
    float iblPerceptualRoughness;
    float3 flakesColor;
    float flakesFGD;
    float clearCoatF0;
    float clearCoatF90;
    float clearCoatIOR;
    float clearCoatReflectance;
    float occlusion;
    float3 geomNormalWS;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    // No one cares...
}

//
// Debug functions
//
void GetGeneratedBSDFDataDebug(uint paramId, BSDFData bsdfdata, inout float3 result, inout bool needLinearToSRGB)
{
    // No one cares...
}


#endif
