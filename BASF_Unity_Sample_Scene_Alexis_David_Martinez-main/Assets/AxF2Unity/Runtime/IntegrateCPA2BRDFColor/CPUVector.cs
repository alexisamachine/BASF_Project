// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using UnityEngine;

namespace AxF2Unity
{
    public class CPUVector
    {
        private int m_start;

        private int m_count;

        private int m_stride;

        private float[] m_buffer;

        public int start => m_start;
        public int count => m_count;
        public int stride => m_stride;

        public float[] buffer => m_buffer;

        public CPUVector(int count)
        {
            this.m_start = 0;
            this.m_count = count;
            this.m_stride = 1;
            this.m_buffer = new float[count];
        }

        public CPUVector(int start, int count, int stride, float[] buffer)
        {
            Debug.Assert(start + (count-1) * stride < buffer.Length);
            this.m_start = start;
            this.m_count = count;
            this.m_stride = stride;
            this.m_buffer = buffer;
        }

        public float this[int i]
        {
            get {
                Debug.Assert(i >= 0 && i < this.count);
                return this.buffer[this.start + i * this.stride];
            }
            set {
                Debug.Assert(i >= 0 && i < this.count);
                this.buffer[this.start + i * this.stride] = value;
            }
        }

        public void reset(int count)
        {
            Debug.Assert(count <= m_buffer.Length);
            this.m_start = 0;
            this.m_count = count;
            this.m_stride = 1;
        }

        public CPUVector slice(int begin, int end)
        {
            Debug.Assert(end - begin > 0);
            Debug.Assert(end - begin <= count);
            return new CPUVector(this.start + begin * this.stride, end-begin, this.stride, buffer);
        }

        public CPUVector reverse()
        {
            return new CPUVector(this.start + (this.count-1) * this.stride, this.count, -this.stride, buffer);
        }

        public override string ToString()
        {
            var builder = new System.Text.StringBuilder();

            for (int i = 0; i < count; ++i)
            {
                builder.Append($"{buffer[start + i * stride]} ");
            }

            return builder.ToString();
        }

    }
}
