using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record ListRecordMergeArm(
            Atom Key,
            Atom? TupleTag,
            Atom RecordTag,
            int Arity,
            int Selector,
            Expr.Call Call,
            (int Destination, int Source)[] Fields
        );

        private bool TryEmitListRecordMerge(FunctionDefinition function, int functionInfo)
        {
            var clauses = function.Clauses;
            if (clauses.Count < 5 || function.Arity + 2 >= FirstReservedXRegister
                || clauses[^2] is not { Guard: null, Body: Expr.Call otherCall } other
                || clauses[^1] is not { Guard: null, Body: Expr.Call emptyCall } empty)
            {
                return false;
            }

            int input = Array.FindIndex(empty.Patterns.ToArray(), pattern => ListEntryTest(pattern) == IsNil);
            int head = function.Arity;
            int nextHead = head + 1;
            if (input < 0
                || other.Patterns[input] is not Pattern.List { Items: [Pattern.Variable otherHead], Tail: Pattern.Variable otherTail }
                || ReadBindings(other, otherTail.Name) is not { } otherInputs
                || !otherInputs.TryAdd(otherHead.Name, head)
                || ReadBindings(empty, VariableScopeNames.Wildcard) is not { } emptyInputs
                || !TryEmitRegisterConstructedArgument(otherCall, otherInputs, validateOnly: true)
                || ReadEntryCall(emptyCall, Names(emptyInputs)) is not { } end)
            {
                return false;
            }

            var arms = new List<ListRecordMergeArm>();
            foreach (Clause clause in clauses.SkipLast(2))
            {
                if (ReadArm(clause) is not { } arm)
                {
                    return false;
                }

                arms.Add(arm);
            }

            var scalar = arms[0];
            var tuples = arms.Skip(1).ToArray();
            if (scalar.TupleTag is not null || tuples[0].TupleTag is not { } tupleTag
                || tuples.Any(arm => !tupleTag.Equals(arm.TupleTag) || !scalar.RecordTag.Equals(arm.RecordTag)
                    || arm.Arity != scalar.Arity || arm.Selector != scalar.Selector)
                || tuples.Select(arm => arm.Key).Distinct().Count() != tuples.Length)
            {
                return false;
            }

            int[] entries = tuples.Select(_ => Label()).ToArray();
            int scalarEntry = Label();
            int fallback = Label();
            int nonList = Label();
            int[] tupleFailures = tuples.Select(_ => Label()).ToArray();
            int scalarFailure = Label();
            owner.EmitNonemptyList(input, nonList);
            owner.EmitListParts(
                input,
                XRegisterTag,
                head,
                XRegisterTag,
                input
            );
            EmitDetailRecordTest(
                head,
                scalar.Arity,
                scalar.RecordTag,
                fallback
            );
            owner.EmitTuplePart(head, scalar.Selector, nextHead);
            owner.Op(IsTuple, LabelTag, scalarEntry);
            owner.Operand(XRegisterTag, nextHead);
            EmitDetailRecordTest(
                nextHead,
                2,
                tupleTag,
                fallback,
                TupleTypeBits
            );
            owner.EmitTuplePart(nextHead, 1, nextHead);
            owner.EmitMembershipSelect(
                nextHead,
                fallback,
                tuples.Select((arm, index) => ((Term)arm.Key, entries[index]))
                .OrderBy(entry => entry.Item1, Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true)))
            );
            for (int index = 0; index < tuples.Length; index++)
            {
                Mark(entries[index]);
                EmitUpdate(tuples[index], tupleFailures[index]);
            }

            Mark(scalarEntry);
            owner.EmitAtomTest(nextHead, scalar.Key.Name, fallback);
            EmitUpdate(scalar, scalarFailure);
            Mark(fallback);
            TryEmitRegisterConstructedArgument(otherCall, otherInputs);
            Mark(nonList);
            owner.EmitNilTest(XRegisterTag, input, functionInfo);
            EmitRegisterTailCall(end);
            for (int index = 0; index < tuples.Length; index++)
            {
                EmitBadSource(tuples[index], tupleFailures[index]);
            }

            EmitBadSource(scalar, scalarFailure);

            return true;

            void EmitBadSource(ListRecordMergeArm arm, int failure)
            {
                Mark(failure);
                owner.EmitLine(CallLocation(arm.Call, location));
                owner.Op(BadRecord, XRegisterTag, nextHead);
            }

            Dictionary<string, int>? ReadBindings(Clause clause, string tail)
            {
                var bindings = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int register = 0; register < function.Arity; register++)
                {
                    if ((register == input ? new Pattern.Variable(tail) : clause.Patterns[register]) is not Pattern.Variable variable
                        || variable.Name != VariableScopeNames.Wildcard && !bindings.TryAdd(variable.Name, register))
                    {
                        return null;
                    }
                }

                return bindings;
            }

            string[] Names(Dictionary<string, int> bindings)
            {
                string[] names = Enumerable.Repeat(VariableScopeNames.Wildcard, function.Arity).ToArray();
                foreach (var (name, register) in bindings)
                {
                    names[register] = name;
                }

                return names;
            }

            ListRecordMergeArm? ReadArm(Clause clause)
            {
                if (clause is not { Guard: null, Body: Expr.Call call }
                    || clause.Patterns[input] is not Pattern.List
                    {
                        Items: [AliasPattern alias, Pattern.Variable next],
                        Tail: Pattern.Variable rest
                    }
                    || !TryReadPatternAlias(alias, out var original, out var shape)
                    || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom recordTag }, ..] } record
                    || ReadBindings(clause, rest.Name) is not { } inputs
                    || original.Name == VariableScopeNames.Wildcard || next.Name == VariableScopeNames.Wildcard
                    || !inputs.TryAdd(original.Name, head) || !inputs.TryAdd(next.Name, nextHead)
                    || call.Arguments.Count != function.Arity
                    || call.Arguments[input] is not Expr.List
                    {
                        Items: [Expr.RecordUpdate { Value: Expr.Variable updateTarget, Fields.Count: > 0 } update],
                        Tail: Expr.Variable tail
                    }
                    || updateTarget.Name != original.Name || update.Name != recordTag.Name || update.Arity != record.Items.Count
                    || rest.Name == VariableScopeNames.Wildcard || tail.Name != rest.Name)
                {
                    return null;
                }

                int selector = -1;
                for (int index = 1; index < record.Items.Count; index++)
                {
                    if (record.Items[index] is Pattern.Variable { Name: VariableScopeNames.Wildcard })
                    {
                        continue;
                    }

                    if (selector >= 0)
                    {
                        return null;
                    }

                    selector = index;
                }

                if (selector < 0)
                {
                    return null;
                }

                var (key, tag) = record.Items[selector] switch
                {
                    Pattern.Literal { Value: Atom atom } => (atom, (Atom?)null),
                    Pattern.Tuple { Items: [Pattern.Literal { Value: Atom prefix }, Pattern.Literal { Value: Atom atom }] } => (atom, prefix),
                    _ => (null, null)
                };
                if (key is null || call.Arguments.Where((_, index) => index != input)
                    .Any(argument => argument is not Expr.Variable))
                {
                    return null;
                }

                for (int index = 0; index < function.Arity; index++)
                {
                    if (index != input && (!inputs.TryGetValue(((Expr.Variable)call.Arguments[index]).Name, out int register)
                        || register != index))
                    {
                        return null;
                    }
                }

                var fields = new List<(int Destination, int Source)>();
                foreach (var field in update.Fields.OrderBy(field => field.Index))
                {
                    if (field.Value is not Expr.RecordAccess { Value: Expr.Variable source } access
                        || source.Name != next.Name || access.Name != update.Name || access.Arity != update.Arity)
                    {
                        return null;
                    }

                    fields.Add((field.Index + 1, access.Index));
                }

                if (nextHead + fields.Count - 1 >= FirstReservedXRegister)
                {
                    return null;
                }

                var prepared = call with
                {
                    Arguments = call.Arguments.Select((argument, index) => index == input ? tail : argument).ToArray()
                };

                return TryResolveOrdinaryCall(prepared, out string? module)
                    ? new(
                        key,
                        tag,
                        recordTag,
                        update.Arity,
                        selector,
                        call with { Module = module },
                        fields.ToArray()
                    )
                    : null;
            }

            void EmitUpdate(ListRecordMergeArm arm, int badRecord)
            {
                owner.EmitNonemptyList(input, fallback);
                owner.EmitListParts(
                    input,
                    XRegisterTag,
                    nextHead,
                    XRegisterTag,
                    input
                );
                EmitDetailRecordTest(
                    nextHead,
                    arm.Arity,
                    arm.RecordTag,
                    badRecord
                );
                owner.Op(TestHeap, BeamWire.UnsignedTag, arm.Arity + 1 + ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, nextHead + 1);
                for (int index = 0; index < arm.Fields.Length; index++)
                {
                    owner.EmitTuplePart(nextHead, arm.Fields[index].Source, FieldRegister(index));
                }

                owner.Op(UpdateRecord, AtomTag, owner.Intern("copy"));
                owner.Operand(BeamWire.UnsignedTag, arm.Arity);
                owner.EmitTypedRegister(head, TupleTypeBits);
                owner.Operand(XRegisterTag, head);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, arm.Fields.Length * 2);
                for (int index = 0; index < arm.Fields.Length; index++)
                {
                    owner.Operand(BeamWire.UnsignedTag, arm.Fields[index].Destination);
                    owner.Operand(XRegisterTag, FieldRegister(index));
                }

                owner.Op(PutList, XRegisterTag, head);
                owner.Operand(XRegisterTag, input);
                owner.Operand(XRegisterTag, input);
                EmitRegisterTailTarget(arm.Call, function.Arity);

                int FieldRegister(int index) => index == arm.Fields.Length - 1 ? nextHead : nextHead + index + 1;
            }
        }
    }
}
