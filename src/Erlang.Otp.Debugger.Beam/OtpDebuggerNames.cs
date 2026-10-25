namespace Erlang.Otp;

internal static class OtpDebuggerNames
{
    internal const string Prefix = "Erlang.Otp.Debugger.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string Application = "debugger.app";
    internal const string Upgrade = "debugger.appup";
    internal const string MissingResource = "Original debugger library resource is missing";
    internal const string UnknownModule = "Module is not included in the original debugger library";
    internal const string HashMismatch = "Original debugger library resource hash mismatch";
    internal const string SupportPrefix = "support/";
    internal const string UnknownSupportFile = "File is not included in the original debugger support resources";
}
