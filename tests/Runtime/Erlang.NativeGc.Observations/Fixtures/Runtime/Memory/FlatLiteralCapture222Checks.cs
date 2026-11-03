using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.FlatLiteralCapture222Names;

namespace Erlang.Tests;

internal static class FlatLiteralCapture222Checks
{
    private static readonly Type RootType = typeof(ProcessRuntime).Assembly.GetType(RootsType, true)!;
    private static readonly MethodInfo CaptureMethod = RootType.GetMethod(Capture, BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo? FastMethod = RootType.GetMethod(Fast, BindingFlags.Static | BindingFlags.NonPublic);

    // Return a genuine independently retained flat sidecar for the other-VM
    // negative. No domain/image/lease is fabricated by this control.
    internal static IDisposable Run(ProcessContext context, Term literal)
    {
        object heap = Property(context, Heap);
        using var first = (IDisposable)Call(heap, Enter)!;
        using var second = (IDisposable)Call(heap, Enter)!;
        Term bad = (Term)Call(
            first,
            TupleMethod,
            (int)Call(first, Reserve)!,
            new Term[] { literal }
        )!;
        Term healthy = (Term)Call(
            second,
            TupleMethod,
            (int)Call(second, Reserve)!,
            new Term[] { literal }
        )!;
        using var badSidecar = (IDisposable)Call(heap, RetainSidecar, bad)!;
        IDisposable healthySidecar = (IDisposable)Call(heap, RetainSidecar, healthy)!;
        bool success = false;
        try
        {
            Require(IsFlat(healthySidecar));
            Measure(heap, healthy, SingleCase);
            var badLeases = RealLeases(badSidecar);
            var goodLeases = RealLeases(healthySidecar);
            Require(badLeases.Count == 1 && goodLeases.Count > 0 && !badLeases.Overlaps(goodLeases));
            // Dispose ONLY a real distinct fork belonging to the throwaway
            // wrapper. Its metadata node still has actual retained interests.
            ((IDisposable)badLeases.Single()).Dispose();
            bool rejected = false;
            try
            {
                // Capture walks the valid literal FIRST (LIFO) and records its
                // same domain; that must not hide the invalid actual holder.
                using var unexpected = CaptureValue(heap, Term.Tuple(bad, literal));
            }
            catch (InvalidOperationException error) when (error.Message == NoBackingClosure192Names.Foreign)
            {
                rejected = true;
            }
            Require(rejected);
            using (var retained = CaptureValue(heap, healthy)) Require(retained is not null);
            success = true;

            return healthySidecar;
        }
        finally
        {
            if (!success) healthySidecar.Dispose();
        }
    }

    // Compatible with the old production code so Root can build/run a genuine
    // BEFORE allocation baseline by applying only the acceptance postimages.
    internal static void Measure(object heap, Term value, string label)
    {
        using var sidecar = (IDisposable)Call(heap, RetainSidecar, value)!;
        Require(sidecar is not null);
        List<object> nodes = Nodes(sidecar!);
        VerifyProtocol(heap, nodes);
        for (int index = 0; index < Warmup; index++)
            using (var held = CaptureValue(heap, value)) Require(held is not null);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < Calls; index++)
            using (var held = CaptureValue(heap, value)) Require(held is not null);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine(string.Format(
            AllocationLine,
            label,
            IsFlat(sidecar!),
            nodes.Count,
            Items(sidecar!, Parents).Length,
            Calls,
            allocated
        ));
    }

    internal static void RejectForeignHolder(object heap, IDisposable realForeignHolder)
    {
        if (FastMethod is null) return; // BEFORE source has no new helper.
        var sets = NewSets();
        // Metadata dedup state is deliberately prefilled with the real foreign
        // domains. It confers no authority; EVERY actual lease must still fail.
        foreach (object node in Nodes(realForeignHolder))
        {
            foreach (object lease in Items(node, Leases)) Add(sets.Domains, Property(lease, Domain));
            foreach (object lease in Items(node, Intrinsics)) Add(sets.Intrinsics, Property(lease, Domain));
        }
        bool rejected = false;
        try
        {
            Invoke(
                FastMethod,
                null,
                heap,
                realForeignHolder,
                sets.Borrowed,
                sets.Domains,
                sets.Intrinsics
            );
        }
        catch (InvalidOperationException error) when (error.Message == NoBackingClosure192Names.Foreign)
        {
            rejected = true;
        }
        finally
        {
            ReleaseSelected(sets.Borrowed);
        }
        Require(rejected && CountOf(sets.Borrowed) == 0);
    }

    private static void VerifyProtocol(object heap, List<object> nodes)
    {
        if (FastMethod is null) return;
        foreach (object node in nodes)
        {
            var sets = NewSets();
            var counts = Nodes(node).ToDictionary(value => value, value => (int)Field(value, References), ReferenceEqualityComparer.Instance);
            try
            {
                bool actual = (bool)Invoke(
                    FastMethod,
                    null,
                    heap,
                    node,
                    sets.Borrowed,
                    sets.Domains,
                    sets.Intrinsics
                )!;
                Require(actual == IsFlat(node));
                if (!actual)
                {
                    Require(CountOf(sets.Borrowed) == 0 && CountOf(sets.Domains) == 0 && CountOf(sets.Intrinsics) == 0);
                    continue;
                }
                HashSet<object> expected = ExpectedOwners(node);
                Require(expected.SetEquals(((IEnumerable)sets.Borrowed).Cast<object>()));
                var expectedDomains = new HashSet<object>(ReferenceEqualityComparer.Instance);
                var expectedIntrinsics = new HashSet<object>(ReferenceEqualityComparer.Instance);
                foreach (object descendant in Nodes(node))
                {
                    foreach (object lease in Items(descendant, Leases)) expectedDomains.Add(Property(lease, Domain));
                    foreach (object lease in Items(descendant, Intrinsics)) expectedIntrinsics.Add(Property(lease, Domain));
                }
                Require(expectedDomains.SetEquals(((IEnumerable)sets.Domains).Cast<object>())
                    && expectedIntrinsics.SetEquals(((IEnumerable)sets.Intrinsics).Cast<object>()));
            }
            finally
            {
                ReleaseSelected(sets.Borrowed);
                foreach (var count in counts) Require((int)Field(count.Key, References) == count.Value);
            }
        }
    }

    private static HashSet<object> ExpectedOwners(object root)
    {
        var selected = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var domains = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var intrinsic = new HashSet<object>(ReferenceEqualityComparer.Instance);
        // Parent-first reference selection on the exact actual flat graph.
        foreach (object node in Items(root, Parents).Append(root).Distinct(ReferenceEqualityComparer.Instance))
        {
            bool contributes = Items(node, Leases).Any(lease => !domains.Contains(Property(lease, Domain)))
                || Items(node, Intrinsics).Any(lease => !intrinsic.Contains(Property(lease, Domain)));
            if (!contributes) continue;
            selected.Add(node);
            foreach (object descendant in Nodes(node))
            {
                foreach (object lease in Items(descendant, Leases)) domains.Add(Property(lease, Domain));
                foreach (object lease in Items(descendant, Intrinsics)) intrinsic.Add(Property(lease, Domain));
            }
        }

        return selected;
    }

    private static (object Borrowed, object Domains, object Intrinsics) NewSets()
        => (NewSet(RootType), NewSet(RootType.Assembly.GetType(DomainType, true)!), NewSet(RootType.Assembly.GetType(IntrinsicType, true)!));

    private static object NewSet(Type type) => Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(type))!;

    private static void Add(object set, object value) => Invoke(set.GetType().GetMethod(AddMethod)!, set, value);

    private static int CountOf(object set) => (int)Property(set, Count);

    private static void ReleaseSelected(object selected)
    {
        foreach (IDisposable owner in (IEnumerable)selected) owner.Dispose();
    }

    private static bool IsFlat(object node) => Items(node, Parents).All(parent => Items(parent, Parents).Length == 0);

    private static HashSet<object> RealLeases(object root)
        => new(Nodes(root).SelectMany(node => Items(node, Leases)), ReferenceEqualityComparer.Instance);

    private static List<object> Nodes(object root)
    {
        var result = new List<object>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<object>();
        pending.Push(root);
        while (pending.TryPop(out object? node))
        {
            if (!seen.Add(node)) continue;
            result.Add(node);
            foreach (object parent in Items(node, Parents)) pending.Push(parent);
        }

        return result;
    }

    private static object[] Items(object value, string name) => ((IEnumerable)Field(value, name)).Cast<object>().ToArray();

    private static object Field(object value, string name) => value.GetType().GetField(name, Members)!.GetValue(value)!;

    private static IDisposable? CaptureValue(object heap, Term value) => (IDisposable?)Invoke(
        CaptureMethod,
        null,
        heap,
        value,
        null
    );

    private static object? Invoke(MethodInfo method, object? target, params object?[] args)
    {
        try
        {
            return method.Invoke(target, args);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }
}
