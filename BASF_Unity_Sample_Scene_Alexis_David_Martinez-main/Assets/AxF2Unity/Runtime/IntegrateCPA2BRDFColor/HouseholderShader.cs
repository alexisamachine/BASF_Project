// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using UnityEngine;
using UnityEngine.Rendering;

namespace AxF2Unity
{
    public class HouseholderShader : Singleton<HouseholderShader>
    {
        private ComputeShader householderShader;
        private int householderComputeAlphaAndRKernelIdx;
        private int householderMatMulKernelIdx;


        public HouseholderShader()
        {
            householderShader = (ComputeShader)Resources.Load("Shaders/Householder");
            householderComputeAlphaAndRKernelIdx = householderShader.FindKernel("CSHouseholderComputeAlphaAndR");
            householderMatMulKernelIdx = householderShader.FindKernel("CSHouseholderMatMul");
        }

        public void ComputeAlphaAndR(CommandBuffer cmd, GPUMatrix A, GPUVector R, GPUVector Alpha)
        {
            Debug.Assert(A.rows == R.count);

            cmd.SetGPUMatrixParam(householderShader, householderComputeAlphaAndRKernelIdx, "_InputMatrix", A);
            cmd.SetGPUVectorParam(householderShader, householderComputeAlphaAndRKernelIdx, "_R", R);
            cmd.SetGPUVectorParam(householderShader, householderComputeAlphaAndRKernelIdx, "_Alpha", Alpha);

            cmd.DispatchCompute(householderShader, householderComputeAlphaAndRKernelIdx, 1, 1, 1);
        }

        public void MultiplyHouseholder(CommandBuffer cmd, GPUVector R, GPUMatrix In, GPUMatrix Out)
        {
            Debug.Assert(In.rows == Out.rows);
            Debug.Assert(In.cols == Out.cols);
            Debug.Assert(In.rows == R.count);

            cmd.SetGPUMatrixParam(householderShader, householderMatMulKernelIdx, "_InputMatrix", In);
            cmd.SetGPUMatrixParam(householderShader, householderMatMulKernelIdx, "_OutputMatrix", Out);
            cmd.SetGPUVectorParam(householderShader, householderMatMulKernelIdx, "_R", R);

            cmd.DispatchCompute(householderShader, householderMatMulKernelIdx, In.cols, MathUtil.CeilDiv(In.rows, 32), 1);
        }


    }
}
