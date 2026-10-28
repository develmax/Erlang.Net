using System.Reflection;
using Erlang.Differential;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static Erlang.Tests.ResourceBinary577TestNames;

namespace Erlang.Tests;

internal static class ResourceBinary577Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static Task HeaderReadersAndCorruption() => Run(async fixture =>
    {
        IDisposable roots = fixture.Frame();
        int slot = Slot(roots);
        long before = PhysicalWords(fixture.Heap);
        var owned = (BitString)Call(
            roots,
            Adopt,
            slot,
            fixture.View
        )!;
        Check(PhysicalWords(fixture.Heap) == before + 8 && owned.BitLength == Bytes * 8);
        Check(owned.ToArray().SequenceEqual(fixture.Data.Skip(1)));
        Check(Size(fixture, owned, false) == 8 && Size(fixture, owned, true) == 8);
        object storage = Get(owned, Storage), cells = Get(Get(storage, Slab), Cells);
        int offset = (int)Get(storage, Offset);
        lock (Get(fixture.Heap, Gate))
        {
            Check((nuint)Call(cells, ReadHeader, offset)! == 0x124);
            object?[] arguments = [owned, null];
            Check((bool)Call(fixture.Heap, Node, arguments)!);
            object node = arguments[1]!;
            object binRef = Get(node, NodeBlock), binCells = Get(Get(binRef, Slab), Cells);
            int binOffset = (int)Get(binRef, Offset);
            Check((nuint)Call(binCells, ReadHeader, binOffset)! == 0xA8);
            Check((nuint)Call(cells, ReadNumeric, offset + 4)! == ((nuint)(nint)Get(node, NodeAddress) | 2));
            nuint originalStart = Word(cells, offset + 2), originalEnd = Word(cells, offset + 3);
            nuint originalBase = Word(cells, offset + 1), originalOrigin = Word(cells, offset + 4);
            Check(originalStart == (nuint)((fixture.Address + 1) & 3) * 8);
            var restorationFailures = new List<Exception>();
            Action[] restorations =
            [
                () => Call(
                    cells,
                    WriteHeaders,
                    new nuint[] { 0x124 },
                    offset
                ),
                () => Write(cells, offset + 1, originalBase),
                () => Write(cells, offset + 2, originalStart),
                () => Write(cells, offset + 3, originalEnd),
                () => Write(cells, offset + 4, originalOrigin)
            ];
            try
            {
                Write(cells, offset + 2, originalStart + 8);
                Check(owned.BitLength == (Bytes - 1) * 8 && owned.ToArray().SequenceEqual(fixture.Data.Skip(2)));
                Write(cells, offset + 2, originalStart);
                Write(cells, offset + 4, 0);
                Expect<InvalidOperationException>(() => _ = owned.BitLength);
                Write(cells, offset + 4, originalOrigin);
                Write(cells, offset + 1, originalBase | 1);
                Expect<InvalidOperationException>(() => _ = owned.BitLength);
                Write(cells, offset + 1, originalBase);
                Write(cells, offset + 3, originalStart == 0 ? nuint.MaxValue : originalStart - 1);
                Expect<InvalidOperationException>(() => _ = owned.BitLength);
                Write(cells, offset + 3, originalEnd);
                Call(
                    cells,
                    WriteHeaders,
                    new nuint[] { 0 },
                    offset
                );
                Expect<InvalidOperationException>(() => _ = owned.BitLength);
            }
            catch (Exception error)
            {
                restorationFailures.Add(error);
            }
            finally
            {
                foreach (Action restore in restorations)
                    try
                    {
                        restore();
                    }
                    catch (Exception error)
                    {
                        restorationFailures.Add(error);
                    }
            }
            ThrowFailures(restorationFailures);
        }
        Check(PhysicalWords(fixture.Heap) == before + 8 && owned.ToArray().SequenceEqual(fixture.Data.Skip(1)));
        Call(roots, Clear, slot);
        Call(fixture.Heap, Collect);
        Check(PhysicalWords(fixture.Heap) == before);
        Expect<ObjectDisposedException>(() => _ = owned.BitLength);
        await Task.CompletedTask;
    });

    internal static Task SliceSharingAndGenerations() => Run(async fixture =>
    {
        IDisposable roots = fixture.Frame();
        int sourceSlot = Slot(roots), recipeSlot = Slot(roots), sliceSlot = Slot(roots), independentSlot = Slot(roots), pairSlot = Slot(roots);
        long baseline = PhysicalWords(fixture.Heap);
        var source = (BitString)Call(
            roots,
            Adopt,
            sourceSlot,
            fixture.View
        )!;
        nuint originalBinRef = Origin(source);
        BitString recipe = source.SliceBits(8, 65 * 8);
        Check(PhysicalWords(fixture.Heap) == baseline + 8);
        Call(
            roots,
            SetLocal,
            recipeSlot,
            recipe
        );
        Call(roots, Clear, sourceSlot);
        Call(fixture.Heap, Collect);
        Check(source.BitLength == Bytes * 8 && Origin(source) == originalBinRef);
        var slice = (BitString)Call(
            roots,
            Adopt,
            sliceSlot,
            recipe
        )!;
        Check(PhysicalWords(fixture.Heap) == baseline + 13 && Origin(slice) == originalBinRef);
        var pair = (TupleTerm)Call(
            roots,
            Adopt,
            pairSlot,
            Term.Tuple(source, slice)
        )!;
        Check(Size(fixture, pair, false) == 16 && Size(fixture, pair, true) == 19);
        Call(roots, Clear, pairSlot);
        Call(fixture.Heap, Collect);
        Check(PhysicalWords(fixture.Heap) == baseline + 13);
        Call(roots, Clear, recipeSlot);
        Call(fixture.Heap, Collect);
        Check(PhysicalWords(fixture.Heap) == baseline + 8);
        Expect<ObjectDisposedException>(() => _ = source.BitLength);
        Check(slice.ToArray().SequenceEqual(fixture.Data.Skip(2).Take(65)));
        var independent = (BitString)Call(
            roots,
            Adopt,
            independentSlot,
            fixture.NewView()
        )!;
        Check(Origin(independent) != Origin(slice) && PhysicalWords(fixture.Heap) == baseline + 16);
        Call(
            fixture.Heap,
            CollectGeneration,
            false,
            ushort.MaxValue
        );
        Call(
            fixture.Heap,
            CollectGeneration,
            false,
            ushort.MaxValue
        );
        Call(roots, Clear, sliceSlot);
        Call(
            fixture.Heap,
            CollectGeneration,
            false,
            ushort.MaxValue
        );
        Check(PhysicalWords(fixture.Heap) == baseline + 16 && slice.BitLength == 65 * 8);
        Call(fixture.Heap, Collect);
        Check(PhysicalWords(fixture.Heap) == baseline + 8 && independent.BitLength == Bytes * 8);
        Expect<ObjectDisposedException>(() => _ = slice.BitLength);
        Check(Convert.ToInt64(Get(Call(fixture.DomainObject, DomainSnapshot)!, LiveCount)) == fixture.InitialCount + 1);
        Call(roots, Clear, independentSlot);
        Call(fixture.Heap, Collect);
        Check(PhysicalWords(fixture.Heap) == baseline);
        await Task.CompletedTask;
    });

    internal static Task NestedRecipeRetainsOriginalBinRef() => Run(async fixture =>
    {
        IDisposable roots = fixture.Frame();
        int sourceSlot = Slot(roots), recipeSlot = Slot(roots), adoptedSlot = Slot(roots);
        long baseline = PhysicalWords(fixture.Heap);
        var source = (BitString)Call(
            roots,
            Adopt,
            sourceSlot,
            fixture.View
        )!;
        nuint binRef = Origin(source);
        BitString firstRecipe = source.SliceBits(8, 80 * 8);
        BitString nestedRecipe = firstRecipe.SliceBits(8, 65 * 8);
        Check(PhysicalWords(fixture.Heap) == baseline + 8);
        // Only the SECOND unowned recipe is a registered root. CLR locals do
        // not retain physical guest headers or supply an implicit GC root.
        Call(
            roots,
            SetLocal,
            recipeSlot,
            nestedRecipe
        );
        Call(roots, Clear, sourceSlot);
        Call(fixture.Heap, Collect);
        Check(source.BitLength == Bytes * 8 && Origin(source) == binRef);
        Check(PhysicalWords(fixture.Heap) == baseline + 8);
        var adopted = (BitString)Call(
            roots,
            Adopt,
            adoptedSlot,
            nestedRecipe
        )!;
        Check(Origin(adopted) == binRef && PhysicalWords(fixture.Heap) == baseline + 13);
        Check(adopted.ToArray().SequenceEqual(fixture.Data.Skip(3).Take(65)));
        Call(roots, Clear, recipeSlot);
        Call(fixture.Heap, Collect);
        Check(PhysicalWords(fixture.Heap) == baseline + 8 && Origin(adopted) == binRef);
        Expect<ObjectDisposedException>(() => _ = source.BitLength);
        Check(adopted.ToArray().SequenceEqual(fixture.Data.Skip(3).Take(65)));
        Call(roots, Clear, adoptedSlot);
        Call(fixture.Heap, Collect);
        Check(PhysicalWords(fixture.Heap) == baseline);
        Expect<ObjectDisposedException>(() => _ = adopted.BitLength);
        fixture.ReleaseNative();
        await ((Task)Get(fixture.Payload, Retired)).WaitAsync(Deadline);
    });

    internal static Task RollbackAndHostEscape() => Run(async fixture =>
    {
        long baseline = PhysicalWords(fixture.Heap);
        IDisposable construction = fixture.Frame();
        object custody = Call(construction, BeginConstruction, fixture.View)!;
        BitString unpublished;
        lock (Get(fixture.Heap, Gate))
            unpublished = (BitString)Call(
                fixture.Heap,
                Create,
                fixture.View,
                custody,
                true
            )!;
        Check(PhysicalWords(fixture.Heap) == baseline + 8 && unpublished.BitLength == Bytes * 8);
        construction.Dispose();
        Check(PhysicalWords(fixture.Heap) == baseline);
        Expect<ObjectDisposedException>(() => _ = unpublished.BitLength);
        IDisposable roots = fixture.Frame();
        int slot = Slot(roots);
        var owned = (BitString)Call(
            roots,
            Adopt,
            slot,
            fixture.View
        )!;
        object otherHeap = Activator.CreateInstance(
            fixture.Heap.GetType(),
            Members,
            null,
            [fixture.Runtime],
            null
        )!;
        fixture.ExtraHeaps.Add(otherHeap);
        Expect<InvalidOperationException>(() => Call(otherHeap, ReadBits, Get(owned, Storage)));
        using var host = new NativeTermHostScope524(fixture.Runtime);
        var portable = (BitString)Call(
            fixture.Heap,
            Export,
            roots,
            owned
        )!;
        MethodInfo admit = typeof(NativeTermHostScope524).GetMethod(
            AdmitHost,
            StaticMembers,
            null,
            [typeof(ProcessRuntime), typeof(Term)],
            null
        )!;
        _ = admit.Invoke(null, [fixture.Runtime, portable]);
        fixture.ReleaseNative();
        Call(roots, Clear, slot);
        Call(fixture.Heap, Collect);
        Check(PhysicalWords(fixture.Heap) == baseline && portable.ToArray().SequenceEqual(fixture.Data.Skip(1)));
        Expect<ObjectDisposedException>(() => _ = owned.BitLength);
        ProcessRuntime foreignRuntime = fixture.ForeignRuntime();
        ExpectForeign(() => DirectNative(owned, foreignRuntime));
        Expect<ObjectDisposedException>(() => DirectNative(owned, fixture.Runtime));
        Check(!((Task)Get(fixture.Payload, Retired)).IsCompleted);
        host.Dispose();
        await ((Task)Get(fixture.Payload, Retired)).WaitAsync(Deadline);
        Expect<ObjectDisposedException>(() => _ = portable.ToArray());
    });

    internal static Task CanonicalPartAndOpaqueReregister() => Run(async fixture =>
    {
        using var guest = GeneratedModuleCompiler.Compile(GuestSource);
        guest.Definition.Register(fixture.Runtime.Modules);
        Term result = await fixture.GuestCall(GuestModule, GuestPart, fixture.View);
        Check(result.Equals(Term.I(65)));
        var functions = (Dictionary<(string Module, string Function, int Arity), ErlangFunction>)typeof(ModuleRegistry)
            .GetField(Functions, Members)!.GetValue(fixture.Runtime.Modules)!;
        ErlangFunction canonical = functions[(NativeGuest535Names.BinaryModule, Part, 3)];
        fixture.Runtime.Modules.DeleteModule(NativeGuest535Names.BinaryModule);
        fixture.Runtime.Modules.Register(
            NativeGuest535Names.BinaryModule,
            Part,
            3,
            canonical
        );
        bool refused = false;
        try
        {
            _ = await fixture.GuestCall(GuestModule, GuestPart, fixture.View);
        }
        catch (NotSupportedException error) when (error.Message == NativeGuest535Names.ScopeRequired)
        {
            refused = true;
        }
        Check(refused);
    });

    private static ulong Size(Fixture fixture, Term value, bool flat)
    {
        object?[] arguments = [fixture.Context, value, flat, 0UL];
        Check((bool)Call(fixture.Heap, ReadSize, arguments)!);

        return (ulong)arguments[3]!;
    }

    private static nuint Origin(BitString value)
    {
        object storage = Get(value, Storage), heap = Get(storage, "Owner");
        lock (Get(heap, Gate))
            return Word(Get(Get(storage, Slab), Cells), (int)Get(storage, Offset) + 4);
    }

    private static int Slot(IDisposable frame) => (int)Call(frame, Reserve)!;

    private static long PhysicalWords(object heap) => Convert.ToInt64(Get(Get(heap, Snapshot), Physical));

    private static nuint Word(object cells, int index) => (nuint)Call(cells, ReadNumeric, index)!;

    private static void Write(object cells, int index, nuint value) => Call(
        cells,
        WriteNumeric,
        new[] { value },
        index
    );

    private static object Get(object target, string name)
    {
        try
        {
            return target.GetType().GetProperty(name, Members)!.GetValue(target)!;
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static object? Call(object target, string name, params object?[] args)
        => NativeResource524Access.Call(target, name, args);

    private static void Expect<T>(Action body) where T : Exception
    {
        try
        {
            body();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(Failure);
    }

    private static void Check(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }

    private static object? DirectNative(Term value, ProcessRuntime expected)
    {
        Type type = typeof(ProcessRuntime).Assembly.GetType("Erlang.NativeResourceRoots524", true)!;
        try
        {
            return type.GetMethod("Direct524", StaticMembers)!.Invoke(null, [value, expected]);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static void ExpectForeign(Action body)
    {
        Type names = typeof(ProcessRuntime).Assembly.GetType("Erlang.NativeResource524Names", true)!;
        string expected = (string)names.GetField("Foreign", StaticMembers)!.GetRawConstantValue()!;
        try
        {
            body();
        }
        catch (NotSupportedException error) when (error.Message == expected)
        {
            return;
        }
        throw new InvalidOperationException(Failure);
    }

    private static void ThrowFailures(List<Exception> failures)
    {
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    }

    private static async Task Run(Func<Fixture, Task> body)
    {
        var fixture = new Fixture();
        var failures = new List<Exception>();
        try
        {
            await fixture.Initialize();
            await body(fixture);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            await fixture.Drain(failures);
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    }

    private sealed class Fixture
    {
        internal ProcessRuntime Runtime { get; } = OwnedGuest506RuntimeFactory.Create();
        private RuntimeNodeFoundationHostActor? actor;
        private readonly List<IDisposable> roots = new();
        internal List<object> ExtraHeaps { get; } = new();
        private readonly List<ProcessRuntime> foreignRuntimes = new();
        private object? bindings;
        private bool released;
        internal ProcessContext Context { get; private set; } = null!;
        internal object Heap { get; private set; } = null!;
        internal object DomainObject { get; private set; } = null!;
        internal object Payload { get; private set; } = null!;
        internal BitString View { get; private set; } = null!;
        internal long InitialCount { get; private set; }
        internal nint Address { get; private set; }
        internal byte[] Data { get; } = Enumerable.Range(0, NativeResource524Names.PayloadBytes).Select(index => (byte)index).ToArray();

        internal async Task Initialize()
        {
            actor = new RuntimeNodeFoundationHostActor(Runtime);
            ProcessContext context = await actor.Ready.WaitAsync(Deadline);
            Context = context;
            Heap = Get(context, ResourceBinary577TestNames.Heap);
            DomainObject = Get(Runtime, Domain);
            InitialCount = Convert.ToInt64(Get(Call(DomainObject, DomainSnapshot)!, LiveCount));
            var provider = NativeResource524Access.Provider(Runtime);
            bindings = provider.Bindings;
            Payload = NativeResource524Access.Allocate(Runtime, provider.Provider, 0);
            Address = (nint)Get(Payload, ResourceBinary577TestNames.Pointer);
            Marshal.Copy(
                Data,
                0,
                Address,
                Data.Length
            );
            View = NewView();
        }

        internal Task<Term> GuestCall(string module, string function, Term argument)
            => actor!.InvokeAsync(async context => await Runtime.Modules.Call(
                context,
                module,
                function,
                argument
            ));

        internal BitString NewView() => (BitString)Call(
            DomainObject,
            Binary,
            Payload,
            Address + 1,
            (nuint)Bytes
        )!;

        internal IDisposable Frame()
        {
            roots.EnsureCapacity(checked(roots.Count + 1));
            var frame = (IDisposable)Call(Heap, Enter)!;
            roots.Add(frame);

            return frame;
        }

        internal ProcessRuntime ForeignRuntime()
        {
            foreignRuntimes.EnsureCapacity(checked(foreignRuntimes.Count + 1));
            ProcessRuntime runtime = OwnedGuest506RuntimeFactory.Create();
            foreignRuntimes.Add(runtime);

            return runtime;
        }

        internal void ReleaseNative()
        {
            if (released || Payload is null) return;
            if (NativeCount(Payload) == 0)
            {
                released = true;

                return;
            }
            var failures = new List<Exception>();
            try
            {
                Call(Payload, ResourceBinary577TestNames.ReleaseNative);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            // A queue/retirement exception can occur after the real decrement.
            // Preserve that error, but never retry an already-consumed native ref.
            try
            {
                released = NativeCount(Payload) == 0;
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            ThrowFailures(failures);
        }

        private static long NativeCount(object payload)
        {
            object payloadGate = payload.GetType().GetField("gate", Members)!.GetValue(payload)!;
            lock (payloadGate)
                return (long)payload.GetType().GetField("native", Members)!.GetValue(payload)!;
        }

        internal async Task Drain(List<Exception> failures)
        {
            foreach (IDisposable root in roots)
                try
                {
                    root.Dispose();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            try
            {
                ReleaseNative();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (Heap is not null)
                try
                {
                    Call(Heap, Collect);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (Payload is not null)
                try
                {
                    await ((Task)Get(Payload, Retired)).WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (bindings is not null)
                try
                {
                    await ((Task)Call(bindings, RetireImage)!).WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            foreach (object heap in ExtraHeaps)
                try
                {
                    await ((Task)Call(heap, "RetireAsync")!).WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            foreach (ProcessRuntime foreign in foreignRuntimes)
                try
                {
                    await foreign.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            try
            {
                await Runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
    }
}
