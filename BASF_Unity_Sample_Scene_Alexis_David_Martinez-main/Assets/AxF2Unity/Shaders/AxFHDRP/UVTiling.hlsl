// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung
#ifndef UV_TILING_HLSL
#define UV_TILING_HLSL

#define SQRT_2      1.41421356237309504880
#define SQRT_3      1.73205080756887729353
#define SQRT_5      2.23606797749978969641
#define SQRT_7      2.64575131106459059050
#define SQRT_11     3.31662479035539984911
#define SQRT_13     3.60555127546398929312

// NOTE: Uniform values and properties declared in AxF2Lit*Properties.hlsl

float3 ComputeRandomValue(int2 xy)
{
    float3x2 coeffs = float3x2(
        SQRT_2, SQRT_13,
        SQRT_11, SQRT_3,
        SQRT_5, SQRT_7
    );

    return frac(sin(mul(coeffs, float2(xy))) * 43758.5453);
}

void TransformationAtGridPoint(int2 xy, out float2 translation, out float2x2 rotation)
{
    float3 value = ComputeRandomValue(xy);

    translation = value.xy;

    float theta = value.z * TWO_PI;
    float sinTheta, cosTheta;
    sincos(theta, sinTheta, cosTheta);
    rotation = float2x2(cosTheta, -sinTheta,
                        sinTheta,  cosTheta);
}

void ApplyUVTiling(inout float2 uv, inout float2 duvdx, inout float2 duvdy)
{
#if defined(USE_RANDOMIZED_UV_TILING)

    float2 translation;
    float2x2 rotation;
    TransformationAtGridPoint(int2(uv * _UVTilingGridSize), translation, rotation);

    float2 frac_uv = frac(uv * _UVTilingGridSize);
    uv = translation + mul(rotation, frac_uv / _UVTilingGridSize);

    duvdx = mul(rotation, duvdx);
    duvdy = mul(rotation, duvdy);

#elif defined(USE_UV_MIRRORING)

    // Hide tiles by mirroring
    if (0 == (uint(uv[1]) % 2))
    {
        uv[0] += 0.5f;
    }
    else if (0 == (uint(uv[0]) % 3))
    {
        uv[1] = -uv[1];
    }
    else
    {
        uv[0] = -uv[0];
    }

#endif // USE_RANDOMIZED_UV_TILING, USE_UV_MIRRORING
}

#endif // UV_TILING_HLSL
