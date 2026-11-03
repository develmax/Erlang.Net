namespace Erlang.NifNativeBackend;

public static class HeapBitStringCaseNames
{
    internal const string Failure = "Process heap bitstring storage, payload or retirement contract differs";
    public const string Physical = "runtime/statistics-process-heap-bits-physical-inline-birth-and-reclaim";
    public const string ReferenceShape = "runtime/statistics-process-heap-bits-preserve-small-reference-shape";
    public const string Transfer = "runtime/statistics-process-heap-bits-transfer-detached-escape-and-bits";
    internal const int ReferenceParentBytes = 256;
    internal const int ReferenceByteOffset = 1;
    internal const int ReferenceViewBits = 40;
    internal const string ReferenceFactory = "FromOwnedBacking";
    internal const string HasSubBits = "HasSubBitsStorage430";
    internal const string ReferencedBytes = "ReferencedByteSize";
    internal const int BitsPerByte = 8;
    internal const int ByteRounding = 7;
    internal const int PatternStep = 37;
    internal const int PatternOrigin = 11;
    internal const int TransferBits = 13;
    internal const int SliceOffset = 3;
    internal const int SliceBits = 7;
    internal const int SliceAllocationWords = 3;
    internal const string HeapStorage = "OwnedHeapBits";
    internal const string ResourceStorage = "OwnedStorage577";
    internal const string ReadBit = "Bit";
    internal static readonly byte[] TransferBytes = [0x96, 0x58];
    // Independent pinned erl_bits.h/erl_term.h vectors: bits, words32, header32, words64, header64.
    internal static readonly HeapBitStringVector[] Vectors =
    [
        new(
            0,
            2,
            0x60,
            2,
            0x60
        ),
        new(
            1,
            3,
            0xA0,
            3,
            0xA0
        ),
        new(
            7,
            3,
            0xA0,
            3,
            0xA0
        ),
        new(
            8,
            3,
            0xA0,
            3,
            0xA0
        ),
        new(
            9,
            3,
            0xA0,
            3,
            0xA0
        ),
        new(
            63,
            4,
            0xE0,
            3,
            0xA0
        ),
        new(
            64,
            4,
            0xE0,
            3,
            0xA0
        ),
        new(
            65,
            5,
            0x120,
            4,
            0xE0
        ),
        new(
            511,
            18,
            0x460,
            10,
            0x260
        ),
        new(
            512,
            18,
            0x460,
            10,
            0x260
        )
    ];
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
}
