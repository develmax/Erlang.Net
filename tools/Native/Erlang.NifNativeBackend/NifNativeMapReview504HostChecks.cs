using static Erlang.NifNativeBackend.NifNativeMapReview504HostNames;

namespace Erlang.NifNativeBackend;

internal static unsafe class NifNativeMapReview504HostChecks
{
    internal static void SourceFunUniquePreconditions(ProcessContext context)
    {
        var functions = Functions();
        using var hashes = context.Runtime.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append);
        ulong first = hashes.Hash(functions[0]);
        for (int index = 1; index < functions.Length; index++)
        {
            Require(!functions[0].Equals(functions[index]));
            Require(TermOrder.Compare(functions[0], functions[index], true) == 0);
            Require(hashes.Hash(functions[index]) == first);
        }
    }

    // Allocation is measured after construction and warmup; output handles and
    // callback bookkeeping still count. This checks scaling, not elapsed time.
    internal static void StaticLookupAllocations(ProcessContext context)
    {
        var measured = MeasureStaticLookupAllocations(context);
        Require(measured.LargeBytes <= checked(measured.SmallBytes * AllocationScale + AllocationAllowance));
    }

    internal static (long SmallBytes, long LargeBytes) MeasureStaticLookupAllocations(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(Module),
            0,
            state
        );
        using var table = new NifNativeWindowsCallbacks();
        long small = Measure(table, owner, SmallMap);
        long large = Measure(table, owner, LargeMap);
        state.ThrowPending();

        return (small, large);
    }

    // Diagnostic matrix only. Mutation expectations must be taken from the
    // genuine original receipt before registering an exact-behavior assertion.
    internal static Term CaptureCollisionMatrix(ProcessContext context)
    {
        var functions = Functions();
        var entries = new List<KeyValuePair<NifNativeTermValue, NifNativeTermValue>>
        {
            new(new(functions[0]), new(Term.I(100))),
            new(new(functions[1]), new(Term.I(200)))
        };
        for (int index = 0; index < 31; index++)
            entries.Add(new(new(Term.I(index + 1)), new(Term.I(1000 + index))));
        var original = (NifNativeMapNode504)NifNativeMapGraph504.Create(entries, context.Runtime).Pending!;
        var results = new List<Term> { Snapshot(original, functions, context.Runtime) };
        foreach (var kind in new[] { NifNativeMapMutation504Kind.Put, NifNativeMapMutation504Kind.Update, NifNativeMapMutation504Kind.Remove })
            for (int which = 0; which < functions.Length; which++)
            {
                int replacement = kind == NifNativeMapMutation504Kind.Put ? 500 + which : 600 + which;
                var changed = NifNativeMapGraph504.Change(
                    original,
                    new(functions[which]),
                    new(Term.I(replacement)),
                    kind,
                    context.Runtime,
                    out bool found
                );
                results.Add(Term.Tuple(Term.I(found ? 1 : 0), Snapshot(changed, functions, context.Runtime)));
            }
        results.Add(Snapshot(original, functions, context.Runtime));

        return Cons.From(results);
    }

    internal static void IndexedStaticLookupMisses(ProcessContext context)
    {
        foreach (var measurement in MeasureIndexedStaticLookupMisses(context))
            Require(measurement.AllocatedBytes <= checked((long)Lookups * MaximumMissingLookupBytes));
    }

    // Source rule confirmed by actual original2: ordering ties replace the first
    // physical collision leaf, even when exact fun identities differ. Roles are
    // read from this VM's own constructor order, not hardcoded from that sample.
    internal static void CollisionCmpUsesActualFirstRole(ProcessContext context)
    {
        var functions = Functions();
        var entries = new List<KeyValuePair<NifNativeTermValue, NifNativeTermValue>>
        {
            new(new(functions[0]), new(Term.I(100))),
            new(new(functions[1]), new(Term.I(100)))
        };
        for (int index = 0; index < 31; index++)
            entries.Add(new(new(Term.I(index + 1)), new(Term.I(1000 + index))));
        var original = (NifNativeMapNode504)NifNativeMapGraph504.Create(entries, context.Runtime).Pending!;
        Term first = original.Entries.Select(pair => NifNativeTermGraph.Resolve(pair.Key)).First(key => key is FunctionTerm);
        int selected = first.Equals(functions[0]) ? 0 : 1;
        var roles = new[] { functions[selected], functions[1 - selected], functions[2] };
        foreach (var kind in new[] { NifNativeMapMutation504Kind.Put, NifNativeMapMutation504Kind.Update })
            for (int which = 0; which < roles.Length; which++)
            {
                int value = (kind == NifNativeMapMutation504Kind.Put ? 500 : 600) + which;
                var changed = NifNativeMapGraph504.Change(
                    original,
                    new(roles[which]),
                    new(Term.I(value)),
                    kind,
                    context.Runtime,
                    out bool found
                );
                Require(found && changed.Entries.Count == 33);
                Require(NifNativeMapGraph504.TryGet(
                    changed,
                    roles[which],
                    context.Runtime,
                    out var updated
                )
                    && NifNativeTermGraph.Resolve(updated.Value).Equals(Term.I(value)));
                Require(NifNativeMapGraph504.TryGet(
                    changed,
                    roles[0],
                    context.Runtime,
                    out _
                ) == (which == 0));
                Require(changed.Entries.Count(pair => NifNativeTermGraph.Resolve(pair.Key).Equals(roles[which]))
                    == (which == 1 ? 2 : 1));
            }
        for (int which = 0; which < roles.Length; which++)
        {
            var changed = NifNativeMapGraph504.Change(
                original,
                new(roles[which]),
                default,
                NifNativeMapMutation504Kind.Remove,
                context.Runtime,
                out bool found
            );
            Require(found == (which < 2) && changed.Entries.Count == (which < 2 ? 32 : 33));
        }
        Require(original.Entries.Count == 33
            && NifNativeMapGraph504.TryGet(
                original,
                roles[0],
                context.Runtime,
                out var old
            )
            && NifNativeTermGraph.Resolve(old.Value).Equals(Term.I(100)));
    }

    internal static IReadOnlyList<(int Count, bool NullHint, long AllocatedBytes)> MeasureIndexedStaticLookupMisses(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(Module),
            0,
            state
        );
        using var table = new NifNativeWindowsCallbacks();
        var get = (delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint*, int>)((nint*)table.Pointer)[Get];
        var measurements = new List<(int Count, bool NullHint, long AllocatedBytes)>();
        foreach (int count in new[] { SmallMap, LargeMissingMap })
        {
            var map = new MapTerm(Enumerable.Range(0, count).Select(index =>
                new KeyValuePair<Term, Term>(Term.I(index), Term.I(index + 1))));
            nuint handle = owner.Add(map), key = owner.Add(Term.I(count + 1));
            foreach (nint hint in new[] { owner.Pointer, (nint)0 })
            {
                nuint output = MissingSentinel;
                for (int index = 0; index < MissingWarmups; index++)
                    Require(get(
                        hint,
                        handle,
                        key,
                        &output
                    ) == 0 && output == MissingSentinel);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int index = 0; index < Lookups; index++)
                    Require(get(
                        hint,
                        handle,
                        key,
                        &output
                    ) == 0 && output == MissingSentinel);
                measurements.Add((count, hint == 0, GC.GetAllocatedBytesForCurrentThread() - before));
            }
        }
        state.ThrowPending();

        return measurements;
    }

    private static long Measure(NifNativeWindowsCallbacks table, NifNativeCallEnvironment owner, int count)
    {
        var map = new MapTerm(Enumerable.Range(0, count).Select(index =>
            new KeyValuePair<Term, Term>(Term.I(index), Term.I(index + 1))));
        nuint handle = owner.Add(map), key = owner.Add(Term.I(count / 2));
        Require(map.TryGet(Term.I(count / 2), out var expected));
        var get = (delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint*, int>)((nint*)table.Pointer)[Get];
        nuint output = 0;
        for (int index = 0; index < Warmups; index++)
            Require(get(
                owner.Pointer,
                handle,
                key,
                &output
            ) == 1);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < Lookups; index++)
        {
            Require(get(
                owner.Pointer,
                handle,
                key,
                &output
            ) == 1);
            Require(ReferenceEquals(owner.GetValue(output).StaticTerm, expected));
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static Term Snapshot(NifNativeMapNode504 map, IReadOnlyList<FunctionTerm> functions, ProcessRuntime runtime)
    {
        var result = new List<Term> { Term.I(map.Entries.Count) };
        foreach (var function in functions)
        {
            bool found = NifNativeMapGraph504.TryGet(
                map,
                function,
                runtime,
                out var pair
            );
            result.Add(Term.Tuple(Term.I(found ? 1 : 0), found ? NifNativeTermGraph.Resolve(pair.Value) : Term.I(-1)));
        }

        return Cons.From(result);
    }

    private static FunctionTerm[] Functions()
        => new[] { 17, 29, 43 }.Select(value => new FunctionTerm(
            new LocalFunctionIdentity(
                Term.A(Module),
                null,
                FunctionIndex,
                OldUnique,
                new BitString(Enumerable.Repeat((byte)value, UniqueBytes).ToArray())
            ),
            0,
            [Term.I(EnvironmentValue)],
            static (_, _) => throw new InvalidOperationException(Failure)
        )).ToArray();

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
