namespace Erlang.BeamBuild;

internal static class BeamLibraryBuildNames
{
    internal const string Backend = "Backend";
    internal const string Image = "Image";
    internal const string Sha256 = "Sha256";
    internal const string Dependencies = "Dependencies";
    internal const string Link = "Link";
    internal const string TargetPath = "TargetPath";
    internal const string ImageDirectory = "erlang-libraries";
    internal const string Extension = ".beam";
    internal const string TemporaryExtension = ".tmp";
    internal const string Invalid = "Invalid ErlangLibrary declaration; select Managed or Beam with an explicit image, SHA256 and dependencies";
    internal const string HashMismatch = "ErlangLibrary image differs from its declared SHA256";
}
