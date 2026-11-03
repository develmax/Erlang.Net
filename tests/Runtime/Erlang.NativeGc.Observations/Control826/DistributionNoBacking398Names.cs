using System.Reflection;

namespace Erlang.Tests;

internal static class DistributionNoBacking398Names
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string Heap = "TermHeap506", Gate = "Gate", Enter = "Enter", Reserve = "Reserve";
    internal const string Tuple = "CreateTupleInto507", Cons = "CreateConsInto507", Collect = "CollectGeneration508";
    internal const string Storage = "Storage", BackingProof = "HasImmutableNoBackingClosure192", BinaryProof = "HasImmutableNoBinaryClosure192";
    internal const string ReadonlyProof = "IsImmutableReadonlyCaptureClosure138", CheckProof = "IsImmutableNoBinaryClosure192";
    internal const string Magic = "HasOwnedMagicReference576", Registry = "BinaryRegistry512", Retain = "Retain512";
    internal const string Traversal = "TraversalSnapshot514", Visits = "VisitedTerms", Operations = "RetainOperations";
    internal const string Entry = "Entry", Close = "Close";
    internal const string Node = "distribution_no_backing398@localhost";
    internal const string Failure = "Distribution-only398 backing proof or actual resource boundary changed.";
    internal const int ChainLength = 64, Repeats = 8;
    internal static readonly (string Type, string Method)[] DirectFamilies = [
        ("Erlang.NativeResourceRoots524", "Direct524"), ("Erlang.AtomicsRoots586", "Direct586"),
        ("Erlang.CountersRoots596", "Direct596"), ("Erlang.ReMagicRoots597", "Direct597"),
        ("Erlang.BinaryProgramRoots598", "Direct598"), ("Erlang.PreparedLoaderRoots807", "Direct807")
    ];
}
