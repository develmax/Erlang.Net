namespace Erlang.Tests;

internal static class OrdinaryCollectorNames
{
    internal static readonly Atom OkAtom = Term.A(Ok);
    internal static readonly Atom TrueAtom = Term.A(ErlangBooleanAtoms.True);
    internal static readonly Atom FalseAtom = Term.A(ErlangBooleanAtoms.False);

    internal const string BeamCase = "runtime/statistics/ordinary-beam-original-values-stacks-and-instruction-gc";
    internal const string Case = "runtime/statistics/ordinary-source-real-collector-before-gc";
    internal const string Ok = "ok";
    internal const string GuestModule = "ordinary_collector";
    internal const string Probe = "ordinary_collector_probe";
    internal const string Entry = "run";
    internal const string Backing = "backing_present";
    internal const string Observe = "observe";
    internal const string HeapField = "termHeap506";
    internal const string HeapSnapshot = "Snapshot";
    internal const string Blocks = "Blocks";
    internal const string Statistics = "Statistics";
    internal const string Sample = "SampleVmCollections";
    internal const string Failure = "Ordinary canonical Source execution did not own real terms before collection.";
    internal const string Source = """
        -module(ordinary_collector).
        -export([run/0, allocate_drop/0]).
        run() ->
            put(kept, {kept, [1,2,3], #{key => {make_ref(),self()}}}),
            ok = allocate_drop(),
            true = ordinary_collector_probe:backing_present(),
            true = garbage_collect(),
            {kept,[1,2,3],#{key := {Ref,Pid}}} = erase(kept),
            true = is_reference(Ref),
            true = Pid =:= self(),
            ok.
        allocate_drop() ->
            ordinary_collector_probe:observe({drop, [4,5,6], #{key => {make_ref(),self()}}}),
            ok.
        """;
}
