namespace Erlang;

internal static class ProcessTraceRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            ProcessTraceNames.Configure,
            3,
            (context, arguments) => ValueTask.FromResult(context.Runtime.ProcessTracing.Configure(context, arguments))
        );
        registry.Register(
            ErlangModuleNames.Module,
            ProcessTraceNames.Info,
            2,
            (context, arguments) => ValueTask.FromResult(context.Runtime.ProcessTracing.Info(context, arguments))
        );
        registry.Register(
            ErlangModuleNames.Module,
            ProcessTraceNames.Pattern,
            2,
            (context, arguments) => ValueTask.FromResult(context.Runtime.ProcessTracing.Pattern(context, arguments))
        );
        registry.Register(
            ErlangModuleNames.Module,
            ProcessTraceNames.Pattern,
            3,
            (context, arguments) => ValueTask.FromResult(context.Runtime.ProcessTracing.Pattern(context, arguments))
        );
        registry.Register(
            ErlangModuleNames.Module,
            ProcessTraceNames.Delivered,
            1,
            (context, arguments) => ValueTask.FromResult<Term>(context.Runtime.ProcessTracing.Delivered(arguments[0], context))
        );
    }
}
