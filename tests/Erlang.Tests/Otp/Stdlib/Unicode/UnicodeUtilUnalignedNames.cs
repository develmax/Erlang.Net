namespace Erlang.Tests;

internal static class UnicodeUtilUnalignedNames
{
    internal const string Test = "runtime/unicode-util/unaligned-utf8-prefix-linear-allocation-and-backing";
    internal const string Failure = "Unicode utility unaligned traversal changes values, backing or allocation scaling";
    internal const int Small = 4096;
    internal const int Large = 8192;
    internal const int Offset = 3;
    internal const int BitsPerByte = 8;
    internal const int Character = 65;
    internal const int MaximumDoubling = 3;
    internal const int NoiseBytes = 1024 * 1024;
    internal static readonly TimeSpan Deadline = TimeSpan.FromMinutes(1);
}
