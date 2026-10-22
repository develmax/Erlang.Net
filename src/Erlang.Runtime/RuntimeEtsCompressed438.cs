using System.Runtime.CompilerServices;
namespace Erlang;
// Only these bytes/key nodes and genuine owner sidecars survive publication.
internal sealed class RuntimeEtsCompressed438
{
    internal byte[] Encoded { get; }
    internal int DecodedBytes { get; }
    internal int NodeCount { get; }
    internal long KeyPosition { get; }
    internal int[] KeyOriginalIds { get; }
    internal RuntimeEtsStoredNode[] KeyNodes { get; }
    internal RuntimeExternalTermReservation[] Identities { get; }
    internal IRuntimeStoredFunctionRecipe[] Functions { get; }
    internal long ArrayPayloadBytes { get; }

    internal RuntimeEtsCompressed438(
        byte[] encoded,
        int decodedBytes,
        int nodeCount,
        long keyPosition,
        int[] keyOriginalIds,
        RuntimeEtsStoredNode[] keyNodes,
        RuntimeExternalTermReservation[] identities,
        IRuntimeStoredFunctionRecipe[] functions
    )
    {
        long bytes = checked(encoded.LongLength + (long)keyOriginalIds.Length * sizeof(int)
            + (long)keyNodes.Length * Unsafe.SizeOf<RuntimeEtsStoredNode>()
            + (long)identities.Length * IntPtr.Size + (long)functions.Length * IntPtr.Size);
        foreach (var node in keyNodes)
            bytes = checked(bytes + (node.Bytes?.LongLength ?? 0) + (node.Children?.LongLength ?? 0) * sizeof(int)
                + (node.Names?.LongLength ?? 0) * IntPtr.Size);
        Encoded = encoded;
        DecodedBytes = decodedBytes;
        NodeCount = nodeCount;
        KeyPosition = keyPosition;
        KeyOriginalIds = keyOriginalIds;
        KeyNodes = keyNodes;
        Identities = identities;
        Functions = functions;
        ArrayPayloadBytes = bytes;
    }

    internal void AddOwnedCensus(EtsOwnedCensus420 census)
    {
        if (!census.Add(this)) return;
        census.Array(Encoded);
        census.Array(KeyOriginalIds);
        census.Array(KeyNodes);
        census.Array(Identities);
        census.Array(Functions);
        foreach (var node in KeyNodes)
        {
            census.Array(node.Bytes);
            census.Array(node.Children);
            census.Array(node.Names);
            census.Add(node.Identity);
            if (node.Function is ExternalStoredFunctionRecipe or BeamStoredFunctionRecipe) census.Add(node.Function);
        }
        foreach (var identity in Identities) census.Add(identity);
        foreach (var function in Functions) if (function is ExternalStoredFunctionRecipe or BeamStoredFunctionRecipe) census.Add(function);
    }
}
