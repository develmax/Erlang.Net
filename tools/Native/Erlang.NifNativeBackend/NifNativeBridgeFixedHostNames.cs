namespace Erlang.NifNativeBackend;

internal static class NifNativeBridgeFixedHostNames
{
    internal const string Failure = "Fixed Linux bridge callback contract failed";
    internal const string Factory = "CreateTable";
    internal const string Module = "fixed_bridge_host_contract";
    internal const int ExpectedVersion = 4;
    internal const int ExpectedTableBytes = 360;
    internal const int LegacyPointerCount = 12;
    internal const int AddedPointerCount = 32;
    internal const int MetadataBytes = 8;
    internal const int ExpectedBadArgumentOutput = 3152;
    internal const int ExpectedSystemLimitOutput = 15440;
}
