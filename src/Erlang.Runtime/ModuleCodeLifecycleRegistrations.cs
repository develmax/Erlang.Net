namespace Erlang;

internal static class ModuleCodeLifecycleRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            ModuleCodeLifecycleNames.DeleteModule,
            1,
            (context, arguments) => ValueTask.FromResult(arguments[0] is Atom module ? context.Runtime.Modules.DeleteModule(module.Name) : throw new ErlangException(ErlangErrorReasons.BadArgument))
        );
        registry.Register(
            ErlangModuleNames.Module,
            ModuleCodeLifecycleNames.CheckOldCode,
            1,
            (context, arguments) => ValueTask.FromResult<Term>(arguments[0] is Atom module ? context.Runtime.Modules.HasOldCode(module.Name) ? Term.A(ErlangBooleanAtoms.True) : Term.A(ErlangBooleanAtoms.False) : throw new ErlangException(ErlangErrorReasons.BadArgument))
        );
    }
}
