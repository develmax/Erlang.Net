namespace Erlang.Tests;

internal static class QueueBeamFullSmokeCases
{
    internal static readonly (string Function, int Arity)[] Exports =
    [
        (QueueModuleNames.New, 0),
        (QueueModuleNames.IsQueue, 1),
        (QueueModuleNames.IsEmpty, 1),
        (QueueModuleNames.Length, 1),
        (QueueModuleNames.In, 2),
        (QueueModuleNames.Out, 1),
        (QueueModuleNames.FromList, 1),
        (QueueModuleNames.ToList, 1),
        (QueueModuleNames.Member, 2),
        (QueueModuleNames.Reverse, 1),
        (QueueModuleNames.Join, 2),
        (QueueModuleNames.Split, 2),
        (QueueModuleNames.Filter, 2),
        (QueueModuleNames.FilterMap, 2),
        (QueueModuleNames.Fold, 3),
        (QueueModuleNames.Any, 2),
        (QueueModuleNames.All, 2),
        (QueueModuleNames.Delete, 2),
        (QueueModuleNames.DeleteReverse, 2),
        (QueueModuleNames.DeleteWith, 2),
        (QueueModuleNames.DeleteWithReverse, 2),
        .. QueueBeamEndsSmokeContracts.Exports
    ];

    internal static async Task Run(
        string function,
        int arity,
        bool beamBackend,
        bool originalLists = false,
        bool startupSelection = false
    )
    {
        QueueBeamFullSmokeBackend.Require(Exports.Length == QueueBeamFullSmokeNames.Exports);
        QueueBeamFullSmokeBackend.Require(Exports.Distinct().Count() == QueueBeamFullSmokeNames.Exports);
        if (QueueBeamCoreSmokeContracts.Owns(QueueModuleNames.Module, function, arity))
            await QueueBeamCoreSmokeContracts.Run(
                function,
                arity,
                beamBackend,
                originalLists,
                startupSelection
            );
        else if (QueueBeamHigherSmokeContracts.Owns(QueueModuleNames.Module, function, arity))
            await QueueBeamHigherSmokeContracts.Run(
                function,
                arity,
                beamBackend,
                originalLists,
                startupSelection
            );
        else
            await QueueBeamEndsSmokeContracts.Run(
                function,
                arity,
                beamBackend,
                originalLists,
                startupSelection
            );
    }
}
