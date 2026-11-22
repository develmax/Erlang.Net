using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record NestedTerminalBucket(
            Atom NestedTag,
            int Arity,
            Term Sentinel,
            int SentinelRegister,
            (int Index, int Register)[] Fields,
            TerminalCaseBranch Exact,
            TerminalCaseBranch Constructor,
            TerminalCaseBranch Fallback,
            BeamFramePlan Frames
        );

        private sealed record FlatTerminalBucket(TerminalCaseBranch Body, bool RequiresFrame, int[] PublishedFields);

        private sealed record TerminalTupleBucket(Term Key, FlatTerminalBucket? Flat, NestedTerminalBucket? Nested);

        private bool TryEmitTerminalTupleBuckets(
            Clause[] clauses,
            int input,
            int noMatch,
            out Clause[] residual
        )
        {
            residual = clauses;
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || clauses is not [var firstClause, ..] || EntryShape(firstClause.Patterns[input]) is not Pattern.Tuple first
                || clauses[0].Patterns.Count + first.Items.Count >= FirstReservedXRegister)
            {
                return false;
            }

            var groups = new Dictionary<Term, List<Clause>>();
            foreach (Clause clause in clauses)
            {
                if (EntryShape(clause.Patterns[input]) is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom or Integer } selected, ..] })
                {
                    // A variable or overlapping row is the start of a new ordered
                    // partition. Only the concrete prefix can be dispatched early.
                    break;
                }

                Term key = selected.Value;
                if (!groups.TryGetValue(key, out var rows))
                {
                    rows = [];
                    groups.Add(key, rows);
                }

                rows.Add(clause);
            }

            int incoming = clauses[0].Patterns.Count;
            int selector = incoming;
            int payload = incoming + 1;
            var buckets = new List<TerminalTupleBucket>();
            var admitted = new HashSet<Clause>(ReferenceEqualityComparer.Instance);
            var publishedFields = new SortedSet<int>();
            foreach (var (key, rows) in groups)
            {
                if (rows is [var row] && ReadFlatTerminalBucket(row, input) is { } flat)
                {
                    buckets.Add(new(key, flat, null));
                    publishedFields.UnionWith(flat.PublishedFields);
                    admitted.Add(row);
                }
                else if (key is Atom && first.Items.Count == 2
                    && ReadNestedTerminalBucket(rows, input, payload) is { } nested)
                {
                    buckets.Add(new(key, null, nested));
                    publishedFields.Add(1);
                    admitted.UnionWith(rows);
                }
            }

            bool constantGroup = admitted.Count == clauses.Length && buckets.All(bucket =>
                bucket.Flat is { Body.Calls.Length: 0, Body.Result.Value: null });
            if (!constantGroup
                && !buckets.Any(bucket => bucket.Flat?.RequiresFrame == true || bucket.Nested?.Frames.Origins.Count > 0))
            {
                return false;
            }

            var ordered = buckets.OrderBy(bucket => bucket.Key, Comparer<Term>.Create(
                (left, right) => TermOrder.Compare(left, right, exact: true)
            )).ToArray();
            int fallthrough = ordered.Length + 1;
            var blocks = new List<BeamFrameBlock>
            {
                new(
                    0,
                    [fallthrough, .. Enumerable.Range(1, ordered.Length)],
                    [],
                    RequiresFrame: false
                )
            };
            for (int index = 0; index < ordered.Length; index++)
            {
                blocks.Add(new(
                    index + 1,
                    [],
                    [],
                    ordered[index].Flat?.RequiresFrame == true
                ));
            }

            blocks.Add(new(
                fallthrough,
                [],
                [],
                RequiresFrame: false
            ));
            if (BeamFramePlacement.Place(0, blocks) is not { } frames || frames.Owners[0] is not null)
            {
                return false;
            }

            residual = clauses.Where(clause => !admitted.Contains(clause)).ToArray();
            int miss = residual.Length == 0 ? noMatch : Label();
            owner.EmitTuplePart(input, 0, selector);
            foreach (int field in publishedFields)
            {
                owner.EmitTuplePart(input, field, incoming + field);
            }

            if (ordered is [var bucketOnly])
            {
                owner.Op(ExactEqualTest, LabelTag, miss);
                owner.Operand(XRegisterTag, selector);
                owner.LiteralOperand(bucketOnly.Key);
                EmitBucket(bucketOnly, frames.Owners[1] is not null);
            }
            else
            {
                int[] labels = ordered.Select(_ => Label()).Reverse().ToArray();
                owner.EmitMembershipSelect(selector, miss, ordered.Select((bucket, index) => (bucket.Key, labels[index])));
                for (int index = ordered.Length - 1; index >= 0; index--)
                {
                    Mark(labels[index]);
                    EmitBucket(ordered[index], frames.Owners[index + 1] is not null);
                }
            }

            if (residual.Length != 0)
            {
                Mark(miss);
            }

            return true;

            void EmitBucket(TerminalTupleBucket selected, bool frame)
            {
                if (selected.Flat is { } flat)
                {
                    EmitTerminalCaseArm(flat.Body, frame);

                    return;
                }

                NestedTerminalBucket bucket = selected.Nested!;
                const int exact = 2;
                const int constructor = 3;
                const int fallback = 4;
                int wrongShape = Label();
                int wrongValue = Label();
                owner.Op(IsTaggedTuple, LabelTag, wrongShape);
                owner.Operand(XRegisterTag, payload);
                owner.Operand(BeamWire.UnsignedTag, bucket.Arity);
                owner.LiteralOperand(bucket.NestedTag);
                foreach (var (index, register) in bucket.Fields)
                {
                    owner.EmitTuplePart(payload, index, register);
                }

                owner.Op(ExactEqualTest, LabelTag, wrongValue);
                owner.Operand(XRegisterTag, bucket.SentinelRegister);
                owner.LiteralOperand(bucket.Sentinel);
                EmitTerminalCaseArm(bucket.Exact, bucket.Frames.Owners[exact] is not null);
                Mark(wrongValue);
                EmitTerminalCaseArm(bucket.Constructor, bucket.Frames.Owners[constructor] is not null);
                Mark(wrongShape);
                EmitTerminalCaseArm(bucket.Fallback, bucket.Frames.Owners[fallback] is not null);
            }
        }

        private FlatTerminalBucket? ReadFlatTerminalBucket(Clause row, int input)
        {
            if (row.Guard is not null || row.Patterns[input] is not Pattern.Tuple tuple
                || !tuple.Items.Skip(1).All(field => field is Pattern.Variable))
            {
                return null;
            }

            int incoming = row.Patterns.Count;
            string[]? parameters = TerminalBucketArguments(row, input, incoming + tuple.Items.Count - 1);
            if (parameters is null)
            {
                return null;
            }

            for (int index = 1; index < tuple.Items.Count; index++)
            {
                parameters[incoming + index] = ((Pattern.Variable)tuple.Items[index]).Name;
            }

            if (!HasDistinctTerminalBindings(parameters)
                || LowerTerminalCaseArm(null, row.Body, parameters) is not { } arm
                || AllocateTerminalCaseRegisters(arm, parameters) is not { } body)
            {
                return null;
            }

            int[] published = arm.Operations.SelectMany(operation => operation.Uses)
                .Where(value => value > incoming && value < parameters.Length)
                .Select(value => value - incoming).Distinct().Order().ToArray();

            return new(
                body,
                arm.RequiresFrame,
                published
            );
        }

        private NestedTerminalBucket? ReadNestedTerminalBucket(
            List<Clause> rows,
            int input,
            int payload
        )
        {
            if (rows is not [{ Guard: null } exact, { Guard: null } constructor, { Guard: null } fallback]
                || exact.Patterns[input] is not Pattern.Tuple
                {
                    Items: [_, Pattern.Tuple { Items: [Pattern.Literal { Value: Atom exactTag }, ..] } exactTuple]
                }
                || constructor.Patterns[input] is not Pattern.Tuple
                {
                    Items: [_, Pattern.Tuple { Items: [Pattern.Literal { Value: Atom constructorTag }, ..] } constructorTuple]
                }
                || fallback.Patterns[input] is not Pattern.Tuple { Items: [_, Pattern.Variable other] }
                || exactTuple.Items.Count != constructorTuple.Items.Count || !exactTag.Equals(constructorTag))
            {
                return null;
            }

            string[]? exactInputs = TerminalBucketArguments(exact, input, payload);
            string[]? constructorInputs = TerminalBucketArguments(constructor, input, payload);
            string[]? fallbackInputs = TerminalBucketArguments(fallback, input, payload);
            if (exactInputs is null || constructorInputs is null || fallbackInputs is null)
            {
                return null;
            }

            fallbackInputs[payload] = other.Name;
            var exactNames = exactInputs.ToList();
            var constructorNames = constructorInputs.ToList();
            var exactUses = LocalFunctionEntries.Walk(exact.Body).OfType<Expr.Variable>()
                .Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
            var constructorUses = LocalFunctionEntries.Walk(constructor.Body).OfType<Expr.Variable>()
                .Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
            var sourceExactNames = exactNames.ToList();
            var sourceConstructorNames = constructorNames.ToList();
            var fields = new List<(int Index, int Register)>();
            var occupied = new HashSet<int> { payload };
            for (int index = 0; index < exactNames.Count; index++)
            {
                if (exactUses.Contains(exactNames[index]) || constructorUses.Contains(constructorNames[index]))
                {
                    occupied.Add(index);
                }
            }

            Term? sentinel = null;
            int sentinelRegister = -1;
            for (int index = 1; index < exactTuple.Items.Count; index++)
            {
                Pattern fixedField = exactTuple.Items[index];
                if (constructorTuple.Items[index] is not Pattern.Variable variable)
                {
                    return null;
                }

                int register = 0;
                while (occupied.Contains(register))
                {
                    register++;
                }

                if (register >= FirstReservedXRegister)
                {
                    return null;
                }

                if (fixedField is Pattern.Literal literal && sentinel is null)
                {
                    sentinel = literal.Value;
                    sentinelRegister = register;
                }
                else if (fixedField is not Erlang.Pattern.Variable)
                {
                    return null;
                }

                if (fixedField is Pattern.Variable binding)
                {
                    sourceExactNames.Add(binding.Name);
                    Store(exactNames, register, binding.Name);
                }

                sourceConstructorNames.Add(variable.Name);
                Store(constructorNames, register, variable.Name);
                occupied.Add(register);
                fields.Add((index, register));
            }

            if (sentinel is null || !HasDistinctTerminalBindings(sourceExactNames)
                || !HasDistinctTerminalBindings(sourceConstructorNames) || !HasDistinctTerminalBindings(fallbackInputs)
                || LowerTerminalCaseArm(null, exact.Body, exactNames.ToArray()) is not { } matches
                || LowerTerminalCaseArm(null, constructor.Body, constructorNames.ToArray()) is not { } otherwise
                || LowerTerminalCaseArm(null, fallback.Body, fallbackInputs) is not { } onMiss)
            {
                return null;
            }

            var frames = BeamFramePlacement.Place(
                0,
                [
                    new(
                        0,
                        [4, 1],
                        [],
                        RequiresFrame: false
                    ),
                    new(
                        1,
                        [3, 2],
                        [],
                        RequiresFrame: false
                    ),
                    new(
                        2,
                        [],
                        [],
                        matches.RequiresFrame
                    ),
                    new(
                        3,
                        [],
                        [],
                        otherwise.RequiresFrame
                    ),
                    new(
                        4,
                        [],
                        [],
                        onMiss.RequiresFrame
                    )
                ]
            );
            if (frames is null || frames.Owners[0] is not null || frames.Owners[1] is not null
                || AllocateTerminalCaseRegisters(matches, exactNames.ToArray()) is not { } plannedExact
                || AllocateTerminalCaseRegisters(otherwise, constructorNames.ToArray()) is not { } plannedConstructor
                || AllocateTerminalCaseRegisters(onMiss, fallbackInputs) is not { } plannedFallback)
            {
                return null;
            }

            return new(
                exactTag,
                exactTuple.Items.Count,
                sentinel,
                sentinelRegister,
                fields.ToArray(),
                plannedExact,
                plannedConstructor,
                plannedFallback,
                frames
            );

            static void Store(List<string> bindings, int register, string name)
            {
                while (bindings.Count <= register)
                {
                    bindings.Add(VariableScopeNames.Wildcard);
                }

                bindings[register] = name;
            }
        }

        private static string[]? TerminalBucketArguments(Clause clause, int input, int last)
        {
            string[] names = Enumerable.Repeat(VariableScopeNames.Wildcard, last + 1).ToArray();
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                if (index == input)
                {
                    continue;
                }

                if (clause.Patterns[index] is not Pattern.Variable variable)
                {
                    return null;
                }

                names[index] = variable.Name;
            }

            return names;
        }
    }
}
