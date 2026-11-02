namespace Erlang;

internal static class ExternalIdentifierLayout
{
    internal const int HeadWords = 3;
    internal const int HeaderWords = 1;
    internal const int NodeWord = 1;
    internal const int NextWord = 2;
    internal const int DataWord = 3;
    internal const int UintBits = 32;
    internal const int HeaderShift = 6;
    internal const nuint PidTag = 0x30;
    internal const nuint PortTag = 0x34;
    internal const nuint ReferenceTag = 0x38;
    internal const string Invalid = "Invalid external identifier descriptor";
    internal const string Platform = "External identifier descriptors require little-endian 64-bit words";

    internal static bool IsKind(NativeOffheapKind576 kind) => kind is NativeOffheapKind576.ExternalPid
        or NativeOffheapKind576.ExternalPort or NativeOffheapKind576.ExternalReference;

    internal static nuint Header(NativeOffheapKind576 kind, int dataWords)
        => ((nuint)(HeadWords + dataWords - 1) << HeaderShift) | (kind switch
        {
            NativeOffheapKind576.ExternalPid => PidTag,
            NativeOffheapKind576.ExternalPort => PortTag,
            NativeOffheapKind576.ExternalReference => ReferenceTag,
            _ => throw new InvalidOperationException(Invalid)
        });
}
