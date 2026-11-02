using System.Numerics;
using System.Reflection;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;
using static Erlang.Tests.NoBackingClosure192Names;

namespace Erlang.Tests;

internal static class NoBackingClosure192Checks
{
    internal static async Task Run()
    {
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
            await using var foreignRuntime = OwnedGuest506RuntimeFactory.Create(owned);
            await using var foreignActor = new RuntimeNodeFoundationHostActor(foreignRuntime);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            Term? foreign = null;
            IDisposable? foreignFrame = null;
            await foreignActor.InvokeAsync(context =>
            {
                object heap = Property(context, Heap);
                foreignFrame = (IDisposable)Call(heap, Enter)!;
                Term foreignNumber = (Term)Call(
                    foreignFrame,
                    Adopt,
                    (int)Call(foreignFrame, Reserve)!,
                    new Integer(BigInteger.One << IntegerShift)
                )!;
                foreign = (Term)Call(
                    foreignFrame,
                    TupleMethod,
                    (int)Call(foreignFrame, Reserve)!,
                    new Term[] { foreignNumber }
                )!;

                return Task.FromResult(true);
            });
            var provider = Provider(runtime);
            object payload = Allocate(runtime, provider.Provider, 0);
            BitString resource = Binary(runtime, payload);
            try
            {
                await actor.InvokeAsync(context =>
                {
                    object heap = Property(context, Heap);
                    Integer integer;
                    FloatTerm number;
                    BitString inline;
                    Term stale;
                    using (var frame = (IDisposable)Call(heap, Enter)!)
                    {
                        integer = (Integer)Own(frame, new Integer(BigInteger.One << IntegerShift));
                        number = (FloatTerm)Own(frame, new FloatTerm(1.5));
                        inline = (BitString)Own(frame, new BitString(Enumerable.Repeat((byte)Sentinel, InlineBytes).ToArray()));
                        Require(StorageOf(heap, integer) is not null && StorageOf(heap, number) is not null && StorageOf(heap, inline) is not null);
                        long first = Accumulate(
                            runtime,
                            heap,
                            frame,
                            integer,
                            number,
                            SmallCount
                        );
                        long second = Accumulate(
                            runtime,
                            heap,
                            frame,
                            integer,
                            number,
                            LargeCount
                        );
                        Require(first == SmallCount && second == LargeCount);
                        object beforeInline = Snapshot(runtime);
                        int inlineRoot = (int)Call(frame, Reserve)!;
                        Term list = Nil.Value;
                        for (int index = 0; index < SmallCount; index++)
                            list = (Term)Call(
                                frame,
                                ConsMethod,
                                inlineRoot,
                                inline,
                                list
                            )!;
                        Certificate(
                            heap,
                            list,
                            noBacking: true,
                            noBinary: false
                        );
                        Require(Read(Snapshot(runtime), References) == Read(beforeInline, References) + 1);
                        Call(frame, Clear, inlineRoot);
                        Require(Read(Snapshot(runtime), References) == Read(beforeInline, References));

                        var ordinary = (BitString)Own(frame, new BitString(new byte[OrdinaryBytes]));
                        int invoked = 0;
                        var opaque = new FunctionTerm(0, (_, _) =>
                        {
                            invoked++;

                            return ValueTask.FromResult<Term>(Nil.Value);
                        });
                        var writable = (BitString)typeof(BitString).GetMethod(Writable, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [InlineBytes])!;
                        foreach (Term excluded in new Term[] { ordinary, resource, writable, opaque, new MapTerm([new(Term.I(1), integer)]), runtime.MakeReference() })
                        {
                            int root = (int)Call(frame, Reserve)!;
                            var boxed = (Term)Call(
                                frame,
                                TupleMethod,
                                root,
                                new Term[] { excluded }
                            )!;
                            Certificate(
                                heap,
                                boxed,
                                noBacking: false,
                                noBinary: false
                            );
                            if (!ReferenceEquals(excluded, ordinary)) EmptyLiteralProof200Checks.AssertNotCertified(heap, boxed);
                        }
                        Require(invoked == 0);
                        int foreignSlot = (int)Call(frame, Reserve)!;
                        bool rejected = false;
                        try
                        {
                            Call(
                                frame,
                                TupleMethod,
                                foreignSlot,
                                new Term[] { foreign! }
                            );
                        }
                        catch (InvalidOperationException error) when (error.Message == Foreign)
                        {
                            rejected = true;
                        }
                        Require(rejected);
                        EmptyLiteralProof200Checks.RejectForeign(heap, foreign!);
                        int retained = (int)Call(frame, Reserve)!;
                        stale = (Term)Call(
                            frame,
                            TupleMethod,
                            retained,
                            new Term[] { integer, number, inline }
                        )!;
                        Certificate(
                            heap,
                            stale,
                            noBacking: true,
                            noBinary: false
                        );
                        Call(
                            heap,
                            Collect,
                            true,
                            Major
                        );
                        Require(integer.Value == BigInteger.One << IntegerShift && number.Value == 1.5 && inline.ToArray()[0] == Sentinel);
                    }
                    Call(
                        heap,
                        Collect,
                        true,
                        Major
                    );
                    Stale(() => _ = integer.Value);
                    Stale(() => _ = number.Value);
                    Stale(() => _ = inline.BitLength);
                    using (var reader = (IDisposable)Call(heap, Enter)!)
                        Stale(() => Call(
                            reader,
                            Set,
                            (int)Call(reader, Reserve)!,
                            stale
                        ));

                    return Task.FromResult(true);
                });
            }
            finally
            {
                Call(payload, SuspensionRootLease821Names.ReleaseNative);
                await runtime.DisposeAsync();
                await ((Task)Property(payload, SuspensionRootLease821Names.Retired)).WaitAsync(Deadline);
                await ((Task)Call(provider.Bindings, SuspensionRootLease821Names.RetireImage)!).WaitAsync(Deadline);
                foreignFrame?.Dispose();
                GC.KeepAlive(resource);
            }
        }
    }

    internal static void AssertLiteralChild(ProcessContext context, Term literal)
    {
        object heap = Property(context, Heap);
        using var frame = (IDisposable)Call(heap, Enter)!;
        int slot = (int)Call(frame, Reserve)!;
        var enclosing = (TupleTerm)Call(
            frame,
            TupleMethod,
            slot,
            new Term[] { literal }
        )!;
        Require(ReferenceEquals(enclosing.Items[0], literal));
        Certificate(
            heap,
            enclosing,
            noBacking: false,
            noBinary: false
        );
        EmptyLiteralProof200Checks.AssertLiteralGraph(heap, enclosing);
        LiteralCapture397Checks.AssertLiteralChild(context, literal);
    }

    private static Term Own(object frame, Term value)
        => (Term)Call(
            frame,
            Adopt,
            (int)Call(frame, Reserve)!,
            value
        )!;

    private static long Accumulate(
        ProcessRuntime runtime,
        object heap,
        object frame,
        Term first,
        Term second,
        int count
    )
    {
        int root = (int)Call(frame, Reserve)!;
        object before = Snapshot(runtime);
        Term value = Nil.Value;
        for (int index = 0; index < count; index++)
            value = (Term)Call(
                frame,
                ConsMethod,
                root,
                index % 2 == 0 ? first : second,
                value
            )!;
        object after = Snapshot(runtime);
        Certificate(
            heap,
            value,
            noBacking: true,
            noBinary: true
        );
        Require(Read(after, Operations) - Read(before, Operations) == count);
        long visits = Read(after, Visits) - Read(before, Visits);
        Call(frame, Clear, root);

        return visits;
    }

    private static object Snapshot(ProcessRuntime runtime) => Property(Property(runtime, Registry), Traversal);

    private static long Read(object value, string name) => (long)Property(value, name);

    private static object? StorageOf(object heap, Term value)
        => heap.GetType().GetMethod(Storage, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, [value]);

    private static void Certificate(
        object heap,
        Term value,
        bool noBacking,
        bool noBinary
    )
    {
        object storage = StorageOf(heap, value)!;
        Require((bool)Property(storage, NoBacking) == noBacking && (bool)Property(storage, NoBinary) == noBinary);
    }

    private static void Stale(Action read)
    {
        bool rejected = false;
        try
        {
            read();
        }
        catch (ObjectDisposedException)
        {
            rejected = true;
        }
        Require(rejected);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
