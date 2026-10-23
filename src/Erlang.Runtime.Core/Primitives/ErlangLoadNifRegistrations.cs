namespace Erlang;

internal static class ErlangLoadNifRegistrations
{
    internal static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            SourceLoadNifNames.Function,
            SourceLoadNifNames.Arity,
            (_, _) => ValueTask.FromResult(ErlangLoadNifTerms.IndirectCallError)
        );
    }
}
