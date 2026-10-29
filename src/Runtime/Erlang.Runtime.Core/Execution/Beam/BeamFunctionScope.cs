namespace Erlang;

internal sealed class BeamFunctionScope : IDisposable
{
    private readonly ProcessContext context;
    private readonly IDisposable function;
    private IDisposable? location;
    private IDisposable? continuation;
    private Term? currentLocation;
    private readonly BeamOwnedRoots507? roots507;

    internal BeamFunctionScope(ProcessContext context, BeamLinkedFunction target, IReadOnlyList<Term> arguments)
    {
        this.context = context;
        Arguments = Array.AsReadOnly(arguments.ToArray());
        if (context.OwnedExecutionEnabled683)
            roots507 = new(context, 2);
        try
        {
            roots507?.Prepare(0, BeamRegisterSlot.FromTerm(Term.Tuple(arguments.ToArray())), false);
            roots507?.Commit(0);
            function = context.EnterBeamFunction(target.Module, target.Name, target.Arity);
        }
        catch
        {
            roots507?.Dispose();
            throw;
        }
    }

    internal IReadOnlyList<Term> Arguments { get; }

    internal void SetContinuation(BeamLinkedCode code, int instructionIndex)
    {
        ClearContinuation();
        continuation = context.EnterBeamContinuation(code.ContinuationIdentities.GetOrAdd(instructionIndex, _ => new object()));
    }

    internal void ClearContinuation()
    {
        continuation?.Dispose();
        continuation = null;
    }

    internal void SetLocation(Term value)
    {
        if (ReferenceEquals(currentLocation, value))
            return;
        location?.Dispose();
        roots507?.Prepare(1, BeamRegisterSlot.FromTerm(value), false);
        try
        {
            location = context.EnterSourceLocation(value);
            roots507?.Commit(1);
        }
        catch
        {
            roots507?.Rollback(1);
            throw;
        }
        currentLocation = value;
    }

    public void Dispose()
    {
        try
        {
            ClearContinuation();
            location?.Dispose();
            location = null;
            function.Dispose();
        }
        finally
        {
            roots507?.Dispose();
        }
    }
}
