namespace Erlang.Otp;

internal static class OtpObserverNames
{
    internal const string Prefix = "Erlang.Otp.Observer.Beam.";
    internal const string Catalog = "catalog.json";
    internal const string BeamExtension = ".beam";
    internal const string Application = "observer.app";
    internal const string Upgrade = "observer.appup";
    internal const string MissingResource = "Original observer library resource is missing";
    internal const string UnknownModule = "Module is not included in the original observer library";
    internal const string HashMismatch = "Original observer library resource hash mismatch";
    internal const string SupportPrefix = "support/";
    internal const string UnknownSupportFile = "File is not included in the original observer support resources";
}
