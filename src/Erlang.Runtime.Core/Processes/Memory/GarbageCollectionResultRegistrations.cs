namespace Erlang;

internal static class GarbageCollectionResultRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            GarbageCollectionResultNames.Function,
            GarbageCollectionResultNames.Arity,
            (context, arguments) =>
            {
                if (arguments[0] is not ReferenceTerm)
                    throw new ErlangException(ErlangErrorReasons.FunctionClause);

                return GarbageCollectionResultReducer.GatherAsync(
                    context,
                    arguments[0],
                    context.Runtime.ActorDispatchCapacity
                );
            }
        );
    }
}
