using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleCaseCallContinuation(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Case
                {
                    Value: Expr.Call producer,
                    Clauses: [var first, var second]
                } decision)
            {
                return false;
            }

            var (matched, other) = first.Patterns is [Erlang.Pattern.Tuple] ? (first, second) : (second, first);
            if (matched is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple pattern],
                    Body: Expr.Sequence
                    {
                        Items: [
                            Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Call next },
                            Expr.Tuple output
                        ]
                    }
                }
                || other is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom alternative }], Body: Expr.Tuple fallback }
                || result.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            var fields = Semantics.Variables(pattern).ToArray();
            var projected = output.Items.OfType<Expr.Variable>()
                .Select(variable => variable.Name)
                .Where(name => !parameters.Contains(name) && name != result.Name)
                .Distinct(StringComparer.Ordinal).ToArray();
            if (projected is not [var payload] || parameters.Contains(result.Name) || fields.Contains(result.Name)
                || ReadTupleProjection(pattern, payload, parameters) is not { } path)
            {
                return false;
            }

            // The continuation uses the projected field and the call result. The
            // alternative can still use the original inputs, so trim only after matching.
            int[] liveInputs = path.SelectMany(step => step.Checks).Select(check => check.Input)
                .Concat(fallback.Items.OfType<Expr.Variable>().Select(variable => Array.IndexOf(parameters, variable.Name)))
                .Concat(next.Arguments.OfType<Expr.Variable>().Select(variable => Array.IndexOf(parameters, variable.Name)))
                .Distinct().OrderDescending().ToArray();
            if (liveInputs.Length == 0 || liveInputs[^1] < 0
                || ReadEntryCall(producer, parameters) is not { } select)
            {
                return false;
            }

            var saved = liveInputs.Select((input, slot) => (Name: parameters[input], Slot: slot))
                .ToDictionary(binding => binding.Name, binding => binding.Slot, StringComparer.Ordinal);
            var payloadSlot = new Dictionary<string, int>(StringComparer.Ordinal) { [payload] = 0 };
            if (ReadEntryCall(next, [], liveInputs.Select(input => parameters[input]).ToArray()) is not { } call
                || ReadCaseTuple(output, result.Name, payloadSlot) is not { UsesSelector: true } success
                || ReadCaseTuple(fallback, null, saved) is not { } otherwise)
            {
                return false;
            }

            // Reuse a slot only when neither the continuation nor a remaining
            // pattern test needs its old value.
            var finalStep = path[^1];
            int temporary = Array.FindIndex(liveInputs, input =>
                !finalStep.Checks.Any(check => check.Input == input)
                && call.SavedInputs?.Any(move => move.Source == saved[parameters[input]]) != true);
            if (temporary < 0)
            {
                return false;
            }

            int frame = liveInputs.Length;
            var selector = new EntrySelector(select, null, liveInputs, [], 0, []);
            EmitEntrySelector(selector, producer, parameters);
            int alternate = Label();
            int failure = Label();
            owner.Op(IsTuple, LabelTag, alternate);
            owner.Operand(XRegisterTag, 0);

            int register = 0;
            for (int index = 0; index < path.Length; index++)
            {
                var step = path[index];
                if (step.Tag is { } tag)
                {
                    EmitDetailRecordTest(register, step.Arity, tag, failure, typedTuple: index == 0);
                }
                else if (index == 0)
                {
                    owner.Op(TestArity, LabelTag, failure);
                    owner.Operand(XRegisterTag, register);
                    owner.Operand(BeamWire.UnsignedTag, step.Arity);
                }
                else
                {
                    EmitTupleShapeTest(XRegisterTag, register, step.Arity, failure);
                }

                if (index < path.Length - 1)
                {
                    EmitProjectionChecks(step, register, failure, liveInputs);
                    owner.EmitTuplePart(register, step.Field, 1);
                    register = 1;
                    continue;
                }

                int scratch = register == 1 ? 2 : 1;
                bool singleCheck = step.Checks.Length == 1;
                if (singleCheck && step.Checks[0].Field < step.Field)
                {
                    owner.EmitTuplePart(register, step.Checks[0].Field, scratch);
                }

                owner.Op(GetTupleElement, XRegisterTag, register);
                owner.Operand(BeamWire.UnsignedTag, step.Field);
                owner.Operand(YRegisterTag, temporary);
                if (singleCheck)
                {
                    var check = step.Checks[0];
                    if (check.Field > step.Field)
                    {
                        owner.EmitTuplePart(register, check.Field, scratch);
                    }

                    int slot = Array.IndexOf(liveInputs, check.Input);
                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.Operand(XRegisterTag, scratch);
                    if (parentFunction is { } function && HasProvenConsEntry(function, check.Input))
                    {
                        owner.EmitTypedRegister(slot, ConsTypeBits, registerTag: YRegisterTag);
                    }
                    else
                    {
                        owner.Operand(YRegisterTag, slot);
                    }
                }
                else
                {
                    EmitProjectionChecks(step, register, failure, liveInputs);
                }
            }

            owner.EmitOrderedTailMoves(call.Moves, call.Constants, call.SavedInputs);
            if (temporary != frame - 1)
            {
                owner.MoveRegister(YRegisterTag, temporary, YRegisterTag, frame - 1);
            }

            if (frame > 1)
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, frame - 1);
                owner.Operand(BeamWire.UnsignedTag, 1);
            }

            var previous = CallLocation(producer, location);
            owner.EmitCallLine(next, previous);
            Call(call.Module, next.Function, next.Arguments.Count);
            EmitCaseTuple(success);
            owner.EmitFrameReturn(1);

            Mark(alternate);
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(alternative);
            EmitCaseTuple(otherwise);
            owner.EmitFrameReturn(frame);

            Mark(failure);
            if (decision.SourceLine != 0)
            {
                owner.EmitLine(new BeamSourceLocation(decision.PreprocessorFile ?? location.File, decision.SourceLine));
            }

            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;
        }
    }
}
