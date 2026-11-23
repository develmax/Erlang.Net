using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleFieldEffectReturn(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Guard: null,
                    Body: Expr.Sequence
                    {
                        Items: [var action, var result]
                    }
                } clause
                ]
                || DiscardedCall(action) is not { Arguments: [Expr.Variable used] } call
                || !TryResolveOrdinaryCallTarget(call, out string? module)
                || !AttributeLiteral.TryNormalizeConstant(result, out Term constant))
            {
                return false;
            }

            int input = -1;
            Pattern.Tuple? tuple = null;
            var bindings = new List<Pattern.Variable>();
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                if (clause.Patterns[index] is Pattern.Tuple shape && tuple is null
                    && shape.Items.All(field => field is Pattern.Variable))
                {
                    input = index;
                    tuple = shape;
                    bindings.AddRange(shape.Items.Cast<Pattern.Variable>());
                }
                else if (clause.Patterns[index] is Pattern.Variable parameter)
                {
                    bindings.Add(parameter);
                }
                else
                {
                    return false;
                }
            }

            if (tuple is null || !DistinctBindings(bindings.Where(binding => binding.Name != VariableScopeNames.Wildcard).ToArray())
                || Array.FindIndex(
                    tuple.Items.ToArray(),
                    field => field is Pattern.Variable variable
                    && variable.Name == used.Name && variable.Name != VariableScopeNames.Wildcard
                ) is not (>= 0 and var field))
            {
                return false;
            }

            // The checked tuple is the only input needed by the effect. Its named
            // fields need a register only when the call reads them.
            owner.EmitTupleCheck(input, tuple.Items.Count, owner.nextLabel - 2);
            EmitFrameAllocation(input + 1, 0);
            owner.EmitTuplePart(input, field, 0);
            owner.EmitCallLine(call, location);
            Call(module, call.Function, 1);
            owner.EmitConstant(constant, 0);
            owner.EmitFrameReturn(0);

            return true;
        }

        private static Expr.Call? DiscardedCall(Expr expression) => expression switch
        {
            Expr.Call call => call,
            Expr.Match { Pattern: Pattern.Variable { Name: VariableScopeNames.Wildcard }, Value: Expr.Call call } => call,
            _ => null
        };

        private bool TryEmitNodeGuardPreservedTupleEffect(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Clauses is not
                [
                {
                    Patterns: [Pattern.Tuple guardTuple, Pattern.Variable state],
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.NumericNotEqual or ErlangOperators.ExactNotEqual,
                        Left: Expr.Call { Function: AutoImportNames.Node, Arguments: [Expr.Variable tested] } remoteNode,
                        Right: Expr.Call { Function: AutoImportNames.Node, Arguments: [] } localNode
                    },
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable returned] }
                },
                {
                    Patterns: [Pattern.Variable input, Pattern.Tuple stateTuple],
                    Guard: null,
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                var action,
                                Expr.Tuple { Items: [Expr.Literal { Value: Atom effectTag }, Expr.Tuple rebuilt] }
                            ]
                    }
                },
                {
                    Patterns: [Pattern.Variable ignored, Pattern.Variable fallbackState],
                    Guard: null,
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom fallbackTag }, Expr.Variable fallbackReturn] }
                }
                ]
                || state.Name != returned.Name || fallbackState.Name != fallbackReturn.Name
                || !DistinctBindings(state) || !DistinctBindings(fallbackState) || ignored.Name == fallbackState.Name
                || !tag.Equals(effectTag) || !tag.Equals(fallbackTag)
                || ReadFields(guardTuple, state) is not { } guardFields
                || ReadFields(stateTuple, input) is not { } stateFields
                || Array.IndexOf(guardFields, tested.Name) is not (>= 0 and var endpoint)
                || !owner.ResolvesErlangBif(remoteNode) || !owner.ResolvesErlangBif(localNode)
                || !ReturnsMatchedValue(stateTuple, rebuilt)
                || DiscardedCall(action) is not { Arguments: [Expr.Variable first, Expr.Variable second, Expr.Call prepare] } effect
                || prepare.Arguments is not [Expr.Variable eventValue] || input.Name != eventValue.Name
                || Array.IndexOf(stateFields, first.Name) is not (>= 0 and var firstField)
                || Array.IndexOf(stateFields, second.Name) is not (>= 0 and var secondField)
                || firstField >= secondField
                || !TryResolveOrdinaryCallTarget(prepare, out string? prepareModule)
                || !TryResolveOrdinaryCallTarget(effect, out string? effectModule))
            {
                return false;
            }

            int guardedFailure = Label();
            int unchanged = Label();
            owner.EmitTupleCheck(0, guardFields.Length, guardedFailure);
            owner.EmitTuplePart(0, endpoint, 2);
            owner.Op(BifOne, LabelTag, guardedFailure);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Node, 1));
            owner.Operand(XRegisterTag, 2);
            owner.Operand(XRegisterTag, 2);
            owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Node, 0));
            owner.Operand(XRegisterTag, 3);
            owner.Op(ExactEqualTest, LabelTag, unchanged);
            owner.EmitTypedRegister(2, AtomTypeBits);
            owner.EmitTypedRegister(3, AtomTypeBits);

            Mark(guardedFailure);
            owner.EmitTupleCheck(1, stateFields.Length, unchanged);
            EmitFrameAllocation(2, 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitCallLine(prepare, location);
            Call(prepareModule, prepare.Function, 1);
            EmitSavedTuplePart(
                0,
                firstField,
                XRegisterTag,
                1
            );
            EmitSavedTuplePart(
                0,
                secondField,
                XRegisterTag,
                2
            );
            owner.EmitOrderedTailMoves([(0, 1), (1, 2), (2, 0)], []);
            owner.EmitCallLine(effect, CallLocation(prepare, location));
            Call(effectModule, effect.Function, 3);
            EmitStateResult(
                tag,
                YRegisterTag,
                0,
                0
            );
            owner.EmitFrameReturn(1);

            Mark(unchanged);
            EmitStateResult(
                tag,
                XRegisterTag,
                1,
                2
            );
            owner.instructions.WriteByte(Return);

            return true;

            static string[]? ReadFields(Pattern.Tuple tuple, Pattern.Variable parameter)
            {
                if (!DistinctBindings(parameter) || !tuple.Items.All(field => field is Pattern.Variable)
                    || !DistinctBindings(tuple.Items.Cast<Pattern.Variable>().Append(parameter)
                        .Where(binding => binding.Name != VariableScopeNames.Wildcard).ToArray()))
                {
                    return null;
                }

                return tuple.Items.Cast<Pattern.Variable>().Select(field => field.Name).ToArray();
            }
        }

        private void EmitSavedTuplePart(
            int slot,
            int field,
            int destinationTag,
            int destination
        )
        {
            owner.Op(GetTupleElement, YRegisterTag, slot);
            owner.Operand(BeamWire.UnsignedTag, field);
            owner.Operand(destinationTag, destination);
        }
    }
}
