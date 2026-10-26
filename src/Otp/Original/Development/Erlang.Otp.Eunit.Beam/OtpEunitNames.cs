namespace Erlang.Otp;

internal static class OtpEunitNames
{
    internal const string Prefix = "Erlang.Otp.Eunit.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string Application = "eunit.app";
    internal const string Upgrade = "eunit.appup";
    internal const string MissingResource = "Original eunit library resource is missing";
    internal const string UnknownModule = "Module is not included in the original eunit library";
    internal const string HashMismatch = "Original eunit library resource hash mismatch";
}
