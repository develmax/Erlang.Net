namespace Erlang;

internal static class ErlangRaiseRegistrations
{
    public static void Register(ModuleRegistry registry) => registry.RegisterCanonicalRuntimeRead535(
        ErlangModuleNames.Module,
        ErlangRaiseBifNames.Raise,
        3,
        (context, arguments) => ValueTask.FromResult(ErlangRaisedStack.Raise(arguments, context.Runtime.BacktraceDepth))
    );
}
