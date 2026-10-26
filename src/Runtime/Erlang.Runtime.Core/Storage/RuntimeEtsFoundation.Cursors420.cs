namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal async ValueTask<Term> CoreCursor420(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        bool initial = arguments.Count == 3;
        if (!initial && arguments[0] is Atom { Name: EtsCursorNames420.End }) return arguments[0];
        Term identifier;
        TupleTerm? raw = null;
        if (initial) identifier = arguments[0];
        else
        {
            raw = arguments[0] as TupleTerm;
            if (raw is null || raw.Items.Count < 1) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            identifier = raw.Items[0];
        }
        RuntimeEtsFoundationTable admitted;
        RuntimeEtsCoreOptions407 options;
        lock (gate)
        {
            admitted = CoreResolve407(context, identifier, false);
            options = coreOptions407.GetValue(admitted, _ => throw new InvalidOperationException(EtsIndexNames420.InvalidIndex));
        }
        EtsCursorState420 state;
        if (initial)
        {
            long chunk = EtsCursorTerms420.Small(arguments[2]);
            if (chunk <= 0) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            Term specification = function is EtsCursorNames420.Match or EtsCursorNames420.MatchObject
             ? Term.List(Term.Tuple(
                 arguments[1],
                 Nil.Value,
                 Term.List(Term.A(function == EtsCursorNames420.Match ? MatchSpecNames.Variables : MatchSpecNames.Whole))
             )) : arguments[1];
            if (specification is Nil) return Term.A(EtsCursorNames420.End);
            MatchSpecProgram program = MatchSpecCompiler.Compile(specification, MatchSpecDialect.Table).Program ?? throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            var bounds = EtsCursorPattern420.Analyze(specification, options.KeyPosition);
            if (bounds.Kind == EtsCursorBoundness420.Impossible) return Term.A(EtsCursorNames420.End);
            state = new() { Tid = identifier, Chunk = chunk, Program = program, Initial = true, Bounds = bounds, BoundSingle = bounds.Kind == EtsCursorBoundness420.Bound, Reverse = function == EtsCursorNames420.SelectReverse ? 1 : 0, EndCondition = bounds.Kind == EtsCursorBoundness420.Partial ? (function == EtsCursorNames420.SelectReverse ? bounds.Least! : bounds.Most!) : Nil.Value };
        }
        else state = ParseCursor420(context, raw!, options.Kind);
        EtsCursorSnapshot420 snapshot = CaptureCursor420(
            context,
            admitted,
            state,
            options
        );

        return options.Kind == EtsCoreTableKind407.OrderedSet ? await RunTreeCursor420(context, state, snapshot) : await RunHashCursor420(context, state, snapshot);
    }

    private static EtsCursorState420 ParseCursor420(ProcessContext context, TupleTerm raw, EtsCoreTableKind407 kind)
    {
        var fields = raw.Items;
        bool tree = kind == EtsCoreTableKind407.OrderedSet;
        if (tree && fields.Count != EtsCursorNames420.TreeArity || !tree && fields.Count is not (EtsCursorNames420.HashPublicArity or EtsCursorNames420.HashTrapArity)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
        long chunk = EtsCursorTerms420.Small(fields[tree ? 3 : 2]);
        EtsCursorTerms420.List(fields[tree ? 5 : 4]);
        long count = EtsCursorTerms420.Small(fields[tree ? 7 : 5]);
        long reverse = tree ? EtsCursorTerms420.Small(fields[6]) : 0;
        long slot = tree ? 0 : EtsCursorTerms420.Small(fields[1]);
        if (!tree && (chunk < 0 || count < 0)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
        if (!tree && fields.Count == EtsCursorNames420.HashTrapArity) _ = EtsCursorTerms420.Small(fields[6]);
        if (fields[tree ? 4 : 3] is not ReferenceTerm reference) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
        MatchSpecProgram program;
        using (var lease = context.Runtime.ReferenceResources.AcquireMatchSpec419(reference))
        {
            if (lease is null) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            program = lease.Value.Program;
        }

        return new() { Tid = fields[0], Chunk = chunk, Count = count, Reverse = reverse, Slot = slot, LastKey = tree ? fields[1] : Nil.Value, EndCondition = tree ? fields[2] : Nil.Value, Accumulator = fields[tree ? 5 : 4], Reference = reference, Program = program };
    }

    private EtsCursorSnapshot420 CaptureCursor420(
        ProcessContext context,
        RuntimeEtsFoundationTable admitted,
        EtsCursorState420 state,
        RuntimeEtsCoreOptions407 options
    )
    {
        RuntimeEtsReadLease?[] leases;
        int[] slots, positions;
        Term handle;
        int extent;
        lock (gate)
        {
            // Initial name resolution cannot switch generation while compilation/copy runs.
            var table = CoreResolve407(context, admitted.Handle, false);
            if (!ReferenceEquals(table, admitted)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Id);
            var index = table.Primary420 ?? throw new InvalidOperationException(EtsIndexNames420.InvalidIndex);
            handle = table.Handle;
            extent = index.BucketCount;
            var payloads = new List<RuntimeEtsStoredObject>();
            var locations = new List<int>();
            if (options.Kind == EtsCoreTableKind407.OrderedSet)
            {
                slots = Array.Empty<int>();
                foreach (var group in index.Ordered) foreach (var row in group.Rows)
                {
                    payloads.Add(row.Payload);
                    locations.Add(0);
                }
            }
            else
            {
                if (state.Initial && state.Bounds?.HashKeys is { } keys)
                {
                    var bound = new List<int>();
                    foreach (var key in keys)
                    {
                        var group = CoreKeyGroup420(table, key, context.Cancellation);
                        if (group is not null && !group.Deleted)
                        {
                            int slot = index.Slot(CoreHash420(key));
                            if (!bound.Contains(slot)) bound.Add(slot);
                        }
                    }
                    slots = bound.ToArray();
                }
                else if (!state.Initial && (state.Slot < 0 || state.Chunk != 0 && state.Count >= state.Chunk)) slots = Array.Empty<int>();
                else
                {
                    long start = state.Initial ? 0 : state.Slot;
                    if (start >= extent) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
                    slots = Enumerable.Range((int)start, extent - (int)start).ToArray();
                }
                foreach (int slot in slots) foreach (var group in index.Buckets[slot]) for (int i = group.Rows.Length - 1; i >= 0; i--)
                {
                    payloads.Add(group.Rows[i].Payload);
                    locations.Add(slot);
                }
            }
            positions = locations.ToArray();
            leases = new RuntimeEtsReadLease?[payloads.Count];
            try
            {
                for (int i = 0; i < leases.Length; i++)
                {
                    context.Cancellation.ThrowIfCancellationRequested();
                    leases[i] = payloads[i].Acquire();
                }
            }
            catch (Exception error)
            {
                EtsSelectRows409.Release(leases, error);
                throw;
            }
        }
        TupleTerm[] materialized;
        Exception? failure = null;
        try
        {
            materialized = EtsSelectRows409.Materialize(leases, context.Cancellation);
        }
        catch (Exception error)
        {
            failure = error;
            throw;
        }
        finally
        {
            EtsSelectRows409.Release(leases, failure);
        }
        var rows = new EtsCursorRow420[materialized.Length];
        for (int i = 0; i < rows.Length; i++) rows[i] = new(positions[i], CoreKey407(materialized[i], options.KeyPosition), materialized[i]);

        return new(
            options.Kind,
            handle,
            extent,
            rows,
            slots
        );
    }

    private static ReferenceTerm CursorProgram420(ProcessContext context, EtsCursorState420 state)
     => state.Reference ??= context.Runtime.PublishMatchSpec419(context, new EtsCompiledProgram419(state.Program));

    private static async ValueTask<Term> RunHashCursor420(ProcessContext context, EtsCursorState420 state, EtsCursorSnapshot420 snapshot)
    {
        Term accumulator = state.Accumulator;
        long got = state.Count, next = state.Initial ? -1 : state.Slot;
        int reductions = 0;
        bool skip = !state.Initial && (next < 0 || state.Chunk != 0 && got >= state.Chunk);
        bool bound = state.Initial && state.Bounds?.HashKeys is not null;
        if (!skip)
        {
            int row = 0;
            next = -1;
            for (int bucket = 0; bucket < snapshot.ScanSlots.Length; bucket++)
            {
                int slot = snapshot.ScanSlots[bucket];
                while (row < snapshot.Rows.Length && snapshot.Rows[row].Slot == slot)
                {
                    context.Cancellation.ThrowIfCancellationRequested();
                    var result = state.Program.Run(context, snapshot.Rows[row++].Row, MatchSpecRunMode.Production);
                    if (result.Matched)
                    {
                        accumulator = new Cons(result.Value, accumulator);
                        got++;
                    }
                    if (++reductions == EtsCursorNames420.ScanBudget)
                    {
                        reductions = 0;
                        await Task.Yield();
                    }
                }
                if (!bound && state.Chunk != 0 && got >= state.Chunk)
                {
                    next = bucket + 1 < snapshot.ScanSlots.Length ? snapshot.ScanSlots[bucket + 1] : -1;
                    break;
                }
            }
        }
        if (state.Chunk == 0) return accumulator;
        Term rest = Nil.Value;
        long restCount = 0;
        if (got > state.Chunk)
        {
            restCount = got - state.Chunk;
            rest = EtsCursorTerms420.Prefix(
                accumulator,
                restCount,
                !state.Initial,
                out accumulator
            );
        }
        if (rest is not Nil || next >= 0)
        {
            var resource = CursorProgram420(context, state);
            Term tid = state.Initial && state.Tid is Atom ? snapshot.Handle : state.Tid;
            Term cursor = Term.Tuple(
                tid,
                EtsCursorTerms420.EncodedSmall(next),
                Term.I(state.Chunk),
                resource,
                rest,
                EtsCursorTerms420.EncodedSmall(restCount)
            );

            return Term.Tuple(accumulator, cursor);
        }

        return accumulator is Nil ? Term.A(EtsCursorNames420.End) : Term.Tuple(accumulator, Term.A(EtsCursorNames420.End));
    }

    private static async ValueTask<Term> RunTreeCursor420(ProcessContext context, EtsCursorState420 state, EtsCursorSnapshot420 snapshot)
    {
        bool reverse = state.Reverse != 0;
        bool forward = state.Chunk != 0 ? !reverse : reverse;
        Term accumulator = state.Accumulator;
        long got = state.Count;
        Term? last = null;
        int reductions = 0;
        bool firstSlice = state.Initial;
        for (int scan = 0; scan < snapshot.Rows.Length; scan++)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            var row = snapshot.Rows[forward ? scan : snapshot.Rows.Length - 1 - scan];
            if (state.Initial)
            {
                if (state.BoundSingle && TermOrder.Compare(row.Key, state.Bounds!.Least!) != 0) continue;
                if (state.Bounds?.Kind == EtsCursorBoundness420.Partial)
                {
                    Term start = forward ? state.Bounds.Least! : state.Bounds.Most!;
                    int order = EtsCursorPattern420.PartlyCompare(start, row.Key);
                    if (forward ? order > 0 : order < 0) continue;
                }
            }
            else
            {
                int order = TermOrder.Compare(row.Key, state.LastKey);
                if (forward ? order <= 0 : order >= 0) continue;
            }
            last = row.Key;
            if (firstSlice && state.EndCondition is not Nil)
            {
                int end = EtsCursorPattern420.PartlyCompare(state.EndCondition, row.Key);
                if (forward ? end < 0 : end > 0) break;
            }
            var result = state.Program.Run(context, row.Row, MatchSpecRunMode.Production);
            if (result.Matched)
            {
                accumulator = new Cons(result.Value, accumulator);
                got++;
            }
            if (state.Initial && state.BoundSingle) break;
            if (state.Chunk != 0 && got == state.Chunk) break;
            if (++reductions == EtsCursorNames420.ScanBudget)
            {
                // Native resumed traversals deliberately clear sc.end_condition.
                // The supplied raw bound is examined only at a budget trap,
                // after chunk-filled/exhausted paths have already returned.
                if (!firstSlice && state.EndCondition is not Nil)
                {
                    int end = EtsCursorPattern420.PartlyCompare(state.EndCondition, row.Key);
                    if (forward ? end < 0 : end > 0) break;
                }
                firstSlice = false;
                reductions = 0;
                await Task.Yield();
            }
        }
        if (state.Chunk == 0) return accumulator;
        if (state.Initial && state.BoundSingle) return accumulator is Nil ? Term.A(EtsCursorNames420.End) : Term.Tuple(accumulator, Term.A(EtsCursorNames420.End));
        Term continuation;
        if (got < state.Chunk || last is null) continuation = Term.A(EtsCursorNames420.End);
        else continuation = Term.Tuple(
            state.Tid,
            last,
            state.EndCondition,
            Term.I(state.Chunk),
            CursorProgram420(context, state),
            Nil.Value,
            Term.I(state.Reverse),
            Term.I(0)
        );
        if (continuation is Atom { Name: EtsCursorNames420.End } && got == 0) return continuation;

        return await EtsCursorTerms420.ReverseTree(context, accumulator, continuation);
    }
}
