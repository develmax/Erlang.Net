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

    internal Term Complete(ProcessRuntime runtime, ProcessContext? requester)
    {
        RequireSuccess();
        OwnedTermInvocation506? active = OwnedTermInvocation506.Active;
        if (requester is not null && active is { IsDisposed: false, CanBorrow507: true }
            && ReferenceEquals(active.Runtime, runtime) && active.OwnsContext507(requester)
            && ReferenceEquals(requester.CurrentOwned506, active)
            && active.BindingKind507 == OwnedBindingKind507.RuntimeRead535
            && RuntimeActorExecutionTurn.IsOwnedBy(requester))
            return active.Frame.AdoptGuestInto507(active.ResultSlot507, Value);

        // Ordinary snapshots already contain portable values. Avoid changing
        // public callback/term identity through an unnecessary whole-graph copy.
        if (!NativeResourceRoots524.Contains524(Value) && !AtomicsRoots586.Contains586(Value)) return Value;

        return AtomicsHostAggregate594.Publish(transaction =>
        {
            Term Export(Term value)
            {
                if (!NativeResourceRoots524.Contains524(value) && !AtomicsRoots586.Contains586(value)) return value;
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
                    if (!NativeResourceRoots524.Contains524(pair.Items[1]) && !AtomicsRoots586.Contains586(pair.Items[1])) return pair;

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

    public void Dispose() => Interlocked.Exchange(ref messages, null)?.Dispose();

    private MailboxSnapshot601? messages;
}
