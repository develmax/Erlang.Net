namespace Erlang;

internal static class QueueTailAliases
{
    internal static bool Matches(string wrapper, string target) => (wrapper, target) switch
    {
        (QueueModuleNames.Cons, QueueModuleNames.InReverse) => true,
        (QueueModuleNames.Snoc, QueueModuleNames.In) => true,
        (QueueModuleNames.Tail, QueueModuleNames.Drop) => true,
        (QueueModuleNames.Daeh or QueueModuleNames.Last, QueueModuleNames.GetReverse) => true,
        (QueueModuleNames.Init or QueueModuleNames.Liat or QueueModuleNames.Lait, QueueModuleNames.DropReverse) => true,
        _ => false
    };
}
