namespace Erlang.Tests;

internal static class InternalPortsOriginal416Names
{
    internal const string Test = "oracle/compiled-module/internal-ports416-statuses-and-undef174-full-typed-raw";
    internal const string Prefix = "Erlang.Tests.InternalPortsOriginal416.", Manifest = "manifest.json";
    internal const string Runtime = "erlang", Internal = "erts_internal";
    internal const string Module = "beam_internal_ports416", WorkerBeam = "beam_internal_ports416.beam", FacetBeam = "erts_internal.beam";
    internal const string Run = "run", Stacks = "run_stacks", Encode = "term_to_binary";
    internal const string Values = "values.etf", StackValues = "stacks.etf", Actual = "actual-", Expected = "expected-", Json = ".json";
    internal const string Output = "artifacts/library416-internal-ports-permanent", Guid = "N", Receipt = "comparison.json";
    internal const string Failure = "Original internal port statuses and undef cohort differs; inspect whole raw/typed outcomes";
    internal const string ValueScope = "values", StackScope = "stacks";
    internal const int Rows = 29, WholeRows = 174, NativeCount = 6;
    internal static readonly (string Name, int Arity)[] NativeSignatures =
    [
        (PortNames.Open, 2),
        (PortNames.Command, 3),
        (PortNames.Connect, 2),
        (PortNames.Close, 1),
        (PortNames.Info, 1),
        (PortNames.Info, 2)
    ];
}
