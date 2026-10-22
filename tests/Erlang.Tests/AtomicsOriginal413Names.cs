namespace Erlang.Tests;

internal static class AtomicsOriginal413Names
{
    internal const string ChildSwitch = "--atomics413-matched-child", Worker = "beam_atomics413", WorkerBeam = "beam_atomics413.beam";
    internal const string Run = "run", Stacks = "run_stacks", ResourceStacks = "resource_stacks";
    internal const string Values = "values.etf", StackValues = "stacks.etf", ResourceValues = "resource-stacks.etf";
    internal const string Actual = "actual-", Expected = "expected-", Json = ".json", Receipt = "comparison.json", FailureFile = "child-failure.json";
    internal const string InvalidArguments = "Expected atomics413 child switch and absolute output folder";
    internal const int Rows = 43, StableRows = 258, ResourceRows = 45;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(90);
}
