namespace Erlang.Otp;

internal static class SystemMessageReply
{
    public static async ValueTask Send(ProcessContext context, Term from, Term reply)
    {
        if (from is not TupleTerm { Items: [var destination, var tag] })
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        ReferenceTerm? alias = tag switch
        {
            Cons { Head: Atom { Name: SysTerminationNames.Alias }, Tail: ReferenceTerm reference } => reference,
            Cons { Head: Cons { Head: Atom { Name: SysTerminationNames.Alias }, Tail: ReferenceTerm reference } } => reference,
            _ => null
        };
        if (alias is not null)
        {
            await ErlangOperatorValues.TrySendAsync(
                context,
                alias,
                Term.Tuple(tag, reply)
            );

            return;
        }
        try
        {
            await ErlangOperatorValues.TrySendAsync(
                context,
                destination,
                Term.Tuple(tag, reply)
            );
        }
        catch (ErlangException)
        {
            // gen:reply catches send failures only in its ordinary destination clause.
        }
    }
}
