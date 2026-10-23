namespace Erlang.Compiler;

internal sealed class SourceBifReturnObservation : IDisposable
{
    private static readonly AsyncLocal<SourceBifReturnObservation?> Active = new();
    private readonly object gate = new();
    private SourceBifReturnObservation? previous;
    private ModuleDefinition? module;
    private ProcessContext? context;
    private Expr.Call? target;
    private Term? index;
    private Term? tuple;
    private Term? result;
    private int hits;
    private bool closed;

    private SourceBifReturnObservation(
        ModuleDefinition owner,
        ProcessContext caller,
        Expr.Call body,
        Term position,
        Term value
    )
    {
        module = owner;
        context = caller;
        target = body;
        index = position;
        tuple = value;
        previous = Active.Value;
        Active.Value = this;
    }

    internal static SourceBifReturnObservation? Enter(
        ModuleDefinition? module,
        ProcessContext context,
        Expr.Call call,
        Term[] arguments
    )
    {
        if (module is null || !call.CanDiscardBifContinuation || SourceReturnSafety.HasNativeOverrides(module, context)
            || SourceBifForwardingContracts.Resolve(
                module,
                call,
                out int indexSlot,
                out int tupleSlot
            ) is not { } target)
            return null;

        return new(
            module,
            context,
            target,
            arguments[indexSlot],
            arguments[tupleSlot]
        );
    }

    internal static void Record(
        ModuleDefinition? module,
        ProcessContext context,
        Expr.Call call,
        Term[] arguments,
        Term result,
        bool canonical
    )
    {
        if (!canonical || arguments.Length != SourceBifReturnParameters.ElementArity || Active.Value is not { } active)
            return;
        lock (active.gate)
        {
            if (active.closed || !ReferenceEquals(active.module, module) || !ReferenceEquals(active.context, context)
                || !ReferenceEquals(active.target, call) || !active.index!.Equals(arguments[0])
                || !ReferenceEquals(active.tuple, arguments[1]) || SourceReturnSafety.HasNativeOverrides(module!, context))
                return;
            active.result = result;
            active.hits++;
        }
    }

    internal bool Matches(Term value)
    {
        lock (gate)
            return !closed && hits == 1 && ReferenceEquals(result, value);
    }

    public void Dispose()
    {
        SourceBifReturnObservation? restore;
        lock (gate)
        {
            if (closed)
                return;
            closed = true;
            restore = previous;
            previous = null;
            module = null;
            context = null;
            target = null;
            index = tuple = result = null;
        }
        if (ReferenceEquals(Active.Value, this))
            Active.Value = restore;
    }
}
