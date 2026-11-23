using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedRecordCallBranches(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Tuple outer, AliasPattern firstState],
                    Body: Expr.Sequence
                    {
                        Items: [
                        Expr.Match { Pattern: Pattern.Variable firstResult, Value: Expr.Call first },
                        Expr.Match { Pattern: Pattern.Variable secondResult, Value: Expr.Call second },
                        Expr.Call matched, Expr.Tuple matchedReply
                    ]
                    }
                },
                {
                    Guard: null, Patterns: [AliasPattern otherEvent, AliasPattern otherState],
                    Body: Expr.Sequence { Items: [Expr.Call fallback, Expr.Tuple fallbackReply] }
                }
            ] || !TryReadPatternAlias(firstState, out var state, out var stateShape)
                || stateShape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom stateTag }, ..] } stateFields
                || !TryReadPatternAlias(otherState, out var remainingState, out var remainingShape)
                || !IsUnboundTaggedTuple(remainingShape, stateFields.Items.Count, stateTag)
                || !TryReadPatternAlias(otherEvent, out var eventBinding, out var eventShape)
                || outer.Items is not [Pattern.Literal { Value: Atom outerTag }, ..]
                || !IsUnboundTaggedTuple(eventShape, outer.Items.Count, outerTag)
                || !DistinctBindings(remainingState, eventBinding)
                || first is not { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false, Arguments: [Expr.Variable retained] }
                || second is not { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false, Arguments: [Expr.Variable input, Expr.Variable prior] }
                || input.Name != retained.Name || prior.Name != firstResult.Name
                || matchedReply.Items is not [Expr.Literal { Value: Atom reply }, Expr.Variable recorded]
                || fallbackReply.Items is not [Expr.Literal { Value: Atom otherReply }, Expr.Variable returned]
                || !reply.Equals(otherReply) || recorded.Name != state.Name || returned.Name != remainingState.Name
                || !ReadRecordBranchLog(
                    matched,
                    state,
                    secondResult.Name,
                    stateFields,
                    stateTag,
                    out int config,
                    out var matchFormat
                )
                || !ReadRecordBranchLog(
                    fallback,
                    remainingState,
                    eventBinding.Name,
                    stateFields,
                    stateTag,
                    out int fallbackConfig,
                    out var otherFormat
                )
                || config != fallbackConfig)
            {
                return false;
            }

            int field = -1;
            for (int index = 1; index < stateFields.Items.Count; index++)
            {
                if (stateFields.Items[index] is Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    continue;
                }

                if (field >= 0 || stateFields.Items[index] is not Pattern.Variable variable
                    || variable.Name != retained.Name || !DistinctBindings(
                        state,
                        variable,
                        firstResult,
                        secondResult
                    ))
                {
                    return false;
                }

                field = index;
            }

            int selected = -1;
            Pattern.Tuple? nested = null;
            for (int index = 1; index < outer.Items.Count; index++)
            {
                if (outer.Items[index] is Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    continue;
                }

                if (nested is not null || outer.Items[index] is not Pattern.Tuple tuple)
                {
                    return false;
                }

                nested = tuple;
                selected = index;
            }

            if (field < 0 || nested?.Items is not [Pattern.Literal { Value: Atom nestedTag }, ..])
            {
                return false;
            }

            int keyField = -1;
            Atom? key = null;
            for (int index = 1; index < nested.Items.Count; index++)
            {
                if (nested.Items[index] is Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    continue;
                }

                if (key is not null || nested.Items[index] is not Pattern.Literal { Value: Atom constant })
                {
                    return false;
                }

                keyField = index;
                key = constant;
            }

            if (key is null)
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int alternative = Label();
            EmitDetailRecordTest(
                0,
                outer.Items.Count,
                outerTag,
                failure
            );
            owner.EmitTuplePart(0, selected, 2);
            EmitDetailRecordTest(
                2,
                nested.Items.Count,
                nestedTag,
                alternative
            );
            owner.EmitTuplePart(2, keyField, 2);
            owner.EmitAtomTest(2, key.Name, alternative);
            EmitDetailRecordTest(
                1,
                stateFields.Items.Count,
                stateTag,
                failure
            );
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                1
            );
            GetTupleField(
                1,
                field,
                YRegisterTag,
                0
            );
            Load(0);
            var previous = location;
            EmitWindowCall(first, ref previous);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            Load(0);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitWindowCall(second, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitRecordBranchList();
            owner.Op(GetTupleElement, YRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, config);
            owner.Operand(XRegisterTag, 0);
            owner.EmitConstant(matchFormat, 1);
            EmitWindowCall(matched, ref previous);
            EmitRetainedRecordReply([reply]);

            Mark(alternative);
            EmitDetailRecordTest(
                1,
                stateFields.Items.Count,
                stateTag,
                failure
            );
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            EmitRecordBranchList();
            owner.EmitTuplePart(1, fallbackConfig, 0);
            owner.EmitConstant(otherFormat, 1);
            previous = location;
            EmitWindowCall(fallback, ref previous);
            EmitRetainedRecordReply([reply]);

            return true;
        }

        private static bool IsUnboundTaggedTuple(Pattern pattern, int arity, Atom tag)
            => pattern is Pattern.Tuple { Items: [Pattern.Literal { Value: Atom actual }, ..] } tuple
               && tuple.Items.Count == arity && tag.Equals(actual)
               && tuple.Items.Skip(1).All(item => item is Pattern.Variable { Name: VariableScopeNames.Wildcard });

        private static bool ReadRecordBranchLog(
            Expr.Call call,
            Pattern.Variable state,
            string payload,
            Pattern.Tuple shape,
            Atom tag,
            out int field,
            out Term format
        )
        {
            if (call is
                {
                    Module: not (null or ErlangModuleNames.Module), DynamicRemote: false, Arguments: [
                Expr.RecordAccess { Value: Expr.Variable record } access,
                Expr.Literal literal,
                Expr.List { Tail: null, Items: [Expr.Variable argument] }
            ]
                } && record.Name == state.Name && access.Name == tag.Name
                && access.Arity == shape.Items.Count && access.Index > 0 && access.Index < access.Arity
                && argument.Name == payload)
            {
                field = access.Index;
                format = literal.Value;

                return true;
            }

            field = default;
            format = Nil.Value;

            return false;
        }

        private void EmitRecordBranchList()
        {
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(AtomTag, 0);
            owner.Operand(XRegisterTag, 2);
        }
    }
}
