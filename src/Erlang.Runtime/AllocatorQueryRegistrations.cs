namespace Erlang;

internal static class AllocatorQueryRegistrations
{
    internal static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            SystemAllocatorQueryNames.Info,
            SystemAllocatorQueryNames.QueryArity,
            (context, arguments) => AllocatorQueryMailbox.Gather(
                context,
                arguments[0],
                SystemAllocatorQueryProvider.Create(context.Runtime, AllocatorQueryKind.Full)
            )
        );
        registry.Register(
            ErlangModuleNames.Module,
            SystemAllocatorQueryNames.Sizes,
            SystemAllocatorQueryNames.QueryArity,
            (context, arguments) => AllocatorQueryMailbox.Gather(
                context,
                arguments[0],
                SystemAllocatorQueryProvider.Create(context.Runtime, AllocatorQueryKind.Sizes)
            )
        );
    }
}
