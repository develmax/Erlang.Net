namespace Erlang;

public abstract partial record Pattern
{
    private bool IsCanonicalPattern507 => this is Any or Variable or Literal or Tuple or List or AliasPattern or ICanonicalPattern507;

    private bool MatchBoundary507(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context,
        Dictionary<string, Term>? keyScope
    )
    {
        if (context is null || !context.Runtime.OwnedTermsPilot506 || IsCanonicalPattern507)
            return MatchCore(
                value,
                bindings,
                context,
                keyScope
            );
        using var boundary = new OpaquePatternInvocation507(
            context,
            value,
            bindings,
            keyScope
        );
        try
        {
            bool matched = MatchCore(
                boundary.Value,
                boundary.Bindings,
                context,
                boundary.KeyScope
            );

            return boundary.Complete507(matched, bindings, keyScope);
        }
        catch (ErlangException error)
        {
            throw boundary.Transfer507(error);
        }
        catch (Exception control) when (control is IOwnedControlTransfer506)
        {
            throw boundary.TransferControl507(control);
        }
    }

    private ValueTask<bool> MatchBoundaryAsync507(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context,
        Dictionary<string, Term>? keyScope
    )
        => context is null || !context.Runtime.OwnedTermsPilot506 || IsCanonicalPattern507
            ? MatchCoreAsync(
                value,
                bindings,
                context,
                keyScope
            )
            : AwaitOpaquePattern507(
                value,
                bindings,
                context,
                keyScope
            );

    private async ValueTask<bool> AwaitOpaquePattern507(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext context,
        Dictionary<string, Term>? keyScope
    )
    {
        using var boundary = new OpaquePatternInvocation507(
            context,
            value,
            bindings,
            keyScope
        );
        try
        {
            bool matched = await MatchCoreAsync(
                boundary.Value,
                boundary.Bindings,
                context,
                boundary.KeyScope
            );

            return boundary.Complete507(matched, bindings, keyScope);
        }
        catch (ErlangException error)
        {
            throw boundary.Transfer507(error);
        }
        catch (Exception control) when (control is IOwnedControlTransfer506)
        {
            throw boundary.TransferControl507(control);
        }
    }
}
