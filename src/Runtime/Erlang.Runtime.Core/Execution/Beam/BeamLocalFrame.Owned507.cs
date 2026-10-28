namespace Erlang;

internal sealed partial class BeamLocalFrame
{
    internal void WriteTuple507(int index, IReadOnlyList<Term> children)
    {
        if (ReadSlot(index).Kind == BeamRegisterSlotKind.Handler)
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
        try
        {
            Term value = roots507?.Tuple(index, children) ?? new TupleTerm(children);
            WriteRaw504(index, BeamRegisterSlot.FromTerm(value));
        }
        catch
        {
            roots507?.Rollback(index);
            throw;
        }
    }

    internal void WriteCons507(int index, Term head, Term tail)
    {
        if (ReadSlot(index).Kind == BeamRegisterSlotKind.Handler)
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
        try
        {
            Term value = roots507?.Cons(index, head, tail) ?? new Cons(head, tail);
            WriteRaw504(index, BeamRegisterSlot.FromTerm(value));
        }
        catch
        {
            roots507?.Rollback(index);
            throw;
        }
    }
}
