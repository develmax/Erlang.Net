namespace Erlang;

internal sealed partial class BeamRegisterState
{
    internal bool HasOwnedRoots507 => roots507 is not null;

    internal ValueTask ReceiveCheckpoint507(string phase)
        => roots507?.ReceiveCheckpoint507(phase) ?? ValueTask.CompletedTask;

    private void WriteXSlot507(int index, BeamRegisterSlot value, bool adopt)
    {
        BeamRegisterSlot prepared = roots507?.Prepare(index, value, adopt) ?? value;
        try
        {
            values[index] = prepared;
            roots507?.Commit(index);
        }
        catch
        {
            roots507?.Rollback(index);
            throw;
        }
    }

    // Called only immediately after roots507.Tuple/Cons publishes Candidate.
    // The producer's outer catch performs the same rollback as ordinary writes.
    private void CommitConstructedX548(int index, Term value)
    {
        try
        {
            roots507!.RequireConstructedCandidate548(index, value);
            values[index] = BeamRegisterSlot.FromTerm(value);
            roots507.Commit(index);
        }
        catch
        {
            roots507?.Rollback(index);
            throw;
        }
    }

    internal void ApplyLiveHint507(int count)
    {
        CheckLive(count);
        if (roots507 is not null)
            ClearXExcept(Enumerable.Range(0, count).ToArray());
    }

    internal Term CompleteGuestReturn507(Term value) => roots507?.CompleteGuestReturn507(value) ?? value;

    internal Term Export507(Term value) => roots507?.Export(value) ?? value;

    internal Term[] ExportArguments507(Term[] arguments) => roots507?.ExportArguments(arguments) ?? arguments;

    internal ErlangException ExportException507(ErlangException error)
        => roots507 is null ? error : error.WithTransferredTerms506(roots507.Export);

    internal IDisposable? Protect507(params Term[] values)
        => roots507?.Protect(Term.Tuple(values));

    internal bool CanReuseListSource550(BeamOperand source, BeamOperand firstDestination, Term cell)
    {
        if (source.Kind == firstDestination.Kind && source.Value == firstDestination.Value)
            return false;
        // TypeIndex annotates a register; it does not identify a separate slot.
        if (source.Kind == BeamOperandParameters.X)
            return roots507?.HoldsPublishedTerm550((int)source.Value, cell) == true;
        if (source.Kind == BeamOperandParameters.Y && frames.TryPeek(out BeamLocalFrame? local))
            return local.HoldsPublishedTerm550((int)source.Value, cell);

        return false;
    }

    internal void WriteListParts550(
        string opcode,
        BeamOperand source,
        BeamOperand firstDestination,
        BeamOperand? secondDestination,
        Cons cell
    )
    {
        bool pair = opcode == BeamGeneralOpcodeNames.GetList;
        if (pair && secondDestination is null)
            throw new InvalidDataException(BeamRegisterNames.Unsupported);
        bool sourceKeepsCustody = pair && CanReuseListSource550(source, firstDestination, cell);
        using var temporary = HasOwnedRoots507 && !sourceKeepsCustody ? Protect507(cell) : null;
        Term head = cell.Head;
        Term tail = cell.Tail;
        Write(firstDestination, opcode == BeamGeneralOpcodeNames.GetTail ? tail : head);
        if (pair)
            Write(secondDestination!, tail);
    }

    internal void WriteTuple507(BeamOperand destination, IReadOnlyList<Term> children)
    {
        if (roots507 is null)
        {
            Write(destination, new TupleTerm(children));

            return;
        }
        using var builder = Protect507(children.ToArray());
        if (destination.Kind == BeamOperandParameters.Y)
        {
            frames.Peek().WriteTuple507((int)destination.Value, children);

            return;
        }
        int index = (int)destination.Value;
        try
        {
            Term value = roots507?.Tuple(index, children) ?? new TupleTerm(children);
            CommitConstructedX548(index, value);
        }
        catch
        {
            roots507?.Rollback(index);
            throw;
        }
    }

    internal void WriteCons507(BeamOperand destination, Term head, Term tail)
    {
        if (roots507 is null)
        {
            Write(destination, new Cons(head, tail));

            return;
        }
        using var builder = Protect507(head, tail);
        if (destination.Kind == BeamOperandParameters.Y)
        {
            frames.Peek().WriteCons507((int)destination.Value, head, tail);

            return;
        }
        int index = (int)destination.Value;
        try
        {
            Term value = roots507?.Cons(index, head, tail) ?? new Cons(head, tail);
            CommitConstructedX548(index, value);
        }
        catch
        {
            roots507?.Rollback(index);
            throw;
        }
    }
}
