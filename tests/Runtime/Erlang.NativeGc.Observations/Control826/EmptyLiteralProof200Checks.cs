using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.EmptyLiteralProof200Names;

namespace Erlang.Tests;

internal static class EmptyLiteralProof200Checks
{
    private static readonly MethodInfo CaptureMethod = typeof(ProcessRuntime).Assembly.GetType(CaptureType, true)!
        .GetMethod(Capture, BindingFlags.NonPublic | BindingFlags.Static)!;

    internal static async Task Run()
    {
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(context =>
            {
                object heap = Property(context, Heap);
                Term stale;
                using (var frame = (IDisposable)Call(heap, Enter)!)
                {
                    AssertMutableToken(context, heap, frame);
                    Term binary = Own(frame, new BitString(new byte[OrdinaryBytes]));
                    Term integer = Own(frame, new Integer(BigInteger.One << IntegerShift));
                    Term number = Own(frame, new FloatTerm(1.5));
                    int headSlot = (int)Call(frame, Reserve)!;
                    var head = (Term)Call(
                        frame,
                        TupleMethod,
                        headSlot,
                        new Term[] { binary, integer, number }
                    )!;
                    AssertProof(heap, head, true);
                    Term small = Build(frame, head, SmallNodes);
                    Term large = Build(frame, head, LargeNodes);
                    AssertProof(heap, small, true);
                    AssertProof(heap, large, true);
                    for (int index = 0; index < Warmup; index++)
                    {
                        CaptureEmpty(heap, small);
                        CaptureEmpty(heap, large);
                    }
                    // Synchronous actual Capture801 calls on this actor thread;
                    // the complete held graphs differ twofold in node count.
                    long first = Measure(heap, small);
                    long second = Measure(heap, large);
                    Require(first > 0 && second <= first + first / 2);
                    Console.WriteLine(string.Format(
                        Allocations,
                        owned,
                        first,
                        second
                    ));
                    Call(
                        heap,
                        Collect,
                        true,
                        Major
                    );
                    Require(((TupleTerm)((Cons)large).Head).Items[0] is BitString actual && actual.BitLength == OrdinaryBytes * 8);
                    stale = large;
                }
                Call(
                    heap,
                    Collect,
                    true,
                    Major
                );
                bool rejected = false;
                try
                {
                    CaptureValue(heap, stale);
                }
                catch (ObjectDisposedException)
                {
                    rejected = true;
                }
                Require(rejected);

                return Task.FromResult(true);
            });
        }
    }

    internal static void AssertNotCertified(object heap, Term value) => AssertProof(heap, value, false);

    internal static void AssertLiteralGraph(object heap, Term value)
    {
        AssertProof(heap, value, false);
        IDisposable? retained = CaptureValue(heap, value);
        try
        {
            Require(retained is not null);
        }
        finally
        {
            retained?.Dispose();
        }
    }

    internal static void RejectForeign(object heap, Term value)
    {
        bool rejected = false;
        try
        {
            CaptureValue(heap, value);
        }
        catch (InvalidOperationException error) when (error.Message == NoBackingClosure192Names.Foreign)
        {
            rejected = true;
        }
        Require(rejected);
    }

    private static void AssertMutableToken(ProcessContext context, object heap, object frame)
    {
        Assembly runtimeAssembly = typeof(ProcessRuntime).Assembly;
        object token = Activator.CreateInstance(
            runtimeAssembly.GetType(TokenType, true)!,
            [Enum.ToObject(runtimeAssembly.GetType(FlagsType, true)!, 0), Nil.Value, 0L, context.Self, 0L]
        )!;
        lock (Property(heap, Gate))
        {
            var mutable = (Term)Call(heap, MakeMutableToken, token)!;
            Call(
                frame,
                Set,
                (int)Call(frame, Reserve)!,
                mutable
            );
            AssertNotCertified(heap, mutable);
            var enclosing = (Term)Call(
                frame,
                TupleMethod,
                (int)Call(frame, Reserve)!,
                new Term[] { mutable }
            )!;
            AssertNotCertified(heap, enclosing);
        }
    }

    private static Term Own(object frame, Term value)
        => (Term)Call(
            frame,
            Adopt,
            (int)Call(frame, Reserve)!,
            value
        )!;

    private static Term Build(object frame, Term head, int count)
    {
        int root = (int)Call(frame, Reserve)!;
        Term value = Nil.Value;
        for (int index = 0; index < count; index++)
            value = (Term)Call(
                frame,
                ConsMethod,
                root,
                head,
                value
            )!;

        return value;
    }

    private static long Measure(object heap, Term value)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < Repeats; index++) CaptureEmpty(heap, value);

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static void CaptureEmpty(object heap, Term value)
    {
        IDisposable? unexpected = CaptureValue(heap, value);
        try
        {
            Require(unexpected is null);
        }
        finally
        {
            unexpected?.Dispose();
        }
    }

    private static IDisposable? CaptureValue(object heap, Term value)
    {
        try
        {
            return (IDisposable?)CaptureMethod.Invoke(null, [heap, value, null]);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void AssertProof(object heap, Term value, bool expected)
    {
        object storage = heap.GetType().GetMethod(Storage, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(null, [value])!;
        Require((bool)Property(storage, Proof) == expected);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
