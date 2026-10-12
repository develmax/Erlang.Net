namespace Erlang;

internal static class ProcessTableRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            ProcessTableNames.Iterator,
            0,
            (_, _) => ValueTask.FromResult<Term>(Term.Tuple(Term.I(ProcessTableParameters.InitialCursor), Nil.Value))
        );
        registry.Register(
            ErlangModuleNames.Module,
            ProcessTableNames.Next,
            1,
            (context, arguments) => ValueTask.FromResult(ProcessTableQueries.Next(context, arguments[0]))
        );
    }
}
