namespace Erlang;

// One mirror root per logical register, with a second publication slot while
// the old 504 cell is being replaced. Never hold both owners' gates at once.
internal sealed class BeamOwnedRoots507 : IDisposable
{
    private readonly ProcessContext context;
    private readonly ProcessTermFrame506 frame;
    private readonly int[] first;
    private readonly int[] second;
    private readonly bool[] alternate;
    private readonly bool[] published;

    internal BeamOwnedRoots507(ProcessContext context, int count)
    {
        this.context = context;
        frame = context.TermHeap506.Enter();
        try
        {
            first = Enumerable.Range(0, count).Select(_ => frame.Reserve()).ToArray();
            second = Enumerable.Range(0, count).Select(_ => frame.Reserve()).ToArray();
            alternate = new bool[count];
            published = new bool[count];
        }
        catch
        {
            frame.Dispose();
            throw;
        }
    }

    private int Candidate(int index) => alternate[index] ? first[index] : second[index];

    private int Current(int index) => alternate[index] ? second[index] : first[index];

    internal BeamRegisterSlot Prepare(int index, BeamRegisterSlot value, bool adopt)
    {
        if (value.Kind == BeamRegisterSlotKind.Term)
        {
            Term prepared = adopt
                ? frame.AdoptGuestInto507(Candidate(index), value.ReadTerm())
                : frame.SetLocalGraph507(Candidate(index), value.ReadTerm());

            return BeamRegisterSlot.FromTerm(prepared);
        }
        frame.SetLocalGraph507(Candidate(index), value.RootGraph507());

        return value;
    }

    internal Term Tuple(int index, IReadOnlyList<Term> children)
        => frame.CreateTupleInto507(Candidate(index), children);

    internal Term Cons(int index, Term head, Term tail)
        => frame.CreateConsInto507(Candidate(index), head, tail);

    internal void Commit(int index)
    {
        frame.Clear(Current(index));
        alternate[index] = !alternate[index];
        published[index] = true;
    }

    internal void Rollback(int index) => frame.Clear(Candidate(index));

    internal void Clear(int index)
    {
        if (!published[index])
            return;
        frame.Clear(first[index]);
        frame.Clear(second[index]);
        alternate[index] = false;
        published[index] = false;
    }

    // A trusted guest caller owns its invocation root until its bridge returns.
    // Publish there before this interpreter disposes its X/Y mirror frames.
    // Public/opaque and non-actor entries keep the portable export boundary.
    internal Term CompleteGuestReturn507(Term value)
    {
        var invocation = context.CurrentOwned506;
        if (invocation is { IsDisposed: false, CanBorrow507: true }
            && invocation.BindingKind507 != OwnedBindingKind507.Opaque
            && invocation.OwnsContext507(context)
            && RuntimeActorExecutionTurn.IsOwnedBy(context))
            return invocation.HoldGuestResult507(value);

        return Export(value);
    }

    internal Term Export(Term value) => context.TermHeap506.Export(frame, value);

    internal Term[] ExportArguments(IReadOnlyList<Term> values)
        => ((TupleTerm)Export(Term.Tuple(values.ToArray()))).Items.ToArray();

    internal IDisposable Protect(Term value) => new BeamOwnedTemporary507(context, value);

    internal ValueTask ReceiveCheckpoint507(string phase) => BeamOwnedCheckpoint507.Reach(context, phase);

    public void Dispose() => frame.Dispose();
}
