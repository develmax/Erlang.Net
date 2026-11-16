namespace Erlang;

internal sealed partial class ProcessTracing
{
    internal void CaptureNodeDistReferences(NodeDistReferenceSnapshot snapshot)
    {
        ProcessTracePattern[] selected;
        lock (gate) selected = patterns.Values.Concat(new[] { sendPattern, receivePattern }.OfType<ProcessTracePattern>()).Distinct().ToArray();
        foreach (var view in LiveTraceViews425()) view.CaptureNodeDistReferences(snapshot);
        foreach (var pattern in selected)
            if (pattern.Program is { } program)
                snapshot.MatchProgram(
                    program,
                    NodeDistReferenceSnapshot.Owner(NodeDistReferenceNames.MatchSpec,
                    new Integer(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(program)))
                );
    }
}
