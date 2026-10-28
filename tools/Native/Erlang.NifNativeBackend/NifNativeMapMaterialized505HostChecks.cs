using static Erlang.NifNativeBackend.NifNativeMapMaterialized505HostNames;

namespace Erlang.NifNativeBackend;

internal static class NifNativeMapMaterialized505HostChecks
{
    internal static void FlatPhysicalLayoutAndEquality(ProcessContext context)
    {
        var physical = new[] { Pair(ChangedKeyByte, 100), Pair(1, 101) };
        var native = MaterializeFlat(context.Runtime, physical);
        var same = MaterializeFlat(context.Runtime, physical);
        var fresh = new MapTerm(physical.Select(pair =>
            new KeyValuePair<Term, Term>(pair.Key.StaticTerm!, pair.Value.StaticTerm!)));
        Require(!native.Equals(fresh) && !fresh.Equals(native));
        Require(native.Equals(same));
        var entries = MapIteration.Entries(native, context.Runtime);
        Require(entries.Count == 2 && entries[0].Value.Equals(Term.I(100))
            && entries[1].Value.Equals(Term.I(101)));
        var duplicate = MaterializeFlat(context.Runtime, [Pair(1, 100), Pair(1, 101)]);
        Require(duplicate.Entries.Count == 2 && !duplicate.Equals(new MapTerm([
            new KeyValuePair<Term, Term>(Binary(1), Term.I(101))
        ])));
        var duplicateEntries = MapIteration.Entries(duplicate, context.Runtime);
        Require(duplicateEntries.Count == 2 && duplicateEntries[0].Value.Equals(Term.I(100))
            && duplicateEntries[1].Value.Equals(Term.I(101)));
        using var hash = context.Runtime.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append);
        // Equality and internal hashing traverse the same saved physical layout,
        // including when the native map itself is used as a nested map key.
        Require(hash.Hash(native) == hash.Hash(same));
    }

    internal static void NativeBufferReleaseAndSavedIndex(ProcessContext context)
    {
        long buffers = NifNativeBinaryStorage.LiveBufferCount;
        var map = MaterializeMutated(context.Runtime);
        Require(NifNativeBinaryStorage.LiveBufferCount == buffers);
        Require(map.Entries.Count == LargeEntries && !map.TryGet(Binary(ChangedKeyByte), out _));
        var entries = MapIteration.Entries(map, context.Runtime);
        Require(entries.Count == LargeEntries && entries.Count(pair => pair.Key.Equals(Binary(ChangedKeyByte))) == 1);
        using var index = context.Runtime.AcquireHamtIndex429(map);
        using var hashes = context.Runtime.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append);
        Require(index.Index.RetainedOwnership is not null);
        Require(!HamtIndexLookup429.TryGet(
            index.Index,
            Binary(ChangedKeyByte),
            hashes.Hash(Binary(ChangedKeyByte)),
            out _
        ));
        _ = hashes.Hash(map);
        Require(!map.Equals(new MapTerm(map.Entries)));
    }

    internal static void DeepNativeMapEquality(ProcessContext context)
    {
        NifNativeTermValue left = new(Term.I(0)), right = new(Term.I(0));
        for (int depth = 0; depth < DeepMaps; depth++)
        {
            left = new(new NifNativeMapNode504([new(new(Term.I(0)), left)], context.Runtime));
            right = new(new NifNativeMapNode504([new(new(Term.I(0)), right)], context.Runtime));
        }
        Term a = NifNativeTermGraph.Resolve(left), b = NifNativeTermGraph.Resolve(right);
        Require(a.Equals(b));
    }

    internal static async Task CrossRuntimeAndClosedOwner()
    {
        await using var original = new ProcessRuntime();
        await using var other = new ProcessRuntime();
        var map = MaterializeMutated(original);
        Require(!map.TryGet(Binary(ChangedKeyByte), out _));
        using (var index = other.AcquireHamtIndex429(map))
        using (var hashes = other.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append))
            Require(HamtIndexLookup429.TryGet(
                index.Index,
                Binary(ChangedKeyByte),
                hashes.Hash(Binary(ChangedKeyByte)),
                out _
            ));
        await NifNativeBinaryHostChecks.Run(context => CrossRuntimeNativeGet(context, map));
        await original.DisposeAsync();
        await NifNativeBinaryHostChecks.Run(context => CrossRuntimeNativeGet(context, map));
        try
        {
            _ = map.TryGet(Binary(ChangedKeyByte), out _);
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        throw new InvalidOperationException(Failure);
    }

    private static unsafe void CrossRuntimeNativeGet(ProcessContext context, MapTerm map)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var environment = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(Module),
            0,
            state
        );
        using var table = new NifNativeWindowsCallbacks();
        var get = (delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint*, int>)((nint*)table.Pointer)[GetMapValueSlot];
        nuint handle = environment.Add(map), key = environment.Add(Binary(ChangedKeyByte));
        foreach (nint hint in new[] { environment.Pointer, (nint)0 })
        {
            nuint output = 0;
            Require(get(
                hint,
                handle,
                key,
                &output
            ) == 1);
            state.ThrowPending();
            Require(environment.Get(output).Equals(Term.I(100)));
        }
    }

    private static unsafe MapTerm MaterializeMutated(ProcessRuntime runtime)
    {
        var storage = NifNativeBinaryStorage.TryAllocate(BinaryBytes, false)
            ?? throw new OutOfMemoryException();
        storage.RetainEnvironment();
        try
        {
            new Span<byte>(storage.Pointer, BinaryBytes).Clear();
            var entries = Enumerable.Range(0, LargeEntries).Select(value => Pair(value, 100 + value)).ToArray();
            entries[0] = new(new(new NifNativeBinaryNode(storage, 0, BinaryBytes)), new(Term.I(100)));
            var pending = NifNativeMapGraph504.Create(entries, runtime);
            new Span<byte>(storage.Pointer, BinaryBytes).Fill(ChangedKeyByte);

            return (MapTerm)NifNativeTermGraph.Resolve(pending);
        }
        finally
        {
            storage.ReleaseEnvironment();
        }
    }

    private static MapTerm MaterializeFlat(ProcessRuntime runtime,
        KeyValuePair<NifNativeTermValue, NifNativeTermValue>[] entries)
        => (MapTerm)NifNativeTermGraph.Resolve(new(new NifNativeMapNode504(entries, runtime)));

    private static KeyValuePair<NifNativeTermValue, NifNativeTermValue> Pair(int keyByte, int value)
        => new(new(Binary(keyByte)), new(Term.I(value)));

    private static BitString Binary(int value) => new(Enumerable.Repeat((byte)value, BinaryBytes).ToArray());

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
