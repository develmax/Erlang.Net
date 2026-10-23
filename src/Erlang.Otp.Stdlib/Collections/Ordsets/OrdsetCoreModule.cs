namespace Erlang;

public static class OrdsetCoreModule
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            OrdsetCoreNames.Module,
            OrdsetCoreNames.New,
            0,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        registry.Register(
            OrdsetCoreNames.Module,
            OrdsetCoreNames.IsEmpty,
            1,
            (_, arguments) => ValueTask.FromResult(CoreModules.Boolean(arguments[0] is Nil))
        );
        registry.Register(
            OrdsetCoreNames.Module,
            OrdsetCoreNames.ToList,
            1,
            (_, arguments) => ValueTask.FromResult(arguments[0])
        );
        registry.Register(
            OrdsetCoreNames.Module,
            OrdsetCoreNames.IsSet,
            1,
            (context, arguments) => OrdsetCoreExecution.IsSet(context, arguments[0])
        );
        registry.Register(
            OrdsetCoreNames.Module,
            OrdsetCoreNames.Size,
            1,
            (context, arguments) => OrdsetCoreExecution.Size(context, arguments[0])
        );
        registry.Register(
            OrdsetCoreNames.Module,
            OrdsetCoreNames.IsEqual,
            2,
            (context, arguments) => ValueTask.FromResult(OrdsetCoreExecution.IsEqual(context, arguments[0], arguments[1]))
        );
        registry.Register(
            OrdsetCoreNames.Module,
            OrdsetCoreNames.IsElement,
            2,
            (context, arguments) => OrdsetCoreExecution.IsElement(context, arguments[0], arguments[1])
        );
        registry.Register(
            OrdsetCoreNames.Module,
            OrdsetCoreNames.AddElement,
            2,
            (context, arguments) => OrdsetCoreExecution.AddElement(context, arguments[0], arguments[1])
        );
        OrdsetFullModule.Register(registry);
    }
}
