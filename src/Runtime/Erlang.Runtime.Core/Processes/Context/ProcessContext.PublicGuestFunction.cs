namespace Erlang;

public sealed partial class ProcessContext
{
    private async ValueTask<Term> InvokePublicGuestFunction(FunctionTerm function, OwnedTermInvocation506 boundary)
    {
        // Import/root the real callable without manufacturing an unreturned
        // Host copy of its captured resources. The caller already has a Host
        // boundary; only an exact admitted recipe reaches the guest path.
        FunctionTerm selected = (FunctionTerm)boundary.Frame.Retain(function);
        using var admission = EnterOwnedBeamFunAdmission650(function, selected);
        try
        {
            Term result = await InvokeGuestFunction507(selected, boundary.Arguments);

            return boundary.Complete(result);
        }
        catch (ErlangException error)
        {
            throw boundary.Transfer(error);
        }
        catch (Exception control) when (control is IOwnedControlTransfer506)
        {
            throw boundary.TransferControl506(control);
        }
    }
}
