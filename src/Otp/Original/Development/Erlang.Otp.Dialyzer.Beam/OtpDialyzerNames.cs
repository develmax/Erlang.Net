namespace Erlang.Otp;

internal static class OtpDialyzerNames
{
    internal const string Prefix = "Erlang.Otp.Dialyzer.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string Application = "dialyzer.app";
    internal const string Upgrade = "dialyzer.appup";
    internal const string MissingResource = "Original dialyzer library resource is missing";
    internal const string UnknownModule = "Module is not included in the original dialyzer library";
    internal const string HashMismatch = "Original dialyzer library resource hash mismatch";
    internal const string SupportPrefix = "support/";
    internal const string UnknownSupportFile = "File is not included in the original dialyzer support resources";
}
