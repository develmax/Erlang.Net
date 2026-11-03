using System.Runtime.ExceptionServices;

namespace Erlang;

// Also serves as the composite origin's disposable group after handoff.
internal sealed class ProcessInfoSnapshot601 : IDisposable
{
    internal MailboxSnapshot601? Messages { get => messages; set => messages = value; }
    internal Term Value { get; set; } = ProcessInfoAtoms.Undefined;
    internal Exception? Failure { get; set; }

    internal void RequireSuccess()
    {
        if (Failure is { } failure) ExceptionDispatchInfo.Capture(failure).Throw();
        if (Messages?.Failure is { } messageFailure) ExceptionDispatchInfo.Capture(messageFailure).Throw();
    }

    internal ModuleLiteralRoots801? RetainLiteralRoots801() => Messages?.RetainLiteralRoots801();

    internal Term Complete(ProcessRuntime runtime, ProcessContext? requester)
        => CompleteWithLiteral801(runtime, requester, null);

    internal Term CompleteWithLiteral801(
        ProcessRuntime runtime,
        ProcessContext? requester,
        ModuleLiteralRoots801? carriedLiteral801
    )
    {
        RequireSuccess();
        OwnedTermInvocation506? active = OwnedTermInvocation506.Active;
        if (requester is not null && active is { IsDisposed: false, CanBorrow507: true }
            && ReferenceEquals(active.Runtime, runtime) && active.OwnsContext507(requester)
            && ReferenceEquals(requester.CurrentOwned506, active)
            && active.BindingKind507 == OwnedBindingKind507.RuntimeRead535
            && RuntimeActorExecutionTurn.IsOwnedBy(requester))
        {
            // The actual snapshot interests stay held through cross-process
            // import. Target fragments are not requester-local guest roots.
            using ModuleLiteralRoots801? capturedLiteral801 = RetainLiteralRoots801();
            using ModuleLiteralRoots801? combinedLiteral801 = ModuleLiteralRoots801.RetainCombined801(
                [capturedLiteral801, carriedLiteral801]);

            return MailboxValueEscape.ImportGuestWithLiteral801(
                runtime,
                active.Frame,
                Value,
                combinedLiteral801
            );
        }

        // Ordinary snapshots already contain portable values. Avoid changing
        // public callback/term identity through an unnecessary whole-graph copy.
        if (!MailboxValueEscape.RequiresDetach(Value)) return Value;

        return AtomicsHostAggregate594.Publish(transaction =>
        {
            Term Export(Term value)
            {
                if (!MailboxValueEscape.RequiresDetach(value)) return value;
                Term portable = OwnedTermDetachedCopy506.CopyAtomics586(
                    runtime,
                    value,
                    exportHostAtomics586: true,
                    hostCopies591: transaction
                );

                return NativeResourceRoots524.Contains524(portable)
                    ? NativeTermHostScope524.AdmitCurrent524(runtime, portable) : portable;
            }

            Term Field(Term field)
            {
                // This class holds the actual process_info projection produced
                // by ReadProcessInfoSnapshot601, not arbitrary user metadata.
                if (field is TupleTerm { Items.Count: 2 } pair
                    && pair.Items[0] is Atom { Name: ProcessInfoNames.Messages })
                {
                    if (!MailboxValueEscape.RequiresDetach(pair.Items[1])) return pair;

                    return Term.Tuple(pair.Items[0], Cons.From(Cons.Items(pair.Items[1]).Select(Export)));
                }

                return Export(field);
            }

            return Value is TupleTerm ? Field(Value)
                : Value is Cons ? Cons.From(Cons.Items(Value).Select(Field)) : Export(Value);
        });
    }

    internal Term CompleteAndDispose(ProcessRuntime runtime, ProcessContext? requester)
    {
        Exception? primary = null;
        try
        {
            return Complete(runtime, requester);
        }
        catch (Exception error)
        {
            primary = error;
            throw;
        }
        finally
        {
            try
            {
                Dispose();
            }
            catch (Exception cleanup) when (primary is not null)
            {
                throw new AggregateException(primary, cleanup);
            }
        }
    }

    internal AtomicsHostCopyTransaction591 CreateDictionaryHostCopies611()
    {
        var copies = new AtomicsHostCopyTransaction591();
        // Allocations occur before this holder owns any native Host ticket.
        // Capture is serialized before publication; origin disposal begins only
        // after capture completes or records its failure.
        var node = new DictionaryHostCopies611(copies, dictionaryHostCopies611);
        dictionaryHostCopies611 = node;

        return copies;
    }

    public void Dispose()
    {
        MailboxSnapshot601? capturedMessages = Interlocked.Exchange(ref messages, null);
        DictionaryHostCopies611? copies = Interlocked.Exchange(ref dictionaryHostCopies611, null);
        Exception? messageFailure = null;
        try
        {
            capturedMessages?.Dispose();
        }
        catch (Exception error)
        {
            messageFailure = error;
        }
        bool copyFailure = false;
        for (DictionaryHostCopies611? node = copies; node is not null; node = node.Next)
            try
            {
                node.Copies.Dispose();
            }
            catch (Exception error)
            {
                node.Failure = error;
                copyFailure = true;
            }
        // No error aggregation allocation can prevent an independent release.
        if (messageFailure is not null || copyFailure)
            throw new AggregateException(Failures611(messageFailure, copies));
    }

    private static IEnumerable<Exception> Failures611(Exception? messageFailure, DictionaryHostCopies611? copies)
    {
        if (messageFailure is not null) yield return messageFailure;
        for (DictionaryHostCopies611? node = copies; node is not null; node = node.Next)
            if (node.Failure is { } failure) yield return failure;
    }

    private sealed class DictionaryHostCopies611(AtomicsHostCopyTransaction591 copies, DictionaryHostCopies611? next)
    {
        internal AtomicsHostCopyTransaction591 Copies { get; } = copies;
        internal DictionaryHostCopies611? Next { get; } = next;
        internal Exception? Failure { get; set; }
    }

    private DictionaryHostCopies611? dictionaryHostCopies611;

    private MailboxSnapshot601? messages;
}
