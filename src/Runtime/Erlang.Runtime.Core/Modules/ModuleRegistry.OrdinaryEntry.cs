namespace Erlang;

public sealed partial class ModuleRegistry
{
    private async ValueTask<Term> InvokeFirstOrdinaryEntry(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments,
        ErlangFunction body,
        ModuleCodeImage? image,
        IDisposable? code,
        OwnedBindingKind507 kind
    )
    {
        OwnedTermInvocation506? boundary = null;
        bool selectedOwnsCode = false;
        try
        {
            // The public bridge was skipped while this context had no backing.
            // Add that actual Host boundary around the ALREADY selected body;
            // its inner canonical frame can borrow instead of exporting inputs
            // into hidden Host carriers. Do not redispatch or reselect an image.
            boundary = OwnedTermInvocation506.CreatePublic414(context, arguments);
            selectedOwnsCode = true;
            Term result = await InvokeSelectedOwned506(
                context,
                module,
                function,
                boundary.Arguments,
                body,
                image,
                code,
                kind
            );

            return boundary.Complete(result);
        }
        catch (ErlangException error) when (boundary is not null)
        {
            throw boundary.Transfer(error);
        }
        catch (Exception control) when (boundary is not null && control is IOwnedControlTransfer506)
        {
            throw boundary.TransferControl506(control);
        }
        finally
        {
            try
            {
                boundary?.Dispose();
            }
            finally
            {
                if (!selectedOwnsCode)
                    code?.Dispose();
            }
        }
    }
}
