// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

#ifndef UNITY_MISSING_DEFINITIONS_CGINC
#define UNITY_MISSING_DEFINITIONS_CGINC

// #if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL)
#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (defined(SHADER_TARGET_SURFACE_ANALYSIS) && !defined(SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER) && UNITY_VERSION >= 201800)

    // Declare missing Unity macros:
    #define UNITY_ARGS_TEX2D(tex) Texture2D tex, SamplerState sampler##tex
    #define UNITY_PASS_TEX2D(tex) tex, sampler##tex

    #define UNITY_SAMPLE_TEX2D_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord,lod)
    #define UNITY_SAMPLE_TEX2D_BIAS(tex,coord,bias) tex.SampleBias (sampler##tex,coord,bias)

    #define UNITY_SAMPLE_TEX2D_GRAD(tex,coord,dx,dy) tex.SampleGrad (sampler##tex,coord,dx,dy)
    #define UNITY_SAMPLE_TEX2DARRAY_GRAD(tex,coord,dx,dy) tex.SampleGrad (sampler##tex,coord,dx,dy)

#else

    #define UNITY_ARGS_TEX2D(tex) sampler2D tex
    #define UNITY_PASS_TEX2D(tex) tex

    #define UNITY_SAMPLE_TEX2D_LOD(tex,coord,lod) tex2Dlod (tex,float4(coord.x,coord.y,0,lod))
    #define UNITY_SAMPLE_TEX2D_BIAS(tex,coord,bias) tex2Dbias (tex,coord)

    #define UNITY_SAMPLE_TEX2D_GRAD(tex,coord,dx,dy) textureGrad(tex,coord,dx,dy)
    #define UNITY_SAMPLE_TEX2DARRAY_GRAD(tex,coord,dx,dy) textureGrad(tex,coord,dx,dy)


    // Cube arrays
    #define UNITY_DECLARE_TEXCUBEARRAY(tex) float tex // samplerCUBEARRAY tex
    #define UNITY_ARGS_TEXCUBEARRAY(tex) float tex // samplerCUBEARRAY tex
    #define UNITY_PASS_TEXCUBEARRAY(tex) tex // tex

    #define UNITY_SAMPLE_TEXCUBEARRAY(tex,coord) float4(tex*coord) // texCUBEARRAY (tex, coord)
    #define UNITY_SAMPLE_TEXCUBEARRAY_LOD(tex,coord,lod) float4(tex*coord*lod) // texCUBEARRAYlod (tex, coord, lod)


#endif // SHADER_API_*


#define Sq(x) ((x)*(x))

// Ref: "Efficient Evaluation of Irradiance Environment Maps" from ShaderX 2
float3 SHEvalLinearL0L1(float3 N, float4 shAr, float4 shAg, float4 shAb)
{
    float4 vA = float4(N, 1.0);

    float3 x1;
    // Linear (L1) + constant (L0) polynomial terms
    x1.r = dot(shAr, vA);
    x1.g = dot(shAg, vA);
    x1.b = dot(shAb, vA);

    return x1;
}

float3 SHEvalLinearL2(float3 N, float4 shBr, float4 shBg, float4 shBb, float4 shC)
{
    float3 x2;
    // 4 of the quadratic (L2) polynomials
    float4 vB = N.xyzz * N.yzzx;
    vB.z = 3*vB.z - 1;
    x2.r = dot(shBr, vB);
    x2.g = dot(shBg, vB);
    x2.b = dot(shBb, vB);

    // Final (5th) quadratic (L2) polynomial
    float vC = N.x * N.x - N.y * N.y;
    float3 x3 = shC.rgb * vC;

    return x2 + x3;
}

float3 SampleSH9(float4 SHCoefficients[7], float3 N)
{
    float4 shAr = SHCoefficients[0];
    float4 shAg = SHCoefficients[1];
    float4 shAb = SHCoefficients[2];
    float4 shBr = SHCoefficients[3];
    float4 shBg = SHCoefficients[4];
    float4 shBb = SHCoefficients[5];
    float4 shCr = SHCoefficients[6];

    // Linear + constant polynomial terms
    float3 res = SHEvalLinearL0L1(N, shAr, shAg, shAb);

    // Quadratic polynomials
    res += SHEvalLinearL2(N, shBr, shBg, shBb, shCr);

    return res;
}

#endif // UNITY_MISSING_DEFINITIONS_CGINC
