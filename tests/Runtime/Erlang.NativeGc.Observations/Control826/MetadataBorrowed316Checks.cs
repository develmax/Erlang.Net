using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Erlang.Compiler;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;

namespace Erlang.Tests;

// These public metadata containers borrow exact caller Terms. This control
// deliberately keeps genuine source custody until all live reads are complete.
internal static class MetadataBorrowed316Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    internal static async Task Run()
    {
        foreach (bool owned in new[] { false, true })
            await One(owned);
    }

    private static async Task One(bool owned)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var provider = Provider(runtime);
        int destroyed = 0;
        Destructor destructor = (_, _) => Interlocked.Increment(ref destroyed);
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
        bool released = false;
        IDisposable? atomicsHost = null;
        Task? atomicsRetired = null, image = null;
        ModuleCodeDescriptor? descriptor = null;
        NativeRecordDefinition? definition = null;
        Term? borrowedGraph = null, magic = null;
        BitString? native = null, ordinary = null, writer = null, slice = null;
        MapTerm? map = null;
        BitString? smallSlice = null;
        byte[]? smallSliceBytes = null;
        object? sourceHeap = null, mapRoot = null;
        var failures = new List<Exception>();
        try
        {
            await actor.InvokeAsync(async context =>
            {
                _ = await Execution.EvaluateAsync(new Expr.Literal(Nil.Value), context);
                for (int index = 0; index < PayloadBytes; index++)
                    Marshal.WriteByte((nint)Property(payload, MetadataBorrowed316Names.Pointer), index, MetadataBorrowed316Names.PayloadValue);
                object binding = Activator.CreateInstance(
                    typeof(ProcessRuntime).Assembly.GetType(MetadataBorrowed316Names.AtomicsProducer, true)!,
                    Members,
                    null,
                    [runtime, runtime.Modules],
                    null
                )!;
                Term hostMagic = await ((ErlangFunction)Property(binding, MetadataBorrowed316Names.Body))(context, [Term.I(1), Term.I(0)]);
                atomicsHost = (IDisposable)Property(hostMagic, MetadataBorrowed316Names.AtomicsHost);
                object atomicsRoot = DirectAtomics(hostMagic, runtime);
                atomicsRetired = (Task)Property(Property(atomicsRoot, MetadataBorrowed316Names.Array), MetadataBorrowed316Names.AtomicsRetired);
                using var hostScope = new NativeTermHostScope524(runtime);
                object heap = Property(context, MetadataBorrowed316Names.Heap);
                sourceHeap = heap;
                using (var frame = (IDisposable)Call(heap, MetadataBorrowed316Names.Enter)!)
                {
                    MethodInfo initialize = RequiredBitStringMethod(MetadataBorrowed316Names.InitializeWritable, [typeof(int)]);
                    MethodInfo append = RequiredBitStringMethod(MetadataBorrowed316Names.Append, [typeof(BitString), typeof(BitString)]);
                    var empty = (BitString)initialize.Invoke(null, [MetadataBorrowed316Names.WritableCapacity])!;
                    byte[] writerBytes = Enumerable.Repeat(MetadataBorrowed316Names.PayloadValue, MetadataBorrowed316Names.WritableBytes).ToArray();
                    var writable = (BitString)append.Invoke(null, [empty, new BitString(writerBytes)])!;
                    writable = (BitString)Call(
                        frame,
                        MetadataBorrowed316Names.Adopt,
                        (int)Call(frame, MetadataBorrowed316Names.Reserve)!,
                        writable
                    )!;
                    Term input = Term.Tuple(
                        Binary(runtime, payload),
                        new BitString(new byte[MetadataBorrowed316Names.OrdinaryBytes]),
                        writable,
                        hostMagic,
                        new MapTerm([new KeyValuePair<Term, Term>(Term.A(MetadataBorrowed316Names.FieldName), Term.Tuple(hostMagic, Term.I(1)))])
                    );
                    int slot = (int)Call(frame, MetadataBorrowed316Names.Reserve)!;
                    Call(
                        frame,
                        MetadataBorrowed316Names.Adopt,
                        slot,
                        input
                    );
                    var actual = (TupleTerm)Call(frame, MetadataBorrowed316Names.Read, slot)!;
                    borrowedGraph = actual;
                    native = (BitString)actual.Items[MetadataBorrowed316Names.TupleNative];
                    ordinary = (BitString)actual.Items[MetadataBorrowed316Names.TupleOrdinary];
                    writer = (BitString)actual.Items[MetadataBorrowed316Names.TupleWriter];
                    magic = actual.Items[MetadataBorrowed316Names.TupleMagic];
                    map = (MapTerm)actual.Items[MetadataBorrowed316Names.TupleMap];
                    // Small slices are intentionally detached values. Keep that
                    // positive control; the stale control below must be real.
                    smallSlice = ordinary.SliceBits(MetadataBorrowed316Names.SliceOffset, MetadataBorrowed316Names.SmallSliceBits);
                    smallSliceBytes = smallSlice.ToArray();
                    Require(Property(smallSlice, MetadataBorrowed316Names.OwnedBinaryStorage) is null
                        && Property(smallSlice, MetadataBorrowed316Names.SliceOrigin) is null);
                    BitString unalignedInput = ordinary.SliceBits(MetadataBorrowed316Names.SliceOffset, MetadataBorrowed316Names.SliceBits);
                    byte[] unalignedBytes = unalignedInput.ToArray();
                    slice = (BitString)Call(
                        frame,
                        MetadataBorrowed316Names.Adopt,
                        (int)Call(frame, MetadataBorrowed316Names.Reserve)!,
                        unalignedInput
                    )!;
                    object sliceStorage = Property(slice, MetadataBorrowed316Names.OwnedBinaryStorage);
                    Require(sliceStorage is not null && ReferenceEquals(Property(sliceStorage, MetadataBorrowed316Names.StorageOwner), heap)
                        && slice.BitLength == MetadataBorrowed316Names.SliceBits && !slice.IsBinary
                        && slice.ToArray().SequenceEqual(unalignedBytes));
                    object mapPayload = Property(map, MetadataBorrowed316Names.MapPayload);
                    Require(mapPayload is not null);
                    mapRoot = Property(mapPayload!, MetadataBorrowed316Names.MapRoot);
                    Require(ReferenceEquals(Property(mapRoot, MetadataBorrowed316Names.StorageOwner), heap));
                    _ = Call(heap, MetadataBorrowed316Names.ReadMapHeader, mapRoot);
                    Require(map.Entries[0].Key.Equals(Term.A(MetadataBorrowed316Names.FieldName)));
                    var baseline = Counts(payload);
                    definition = new NativeRecordDefinition(
                        MetadataBorrowed316Names.RecordName,
                        false,
                        [new KeyValuePair<Atom, Term?>(Term.A(MetadataBorrowed316Names.FieldName), actual)],
                        [new NativeRecordFieldType(Term.A(MetadataBorrowed316Names.FieldName), actual, 0)]
                    );
                    descriptor = new ModuleCodeDescriptor(
                        [],
                        [],
                        new BitString(new byte[16]),
                        actual,
                        actual,
                        nativeRecords: [definition],
                        attributePolicy: ModuleAttributePolicy.PreserveDecoded
                    );
                    runtime.Modules.PublishModuleCode(MetadataBorrowed316Names.MetadataModule, descriptor);
                    Require(Counts(payload) == baseline
                        && (bool)Property(
                            Property(Property(writer, MetadataBorrowed316Names.Backing), MetadataBorrowed316Names.Native),
                            MetadataBorrowed316Names.IsWritable
                        ));
                    RequireExact(descriptor, definition, actual);
                    Require(ReferenceEquals(runtime.Modules.ModuleInformation(MetadataBorrowed316Names.MetadataModule, MetadataBorrowed316Names.CompileInfo), actual)
                        && ReferenceEquals(runtime.Modules.ModuleInformation(MetadataBorrowed316Names.MetadataModule, MetadataBorrowed316Names.Attributes), actual));
                    Require(native.ToArray().All(value => value == MetadataBorrowed316Names.PayloadValue)
                        && ordinary.ToArray().Length == MetadataBorrowed316Names.OrdinaryBytes
                        && writer.ToArray().SequenceEqual(writerBytes)
                        && slice.BitLength == MetadataBorrowed316Names.SliceBits
                        && ReferenceEquals(DirectAtomics(magic, runtime), atomicsRoot)
                        && ReferenceEquals(DirectAtomics(((TupleTerm)map.Entries.Single().Value).Items[0], runtime), atomicsRoot));
                    object nativeBacking = Property(Property(native, MetadataBorrowed316Names.Backing), MetadataBorrowed316Names.Native);
                    Require(nativeBacking is not null);
                    Call(payload, MetadataBorrowed316Names.ReleaseNative);
                    released = true;
                    atomicsHost.Dispose();
                    atomicsHost = null;
                    image = (Task)Call(provider.Bindings, MetadataBorrowed316Names.RetireImage)!;
                    Require(destroyed == 0 && !image.IsCompleted && !atomicsRetired.IsCompleted);
                    Require(native.ToArray().Length == PayloadBytes && ReferenceEquals(DirectAtomics(magic, runtime), atomicsRoot));
                }
                // The metadata objects and registered module deliberately remain
                // alive; they must not become an implicit physical/native root.
                Call(heap, MetadataBorrowed316Names.Collect);

                return Nil.Value;
            });
            await Task.WhenAll((Task)Property(payload, MetadataBorrowed316Names.Retired), atomicsRetired!, image!).WaitAsync(Deadline);
            Require(destroyed == 1 && provider.Owner.Disposed);
            RequireExact(descriptor!, definition!, borrowedGraph!);
            RequireDisposed(() => _ = native!.ToArray());
            RequireDisposed(() => _ = ordinary!.ToArray());
            RequireDisposed(() => _ = writer!.ToArray());
            RequireDisposed(() => _ = slice!.ToArray());
            Require(smallSlice!.BitLength == MetadataBorrowed316Names.SmallSliceBits
                && smallSlice.ToArray().SequenceEqual(smallSliceBytes!));
            RequireDisposed(() => _ = DirectAtomics(magic!, runtime));
            RequireDisposed(() => _ = Call(sourceHeap!, MetadataBorrowed316Names.ReadMapHeader, mapRoot));
            RequireDisposed(() => _ = map!.Entries[0]);
            RequireDisposed(() => _ = map!.Entries.Count);
            await runtime.DisposeAsync();
            RequireDisposed(() => _ = native!.ToArray());
            RequireDisposed(() => _ = DirectAtomics(magic!, runtime));
            // Pure managed metadata remains ordinary immutable value data.
            var managed = Term.Tuple(Term.A(MetadataBorrowed316Names.FieldName), new BitString(new byte[MetadataBorrowed316Names.OrdinaryBytes]));
            var portable = new ModuleCodeDescriptor(
                [],
                [],
                new BitString(new byte[16]),
                managed,
                managed
            );
            Require(ReferenceEquals(portable.Compilation, managed) && ReferenceEquals(portable.Attributes, managed)
                && ((BitString)((TupleTerm)portable.Compilation).Items[1]).ToArray().Length == MetadataBorrowed316Names.OrdinaryBytes);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                atomicsHost?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                if (!released) Call(payload, MetadataBorrowed316Names.ReleaseNative);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                await runtime.DisposeAsync();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                image ??= (Task)Call(provider.Bindings, MetadataBorrowed316Names.RetireImage)!;
                await Task.WhenAll((Task)Property(payload, MetadataBorrowed316Names.Retired), image).WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                if (atomicsRetired is not null) await atomicsRetired.WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            GC.KeepAlive(destructor);
            GC.KeepAlive(descriptor);
            GC.KeepAlive(definition);
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    private static void RequireExact(ModuleCodeDescriptor descriptor, NativeRecordDefinition definition, Term value)
        => Require(ReferenceEquals(descriptor.Compilation, value) && ReferenceEquals(descriptor.Attributes, value)
            && ReferenceEquals(descriptor.NativeRecords.Single(), definition)
            && ReferenceEquals(definition.Fields.Single().Value, value)
            && ReferenceEquals(definition.FieldTypes.Single().Syntax, value));

    private static object DirectAtomics(Term value, ProcessRuntime runtime)
    {
        try
        {
            return typeof(ProcessRuntime).Assembly.GetType(MetadataBorrowed316Names.AtomicsRoots, true)!
                .GetMethod(MetadataBorrowed316Names.AtomicsDirect, BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, [value, runtime])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static (long Native, long Roots) Counts(object payload)
    {
        object Field(string name) => payload.GetType().GetField(name, Members)!.GetValue(payload)!;
        lock (Field(MetadataBorrowed316Names.PayloadGate))
            return ((long)Field(MetadataBorrowed316Names.NativeCount), (long)Field(MetadataBorrowed316Names.RootCount));
    }

    private static MethodInfo RequiredBitStringMethod(string name, Type[] parameters)
        => typeof(BitString).GetMethod(
            name,
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            parameters,
            null
        )
            ?? throw new MissingMethodException(MetadataBorrowed316Names.MissingBitStringMethod + name);

    private static void RequireDisposed(Action read)
    {
        try
        {
            read();
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        throw new InvalidOperationException(MetadataBorrowed316Names.Failure);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(MetadataBorrowed316Names.Failure);
    }
}
