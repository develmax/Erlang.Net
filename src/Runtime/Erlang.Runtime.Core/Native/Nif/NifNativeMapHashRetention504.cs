namespace Erlang;

internal sealed class NifNativeMapHashRetention504(
    HamtHashLeases429 ownership,
    NifNativeMapHashRetention504? previous = null
)
{
    // Shared tokens register with the existing runtime owner and drain on Close.
    internal HamtHashLeases429 Ownership { get; } = ownership;
    internal NifNativeMapHashRetention504? Previous { get; } = previous;
}
