namespace Erlang;

internal sealed class BeamModuleApplyTailScope : IDisposable
{
    private static readonly AsyncLocal<BeamModuleApplyTailScope?> current = new();
    private readonly BeamModuleApplyTailScope? previous;
    private readonly ProcessContext context;
    private readonly Action transfer;
    private int consumed;

    internal BeamModuleApplyTailScope(ProcessContext context, Action transfer)
    {
        this.context = context;
        this.transfer = transfer;
        previous = current.Value;
        current.Value = this;
    }

    internal static bool IsModuleApply(BeamExternalImport import)
        => import.Module == ErlangModuleNames.Module
            && import.Function == ErlangBasicBifNames.Apply
            && import.Arity == ErlangApplyLayout.ModuleApplyArity;

    internal static bool TryTransfer(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments,
        out ValueTask<Term> pending
    )
    {
        pending = default;
        var scope = current.Value;
        if (scope is null || !ReferenceEquals(scope.context, context) || Interlocked.Exchange(ref scope.consumed, 1) != 0)
            return false;
        if (module == ErlangModuleNames.Module
            && (function is ErlangModuleNames.Error or ErlangModuleNames.Exit or ErlangModuleNames.Throw)
            && context.Runtime.Modules.TryInvokeCanonicalNoReturn(
                context,
                module,
                function,
                arguments,
                out pending
            ))
            return true;
        if (module == ErlangModuleNames.Module && function == SourceLoadNifNames.Function && arguments.Length == 2)
            return false;
        scope.transfer();
        pending = ErlangApplyDispatch.InvokeTarget(
            context,
            module,
            function,
            arguments
        );

        return true;
    }

    public void Dispose() => current.Value = previous;
}
