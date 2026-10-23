namespace Erlang;

internal static class DebugCodeNative427
{
    internal static bool TryCapture(
        ProcessRuntime? runtime,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (runtime is null || module != DebugCodeNames427.Module || !DebugCodeNames427.Signatures.Contains((function, arity))) return false;
        DebugCodeProfile427? profile = BeamRegisterInterpreter.ManagedDebugCodeProfile427;
        if (profile is null) return false;
        body = (context, arguments) => ValueTask.FromResult(runtime.ReadDebugCode427(
            context,
            profile,
            function,
            arguments
        ));

        return true;
    }
}
