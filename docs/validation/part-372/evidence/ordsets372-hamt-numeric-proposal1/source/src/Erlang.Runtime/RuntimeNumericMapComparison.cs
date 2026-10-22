using System.Runtime.CompilerServices;

namespace Erlang;

internal static class RuntimeNumericMapComparison
{
    private static readonly ConditionalWeakTable<ProcessRuntime, Func<ITermMapComparisonLayout>> factories = new();

    internal static TermMapComparisonScope EnterThread(ProcessRuntime runtime)
        => TermMapComparisonEnvironment.EnterThread(factories.GetValue(
            runtime,
            owner => () => new RuntimeNumericMapLayout(owner)
        ));
}
