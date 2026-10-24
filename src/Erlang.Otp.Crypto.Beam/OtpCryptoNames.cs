namespace Erlang.Otp;

internal static class OtpCryptoNames
{
    internal const string Prefix = "Erlang.Otp.Crypto.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string Application = "crypto.app";
    internal const string Upgrade = "crypto.appup";
    internal const string MissingResource = "Original crypto library resource is missing";
    internal const string UnknownModule = "Module is not included in the original crypto library";
    internal const string HashMismatch = "Original crypto library resource hash mismatch";
}
