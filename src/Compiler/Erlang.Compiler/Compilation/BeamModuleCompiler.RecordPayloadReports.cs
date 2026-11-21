using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRecordPayloadReports(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses.Count < 4
                || function.Clauses[^1] is not
                {
                    Guard: null,
                    Patterns: [AliasPattern outerAlias, AliasPattern stateAlias],
                    Body: Expr.Sequence { Items: [Expr.Call report, var reply] }
                }
                || !TryReadPatternAlias(outerAlias, out var eventValue, out var eventShape)
                || eventShape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom outerTag }, ..] } outer
                || !IsUnboundTaggedTuple(outer, outer.Items.Count, outerTag)
                || !TryReadPatternAlias(stateAlias, out var state, out var stateShape)
                || stateShape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom stateTag }, ..] } stateTuple
                || !IsUnboundTaggedTuple(stateTuple, stateTuple.Items.Count, stateTag)
                || !DistinctBindings(eventValue, state)
                || ReadGeometryReply(reply, state.Name) is not { } result
                || !ReadRecordBranchLog(
                    report,
                    state,
                    eventValue.Name,
                    stateTuple,
                    stateTag,
                    out int defaultConfig,
                    out var defaultFormat
                ))
            {
                return false;
            }

            var arms = new List<RecordPayloadReport>();
            int? outerField = null;
            foreach (Clause clause in function.Clauses.SkipLast(1))
            {
                if (ReadRecordPayloadReport(
                    clause,
                    outer,
                    outerTag,
                    stateTuple,
                    stateTag,
                    result
                ) is not { } arm || outerField.HasValue && outerField != arm.OuterField)
                {
                    return false;
                }

                outerField = arm.OuterField;
                arms.Add(arm);
            }

            var atomOrder = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var groups = arms.GroupBy(arm => arm.Arity).OrderByDescending(group => group.Key)
                .Select(group => (Arity: group.Key, Tags: group.GroupBy(arm => arm.Tag)
                    .OrderByDescending(tagGroup => tagGroup.Key, atomOrder).ToArray()))
                .ToArray();
            foreach (var group in groups)
            {
                foreach (var sameTag in group.Tags)
                {
                    var taggedArms = sameTag.ToArray();
                    if (taggedArms.Length > 1 && (taggedArms.Any(arm => arm.Kind is null)
                        || taggedArms.Select(arm => arm.KindField).Distinct().Count() != 1
                        || taggedArms.Select(arm => arm.Kind).Distinct().Count() != taggedArms.Length))
                    {
                        return false;
                    }
                }
            }

            if (groups.Length == 1 && groups[0].Tags.Length < 2)
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            var arityLabels = new Dictionary<int, int>();
            var tagLabels = new Dictionary<(int Arity, Atom Tag), int>();
            var kindLabels = new Dictionary<RecordPayloadReport, int>();
            foreach (var group in groups)
            {
                if (groups.Length > 1)
                {
                    arityLabels.Add(group.Arity, Label());
                }

                foreach (var tagGroup in group.Tags)
                {
                    if (group.Tags.Length > 1)
                    {
                        tagLabels.Add((group.Arity, tagGroup.Key), Label());
                    }

                    if (tagGroup.Count() > 1)
                    {
                        foreach (var arm in tagGroup.OrderByDescending(item => item.Kind!, atomOrder))
                        {
                            kindLabels.Add(arm, Label());
                        }
                    }
                }
            }

            int fallback = Label();
            EmitDetailRecordTest(
                0,
                outer.Items.Count,
                outerTag,
                failure
            );
            owner.EmitTuplePart(0, outerField!.Value, 2);
            owner.Op(IsTuple, LabelTag, fallback);
            owner.Operand(XRegisterTag, 2);
            if (groups.Length == 1)
            {
                owner.Op(TestArity, LabelTag, fallback);
                owner.Operand(XRegisterTag, 2);
                owner.Operand(BeamWire.UnsignedTag, groups[0].Arity);
            }
            else
            {
                owner.instructions.WriteByte(SelectTupleArity);
                owner.EmitTypedRegister(2, TupleTypeBits);
                owner.Operand(LabelTag, fallback);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 2 * groups.Length);
                foreach (var group in groups.Reverse())
                {
                    owner.Operand(BeamWire.UnsignedTag, group.Arity);
                    owner.Operand(LabelTag, arityLabels[group.Arity]);
                }
            }

            foreach (var group in groups)
            {
                if (arityLabels.TryGetValue(group.Arity, out int arityLabel))
                {
                    Mark(arityLabel);
                }

                owner.EmitTuplePart(2, 0, 3);
                var payloads = group.Tags.SelectMany(tagGroup => tagGroup).Select(arm => arm.PayloadField).Distinct().ToArray();
                bool shared = group.Tags.Length > 1 && payloads is [>= 1]
                    && group.Tags.SelectMany(tagGroup => tagGroup).All(arm => arm.Fetch is null);
                if (shared)
                {
                    owner.EmitTuplePart(2, payloads[0], 4);
                }

                if (group.Tags.Length == 1)
                {
                    owner.EmitAtomTest(3, group.Tags[0].Key.Name, fallback);
                }
                else
                {
                    EmitReportSwitch(3, group.Tags.Reverse()
                        .Select(tagGroup => (tagGroup.Key, tagLabels[(group.Arity, tagGroup.Key)])).ToArray());
                }

                foreach (var tagGroup in group.Tags)
                {
                    if (tagLabels.TryGetValue((group.Arity, tagGroup.Key), out int tagLabel))
                    {
                        Mark(tagLabel);
                    }

                    var variants = tagGroup.ToArray();
                    if (variants.Length == 1)
                    {
                        var arm = variants[0];
                        if (arm.Kind is { } kind)
                        {
                            int register = shared ? 2 : 3;
                            owner.EmitTuplePart(2, arm.KindField, register);
                            owner.EmitAtomTest(register, kind.Name, fallback);
                        }

                        EmitArm(arm, shared);
                    }
                    else
                    {
                        int register = shared ? 2 : 3;
                        owner.EmitTuplePart(2, variants[0].KindField, register);
                        var inKeyOrder = variants.OrderBy(arm => arm.Kind!, atomOrder).ToArray();
                        EmitReportSwitch(register, inKeyOrder.Select(arm => (arm.Kind!, kindLabels[arm])).ToArray());
                        foreach (var arm in inKeyOrder.Reverse())
                        {
                            Mark(kindLabels[arm]);
                            EmitArm(arm, shared);
                        }
                    }
                }
            }

            Mark(fallback);
            EmitDetailRecordTest(
                1,
                stateTuple.Items.Count,
                stateTag,
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
            owner.EmitTuplePart(1, defaultConfig, 0);
            owner.EmitConstant(defaultFormat, 1);
            var previous = location;
            EmitWindowCall(report, ref previous);
            EmitRetainedRecordReply([result]);

            return true;

            void EmitArm(RecordPayloadReport arm, bool shared)
            {
                EmitDetailRecordTest(
                    1,
                    stateTuple.Items.Count,
                    stateTag,
                    failure
                );
                EmitFrameAllocation(arm.Fetch is not null ? 2 : shared ? 5 : 3, 1,
                    heapWords: arm.Fetch is null ? ConsHeapWords : 0);
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    0
                );
                var prior = location;
                if (arm.Fetch is { } fetch)
                {
                    owner.EmitTuplePart(0, arm.FetchField, 0);
                    EmitWindowCall(fetch, ref prior);
                    owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                    owner.Operand(BeamWire.UnsignedTag, 1);
                }
                else if (!shared)
                {
                    owner.EmitTuplePart(2, arm.PayloadField, 0);
                }

                owner.Op(PutList, XRegisterTag, shared ? 4 : 0);
                owner.LiteralOperand(Nil.Value);
                owner.Operand(XRegisterTag, 2);
                owner.EmitTuplePart(
                    arm.Fetch is not null ? 0 : 1,
                    arm.ConfigField,
                    0,
                    arm.Fetch is not null ? YRegisterTag : XRegisterTag
                );
                owner.EmitConstant(arm.Format, 1);
                EmitWindowCall(arm.Report, ref prior);
                EmitRetainedRecordReply([result]);
            }

            void EmitReportSwitch(int register, (Atom Atom, int Label)[] entries)
            {
                owner.Op(SelectValue, XRegisterTag, register);
                owner.Operand(LabelTag, fallback);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, entries.Length * 2);
                foreach (var entry in entries)
                {
                    owner.LiteralOperand(entry.Atom);
                    owner.Operand(LabelTag, entry.Label);
                }
            }
        }

        private static RecordPayloadReport? ReadRecordPayloadReport(
            Clause clause,
            Pattern.Tuple outer,
            Atom outerTag,
            Pattern.Tuple stateShape,
            Atom stateTag,
            Atom result
        )
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } eventPattern, AliasPattern alias],
                    Body: Expr.Sequence events
                }
                || !tag.Equals(outerTag) || eventPattern.Items.Count != outer.Items.Count
                || !TryReadPatternAlias(alias, out var state, out var matchedState)
                || !IsUnboundTaggedTuple(matchedState, stateShape.Items.Count, stateTag)
                || events.Items.Count is not (2 or 3)
                || events.Items[^2] is not Expr.Call report
                || ReadGeometryReply(events.Items[^1], state.Name) is not { } reply || !reply.Equals(result))
            {
                return null;
            }

            Pattern.Tuple? nested = null;
            int outerField = -1;
            int fetchField = -1;
            Pattern.Variable? fetchInput = null;
            for (int index = 1; index < eventPattern.Items.Count; index++)
            {
                switch (eventPattern.Items[index])
                {
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    case Pattern.Tuple inner when nested is null:
                        nested = inner;
                        outerField = index;
                        break;
                    case Pattern.Variable variable when fetchInput is null:
                        fetchInput = variable;
                        fetchField = index;
                        break;
                    default:
                        return null;
                }
            }

            if (nested?.Items is not [Pattern.Literal { Value: Atom nestedTag }, ..])
            {
                return null;
            }

            int kindField = -1;
            Atom? kind = null;
            int payloadField = -1;
            Pattern.Variable? payload = null;
            for (int index = 1; index < nested.Items.Count; index++)
            {
                switch (nested.Items[index])
                {
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    case Pattern.Literal { Value: Atom discriminator } when kind is null:
                        kind = discriminator;
                        kindField = index;
                        break;
                    case Pattern.Variable variable when payload is null:
                        payload = variable;
                        payloadField = index;
                        break;
                    default:
                        return null;
                }
            }

            Expr.Call? fetch = null;
            string payloadName;
            if (events.Items.Count == 3)
            {
                if (fetchInput is null || payload is not null
                    || events.Items[0] is not Expr.Match
                    {
                        Pattern: Pattern.Variable readValue,
                        Value: Expr.Call
                        {
                            Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                            Arguments: [Expr.Variable readSource]
                        } call
                    }
                    || !DistinctBindings(state, fetchInput, readValue)
                    || readSource.Name != fetchInput.Name)
                {
                    return null;
                }

                payloadName = readValue.Name;
                fetch = call;
            }
            else
            {
                if (fetchInput is not null || payload is null || !DistinctBindings(state, payload))
                {
                    return null;
                }

                payloadName = payload.Name;
            }

            return ReadRecordBranchLog(
                report,
                state,
                payloadName,
                stateShape,
                stateTag,
                out int config,
                out var format
            ) ? new(
                outerField,
                nested.Items.Count,
                nestedTag,
                kindField,
                kind,
                payloadField,
                fetchField,
                fetch,
                report,
                config,
                format
            ) : null;
        }

        private sealed record RecordPayloadReport(
            int OuterField,
            int Arity,
            Atom Tag,
            int KindField,
            Atom? Kind,
            int PayloadField,
            int FetchField,
            Expr.Call? Fetch,
            Expr.Call Report,
            int ConfigField,
            Term Format
        );
    }
}
