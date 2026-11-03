namespace Erlang.Tests;

internal static class NativeResource527TestLayout
{
    // Source-profile offsets; independent actual C/CLR layout probes remain
    // required during parent integration. They are not golden observations.
    internal const int PointerBytes = 8;
    internal const int Flags = 0;
    internal const int ReferenceCount = PointerBytes * 2;
    internal const int OriginalSize = PointerBytes * 3;
    internal const int Reference0 = PointerBytes * 5;
    internal const int Reference1 = Reference0 + sizeof(uint);
    internal const int Reference2 = Reference1 + sizeof(uint);
}
