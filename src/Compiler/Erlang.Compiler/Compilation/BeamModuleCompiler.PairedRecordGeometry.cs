using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitPairedRecordGeometry(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not [var drag, var resize, var fallback]
                || ReadGeometryEvent(drag, 4) is not { } dragEvent
                || ReadGeometryEvent(resize, 2) is not { } resizeEvent
                || dragEvent.OuterArity != resizeEvent.OuterArity || !dragEvent.OuterTag.Equals(resizeEvent.OuterTag)
                || dragEvent.OuterField != resizeEvent.OuterField || dragEvent.EventArity <= resizeEvent.EventArity
                || !dragEvent.StateTag.Equals(resizeEvent.StateTag) || dragEvent.StateArity != resizeEvent.StateArity
                || ReadGeometryDrag(drag.Body, dragEvent) is not { } dragPlan
                || ReadGeometryResize(resize.Body, resizeEvent) is not { } resizePlan
                || !dragPlan.Reply.Equals(resizePlan.Reply)
                || fallback is not
                {
                    Guard: null,
                    Patterns: [AliasPattern otherEvent, AliasPattern otherState],
                    Body: Expr.Sequence { Items: [Expr.Call report, var reply] }
                }
                || !TryReadPatternAlias(otherEvent, out var eventBinding, out var eventShape)
                || !IsUnboundTaggedTuple(eventShape, dragEvent.OuterArity, dragEvent.OuterTag)
                || !TryReadPatternAlias(otherState, out var stateBinding, out var stateShape)
                || stateShape is not Pattern.Tuple stateTuple
                || !IsUnboundTaggedTuple(stateTuple, dragEvent.StateArity, dragEvent.StateTag)
                || !DistinctBindings(eventBinding, stateBinding)
                || ReadGeometryReply(reply, stateBinding.Name) is not { } fallbackReply || !fallbackReply.Equals(dragPlan.Reply)
                || !ReadRecordBranchLog(
                    report,
                    stateBinding,
                    eventBinding.Name,
                    stateTuple,
                    dragEvent.StateTag,
                    out int config,
                    out var format
                ))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int dragBranch = Label();
            int resizeBranch = Label();
            int fallbackBranch = Label();
            int badPosition = Label();
            int badSize = Label();
            EmitDetailRecordTest(
                0,
                dragEvent.OuterArity,
                dragEvent.OuterTag,
                failure
            );
            owner.EmitTuplePart(0, dragEvent.OuterField, 2);
            owner.Op(IsTuple, LabelTag, fallbackBranch);
            owner.Operand(XRegisterTag, 2);
            owner.instructions.WriteByte(SelectTupleArity);
            owner.EmitTypedRegister(2, TupleTypeBits);
            owner.Operand(LabelTag, fallbackBranch);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, resizeEvent.EventArity);
            owner.Operand(LabelTag, resizeBranch);
            owner.Operand(BeamWire.UnsignedTag, dragEvent.EventArity);
            owner.Operand(LabelTag, dragBranch);

            owner.frameEntryTupleReads.Add((dragBranch, 2, 1, 3));
            Mark(dragBranch);
            PrepareGeometry(dragEvent, fallbackBranch);
            owner.EmitTuplePart(2, 1, 3);
            EmitDetailRecordTest(
                1,
                dragEvent.StateArity,
                dragEvent.StateTag,
                failure
            );
            EmitGeometryDrag(dragPlan, badPosition, badSize);

            Mark(resizeBranch);
            PrepareGeometry(resizeEvent, fallbackBranch);
            EmitDetailRecordTest(
                1,
                resizeEvent.StateArity,
                resizeEvent.StateTag,
                failure
            );
            EmitGeometryResize(resizePlan);

            Mark(fallbackBranch);
            EmitDetailRecordTest(
                1,
                dragEvent.StateArity,
                dragEvent.StateTag,
                failure
            );
            EmitFrameAllocation(2, 1, heapWords: ConsHeapWords);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            EmitRecordBranchList();
            owner.EmitTuplePart(1, config, 0);
            owner.EmitConstant(format, 1);
            var previous = location;
            EmitWindowCall(report, ref previous);
            EmitRetainedRecordReply([fallbackReply]);

            Mark(badPosition);
            owner.EmitLine(CallLocation(dragPlan.Position, location));
            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(badSize);
            owner.EmitLine(CallLocation(dragPlan.Size, location));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private void PrepareGeometry(GeometryEvent source, int failure)
        {
            owner.EmitTuplePart(2, 0, 3);
            owner.EmitAtomTest(3, source.EventTag.Name, failure);
            owner.EmitTuplePart(2, source.EventField, 2);
            owner.EmitTupleCheck(2, source.Values.Length, failure);
        }

        private static GeometryEvent? ReadGeometryEvent(Clause clause, int fields)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom outerTag }, ..] } outer, AliasPattern alias]
                }
                || !TryReadPatternAlias(alias, out var state, out var shape)
                || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom stateTag }, ..] } stateTuple
                || ReadSoleNestedTuple(outer) is not { Nested: var nested, Index: var outerIndex }
                || nested.Items is not [Pattern.Literal { Value: Atom eventTag }, ..]
                || ReadSoleNestedTuple(nested) is not { Nested: var values, Index: var eventIndex }
                || values.Items.Count != fields || values.Items.Any(item => item is not Erlang.Pattern.Variable))
            {
                return null;
            }

            var retained = new Dictionary<string, int>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal) { state.Name };
            if (state.Name == VariableScopeNames.Wildcard)
            {
                return null;
            }

            for (int index = 1; index < stateTuple.Items.Count; index++)
            {
                if (stateTuple.Items[index] is not Pattern.Variable variable)
                {
                    return null;
                }
                if (variable.Name != VariableScopeNames.Wildcard)
                {
                    if (!names.Add(variable.Name))
                    {
                        return null;
                    }

                    retained.Add(variable.Name, index);
                }
            }

            var parameters = values.Items.Cast<Pattern.Variable>().Select(item => item.Name).ToArray();
            if (retained.Count != 2 || parameters.Any(value => value != VariableScopeNames.Wildcard && !names.Add(value)))
            {
                return null;
            }

            return new(
                outerTag,
                outer.Items.Count,
                outerIndex,
                eventTag,
                nested.Items.Count,
                eventIndex,
                stateTag,
                stateTuple.Items.Count,
                state.Name,
                parameters,
                retained,
                names
            );
        }

        private static (Pattern.Tuple Nested, int Index)? ReadSoleNestedTuple(Pattern.Tuple tuple)
        {
            (Pattern.Tuple Nested, int Index)? result = null;
            for (int index = 1; index < tuple.Items.Count; index++)
            {
                if (tuple.Items[index] is Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    continue;
                }
                if (result is not null || tuple.Items[index] is not Pattern.Tuple nested)
                {
                    return null;
                }

                result = (nested, index);
            }

            return result;
        }

        private static Atom? ReadGeometryReply(Expr expression, string state) => expression is Expr.Tuple
        {
            Items: [Expr.Literal { Value: Atom tag }, Expr.Variable retained]
        } && retained.Name == state ? tag : null;

        private static int? ReadGeometryStateCall(Expr.Call call, GeometryEvent source)
        {
            if (!IsGeometryCall(call) || call.Arguments is not
                [
                    Expr.RecordAccess { Value: Expr.Variable record } access
                ] || record.Name != source.State || access.Name != source.StateTag.Name || access.Arity != source.StateArity)
            {
                return null;
            }

            return access.Index;
        }

        private static bool IsGeometryCall(Expr.Call call) =>
            call is { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false };

        private sealed record GeometryEvent(
            Atom OuterTag,
            int OuterArity,
            int OuterField,
            Atom EventTag,
            int EventArity,
            int EventField,
            Atom StateTag,
            int StateArity,
            string State,
            string[] Values,
            IReadOnlyDictionary<string, int> Fields,
            IReadOnlySet<string> Bound
        );
    }
}
