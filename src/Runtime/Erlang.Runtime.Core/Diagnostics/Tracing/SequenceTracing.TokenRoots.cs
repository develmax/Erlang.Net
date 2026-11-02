namespace Erlang;

internal sealed partial class SequenceTracing
{
    private Term SetLabelPrepared(ProcessContext context, Term value)
    {
        runtime.AssertSequenceTracePreparation();
        while (true)
        {
            SequenceTraceToken? previous;
            ProcessTermBorrow506? borrow;
            ulong last;
            lock (gate)
            {
                runtime.ThrowIfHalted();
                previous = context.SequentialTrace.Token;
                borrow = context.SequentialTrace.BorrowPhysical();
                last = context.SequentialTrace.LastCount;
            }
            using (borrow)
            {
                var hostCopies429 = context.CurrentOwned506 is null ? new AtomicsHostCopyTransaction591() : null;
                Exception? primary429 = null;
                try
                {
                    SequenceTraceToken initial = previous ?? new(
                        SequenceTraceFlags.None,
                        Term.I(0),
                        0,
                        context.Self,
                        SequenceTraceParameters.Signed(last)
                    );
                    Term result = PreservePreviousResult(context, initial.Label, hostCopies429);
                    SequenceTraceToken replacement = initial with { Label = value };
                    using PreparedSequenceTraceTransport641? transport = context.CurrentOwned506 is null
                        ? PreparedSequenceTraceTransport641.Capture(runtime, replacement) : null;
                    SequenceTraceStorage? prepared = transport is null
                        ? SequenceTraceStorage.CreateLocal(context, replacement)
                        : SequenceTraceStorage.CreatePreparedWithLiteral814(context, transport.Token, transport.LiteralRoots814);
                    try
                    {
                        lock (gate)
                        {
                            runtime.ThrowIfHalted();
                            if (!ReferenceEquals(previous, context.SequentialTrace.Token)) continue;
                            context.SequentialTrace.InstallPrepared(prepared);
                            prepared = null;

                            hostCopies429?.CommitWrappers593();

                            return result;
                        }
                    }
                    finally
                    {
                        prepared?.Dispose();
                    }
                }
                catch (Exception error)
                {
                    primary429 = error;
                    throw;
                }
                finally
                {
                    DisposeSpeculativeResult429(hostCopies429, primary429);
                }
            }
        }
    }

    private Term ClearTokenPrepared(ProcessContext context)
    {
        runtime.AssertSequenceTracePreparation();
        while (true)
        {
            SequenceTraceToken? previous;
            ProcessTermBorrow506? borrow;
            lock (gate)
            {
                runtime.ThrowIfHalted();
                previous = context.SequentialTrace.Token;
                if (previous is null) return Nil.Value;
                borrow = context.SequentialTrace.BorrowPhysical();
            }
            using (borrow)
            {
                var hostCopies429 = context.CurrentOwned506 is null ? new AtomicsHostCopyTransaction591() : null;
                Exception? primary429 = null;
                try
                {
                    Term result = PreservePreviousResult(context, previous.PhysicalTuple ?? previous.ToTerm(), hostCopies429);
                    lock (gate)
                    {
                        runtime.ThrowIfHalted();
                        if (!ReferenceEquals(previous, context.SequentialTrace.Token)) continue;
                        context.SequentialTrace.Token = null;

                        hostCopies429?.CommitWrappers593();

                        return result;
                    }
                }
                catch (Exception error)
                {
                    primary429 = error;
                    throw;
                }
                finally
                {
                    DisposeSpeculativeResult429(hostCopies429, primary429);
                }
            }
        }
    }

    private static void DisposeSpeculativeResult429(AtomicsHostCopyTransaction591? copies, Exception? primary)
    {
        try
        {
            copies?.Dispose();
        }
        catch (Exception cleanup) when (primary is not null)
        {
            throw new AggregateException(primary, cleanup);
        }
    }

    // Called while the old state is still borrowed. Guest results acquire a
    // real invocation root before old-state close; Host results detach before R
    // and acquire actual native Host custody before the source borrow ends.
    private Term PreservePreviousResult(ProcessContext context, Term value, AtomicsHostCopyTransaction591? hostCopies)
    {
        if (context.CurrentOwned506 is { } invocation)
        {
            if (value is TupleTerm { OwnedStorage506: null } tuple)
                return invocation.Frame.CreateTupleInto507(invocation.Frame.Reserve(), tuple.Items.ToArray());

            return invocation.Frame.RetainLocalGraph507(value);
        }
        Term portable = OwnedTermDetachedCopy506.CopyAtomics586(
            runtime,
            value,
            exportHostAtomics586: true,
            hostCopies591: hostCopies
        );

        return NativeResourceRoots524.Contains524(portable)
            ? NativeTermHostScope524.AdmitCurrent524(runtime, portable) : portable;
    }
}
