namespace Erlang;

public static class QueueHigherModule
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (var entry in new (string Name, int Arity)[]
        {
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
            (QueueModuleNames.DeleteWithReverse, 2)
        })
        {
            string function = entry.Name;
            registry.Register(QueueModuleNames.Module, function, entry.Arity,
                (context, arguments) => QueueHigherExecution.Run(context, function, arguments));
        }
    }
}
