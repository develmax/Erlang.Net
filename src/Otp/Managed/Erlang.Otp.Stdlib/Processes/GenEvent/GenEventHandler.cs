// Derived from Erlang/OTP 29.1.1 gen_event.erl (Apache-2.0), Ericsson AB.
namespace Erlang;

internal sealed record GenEventHandler(
    Term Module,
    Term Id,
    Term State,
    Term Supervisor
)
{
    internal Term Identity => Id.Equals(Term.A(ErlangBooleanAtoms.False)) ? Module : Term.Tuple(Module, Id);

    internal Term Encode() => Term.Tuple(
        Term.A(GenEventHandlerLayout.Tag),
        Module,
        Id,
        State,
        Supervisor
    );

    internal static GenEventHandler Decode(Term term)
    {
        if (term is not TupleTerm { Items.Count: GenEventHandlerLayout.FieldCount } tuple || !tuple.Items[0].Equals(Term.A(GenEventHandlerLayout.Tag)))
        {
            throw new ErlangException(Term.Tuple(Term.A(GenEventErrorReasons.BadRecord), term));
        }

        return new(
            tuple.Items[GenEventHandlerLayout.ModuleIndex],
            tuple.Items[GenEventHandlerLayout.IdIndex],
            tuple.Items[GenEventHandlerLayout.StateIndex],
            tuple.Items[GenEventHandlerLayout.SupervisorIndex]
        );
    }

    internal static GenEventHandler Create(Term identity, Term supervisor)
        => identity is TupleTerm { Items.Count: GenEventHandlerIdentityLayout.FieldCount } pair
            ? new(
                pair.Items[GenEventHandlerIdentityLayout.ModuleIndex],
                pair.Items[GenEventHandlerIdentityLayout.IdIndex],
                Term.A(GenEventResults.Undefined),
                supervisor
            )
            : new(
                identity,
                Term.A(ErlangBooleanAtoms.False),
                Term.A(GenEventResults.Undefined),
                supervisor
            );
}
