namespace Erlang;

internal static class ReferenceTextRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            ReferenceTextNames.ToList,
            1,
            (context, arguments) => arguments[0] is ReferenceTerm reference ? ValueTask.FromResult(ReferenceText.ToList(context.Runtime, reference)) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        registry.Register(
            ErlangModuleNames.Module,
            ReferenceTextNames.FromList,
            1,
            (context, arguments) => ValueTask.FromResult<Term>(ReferenceText.FromList(context.Runtime, arguments[0]))
        );
    }
}
