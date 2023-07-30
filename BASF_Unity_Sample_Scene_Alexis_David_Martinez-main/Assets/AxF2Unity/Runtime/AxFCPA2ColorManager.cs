// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

namespace AxF2Unity
{
    public class AxFCPA2ColorManager : MonoBehaviour
    {
        public bool isGlobal;
        public Material skyMaterial; // RenderingSettings.skybox if null
        public int defaultCubemapSize = 8;
        public int defaultArraySize = 8;

        public int skySize = 1024; // Size of the render texture that the sky is rendered into!

        public RenderTexture skyTexture;

        const string kBRDFColorMode = "_BRDFColorMode";
        const string kBRDFColorLUT = "_BRDFColorLUT";
        const string kBRDFColorLUTxSkyDiffuse = "_BRDFColorLUTxSkyDiffuse";
        const string kBRDFColorLUTxSkySpecular = "_BRDFColorLUTxSkySpecular";
        const string kBRDFColorLUTxSky_ArraySize = "_BRDFColorLUTxSky_ArraySize";

        const string kBRDFColorLUTxSkySpecularCubemap = "_BRDFColorLUTxSkySpecularCubemap";
        const string kBRDFColorLUTxSkySpecularArray = "_BRDFColorLUTxSkySpecularArray";
        const string kBRDFColorLUTxSkySpecularMeanColor = "_BRDFColorLUTxSkySpecularMeanColor";

        const string kBRDFColorLUTxSkyDiffuseCubemap = "_BRDFColorLUTxSkyDiffuseCubemap";
        const string kBRDFColorLUTxSkyDiffuseArray = "_BRDFColorLUTxSkyDiffuseArray";
        const string kBRDFColorLUTxSkyDiffuseMeanColor = "_BRDFColorLUTxSkyDiffuseMeanColor";

        const string kCTCoeffs = "_CTCoeffs";
        const string kCTF0s = "_CTF0s";
        const string kCTSpreads = "_CTSpreads";

        const string kEnableClearCoat = "_EnableClearCoat";
        const string kCCIOR = "_ClearCoatIOR";
        // Assume _CCNoRefraction == 0
        // const string kCCNoRefraction = "_CCNoRefraction";

        private TextureFormat textureFormat = TextureFormat.RGBAFloat;
        private RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGBFloat;


        private CommandBuffer commandBuffer;


        private struct Key : IEquatable<Key>
        {
            public Texture2D colorLUT;

            public Vector4 CTCoeffs;
            public Vector4 CTSpreads;
            public Vector4 CTF0s;

            public float CCIOR;
            public float CCNoRefraction;

            public int outputSize;
            public int outputLayers; // TODO use different layer count per material? yes.

            public Key(Texture2D colorLUT, Vector4 CTCoeffs, Vector4 CTSpreads, Vector4 CTF0s, float CCIOR, float CCNoRefraction, int outputSize, int outputLayers)
            {
                this.colorLUT = colorLUT;
                this.outputSize = outputSize;
                this.outputLayers = outputLayers;

                this.CTCoeffs = CTCoeffs;
                this.CTSpreads = CTSpreads;
                this.CTF0s = CTF0s;

                this.CCIOR = CCIOR;
                this.CCNoRefraction = CCNoRefraction;
            }

            public override int GetHashCode()
            {
                var hashCode = 43270662;
                hashCode = hashCode * -1521134295 + colorLUT.GetHashCode();
                hashCode = hashCode * -1521134295 + CTCoeffs.GetHashCode();
                hashCode = hashCode * -1521134295 + CTSpreads.GetHashCode();
                hashCode = hashCode * -1521134295 + CTF0s.GetHashCode();
                hashCode = hashCode * -1521134295 + CCIOR.GetHashCode();
                hashCode = hashCode * -1521134295 + CCNoRefraction.GetHashCode();
                hashCode = hashCode * -1521134295 + outputSize.GetHashCode();
                hashCode = hashCode * -1521134295 + outputLayers.GetHashCode();
                return hashCode;
            }
            public override bool Equals(object obj)
            {
                if (!(obj is Key))
                    return false;
                Key other = (Key)obj;
                return colorLUT == other.colorLUT &&
                    CTCoeffs == other.CTCoeffs &&
                    CTSpreads == other.CTSpreads &&
                    CTF0s == other.CTF0s &&
                    CCIOR == other.CCIOR &&
                    CCNoRefraction == other.CCNoRefraction &&
                    outputSize == other.outputSize &&
                    outputLayers == other.outputLayers;
            }
            public bool Equals(Key other)
            {
                return colorLUT == other.colorLUT &&
                    CTCoeffs == other.CTCoeffs &&
                    CTSpreads == other.CTSpreads &&
                    CTF0s == other.CTF0s &&
                    CCIOR == other.CCIOR &&
                    CCNoRefraction == other.CCNoRefraction &&
                    outputSize == other.outputSize &&
                    outputLayers == other.outputLayers;
            }
        }
        private class Value
        {
            public HashSet<Material> materials = new HashSet<Material>();
            public CubemapArray specularCubemapArray = null;
            public CubemapArray diffuseCubemapArray = null;
            public bool computeSVD = false;
            public Cubemap specularCubemap = null;
            public Texture2D specularArray = null;
            public Cubemap diffuseCubemap = null;
            public Texture2D diffuseArray = null;
        }

        private Dictionary<Key, Value> materials = new Dictionary<Key, Value>();

        private static string[] requiredProperties =
        {
            kBRDFColorMode,
            kBRDFColorLUT,
            kBRDFColorLUTxSkyDiffuse,
            kBRDFColorLUTxSkySpecular,
            kBRDFColorLUTxSky_ArraySize,
            kCTCoeffs,
            kCTF0s,
            kCTSpreads
        };
        private bool HasRequiredProperties(Material mat)
        {
            foreach (var prop in requiredProperties)
            {
                if (!mat.HasProperty(prop))
                    return false;
            }
            return true;
        }

        private void CollectMaterialsFromRoot(Transform root)
        {
            // Collect all CPA2 materials for which PreIntegratedCPA2ColorLUT is enabled
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat == null)
                        continue;
                    // Check for all the properties that are relevant!
                    if (!HasRequiredProperties(mat))
                        continue;

                    // User has to enable the feature per material!
                    if (mat.GetFloat(kBRDFColorMode) != (float) AxFCPA2BRDFColorMode.IntegratedSky &&
                        mat.GetFloat(kBRDFColorMode) != (float) AxFCPA2BRDFColorMode.IntegratedSkySVD)
                        continue;

                    Texture2D colorLUT = mat.GetTexture(kBRDFColorLUT) as Texture2D;
                    if (colorLUT == null)
                        continue;

                    Debug.Log($"Found material {mat.name}");

                    Vector4 CTCoeffs = mat.GetVector(kCTCoeffs);
                    Vector4 CTSpreads = mat.GetVector(kCTSpreads);
                    Vector4 CTF0s = mat.GetVector(kCTF0s);
                    bool hasClearCoat = mat.HasProperty(kEnableClearCoat) && mat.GetFloat(kEnableClearCoat) > 0 && mat.HasProperty(kCCIOR);
                    float CCIOR = hasClearCoat ? mat.GetFloat(kCCIOR) : 1.0f;
                    float CCNoRefraction = 0.0f; // mat.GetFloat(kCCNoRefraction);

                    // TODO don't use default sizes!
                    var key = new Key(colorLUT, CTCoeffs, CTSpreads, CTF0s, CCIOR, CCNoRefraction, defaultCubemapSize, defaultArraySize);

                    Value value;
                    if (!materials.TryGetValue(key, out value))
                    {
                        value = new Value();
                        materials.Add(key, value);
                    }
                    if (value.materials.Add(mat))
                    {
                        // Material was no already in list...

                        CubemapArray cubemapArray;

                        // If the material contains a suitable cubemaparray, reuse it
                        cubemapArray = mat.GetTexture(kBRDFColorLUTxSkySpecular) as CubemapArray;
                        if (value.specularCubemapArray == null && cubemapArray != null && cubemapArray.width == key.outputSize && cubemapArray.height == key.outputSize && cubemapArray.cubemapCount == key.outputLayers)
                            value.specularCubemapArray = cubemapArray;

                        cubemapArray = mat.GetTexture(kBRDFColorLUTxSkyDiffuse) as CubemapArray;
                        if (value.diffuseCubemapArray == null && cubemapArray != null && cubemapArray.width == key.outputSize && cubemapArray.height == key.outputSize && cubemapArray.cubemapCount == key.outputLayers)
                            value.diffuseCubemapArray = cubemapArray;

                        if (mat.GetFloat(kBRDFColorMode) == (float) AxFCPA2BRDFColorMode.IntegratedSkySVD)
                        {
                            value.computeSVD = true;

                            Cubemap cubemap;
                            Texture2D array;

                            cubemap = mat.GetTexture(kBRDFColorLUTxSkySpecularCubemap) as Cubemap;
                            if (value.specularCubemap == null && cubemap != null && cubemap.width == key.outputSize && cubemap.height == key.outputSize)
                                value.specularCubemap = cubemap;

                            array = mat.GetTexture(kBRDFColorLUTxSkySpecularArray) as Texture2D;
                            if (value.specularArray == null && array != null && array.width == key.outputLayers && array.height == 3)
                                value.specularArray = array;

                            cubemap = mat.GetTexture(kBRDFColorLUTxSkyDiffuseCubemap) as Cubemap;
                            if (value.diffuseCubemap == null && cubemap != null && cubemap.width == key.outputSize && cubemap.height == key.outputSize)
                                value.diffuseCubemap = cubemap;

                            array = mat.GetTexture(kBRDFColorLUTxSkyDiffuseArray) as Texture2D;
                            if (value.diffuseArray == null && array != null && array.width == key.outputLayers && array.height == 3)
                                value.diffuseArray = array;
                        }
                    }
                }
            }
        }

        public void CollectMaterialsFromScene()
        {
            materials.Clear();

            if (isGlobal)
            {
                for (int i = 0; i < SceneManager.sceneCount; ++i)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    foreach (GameObject go in scene.GetRootGameObjects())
                    {
                        CollectMaterialsFromRoot(go.transform);
                    }
                }
            }
            else
            {
                CollectMaterialsFromRoot(this.transform);
            }

            // Make sure that all materials are using the respective output cuebmap arrays, and also make sure that they exist!
            foreach (KeyValuePair<Key, Value> entry in materials)
            {
                if (entry.Value.specularCubemapArray == null)
                {
                    entry.Value.specularCubemapArray = new CubemapArray(entry.Key.outputSize, entry.Key.outputLayers, textureFormat, mipChain: false, linear: false);
                }
                if (entry.Value.diffuseCubemapArray == null)
                {
                    entry.Value.diffuseCubemapArray = new CubemapArray(entry.Key.outputSize, entry.Key.outputLayers, textureFormat, mipChain: false, linear: false);
                }

                if (entry.Value.computeSVD)
                {
                    if (entry.Value.specularCubemap == null)
                    {
                        entry.Value.specularCubemap = new Cubemap(entry.Key.outputSize, textureFormat, mipChain: false); // no linear option!
                    }
                    if (entry.Value.specularArray == null)
                    {
                        entry.Value.specularArray = new Texture2D(entry.Key.outputLayers, 3, textureFormat, mipChain: false, linear: false);
                    }
                    if (entry.Value.diffuseCubemap == null)
                    {
                        entry.Value.diffuseCubemap = new Cubemap(entry.Key.outputSize, textureFormat, mipChain: false); // no linear option!
                    }
                    if (entry.Value.diffuseArray == null)
                    {
                        entry.Value.diffuseArray = new Texture2D(entry.Key.outputLayers, 3, textureFormat, mipChain: false, linear: false);
                    }
                }

                foreach (Material mat in entry.Value.materials)
                {
                    mat.SetTexture(kBRDFColorLUTxSkySpecular, entry.Value.specularCubemapArray);
                    mat.SetTexture(kBRDFColorLUTxSkyDiffuse, entry.Value.diffuseCubemapArray);
                    mat.SetInt(kBRDFColorLUTxSky_ArraySize, entry.Key.outputLayers);

                    // TODO depend on whether material wants to have svd textures!
                    if (entry.Value.computeSVD)
                    {
                        mat.SetTexture(kBRDFColorLUTxSkySpecularCubemap, entry.Value.specularCubemap);
                        mat.SetTexture(kBRDFColorLUTxSkySpecularArray, entry.Value.specularArray);
                        mat.SetTexture(kBRDFColorLUTxSkyDiffuseCubemap, entry.Value.diffuseCubemap);
                        mat.SetTexture(kBRDFColorLUTxSkyDiffuseArray, entry.Value.diffuseArray);
                    }
                }
            }
        }


        public void UpdateFilteredCubemaps()
        {
            // Render sky material into cubemap
            Material skyMaterial = this.skyMaterial != null ? this.skyMaterial : RenderSettings.skybox;
            if (skyMaterial == null)
                throw new Exception("No skybox found!");

            if (commandBuffer == null)
            {
                commandBuffer = new CommandBuffer();
                commandBuffer.name = "AxFCPA2ColorManager";
            }
            else
            {
                commandBuffer.Clear();
            }
            // CommandBuffer commandBuffer = CommandBufferPool.Get("AxFCPA2ColorManager");

            DrawSkyboxContext drawSky = new DrawSkyboxContext(skySize);
            drawSky.DrawSkyMaterial(commandBuffer, skyMaterial);

            skyTexture = drawSky.skyTexture;

            // TODO determine maximum cubemap size!
            FilterCPA2ColorContext filterContext = new FilterCPA2ColorContext(defaultCubemapSize, renderTextureFormat);
            ApproxCubemapArraySVDContext approxSVDContext = null;

            foreach (KeyValuePair<Key, Value> entry in materials)
            {
                filterContext.FilterSpecularColor(commandBuffer, drawSky.skyTexture, entry.Key.colorLUT, entry.Key.CTCoeffs, entry.Key.CTSpreads, entry.Key.CTF0s, entry.Key.CCIOR, entry.Key.CCNoRefraction, entry.Value.specularCubemapArray);
                filterContext.FilterDiffuseColor(commandBuffer, drawSky.skyTexture, entry.Key.colorLUT, entry.Key.CTCoeffs, entry.Key.CTSpreads, entry.Key.CTF0s, entry.Key.CCIOR, entry.Key.CCNoRefraction, entry.Value.diffuseCubemapArray);

                if (entry.Value.computeSVD)
                {
                    if (approxSVDContext == null)
                        approxSVDContext = new ApproxCubemapArraySVDContext(defaultCubemapSize, defaultArraySize);

                    approxSVDContext.Approximate(commandBuffer, entry.Value.specularCubemapArray, entry.Value.specularCubemap, entry.Value.specularArray, out Color meanSpecularColor);
                    approxSVDContext.Approximate(commandBuffer, entry.Value.diffuseCubemapArray, entry.Value.diffuseCubemap, entry.Value.diffuseArray, out Color meanDiffuseColor);

                    foreach (Material mat in entry.Value.materials)
                    {
                        mat.SetColor(kBRDFColorLUTxSkySpecularMeanColor, meanSpecularColor.gamma);
                        mat.SetColor(kBRDFColorLUTxSkyDiffuseMeanColor, meanDiffuseColor.gamma);
                    }
                }
            }

            Graphics.ExecuteCommandBuffer(commandBuffer);
            // CommandBufferPool.Release(commandBuffer);

            drawSky.Release();
            filterContext.Release();
            if (approxSVDContext != null)
                approxSVDContext.Release();
        }

    }
}
