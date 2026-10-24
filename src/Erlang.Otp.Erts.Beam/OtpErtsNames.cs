namespace Erlang.Otp;

internal static class OtpErtsNames
{
    internal const string Prefix = "Erlang.Otp.Erts.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string Application = "erts.app";
    internal const string MissingResource = "Original erts library resource is missing";
    internal const string UnknownModule = "Module is not included in the original erts library";
    internal const string HashMismatch = "Original erts library resource hash mismatch";
}
