using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitLiteralCaseRetainedCallChain(Clause clause)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Variable { Name: not VariableScopeNames.Wildcard } incoming],
                    Body: Expr.Case
                    {
                        Value: Expr.Call selector,
                        Clauses:
                        [
                        { Guard: null, Patterns: [Pattern.Literal key], Body: var literal },
                        { Guard: null, Patterns: [Pattern.Variable selected], Body: Expr.Sequence continuation }
                        ]
                    }
                }
                || selected.Name == VariableScopeNames.Wildcard || selected.Name == incoming.Name
                || !AttributeLiteral.TryNormalizeConstant(literal, out Term onMatch)
                || ReadEntryCall(selector, [incoming.Name]) is not { } first
                || continuation.Items is not
                [
                    Expr.Match { Pattern: Pattern.Variable next, Value: Expr.Call lookup },
                    Expr.Match { Pattern: Pattern.Variable constructed, Value: Expr.Call construct },
                    Expr.Call tail
                ]
                || new[] { incoming.Name, selected.Name, next.Name, constructed.Name }.Distinct(StringComparer.Ordinal).Count() != 4
                || next.Name == VariableScopeNames.Wildcard || constructed.Name == VariableScopeNames.Wildcard
                || ReadEntryCall(lookup, [], [VariableScopeNames.Wildcard, incoming.Name]) is not { } second
                || second.Constants.All(constant => constant.Destination != 0)
                || second.SavedInputs is not [{ Destination: > 0, Source: 1 }]
                || construct.Arguments is not [Expr.Variable left, Expr.Variable right]
                || left.Name != selected.Name || right.Name != next.Name
                || ReadEntryCall(construct, [next.Name], [selected.Name]) is not { } third
                || ReadEntryCall(tail, [constructed.Name]) is not { } last
                || !tail.Arguments.OfType<Expr.Variable>().Any(variable => variable.Name == constructed.Name))
            {
                return false;
            }

            // The selected value survives the second call and has its own slot
            // at the case split. The incoming argument dies during call setup.
            EmitFrameAllocation(1, 2);
            EmitFrameInitialization(0, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            EmitEntryCall(first);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            int fallback = Label();
            owner.Op(ExactEqualTest, LabelTag, fallback);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(key.Value);
            owner.EmitConstant(onMatch, 0);
            owner.EmitFrameReturn(2);

            Mark(fallback);
            owner.EmitOrderedTailMoves(
                second.Moves,
                [],
                second.SavedInputs
            );
            foreach (var value in second.Constants.Where(value => value.Destination != 0))
            {
                owner.EmitConstant(value.Value, value.Destination);
            }

            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitConstant(second.Constants.Single(value => value.Destination == 0).Value, 0);
            var source = CallLocation(selector, location);
            owner.EmitCallLine(lookup, source);
            Call(second.Module, lookup.Function, lookup.Arguments.Count);

            owner.EmitOrderedTailMoves(
                third.Moves,
                third.Constants,
                third.SavedInputs
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitCallLine(construct, CallLocation(lookup, source));
            Call(third.Module, construct.Function, construct.Arguments.Count);
            EmitEmptyFrameTail(last, CallLocation(construct, CallLocation(lookup, source)));

            return true;
        }
    }
}
