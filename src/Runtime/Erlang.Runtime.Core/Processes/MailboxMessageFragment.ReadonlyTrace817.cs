namespace Erlang;

internal sealed partial class MailboxMessageFragment
{
    // Outside R/M only: raw/token envelopes have separate physical root slots
    // and are deliberately not admitted by this first single-Value domain.
    internal bool HasSingleReadonlyValueSlot817()
    {
        ProcessTermFrame506? current = Volatile.Read(ref frame);

        return Token is null && current is not null && current.IssuedRootSlotIds507 == 1;
    }

    // Full native cells are constructed before publication, but each backing
    // is still owned solely by the held source Entry operation. The readonly
    // ordinary leases are real one-shot reservations, not admitted shares.
    internal static async ValueTask<(MailboxMessageFragment Fragment, OrdinaryBinaryLease[] Leases)> ReserveReadonlyTrace817Async(
        ProcessContext tracee,
        ReadonlyTraceSource817 source
    )
    {
        ProcessRuntime runtime = tracee.Runtime;
        runtime.AssertSequenceTracePreparation();
        source.RequireValidated();
        if (!ReferenceEquals(source.Runtime, runtime) || !ReferenceEquals(source.Receiver817, tracee))
            throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
        ReadonlyTraceArena817 arena = await ReadonlyTraceArena817.CreateAsync(source).ConfigureAwait(false);
        ProcessTermHeap506 heap = arena.Heap;
        ProcessTermFrame506? frame = arena.Frame;
        try
        {
            ProcessTermConstruction514 custody = frame.BeginConstruction514(Nil.Value);
            var stack = new Stack<ReadonlyTraceBuildFrame817>();
            stack.Push(new(source.Value));
            Term copied;
            int remaining = ReadonlyTrace817Names.MaximumSourceNodes;
            while (true)
            {
                ReadonlyTraceBuildFrame817 current = stack.Peek();
                if (current.Index < current.Children.Length)
                {
                    stack.Push(new(current.Children[current.Index]));
                    continue;
                }
                if (--remaining < 0) throw new InvalidOperationException(ReadonlyTrace817Names.Unsupported);
                lock (heap.Gate)
                {
                    if (current.Source is BitString bits)
                        copied = heap.CreateReservedReadonlyTraceBinary817(
                            arena,
                            bits,
                            custody
                        );
                    else if (current.Source is Cons)
                    {
                        copied = heap.CreateCons(current.Built[0], current.Built[1]);
                        custody.Publish(copied);
                    }
                    else if (current.Source is TupleTerm)
                    {
                        copied = heap.CreateTuple(current.Built);
                        custody.Publish(copied);
                    }
                    else if (current.Source is Atom or Nil || current.Source is Integer integer && ProcessTermHeap506.IsImmediateInteger559(integer))
                    {
                        copied = current.Source;
                        custody.Admit(copied);
                    }
                    else throw new InvalidOperationException(ReadonlyTrace817Names.Unsupported);
                }
                stack.Pop();
                if (stack.Count == 0) break;
                ReadonlyTraceBuildFrame817 parent = stack.Peek();
                parent.Built[parent.Index++] = copied;
            }
            int slot = frame.Reserve();
            Term notification = frame.CreateTupleInto507(slot,
                new Term[] { Term.A(ProcessTraceNames.Message), tracee.Self, Term.A(ProcessTraceNames.Receive), copied });

            return (new(
                heap,
                frame,
                notification,
                true
            ), arena.Leases.ToArray());
        }
        catch (Exception original)
        {
            List<Exception> errors = [original];
            try
            {
                frame?.Dispose();
            }
            catch (Exception cleanup)
            {
                errors.Add(cleanup);
            }
            try
            {
                await heap.RetireAsync().ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                errors.Add(cleanup);
            }
            if (errors.Count > 1) throw new AggregateException(errors);
            throw;
        }
    }
}
