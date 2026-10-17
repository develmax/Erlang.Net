using Erlang.Differential;

namespace Erlang.Tests;

internal static class RuntimeIncrementCompiledCases
{
    internal const string TestPrefix = "oracle/compiled-module/";

    internal static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        .. FullSweepCases.Modules,
        .. RuntimeReductionCases.Modules,
        .. MonitorOptionOrderCases.Modules,
        .. SchedulerStatisticsCases.Modules,
        .. BusyPortCases.Modules
    ];

    internal static IReadOnlyList<CompiledModuleCase> NativeHostModules { get; } = NativeHostDefaultCases.Modules;

    internal static IReadOnlySet<string> Names { get; } = Modules
        .Concat(NativeHostModules)
        .Select(fixture => fixture.Name)
        .ToHashSet(StringComparer.Ordinal);
}
