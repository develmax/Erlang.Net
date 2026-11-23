namespace Erlang.Tests;

internal static class BeamCheckedFramePeepholeDiagnostics
{
    internal const string SharedReturn = "Expected the earlier label to select the identical framed fallthrough return.";
    internal const string DifferentFrame = "A return with a different deallocation width must remain separate.";
    internal const string DifferentSlot = "A return reading a different live Y slot must remain separate.";
    internal const string DifferentLine = "The instruction rewrite must preserve the explicit line boundary.";
    internal const string CheckedProjection = "Expected the fused heap allocation before the checked projection with both X roots and the same Y slots.";
    internal const string CallBarrier = "Stack and heap allocation must remain separate across an effect.";
    internal const string LaterRoots = "The restricted allocation fold requires a later heap test with no live X roots.";
    internal const string DictionaryLookup = "Hoist the constructor heap check across a noncollecting process dictionary lookup.";
    internal const string DictionaryLookupBarrier = "Do not hoist the heap check across an unproven dictionary lookup region.";
}
