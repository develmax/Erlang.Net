namespace Erlang;

internal sealed class BeamFunctionScope : IDisposable
{
    private readonly ProcessContext context;
    private readonly IDisposable function;
    private IDisposable? location;
    private IDisposable? continuation;
    private Term? currentLocation;

    internal BeamFunctionScope(ProcessContext context, BeamLinkedFunction target, IReadOnlyList<Term> arguments)
    {
        this.context = context;
        Arguments = Array.AsReadOnly(arguments.ToArray());
        function = context.EnterBeamFunction(target.Module, target.Name, target.Arity);
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
        location = context.EnterSourceLocation(value);
        currentLocation = value;
    }

    public void Dispose()
    {
        ClearContinuation();
        location?.Dispose();
        location = null;
        function.Dispose();
    }
}
