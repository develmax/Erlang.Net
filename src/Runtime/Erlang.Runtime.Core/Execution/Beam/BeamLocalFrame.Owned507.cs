namespace Erlang;

internal sealed partial class BeamLocalFrame
{
    // Only already-published constructor results enter this private path.
    private void CommitConstructedY548(int index, Term value)
    {
        if (roots507 is null)
        {
            WriteRaw504(index, BeamRegisterSlot.FromTerm(value));

            return;
        }
        try
        {
            roots507.RequireConstructedCandidate548(index, value);
            BeamRegisterSlot prepared = BeamRegisterSlot.FromTerm(value);
            if (storage504 is null)
                values![index] = prepared;
            else
                storage504.Write(index, prepared.StoredValue504);
            roots507.Commit(index);
        }
        catch
        {
            roots507.Rollback(index);
            throw;
        }
    }

    internal void WriteTuple507(int index, IReadOnlyList<Term> children)
    {
        if (ReadSlot(index).Kind == BeamRegisterSlotKind.Handler)
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
        try
        {
            Term value = roots507?.Tuple(index, children) ?? new TupleTerm(children);
            CommitConstructedY548(index, value);
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
            CommitConstructedY548(index, value);
        }
        catch
        {
            roots507?.Rollback(index);
            throw;
        }
    }
}
