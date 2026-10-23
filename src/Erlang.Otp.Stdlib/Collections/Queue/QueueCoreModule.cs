namespace Erlang;

public static class QueueCoreModule
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.New,
            0,
            (_, _) => ValueTask.FromResult<Term>(QueueOperations.New())
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.IsQueue,
            1,
            (_, arguments) => ValueTask.FromResult(QueueOperations.IsQueue(arguments[0]))
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.IsEmpty,
            1,
            (context, arguments) => ValueTask.FromResult(QueueCoreExecution.IsEmpty(context, arguments[0]))
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.Length,
            1,
            (context, arguments) => QueueCoreExecution.Length(context, arguments[0])
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.In,
            2,
            (context, arguments) => ValueTask.FromResult(QueueCoreExecution.In(context, arguments[0], arguments[1]))
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.Out,
            1,
            (context, arguments) => QueueCoreExecution.Out(context, arguments[0])
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.FromList,
            1,
            (context, arguments) => QueueCoreExecution.FromList(context, arguments[0])
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.ToList,
            1,
            (context, arguments) => QueueCoreExecution.ToList(context, arguments[0])
        );
        QueueEndsModule.Register(registry);
        QueueHigherModule.Register(registry);
    }
}
