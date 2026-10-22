using System.Reflection;
using Erlang;
using static Erlang.Tests.BeamReceiveMarkerTypeAccess;
using static Erlang.Tests.BeamReceiveMarkerTypeHostNames;

namespace Erlang.Tests;

internal static class BeamProtectedReceiveProtocolChecks
{
    internal static Task Admission()
    {
        Require(BeamModule.Prepare(BeamProtectedReceiveProtocolBytes.Timed()).Supports(BeamOpcodeEdgeNames.Run, 1));
        Reject(BeamProtectedReceiveProtocolBytes.Timed(invalidHandler: true));
        Reject(BeamProtectedReceiveProtocolBytes.Timed(handlerTimeout: true));
        Reject(BeamProtectedReceiveProtocolBytes.Timed(scratchTimeout: true));
        Reject(BeamProtectedReceiveProtocolBytes.Timed(handlerMatcher: true));
        Reject(BeamProtectedReceiveProtocolBytes.Timed(markerTimeout: true));

        return Task.CompletedTask;
    }

    internal static Task CloneAndTrim()
    {
        object state = New(StateType);
        Call(state, AllocateMethod, 4);
        Call(
            state,
            WriteYMethod,
            0,
            Nil.Value
        );
        Call(
            state,
            WriteYMethod,
            1,
            Nil.Value
        );
        object function = New(
            FunctionType,
            BeamOpcodeEdgeNames.Module,
            BeamOpcodeEdgeNames.Run,
            0,
            1,
            2,
            0,
            8
        );
        object checkpoint = New(
            CheckpointType,
            function,
            3,
            0,
            0,
            0,
            1
        );
        object outer = Call(
            state,
            InstallHandlerMethod,
            3,
            checkpoint
        );
        object inner = Call(
            state,
            InstallHandlerMethod,
            2,
            checkpoint
        );
        object clone = Call(state, CloneMethod);
        Call(
            clone,
            TrimMethod,
            1,
            3
        );
        Require(ReferenceEquals(Call(clone, BeamProtectedReceiveProtocolNames.ReadHandler, 1), inner));
        Require(ReferenceEquals(Call(clone, BeamProtectedReceiveProtocolNames.ReadHandler, 2), outer));
        object frame = inner.GetType().GetProperty(BeamProtectedReceiveProtocolNames.Frame,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(inner)!;
        object foreign = New(BeamProtectedReceiveProtocolNames.HandlerType, frame, checkpoint);
        Refuses(() => Call(
            clone,
            BeamProtectedReceiveProtocolNames.RemoveHandler,
            1,
            foreign
        ));
        Refuses(() => Call(
            clone,
            TrimMethod,
            2,
            1
        ));
        Require(ReferenceEquals(Call(clone, BeamProtectedReceiveProtocolNames.ReadHandler, 1), inner));
        Call(
            clone,
            BeamProtectedReceiveProtocolNames.RemoveHandler,
            1,
            inner
        );
        Require(Call(clone, ReadYMethod, 1).Equals(Nil.Value));
        Require(ReferenceEquals(Call(state, BeamProtectedReceiveProtocolNames.ReadHandler, 2), inner));
        Require(ReferenceEquals(Call(state, BeamProtectedReceiveProtocolNames.ReadHandler, 3), outer));
        Call(
            clone,
            BeamProtectedReceiveProtocolNames.RemoveHandler,
            2,
            outer
        );
        Call(clone, DeallocateMethod, 3);
        Refuses(() => Call(
            state,
            TrimMethod,
            3,
            1
        ));
        Require(ReferenceEquals(Call(state, BeamProtectedReceiveProtocolNames.ReadHandler, 2), inner));

        return Task.CompletedTask;
    }

    private static void Reject(byte[] bytes)
    {
        try
        {
            if (!BeamModule.Prepare(bytes).Supports(BeamOpcodeEdgeNames.Run, 1))
                return;
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return;
        }
        throw new InvalidDataException(BeamProtectedReceiveProtocolNames.Failure);
    }
}
