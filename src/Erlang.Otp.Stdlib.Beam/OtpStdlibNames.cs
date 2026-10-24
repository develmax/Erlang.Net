namespace Erlang.Otp;

internal static class OtpStdlibNames
{
    internal const string ResourcePrefix = "Erlang.Otp.Stdlib.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string MissingResource = "Original OTP library resource is missing";
    internal const string UnknownModule = "Module is not included in the original stdlib library";
    internal const string HashMismatch = "Original OTP library image hash mismatch";
    internal const string ExplicitDependencies = "This module requires an explicit dependency selection; the packaged image does not establish a complete startup closure";
}
