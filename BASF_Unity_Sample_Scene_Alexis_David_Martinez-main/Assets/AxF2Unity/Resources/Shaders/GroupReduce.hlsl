#ifndef GROUPREDUCE_HLSL
#define GROUPREDUCE_HLSL

groupshared float sharedData[32];

float GroupReduce(float value, int tid)
{
    sharedData[tid] = value;

    // Final reduce in shared memory
    // barrier();
    AllMemoryBarrierWithGroupSync();

    if (tid < 16)
        sharedData[tid] += sharedData[tid + 16];
    AllMemoryBarrierWithGroupSync();

    if (tid < 8)
        sharedData[tid] += sharedData[tid + 8];
    AllMemoryBarrierWithGroupSync();

    if (tid < 4)
        sharedData[tid] += sharedData[tid + 4];
    AllMemoryBarrierWithGroupSync();

    if (tid < 2)
        sharedData[tid] += sharedData[tid + 2];
    AllMemoryBarrierWithGroupSync();

    if (tid < 1)
        sharedData[tid] += sharedData[tid + 1];
    AllMemoryBarrierWithGroupSync();

    return sharedData[0];
}

#endif // GROUPREDUCE_HLSL
