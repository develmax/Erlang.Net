namespace Erlang.Tests;

internal static class NifAppendOnly516Names
{
    internal const string ScalingTest = "runtime/nif516/static-list-tail-root-walk-scaling";
    internal const string HiddenTest = "runtime/nif516/append-only-hidden-map-and-new-composite";
    internal const string UnknownTest = "runtime/nif516/unknown-static-graph-remains-conservative";
    internal const string Heap = "TermHeap506";
    internal const string Registry = "BinaryRegistry512";
    internal const string Traversal = "TraversalSnapshot514";
    internal const string Visits = "VisitedTerms";
    internal const string Retains = "RetainOperations";
    internal const string Peak = "PeakBackingReferences";
    internal const string References = "LiveBackingReferences";
    internal const string Add = "Add";
    internal const string AddValue = "AddValue";
    internal const string GetValue = "GetValue";
    internal const string Get = "Get";
    internal const string TryCons = "TryCons";
    internal const string EnvironmentFor = "EnvironmentFor";
    internal const string Image = "Image";
    internal const string HiddenMap = "HiddenMap";
    internal const string Failure = "NIF516 append-only graph custody control failed.";
    internal const string Diagnostic = "N={0}: actual visits={1}, retains={2}, peak backing references={3}, live backing references={4}";
    internal const int BackingBytes = 65;
    internal const int VisitsPerElementLimit = 8;
    internal const int VisitConstantLimit = 16;
    internal const int MaximumRetains = 3;
    internal const int MaximumCopies = 2;
    internal static readonly int[] Sizes = [16, 32, 64];
}
