namespace Erlang;

public static class QueueEndsModule
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.InReverse,
            2,
            (context, arguments) => ValueTask.FromResult(QueueEndsExecution.InRear(context, arguments[0], arguments[1]))
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.Cons,
            2,
            (context, arguments) => ValueTask.FromResult(QueueEndsExecution.InRear(context, arguments[0], arguments[1]))
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.Snoc,
            2,
            (context, arguments) => ValueTask.FromResult(QueueCoreExecution.In(context, arguments[1], arguments[0]))
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.OutReverse,
            1,
            (context, arguments) => QueueEndsExecution.OutRear(context, arguments[0])
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.Get,
            1,
            (context, arguments) => QueueEndsExecution.Get(context, arguments[0], false)
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.Head,
            1,
            (context, arguments) => QueueEndsExecution.Get(context, arguments[0], true)
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.GetReverse,
            1,
            (context, arguments) => QueueEndsExecution.GetRear(context, arguments[0])
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.Last,
            1,
            (context, arguments) => QueueEndsExecution.GetRear(context, arguments[0])
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.Daeh,
            1,
            (context, arguments) => QueueEndsExecution.GetRear(context, arguments[0])
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.Peek,
            1,
            (context, arguments) => QueueEndsExecution.Peek(context, arguments[0], false)
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.PeekReverse,
            1,
            (context, arguments) => QueueEndsExecution.Peek(context, arguments[0], true)
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.Drop,
            1,
            (context, arguments) => QueueEndsExecution.Drop(context, arguments[0], false)
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.Tail,
            1,
            (context, arguments) => QueueEndsExecution.Drop(context, arguments[0], false)
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.DropReverse,
            1,
            (context, arguments) => QueueEndsExecution.Drop(context, arguments[0], true)
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.Init,
            1,
            (context, arguments) => QueueEndsExecution.Drop(context, arguments[0], true)
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.Liat,
            1,
            (context, arguments) => QueueEndsExecution.Drop(context, arguments[0], true)
        );
        registry.Register(
            QueueModuleNames.Module,
            QueueModuleNames.Lait,
            1,
            (context, arguments) => QueueEndsExecution.Drop(context, arguments[0], true)
        );
    }
}
