namespace Erlang;

// context:null work ensures queued delivery does not postpone sender Done.
internal sealed class PreparedSequenceTraceTransport641 : IDisposable
{
    private RuntimeProcessPayload641? payload;
    internal SequenceTraceToken Token { get; }
    internal RuntimeProcessPayload641 Payload641 => payload
        ?? throw new ObjectDisposedException(nameof(PreparedSequenceTraceTransport641));

    internal ModuleLiteralRoots801? LiteralRoots814 => Payload641.Graph641?.LiteralRoots801;

    private PreparedSequenceTraceTransport641(RuntimeProcessPayload641 payload, SequenceTraceToken token)
    {
        this.payload = payload;
        Token = token;
    }

    // Source custody must cover every field while Capture reads it. Portable
    // and native preparation happen before R/M, never from a locked producer.
    internal static PreparedSequenceTraceTransport641 Capture(ProcessRuntime runtime, SequenceTraceToken token)
    {
        runtime.AssertSequenceTracePreparation();
        using ModuleLiteralRoots801? sourceLiteral = SequenceTraceLiteral814.CapturePhysical(runtime, token);
        RuntimeProcessPayload641 owner = RuntimeProcessPayload641.PrepareWithLiteral814(
            runtime,
            null,
            token.ToTerm(),
            sourceLiteral
        );
        try
        {
            return new(owner, SequenceTraceToken.FromPortable((TupleTerm)owner.Value641));
        }
        catch
        {
            owner.Close641();
            throw;
        }
    }

    internal static PreparedSequenceTraceTransport641 FromPayload641(RuntimeProcessPayload641 origin, TupleTerm tokenSubterm)
    {
        origin.Runtime641.AssertSequenceTracePreparation();
        if (!ContainsReference(origin.Value641, tokenSubterm))
            throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
        SequenceTraceToken token = SequenceTraceToken.FromPortable(tokenSubterm);
        RuntimeProcessPayload641 owner = origin.Borrow641(null);

        return new(owner, token);
    }

    // Only scalar fields and genuine local immediate Sender may change under R.
    // Arbitrary Label and external Sender remain the exact covered objects.
    internal PreparedSequenceTraceTransport641 ForkMetadata(
        SequenceTraceFlags flags,
        long serial,
        Term sender,
        long last
    )
    {
        RuntimeProcessPayload641 owner = Payload641;
        if ((long)flags < SequenceTraceParameters.MinimumSerial || (long)flags > SequenceTraceParameters.MaximumSerial ||
            serial < SequenceTraceParameters.MinimumSerial || serial > SequenceTraceParameters.MaximumSerial ||
            last < SequenceTraceParameters.MinimumSerial || last > SequenceTraceParameters.MaximumSerial ||
            !ReferenceEquals(sender, Token.Sender) && sender is not Atom &&
            !(sender is Pid pid && ReferenceEquals(pid.LocalIdentity, owner.Runtime641.LocalIdentity)))
            throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
        SequenceTraceToken next = Token with { Flags = flags, Serial = serial, Sender = sender, LastCount = last };
        RuntimeProcessPayload641 borrowed = owner.Borrow641(null);

        return new(borrowed, next);
    }

    private static bool ContainsReference(Term root, Term candidate)
    {
        var pending = new Stack<Term>();
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        pending.Push(root);
        while (pending.TryPop(out Term? value))
        {
            if (ReferenceEquals(value, candidate))
                return true;
            if (!seen.Add(value))
                continue;
            foreach (Term child in NativeMapOwnedRoots510.Children(value))
                pending.Push(child);
        }

        return false;
    }

    public void Dispose() => Interlocked.Exchange(ref payload, null)?.Close641();
}
