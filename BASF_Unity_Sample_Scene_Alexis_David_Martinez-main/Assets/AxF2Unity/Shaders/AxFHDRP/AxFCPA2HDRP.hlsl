// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung
#ifndef AXFCPA2HDRP_HLSL
#define AXFCPA2HDRP_HLSL

//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
#include "AxFCPA2HDRP.cs.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

// Add support for LTC Area Lights
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"
//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFLTCAreaLight/AxFLTCAreaLight.hlsl"


//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

float4 GetDiffuseOrDefaultColor(BSDFData bsdfData, float replace)
{
    float weight = bsdfData.ctFGD / (bsdfData.diffuseFGD + bsdfData.ctFGD);

    return float4(lerp(bsdfData.diffuseColor, bsdfData.specularColor, weight * replace), weight);
}

float3 GetNormalForShadowBias(BSDFData bsdfData)
{
    return bsdfData.geomNormalWS;
}

float GetAmbientOcclusionForMicroShadowing(BSDFData bsdfData)
{
    return bsdfData.occlusion;
}

//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------
void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedSurfaceDataDebug(paramId, surfaceData, result, needLinearToSRGB);

    // No one cares...
}

void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);

    // No one cares...
}

void GetPBRValidatorDebug(SurfaceData surfaceData, inout float3 result)
{
    result = surfaceData.diffuseColor;
}

bool HasClearCoat()
{
#ifdef CLEAR_COAT
    return true;
#else
    return false;
#endif // CLEAR_COAT
}



// This function is used to help with debugging and must be implemented by any lit material
// Implementer must take into account what are the current override component and
// adjust SurfaceData properties accordingdly
void ApplyDebugToSurfaceData(float3x3 tangentToWorld, inout SurfaceData surfaceData)
{
    // No one cares...
}

// This function is similar to ApplyDebugToSurfaceData but for BSDFData
//
// NOTE:
//  This will be available and used in ShaderPassForward.hlsl since in AxF.shader,
//  just before including the core code of the pass (ShaderPassForward.hlsl) we include
//  Material.hlsl (or Lighting.hlsl which includes it) which in turn includes us,
//  AxF.shader, via the #if defined(UNITY_MATERIAL_*) glue mechanism.
//
void ApplyDebugToBSDFData(inout BSDFData bsdfData)
{
    // No one cares...
}

NormalData ConvertSurfaceDataToNormalData(SurfaceData surfaceData)
{
    NormalData normalData;

    normalData.normalWS = surfaceData.normalWS;

    if (HasClearCoat())
    {
        normalData.perceptualRoughness = 0;
    }
    else
    {
        normalData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.ggxSmoothness);
    }

    return normalData;
}


//-----------------------------------------------------------------------------
// BRDF utility methods
//-----------------------------------------------------------------------------

float D_Beckmann(float NdotH, float roughness)
{
    // Just don't!
    //if (roughness == 0)
    //    return 0;

    float NdotH_sqr = Sq(NdotH);
    float m_sqr = Sq(roughness);
    float e = (NdotH_sqr - 1) / (NdotH_sqr*m_sqr);
    return INV_PI * exp(e) / (m_sqr * Sq(NdotH_sqr));
}

float V_SmithJointGGX_Stable(float NoL, float NoV, float roughness)
{
    float a2 = Sq(roughness);
    float lambdaV = NoL * sqrt(NoV * (1 - a2) * NoV + a2);
    float lambdaL = NoV * sqrt(NoL * (1 - a2) * NoL + a2);
    return 0.5 / (lambdaV + lambdaL);
}


#ifdef USE_FULL_COLOR_LUT_POINT_LIGHTS
float3 SampleBRDFColorLUT(float NdotH_below, float HdotL_below)
{
    float theta_f = acos(clamp(NdotH_below, 0, 1));
    float theta_i = acos(clamp(HdotL_below, 0, 1));

    float2 uv = float2(theta_f, theta_i) / (0.5 * PI);
    uv += 0.5*_BRDFColorLUT_TexelSize.xy;
    return SAMPLE_TEXTURE2D_LOD(_BRDFColorLUT, sampler_BRDFColorLUT, uv, 0).xyz;
}
#endif // USE_FULL_COLOR_LUT_POINT_LIGHTS


#ifdef USE_FULL_FLAKES
float SampleFlakesSliceLUT(int index)
{
    return SAMPLE_TEXTURE2D_LOD(_FullFlakesSliceLUT, sampler_FullFlakesSliceLUT, float2((index+0.5)/64.0, 0), 0).r * 255.0;
}

float3 SampleFlakesSlice(int sliceBase, int sliceOffset, float2 uv, float2 duvdx, float2 duvdy)
{
    float2 scaleOffset = SAMPLE_TEXTURE2D_LOD(_FullFlakesColorScaleOffsetMap, sampler_FullFlakesColorScaleOffsetMap, float2((sliceOffset+0.5) / _FullFlakesMaxThetaF, 0), 0).xy;
    float3 rawFlake = SAMPLE_TEXTURE2D_ARRAY_GRAD(_FullFlakesColorMap, sampler_FullFlakesColorMap, uv, sliceBase + sliceOffset, duvdx, duvdy).rgb;

    return rawFlake * scaleOffset.x + scaleOffset.y;
}

float3 SampleFlakesBTF(float NdotH_below, float HdotL_below, float2 uv, float2 duvdx, float2 duvdy)
{
    // f_thetaF/I in [0, pi/2]
    float f_thetaF = acos(clamp(NdotH_below, 0, 1));
    float f_thetaI = acos(clamp(HdotL_below, 0, 1));

    // thetaF sampling defines the angular sampling, i.e. angular flake lifetime
    // int i_angular_sampling = _FullFlakesNumThetaF;
    f_thetaF = (f_thetaF / (0.5 * PI) * _FullFlakesNumThetaF) + 0.5;
    f_thetaI = (f_thetaI / (0.5 * PI) * _FullFlakesNumThetaI) + 0.5;

    // Bilinear interp indices and weights
    int i_thetaF_low = int(floor(f_thetaF));
    int i_thetaF_high = i_thetaF_low + 1;
    int i_thetaI_low = int(floor(f_thetaI));
    int i_thetaI_high = i_thetaI_low + 1;
    float f_thetaF_w = f_thetaF - float(i_thetaF_low);
    float f_thetaI_w = f_thetaI - float(i_thetaI_low);

    //  to allow lower thetaI samplings while preserving flake lifetime
    // "virtual" thetaI patches are generated by shifting existing ones
    float2 v2_offset_l = float2(0,0);
    float2 v2_offset_h = float2(0,0);
    /*
    if ( flakesNumThetaI < i_angular_sampling )
    {
        v2_offset_l = vec2(rnd_numbers[2*i_thetaI_low],rnd_numbers[2*i_thetaI_low+1]);
        v2_offset_h = vec2(rnd_numbers[2*i_thetaI_high],rnd_numbers[2*i_thetaI_high+1]);
        if (i_thetaI_low%2==1) uv.xy = uv.yx;
        if (i_thetaI_high%2==1) uv.xy = uv.yx;
        //map to the original sampling
        i_thetaI_low = int(floor(i_thetaI_low*float(materialData.flakesNumThetaI)/float(materialData.flakesNumThetaF)));
        i_thetaI_high = int(floor(i_thetaI_high*float(materialData.flakesNumThetaI)/float(materialData.flakesNumThetaF)));
    }
    */

    float3 v3_ll = float3(0, 0, 0);
    float3 v3_lh = float3(0, 0, 0);
    float3 v3_hl = float3(0, 0, 0);
    float3 v3_hh = float3(0, 0, 0);
    // Access flake texture - make sure to stay in the correct slices (no slip over)
    if (i_thetaI_low  < _FullFlakesMaxThetaI)
    {
        float lut_thetaI_low = SampleFlakesSliceLUT(i_thetaI_low);
        float lut_thetaI_low_1 = SampleFlakesSliceLUT(i_thetaI_low+1);
        if (lut_thetaI_low + i_thetaF_low < lut_thetaI_low_1)
        {
            v3_ll = SampleFlakesSlice(lut_thetaI_low, i_thetaF_low, uv, duvdx, duvdy);
        }
        if (lut_thetaI_low + i_thetaF_high < lut_thetaI_low_1)
        {
            v3_hl = SampleFlakesSlice(lut_thetaI_low, i_thetaF_high, uv, duvdx, duvdy);
        }
    }
    if (i_thetaI_high < _FullFlakesMaxThetaI)
    {
        float lut_thetaI_high = SampleFlakesSliceLUT(i_thetaI_high);
        float lut_thetaI_high_1 = SampleFlakesSliceLUT(i_thetaI_high+1);
        if (lut_thetaI_high + i_thetaF_low < lut_thetaI_high_1)
        {
            v3_lh = SampleFlakesSlice(lut_thetaI_high, i_thetaF_low, uv, duvdx, duvdy);
        }
        if (lut_thetaI_high + i_thetaF_high < lut_thetaI_high_1)
        {
            v3_hh = SampleFlakesSlice(lut_thetaI_high, i_thetaF_high, uv, duvdx, duvdy);
        }
    }

    // Bilinear interpolation
    float3 v3_l = lerp(v3_ll, v3_hl, f_thetaF_w);
    float3 v3_h = lerp(v3_lh, v3_hh, f_thetaF_w);
    return lerp(v3_l, v3_h, f_thetaI_w);
}
#endif // USE_FULL_FLAKES


//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData surfaceData)
{
    BSDFData    bsdfData;
    //  ZERO_INITIALIZE(BSDFData, data);

    bsdfData.uv = surfaceData.uv;
    bsdfData.duvdx = surfaceData.duvdx;
    bsdfData.duvdy = surfaceData.duvdy;
    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.diffuseColor = surfaceData.diffuseColor;
    bsdfData.specularColor = surfaceData.specularColor;
    bsdfData.diffuseFGD = surfaceData.diffuseFGD;
    bsdfData.ggxPerceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.ggxSmoothness);
    bsdfData.ggxFGD = surfaceData.ggxFGD;
    bsdfData.ctCoeffs = surfaceData.ctCoeffs;
    bsdfData.ctF0s = surfaceData.ctF0s;
    bsdfData.ctSpreads = surfaceData.ctSpreads;
    bsdfData.ctFGD = surfaceData.ctFGD;
    bsdfData.iblPerceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.iblSmoothness);
    bsdfData.flakesColor = surfaceData.flakesColor;
    bsdfData.flakesFGD = surfaceData.flakesFGD;
    bsdfData.clearCoatF0 = surfaceData.clearCoatF0;
    bsdfData.clearCoatF90 = surfaceData.clearCoatF90;
    bsdfData.clearCoatIOR = surfaceData.clearCoatIOR;
    bsdfData.clearCoatReflectance = surfaceData.clearCoatReflectance;
    bsdfData.occlusion = surfaceData.occlusion;
    bsdfData.geomNormalWS = surfaceData.geomNormalWS;

    ApplyDebugToBSDFData(bsdfData);
    return bsdfData;
}

//-----------------------------------------------------------------------------
// PreLightData
//
// Make sure we respect naming conventions to reuse ShaderPassForward as is,
// ie struct (even if opaque to the ShaderPassForward) name is PreLightData,
// GetPreLightData prototype.
//-----------------------------------------------------------------------------


// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
    float   NdotV_above;
    float   NdotV_below;
    float3  viewDirWS_below;

    float clearCoatViewAttenuation;

    float3 iblReflDirWS;

    // TODO find stuff to store here!

    // TODO store clear coat attenuation here

// Area lights (18 VGPRs)
// TODO: 'orthoBasisViewNormal' is just a rotation around the normal and should thus be just 1x VGPR.
    float3x3    orthoBasisViewNormal;   // Right-handed view-dependent orthogonal basis around the normal (6x VGPRs)
    float3x3    ltcTransformDiffuse;    // Inverse transformation                                         (4x VGPRs)
    float3x3    ltcTransformSpecular;   // Inverse transformation                                         (4x VGPRs)
    float3x3    ltcTransformClearCoat;
};

//
// ClampRoughness helper specific to this material
//
void ClampRoughness(inout PreLightData preLightData, inout BSDFData bsdfData, float minRoughness)
{
}

PreLightData GetPreLightData(float3 viewDirWS, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData    preLightData;
    //  ZERO_INITIALIZE(PreLightData, preLightData);

    preLightData.NdotV_above = ClampNdotV(dot(bsdfData.normalWS, viewDirWS));

    //-----------------------------------------------------------------------------
    // Handle clearcoat refraction of view ray
    if (HasClearCoat())
    {
        preLightData.viewDirWS_below = -refract(-viewDirWS, bsdfData.normalWS, 1.0 / bsdfData.clearCoatIOR);
        preLightData.NdotV_below = dot(bsdfData.normalWS, preLightData.viewDirWS_below);
    }
    else
    {
        preLightData.viewDirWS_below = viewDirWS;
        preLightData.NdotV_below = preLightData.NdotV_above;
    }
    preLightData.clearCoatViewAttenuation = 1 - bsdfData.clearCoatReflectance;

    preLightData.iblReflDirWS = reflect(-viewDirWS, bsdfData.normalWS);


//-----------------------------------------------------------------------------
// Area lights

    // UVs for sampling the LUTs
    float theta = FastACosPos(preLightData.NdotV_above); // For Area light - UVs for sampling the LUTs
    float2 uv = Remap01ToHalfTexelCoord(float2(bsdfData.ggxPerceptualRoughness, theta * INV_HALF_PI), LTC_LUT_SIZE);

    // Note we load the matrix transpose (avoid to have to transpose it in shader)
    preLightData.ltcTransformDiffuse = k_identity3x3;

    // Get the inverse LTC matrix for GGX
    // Note we load the matrix transpose (avoid to have to transpose it in shader)
    preLightData.ltcTransformSpecular      = 0.0;
    preLightData.ltcTransformSpecular._m22 = 1.0;
    //preLightData.ltcTransformSpecular._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTC_GGX_MATRIX_INDEX, 0);
    preLightData.ltcTransformSpecular._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTCLIGHTINGMODEL_GGX, 0);

    // Construct a right-handed view-dependent orthogonal basis around the normal
    preLightData.orthoBasisViewNormal = GetOrthoBasisViewNormal(viewDirWS, bsdfData.normalWS, preLightData.NdotV_above);

    preLightData.ltcTransformClearCoat = 0.0;
    /*
    {
        float2 uv = LTC_LUT_OFFSET + LTC_LUT_SCALE * float2(CLEAR_COAT_PERCEPTUAL_ROUGHNESS, theta * INV_HALF_PI);

        // Get the inverse LTC matrix for GGX
        // Note we load the matrix transpose (avoid to have to transpose it in shader)
        preLightData.ltcTransformCoat._m22 = 1.0;
        preLightData.ltcTransformCoat._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTC_GGX_MATRIX_INDEX, 0);
    }
    #endif // CLEAR_COAT
    */

    return preLightData;
}


//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

// This define allow to say that we implement a ModifyBakedDiffuseLighting function to be call in PostInitBuiltinData
#define MODIFY_BAKED_DIFFUSE_LIGHTING

void ModifyBakedDiffuseLighting(float3 viewDirWS, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, inout BuiltinData builtinData)
{
    builtinData.bakeDiffuseLighting *= bsdfData.diffuseFGD * bsdfData.diffuseColor;
}

//-----------------------------------------------------------------------------
// light transport functions
//-----------------------------------------------------------------------------
LightTransportData  GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    LightTransportData lightTransportData;

    // Ignore flakes in Meta pass...

    lightTransportData.diffuseColor = bsdfData.diffuseFGD * bsdfData.diffuseColor + bsdfData.ctFGD / bsdfData.ggxFGD * bsdfData.specularColor;
    lightTransportData.emissiveColor = float3(0.0, 0.0, 0.0);

    return lightTransportData;
}

//-----------------------------------------------------------------------------
// LightLoop related function (Only include if required)
// HAS_LIGHTLOOP is define in Lighting.hlsl
//-----------------------------------------------------------------------------

#ifdef HAS_LIGHTLOOP

//-----------------------------------------------------------------------------
// BSDF shared between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

// Same for all shading models.
bool IsNonZeroBSDF(float3 viewDirWS, float3 lightDirWS, PreLightData preLightData, BSDFData bsdfData)
{
    float NdotL = dot(bsdfData.normalWS, lightDirWS);

    return NdotL > 0;
}

float EvaluateLightBRDFModel_GGX(float3 viewDirWS, float3 lightDirWS, PreLightData preLightData, BSDFData bsdfData)
{
    float NdotL = dot(bsdfData.normalWS, lightDirWS);
    float NdotV = preLightData.NdotV_above;

    float3 H = normalize(viewDirWS + lightDirWS);
    float NdotH = dot(bsdfData.normalWS, H);
    float HdotV = dot(viewDirWS, H);

    float roughness = PerceptualRoughnessToRoughness(bsdfData.ggxPerceptualRoughness);

    float D = D_GGX(NdotH, roughness);
    float V = V_SmithJointGGX_Stable(NdotL, NdotV, roughness);
    float F = 1; // F_Schlick(surfData.GGXFresnelF0, HdotV);

    float specular = D * V * F;

    float scale = bsdfData.ctFGD / bsdfData.ggxFGD;

    return NdotL * specular * scale;
}

float EvaluateLightBRDFModel_CT(float3 viewDirWS, float3 lightDirWS, PreLightData preLightData, BSDFData bsdfData)
{
    float NdotL_above = dot(bsdfData.normalWS, lightDirWS);
    float NdotV_above = preLightData.NdotV_above;

    float3 viewDirWS_below = preLightData.viewDirWS_below;
    float3 lightDirWS_below = lightDirWS;

    float clearCoatAttenuation = 1;
    if (HasClearCoat())
    {
        lightDirWS_below = -refract(-lightDirWS, bsdfData.normalWS, 1.0/bsdfData.clearCoatIOR);
        float reflLight = F_Schlick(bsdfData.clearCoatF0, bsdfData.clearCoatF90, NdotL_above);
        clearCoatAttenuation = (1 - reflLight) * preLightData.clearCoatViewAttenuation;
    }

    float3 halfwayWS_below = normalize(viewDirWS_below + lightDirWS_below);
    float NdotL_below = dot(bsdfData.normalWS, lightDirWS_below);
    float NdotV_below = dot(bsdfData.normalWS, viewDirWS_below);
    float NdotH_below = dot(bsdfData.normalWS, halfwayWS_below);
    float HdotV_below = dot(halfwayWS_below, viewDirWS_below);

    float specular = 0;
    for (int i = 0; i < 3; ++i)
    {
        specular += bsdfData.ctCoeffs[i] * D_Beckmann(NdotH_below, bsdfData.ctSpreads[i]) * F_Schlick(bsdfData.ctF0s[i], HdotV_below);
    }
    specular *= G_CookTorrance(NdotV_below, NdotL_below, NdotH_below, HdotV_below) / (NdotL_below * NdotV_below);

    return NdotL_above * clearCoatAttenuation * specular;
}

float EvaluateLightBRDFModel_Diffuse(float3 viewDirWS, float3 lightDirWS, PreLightData preLightData, BSDFData bsdfData)
{
    float NdotL_above = dot(bsdfData.normalWS, lightDirWS);
    float NdotV_above = preLightData.NdotV_above;

    float3 viewDirWS_below = preLightData.viewDirWS_below;
    float3 lightDirWS_below = lightDirWS;

    float clearCoatAttenuation = 1;
    if (HasClearCoat())
    {
        lightDirWS_below = -refract(-lightDirWS, bsdfData.normalWS, 1.0/bsdfData.clearCoatIOR);
        float reflLight = F_Schlick(bsdfData.clearCoatF0, bsdfData.clearCoatF90, NdotL_above);
        clearCoatAttenuation = (1 - reflLight) * preLightData.clearCoatViewAttenuation;
    }

    float albedo = INV_PI * bsdfData.ctCoeffs.w;

    return NdotL_above * clearCoatAttenuation * albedo;
}

void EvaluateLightBRDFColor(float3 viewDirWS, float3 lightDirWS, PreLightData preLightData, BSDFData bsdfData, out float3 diffuseColor, out float3 specularColor)
{
    #ifdef USE_FULL_COLOR_LUT_POINT_LIGHTS
        float3 viewDirWS_below = preLightData.viewDirWS_below;
        float3 lightDirWS_below = lightDirWS;
        if (HasClearCoat())
        {
            lightDirWS_below = -refract(-lightDirWS, bsdfData.normalWS, 1.0/bsdfData.clearCoatIOR);
        }
        float3 halfwayWS_below = normalize(viewDirWS_below + lightDirWS_below);
        float NdotH_below = dot(bsdfData.normalWS, halfwayWS_below);
        float HdotV_below = dot(halfwayWS_below, viewDirWS_below);

        float3 brdfColor = SampleBRDFColorLUT(NdotH_below, HdotV_below);
        diffuseColor = brdfColor;
        specularColor = brdfColor;
    #else
        diffuseColor = bsdfData.diffuseColor;
        specularColor = bsdfData.specularColor;
    #endif // USE_FULL_COLOR_LUT_POINT_LIGHTS
}


float3 EvaluateLightFlakes_Lobe(float3 viewDirWS, float3 lightDirWS, PreLightData preLightData, BSDFData bsdfData, float specularPower)
{
    specularPower *= bsdfData.flakesFGD / bsdfData.ctFGD;

    return bsdfData.flakesColor * specularPower;
}

#ifdef USE_FULL_FLAKES
float3 EvaluateLightFlakes_BTF(float3 viewDirWS, float3 lightDirWS, PreLightData preLightData, BSDFData bsdfData)
{
    float NdotL_above = dot(bsdfData.normalWS, lightDirWS);
    float NdotV_above = preLightData.NdotV_above;

    float3 viewDirWS_below = preLightData.viewDirWS_below;
    float3 lightDirWS_below = lightDirWS;

    float clearCoatAttenuation = 1;
    if (HasClearCoat())
    {
        lightDirWS_below = -refract(-lightDirWS, bsdfData.normalWS, 1.0/bsdfData.clearCoatIOR);

        float reflLight = F_Schlick(bsdfData.clearCoatF0, bsdfData.clearCoatF90, NdotL_above);
        clearCoatAttenuation = (1 - reflLight) * preLightData.clearCoatViewAttenuation;
    }

    float3 halfwayWS_below = normalize(viewDirWS_below + lightDirWS_below);
    float NdotH_below = dot(bsdfData.normalWS, halfwayWS_below);
    float HdotV_below = dot(halfwayWS_below, viewDirWS_below);

    float3 flakes = SampleFlakesBTF(NdotH_below, HdotV_below, bsdfData.uv, bsdfData.duvdx, bsdfData.duvdy);

    return NdotL_above * clearCoatAttenuation * flakes;
}
#endif // USE_FULL_FLAKES

void EvaluateLight(float3 viewDirWS, float3 lightDirWS, PreLightData preLightData, BSDFData bsdfData, out float3 diffuseOutput, out float3 specularOutput)
{
    float3 diffuseColor;
    float3 specularColor;
    EvaluateLightBRDFColor(viewDirWS, lightDirWS, preLightData, bsdfData, diffuseColor, specularColor);

    float diffusePower = EvaluateLightBRDFModel_Diffuse(viewDirWS, lightDirWS, preLightData, bsdfData);

#ifdef USE_FULL_BRDF_MODEL
    float specularPower = EvaluateLightBRDFModel_CT(viewDirWS, lightDirWS, preLightData, bsdfData);
#else
    float specularPower = EvaluateLightBRDFModel_GGX(viewDirWS, lightDirWS, preLightData, bsdfData);
#endif // USE_FULL_BRDF_MODEL

    float3 flakesColor = float3(0,0,0);
#ifdef USE_FULL_FLAKES
    // 4D LUT, don't use specularPower here
    flakesColor = EvaluateLightFlakes_BTF(viewDirWS, lightDirWS, preLightData, bsdfData);
#endif // USE_FULL_FLAKES
#if defined(USE_SIMPLE_FLAKES) || defined(USE_SIMPLE_FLAKES_3D)
    // textured specular brdf lobe -> reuse specularPower here
    flakesColor = EvaluateLightFlakes_Lobe(viewDirWS, lightDirWS, preLightData, bsdfData, specularPower);
#endif // USE_SIMPLE_FLAKES, USE_SIMPLE_FLAKES_3D


    diffuseOutput = diffuseColor * diffusePower;
    specularOutput = specularColor * specularPower + flakesColor;
}


// This function applies the BSDF. Assumes that NdotL is positive.
CBSDF EvaluateBSDF(float3 viewDirWS_above, float3 lightDirWS_above, PreLightData preLightData, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    // TODO don't multiply diffuse/specular color here...
    EvaluateLight(viewDirWS_above, lightDirWS_above, preLightData, bsdfData, cbsdf.diffR, cbsdf.specR);

    return cbsdf;
}

//-----------------------------------------------------------------------------
// Surface shading (all light types) below
//-----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/SurfaceShading.hlsl"

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Directional(LightLoopContext lightLoopContext,
                                        float3 viewDirWS, PositionInputs posInput,
                                        PreLightData preLightData, DirectionalLightData lightData,
                                        BSDFData bsdfData, BuiltinData builtinData)
{
    return ShadeSurface_Directional(lightLoopContext, posInput, builtinData, preLightData, lightData, bsdfData, viewDirWS);
    //return (DirectLighting)0;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 viewDirWS, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData,
                                     BSDFData bsdfData, BuiltinData builtinData)
{
    return ShadeSurface_Punctual(lightLoopContext, posInput, builtinData,
                                 preLightData, lightData, bsdfData, viewDirWS);
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Line - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

DirectLighting  EvaluateBSDF_Line(  LightLoopContext lightLoopContext,
                                    float3 viewDirWS, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 positionWS = posInput.positionWS;

    float  len = lightData.size.x;
    float3 T   = lightData.right;

    float3 unL = lightData.positionRWS - positionWS;

    // Pick the major axis of the ellipsoid.
    float3 axis = lightData.right;

    // We define the ellipsoid s.t. r1 = (r + len / 2), r2 = r3 = r.
    // TODO: This could be precomputed.
    float range          = lightData.range;
    float invAspectRatio = saturate(range / (range + (0.5 * len)));

    // Compute the light attenuation.
    float intensity = EllipsoidalDistanceAttenuation(unL, axis, invAspectRatio,
                                                     lightData.rangeAttenuationScale,
                                                     lightData.rangeAttenuationBias);

    // Terminate if the shaded point is too far away.
    if (intensity != 0.0)
    {
        lightData.diffuseDimmer  *= intensity;
        lightData.specularDimmer *= intensity;

        // Translate the light s.t. the shaded point is at the origin of the coordinate system.
        lightData.positionRWS -= positionWS;

        // TODO: some of this could be precomputed.
        float3 P1 = lightData.positionRWS - T * (0.5 * len);
        float3 P2 = lightData.positionRWS + T * (0.5 * len);

        // Rotate the endpoints into the local coordinate system.
        P1 = mul(P1, transpose(preLightData.orthoBasisViewNormal));
        P2 = mul(P2, transpose(preLightData.orthoBasisViewNormal));

        // Compute the binormal in the local coordinate system.
        float3 B = normalize(cross(P1, P2));

        float ltcValue;

        // Evaluate the diffuse part
        ltcValue = LTCEvaluate(P1, P2, B, preLightData.ltcTransformDiffuse);
        ltcValue *= lightData.diffuseDimmer;
        // TODO We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().

        // See comment for specular magnitude, it apply to diffuse as well
        lighting.diffuse = bsdfData.diffuseFGD * bsdfData.diffuseColor * ltcValue;

        // Evaluate the specular part
        ltcValue = LTCEvaluate(P1, P2, B, preLightData.ltcTransformSpecular);
        ltcValue *= lightData.specularDimmer;
        // We need to multiply by the magnitude of the integral of the BRDF
        // ref: http://advances.realtimerendering.com/s2016/s2016_ltc_fresnel.pdf
        // This value is what we store in specularFGD, so reuse it
        lighting.specular = (bsdfData.ctFGD * bsdfData.specularColor + bsdfData.flakesFGD * bsdfData.flakesColor) * ltcValue;

        // Evaluate the coat part
        if (HasClearCoat())
        {
            ltcValue = LTCEvaluate(P1, P2, B, preLightData.ltcTransformClearCoat);
            ltcValue *= lightData.specularDimmer;
            // For clear coat we don't fetch specularFGD we can use directly the perfect fresnel coatIblF
            lighting.specular += bsdfData.clearCoatReflectance * ltcValue;
        }

        // Save ALU by applying 'lightData.color' only once.
        lighting.diffuse *= lightData.color;
        lighting.specular *= lightData.color;

    #ifdef DEBUG_DISPLAY
        if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
        {
            // Only lighting, not BSDF
            // Apply area light on lambert then multiply by PI to cancel Lambert
            lighting.diffuse = LTCEvaluate(P1, P2, B, k_identity3x3);
            lighting.diffuse *= PI * lightData.diffuseDimmer;
        }
    #endif
    }

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Area - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

// #define ELLIPSOIDAL_ATTENUATION

DirectLighting  EvaluateBSDF_Rect(LightLoopContext lightLoopContext,
    float3 viewWS_Clearcoat, PositionInputs posInput,
    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 positionWS = posInput.positionWS;

#if SHADEROPTIONS_BARN_DOOR
    // Apply the barn door modification to the light data
    RectangularLightApplyBarnDoor(lightData, positionWS);
#endif

    float3 unL = lightData.positionRWS - positionWS;

    if (dot(lightData.forward, unL) < FLT_EPS)
    {

        // Rotate the light direction into the light space.
        float3x3 lightToWorld = float3x3(lightData.right, lightData.up, -lightData.forward);
        unL = mul(unL, transpose(lightToWorld));

        // TODO: This could be precomputed.
        float halfWidth  = lightData.size.x * 0.5;
        float halfHeight = lightData.size.y * 0.5;

        // Define the dimensions of the attenuation volume.
        // TODO: This could be precomputed.
        float  range      = lightData.range;
        float3 invHalfDim = rcp(float3(range + halfWidth,
                                    range + halfHeight,
                                    range));

        // Compute the light attenuation.
    #ifdef ELLIPSOIDAL_ATTENUATION
        // The attenuation volume is an axis-aligned ellipsoid s.t.
        // r1 = (r + w / 2), r2 = (r + h / 2), r3 = r.
        float intensity = EllipsoidalDistanceAttenuation(unL, invHalfDim,
                                                        lightData.rangeAttenuationScale,
                                                        lightData.rangeAttenuationBias);
    #else
        // The attenuation volume is an axis-aligned box s.t.
        // hX = (r + w / 2), hY = (r + h / 2), hZ = r.
        float intensity = BoxDistanceAttenuation(unL, invHalfDim,
                                                lightData.rangeAttenuationScale,
                                                lightData.rangeAttenuationBias);
    #endif

        // Terminate if the shaded point is too far away.
        if (intensity != 0.0)
        {
            lightData.diffuseDimmer  *= intensity;
            lightData.specularDimmer *= intensity;

            // Translate the light s.t. the shaded point is at the origin of the coordinate system.
            lightData.positionRWS -= positionWS;

            float4x3 lightVerts;

            // TODO: some of this could be precomputed.
            lightVerts[0] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up * -halfHeight; // LL
            lightVerts[1] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up *  halfHeight; // UL
            lightVerts[2] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up *  halfHeight; // UR
            lightVerts[3] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up * -halfHeight; // LR

            // Rotate the endpoints into the local coordinate system.
            lightVerts = mul(lightVerts, transpose(preLightData.orthoBasisViewNormal));

            float3 ltcValue;

            // Evaluate the diffuse part
            // Polygon irradiance in the transformed configuration.
            float4x3 LD = mul(lightVerts, preLightData.ltcTransformDiffuse);
            ltcValue  = PolygonIrradiance(LD);
            ltcValue *= lightData.diffuseDimmer;

            // Only apply cookie if there is one
            if ( lightData.cookieMode != COOKIEMODE_NONE )
            {
                // Compute the cookie data for the diffuse term
                float3 formFactorD =  PolygonFormFactor(LD);
                ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LD, formFactorD);
            }

            // TODO We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
            // See comment for specular magnitude, it apply to diffuse as well
            lighting.diffuse = bsdfData.diffuseFGD * bsdfData.diffuseColor * ltcValue;

            // Evaluate the specular part
            // Polygon irradiance in the transformed configuration.
            float4x3 LS = mul(lightVerts, preLightData.ltcTransformSpecular);
            ltcValue  = PolygonIrradiance(LS);
            ltcValue *= lightData.specularDimmer;

            // Only apply cookie if there is one
            if (lightData.cookieMode != COOKIEMODE_NONE)
            {
                // Compute the cookie data for the specular term
                float3 formFactorS =  PolygonFormFactor(LS);
                ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LS, formFactorS);
            }

            // We need to multiply by the magnitude of the integral of the BRDF
            // ref: http://advances.realtimerendering.com/s2016/s2016_ltc_fresnel.pdf
            // This value is what we store in specularFGD, so reuse it
            lighting.specular += bsdfData.ctFGD * bsdfData.specularColor * ltcValue;

            // Evaluate the coat part
            if (HasClearCoat())
            {
                float4x3 LSCC = mul(lightVerts, preLightData.ltcTransformClearCoat);
                ltcValue = PolygonIrradiance(LSCC);
                ltcValue *= lightData.specularDimmer;
                // Only apply cookie if there is one
                if ( lightData.cookieMode != COOKIEMODE_NONE )
                {
                    // Compute the cookie data for the specular term
                    float3 formFactorS =  PolygonFormFactor(LSCC);
                    ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LSCC, formFactorS);
                }
                // For clear coat we don't fetch specularFGD we can use directly the perfect fresnel coatIblF
                lighting.specular += bsdfData.clearCoatReflectance * ltcValue;
            }

            // Save ALU by applying 'lightData.color' only once.
            lighting.diffuse *= lightData.color;
            lighting.specular *= lightData.color;

        #ifdef DEBUG_DISPLAY
            if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
            {
                // Only lighting, not BSDF
                // Apply area light on lambert then multiply by PI to cancel Lambert
                lighting.diffuse = PolygonIrradiance(mul(lightVerts, k_identity3x3));
                lighting.diffuse *= PI * lightData.diffuseDimmer;
            }
        #endif
        }

    }

    float  shadow = 1.0;
    float  shadowMask = 1.0;
#ifdef SHADOWS_SHADOWMASK
    // shadowMaskSelector.x is -1 if there is no shadow mask
    // Note that we override shadow value (in case we don't have any dynamic shadow)
    shadow = shadowMask = (lightData.shadowMaskSelector.x >= 0.0) ? dot(BUILTIN_DATA_SHADOW_MASK, lightData.shadowMaskSelector) : 1.0;
#endif

#if defined(SCREEN_SPACE_SHADOWS) && !defined(_SURFACE_TYPE_TRANSPARENT)
    if ((lightData.screenSpaceShadowIndex & SCREEN_SPACE_SHADOW_INDEX_MASK) != INVALID_SCREEN_SPACE_SHADOW)
    {
        shadow = GetScreenSpaceShadow(posInput, lightData.screenSpaceShadowIndex);
    }
    else
#endif // ENABLE_RAYTRACING
    if (lightData.shadowIndex != -1)
    {
#if RASTERIZED_AREA_LIGHT_SHADOWS
            // lightData.positionRWS now contains the Light vector.
            shadow = GetAreaLightAttenuation(lightLoopContext.shadowContext, posInput.positionSS, posInput.positionWS, bsdfData.normalWS, lightData.shadowIndex, normalize(lightData.positionRWS), length(lightData.positionRWS));
#ifdef SHADOWS_SHADOWMASK
            // See comment for punctual light shadow mask
            shadow = lightData.nonLightMappedOnly ? min(shadowMask, shadow) : shadow;
#endif
            shadow = lerp(shadowMask, shadow, lightData.shadowDimmer);
#endif
    }

#if RASTERIZED_AREA_LIGHT_SHADOWS || SUPPORTS_RAYTRACED_AREA_SHADOWS
    float3 shadowColor = ComputeShadowColor(shadow, lightData.shadowTint, lightData.penumbraTint);
    lighting.diffuse *= shadowColor;
    lighting.specular *= shadowColor;
#endif


    return lighting;
}

DirectLighting  EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 viewWS, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BuiltinData builtinData)
{

    if (lightData.lightType == GPULIGHTTYPE_TUBE)
    {
        return EvaluateBSDF_Line(lightLoopContext, viewWS, posInput, preLightData, lightData, bsdfData, builtinData);
    }
    else
    {
        return EvaluateBSDF_Rect(lightLoopContext, viewWS, posInput, preLightData, lightData, bsdfData, builtinData);
    }
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_SSLighting for screen space lighting
// ----------------------------------------------------------------------------

IndirectLighting EvaluateBSDF_ScreenSpaceReflection(PositionInputs posInput,
                                                    PreLightData   preLightData,
                                                    BSDFData       bsdfData,
                                                    inout float    reflectionHierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    // TODO: this texture is sparse (mostly black). Can we avoid reading every texel? How about using Hi-S?
    float4 ssrLighting = LOAD_TEXTURE2D_X(_SsrLightingTexture, posInput.positionSS);
    InversePreExposeSsrLighting(ssrLighting);

    // Apply the weight on the ssr contribution (if required)
    ApplyScreenSpaceReflectionWeight(ssrLighting);

    float3 reflectanceFactor = 0.0;

    if (HasClearCoat())
    {
        reflectanceFactor = bsdfData.clearCoatReflectance;
    }
    else
    {
        reflectanceFactor = bsdfData.ctFGD * bsdfData.specularColor;
    }

    lighting.specularReflected = ssrLighting.rgb * reflectanceFactor;
    reflectionHierarchyWeight  = ssrLighting.a;

    return lighting;
}

IndirectLighting    EvaluateBSDF_ScreenspaceRefraction( LightLoopContext lightLoopContext,
                                                        float3 viewWS_Clearcoat, PositionInputs posInput,
                                                        PreLightData preLightData, BSDFData bsdfData,
                                                        EnvLightData _envLightData,
                                                        inout float hierarchyWeight)
{

    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    return lighting;
}


//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
IndirectLighting EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                                    float3 viewDirWS_above, PositionInputs posInput,
                                    PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                    int influenceShapeType, int GPUImageBasedLightingType,
                                    inout float hierarchyWeight)
{

    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    if (GPUImageBasedLightingType != GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
        return lighting;    // We don't support transmission

    float3  envLighting;
    float3  positionWS = posInput.positionWS;
    float   weight = 1.0;

    // TODO_dir: this shouldn't be undercoat.
    float3  iblReflDirWS = preLightData.iblReflDirWS;

    // Note: using influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)
    float intersectionDistance = EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, influenceShapeType, iblReflDirWS, weight);

    // Sample the pre-integrated environment lighting
    float4 preLD = SampleEnvWithDistanceBaseRoughness(lightLoopContext, posInput, lightData, iblReflDirWS, bsdfData.iblPerceptualRoughness, intersectionDistance);
    weight *= preLD.a; // Used by planar reflection to discard pixel

    envLighting = (bsdfData.ctFGD * bsdfData.specularColor + bsdfData.flakesFGD * bsdfData.flakesColor) * preLD.xyz;

    //-----------------------------------------------------------------------------
    // Evaluate the clearcoat component if needed
    if (HasClearCoat())
    {
        // Evaluate clearcoat sampling direction
        float   unusedWeight = 0.0;
        float3  iblReflDirWS = preLightData.iblReflDirWS;
        EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, influenceShapeType, iblReflDirWS, unusedWeight);

        float4  preLD = SampleEnv(lightLoopContext, lightData.envIndex, iblReflDirWS, 0.0, lightData.rangeCompressionFactorCompensation, posInput.positionNDC);
        envLighting += bsdfData.clearCoatReflectance * preLD.xyz;
    }

    UpdateLightingHierarchyWeights(hierarchyWeight, weight);
    envLighting *= weight * lightData.multiplier;

    lighting.specularReflected = envLighting;

    return lighting;
}

//-----------------------------------------------------------------------------
// PostEvaluateBSDF
// ----------------------------------------------------------------------------

void PostEvaluateBSDF(  LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput,
                        PreLightData preLightData, BSDFData bsdfData, BuiltinData builtinData, AggregateLighting lighting,
                        out LightLoopOutput lightLoopOutput)
{
    AmbientOcclusionFactor aoFactor;
    
    // There is no AmbientOcclusion from data with AxF, but let's apply our SSAO
    GetScreenSpaceAmbientOcclusionMultibounce(  posInput.positionSS, preLightData.NdotV_above,
                                                bsdfData.ggxPerceptualRoughness,
                                                1.0, 1.0, bsdfData.diffuseColor, bsdfData.specularColor, aoFactor);
    ApplyAmbientOcclusionFactor(aoFactor, builtinData, lighting);
    

    lightLoopOutput.diffuseLighting = lighting.direct.diffuse + builtinData.bakeDiffuseLighting + builtinData.emissiveColor;
    lightLoopOutput.specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, lightLoopOutput);
#endif
}

#endif // #ifdef HAS_LIGHTLOOP

#endif // AXFCPA2HDRP_HLSL
