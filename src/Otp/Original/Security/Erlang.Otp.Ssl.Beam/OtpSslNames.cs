namespace Erlang.Otp;

internal static class OtpSslNames
{
    internal const string Prefix = "Erlang.Otp.Ssl.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string Application = "ssl.app";
    internal const string Upgrade = "ssl.appup";
    internal const string MissingResource = "Original ssl library resource is missing";
    internal const string UnknownModule = "Module is not included in the original ssl library";
    internal const string HashMismatch = "Original ssl library resource hash mismatch";
}
