// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using UnityEngine;

namespace AxF2Unity
{
    public struct GPUMatrix
    {
        private int m_start;

        private int m_rows;
        private int m_cols;

        private int m_rowStride;
        private int m_colStride;

        private ComputeBuffer m_buffer;

        public int start => m_start;
        public int rows => m_rows;
        public int cols => m_cols;
        public int rowStride => m_rowStride;
        public int colStride => m_colStride;

        public ComputeBuffer buffer => m_buffer;

        public GPUMatrix(int rows, int cols)
        {
            this.m_start = 0;
            this.m_rows = rows;
            this.m_cols = cols;
            this.m_rowStride = cols;
            this.m_colStride = 1;
            this.m_buffer = new ComputeBuffer(rows * cols, stride: 4, type: ComputeBufferType.Structured);
        }

        public GPUMatrix(int start, int rows, int cols, int rowStride, int colStride, ComputeBuffer buffer)
        {
            this.m_start = start;
            this.m_rows = rows;
            this.m_cols = cols;
            this.m_rowStride = rowStride;
            this.m_colStride = colStride;
            this.m_buffer = buffer;
        }

        public void reset(int rows, int cols)
        {
            Debug.Assert(rows * cols <= m_buffer.count);
            this.m_start = 0;
            this.m_rows = rows;
            this.m_cols = cols;
            this.m_rowStride = cols;
            this.m_colStride = 1;
        }

        public GPUMatrix transposed()
        {
            return new GPUMatrix(start, cols, rows, colStride, rowStride, buffer);
        }

        public GPUMatrix reshape(int newRows, int newCols)
        {
            Debug.Assert(newRows * newCols == this.rows * this.cols);
            Debug.Assert(this.rowStride == this.cols * this.colStride);

            return new GPUMatrix(this.start, newRows, newCols, newCols * this.colStride, this.colStride, this.buffer);
        }

        public GPUMatrix slice(int rowBegin, int rowEnd, int colBegin, int colEnd)
        {
            Debug.Assert(rowEnd - rowBegin > 0);
            Debug.Assert(rowEnd - rowBegin <= this.rows);
            Debug.Assert(colEnd - colBegin > 0);
            Debug.Assert(colEnd - colBegin <= this.cols);
            return new GPUMatrix(this.start + rowBegin * this.rowStride + colBegin * this.colStride, rowEnd-rowBegin, colEnd-colBegin, this.rowStride, this.colStride, this.buffer);
        }

        public GPUMatrix sliceRows(int rowBegin, int rowEnd)
        {
            Debug.Assert(rowEnd - rowBegin > 0);
            Debug.Assert(rowEnd - rowBegin <= this.rows);
            return new GPUMatrix(this.start + rowBegin * this.rowStride, rowEnd-rowBegin, this.cols, this.rowStride, this.colStride, this.buffer);
        }

        public GPUMatrix sliceCols(int colBegin, int colEnd)
        {
            Debug.Assert(colEnd - colBegin > 0);
            Debug.Assert(colEnd - colBegin <= this.cols);
            return new GPUMatrix(this.start + colBegin * this.colStride, this.rows, colEnd-colBegin, this.rowStride, this.colStride, this.buffer);
        }

        public GPUVector sliceRowVector(int row)
        {
            Debug.Assert(row >= 0);
            Debug.Assert(row < this.rows);
            return new GPUVector(this.start + row * this.rowStride, this.cols, this.colStride, this.buffer);
        }

        public GPUVector sliceColVector(int col)
        {
            Debug.Assert(col >= 0);
            Debug.Assert(col < this.cols);
            return new GPUVector(this.start + col * this.colStride, this.rows, this.rowStride, this.buffer);
        }

        public override string ToString()
        {
            var builder = new System.Text.StringBuilder();

            float[] data = new float[buffer.count];
            buffer.GetData(data);
            for (int r = 0; r < rows; ++r)
            {
                for (int c = 0; c < cols; ++c)
                {
                    builder.Append($"{data[start + r * rowStride + c * colStride]} ");
                }
                builder.Append("\n");
            }

            return builder.ToString();
        }
    }
}
