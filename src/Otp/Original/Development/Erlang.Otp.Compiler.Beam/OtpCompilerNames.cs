namespace Erlang.Otp;

internal static class OtpCompilerNames
{
    internal const string Prefix = "Erlang.Otp.Compiler.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string Application = "compiler.app";
    internal const string Upgrade = "compiler.appup";
    internal const string MissingResource = "Original compiler library resource is missing";
    internal const string UnknownModule = "Module is not included in the original compiler library";
    internal const string HashMismatch = "Original compiler library resource hash mismatch";
}
