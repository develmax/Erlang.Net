namespace Erlang;

internal static class DynamicTraceRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (string name in new[] { DynamicTraceNames.GetTag, DynamicTraceNames.GetTagData })
            registry.Register(
                ErlangModuleNames.Module,
                name,
                0,
                (_, _) => ValueTask.FromResult<Term>(Term.A(DynamicTraceResultNames.Undefined))
            );
        registry.Register(
            ErlangModuleNames.Module,
            DynamicTraceNames.PutTag,
            1,
            (_, _) => ValueTask.FromResult<Term>(Term.A(DynamicTraceResultNames.Undefined))
        );
        foreach (string name in new[] { DynamicTraceNames.Append, DynamicTraceNames.Prepend })
            registry.Register(
                ErlangModuleNames.Module,
                name,
                1,
                (_, arguments) => ValueTask.FromResult(arguments[0])
            );
        registry.Register(
            ErlangModuleNames.Module,
            DynamicTraceNames.Spread,
            1,
            (_, arguments) =>
        {
            CoreModules.Bool(arguments[0]);

            return ValueTask.FromResult<Term>(Term.A(ErlangBooleanAtoms.True));
        }
        );
        registry.Register(
            ErlangModuleNames.Module,
            DynamicTraceNames.Restore,
            1,
            (_, arguments) => arguments[0] is Atom { Name: ErlangBooleanAtoms.True }
            ? ValueTask.FromResult<Term>(arguments[0])
            : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
    }
}
