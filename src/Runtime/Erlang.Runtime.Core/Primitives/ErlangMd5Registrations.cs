namespace Erlang;

internal static class ErlangMd5Registrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            ErlangMd5Names.Initialize,
            0,
            (_, _) => ValueTask.FromResult(ErlangMd5Context.Initialize())
        );
        registry.Register(
            ErlangModuleNames.Module,
            ErlangMd5Names.Update,
            2,
            (_, arguments) => ValueTask.FromResult(ErlangMd5Context.Update(arguments[0], arguments[1]))
        );
        registry.Register(
            ErlangModuleNames.Module,
            ErlangMd5Names.Finalize,
            1,
            (_, arguments) => ValueTask.FromResult(ErlangMd5Context.Finalize(arguments[0]))
        );
    }
}
