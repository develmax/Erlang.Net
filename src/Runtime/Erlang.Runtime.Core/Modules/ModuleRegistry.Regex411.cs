namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void RequireRegexRegistrationCaller411() => RequireAsyncSourceCaller();

    internal void InstallRegexProvider411(ProcessRuntime runtime, RuntimeRegexEngine411 engine)
    {
        if (!ReferenceEquals(owningRuntime, runtime)) throw new ArgumentException(ReNames411.Foreign);
        runtime.RequireNativePrimitiveRegistration();
        var bodies = engine.CreateBodies();
        using var source = EnterSourceMutation();
        Apply(_ => InstallNativeProvider(ReNames411.Module, RuntimeNativePrimitives.RegexCore411, bodies));
    }
}
