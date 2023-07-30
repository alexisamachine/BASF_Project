// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

#ifndef AXFCPA2LIGHTING_CGINC
#define AXFCPA2LIGHTING_CGINC

#include "AxFCPA2Surface.cginc"


float G_CookTorrance(float NdotV, float NdotL, float NdotH, float HdotV)
{
    return min(min( (2.0 * NdotH * NdotV / HdotV), (2.0 * NdotH * NdotL / HdotV) ), 1);
}

float D_BeckmannTimesPI(float NoH, float roughness)
{
    // Just don't!
    //if (roughness == 0)
    //    return 0;

    float NoH_sqr = Sq(NoH);
    float m_sqr = Sq(roughness);
    float e = (NoH_sqr - 1) / (NoH_sqr*m_sqr);
    return exp(e) / (m_sqr * Sq(NoH_sqr));
}

float EvaluateLightBRDFModel_GGX(SurfaceInput surfInput, SurfaceData surfData, UnityLight light)
{
    float NdotL = dot(surfData.NormalWS, light.dir);
    if (NdotL < 1e-6)
        return 0;
    float NdotV = saturate(dot(surfData.NormalWS, surfInput.viewDirWS));

    float3 H = normalize(surfInput.viewDirWS + light.dir);
    float NdotH = dot(surfData.NormalWS, H);
    float HdotV = dot(surfInput.viewDirWS, H);

    float roughness = SmoothnessToRoughness(surfData.GGXSmoothness);

    float D = GGXTerm(NdotH, roughness);
    float V = SmithJointGGXVisibilityTerm(NdotL, NdotV, roughness);
    float F = 1; // FresnelTerm(surfData.GGXFresnelF0, HdotV);

    float specular = D * V * F;

    float scale = surfData.CTFGD / surfData.GGXFGD;

    // multiply everything by PI!
    return NdotL * specular * scale * UNITY_PI;
}

float EvaluateLightBRDFModel_CT(SurfaceInput surfInput, SurfaceData surfData, UnityLight light)
{
    float NdotL_above = dot(surfData.NormalWS, light.dir);
    if (NdotL_above < 1e-6)
        return 0;
    float NdotV_above = dot(surfData.NormalWS, surfInput.viewDirWS);


    float3 viewDirBelow = surfInput.viewDirWS;
    float3 lightDirBelow = light.dir;

    float atten = 1;
#ifdef CLEAR_COAT
    viewDirBelow = -refract(-viewDirBelow, surfData.NormalWS, 1.0/surfData.ClearCoatIOR);
    lightDirBelow = -refract(-lightDirBelow, surfData.NormalWS, 1.0/surfData.ClearCoatIOR);

    float reflLight = FresnelLerp(surfData.ClearCoatF0.xxx, surfData.ClearCoatF90.xxx, NdotL_above).x;
    float reflView = FresnelLerp(surfData.ClearCoatF0.xxx, surfData.ClearCoatF90.xxx, NdotV_above).x;
    atten = (1 - reflLight) * (1 - reflView);
#endif // CLEAR_COAT

    float3 halfwayBelow = normalize(viewDirBelow + lightDirBelow);
    float NdotL_below = dot(surfData.NormalWS, lightDirBelow);
    float NdotV_below = dot(surfData.NormalWS, viewDirBelow);
    float NdotH_below = dot(surfData.NormalWS, halfwayBelow);
    float HdotV_below = dot(halfwayBelow, viewDirBelow);

    float specular = 0;
    for (int i = 0; i < 3; ++i)
    {
        specular += surfData.CTCoeffs[i] * D_BeckmannTimesPI(NdotH_below, surfData.CTSpreads[i]) * FresnelTerm(surfData.CTF0s[i].xxx, HdotV_below).x;
    }
    specular *= G_CookTorrance(NdotV_below, NdotL_below, NdotH_below, HdotV_below) / (NdotL_below * NdotV_below);

    // * UNITY_PI handled in D_BeckmannTimesPI
    return NdotL_above * atten * specular;
}

float EvaluateLightBRDFModel_Diffuse(SurfaceInput surfInput, SurfaceData surfData, UnityLight light)
{
    float NdotL_above = dot(surfData.NormalWS, light.dir);
    if (NdotL_above < 1e-6)
        return 0;
    float NdotV_above = dot(surfData.NormalWS, surfInput.viewDirWS);

    float atten = 1;
#ifdef CLEAR_COAT
    float reflLight = FresnelLerp(surfData.ClearCoatF0.xxx, surfData.ClearCoatF90.xxx, NdotL_above).x;
    float reflView = FresnelLerp(surfData.ClearCoatF0.xxx, surfData.ClearCoatF90.xxx, NdotV_above).x;
    atten = (1 - reflLight) * (1 - reflView);
#endif // CLEAR_COAT

    float albedo = surfData.CTCoeffs.w; // / PI * NdotL_above

    return NdotL_above * atten * albedo;
}

void EvaluateLightBRDFColor(SurfaceInput surfInput, SurfaceData surfData, UnityLight light, out float3 diffuseColor, out float3 specularColor)
{
    #ifdef USE_FULL_COLOR_LUT_POINT_LIGHTS
        float3 viewDirBelow = surfInput.viewDirWS;
        float3 lightDirBelow = light.dir;
    #ifdef CLEAR_COAT
        viewDirBelow = -refract(-viewDirBelow, surfData.NormalWS, 1.0/surfData.ClearCoatIOR);
        lightDirBelow = -refract(-lightDirBelow, surfData.NormalWS, 1.0/surfData.ClearCoatIOR);
    #endif // CLEAR_COAT
        float3 halfwayBelow = normalize(viewDirBelow + lightDirBelow);
        float NdotH_below = dot(surfData.NormalWS, halfwayBelow);
        float HdotV_below = dot(halfwayBelow, viewDirBelow);

        float3 brdfColor = SampleBRDFColorLUT(NdotH_below, HdotV_below);
        diffuseColor = brdfColor;
        specularColor = brdfColor;
    #else
        diffuseColor = surfData.DiffuseColor;
        specularColor = surfData.SpecularColor;
    #endif // USE_FULL_COLOR_LUT_POINT_LIGHTS
}


float3 EvaluateLightFlakes_Lobe(SurfaceInput surfInput, SurfaceData surfData, float specularPower, UnityLight light)
{
#ifdef USE_FLAKES_SMOOTHNESS
    SurfaceData tempSurfData = surfData;
    tempSurfData.GGXSmoothness = surfData.FlakesSmoothness;
    // tempSurfData.GGXFGD = TODO!!
    tempSurfData.CTFGD = surfData.FlakesFGD;
    specularPower = EvaluateLightBRDFModel_GGX(surfInput, tempSurfData, light);
#else
    specularPower *= surfData.FlakesFGD / surfData.CTFGD;
#endif

    return surfData.FlakesColor * specularPower;
}

#ifdef USE_FULL_FLAKES
float3 EvaluateLightFlakes_BTF(SurfaceInput surfInput, SurfaceData surfData, UnityLight light)
{
    float NdotL_above = dot(surfData.NormalWS, light.dir);
    if (NdotL_above < 1e-6)
        return float3(0, 0, 0);
    float NdotV_above = dot(surfData.NormalWS, surfInput.viewDirWS);

    float3 viewDirBelow = surfInput.viewDirWS;
    float3 lightDirBelow = light.dir;

    float atten = 1;
#ifdef CLEAR_COAT
    viewDirBelow = -refract(-viewDirBelow, surfData.NormalWS, 1.0/surfData.ClearCoatIOR);
    lightDirBelow = -refract(-lightDirBelow, surfData.NormalWS, 1.0/surfData.ClearCoatIOR);

    float reflLight = FresnelLerp(surfData.ClearCoatF0.xxx, surfData.ClearCoatF90.xxx, NdotL_above).x;
    float reflView = FresnelLerp(surfData.ClearCoatF0.xxx, surfData.ClearCoatF90.xxx, NdotV_above).x;
    atten = (1 - reflLight) * (1 - reflView);
#endif // CLEAR_COAT

    float3 halfwayBelow = normalize(viewDirBelow + lightDirBelow);
    float NdotH_below = dot(surfData.NormalWS, halfwayBelow);
    float HdotV_below = dot(halfwayBelow, viewDirBelow);

    float3 flakes = SampleFlakesBTF(NdotH_below, HdotV_below, surfData.uv, surfData.duvdx, surfData.duvdy);

    return NdotL_above * atten * flakes * UNITY_PI;
}
#endif // USE_FULL_FLAKES

float4 EvaluateLight(SurfaceInput surfInput, SurfaceData surfData, UnityLight light)
{
    float3 diffuseColor;
    float3 specularColor;
    EvaluateLightBRDFColor(surfInput, surfData, light, diffuseColor, specularColor);

    float diffusePower = EvaluateLightBRDFModel_Diffuse(surfInput, surfData, light);

#ifdef USE_FULL_BRDF_MODEL
    float specularPower = EvaluateLightBRDFModel_CT(surfInput, surfData, light);
#else
    float specularPower = EvaluateLightBRDFModel_GGX(surfInput, surfData, light);
#endif // USE_FULL_BRDF_MODEL

    float3 flakesColor = float3(0,0,0);
#ifdef USE_FULL_FLAKES
    // 4D LUT, don't use specularPower here
    flakesColor = EvaluateLightFlakes_BTF(surfInput, surfData, light);
#endif // USE_FULL_FLAKES
#if defined(USE_SIMPLE_FLAKES) || defined(USE_SIMPLE_FLAKES_3D)
    // textured specular brdf lobe -> reuse specularPower here
    flakesColor = EvaluateLightFlakes_Lobe(surfInput, surfData, specularPower, light);
#endif // USE_SIMPLE_FLAKES, USE_SIMPLE_FLAKES_3D

    float4 output = float4(0, 0, 0, 1);

    output.rgb += diffuseColor * diffusePower;
    output.rgb += specularColor * specularPower;
    output.rgb += flakesColor;

    output.rgb *= light.color;
    return output;
}

UnityGI EvaluateLight_GI(SurfaceInput surfInput, inout SurfaceData surfData, UnityGIInput giInput)
{
    UnityGI gi;
    // light atten. + indirect.diffuse
    gi = UnityGI_Base(giInput, surfData.Occlusion, surfData.NormalWS);

    Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(surfData.IBLSmoothness, surfInput.viewDirWS, surfData.NormalWS, 0.42 /* no effect here */);
    // g.reflUVW   = lerp(reflect(-surfInput.viewDirWS, surfData.NormalWS), iblR, 1);

    gi.indirect.specular = UnityGI_IndirectSpecular(giInput, surfData.Occlusion, g);


    // apply prefiltered BRDF color!
    gi.indirect.specular *= surfData.CTFGD * surfData.SpecularColor + surfData.FlakesFGD * surfData.FlakesColor;
    gi.indirect.diffuse *= surfData.DiffuseFGD * surfData.DiffuseColor;


//#ifdef USE_FLAKES_SMOOTHNESS
//    Unity_GlossyEnvironmentData flakes_g = UnityGlossyEnvironmentSetup(surfData.FlakesSmoothness, surfInput.viewDirWS, surfData.NormalWS, 0.42 /* no effect here */);
//    /// flakes_g.roughness /* perceptualRoughness */ = lerp(SmoothnessToPerceptualRoughness(surfData.FlakesSmoothness), RoughnessToPerceptualRoughness(iblRoughness), 1);
//    /// flakes_g.reflUVW   = lerp(reflect(-surfInput.viewDirWS, surfData.NormalWS), iblR, 1);
//
//    gi.indirect.specular += surfData.FlakesColor * UnityGI_IndirectSpecular(giInput, surfData.Occlusion, flakes_g);
//#endif // USE_FLAKES_SMOOTHNESS


#ifdef CLEAR_COAT
    // NOTE: no environment map in deferred shading!
    Unity_GlossyEnvironmentData cc_g = UnityGlossyEnvironmentSetup(1 /* smoothness */, surfInput.viewDirWS, surfData.NormalWS, 0.04 /* no effect here */);
    gi.indirect.specular += surfData.ClearCoatReflectance * UnityGI_IndirectSpecular(giInput, surfData.Occlusion, cc_g);
#endif // CLEAR_COAT

    return gi;
}

#endif // AXFCPA2LIGHTING_CGINC
