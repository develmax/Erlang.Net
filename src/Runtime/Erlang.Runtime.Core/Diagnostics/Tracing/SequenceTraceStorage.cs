namespace Erlang;

// This is the actual process GC root, not a CLR token mirror or statistics
// shadow. The six-word tuple and every boxed child reside on the process heap.
internal sealed class SequenceTraceStorage : IDisposable
{
    private readonly ProcessContext context;
    private ProcessTermFrame506? frame;
    private readonly TupleTerm tuple;
    internal SequenceTraceToken Token { get; private set; }

    private SequenceTraceStorage(ProcessContext context, ProcessTermFrame506 frame, TupleTerm tuple)
    {
        this.context = context;
        this.frame = frame;
        this.tuple = tuple;
        Token = SequenceTraceToken.FromPhysical(tuple);
    }

    // Actual same-context guest input retains the existing label header and
    // writable-binary identity. This does not export or pin a writable label.
    internal static SequenceTraceStorage CreateLocal(ProcessContext context, SequenceTraceToken token)
    {
        ProcessTermFrame506 frame = context.TermHeap506.Enter();
        try
        {
            int slot = frame.Reserve();
            TupleTerm tuple = frame.CreateMutableSequenceTokenInto550(slot, token);

            return new(context, frame, tuple);
        }
        catch
        {
            frame.Dispose();
            throw;
        }
    }

    // Native/code preparation for arbitrary labels happens outside R/M.
    internal static SequenceTraceStorage CreatePrepared(ProcessContext context, SequenceTraceToken portable)
        => CreatePreparedWithLiteral814(context, portable, null);

    internal static SequenceTraceStorage CreatePreparedWithLiteral814(
        ProcessContext context, SequenceTraceToken portable, ModuleLiteralRoots801? carriedLiteral801)
    {
        ProcessTermFrame506 frame = context.TermHeap506.Enter();
        try
        {
            int slot = frame.Reserve();
            TupleTerm tuple = frame.PrepareMutableSequenceTokenWithLiteral814(slot, portable, carriedLiteral801);

            return new(context, frame, tuple);
        }
        catch
        {
            frame.Dispose();
            throw;
        }
    }

    internal SequenceTraceToken RewriteMetadata(SequenceTraceToken next)
    {
        ObjectDisposedException.ThrowIf(frame is null, this);
        Token = context.TermHeap506.RewriteSequenceToken(tuple, Token, next);

        return Token;
    }

    internal ProcessTermBorrow506 Borrow()
        => context.TermHeap506.Borrow(frame ?? throw new ObjectDisposedException(nameof(SequenceTraceStorage)), tuple);

    public void Dispose() => Interlocked.Exchange(ref frame, null)?.Dispose();
}
