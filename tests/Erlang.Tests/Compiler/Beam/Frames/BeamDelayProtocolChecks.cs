using Erlang;
using static Erlang.Tests.BeamReceiveMarkerTypeAccess;

namespace Erlang.Tests;

internal static class BeamDelayProtocolChecks
{
    internal static Task Admission()
    {
        Require(BeamModule.Prepare(BeamDelayProtocolBytes.Image()).Supports(BeamOpcodeEdgeNames.Run, 1));
        Require(BeamModule.Prepare(BeamDelayProtocolBytes.Image(xTimeout: true)).Supports(BeamOpcodeEdgeNames.Run, 1));
        Require(BeamModule.Prepare(BeamDelayProtocolBytes.Unprotected()).Supports(BeamOpcodeEdgeNames.Run, 1));
        Reject(BeamDelayProtocolBytes.Image(invalidHandler: true));
        Reject(BeamDelayProtocolBytes.Image(handlerTimeout: true));
        Reject(BeamDelayProtocolBytes.Image(markerTimeout: true));
        Reject(BeamDelayProtocolBytes.Image(uninitializedTimeout: true));
        Reject(BeamDelayProtocolBytes.Image(wrongTarget: true));
        Reject(BeamDelayProtocolBytes.Image(missingTimeout: true));
        Reject(BeamDelayProtocolBytes.Image(readExpiredX: true));
        Reject(BeamDelayProtocolBytes.Unprotected(uninitializedTimeout: true));
        Reject(BeamDelayProtocolBytes.Unprotected(readExpiredX: true));

        return Task.CompletedTask;
    }

    private static void Reject(byte[] image)
    {
        try
        {
            if (!BeamModule.Prepare(image).Supports(BeamOpcodeEdgeNames.Run, 1))
                return;
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return;
        }
        throw new InvalidDataException(BeamAwaitEntryHostNames.Failure);
    }
}
