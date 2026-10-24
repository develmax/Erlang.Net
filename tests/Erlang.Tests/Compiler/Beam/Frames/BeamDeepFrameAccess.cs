using System.Collections;
using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static class BeamDeepFrameAccess
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static object Image(PreparedBeamImage image)
    {
        object identity = typeof(PreparedBeamImage).GetProperty(BeamDeepFrameNames.ImageIdentity, Members)!.GetValue(image)!;

        return identity.GetType().GetProperty(BeamDeepFrameNames.Image, Members)!.GetValue(identity)!;
    }

    internal static int Active(ProcessContext context, object image)
    {
        object gate = typeof(ProcessContext).GetField(BeamDeepFrameNames.CodeGate, Members)!.GetValue(context)!;
        lock (gate)
        {
            var active = (IDictionary)typeof(ProcessContext).GetField(BeamDeepFrameNames.ActiveCode, Members)!.GetValue(context)!;

            return active.Contains(image) ? (int)active[image]! : 0;
        }
    }

    internal static bool Waiting(ProcessContext context) => (bool)typeof(ProcessContext).GetField(BeamDeepFrameNames.Waiting, Members)!.GetValue(context)!;

    internal static int SourceFrames(ProcessContext context, string function, int logicalDepth)
    {
        object? frame = typeof(ProcessContext).GetField(BeamDeepFrameNames.VisibleFrame, Members)!.GetValue(context);
        int observed = 0;
        int visited = 0;
        int maximum = checked((logicalDepth + 128) * 4);
        while (frame is not null)
        {
            if (++visited > maximum)
                throw new InvalidDataException(BeamDeepFrameNames.SourceInspectionBoundExceeded);
            System.Type type = frame.GetType();
            if ((string?)type.GetProperty(BeamDeepFrameNames.Module, Members)!.GetValue(frame) == BeamDeepFrameNames.Subject
                && (string?)type.GetProperty(BeamDeepFrameNames.Function, Members)!.GetValue(frame) == function)
                observed++;
            frame = type.GetProperty(BeamDeepFrameNames.Parent, Members)!.GetValue(frame);
        }

        return observed;
    }

    internal static Task Unwound(ProcessContext context)
    {
        object completion = typeof(ProcessContext).GetProperty(BeamDeepFrameNames.Unwound, Members)!.GetValue(context)!;

        return (Task)completion.GetType().GetProperty(BeamDeepFrameNames.Task)!.GetValue(completion)!;
    }

    internal static Task<Term> Completion(ProcessContext context)
    {
        object completion = typeof(ProcessContext).GetProperty(BeamDeepFrameNames.Done, Members)!.GetValue(context)!;

        return (Task<Term>)completion.GetType().GetProperty(BeamDeepFrameNames.Task)!.GetValue(completion)!;
    }
}
