namespace Erlang;

internal static class HeapBitStringLayout
{
    internal const int MaximumBytes = 64;
    internal const int BitsPerByte = 8;
    internal const int HeaderWords = 1;
    internal const int SizeWords = 1;
    internal const int HeaderShift = 6;
    internal const nuint Subtag = 8u << 2;
    internal const string Invalid = "The process heap bitstring header is foreign, stale, malformed, or outside its physical allocation.";

    internal static int DataWords(int bitLength)
        => ((bitLength + BitsPerByte - 1) / BitsPerByte + IntPtr.Size - 1) / IntPtr.Size;

    internal static nuint Header(int bitLength)
        => ((nuint)(SizeWords + DataWords(bitLength)) << HeaderShift) | Subtag;
}
