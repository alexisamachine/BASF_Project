// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

namespace AxF2Unity
{
    /// <summary>
    /// Define the uv tiling mode for the AxF CPA2 flakes.
    /// <c>Mirroring</c> is the brick-like tiling pattern proposed in the AxF documentation.
    /// <c>Randomized</c> divides the uv space into uniform grid cells, for which a pseudo-random translation and rotation is applied.
    /// </summary>
    public enum UVTilingMode
    {
        None,
        Mirroring,
        Randomized
    };
}
