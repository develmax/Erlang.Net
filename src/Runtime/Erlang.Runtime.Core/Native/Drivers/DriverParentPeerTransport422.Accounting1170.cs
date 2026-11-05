namespace Erlang;

internal sealed partial class DriverParentPeerTransport422
{
    // Global unmanaged accounting controls are not port tasks and cannot borrow
    // a fake normal permit. Only this closed operation set has zero metadata.
    internal async Task<byte[]> AsyncAccounting1170(ErlDriverHostOperation420 operation, byte[] data)
    {
        if (operation is not (ErlDriverHostOperation420.AsyncAccountingCapture1151
            or ErlDriverHostOperation420.AsyncAccountingApply1151
            or ErlDriverHostOperation420.AsyncAccountingFinish1151
            or ErlDriverHostOperation420.AsyncAccountingRelease1151))
            throw new InvalidDataException(DriverParentWireNames422.Invalid);
        var response = await peer.RequestAsync(
            operation,
            0,
            0,
            0,
            0,
            null,
            data,
            CancellationToken.None,
            schedulerThread: false
        ).ConfigureAwait(false);
        if (response.Version != ErlDriverHostNames420.Version || response.Session != peer.Session438
            || response.Kind != ErlDriverHostKind420.Response || response.Operation != operation || response.Request == 0
            || response.Driver != 0 || response.Port != 0 || response.Actor != 0 || response.Command != 0
            || response.NativeResult != 0 || response.Name is not null || response.SchedulerThread || response.Data is null)
            throw new InvalidDataException(DriverParentWireNames422.Invalid);

        // Peer has already joined the genuine ordered event prefix. Responses
        // may have a nonzero EventSequence after ordinary child output.
        return response.Data;
    }
}
