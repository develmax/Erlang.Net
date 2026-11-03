namespace Erlang.Tests;

internal static class StringCooperationNames
{
    internal const int Characters = 16 * 1024;
    internal const byte Character = (byte)'a';
    internal const string Failure = "String ASCII traversal must charge reductions without changing its result";
    internal const string Length = "length";
    internal const string Reverse = "reverse";
    internal const string ProcessInfo = "process_info";
    internal const string Reductions = "reductions";
    internal const string ListLength = "runtime/string/cooperative-ascii-list-length";
    internal const string BinaryLength = "runtime/string/cooperative-ascii-binary-length";
    internal const string ListReverse = "runtime/string/cooperative-ascii-list-reverse";
    internal const string BinaryReverse = "runtime/string/cooperative-ascii-binary-reverse";
    internal const string Slice = "slice";
    internal const int BitsPerByte = 8;
    internal const int SliceInputCharacters = 2 * Characters;
    internal const string SliceDrop = "runtime/string/cooperative-ascii-binary-slice-drop";
    internal const string SliceKeep = "runtime/string/cooperative-ascii-binary-slice-keep";
}
