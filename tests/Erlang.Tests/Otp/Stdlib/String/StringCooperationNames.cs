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
    internal const string Trim = "trim";
    internal const string Take = "take";
    internal const string Leading = "leading";
    internal const string Trailing = "trailing";
    internal const byte Separator = (byte)'b';
    internal const byte OtherSeparator = (byte)'c';
    internal const string TrimSingle = "runtime/string/cooperative-list-trim-single";
    internal const string TrimMultiple = "runtime/string/cooperative-list-trim-multiple";
    internal const string TakeLeading = "runtime/string/cooperative-list-take-leading-complement";
    internal const string TakeTrailing = "runtime/string/cooperative-list-take-trailing";
    internal const string Equal = "equal";
    internal const string EqualTwo = "runtime/string/cooperative-list-equal-two";
    internal const string EqualThree = "runtime/string/cooperative-list-equal-three-false";
    internal const string Lexemes = "lexemes";
    internal const string TrimFragments = "runtime/string/cooperative-empty-binary-fragments-trim";
    internal const string LexemeFragments = "runtime/string/cooperative-empty-binary-fragments-lexemes";
}
