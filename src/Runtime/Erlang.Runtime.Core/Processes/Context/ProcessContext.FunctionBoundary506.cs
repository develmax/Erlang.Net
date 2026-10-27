namespace Erlang;

public sealed partial class ProcessContext : ITermInvocationBoundary506
{
    bool ITermInvocationBoundary506.OwnedInvocationEnabled506 => Runtime.OwnedTermsPilot506;

    async ValueTask<Term> ITermInvocationBoundary506.InvokeOwnedFunction506(FunctionTerm function, IReadOnlyList<Term> arguments)
    {
        // Public FunctionTerm.Invoke is always a new host boundary. Inherited
        // ExecutionContext data is never accepted as caller/root admission.
        using var owned = EnterOwnedInvocation506(arguments, true)!;
        FunctionTerm selected = (FunctionTerm)owned.Export(function);
        try
        {
            Term[] portable = owned.Arguments.Select(owned.Export).ToArray();
            Term result = await selected.InvokeDirect506(this, portable);

            return owned.Complete(result);
        }
        catch (ErlangException error)
        {
            throw owned.Transfer(error);
        }
        catch (Exception control) when (control is IOwnedControlTransfer506)
        {
            throw owned.TransferControl506(control);
        }
    }
}
