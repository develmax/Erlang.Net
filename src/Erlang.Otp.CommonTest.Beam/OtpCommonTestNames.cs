namespace Erlang.Otp;

internal static class OtpCommonTestNames
{
    internal const string Prefix = "Erlang.Otp.CommonTest.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string Application = "common_test.app";
    internal const string Upgrade = "common_test.appup";
    internal const string MissingResource = "Original common_test library resource is missing";
    internal const string UnknownModule = "Module is not included in the original common_test library";
    internal const string HashMismatch = "Original common_test library resource hash mismatch";
    internal const string SupportPrefix = "support/";
    internal const string UnknownSupportFile = "File is not included in the original common_test support resources";
}
