namespace Erlang.Otp;

internal static class OtpReltoolNames
{
    internal const string Prefix = "Erlang.Otp.Reltool.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string Application = "reltool.app";
    internal const string Upgrade = "reltool.appup";
    internal const string MissingResource = "Original reltool library resource is missing";
    internal const string UnknownModule = "Module is not included in the original reltool library";
    internal const string HashMismatch = "Original reltool library resource hash mismatch";
    internal const string SupportPrefix = "support/";
    internal const string UnknownSupportFile = "File is not included in the original reltool support resources";
}
