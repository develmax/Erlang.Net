namespace Erlang.Otp;

internal static class OtpOsMonNames
{
    internal const string Prefix = "Erlang.Otp.OsMon.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string Application = "os_mon.app";
    internal const string Upgrade = "os_mon.appup";
    internal const string MissingResource = "Original os_mon library resource is missing";
    internal const string UnknownModule = "Module is not included in the original os_mon library";
    internal const string HashMismatch = "Original os_mon library resource hash mismatch";
    internal const string SupportPrefix = "support/";
    internal const string UnknownSupportFile = "File is not included in the original os_mon support resources";
}
