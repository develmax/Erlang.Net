namespace Erlang;

internal static class CodeObjectFileNames
{
    internal const string Extension = ".beam";
    internal const string UndefinedDigest = "undefined";
    internal const string ModuleMd5Key = "md5";

    internal static ReadOnlySpan<byte> FormId => "FOR1"u8;
}
