namespace Erlang.NifNativeBackend;

internal static class NifNativeElfCapsuleHostNames
{
    internal const string Failure = "Metadata ELF capsule contract failed";
    internal const int HeaderType = 16;
    internal const int ProgramOffset = 32;
    internal const int ProgramShape = 54;
    internal const int SegmentOffsetField = 8;
    internal const int FlagField = 4;
    internal const uint Execute = 1;
    internal static readonly byte[] Bridge = System.Text.Encoding.ASCII.GetBytes("/proc/self/fd/77");
    internal static readonly byte[] Reallocation = System.Text.Encoding.ASCII.GetBytes("/proc/self/fd/78");
    internal static readonly byte[] RawTarget = [47, 116, 109, 112, 47, 255, 46, 115, 111];
}
