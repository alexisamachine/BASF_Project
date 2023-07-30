// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace AxF2Unity
{
    public class AxFCPA2ComputePreIntegratedFGD
    {
        static readonly Lazy<AxFCPA2ComputePreIntegratedFGD> s_Instance = new Lazy<AxFCPA2ComputePreIntegratedFGD>(() => new AxFCPA2ComputePreIntegratedFGD());

        public static AxFCPA2ComputePreIntegratedFGD instance => s_Instance.Value;

        private ComputeShader computeShader = null;
        private int kernelIdx;
        private RenderTexture renderTexture = null;
        private Texture2D readbackTexture = null;

        private CommandBuffer commandBuffer;

        public AxFCPA2ComputePreIntegratedFGD()
        {
            commandBuffer = new CommandBuffer();
            commandBuffer.name = "AxFCPA2ComputePreIntegratedFGD";
        }

        private void EnsureLoaded(int outputSize)
        {
            if (computeShader == null)
            {
                computeShader = (ComputeShader)Resources.Load("Shaders/AxFCPA2ComputePreIntegratedFGD");
                kernelIdx = computeShader.FindKernel("CSMain");
            }

            if (renderTexture == null || renderTexture.width < outputSize)
            {
                renderTexture = new RenderTexture(outputSize, 1, 0, RenderTextureFormat.ARGBFloat);
                renderTexture.enableRandomWrite = true;
                renderTexture.Create();
            }

            if (readbackTexture == null || readbackTexture.width < outputSize)
            {
                readbackTexture = new Texture2D(outputSize, 1, TextureFormat.RGBAFloat, mipChain : false, linear : true);
            }
        }

        public void ComputeFGDForMaterial(Material mat)
        {
            Texture2D preIntegratedFGD = (Texture2D)mat.GetTexture("_PreIntegratedFGD");
            Vector4 preIntegratedFGDScale = mat.GetVector("_PreIntegratedFGDScale");

            if (preIntegratedFGD == null)
            {
                Debug.LogWarning("No PreIntegratedFGD texture found to write to!");
                return;
            }

            int outputSize = preIntegratedFGD.width;
            EnsureLoaded(outputSize);

            float CCIOR = mat.GetFloat("_EnableClearCoat") > 0 ? mat.GetFloat("_ClearCoatIOR") : 1.0f;
            float CCNoRefraction = 0;

            Vector4 CTCoeffs = mat.GetVector("_CTCoeffs");
            Vector4 CTF0s = mat.GetVector("_CTF0s");
            Vector4 CTSpreads = mat.GetVector("_CTSpreads");

            float GGXSmoothness = mat.GetFloat("_GGXSmoothness");


            commandBuffer.Clear();
            // CommandBuffer commandBuffer = CommandBufferPool.Get("AxFCPA2ComputePreIntegratedFGD");

            commandBuffer.SetComputeFloatParam(computeShader, "_CCIOR", CCIOR);
            commandBuffer.SetComputeFloatParam(computeShader, "_CCNoRefraction", CCNoRefraction);

            commandBuffer.SetComputeVectorParam(computeShader, "_CTCoeffs", CTCoeffs);
            commandBuffer.SetComputeVectorParam(computeShader, "_CTF0s", CTF0s);
            commandBuffer.SetComputeVectorParam(computeShader, "_CTSpreads", CTSpreads);

            commandBuffer.SetComputeFloatParam(computeShader, "_GGXSmoothness", GGXSmoothness);

            commandBuffer.SetComputeIntParam(computeShader, "_OutputWidth", outputSize);
            commandBuffer.SetComputeTextureParam(computeShader, kernelIdx, "_Output", renderTexture);

            commandBuffer.DispatchCompute(computeShader, kernelIdx, 1, outputSize, 1);

            Graphics.ExecuteCommandBuffer(commandBuffer);
            // CommandBufferPool.Release(commandBuffer);

            RenderTexture backupRenderTexture = RenderTexture.active;
            RenderTexture.active = renderTexture;

            readbackTexture.ReadPixels(new Rect(0, 0, outputSize, 1), 0, 0);
            readbackTexture.Apply();

            RenderTexture.active = backupRenderTexture;

            // Update red and green color channels only!
            Color[] originalData = preIntegratedFGD.GetPixels(miplevel: 0);
            Color[] readbackData = readbackTexture.GetPixels(0, 0, outputSize, 1, miplevel: 0);

            preIntegratedFGDScale.x = 0;
            preIntegratedFGDScale.y = 0;
            for (int i = 0; i < outputSize; ++i)
            {
                if (preIntegratedFGDScale.x < readbackData[i].r)
                    preIntegratedFGDScale.x = readbackData[i].r;
                if (preIntegratedFGDScale.y < readbackData[i].g)
                    preIntegratedFGDScale.y = readbackData[i].g;
            }

            for (int i = 0; i < outputSize; ++i)
            {
                originalData[i].r = readbackData[i].r / preIntegratedFGDScale.x;
                originalData[i].g = readbackData[i].g / preIntegratedFGDScale.y;
            }

            preIntegratedFGD.SetPixels(originalData);
            preIntegratedFGD.Apply();

            mat.SetVector("_PreIntegratedFGDScale", preIntegratedFGDScale);
        }
    }
}
