namespace Erlang.Otp;

internal static class OtpKernelNames
{
    internal const string Prefix = "Erlang.Otp.Kernel.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string Application = "kernel.app";
    internal const string Upgrade = "kernel.appup";
    internal const string MissingResource = "Original kernel library resource is missing";
    internal const string UnknownModule = "Module is not included in the original kernel library";
    internal const string HashMismatch = "Original kernel library resource hash mismatch";
}
