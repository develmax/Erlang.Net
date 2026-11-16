using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleResultCase(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence
                {
                    Items: [Expr.Match { Pattern: Pattern.Tuple pattern, Value: Expr.Call producer } match, Expr.Case decision]
                })
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            var fields = new Dictionary<string, int>(StringComparer.Ordinal);
            Atom? tag = (pattern.Items.FirstOrDefault() as Pattern.Literal)?.Value as Atom;
            for (int index = tag is null ? 0 : 1; index < pattern.Items.Count; index++)
            {
                if (pattern.Items[index] is not Pattern.Variable field
                    || field.Name != VariableScopeNames.Wildcard
                        && (parameters.Contains(field.Name, StringComparer.Ordinal) || !fields.TryAdd(field.Name, index)))
                {
                    return false;
                }
            }

            bool caught = decision.Value is Expr.Catch;
            Expr operand = decision.Value is Expr.Catch handler ? handler.Operand : decision.Value;
            var selectorCall = operand as Expr.Call;
            string? selected = operand is Expr.Variable variable ? variable.Name
                : selectorCall?.Arguments.OfType<Expr.Variable>().Select(argument => argument.Name).FirstOrDefault(fields.ContainsKey);
            if (selected is null || !fields.TryGetValue(selected, out int selectedField)
                || caught && selectorCall is null)
            {
                return false;
            }

            var branches = decision with { Value = new Expr.Literal(Nil.Value) };
            var scope = clause with
            {
                Patterns = [.. clause.Patterns, .. fields.Keys.Select(name => new Pattern.Variable(name))],
                Body = branches
            };
            if (ReadUsedEntryParameters(scope) is not { } branchReads)
            {
                return false;
            }

            var callReads = selectorCall?.Arguments.OfType<Expr.Variable>().Select(argument => argument.Name)
                .ToHashSet(StringComparer.Ordinal) ?? [];
            int[] inputs = Enumerable.Range(0, parameters.Length)
                .Where(index => branchReads.Contains(parameters[index]) || callReads.Contains(parameters[index]))
                .Reverse().ToArray();
            var extraFields = fields.Where(field => branchReads.Contains(field.Key)
                || field.Key != selected && callReads.Contains(field.Key)).ToArray();
            if (ReadTupleCaseProducer(
                match,
                producer,
                parameters,
                inputs
            ) is not { } selector)
            {
                return false;
            }

            var stack = new string[selector.FrameSize + extraFields.Length];
            for (int index = 0; index < selector.Saved.Length; index++)
            {
                stack[index + extraFields.Length] = parameters[selector.Saved[index]];
            }

            for (int slot = 0; slot < extraFields.Length; slot++)
            {
                stack[slot] = extraFields[slot].Key;
            }

            var saved = stack.Select((name, slot) => (name, slot)).Where(binding => branchReads.Contains(binding.name))
                .ToDictionary(binding => binding.name, binding => binding.slot, StringComparer.Ordinal);
            EntryCall? consumer = selectorCall is null ? null : ReadEntryCall(selectorCall, [selected], stack);
            if (selectorCall is not null && consumer is null
                || ReadRegisterCase(
                    branches,
                    saved,
                    allowUnmatchedLiterals: true,
                    selectorCall: caught ? null : consumer
                ) is not { } plan)
            {
                return false;
            }

            if (plan is { Tests: null, Opcode: IsTaggedTuple, Field: >= 0 }
                && decision.Clauses[0] is { Patterns: [Pattern.Tuple shape], Body: Expr.Tuple result }
                && ReturnsMatchedTuple(shape, result))
            {
                plan = plan with { Match = new RegisterCaseResult(null, null), Field = -1 };
            }

            int[] registers = new int[extraFields.Length];
            int register = 1;
            for (int index = 0; index < registers.Length; index++)
            {
                registers[index] = extraFields[index].Key == selected ? 0 : register++;
            }

            RegisterCase? projected = consumer is null && extraFields.Length != 0 && register < FirstReservedXRegister
                ? ProjectCaseFields(plan, registers) : null;
            if (projected is not null)
            {
                plan = projected;
            }

            int offset = projected is null ? extraFields.Length : 0;
            if (offset != 0 && selector.Prefix is not null)
            {
                selector = selector with
                {
                    Call = selector.Call with
                    {
                        SavedInputs = selector.Call.SavedInputs?.Select(move => (move.Destination, move.Source + offset)).ToList()
                    },
                    Expired = selector.Expired.Select(slot => slot + offset).ToArray()
                };
            }

            EmitEntrySelector(
                selector,
                selector.Call.Call,
                parameters,
                savedOffset: offset,
                trailingSlots: caught ? 1 : 0
            );
            var checkedAt = CallLocation(producer, selector.Prefix is { } prefix ? CallLocation(prefix.Call, location) : location);
            var failureOperands = new List<int>();
            if (tag is not null)
            {
                failureOperands.Add(checked((int)owner.instructions.Position + 1));
                EmitDetailRecordTest(
                    0,
                    pattern.Items.Count,
                    tag,
                    0
                );
            }
            else if (selector.Call.Module is not null || !ReturnsTuple(
                producer,
                pattern.Items.Count,
                [],
                exact: true
            ))
            {
                ShapeTest(IsTuple);
                ShapeTest(TestArity);
                owner.Operand(BeamWire.UnsignedTag, pattern.Items.Count);
            }

            for (int slot = 0; slot < extraFields.Length; slot++)
            {
                if (projected is null || registers[slot] != 0)
                {
                    owner.Op(GetTupleElement, XRegisterTag, 0);
                    owner.Operand(BeamWire.UnsignedTag, extraFields[slot].Value);
                    owner.Operand(projected is null ? YRegisterTag : XRegisterTag, projected is null ? slot : registers[slot]);
                }
            }

            owner.EmitTuplePart(0, selectedField, 0);
            int frame = selector.FrameSize + offset;
            var previous = checkedAt;
            if (consumer is not null)
            {
                owner.EmitOrderedTailMoves(consumer.Moves, consumer.Constants, consumer.SavedInputs);
                int catchLabel = caught ? Label() : 0;
                if (caught)
                {
                    EnterCatch(frame, catchLabel);
                }

                owner.EmitCallLine(consumer.Call, previous);
                Call(consumer.Module, consumer.Call.Function, consumer.Call.Arguments.Count);
                previous = CallLocation(consumer.Call, previous);
                if (caught)
                {
                    EndCatch(frame++, catchLabel);
                }
            }

            int beforeBranches = owner.lineInstructions;
            int type = consumer is null || caught ? 0
                : RegisterCaseSelectorType(consumer, plan.Opcode == IsTaggedTuple ? IsTuple : plan.Opcode);
            EmitRegisterCase(
                plan,
                frame,
                previous,
                type
            );
            if (failureOperands.Count != 0)
            {
                int failure = Label();
                foreach (int position in failureOperands)
                {
                    owner.deferredLabelOperands.Add((position, failure));
                }

                EmitTaggedCallMatchFailure(
                    match,
                    failure,
                    checkedAt,
                    previous,
                    forceLine: beforeBranches != owner.lineInstructions
                );
            }

            return true;

            void ShapeTest(byte opcode)
            {
                failureOperands.Add(checked((int)owner.instructions.Position + 1));
                owner.Op(opcode, LabelTag, 0);
                owner.Operand(XRegisterTag, 0);
            }
        }

        private static RegisterCase? ProjectCaseFields(RegisterCase plan, int[] registers)
        {
            if (plan.Field >= 0 || plan.NestedField is not null || ReadResult(plan.Fallback) is not { } fallback)
            {
                return null;
            }

            var match = plan.Match is null ? null : ReadResult(plan.Match);
            if (plan.Match is not null && match is null)
            {
                return null;
            }

            List<(Term Key, RegisterCaseResult Result)>? dispatch = null;
            if (plan.Dispatch is not null)
            {
                dispatch = [];
                foreach (var (key, result) in plan.Dispatch)
                {
                    if (ReadResult(result) is not { } branch)
                    {
                        return null;
                    }

                    dispatch.Add((key, branch));
                }
            }

            List<RegisterCase>? tests = null;
            if (plan.Tests is not null)
            {
                tests = [];
                foreach (RegisterCase test in plan.Tests)
                {
                    if (ProjectCaseFields(test, registers) is not { } branch)
                    {
                        return null;
                    }

                    tests.Add(branch);
                }
            }

            return plan with { Match = match, Fallback = fallback, Dispatch = dispatch, Tests = tests };

            RegisterCaseResult? ReadResult(RegisterCaseResult result)
            {
                if (result is { Tail: not null } or { TupleCall: not null } or { Sequence: not null }
                    or { Cons: not null } or { DeferredField: not null })
                {
                    return null;
                }

                if (result.Slot is int slot)
                {
                    return slot < registers.Length ? result with { Slot = null, Register = registers[slot] }
                        : result with { Slot = slot - registers.Length };
                }

                if (result.Tuple is not { } tuple)
                {
                    return result;
                }

                if (tuple.Steps is not [var step])
                {
                    return null;
                }

                var operands = step.Operands.Select(operand => operand.Tag != YRegisterTag ? operand
                    : operand.Register < registers.Length
                        ? operand with { Tag = XRegisterTag, Register = registers[operand.Register] }
                        : operand with { Register = operand.Register - registers.Length }).ToArray();
                int live = operands.Where(operand => operand.Tag == XRegisterTag)
                    .Select(operand => operand.Register + 1).DefaultIfEmpty(0).Max();
                // The case selector is still live when the branch uses a projected register.
                if (live > 1)
                {
                    live = Math.Max(live, 2);
                }

                return result with { Tuple = tuple with { Steps = [(step.Destination, operands)], EntryLive = live } };
            }
        }

        private EntrySelector? ReadTupleCaseProducer(
            Expr.Match match,
            Expr.Call producer,
            string[] parameters,
            int[] inputs
        )
        {
            var flattened = FlattenCallArguments([match]);
            if (flattened is
                [
                    Expr.Match { Pattern: Pattern.Variable prefixResult, Value: Expr.Call prefix },
                    Expr.Match { Value: Expr.Call read }
                ])
            {
                return ReadEntrySelector(
                    read,
                    parameters,
                    inputs,
                    trimFrame: false,
                    preceding: (prefix, prefixResult.Name, false)
                );
            }

            return ReadEntrySelector(
                producer,
                parameters,
                inputs,
                trimFrame: false
            );
        }

        private static bool ReturnsMatchedTuple(Pattern.Tuple pattern, Expr.Tuple result)
        {
            if (pattern.Items.Count != result.Items.Count)
            {
                return false;
            }

            return pattern.Items.Zip(result.Items).All(pair => pair switch
            {
                (Pattern.Variable variable, Expr.Variable value) =>
                    variable.Name != VariableScopeNames.Wildcard && variable.Name == value.Name,
                (Pattern.Literal literal, Expr.Literal value) => literal.Value.Equals(value.Value),
                _ => false
            });
        }
    }
}
