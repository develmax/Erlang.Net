namespace Erlang;

// One actual child roster lease, retained through Apply/Finish/Release.
internal sealed class ErlDriverAsyncAccountingCapture1151
{
    internal ErlDriverNativeAsyncAccounting1126.Lease? Lease { get; }
    internal ErlDriverAsyncAccountingCaptureReply1151 Metadata { get; }
    internal bool Applied, Finished, Failed, Busy;
    internal string? Action;
    internal TaskCompletionSource? Running;

    internal ErlDriverAsyncAccountingCapture1151(
        ErlDriverNativeAsyncAccounting1126.Lease? lease,
        ErlDriverAsyncAccountingCaptureReply1151 metadata
    )
    {
        Lease = lease;
        Metadata = metadata;
    }
}
