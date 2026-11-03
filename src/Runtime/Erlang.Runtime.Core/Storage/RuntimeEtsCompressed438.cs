using System.Runtime.CompilerServices;
namespace Erlang;
// Only these bytes/key nodes and genuine owner sidecars survive publication.
internal sealed class RuntimeEtsCompressed438 : IDisposable
{
    private RuntimeExternalTermReservation[]? ownedIdentities;
    private readonly Exception?[] identityCleanupFailures;
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
        // Capture owns every input reservation until construction succeeds.
        // Key and encoded views may share one reservation; own it exactly once.
        var unique = new HashSet<RuntimeExternalTermReservation>(ReferenceEqualityComparer.Instance);
        foreach (RuntimeEtsStoredNode node in keyNodes)
            if (node.Identity is { } identity) unique.Add(identity);
        foreach (RuntimeExternalTermReservation identity in identities)
            unique.Add(identity);
        RuntimeExternalTermReservation[] selected = unique.ToArray();
        identityCleanupFailures = new Exception?[selected.Length];
        bytes = checked(bytes + (long)selected.Length * IntPtr.Size * 2);

        Encoded = encoded;
        DecodedBytes = decodedBytes;
        NodeCount = nodeCount;
        KeyPosition = keyPosition;
        KeyOriginalIds = keyOriginalIds;
        KeyNodes = keyNodes;
        Identities = identities;
        Functions = functions;
        ArrayPayloadBytes = bytes;
        ownedIdentities = selected;
    }

    public void Dispose()
    {
        RuntimeExternalTermReservation[]? retired = Interlocked.Exchange(ref ownedIdentities, null);
        if (retired is null) return;
        int failures = 0;
        foreach (RuntimeExternalTermReservation identity in retired)
        {
            try
            {
                identity.Dispose();
            }
            catch (Exception failure)
            {
                identityCleanupFailures[failures++] = failure;
            }
        }
        if (failures == 1)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(identityCleanupFailures[0]!).Throw();
        if (failures > 1)
            throw new AggregateException(identityCleanupFailures.Take(failures).Cast<Exception>());
    }

    internal void AddOwnedCensus(EtsOwnedCensus420 census)
    {
        if (!census.Add(this)) return;
        census.Array(Encoded);
        census.Array(KeyOriginalIds);
        census.Array(KeyNodes);
        census.Array(Identities);
        census.Array(Functions);
        census.Array(Volatile.Read(ref ownedIdentities));
        census.Array(identityCleanupFailures);
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
