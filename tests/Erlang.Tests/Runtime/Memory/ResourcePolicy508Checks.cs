using System.Numerics;
using System.Reflection;
using static Erlang.Tests.ResourcePolicy508TestNames;

namespace Erlang.Tests;

internal static class ResourcePolicy508Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static Task Table()
    {
        object classes = Create(ClassesType, 64);
        ulong previous = 0;
        foreach (ulong expected in ObservedHeapClasses64)
        {
            Check((ulong)Call(
                classes,
                Next,
                expected,
                false
            )! == expected);
            Check((ulong)Call(
                classes,
                Next,
                previous + 1,
                false
            )! == expected);
            if (previous != 0)
                Check((ulong)Call(
                    classes,
                    Next,
                    previous,
                    true
                )! == expected);
            previous = expected;
        }
        Check((ulong)Call(
            classes,
            Next,
            0UL,
            true
        )! == ObservedHeapClasses64[0]);
        object classes32 = Create(ClassesType, 32);
        Check((ulong)Call(
            classes32,
            Next,
            ObservedMinimumHeap,
            false
        )! == ObservedMinimumHeap);

        return Task.CompletedTask;
    }

    internal static Task DefaultsAndConversion()
    {
        object classes = Create(ClassesType, 64);
        object initial = Create(
            PolicyType,
            ObservedMinimumHeap,
            ObservedMinimumBinaryHeap,
            ushort.MaxValue
        );
        object defaults = Create(DefaultsType, classes, initial);
        object capturedBefore = Property(defaults, Snapshot);
        object previous = Call(defaults, ExchangeHeap, Term.I(0))!;
        object capturedAfter = Property(defaults, Snapshot);
        Check((ulong)Property(previous, MinimumHeap) == ObservedMinimumHeap);
        Check((ulong)Property(capturedBefore, MinimumHeap) == ObservedMinimumHeap);
        Check((ulong)Property(capturedAfter, MinimumHeap) == ObservedHeapClasses64[0]);
        Check((ulong)StaticCall(
            Minimum,
            classes,
            ObservedMinimumHeap,
            Term.I(0)
        )! == ObservedMinimumHeap);
        Check((ushort)StaticCall(Sweep, classes, Term.I(65536))! == ushort.MaxValue);
        object?[] parsed = [classes, capturedAfter, capturedAfter, Term.Tuple(Term.A(SweepFlag), Term.I(65536)), null];
        Check((bool)StaticCall(Apply, parsed)!);
        Check((ushort)Property(parsed[^1]!, FullSweep) == 0);
        // An invalid earlier duplicate must fail immediately rather than being
        // silently replaced by a later valid option.
        ExpectBadArgument(() => StaticCall(
            Apply,
            classes,
            initial,
            initial,
            Term.Tuple(Term.A(HeapFlag), Term.I(-1)),
            null
        ));
        ExpectBadArgument(() => StaticCall(Sweep, classes, new Integer(BigInteger.One << 100)));

        return Task.CompletedTask;
    }

    internal static async Task Generations()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            object heap = Property(context, Heap);
            object collectionPolicy = Property(context, CollectionPolicy);
            Call(collectionPolicy, ExchangeThreshold, ushort.MaxValue);
            using var frame = (IDisposable)Call(heap, Enter)!;
            int slot = (int)Call(frame, Reserve)!;
            Call(
                frame,
                Set,
                slot,
                Term.Tuple(Term.I(508), Term.List(Term.I(1), Term.I(2)))
            );
            Term rooted = (Term)Call(frame, Read, slot)!;
            long payload = (long)Property(Property(heap, GenerationSnapshot), Nursery);
            Check(payload > 0);
            Check(!(bool)Property(Call(context, ContextCollection, false)!, Major));
            Check((long)Property(Property(heap, GenerationSnapshot), Survivor) == payload);
            Call(context, ContextCollection, false);
            Check((long)Property(Property(heap, GenerationSnapshot), Old) == payload);
            Call(frame, Clear, slot);
            object minor = Call(context, ContextCollection, false)!;
            Check((long)Property(minor, Reclaimed) == 0);
            Check(rooted.Equals(Term.Tuple(Term.I(508), Term.List(Term.I(1), Term.I(2)))));
            object major = Call(context, ContextCollection, true)!;
            Check((bool)Property(major, Major));
            Check((long)Property(major, Reclaimed) == payload);
            Check((long)Property(Property(heap, GenerationSnapshot), Old) == 0);
            try
            {
                _ = ((TupleTerm)rooted).Items[0];
                throw new InvalidOperationException(Failure);
            }
            catch (ObjectDisposedException) { }
            Call(
                frame,
                Set,
                slot,
                Term.Tuple(Term.I(509))
            );
            Call(collectionPolicy, ExchangeThreshold, (ushort)0);
            Check((bool)Property(Call(context, ContextCollection, false)!, Major));

            return true;
        });
    }

    private static object Create(string name, params object?[] arguments)
        => Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(name, true)!,
            Members,
            null,
            arguments,
            null
        )!;

    private static object Property(object target, string name) => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

    private static object? Call(object target, string name, params object?[] arguments)
        => target.GetType().GetMethod(name, Members)!.Invoke(target, arguments);

    private static object? StaticCall(string name, params object?[] arguments)
        => typeof(ProcessRuntime).Assembly.GetType(ParserType, true)!.GetMethod(name, Static)!.Invoke(null, arguments);

    private static void ExpectBadArgument(Action body)
    {
        try
        {
            body();
            throw new InvalidOperationException(Failure);
        }
        catch (TargetInvocationException error) when (error.InnerException is ErlangException failure)
        {
            Check(failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
        }
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
