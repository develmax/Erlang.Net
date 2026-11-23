using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCheckedResultRecordContinuation(
            Clause clause,
            List<(int Label, BeamSourceLocation Location)>? recordFailures = null
        )
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Case
                {
                    Value: Expr.Call lookup,
                    Clauses:
                    [
                    { Patterns: [Pattern.Literal { Value: Atom sentinel }], Guard: null, Body: Expr.Tuple missing },
                    {
                        Patterns: [Pattern.Variable record],
                        Guard: null,
                        Body: Expr.Sequence
                        {
                            Items:
                                [
                                    Expr.Match first,
                                    Expr.Match second,
                                    Expr.Match third,
                                    Expr.Call continuation,
                                    Expr.Tuple completed
                                ]
                        }
                    }
                    ]
                }
                || record.Name == VariableScopeNames.Wildcard
                || continuation.Arguments is not
                [
                    Expr.Variable firstArgument,
                    Expr.Variable secondArgument,
                    Expr.List { Items: [Expr.Variable head], Tail: Expr.Variable tail },
                    ..
                ]
                || !TryResolveOrdinaryCallTarget(continuation, out string? module))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            var inputs = new Dictionary<string, (int Input, int? Field)>(StringComparer.Ordinal);
            for (int register = 0; register < parameters.Length; register++)
            {
                if (parameters[register] != VariableScopeNames.Wildcard)
                {
                    inputs.Add(parameters[register], (register, null));
                }
            }

            if (inputs.ContainsKey(record.Name) || !inputs.ContainsKey(head.Name)
                || ReadEntryCall(lookup, parameters, allowConstructedArgument: true) is not { } producer
                || ReadFiniteEffectArguments(missing.Items, inputs) is not { } absent
                || ReadFiniteEffectArguments(completed.Items, inputs) is not { } result
                || result.Any(value => value.Binding == head.Name))
            {
                return false;
            }

            var fields = new Dictionary<string, Expr.RecordAccess>(StringComparer.Ordinal);
            Expr.RecordAccess? firstAccess = null;
            foreach (Expr.Match projection in new[] { first, second, third })
            {
                if (projection is not
                    {
                        Pattern: Pattern.Variable binding,
                        Value: Expr.RecordAccess { Value: Expr.Variable receiver } access
                    }
                    || receiver.Name != record.Name || inputs.ContainsKey(binding.Name)
                    || binding.Name == record.Name || binding.Name == VariableScopeNames.Wildcard
                    || access.Index <= 0 || access.Index >= access.Arity
                    || (firstAccess is not null && (access.Name != firstAccess.Name || access.Arity != firstAccess.Arity))
                    || !fields.TryAdd(binding.Name, access))
                {
                    return false;
                }

                firstAccess ??= access;
            }

            if (firstAccess is null || firstArgument.Name == secondArgument.Name
                || firstArgument.Name == tail.Name || secondArgument.Name == tail.Name
                || !fields.TryGetValue(firstArgument.Name, out var firstField)
                || !fields.TryGetValue(secondArgument.Name, out var secondField)
                || !fields.TryGetValue(tail.Name, out var tailField))
            {
                return false;
            }

            var constants = new List<(int Destination, Term Value)>();
            for (int index = 3; index < continuation.Arguments.Count; index++)
            {
                if (!AttributeLiteral.TryNormalizeConstant(continuation.Arguments[index], out Term constant))
                {
                    return false;
                }

                constants.Add((index, constant));
            }

            var saved = new Dictionary<string, int>(StringComparer.Ordinal) { [head.Name] = 0 };
            foreach (var value in result.Concat(absent))
            {
                if (value.Binding is { } name)
                {
                    saved.TryAdd(name, saved.Count);
                }
            }

            if (saved.Count == 1)
            {
                return false;
            }

            int live = Math.Max(
                EntryCallLiveCount(producer, parameters),
                saved.Keys.Max(name => inputs[name].Input + 1)
            );
            if (producer.ConstructedArgument is { } constructed)
            {
                live = Math.Max(
                    live,
                    constructed.Operands.Select(operand => operand.Source.GetValueOrDefault(-1) + 1).DefaultIfEmpty(0).Max()
                );
            }

            EmitFrameAllocation(live, saved.Count, producer.ConstructedArgument?.HeapWords ?? 0);
            foreach (var (name, slot) in saved)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    inputs[name].Input,
                    YRegisterTag,
                    slot
                );
            }

            int missingResult = Label();
            int badRecord = Label();
            EmitEntryCall(producer);
            owner.Op(ExactNotEqualTest, LabelTag, missingResult);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(sentinel);
            owner.EmitRecordTest(
                badRecord,
                XRegisterTag,
                0,
                Term.A(firstAccess.Name),
                firstAccess.Arity
            );

            // The call result is still the only live X register at this heap check. Keep it
            // available until every record field has been read and leave the failure edge intact.
            owner.Op(TestHeap, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(0, tailField.Index, 1);
            owner.Op(PutList, YRegisterTag, saved[head.Name]);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 2);
            owner.EmitTuplePart(0, firstField.Index, 1);
            owner.EmitTuplePart(0, secondField.Index, 0);
            owner.EmitOrderedTailMoves([], constants);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);

            // The cons consumes the head, leaving its lowest slot available for trimming.
            int remaining = saved.Count - 1;
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, remaining);
            owner.EmitCallLine(continuation, CallLocation(lookup, location));
            Call(module, continuation.Function, continuation.Arguments.Count);
            var retained = saved.Where(entry => entry.Value > 0)
                .ToDictionary(entry => entry.Key, entry => entry.Value - 1, StringComparer.Ordinal);
            EmitEntryEffectTupleReturn(result, retained);

            Mark(missingResult);
            EmitEntryEffectTupleReturn(absent, saved);

            var failure = new BeamSourceLocation(firstAccess.PreprocessorFile, firstAccess.SourceLine);
            if (recordFailures is null)
            {
                EmitCheckedResultRecordFailure(badRecord, failure);
            }
            else
            {
                recordFailures.Add((badRecord, failure));
            }

            return true;
        }

        private void EmitCheckedResultRecordFailure(int failure, BeamSourceLocation at)
        {
            Mark(failure);
            if (at.Line > 0)
            {
                BeamSourceLocation previous = owner.lastEmittedLine;
                owner.EmitLine(at);
                owner.lastEmittedLine = previous;
            }

            owner.Op(BadRecord, XRegisterTag, 0);
        }
    }
}
