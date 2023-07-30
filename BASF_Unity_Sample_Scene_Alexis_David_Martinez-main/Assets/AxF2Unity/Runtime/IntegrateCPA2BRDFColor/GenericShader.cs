// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using UnityEngine;
using UnityEngine.Rendering;

namespace AxF2Unity
{
    public class GenericShader : Singleton<GenericShader>
    {
        private ComputeShader shader;
        private int matrixEyeKernelIdx;
        private int matrixBidiagKernelIdx;
        private int matrixCopyKernelIdx;
        private int matrixMulKernelIdx;
        private int matrixRowMeanKernelIdx;
        private int matrixSubtractInplaceKernelIdx;
        private int matrixErrorKernelIdx;

        public GenericShader()
        {
            shader = (ComputeShader)Resources.Load("Shaders/Generic");
            matrixEyeKernelIdx = shader.FindKernel("CSMatrixEye");
            matrixBidiagKernelIdx = shader.FindKernel("CSMatrixBidiag");
            matrixCopyKernelIdx = shader.FindKernel("CSMatrixCopy");
            matrixMulKernelIdx = shader.FindKernel("CSMatrixMul");
            matrixRowMeanKernelIdx = shader.FindKernel("CSMatrixRowMean");
            matrixSubtractInplaceKernelIdx = shader.FindKernel("CSMatrixSubtractInplace");
            matrixErrorKernelIdx = shader.FindKernel("CSMatrixError");
        }

        public void SetMatrixEye(CommandBuffer cmd, GPUMatrix Out)
        {
            cmd.SetGPUMatrixParam(shader, matrixEyeKernelIdx, "_OutputMatrix", Out);

            cmd.DispatchCompute(shader, matrixEyeKernelIdx, MathUtil.CeilDiv(Out.cols, 8), MathUtil.CeilDiv(Out.rows, 8), 1);
        }

        public void SetMatrixBidiag(CommandBuffer cmd, GPUVector Diag, GPUVector UpperDiag, GPUMatrix Out)
        {
            Debug.Assert(Diag.count == UpperDiag.count + 1);
            Debug.Assert(Diag.count == Out.cols);
            Debug.Assert(Out.rows >= Out.cols);

            cmd.SetGPUVectorParam(shader, matrixBidiagKernelIdx, "_InputVector", Diag);
            cmd.SetGPUVectorParam(shader, matrixBidiagKernelIdx, "_SecondInputVector", UpperDiag);
            cmd.SetGPUMatrixParam(shader, matrixBidiagKernelIdx, "_OutputMatrix", Out);

            cmd.DispatchCompute(shader, matrixBidiagKernelIdx, Out.cols, Out.rows, 1);
        }

        public void CopyMatrix(CommandBuffer cmd, GPUMatrix In, GPUMatrix Out)
        {
            Debug.Assert(In.rows == Out.rows);
            Debug.Assert(In.cols == Out.cols);

            cmd.SetGPUMatrixParam(shader, matrixCopyKernelIdx, "_InputMatrix", In);
            cmd.SetGPUMatrixParam(shader, matrixCopyKernelIdx, "_OutputMatrix", Out);

            cmd.DispatchCompute(shader, matrixCopyKernelIdx, MathUtil.CeilDiv(Out.cols, 8), MathUtil.CeilDiv(Out.rows, 8), 1);
        }

        public void MatMul(CommandBuffer cmd, GPUMatrix Lhs, GPUMatrix Rhs, GPUMatrix Out)
        {
            Debug.Assert(Lhs.rows == Out.rows);
            Debug.Assert(Rhs.cols == Out.cols);
            Debug.Assert(Lhs.cols == Rhs.rows);

            cmd.SetGPUMatrixParam(shader, matrixMulKernelIdx, "_InputMatrix", Lhs);
            cmd.SetGPUMatrixParam(shader, matrixMulKernelIdx, "_SecondInputMatrix", Rhs);
            cmd.SetGPUMatrixParam(shader, matrixMulKernelIdx, "_OutputMatrix", Out);

            cmd.DispatchCompute(shader, matrixMulKernelIdx, Out.cols, Out.rows, 1);
        }

        public void MatRowMean(CommandBuffer cmd, GPUMatrix In, GPUVector Out)
        {
            Debug.Assert(In.rows == Out.count);

            cmd.SetGPUMatrixParam(shader, matrixRowMeanKernelIdx, "_InputMatrix", In);
            cmd.SetGPUVectorParam(shader, matrixRowMeanKernelIdx, "_OutputVector", Out);

            cmd.DispatchCompute(shader, matrixRowMeanKernelIdx, MathUtil.CeilDiv(In.cols, 32), In.rows, 1);
        }

        public void MatSubtractInplace(CommandBuffer cmd, GPUMatrix Lhs, GPUMatrix Rhs)
        {
            Debug.Assert(Lhs.rows == Rhs.rows);
            Debug.Assert(Lhs.cols == Rhs.cols);

            cmd.SetGPUMatrixParam(shader, matrixSubtractInplaceKernelIdx, "_OutputMatrix", Lhs);
            cmd.SetGPUMatrixParam(shader, matrixSubtractInplaceKernelIdx, "_InputMatrix", Rhs);

            cmd.DispatchCompute(shader, matrixSubtractInplaceKernelIdx, MathUtil.CeilDiv(Lhs.cols, 8), MathUtil.CeilDiv(Lhs.rows, 8), 1);
        }

        public void MatError(CommandBuffer cmd, GPUMatrix Mat, GPUVector Out)
        {
            Debug.Assert(Out.count == 1);

            cmd.SetGPUMatrixParam(shader, matrixErrorKernelIdx, "_InputMatrix", Mat);
            cmd.SetGPUVectorParam(shader, matrixErrorKernelIdx, "_OutputVector", Out);

            cmd.DispatchCompute(shader, matrixErrorKernelIdx, 1, 1, 1);
        }
    }
}
