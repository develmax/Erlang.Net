using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedRecordReadReport(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom outerTag }, ..] } outer, AliasPattern alias],
                        Body: Expr.Sequence
                        {
                            Items: [Expr.Match { Pattern: Pattern.Variable value, Value: Expr.Call read }, Expr.Call format, var result]
                        }
                    }
                ]
                || !ReadEventState(alias, out var state, out var fields)
                || fields is not [Pattern.Literal { Value: Atom stateTag }, ..]
                || fields.Skip(1).Any(pattern => pattern is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                || !DistinctBindings(value, state) || ReadRetainedEventResult(result, state.Name) is not { } reply
                || read is not
                {
                    Module: not null, DynamicRemote: false, Arguments: [Expr.RecordAccess { Value: Expr.Variable readState } source, _, ..]
                }
                || format is not
                {
                    Module: not null, DynamicRemote: false,
                    Arguments: [Expr.RecordAccess { Value: Expr.Variable formatState } config, var text, Expr.List { Tail: null } data]
                }
                || read.Arguments.Count >= FirstReservedXRegister
                || !TryResolveOrdinaryCallTarget(read, out _) || !TryResolveOrdinaryCallTarget(format, out _)
                || readState.Name != state.Name || formatState.Name != state.Name
                || source.Name != stateTag.Name || config.Name != stateTag.Name
                || source.Arity != fields.Count || config.Arity != fields.Count
                || !AttributeLiteral.TryNormalizeConstant(text, out Term message)
                || data.Items.Count != read.Arguments.Count
                || data.Items[^1] is not Expr.Variable reported || reported.Name != value.Name)
            {
                return false;
            }

            int nestedIndex = -1;
            Pattern.Tuple? nested = null;
            for (int index = 1; index < outer.Items.Count; index++)
            {
                if (outer.Items[index] is Pattern.Tuple tuple && nested is null)
                {
                    nestedIndex = index;
                    nested = tuple;
                }
                else if (outer.Items[index] is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    return false;
                }
            }

            if (nested is not { Items: [Pattern.Literal { Value: Atom eventTag }, Pattern.Literal { Value: Atom selector }, ..] })
            {
                return false;
            }

            var scope = new HashSet<string>(StringComparer.Ordinal) { state.Name, value.Name };
            var captured = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 2; index < nested.Items.Count; index++)
            {
                if (nested.Items[index] is not Pattern.Variable binding)
                {
                    return false;
                }

                if (binding.Name != VariableScopeNames.Wildcard)
                {
                    if (!scope.Add(binding.Name))
                    {
                        return false;
                    }

                    captured.Add(binding.Name, index);
                }
            }

            int dimension = read.Arguments.Count - 1;
            if (captured.Count != dimension)
            {
                return false;
            }

            var projections = new int[dimension];
            for (int index = 0; index < dimension; index++)
            {
                if (read.Arguments[index + 1] is not Expr.Variable field
                    || data.Items[index] is not Expr.Variable log || field.Name != log.Name
                    || !captured.Remove(field.Name, out projections[index]))
                {
                    return false;
                }
            }

            int failure = owner.nextLabel - 2;
            EmitDetailRecordTest(
                0,
                outer.Items.Count,
                outerTag,
                failure
            );
            owner.EmitTuplePart(0, nestedIndex, 2);
            EmitDetailRecordTest(
                2,
                nested.Items.Count,
                eventTag,
                failure
            );
            owner.EmitTuplePart(2, 1, 3);
            owner.EmitAtomTest(3, selector.Name, failure);
            EmitDetailRecordTest(
                1,
                fields.Count,
                stateTag,
                failure
            );
            EmitFrameAllocation(3, dimension + 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                dimension
            );
            owner.EmitTuplePart(1, source.Index, 0);
            for (int index = 0; index < dimension; index++)
            {
                GetTupleField(
                    2,
                    projections[index],
                    YRegisterTag,
                    dimension - 1 - index
                );
            }

            for (int index = 0; index < dimension; index++)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    index,
                    XRegisterTag,
                    dimension - index
                );
            }

            owner.EmitCallLine(read, location);
            owner.EmitStaticRemoteCall(read);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords * (dimension + 1));
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 0);
            for (int index = 0; index < dimension; index++)
            {
                owner.Op(PutList, YRegisterTag, index);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, index == dimension - 1 ? 2 : 0);
            }

            EmitSavedRecordField(dimension, config.Index, 0);
            owner.EmitConstant(message, 1);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, dimension);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitCallLine(format, CallLocation(read, location));
            owner.EmitStaticRemoteCall(format);
            EmitRetainedRecordReply([reply]);

            return true;
        }
    }
}
