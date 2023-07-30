// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using UnityEngine;
using UnityEngine.Rendering;

namespace AxF2Unity
{
    public static class CommandBufferExt
    {
        public static void SetGPUVectorParam(this CommandBuffer commandBuffer, ComputeShader computeShader, int kernelIndex, string name, GPUVector vector)
        {
            commandBuffer.SetComputeBufferParam(computeShader, kernelIndex, name, vector.buffer);
            commandBuffer.SetComputeIntParam(computeShader, name + "Start", vector.start);
            commandBuffer.SetComputeIntParam(computeShader, name + "Stride", vector.stride);
            commandBuffer.SetComputeIntParam(computeShader, name + "Count", vector.count);
        }

        public static void SetGPUMatrixParam(this CommandBuffer commandBuffer, ComputeShader computeShader, int kernelIndex, string name, GPUMatrix matrix)
        {
            commandBuffer.SetComputeBufferParam(computeShader, kernelIndex, name, matrix.buffer);
            commandBuffer.SetComputeIntParam(computeShader, name + "Start", matrix.start);
            commandBuffer.SetComputeIntParam(computeShader, name + "RowCount", matrix.rows);
            commandBuffer.SetComputeIntParam(computeShader, name + "ColCount", matrix.cols);
            commandBuffer.SetComputeIntParam(computeShader, name + "RowStride", matrix.rowStride);
            commandBuffer.SetComputeIntParam(computeShader, name + "ColStride", matrix.colStride);
        }

    }
}
