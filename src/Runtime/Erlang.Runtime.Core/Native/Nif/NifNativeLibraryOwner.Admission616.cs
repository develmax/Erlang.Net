namespace Erlang;

internal sealed partial class NifNativeLibraryOwner
{
    public async ValueTask DisposeAsync()
    {
        NifNativeLifecyclePermit616 permit;
        Exception? admissionFailure = null;
        try
        {
            permit = await NifNativeLifecycleGate.AcquireAsync616(CancellationToken.None);
        }
        catch (Exception error)
        {
            admissionFailure = error;
            // A dead/rejected actor is no reason to skip physical retirement.
            // This admission has no actor token or actor scheduler dependency.
            permit = await NifNativeLifecycleGate.AcquireHostAsync616();
        }
        try
        {
            using (permit)
            {
                try
                {
                    await DisposeSynchronous616(permit).ConfigureAwait(false);
                }
                catch (Exception cleanup)
                {
                    throw admissionFailure is null ? cleanup : new AggregateException(admissionFailure, cleanup);
                }
            }
            if (admissionFailure is not null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(admissionFailure).Throw();
        }
        catch (Exception failure)
        {
            lock (gate)
                if (disposed && !ReferenceEquals(disposalFailure, failure))
                    disposalFailure = disposalFailure is null ? failure : new AggregateException(disposalFailure, failure);
            throw;
        }
    }
}
