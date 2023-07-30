// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using UnityEngine;
using UnityEngine.Rendering;

namespace AxF2Unity
{
    public class FilterCPA2ColorContext
    {
        private RenderTexture cubemapRenderTexture;

        private ComputeShader integrateColorShader;
        private int integrateSpecularKernelIdx;
        private int integrateDiffuseKernelIdx;

        public FilterCPA2ColorContext(int renderTargetSize, RenderTextureFormat renderTextureFormat)
        {
            cubemapRenderTexture = new RenderTexture(renderTargetSize, renderTargetSize, 0, renderTextureFormat);
            cubemapRenderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
            cubemapRenderTexture.volumeDepth = 6;
            cubemapRenderTexture.enableRandomWrite = true;
            cubemapRenderTexture.Create();

            integrateColorShader = (ComputeShader)Resources.Load("Shaders/AxFCPA2ComputePreIntegratedBRDFColor");
            integrateSpecularKernelIdx = integrateColorShader.FindKernel("CSMainSpecular");
            integrateDiffuseKernelIdx = integrateColorShader.FindKernel("CSMainDiffuse");
        }


        private void SetMaterialParameters(CommandBuffer commandBuffer, int kernelIdx, Texture2D colorLUT, Vector4 CTCoeffs, Vector4 CTSpreads, Vector4 CTF0s, float CCIOR, float CCNoRefraction)
        {
            float colorLUTScale = 1;

            commandBuffer.SetComputeTextureParam(integrateColorShader, kernelIdx, "_BRDFColorLUT", colorLUT);
            commandBuffer.SetComputeFloatParam(integrateColorShader, "_BRDFColorLUTScale", colorLUTScale);

            commandBuffer.SetComputeVectorParam(integrateColorShader, "_CTCoeffs", CTCoeffs);
            commandBuffer.SetComputeVectorParam(integrateColorShader, "_CTSpreads", CTSpreads);
            commandBuffer.SetComputeVectorParam(integrateColorShader, "_CTF0s", CTF0s);
            commandBuffer.SetComputeFloatParam(integrateColorShader, "_CCIOR", CCIOR);
            commandBuffer.SetComputeFloatParam(integrateColorShader, "_CCNoRefraction", CCNoRefraction);
        }

        private void SetSkyParameters(CommandBuffer commandBuffer, int kernelIdx, Texture sky)
        {
            commandBuffer.SetComputeIntParam(integrateColorShader, "_SkySize", sky.width);
            commandBuffer.SetComputeTextureParam(integrateColorShader, kernelIdx, "_Sky", sky);
        }

        private void SetOutputParameters(CommandBuffer commandBuffer, int kernelIdx, int outputSize)
        {
            commandBuffer.SetComputeIntParam(integrateColorShader, "_OutWidth", outputSize);
            commandBuffer.SetComputeIntParam(integrateColorShader, "_OutHeight", outputSize);

            commandBuffer.SetComputeTextureParam(integrateColorShader, kernelIdx, "_OutColor", cubemapRenderTexture, 0);
        }


        public void FilterSpecularColor(CommandBuffer commandBuffer, Texture skyTexture, Texture2D colorLUT, Vector4 CTCoeffs, Vector4 CTSpreads, Vector4 CTF0s, float CCIOR, float CCNoRefraction, CubemapArray cubemapArray)
        {
            int outputSize = cubemapArray.width;
            int outputLayerCount = cubemapArray.cubemapCount;

            SetSkyParameters(commandBuffer, integrateSpecularKernelIdx, skyTexture);
            SetMaterialParameters(commandBuffer, integrateSpecularKernelIdx, colorLUT, CTCoeffs, CTSpreads, CTF0s, CCIOR, CCNoRefraction);
            SetOutputParameters(commandBuffer, integrateSpecularKernelIdx, outputSize);

            for (int arrayLayer = 0; arrayLayer < outputLayerCount; ++arrayLayer)
            {
                // float theta = (float)i / (float) outputLayers;
                // float NdotV = Mathf.Cos(theta);
                float NdotV = (1 - (float)arrayLayer / (float)outputLayerCount);

                commandBuffer.SetComputeFloatParam(integrateColorShader, "_NdotV", NdotV);
                commandBuffer.DispatchCompute(integrateColorShader, integrateSpecularKernelIdx, (outputSize-1)/8+1, (outputSize-1)/8+1, 6);
                for (int cubemapFace = 0; cubemapFace < 6; ++cubemapFace)
                {
                    commandBuffer.CopyTexture(cubemapRenderTexture, cubemapFace, cubemapArray, arrayLayer*6 + cubemapFace);
                }
            }
        }

        public void FilterDiffuseColor(CommandBuffer commandBuffer, Texture skyTexture, Texture2D colorLUT, Vector4 CTCoeffs, Vector4 CTSpreads, Vector4 CTF0s, float CCIOR, float CCNoRefraction, CubemapArray cubemapArray)
        {
            int outputSize = cubemapArray.width;
            int outputLayerCount = cubemapArray.cubemapCount;

            SetSkyParameters(commandBuffer, integrateDiffuseKernelIdx, skyTexture);
            SetMaterialParameters(commandBuffer, integrateDiffuseKernelIdx, colorLUT, CTCoeffs, CTSpreads, CTF0s, CCIOR, CCNoRefraction);
            SetOutputParameters(commandBuffer, integrateDiffuseKernelIdx, outputSize);

            for (int arrayLayer = 0; arrayLayer < outputLayerCount; ++arrayLayer)
            {
                // float theta = (float)i / (float) outputLayers;
                // float NdotV = Mathf.Cos(theta);
                float NdotV = (1 - (float)arrayLayer / (float)outputLayerCount);

                commandBuffer.SetComputeFloatParam(integrateColorShader, "_NdotV", NdotV);
                commandBuffer.DispatchCompute(integrateColorShader, integrateDiffuseKernelIdx, (outputSize-1)/8+1, (outputSize-1)/8+1, 6);
                for (int cubemapFace = 0; cubemapFace < 6; ++cubemapFace)
                {
                    commandBuffer.CopyTexture(cubemapRenderTexture, cubemapFace, cubemapArray, arrayLayer*6 + cubemapFace);
                }
            }
        }

        public void Release()
        {
            cubemapRenderTexture.Release();
        }

    }
}
