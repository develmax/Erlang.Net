using System.Reflection;
using Erlang.Compiler;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.FunctionIdentityValue255Names;

namespace Erlang.Tests;

internal static class FunctionIdentityValue255Checks
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }

    private static LocalFunctionIdentity Identity(BitString unique)
        => new(
            Term.A(IdentityModule),
            null,
            0,
            0,
            unique
        );

    private static object? StorageOf(object heap, Term value)
        => heap.GetType().GetMethod(Storage, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(null, [value]);

    private static void Verify(LocalFunctionIdentity identity, int bits, byte[] bytes)
    {
        BitString actual = identity.Unique;
        Require(actual.BitLength == bits && actual.ToArray().SequenceEqual(bytes));
        Require(Property(actual, Owned) is null && Property(actual, OwnedHeap) is null
            && Property(Property(actual, Backing), Native) is null);
    }

    private static void RequireStale(BitString source)
    {
        try
        {
            _ = Identity(source);
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        throw new InvalidOperationException(Failure);
    }

    internal static async Task Run(bool owned)
    {
        ManagedAndWritable();
        Fingerprint274Checks.ManagedAndWritable();
        await Physical(owned);
        await LiteralInputs(owned);
    }

    private static void ManagedAndWritable()
    {
        var stable = new BitString(Enumerable.Range(0, OrdinaryBytes).Select(value => (byte)value).ToArray());
        var identity = Identity(stable);
        Require(ReferenceEquals(identity.Unique, stable));
        LocalFunctionIdentity unchanged = identity with { };
        Require(identity == unchanged && identity.GetHashCode() == unchanged.GetHashCode());
        var (module, name, index, oldUnique, unique, legacyIndex) = unchanged;
        Require(module.Equals(Term.A(IdentityModule)) && name is null && index == 0
            && oldUnique == 0 && ReferenceEquals(unique, stable) && legacyIndex is null);
        // Preserve the historical malformed host metadata shape, including
        // null. The actual consumer continues to own its error ordering.
        Require(Identity(null!).Unique is null && (identity with { Unique = null! }).Unique is null);
        BitString unaligned = stable.SliceBits(SliceOffset, OrdinaryBytes * BitsPerByte - SliceTrim);
        Verify(identity with { Unique = unaligned }, unaligned.BitLength, unaligned.ToArray());
        MethodInfo initialize = typeof(BitString).GetMethod(InitializeWritable, BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodInfo append = typeof(BitString).GetMethod(Append, BindingFlags.Static | BindingFlags.NonPublic)!;
        var empty = (BitString)initialize.Invoke(null, [Capacity])!;
        byte[] expected = Enumerable.Range(0, InlineBytes).Select(value => (byte)value).ToArray();
        var writable = (BitString)append.Invoke(null, [empty, new BitString(expected)])!;
        LocalFunctionIdentity copied = identity with { Unique = writable };
        _ = append.Invoke(null, [writable, new BitString(new byte[InlineBytes * 2])]);
        Verify(copied, InlineBytes * BitsPerByte, expected);
    }

    private static async Task Physical(bool owned)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var snapshots = new List<(LocalFunctionIdentity Identity, int Bits, byte[] Bytes)>();
        var stale = new List<BitString>();
        var fingerprints = new List<(ModuleCodeDescriptor Descriptor, byte[] Bytes)>();
        await actor.InvokeAsync(async context =>
        {
            // Establish actual ordinary guest admission through genuine Source
            // expression execution, as the permanent lazy-register control does.
            _ = await Execution.EvaluateAsync(new Expr.Literal(Nil.Value), context);
            object heap = Property(context, Heap);
            using (var frame = (IDisposable)Call(heap, Enter)!)
            {
                foreach (int count in new[] { 0, InlineBytes, OrdinaryBytes })
                {
                    byte[] bytes = Enumerable.Range(0, count).Select(value => (byte)value).ToArray();
                    var physical = (BitString)Call(
                        frame,
                        Adopt,
                        (int)Call(frame, Reserve)!,
                        new BitString(bytes)
                    )!;
                    Require(StorageOf(heap, physical) is not null);
                    LocalFunctionIdentity identity = Identity(physical);
                    snapshots.Add((identity, physical.BitLength, bytes));
                    if (count == Fingerprint274Names.FingerprintBytes274)
                        fingerprints.Add((Fingerprint274Checks.Create(physical), bytes));
                    snapshots.Add((identity with { Unique = physical }, physical.BitLength, bytes));
                    stale.Add(physical);
                    if (count == OrdinaryBytes)
                    {
                        // A detached view still carries a real process origin;
                        // the copied UID must contain neither origin nor backing.
                        BitString slice = physical.SliceBits(SliceOffset, count * BitsPerByte - SliceTrim);
                        snapshots.Add((Identity(slice), slice.BitLength, slice.ToArray()));
                    }
                }
                MethodInfo initialize = typeof(BitString).GetMethod(InitializeWritable, BindingFlags.Static | BindingFlags.NonPublic)!;
                MethodInfo append = typeof(BitString).GetMethod(Append, BindingFlags.Static | BindingFlags.NonPublic)!;
                var initialized = (BitString)initialize.Invoke(null, [Capacity])!;
                byte[] writableBytes = Enumerable.Range(0, OrdinaryBytes).Select(value => (byte)value).ToArray();
                var writable = (BitString)append.Invoke(null, [initialized, new BitString(writableBytes)])!;
                var stored = (BitString)Call(
                    frame,
                    Adopt,
                    (int)Call(frame, Reserve)!,
                    writable
                )!;
                object native = Property(Property(stored, Backing), Native);
                Require((bool)Property(native, IsWritable));
                snapshots.Add((Identity(stored), writableBytes.Length * BitsPerByte, writableBytes));
                Require((bool)Property(native, IsWritable));
                stale.Add(stored);
                var fingerprintWriter = Fingerprint274Checks.NativeWritable(frame);
                fingerprints.Add((fingerprintWriter.Descriptor, fingerprintWriter.Bytes));
                stale.Add(fingerprintWriter.Source);
                int providerCalls = 0;
                var lazy = new FunctionTerm(
                    () =>
                {
                    providerCalls++;

                    return snapshots[0].Identity;
                },
                    0,
                    Array.Empty<Term>(),
                    (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                );
                _ = Call(
                    frame,
                    Adopt,
                    (int)Call(frame, Reserve)!,
                    lazy
                );
                Require(providerCalls == 0);
            }

            return true;
        });
        await actor.InvokeAsync(context =>
        {
            object heap = Property(context, Heap);
            Call(
                heap,
                Collect,
                true,
                Major
            );
            foreach (BitString source in stale)
                RequireStale(source);
            foreach (var snapshot in snapshots)
                Verify(snapshot.Identity, snapshot.Bits, snapshot.Bytes);
            foreach (var fingerprint in fingerprints)
                Fingerprint274Checks.Verify(fingerprint.Descriptor, fingerprint.Bytes);

            return Task.FromResult(true);
        });
        // A source VM can close without participating in metadata lifetime.
        await runtime.DisposeAsync();
        foreach (var snapshot in snapshots)
            Verify(snapshot.Identity, snapshot.Bits, snapshot.Bytes);
        foreach (var fingerprint in fingerprints)
            Fingerprint274Checks.Verify(fingerprint.Descriptor, fingerprint.Bytes);
    }

    internal static async Task ResourceInputs(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor, BitString input)
    {
        var snapshots = new List<(LocalFunctionIdentity Identity, int Bits, byte[] Bytes)>();
        BitString? physical = null, sub = null;
        await actor.InvokeAsync(async context =>
        {
            _ = await Execution.EvaluateAsync(new Expr.Literal(Nil.Value), context);
            object heap = Property(context, Heap);
            using (var frame = (IDisposable)Call(heap, Enter)!)
            {
                physical = (BitString)Call(
                    frame,
                    Adopt,
                    (int)Call(frame, Reserve)!,
                    input
                )!;
                Require(StorageOf(heap, physical) is not null && Property(physical, Owned) is not null);
                snapshots.Add((Identity(physical), physical.BitLength, physical.ToArray()));
                BitString slice = physical.SliceBits(SliceOffset, physical.BitLength - SliceTrim);
                snapshots.Add((Identity(slice), slice.BitLength, slice.ToArray()));
                sub = (BitString)Call(
                    frame,
                    Adopt,
                    (int)Call(frame, Reserve)!,
                    slice
                )!;
                Require(StorageOf(heap, sub) is not null && Property(sub, Owned) is not null);
                snapshots.Add((Identity(sub), sub.BitLength, sub.ToArray()));
                snapshots.Add((Identity(physical) with { Unique = sub }, sub.BitLength, sub.ToArray()));
            }

            return true;
        });
        await actor.InvokeAsync(context =>
        {
            object heap = Property(context, Heap);
            Call(
                heap,
                Collect,
                true,
                Major
            );
            RequireStale(physical!);
            RequireStale(sub!);
            foreach (var snapshot in snapshots)
                Verify(snapshot.Identity, snapshot.Bits, snapshot.Bytes);

            return Task.FromResult(true);
        });
    }

    private static async Task LiteralInputs(bool owned)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
        foreach (int count in new[] { InlineBytes, OrdinaryBytes })
        {
            byte[] expected = Enumerable.Range(0, count).Select(value => (byte)value).ToArray();
            byte[] image = BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(new BitString(expected)));
            await using PreparedBeamImage prepared = runtime.Modules.PrepareBeam(image);
            var literals = (IReadOnlyList<Term>)Property(Property(Property(prepared, Code), Structure), Literals);
            var literal = (BitString)literals.Single();
            Require(Property(literal, count == InlineBytes ? OwnedHeap : Owned) is not null);
            LocalFunctionIdentity snapshot = Identity(literal);
            Verify(snapshot, count * BitsPerByte, expected);
            ModuleCodeDescriptor? fingerprint = count == Fingerprint274Names.FingerprintBytes274
                ? Fingerprint274Checks.Create(literal) : null;
            await prepared.DisposeAsync();
            Verify(snapshot, count * BitsPerByte, expected);
            RequireStale(literal);
            if (fingerprint is not null)
            {
                Fingerprint274Checks.Verify(fingerprint, expected);
                Fingerprint274Checks.RequireStale(literal);
            }
        }
    }
}
