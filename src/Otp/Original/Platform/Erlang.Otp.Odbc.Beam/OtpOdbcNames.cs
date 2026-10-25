namespace Erlang.Otp;

internal static class OtpOdbcNames
{
    internal const string Prefix = "Erlang.Otp.Odbc.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string Application = "odbc.app";
    internal const string Upgrade = "odbc.appup";
    internal const string MissingResource = "Original odbc library resource is missing";
    internal const string UnknownModule = "Module is not included in the original odbc library";
    internal const string HashMismatch = "Original odbc library resource hash mismatch";
    internal const string SupportPrefix = "support/";
    internal const string UnknownSupportFile = "File is not included in the original odbc support resources";
}
