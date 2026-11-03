using System.Reflection;

namespace Erlang.Tests;

internal static class FlatLiteralCapture222Names
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string RootsType = "Erlang.ModuleLiteralRoots801";
    internal const string DomainType = "Erlang.ModuleLiteralDomain800";
    internal const string IntrinsicType = "Erlang.RuntimeEmptyTupleDomain803";
    internal const string Capture = "Capture801";
    internal const string Fast = "TryCaptureFlat222";
    internal const string Heap = "TermHeap506";
    internal const string Enter = "Enter";
    internal const string Reserve = "Reserve";
    internal const string TupleMethod = "CreateTupleInto507";
    internal const string RetainSidecar = "RetainCertifiedLiteralBlock134";
    internal const string Parents = "parents";
    internal const string Leases = "leases";
    internal const string Intrinsics = "intrinsics803";
    internal const string References = "references";
    internal const string Domain = "Domain";
    internal const string AddMethod = "Add";
    internal const string Count = "Count";
    internal const string SingleCase = "single222";
    internal const string TwoDomainCase = "two-domain222";
    internal const string AllocationLine = "literal-flat222 {0} flat={1} nodes={2} parents={3} calls={4} bytes={5}";
    internal const string Failure = "Actual literal flat capture or lease authentication differs.";
    internal const int Warmup = 8;
    internal const int Calls = 32;
}
