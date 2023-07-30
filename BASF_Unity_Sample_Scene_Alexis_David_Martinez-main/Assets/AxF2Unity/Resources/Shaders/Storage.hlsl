#ifndef STORAGE_HLSL
#define STORAGE_HLSL

#define DECLARE_MATRIX_INTERNAL(buffer, scalar, mat) \
    buffer<scalar> mat; \
    int mat##Start; \
    int mat##RowStride; \
    int mat##RowCount; \
    int mat##ColStride; \
    int mat##ColCount;

#define DECLARE_MATRIX(mat) DECLARE_MATRIX_INTERNAL(StructuredBuffer, float, mat)
#define DECLARE_MATRIX_RW(mat) DECLARE_MATRIX_INTERNAL(RWStructuredBuffer, float, mat)

#define MATRIX_AT(mat, r, c) mat[mat##Start + r * mat##RowStride + c * mat##ColStride]
#define MATRIX_ROW_COUNT(mat) mat##RowCount
#define MATRIX_COL_COUNT(mat) mat##ColCount

#define DECLARE_VECTOR_INTERNAL(buffer, scalar, vec) \
    RWStructuredBuffer<float> vec; \
    int vec##Start; \
    int vec##Stride; \
    int vec##Count;

#define DECLARE_VECTOR(vec) DECLARE_VECTOR_INTERNAL(StructuredBuffer, float, vec)
#define DECLARE_VECTOR_RW(vec) DECLARE_VECTOR_INTERNAL(RWStructuredBuffer, float, vec)

#define VECTOR_AT(vec, i) vec[vec##Start + i * vec##Stride]
#define VECTOR_SIZE(vec) vec##Count

#endif // STORAGE_HLSL
