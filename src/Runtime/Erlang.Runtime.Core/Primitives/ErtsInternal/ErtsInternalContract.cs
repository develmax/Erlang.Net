namespace Erlang;

internal static class ErtsInternalContract
{
    internal const string Module = "erts_internal";
    internal const string SourceFile = "erts_internal.erl";
    internal static readonly (string Function, int Arity)[] LibraryExports =
    [
        (ErtsInternalFunctions.AwaitPortSendResult, 3),
        (ErtsInternalFunctions.McIterator, 1),
        (ErtsInternalFunctions.McRefill, 1),
        (ErtsInternalFunctions.GatherSystemCheckResult, 1),
        (ErtsInternalFunctions.WaitReleaseLiteralAreaSwitch, 1),
        (ErtsInternalFunctions.FlushMonitorMessages, 3),
        (ErtsInternalFunctions.FlushMonitorMessagesRefopt, 0),
        (ErtsInternalFunctions.AwaitResult, 1),
        (ErtsInternalFunctions.GatherIoBytes, 2),
        (ErtsInternalFunctions.SetCodeAndLiteralCleanerPrio, 1),
        (ErtsInternalFunctions.AwaitMicrostateAccountingModifications, 3),
        (ErtsInternalFunctions.GatherMicrostateAccountingResult, 2),
        (ErtsInternalFunctions.DistCtrlPutData, 2),
        (ErtsInternalFunctions.SystemFlagSchedulerWallTime, 1),
        (ErtsInternalFunctions.GatherSchedWallTimeResult, 1),
        (ErtsInternalFunctions.AwaitSchedWallTimeModifications, 2),
        (ErtsInternalFunctions.EtsInfoBinary, 1),
        (ErtsInternalFunctions.GetInternalStateBlocked, 1),
        (ErtsInternalFunctions.SpawnInit, 1),
        (ErtsInternalFunctions.DistSpawnInit, 1),
        (ErtsInternalFunctions.Crasher, 6),
        (ErtsInternalFunctions.DynamicNodeName, 0),
        (ErtsInternalFunctions.DynamicNodeName, 1),
        (ErtsInternalFunctions.Breakpoint, 4),
        (ErtsInternalFunctions.CheckProcessCode, 3),
        (ErtsInternalFunctions.IsProcessAlive, 1),
        (ErtsInternalFunctions.TermToString, 1),
    ];

    internal static int Line(string function) => function switch
    {
        ErtsInternalFunctions.AwaitPortSendResult => 142,
        ErtsInternalFunctions.McIterator => 467,
        ErtsInternalFunctions.McRefill => 512,
        ErtsInternalFunctions.GatherSystemCheckResult => 396,
        ErtsInternalFunctions.WaitReleaseLiteralAreaSwitch => 378,
        ErtsInternalFunctions.FlushMonitorMessages => 523,
        ErtsInternalFunctions.FlushMonitorMessagesRefopt => 537,
        ErtsInternalFunctions.AwaitResult => 152,
        ErtsInternalFunctions.GatherIoBytes => 162,
        ErtsInternalFunctions.SetCodeAndLiteralCleanerPrio => 564,
        ErtsInternalFunctions.AwaitMicrostateAccountingModifications => 589,
        ErtsInternalFunctions.GatherMicrostateAccountingResult => 597,
        ErtsInternalFunctions.DistCtrlPutData => 687,
        ErtsInternalFunctions.SystemFlagSchedulerWallTime => 745,
        ErtsInternalFunctions.GatherSchedWallTimeResult => 768,
        ErtsInternalFunctions.AwaitSchedWallTimeModifications => 759,
        ErtsInternalFunctions.EtsInfoBinary => 951,
        ErtsInternalFunctions.GetInternalStateBlocked => 985,
        ErtsInternalFunctions.SpawnInit => 1011,
        ErtsInternalFunctions.DistSpawnInit => 1040,
        ErtsInternalFunctions.Crasher => 1073,
        ErtsInternalFunctions.DynamicNodeName => 1131,
        ErtsInternalFunctions.Breakpoint => 1188,
        ErtsInternalFunctions.CheckProcessCode => 318,
        ErtsInternalFunctions.IsProcessAlive => 812,
        ErtsInternalFunctions.TermToString => 1152,
        _ => throw new ArgumentOutOfRangeException(nameof(function))
    };
}
