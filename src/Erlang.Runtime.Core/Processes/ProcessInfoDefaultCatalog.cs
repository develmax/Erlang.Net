namespace Erlang;

internal static class ProcessInfoDefaultCatalog
{
    internal static readonly IReadOnlyList<string> OrderedKeys = Array.AsReadOnly<string>(
    [
        ProcessInfoNames.RegisteredName,
        ProcessInfoNames.CurrentFunction,
        ProcessInfoLogicalNames.InitialCall,
        ProcessInfoNames.Status,
        ProcessInfoNames.MessageQueueLength,
        ProcessInfoNames.Links,
        ProcessInfoNames.Dictionary,
        ProcessInfoNames.TrapExit,
        ProcessInfoLogicalNames.ErrorHandler,
        ProcessInfoLogicalNames.Priority,
        ProcessInfoNames.GroupLeader,
        ProcessInfoResourceNames.TotalHeapSize,
        ProcessInfoResourceNames.HeapSize,
        ProcessInfoResourceNames.StackSize,
        ProcessInfoLogicalNames.Reductions,
        ProcessInfoResourceNames.GarbageCollection,
        ProcessInfoLogicalNames.Suspending
    ]);

    internal static readonly IReadOnlyList<string> MissingPhysicalKeys = Array.AsReadOnly<string>(
    [
        ProcessInfoResourceNames.TotalHeapSize,
        ProcessInfoResourceNames.HeapSize,
        ProcessInfoResourceNames.StackSize,
        ProcessInfoResourceNames.GarbageCollection
    ]);

    internal static readonly IReadOnlyList<ProcessInfoItem> LogicalItems = Array.AsReadOnly(
        OrderedKeys.Where(key => !MissingPhysicalKeys.Contains(key))
            .Select(key => ProcessInfoItem.Parse(Term.A(key))).ToArray()
    );
}
