// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

namespace AxF2Unity
{
    public class SVD
    {
        private int rows;
        private int cols;

        private ComputeBuffer tempBuffer;

        public GPUMatrix Q_T;

        public GPUVector D_gpu;
        public CPUVector D_cpu;

        public GPUVector F_gpu;
        public CPUVector F_cpu;

        public GPUMatrix P_T;


        public SVD(int rows, int cols)
        {
            Debug.Assert(rows >= cols);

            this.rows = rows;
            this.cols = cols;

            this.D_gpu = new GPUVector(cols); // diagonal entries
            this.F_gpu = new GPUVector(cols-1); // off-diagonal entried

            this.D_cpu = new CPUVector(cols);
            this.F_cpu = new CPUVector(cols-1);

            this.Q_T = new GPUMatrix(rows, rows);
            this.P_T = new GPUMatrix(cols, cols);

            this.tempBuffer = new ComputeBuffer(rows + rows * rows, stride: 4, type: ComputeBufferType.Structured);
        }

        public void Bidiagonalize(CommandBuffer cmd, GPUMatrix A)
        {
            // Factorize A = Q @ Bidiag(D, F) @ P_T

            GPUVector D = D_gpu;
            GPUVector F = F_gpu;

            Debug.Assert(A.rows == rows);
            Debug.Assert(A.cols == cols);
            Debug.Assert(Q_T.rows == rows);
            Debug.Assert(Q_T.cols == rows);
            Debug.Assert(P_T.rows == cols);
            Debug.Assert(P_T.cols == cols);

            Debug.Assert(D.count == cols);
            Debug.Assert(D.count == F.count+1);

            var generic = GenericShader.instance;
            var householder = HouseholderShader.instance;

            GPUVector tempR = new GPUVector(0, rows, 1, tempBuffer);
            // tempA, tempQ and tempP alias!
            GPUMatrix tempA = new GPUMatrix(rows, rows, cols, cols, 1, tempBuffer);
            GPUMatrix tempQ = new GPUMatrix(rows, rows, rows, rows, 1, tempBuffer);
            GPUMatrix tempP = new GPUMatrix(rows, cols, cols, cols, 1, tempBuffer);

            generic.SetMatrixEye(cmd, Q_T);
            generic.SetMatrixEye(cmd, P_T);

            for (int i = 0; i < A.cols; ++i)
            {
                var Rs = tempR.slice(i, A.rows);
                householder.ComputeAlphaAndR(cmd, A.slice(i, A.rows, i, i+1), Rs, D.slice(i, i+1));

                householder.MultiplyHouseholder(cmd, Rs, Q_T.sliceRows(i, Q_T.rows), tempQ.sliceRows(i, Q_T.rows));
                // Copy new data back to Q_T
                generic.CopyMatrix(cmd, tempQ.sliceRows(i, Q_T.rows), Q_T.sliceRows(i, Q_T.rows));

                if (i < A.cols-1)
                {
                    householder.MultiplyHouseholder(cmd, Rs, A.slice(i, A.rows, i+1, A.cols), tempA.slice(i, A.rows, i+1, A.cols));
                    // (A, temp) = (temp, A);
                    generic.CopyMatrix(cmd, tempA.slice(i, A.rows, i+1, A.cols), A.slice(i, A.rows, i+1, A.cols));


                    Rs = tempR.slice(i+1, A.cols);
                    householder.ComputeAlphaAndR(cmd, A.slice(i, i+1, i+1, A.cols).transposed(), Rs, F.slice(i, i+1));

                    householder.MultiplyHouseholder(cmd, Rs, P_T.sliceRows(i+1, P_T.rows), tempP.sliceRows(i+1, P_T.rows));
                    // Copy new data back to P
                    generic.CopyMatrix(cmd, tempP.sliceRows(i+1, P_T.rows), P_T.sliceRows(i+1, P_T.rows));


                    householder.MultiplyHouseholder(cmd, Rs, A.slice(i, A.rows, i+1, A.cols).transposed(), tempA.slice(i, A.rows, i+1, A.cols).transposed());
                    // (A, temp) = (temp, A);
                    generic.CopyMatrix(cmd, tempA.slice(i, A.rows, i+1, A.cols).transposed(), A.slice(i, A.rows, i+1, A.cols).transposed());
                }
            }
        }

        public void ReadbackBidiagonal(CommandBuffer cmd)
        {
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            D_gpu.buffer.GetData(D_cpu.buffer);
            F_gpu.buffer.GetData(F_cpu.buffer);
        }

        private bool asyncReadbackDoneD = true;
        private bool asyncReadbackDoneF = true;

        public delegate void Callback();

        public void AsyncReadbackBidiagonal(CommandBuffer cmd, Callback callback)
        {
            Debug.Assert(asyncReadbackDoneD == true);
            Debug.Assert(asyncReadbackDoneF == true);

            asyncReadbackDoneD = false;
            asyncReadbackDoneF = false;
            cmd.RequestAsyncReadback(D_gpu.buffer, (AsyncGPUReadbackRequest request) => {
                Debug.Assert(request.done);
                Debug.Assert(request.width == D_gpu.count * 4);
                Debug.Assert(request.width == D_cpu.count * 4);
                NativeArray<float> data = request.GetData<float>();
                data.CopyTo(D_cpu.buffer);

                // Assume that this callback is called on the main thread
                Debug.Assert(asyncReadbackDoneD == false);
                asyncReadbackDoneD = true;

                if (asyncReadbackDoneD && asyncReadbackDoneF)
                    callback();
            });
            cmd.RequestAsyncReadback(F_gpu.buffer, (AsyncGPUReadbackRequest request) => {
                Debug.Assert(request.done);
                Debug.Assert(request.width == F_gpu.count * 4);
                Debug.Assert(request.width == F_cpu.count * 4);
                NativeArray<float> data = request.GetData<float>();
                data.CopyTo(F_cpu.buffer);

                // Assume that this callback is called on the main thread
                Debug.Assert(asyncReadbackDoneF == false);
                asyncReadbackDoneF = true;

                if (asyncReadbackDoneD && asyncReadbackDoneF)
                    callback();
            });
        }

        private static float SmallestEigenvalueSym2x2(float D1, float D2, float F)
        {
            /*
            Given D1, D2 >= 0, F
            Find eigenvalues of M
            M = D1 F
                F  D2

            Solve
            (D1 - t) * (D2 - t) - F**2 == 0
            t**2 - (D1 + D2) * t + D1 * D2 - F**2 = 0
            t12 = (D1 + D2)/2 +- sqrt((D1 + D2)**2 / 4 - D1*D2 + F**2)
            */

            return (D1 + D2)/2 - Mathf.Sqrt((D1 + D2)*(D1 + D2) / 4 - D1*D2 + F*F);
        }

        private static float SmallestEigenvalueOfBtB2x2(float D1, float D2, float F)
        {
            /*
            Given D1, D2, F
            Find eigenvalues of B^T B
            B = D1 F
                0  D2
            */
            return SmallestEigenvalueSym2x2(D1*D1, D2*D2, D1*F);
        }

        private struct SolveSVDBidiag2x2Result
        {
            public float s1;
            public float s2;
            public float cosThetaX;
            public float sinThetaX;
            public float cosThetaYt;
            public float sinThetaYt;
        }
        private static SolveSVDBidiag2x2Result SolveSVDBidiag2x2(float D1, float D2, float F1)
        {
            /*
            Given D1, D2, F
            Find SVD of B
            B = D1 F
                0  D2

            Sigma = X^T B Y
            -> X Sigma Y^T = B
            */

            SolveSVDBidiag2x2Result result;

            float E = (D1 + D2) / 2;
            float F = (D1 - D2) / 2;
            float G = F1 / 2;
            float H = -F1 / 2;

            float Q = Mathf.Sqrt(E*E + H*H);
            float R = Mathf.Sqrt(F*F + G*G);

            result.s1 = Q + R;
            result.s2 = Q - R;

            float a1 = Mathf.Atan2(G, F);
            float a2 = Mathf.Atan2(H, E);
            float thetaX = (a2 + a1) / 2;
            float thetaYt = (a2 - a1) / 2;

            result.cosThetaX = Mathf.Cos(thetaX);
            result.sinThetaX = Mathf.Sin(thetaX);
            // X = np.array([[cosPhi, -sinPhi], [sinPhi, cosPhi]])

            result.cosThetaYt = Mathf.Cos(thetaYt);
            result.sinThetaYt = Mathf.Sin(thetaYt);
            // Y_T = np.array([[cosTheta, -sinTheta], [sinTheta, cosTheta]])

            return result;
        }

        private static (float, float) Normalize(float x, float y)
        {
            float len = Mathf.Sqrt(x*x + y*y);
            return (x / len, y / len);
        }

        private struct QRIterationGivensRotationPair
        {
            public float cosThetaXt;
            public float sinThetaXt;
            public float cosThetaYt;
            public float sinThetaYt;
        }
        private QRIterationGivensRotationPair[] QRIteration(CPUVector D, CPUVector F, float sigma2)
        {
            /*
            Sigma = X^T B Y
            -> X Sigma Y^T = B

            X^T = ... J6 J4 J2
            Y = J1 J3 J5 ...
            */

            Debug.Assert(D.count == F.count + 1);

            int N = D.count;
            float bulge = 0;

            // TODO only generate two sequences of cosTheta/sinTheta pairs
            // X = np.eye(N)
            // Y_T = np.eye(N)

            QRIterationGivensRotationPair[] givensRots = new QRIterationGivensRotationPair[N-1];
            // List<(float, float)> givensRotsXt = new List<(float, float)>();
            // List<(float, float)> givensRotsYt = new List<(float, float)>();

            {
                // Choose theta_1 such that for T = B.T @ B - sigma2 * eye
                // (J^T @ T)[1, 0] = 0
                // tanTheta = D[0] * F[0] / (sigma2 - D[0]**2)
                float cosTheta = sigma2 - D[0]*D[0];
                float sinTheta = D[0] * F[0];
                (cosTheta, sinTheta) = Normalize(cosTheta, sinTheta);

                // Multiply J1 from right to B
                // Multiply J1 from right to Y
                // Multiply J1t from left to Yt
                // Ji = np.array([[cosTheta, sinTheta], [-sinTheta, cosTheta]])
                // Y_T[0:2, :] = Ji.T @ Y_T[0:2, :]
                // givensRotsYt.Add((cosTheta, -sinTheta)); // NOTE: transposed Ji
                givensRots[0].cosThetaYt = cosTheta;
                givensRots[0].sinThetaYt = -sinTheta;

                // Apply J1 to B (B' = B @ J1)
                float D0 = D[0];
                float D1 = D[1];
                float F0 = F[0];
                //  Update D, F
                D[0] = D0 * cosTheta - F0 * sinTheta;
                D[1] = D1 * cosTheta;
                F[0] = F0 * cosTheta + D0 * sinTheta;
                // Chase the bulge
                bulge = -D1 * sinTheta; // (B @ J1)[1, 0];
            }

            for (int i = 0; i < N-1; ++i)
            {
                {
                    // 1. Bulge is below diagonal at [i+1, i]
                    // Choose sinThetaI, cosThetaI such that (old) bulge becomes 0
                    // Introduces new bulge above diagonal at [i, i+2]
                    // Multiply Ji from left

                    // tanTheta = bulge / D[i]
                    float cosTheta = D[i];
                    float sinTheta = bulge;
                    (cosTheta, sinTheta) = Normalize(cosTheta, sinTheta);

                    // Multiply Ji from left to B
                    // Multiply Ji from left to Xt
                    // Multiply Jit from right to X
                    // Ji = np.array([[cosTheta, sinTheta], [-sinTheta, cosTheta]])
                    // X[:, i:i+2] = X[:, i:i+2] @ Ji.T
                    // givensRotsXt.Add((cosTheta, sinTheta));
                    givensRots[i].cosThetaXt = cosTheta;
                    givensRots[i].sinThetaXt = sinTheta;

                    // Apply Ji to B (B' = Ji @ B)
                    float D0 = D[i];
                    float D1 = D[i+1];
                    float F0 = F[i];
                    //  Update D, F
                    D[i] = cosTheta * D0 + sinTheta * bulge;
                    D[i+1] = cosTheta * D1 - sinTheta * F0;
                    F[i] = cosTheta * F0 + sinTheta * D1;
                    if (i+1 < N-1)
                    {
                        float F1 = F[i+1];
                        F[i+1] = cosTheta * F1;
                        bulge = sinTheta * F1;
                    }
                    else
                    {
                        // print("i+1 = N-1")
                        bulge = 0;
                        break;
                    }
                }

                {
                    // 2. Bulge is above diagonal at [i, i+2]
                    // Choose sinTheta, cosTheta such that (old) bulge becomes 0
                    // Introduces new bulge below diagonal at [i+2, i+1]
                    // Multiply Ji from right

                    // tanTheta = -bulge / F[i]
                    float cosTheta = F[i];
                    float sinTheta = -bulge;
                    (cosTheta, sinTheta) = Normalize(cosTheta, sinTheta);

                    // Multiply Ji from right to B
                    // Multiply Ji from right to Y
                    // Multiply Jit from left to Yt
                    // Ji = np.array([[cosTheta, sinTheta], [-sinTheta, cosTheta]])
                    // Y_T[i+1:i+3, :] = Ji.T @ Y_T[i+1:i+3, :]
                    // givensRotsYt.Add((cosTheta, -sinTheta)); // NOTE: transposed Ji
                    givensRots[i+1].cosThetaYt = cosTheta;
                    givensRots[i+1].sinThetaYt = -sinTheta;

                    // Apply Ji to B (B' = B @ Ji)
                    float D1 = D[i+1];
                    float F0 = F[i];
                    float F1 = F[i+1];
                    D[i+1] = cosTheta * D1 - sinTheta * F1;
                    F[i] = cosTheta * F0 - sinTheta * bulge;
                    F[i+1] = cosTheta * F1 + sinTheta * D1;
                    if (i+2 < N)
                    {
                        float D2 = D[i+2];
                        D[i+2] = cosTheta * D2;
                        bulge = -sinTheta * D2;
                    }
                    else
                    {
                        // print("i+2 = N")
                        bulge = 0;
                        break;
                    }
                }
            }

            return givensRots;
        }

        public void Diagonalize(CommandBuffer cmd)
        {
            // Factorize Bidiag(D, F) = X @ Sigma @ Y_T
            // Sigma = X^T B Y
            // -> X Sigma Y^T = B

            // A = Q @ B @ Pt
            // B = Qt @ A @ P
            // Sigma = Xt @ B @ Y = Xt @ Qt @ A @ P @ Y

            // X is extended from right: X' = X @ X_local
            // Xt is extended from left: Xt' = Xt_local @ Xt
            // Since we want Xt @ Qt, we can just update Qt directly instead of computing Xt!

            // Yt is extended from left: Yt' = local_Yt @ Yt
            // Y is extended from right: Y' = Y @ local_Y
            // Since we want P @ Y, we can just update P directly instead of computing Y!

            var givens = GivensShader.instance;

            CPUVector D = D_cpu;
            CPUVector F = F_cpu;

            GPUMatrix Xt = Q_T;
            GPUMatrix Yt = P_T;

            int N = D.count;

            int maxitn = 15 * N * N;
            float eps = 1e-8f;

            // TODO set values close to zero to zero

            int K1 = 0;
            int K2 = N-1; // last index in D

            int prevK2 = 0;
            bool forward = true;

            int rotationCount = 0;

            for (int i = 0; i < maxitn; ++i)
            {
                if (K2 <= 0)
                    break;
                if (i == maxitn-1)
                    throw new System.Exception("Diagonalization did not converge!");

                // Find largest K1 less than K2 for which F[K1] is less than some tolerance, if none, K1 = -1
                K1 = K2-1;
                while (K1 >= 0 && Mathf.Abs(F[K1]) > eps)
                    --K1;

                if (K1 >= 0)
                {
                    // F[K1] must be close to zero
                    F[K1] = 0;

                    // Found a 1x1 block (already solved)
                    if (K1 == K2-1)
                    {
                        K2 = K2 - 1;
                        continue;
                    }
                }
                K1++;

                if (K1 == K2 - 1)
                {
                    // Solve 2x2 matrix explicitly!

                    SolveSVDBidiag2x2Result result = SolveSVDBidiag2x2(D[K1], D[K1+1], F[K1]);

                    // Update D and F
                    F[K1] = 0;
                    D[K1] = result.s1;
                    D[K1+1] = result.s2;

                    // Update X and Y_T
                    // X[:, K1:K2+1] = X[:, K1:K2+1] @ local_X
                    // Y_T[K1:K2+1, :] = local_Y_T @ Y_T[K1:K2+1, :]
                    givens.RotateMatFromLeft(cmd, Xt.sliceRows(K1, K2+1), result.cosThetaX, -result.sinThetaX); // NOTE: have to transpose rotation here
                    givens.RotateMatFromLeft(cmd, Yt.sliceRows(K1, K2+1), result.cosThetaYt, result.sinThetaYt);

                    K2 -= 2;
                    continue;
                }

                // If current matrix section does not overlap previous iteration, determine shift direction  based on D[K1] <? D[K2]
                /*
                if (K2 != prevK2)
                    forward = D[K1] < D[K2];
                prevK2 = K2;
                */
                // Just doing forward iteration uses the least number of iterations and gives the lowest numeric error!
                forward = true;

                // If shift direction is down, compute shift for QR iteration by choosing smallest eigenvalue of trailing 2x2 submatrix of T = B[K1:K2+1, K1:K2+1].T @ B[K1:K2+1, K1:K2+1]
                // Else use the smallest eigenvalue of the leading 2x2 submatrix of T

                if (forward)
                {
                    // trailing 2x2 submatrix of B^t * B
                    float sigma2 = SmallestEigenvalueOfBtB2x2(D[K2-1], D[K2], F[K2-1]);
                    QRIterationGivensRotationPair[] givensRots = QRIteration(D.slice(K1, K2+1), F.slice(K1, K2), sigma2);
                    // X[:, K1:K2+1] = X[:, K1:K2+1] @ local_X
                    // Y_T[K1:K2+1, :] = local_Y_T @ Y_T[K1:K2+1, :]
                    rotationCount += givensRots.Length;
                    for (int j = 0; j < givensRots.Length; ++j)
                    {
                        // TODO Why do we have to transpose here?!
                        givens.RotateMatFromLeft(cmd, Xt.sliceRows(K1+j, K1+j+2/*K2+1*/), givensRots[j].cosThetaXt, -givensRots[j].sinThetaXt);
                        givens.RotateMatFromLeft(cmd, Yt.sliceRows(K1+j, K1+j+2/*K2+1*/), givensRots[j].cosThetaYt, -givensRots[j].sinThetaYt);
                    }
                }
                else
                {
                    // leading 2x2 submatrix of B^t * B
                    float sigma2 = SmallestEigenvalueOfBtB2x2(D[K1], D[K1+1], F[K1]);
                    // local_X, local_Y_T = QRIterationBackward(D[K1:K2+1], F[K1:K2], sigma2)
                    // NOTE: givensRotsYt and givensRotsXt are swaped here wrt. forward iteration!
                    QRIterationGivensRotationPair[] givensRots = QRIteration(D.slice(K1, K2+1).reverse(), F.slice(K1, K2).reverse(), sigma2);
                    rotationCount += givensRots.Length;
                    for (int j = 0; j < givensRots.Length; ++j)
                    {
                        // The givens rotations applied here are transposed for the forward pass! (the forward pass is already transposed for some reason...)
                        // Note also that the Yt rots are applied to Xt and vice versa!
                        givens.RotateMatFromLeft(cmd, Xt.sliceRows(K2-j-1, K2-j+1), givensRots[j].cosThetaYt, givensRots[j].sinThetaYt);
                        givens.RotateMatFromLeft(cmd, Yt.sliceRows(K2-j-1, K2-j+1), givensRots[j].cosThetaXt, givensRots[j].sinThetaXt);
                    }
                }
            }
            // Debug.Log($"Rotation count {rotationCount}");
        }

        public void Release()
        {
            D_gpu.buffer.Release();
            F_gpu.buffer.Release();

            Q_T.buffer.Release();
            P_T.buffer.Release();

            tempBuffer.Release();
        }
    }
}
