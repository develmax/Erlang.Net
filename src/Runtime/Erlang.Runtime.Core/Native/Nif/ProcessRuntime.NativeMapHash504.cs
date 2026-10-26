namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal HamtHashLeases429 RetainNativeMapHashes504(OtpMapRuntimeIdentityProvider hashes)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(hamtAdmissionClosed429 || disposed || IsHaltRequested, this);
            var owner = hamtIndex429 ??= new(this);

            return hashes.TransferHamtLeases429(owner);
        }
    }
}
