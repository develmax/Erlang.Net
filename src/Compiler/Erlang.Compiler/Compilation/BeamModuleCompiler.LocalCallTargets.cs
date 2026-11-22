using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly Dictionary<(string Name, int Arity), int> emittedFunctionEntries = [];
    private readonly Dictionary<(string Name, int Arity), int> deferredLocalCallTargets = [];
    private int nextDeferredFunctionTarget = int.MinValue + 1;

    private int ReserveDeferredFunctionTarget() => nextDeferredFunctionTarget++;

    private int LocalCallTarget(string function, int arity, int plannedEntry)
    {
        if (layoutProbeSource is not null || plannedEntry <= 0)
        {
            return plannedEntry;
        }

        var signature = (function, arity);
        if (emittedFunctionEntries.TryGetValue(signature, out int actual))
        {
            return actual;
        }

        if (!deferredLocalCallTargets.TryGetValue(signature, out int target))
        {
            target = ReserveDeferredFunctionTarget();
            deferredLocalCallTargets.Add(signature, target);
        }

        return target;
    }

    private void ResolveLocalCallTargets()
    {
        foreach (var (signature, target) in deferredLocalCallTargets)
        {
            if (!emittedFunctionEntries.TryGetValue(signature, out int entry))
            {
                throw new NotSupportedException(Unsupported);
            }

            deferredEntries.Add(target, entry);
        }
    }
}
