namespace Erlang.Otp;

internal static class OtpSaslNames
{
    internal const string Prefix = "Erlang.Otp.Sasl.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string Application = "sasl.app";
    internal const string Upgrade = "sasl.appup";
    internal const string MissingResource = "Original sasl library resource is missing";
    internal const string UnknownModule = "Module is not included in the original sasl library";
    internal const string HashMismatch = "Original sasl library resource hash mismatch";
}
