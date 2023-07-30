// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using UnityEngine;
using UnityEngine.Rendering;

namespace AxF2Unity
{
    public class DrawSkyboxContext
    {
        /// <summary>
        /// List of look at matrices for cubemap faces.
        /// Ref: https://msdn.microsoft.com/en-us/library/windows/desktop/bb204881(v=vs.85).aspx
        /// </summary>
        static private readonly Vector3[] lookAtList =
        {
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(-1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, -1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(0.0f, 0.0f, -1.0f),
        };

        /// <summary>
        /// List of up vectors for cubemap faces.
        /// Ref: https://msdn.microsoft.com/en-us/library/windows/desktop/bb204881(v=vs.85).aspx
        /// </summary>
        static private readonly Vector3[] upVectorList =
        {
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, -1.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
        };


        public RenderTexture skyTexture { get; private set; }

        public DrawSkyboxContext(int outputSize)
        {
            skyTexture = new RenderTexture(outputSize, outputSize, 0, RenderTextureFormat.DefaultHDR);
            skyTexture.isCubemap = true;
            skyTexture.useMipMap = true;
            skyTexture.autoGenerateMips = false;
            skyTexture.Create();
        }

        public void DrawSkyMaterial(CommandBuffer cmd, Material skyMat)
        {
            // Cache this mesh!
            Mesh mesh = GetCubeMesh();

            CubemapFace[] cubemapFaces = { CubemapFace.PositiveX, CubemapFace.NegativeX, CubemapFace.PositiveY, CubemapFace.NegativeY, CubemapFace.PositiveZ, CubemapFace.NegativeZ };

            Matrix4x4 projection = Matrix4x4.Perspective(90.0f, 1, 0.5f, 2) * Matrix4x4.Scale(new Vector3(1.0f, -1.0f, 1.0f));
            cmd.SetProjectionMatrix(projection);

            Matrix4x4[] viewMats = new Matrix4x4[6];
            for (int i = 0; i < 6; ++i)
            {
                var lookAt = Matrix4x4.LookAt(Vector3.zero, lookAtList[i], upVectorList[i]);
                var worldToView = lookAt * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f));
                viewMats[i] = worldToView;
            }

            // Render into output cubemaps
            for (int i = 0; i < 6; ++i)
            {
                cmd.SetRenderTarget(skyTexture, 0, cubemapFaces[i]);
                cmd.SetViewMatrix(viewMats[i]);
                cmd.DrawMesh(mesh, Matrix4x4.identity, skyMat); // , 0, -1, null
            }

            cmd.GenerateMips(skyTexture);
        }

        public void Release()
        {
            skyTexture.Release();
        }


        static private readonly Vector3[] cubeVertices =
        {
            new Vector3(-1, -1, -1),
            new Vector3(-1, -1,  1),
            new Vector3(-1,  1, -1),
            new Vector3(-1,  1,  1),
            new Vector3( 1, -1, -1),
            new Vector3( 1, -1,  1),
            new Vector3( 1,  1, -1),
            new Vector3( 1,  1,  1)
        };

        static private readonly int[] cubeIndices =
        {
            2, 0, 1, 2, 1, 3,
            0, 4, 5, 0, 5, 1,
            4, 6, 7, 4, 7, 5,
            6, 2, 3, 6, 3, 7,
            1, 5, 7, 1, 7, 3,
            0, 2, 6, 0, 6, 4
        };

        private static Mesh _cubeMesh = null;
        private static Mesh GetCubeMesh()
        {
            if (_cubeMesh == null)
            {
                _cubeMesh = new Mesh();
                _cubeMesh.SetVertices(cubeVertices);
                _cubeMesh.SetIndices(cubeIndices, MeshTopology.Triangles, 0);
            }
            return _cubeMesh;
        }
    }
}
