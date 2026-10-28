namespace Erlang.Tests;

internal static class SourceBinaryComposition511Names
{
    internal const string Test = "runtime/source511/composed-binary-retirement-closure-and-hidden-map-lexical-roots";
    internal const string GuestModule = "source_binary511";
    internal const string Probe = "source_binary_probe511";
    internal const string Ok = "ok";
    internal const string Make = "make";
    internal const string MakeMap = "make_map";
    internal const string Live = "live";
    internal const string Dead = "dead";
    internal const string Discarded = "discarded";
    internal const string Closure = "closure";
    internal const string Map = "map";
    internal const string Equality = "equality";
    internal const string BinarySnapshot = "BinarySnapshot512";
    internal const string Bytes = "ReferenceBackingBytes";
    internal const string ContinuationType = "Erlang.Compiler.SourceContinuationLiveness511";
    internal const string ConstraintType = "Erlang.Compiler.CompiledBindingScope";
    internal const string Complete = "CompleteStatement";
    internal const string OuterName = "outer511";
    internal const string HiddenConstraint = "constraint511";
    internal const string CloneTree = "CloneTree";
    internal const string Failure = "Source511/512 binary root composition contract failed.";
    internal const int PayloadBytes = 4096;
    internal const string Source = """
        -module(source_binary511).
        -export([discarded/0,closure/0,map/0,equality/0]).
        discarded() -> source_binary_probe511:make(), source_binary_probe511:dead(), ok.
        closure() -> B=source_binary_probe511:make(), F=fun()->B end,
                     source_binary_probe511:live(), F().
        map() -> M=source_binary_probe511:make_map(), source_binary_probe511:live(), M.
        equality() -> X=1, source_binary_probe511:dead(), X=2.
        """;
}
