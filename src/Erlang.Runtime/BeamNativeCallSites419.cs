using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
namespace Erlang;

internal static class BeamNativeCallSites419
{
    private static readonly ConditionalWeakTable<BeamLinkedCode, ConcurrentDictionary<(int Instruction, bool Entry), object>> sites = new();

    internal static BeamNativeCallSite419 Capture(BeamLinkedCode code, int instruction, ErlangExecutionFrame frame)
    {
        var points = sites.GetValue(code, static _ => new());

        return new(frame, points.GetOrAdd((instruction, true), static _ => new()), points.GetOrAdd((instruction, false), static _ => new()));
    }
}
