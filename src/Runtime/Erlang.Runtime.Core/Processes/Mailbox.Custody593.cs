using System.Runtime.ExceptionServices;

namespace Erlang;

public sealed partial class Mailbox
{
    private bool closed593;
    internal ProcessRuntime? Runtime593 { get; init; }
    internal MailboxRetirementTracker593 PayloadRetirements593 { get; } = new();

    private MailboxEntryCustody593? PrepareCustody593(MailboxPublicationScope593? publication)
    {
        if (publication is null || Volatile.Read(ref closed593))
            return null;

        if (!ReferenceEquals(Runtime593, publication.Runtime))
            throw new NotSupportedException(ProcessTerm506Names.ForeignRoot);

        MailboxForkResult593 prepared = publication.Runtime.MailboxRoots593.ForkForPublication(publication, PayloadRetirements593);
        if (prepared.Failure is not null)
        {
            // A target that closed during admission remains a dropped send;
            // the publication scope still owns rejected-work cleanup outside R.
            if (prepared.Failure is ObjectDisposedException && Volatile.Read(ref closed593))
                return null;

            ExceptionDispatchInfo.Capture(prepared.Failure).Throw();
        }

        return prepared.Admitted is { } work ? new MailboxEntryCustody593(work) : null;
    }

    private Term OpaqueCandidate593(Term value)
    {
        if (Runtime593 is not { MessageSnapshotRouteReady593: true } runtime)
            return value;

        if (!MailboxValueEscape.RequiresDetach(value))
            return value;

        return AtomicsHostAggregate594.Publish(transaction => OpaqueCandidate593(value, transaction));
    }

    private Term OpaqueCandidate593(Term value, AtomicsHostCopyTransaction591 transaction)
    {
        if (Runtime593 is not { MessageSnapshotRouteReady593: true } runtime)
            return value;

        if (!MailboxValueEscape.RequiresDetach(value))
            return value;

        Term portable = OwnedTermDetachedCopy506.CopyAtomics586(
            runtime,
            value,
            exportHostAtomics586: true,
            hostCopies591: transaction
        );

        return NativeResourceRoots524.Contains524(portable)
            ? NativeTermHostScope524.AdmitCurrent524(runtime, portable) : portable;
    }

    internal int AdmittedCount593
    {
        get
        {
            lock (gate) return admittedCount;
        }
    }

    internal Task DrainPayloadRoots593() => PayloadRetirements593.DrainAsync();
}
