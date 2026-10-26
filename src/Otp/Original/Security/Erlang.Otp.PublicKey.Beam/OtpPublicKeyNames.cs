namespace Erlang.Otp;

internal static class OtpPublicKeyNames
{
    internal const string Prefix = "Erlang.Otp.PublicKey.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string Application = "public_key.app";
    internal const string Upgrade = "public_key.appup";
    internal const string MissingResource = "Original public_key library resource is missing";
    internal const string UnknownModule = "Module is not included in the original public_key library";
    internal const string HashMismatch = "Original public_key library resource hash mismatch";
}
