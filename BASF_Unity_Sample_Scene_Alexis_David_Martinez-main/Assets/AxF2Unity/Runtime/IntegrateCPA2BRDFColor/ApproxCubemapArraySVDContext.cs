// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using UnityEngine;
using UnityEngine.Rendering;

namespace AxF2Unity
{
    public class ApproxCubemapArraySVDContext
    {
        private SVD svd;

        private GPUMatrix data;
        private GPUVector dataMean;

        public RenderTexture tempCubemapArrayRenderTexture;
        public RenderTexture tempArrayRenderTexture;

        private int cubemapFaceSize;
        private int cubemapArrayLayers;

        private ComputeShader cubemapMatrixShader;
        private int copyCubemapArrayToMatrixKernelIndex;
        private int copyMatrixToCubemapKernelIndex;
        private int copyMatrixToArrayKernelIndex;

        public ApproxCubemapArraySVDContext(int cubemapFaceSize, int cubemapArrayLayers)
        {
            this.cubemapFaceSize = cubemapFaceSize;
            this.cubemapArrayLayers = cubemapArrayLayers;

            // Temporary workaround since we cannot read from CubemapArray in compute shader
            tempCubemapArrayRenderTexture = new RenderTexture(cubemapFaceSize, cubemapFaceSize, 0, RenderTextureFormat.ARGBFloat);
            tempCubemapArrayRenderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
            tempCubemapArrayRenderTexture.volumeDepth = 6*cubemapArrayLayers;
            tempCubemapArrayRenderTexture.enableRandomWrite = true;
            tempCubemapArrayRenderTexture.Create();

            tempArrayRenderTexture = new RenderTexture(cubemapArrayLayers, 3, 0, RenderTextureFormat.ARGBFloat);
            tempArrayRenderTexture.enableRandomWrite = true;
            tempArrayRenderTexture.Create();

            // SVD is computed on matrix of shape (cubemap_faces * face_size * face_size, array_layers * channels)
            const int channels = 3;
            const int cubemapFaces = 6;
            int svdRows = cubemapFaces * cubemapFaceSize * cubemapFaceSize;
            int svdCols = cubemapArrayLayers * channels;
            svd = new SVD(svdRows, svdCols);

            data = new GPUMatrix(svdRows, svdCols);
            dataMean = new GPUVector(channels);

            cubemapMatrixShader = (ComputeShader)Resources.Load("Shaders/CopyCubemapMatrix");
            copyCubemapArrayToMatrixKernelIndex = cubemapMatrixShader.FindKernel("CSCopyCubemapArrayToMatrix");
            copyMatrixToCubemapKernelIndex = cubemapMatrixShader.FindKernel("CSCopyMatrixToCubemap");
            copyMatrixToArrayKernelIndex = cubemapMatrixShader.FindKernel("CSCopyMatrixToArray");
        }

        private void CopyCubemapArrayToData(CommandBuffer commandBuffer, CubemapArray inputCubemapArray)
        {
            Debug.Assert(cubemapFaceSize == inputCubemapArray.width);
            Debug.Assert(cubemapArrayLayers == inputCubemapArray.cubemapCount);

            // NOTE: Cannot random access read from CubemapArray, only RenderTexture with enableRandomWrite..
            for (int i = 0; i < 6*cubemapArrayLayers; ++i)
            {
                commandBuffer.CopyTexture(inputCubemapArray, i, tempCubemapArrayRenderTexture, i);
            }

            commandBuffer.SetComputeIntParam(cubemapMatrixShader, "_FaceSize", cubemapFaceSize);
            commandBuffer.SetComputeIntParam(cubemapMatrixShader, "_ArraySize", cubemapArrayLayers);

            commandBuffer.SetComputeTextureParam(cubemapMatrixShader, copyCubemapArrayToMatrixKernelIndex, "_CubemapArray", tempCubemapArrayRenderTexture);
            commandBuffer.SetGPUMatrixParam(cubemapMatrixShader, copyCubemapArrayToMatrixKernelIndex, "_OutputMatrix", data);

            commandBuffer.DispatchCompute(cubemapMatrixShader, copyCubemapArrayToMatrixKernelIndex, MathUtil.CeilDiv(data.cols, 8), MathUtil.CeilDiv(data.rows, 8), 1);
        }

        private void ComputeMeanColor(CommandBuffer commandBuffer)
        {
            GenericShader.instance.MatRowMean(commandBuffer, data.reshape(6 * cubemapFaceSize * cubemapFaceSize * cubemapArrayLayers, 3).transposed(), dataMean);
        }

        private void SubtractMeanColor(CommandBuffer commandBuffer)
        {
            GPUMatrix expandedDataMean = new GPUMatrix(dataMean.start, 6 * cubemapFaceSize * cubemapFaceSize * cubemapArrayLayers, dataMean.count, 0, dataMean.stride, dataMean.buffer);
            GenericShader.instance.MatSubtractInplace(commandBuffer, data.reshape(6 * cubemapFaceSize * cubemapFaceSize * cubemapArrayLayers, 3), expandedDataMean);
        }

        private static int[] Top4Indices(CPUVector signedSingularValues)
        {
            // argmax_i abs(x[i])
            int[] indices = {0, 0, 0, 0};
            float[] values = {0, 0, 0, 0};
            for (int i = 0; i < signedSingularValues.count; ++i)
            {
                float value = Mathf.Abs(signedSingularValues[i]);
                if (value > values[3])
                {
                    for (int j = 3; j >= 0; --j)
                    {
                        if (j == 0 || values[j-1] > value)
                        {
                            values[j] = value;
                            indices[j] = i;
                            break;
                        }
                        values[j] = values[j-1];
                        indices[j] = indices[j-1];
                    }
                }
            }
            return indices;
        }

        private void FillOutputCubemap(CommandBuffer commandBuffer, int[] indices, Cubemap outputCubemap)
        {
            Debug.Assert(indices.Length == 4);

            commandBuffer.SetComputeIntParam(cubemapMatrixShader, "_FaceSize", cubemapFaceSize);

            commandBuffer.SetComputeIntParams(cubemapMatrixShader, "_Indices", indices);
            commandBuffer.SetGPUMatrixParam(cubemapMatrixShader, copyMatrixToCubemapKernelIndex, "_InputMatrix", svd.Q_T.transposed());
            commandBuffer.SetComputeTextureParam(cubemapMatrixShader, copyMatrixToCubemapKernelIndex, "_CubemapArray", tempCubemapArrayRenderTexture);

            commandBuffer.DispatchCompute(cubemapMatrixShader, copyMatrixToCubemapKernelIndex, MathUtil.CeilDiv(cubemapFaceSize, 8), MathUtil.CeilDiv(cubemapFaceSize, 8), 6);

            for (int i = 0; i < 6; ++i)
                commandBuffer.CopyTexture(tempCubemapArrayRenderTexture, i, outputCubemap, i);
        }

        private void FillOutputArray(CommandBuffer commandBuffer, int[] indices, float[] scalingFactors, Texture2D outputTexture)
        {
            Debug.Assert(indices.Length == 4);

            commandBuffer.SetComputeIntParam(cubemapMatrixShader, "_ArraySize", cubemapArrayLayers);

            commandBuffer.SetComputeIntParams(cubemapMatrixShader, "_Indices", indices);
            commandBuffer.SetComputeFloatParams(cubemapMatrixShader, "_ScalingFactors", scalingFactors);

            commandBuffer.SetGPUMatrixParam(cubemapMatrixShader, copyMatrixToArrayKernelIndex, "_InputMatrix", svd.P_T);
            commandBuffer.SetComputeTextureParam(cubemapMatrixShader, copyMatrixToArrayKernelIndex, "_Array", tempArrayRenderTexture);

            commandBuffer.DispatchCompute(cubemapMatrixShader, copyMatrixToArrayKernelIndex, MathUtil.CeilDiv(cubemapArrayLayers, 8), 1, 1);

            commandBuffer.CopyTexture(tempArrayRenderTexture, outputTexture);
        }

        public void Approximate(CommandBuffer commandBuffer, CubemapArray inputCubemapArray, Cubemap outputCubemap, Texture2D outputArray, out Color meanColor)
        {
            Debug.Assert(inputCubemapArray.width == cubemapFaceSize);
            Debug.Assert(inputCubemapArray.cubemapCount == cubemapArrayLayers);

            Debug.Assert(outputCubemap.width == cubemapFaceSize);
            Debug.Assert(outputArray.width == cubemapArrayLayers);
            Debug.Assert(outputArray.height == 3);

            // 1. Copy cubemap array to ComputeBuffer
            CopyCubemapArrayToData(commandBuffer, inputCubemapArray);
            // 2. Compute mean color
            ComputeMeanColor(commandBuffer);
            // 3. Subtract mean color
            SubtractMeanColor(commandBuffer);

            // GPUMatrix dataCopy = new GPUMatrix(data.rows, data.cols);
            // GenericShader.instance.CopyMatrix(commandBuffer, data, dataCopy);
            // 4. Compute SVD
            svd.Bidiagonalize(commandBuffer, data); // destroys the contents of data!
            svd.ReadbackBidiagonal(commandBuffer);
            svd.Diagonalize(commandBuffer);

            /*
            svd.D_gpu.buffer.SetData(svd.D_cpu.buffer);
            svd.F_gpu.buffer.SetData(svd.F_cpu.buffer);

            GPUMatrix B = new GPUMatrix(data.rows, data.cols);
            GenericShader.instance.SetMatrixBidiag(commandBuffer, svd.D_gpu, svd.F_gpu, B);

            GPUMatrix temp1 = new GPUMatrix(data.rows, data.cols);
            GPUMatrix recon = new GPUMatrix(data.rows, data.cols);
            GenericShader.instance.MatMul(commandBuffer, svd.Q_T.transposed(), B, temp1);
            GenericShader.instance.MatMul(commandBuffer, temp1, svd.P_T, recon);

            GenericShader.instance.MatSubtractInplace(commandBuffer, recon, data);

            GPUVector diff = new GPUVector(1);
            GenericShader.instance.MatError(commandBuffer, recon, diff);

            Graphics.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();

            Debug.Log($"Q_T\n{svd.Q_T.ToString()}");
            Debug.Log($"P_T\n{svd.P_T.ToString()}");
            Debug.Log($"D\n{svd.D_gpu.ToString()}");
            Debug.Log($"F\n{svd.F_gpu.ToString()}");

            Debug.Log($"data\n{data.ToString()}");
            Debug.Log($"error mat\n{recon.ToString()}");
            Debug.Log($"error {diff.ToString()}");
            */

            // 5. Sort singular values (on CPU)
            int[] indices = Top4Indices(svd.D_cpu);

            // 6. Copy data from SVD to output cubemap and array
            FillOutputCubemap(commandBuffer, indices, outputCubemap);

            float[] scalingFactors = new float[4];
            for (int i = 0; i < 4; ++i)
                scalingFactors[i] = svd.D_cpu[indices[i]];
            FillOutputArray(commandBuffer, indices, scalingFactors, outputArray);

            // 7. Copy mean color data
            float[] meanColorData = new float[3];
            dataMean.buffer.GetData(meanColorData);
            meanColor = new Color(meanColorData[0], meanColorData[1], meanColorData[2]);
        }

        public void Release()
        {
            tempCubemapArrayRenderTexture.Release();
            tempArrayRenderTexture.Release();

            svd.Release();

            data.buffer.Release();
            dataMean.buffer.Release();

        }
    }
}
