namespace Erlang;

internal static class NifNativeBinaryNames
{
    internal const int MaximumTermBytes = int.MaxValue / 8;
    internal const int SmallSubBinaryBytes = 64;
    internal const int DescriptorBytes = 40;
    internal const string TupleShape = "A NIF tuple borrow requires a tuple value.";
    internal const string NullDescriptor = "A NIF binary operation requires a descriptor pointer.";
    internal const string NullTermOutput = "A new NIF binary requires a term output pointer.";
    internal const string NullData = "A nonempty NIF binary requires a data pointer.";
    internal const string ForeignStorage = "Unknown, transferred or released NIF binary ownership.";
    internal const string DescriptorMismatch = "NIF binary descriptor fields differ from their owned allocation.";
    internal const string ExpiredStorage = "NIF binary storage has been released.";
    internal const string ReferenceUnderflow = "NIF binary environment reference underflow.";
    internal const string TermSizeBoundary = "The NIF binary exceeds the current managed BitString bit-length representation.";
    internal const string SubBinaryRange = "A NIF subbinary requires a bitstring and a valid whole-byte range.";
}
