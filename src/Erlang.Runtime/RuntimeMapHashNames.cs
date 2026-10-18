namespace Erlang;

internal static class RuntimeMapHashNames
{
    internal const int PointerAlignmentBits = 3;
    internal const ulong PointerAlignment = 1UL << PointerAlignmentBits;
    internal const int PidNumberBits = 28;
    internal const ulong PidNumberMask = (1UL << PidNumberBits) - 1;
    internal const int WirePidSerialShift = 32;
    internal const int LocalPayloadBits = 60;
    internal const ulong LocalPayloadMask = (1UL << LocalPayloadBits) - 1;
    internal const ulong PidTag = 3;
    internal const ulong PortTag = 7;
    internal const int ImmediateShift = 4;
    internal const int AtomShift = 6;
    internal const ulong AtomTag = 11;
    internal const ulong HostFunctionExtension = 22;
    internal const int MaximumEnvironment = 255;
}
