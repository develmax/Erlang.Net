using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleRemoteDispatch(FunctionDefinition function)
        {
            if (function.Arity is not (2 or 3) || function.Clauses is not
                [{ Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable key, ..] } clause]
                || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            Expr? dispatch = clause.Body;
            Expr.Case? projection = null;
            if (clause.Body is Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.Case routed },
                        Expr.Case { Value: Expr.Variable used } result
                    ]
                } && binding.Name == used.Name && DistinctBindings(input, key, binding) && function.Arity == 2)
            {
                dispatch = routed;
                projection = result;
            }

            if (dispatch is not Expr.Case
                {
                    Value: Expr.Variable source,
                    Clauses:
                    [
                        { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Variable node, Pattern.Variable table] }],
                            Body: Expr.Call { Module: not null, DynamicRemote: false, Arguments:
                            [Expr.Variable destination, Expr.Literal { Value: Atom module },
                                Expr.Literal { Value: Atom operation }, Expr.List { Items: [Expr.Variable target, var remoteValue], Tail: null }] } remote },
                        { Guard: null, Patterns: [Pattern.Variable plain],
                            Body: Expr.Call { Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Variable localTarget, var localValue] } local }
                    ]
                } || source.Name != input.Name || !DistinctBindings(input, key, node, table)
                || !DistinctBindings(input, key, plain)
                || destination.Name != node.Name || target.Name != table.Name || localTarget.Name != plain.Name
                || module.Name != local.Module || operation.Name != local.Function)
            {
                return false;
            }

            bool pair = function.Arity == 3;
            if (pair
                ? clause.Patterns[2] is not Pattern.Variable value || !DistinctBindings(input, key, node, table, value)
                    || !DistinctBindings(input, key, plain, value)
                    || !IsKeyValuePair(localValue, value) || !IsKeyValuePair(remoteValue, value)
                : localValue is not Expr.Variable localKey || localKey.Name != key.Name
                    || remoteValue is not Expr.Variable remoteKey || remoteKey.Name != key.Name)
            {
                return false;
            }

            Atom? success = null;
            Term? missing = null;
            if (projection is not null)
            {
                if (projection.Clauses is not
                    [
                        { Guard: null, Patterns: [Pattern.List { Items:
                            [Pattern.Tuple { Items: [Pattern.Variable matchKey, Pattern.Variable matchValue] }], Tail: null }],
                            Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable returned] } },
                        { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                            Body: Expr.Literal miss }
                    ] || matchKey.Name != key.Name || matchValue.Name != returned.Name
                    || !DistinctBindings(input, key, matchValue)
                    || matchValue.Name == node.Name || matchValue.Name == table.Name || matchValue.Name == plain.Name
                    || projection.Value is Expr.Variable bound
                        && (bound.Name == matchValue.Name || bound.Name == node.Name
                            || bound.Name == table.Name || bound.Name == plain.Name))
                {
                    return false;
                }

                success = tag;
                missing = miss.Value;
            }

            if (projection is not null)
            {
                EmitFrameAllocation(2, 1);
                owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 0);
            }

            int fallback = Label();
            EmitTupleShapeTest(XRegisterTag, 0, 2, fallback);
            Heap(2 * ConsHeapWords + (pair ? 3 : 0), projection is null ? function.Arity : 1);
            int tableRegister = projection is null ? function.Arity : 1;
            int listRegister = projection is null ? 1 : 2;
            owner.EmitTuplePart(0, 1, tableRegister);
            if (pair)
            {
                owner.EmitTagMatchTuple(1, (XRegisterTag, 1), (XRegisterTag, 2));
            }

            owner.Op(PutList, projection is null ? XRegisterTag : YRegisterTag, projection is null ? 1 : 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, listRegister);
            owner.Op(PutList, XRegisterTag, tableRegister);
            owner.Operand(XRegisterTag, listRegister);
            owner.Operand(XRegisterTag, 3);
            owner.EmitTuplePart(0, 0, 0);
            owner.EmitConstant(operation, 2);
            owner.EmitConstant(module, 1);
            EmitCall(remote);
            int join = 0;
            if (projection is not null)
            {
                join = Label();
                JumpTo(join);
            }

            Mark(fallback);
            if (pair)
            {
                Heap(3, function.Arity);
                owner.EmitTagMatchTuple(1, (XRegisterTag, 1), (XRegisterTag, 2));
            }
            EmitCall(local);

            if (projection is not null)
            {
                Mark(join);
                int failedProjection = Label();
                owner.EmitNonemptyList(0, failedProjection);
                owner.EmitListParts(0, XRegisterTag, 1, XRegisterTag, 0);
                EmitTupleShapeTest(XRegisterTag, 1, 2, failedProjection);
                owner.EmitNilTest(XRegisterTag, 0, failedProjection);
                owner.EmitTuplePart(1, 0, 0);
                owner.Op(ExactEqualTest, LabelTag, failedProjection);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(YRegisterTag, 0);
                Heap(3, 2);
                owner.EmitTuplePart(1, 1, 0);
                owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(success!.Name)), (XRegisterTag, 0));
                owner.EmitFrameReturn(1);
                Mark(failedProjection);
                owner.EmitConstant(missing!, 0);
                owner.EmitFrameReturn(1);
            }

            return true;

            bool IsKeyValuePair(Expr expression, Pattern.Variable value) =>
                expression is Expr.Tuple { Items: [Expr.Variable tupleKey, Expr.Variable tupleValue] }
                && tupleKey.Name == key.Name && tupleValue.Name == value.Name;

            void Heap(int words, int live)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, words);
                owner.Operand(BeamWire.UnsignedTag, live);
            }

            void EmitCall(Expr.Call call)
            {
                owner.EmitCallLine(call, location);
                owner.Op(projection is null ? TailExternalCall : ExternalCall, BeamWire.UnsignedTag, call.Arguments.Count);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module!, call.Function, call.Arguments.Count));
            }
        }
    }
}
