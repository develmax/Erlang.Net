namespace Erlang;

public sealed class RuntimeExternalTermReservation : IDisposable
{
    private Term? value;
    private IDisposable? native524;
    private IDisposable? hostCopies591;

    internal RuntimeExternalTermReservation(ProcessRuntime owner, Term value, bool hostAtomics586)
    {
        // The caller retains the existing runtime admission monitor through
        // construction, so VM close cannot invalidate Host custody mid-copy.
        native524 = NativeResourceRoots524.RetainGraph524(value, owner, hostAtomics586: hostAtomics586);
        AtomicsHostCopyTransaction591? transaction = null;
        try
        {
            if (hostAtomics586 && AtomicsRoots586.Contains586(value))
            {
                transaction = new AtomicsHostCopyTransaction591();
                this.value = OwnedTermDetachedCopy506.CopyAtomics586(
                    owner,
                    value,
                    exportHostAtomics586: true,
                    hostCopies591: transaction
                );
                hostCopies591 = transaction.Commit591();
                transaction = null;
            }
            else
                // The store custody above retains the native allocation, not
                // the originating process's actual reference header. Snapshot
                // the stable identity before those source cells may retire.
                this.value = value is ResourceReferenceTerm identity
                    && (identity.ResourceKind == ReferenceResourceKind.Atomics413
                        || !hostAtomics586 && identity.ResourceKind == ReferenceResourceKind.NativeResource524)
                    ? new ResourceReferenceTerm(identity.Entry, new ReferenceTerm(identity)) : value;
        }
        catch (Exception original)
        {
            Exception? copyFailure = null, rootFailure = null;
            try
            {
                transaction?.Dispose();
            }
            catch (Exception e)
            {
                copyFailure = e;
            }
            try
            {
                Interlocked.Exchange(ref hostCopies591, null)?.Dispose();
            }
            catch (Exception e)
            {
                copyFailure ??= e;
            }
            try
            {
                Interlocked.Exchange(ref native524, null)?.Dispose();
            }
            catch (Exception e)
            {
                rootFailure = e;
            }
            if (copyFailure is not null || rootFailure is not null)
                throw new AggregateException(new[] { original, copyFailure, rootFailure }.OfType<Exception>());
            throw;
        }
    }

    public Term Value => Volatile.Read(ref value) ?? throw new ObjectDisposedException(nameof(RuntimeExternalTermReservation));

    public void Dispose()
    {
        Interlocked.Exchange(ref value, null);
        Exception? copyFailure = null, rootFailure = null;
        try
        {
            Interlocked.Exchange(ref hostCopies591, null)?.Dispose();
        }
        catch (Exception e)
        {
            copyFailure = e;
        }
        try
        {
            Interlocked.Exchange(ref native524, null)?.Dispose();
        }
        catch (Exception e)
        {
            rootFailure = e;
        }
        if (copyFailure is not null && rootFailure is not null) throw new AggregateException(copyFailure, rootFailure);
        if (copyFailure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(copyFailure).Throw();
        if (rootFailure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(rootFailure).Throw();
    }
}
