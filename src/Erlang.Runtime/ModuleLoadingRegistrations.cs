namespace Erlang;

internal static class ModuleLoadingRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            ModuleLoadingNames.ModuleLoaded,
            1,
            (context, arguments) =>
            {
                if (arguments[0] is not Atom module)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);

                return ValueTask.FromResult<Term>(Term.A(context.Runtime.Modules.IsModuleLoaded(module.Name) ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False));
            }
        );
        registry.Register(
            ErlangModuleNames.Module,
            ModuleLoadingNames.Loaded,
            0,
            (context, _) => ValueTask.FromResult(context.Runtime.Modules.LoadedModuleList())
        );
    }
}
