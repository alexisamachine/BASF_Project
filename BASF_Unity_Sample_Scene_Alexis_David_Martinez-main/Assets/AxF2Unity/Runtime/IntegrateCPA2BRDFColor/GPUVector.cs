// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using UnityEngine;

namespace AxF2Unity
{
    public struct GPUVector
    {
        private int m_start;

        private int m_count;

        private int m_stride;

        private ComputeBuffer m_buffer;

        public int start => m_start;
        public int count => m_count;
        public int stride => m_stride;

        public ComputeBuffer buffer => m_buffer;

        public GPUVector(int count)
        {
            this.m_start = 0;
            this.m_count = count;
            this.m_stride = 1;
            this.m_buffer = new ComputeBuffer(count, stride: 4, type: ComputeBufferType.Structured);
        }

        public GPUVector(int start, int count, int stride, ComputeBuffer buffer)
        {
            this.m_start = start;
            this.m_count = count;
            this.m_stride = stride;
            this.m_buffer = buffer;
        }

        public void reset(int count)
        {
            Debug.Assert(count <= m_buffer.count);
            this.m_start = 0;
            this.m_count = count;
            this.m_stride = 1;
        }

        public GPUVector slice(int begin, int end)
        {
            Debug.Assert(end - begin > 0);
            Debug.Assert(end - begin <= count);
            return new GPUVector(this.start + begin * this.stride, end-begin, this.stride, buffer);
        }

        public GPUVector reverse()
        {
            return new GPUVector(this.start + (this.count-1) * this.stride, this.count, -this.stride, buffer);
        }

        public void SetData(float[] data)
        {
            buffer.SetData(data);
        }

        public void GetData(float[] data)
        {
            buffer.GetData(data);
        }

        public override string ToString()
        {
            var builder = new System.Text.StringBuilder();

            float[] data = new float[buffer.count];
            buffer.GetData(data);
            for (int i = 0; i < count; ++i)
            {
                builder.Append($"{data[start + i * stride]} ");
            }

            return builder.ToString();
        }
    }
}
