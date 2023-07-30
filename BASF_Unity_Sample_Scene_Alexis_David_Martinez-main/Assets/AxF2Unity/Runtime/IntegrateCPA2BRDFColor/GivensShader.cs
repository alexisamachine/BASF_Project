// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using UnityEngine;
using UnityEngine.Rendering;

namespace AxF2Unity
{
    public class GivensShader : Singleton<GivensShader>
    {
        private ComputeShader shader;
        private int rotateMatFromLeftKernelIdx;

        public GivensShader()
        {
            shader = (ComputeShader)Resources.Load("Shaders/Givens");
            rotateMatFromLeftKernelIdx = shader.FindKernel("CSRotateMatFromLeft");
        }

        public void RotateMatFromLeft(CommandBuffer cmd, GPUMatrix Mat, float cosTheta, float sinTheta)
        {
            Debug.Assert(Mat.rows == 2);

            cmd.SetComputeFloatParam(shader, "_CosTheta", cosTheta);
            cmd.SetComputeFloatParam(shader, "_SinTheta", sinTheta);
            cmd.SetGPUMatrixParam(shader, rotateMatFromLeftKernelIdx, "_Matrix", Mat);

            cmd.DispatchCompute(shader, rotateMatFromLeftKernelIdx, 1, MathUtil.CeilDiv(Mat.cols, 16), 1);
        }

    }
}
