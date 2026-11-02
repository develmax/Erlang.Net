namespace Erlang;

// One mirror root per logical register, with a second publication slot while
// the old 504 cell is being replaced. Never hold both owners' gates at once.
internal sealed partial class BeamOwnedRoots507 : IDisposable
{
    private readonly ProcessContext context;
    private readonly ProcessTermFrame506 frame;
    private readonly int[] first;
    private readonly int[] second;
    private readonly bool[] alternate;
    private readonly bool[] published;
    private readonly bool[] reserved507;

    internal BeamOwnedRoots507(ProcessContext context, int count)
    {
        this.context = context;
        frame = context.TermHeap506.Enter();
        try
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            first = new int[count];
            second = new int[count];
            alternate = new bool[count];
            published = new bool[count];
            reserved507 = new bool[count];
        }
        catch
        {
            frame.Dispose();
            throw;
        }
    }

    private void EnsureSlots507(int index)
    {
        if (reserved507[index])
            return;
        var pair = frame.ReservePair507();
        first[index] = pair.First;
        second[index] = pair.Second;
        reserved507[index] = true;
    }

    private int Candidate(int index)
    {
        EnsureSlots507(index);

        return alternate[index] ? first[index] : second[index];
    }

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

    // Only the tuple/list constructors publish these candidates. Verify the
    // actual registered root; never infer custody from a value's type or cache.
    internal void RequireConstructedCandidate548(int index, Term value)
    {
        if (!ReferenceEquals(frame.Read(Candidate(index)), value))
            throw new InvalidDataException(BeamConstructedCommit548Names.CandidateMismatch);
    }

    // Proof of an existing live published slot, never a type/cache inference.
    internal bool HoldsPublishedTerm550(int index, Term value)
        => published[index] && ReferenceEquals(frame.Read(Current(index)), value);

    internal void Commit(int index)
    {
        EnsureSlots507(index);
        frame.Clear(Current(index));
        alternate[index] = !alternate[index];
        published[index] = true;
    }

    internal void Rollback(int index)
    {
        if (reserved507[index])
            frame.Clear(Candidate(index));
        else
            frame.RequireLive507();
    }

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
