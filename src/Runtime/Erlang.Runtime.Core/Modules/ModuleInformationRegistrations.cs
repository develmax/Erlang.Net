namespace Erlang;

internal static class ModuleInformationRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            ErlangModuleNames.GetModuleInformation,
            1,
            (context, arguments) => ValueTask.FromResult(arguments[0] is Atom module ? context.Runtime.Modules.ModuleInformation(module.Name) : throw new ErlangException(ErlangErrorReasons.BadArgument))
        );
        registry.Register(
            ErlangModuleNames.Module,
            ErlangModuleNames.GetModuleInformation,
            2,
            (context, arguments) => ValueTask.FromResult(arguments[0] is Atom module && arguments[1] is Atom key ? context.Runtime.Modules.ModuleInformation(module.Name, key.Name) : throw new ErlangException(ErlangErrorReasons.BadArgument))
        );
    }
}
