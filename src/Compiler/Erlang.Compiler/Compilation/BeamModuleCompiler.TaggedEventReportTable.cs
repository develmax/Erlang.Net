using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record EventReportMessage(Expr.Call Call, Term Text, int Field);

        private sealed record EventReportArm(
            Atom Selector,
            EventReportMessage? Report,
            Expr.Call? Read,
            int Projection,
            Expr.Case? Decision,
            (Integer Key, EventReportMessage Report)[] Choices
        );

        private bool TryEmitTaggedEventReportTable(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses.Count < 3)
            {
                return false;
            }

            Atom? outerTag = null;
            Atom? innerTag = null;
            Atom? stateTag = null;
            Atom? reply = null;
            int outerArity = 0;
            int innerArity = 0;
            int stateArity = 0;
            int nestedIndex = 0;
            int objectIndex = -1;
            int choiceIndex = -1;
            var arms = new List<EventReportArm>();
            foreach (var clause in function.Clauses)
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom outer }, ..] } envelope, AliasPattern statePattern],
                        Body: Expr.Sequence { Items: [_, .., var response] } body
                    }
                    || !ReadEventState(statePattern, out var state, out var fields)
                    || fields is not [Pattern.Literal { Value: Atom stateType }, ..]
                    || ReadRetainedEventResult(response, state.Name) is not { } responseType)
                {
                    return false;
                }

                var outerFields = new Dictionary<string, int>(StringComparer.Ordinal);
                var innerFields = new Dictionary<string, int>(StringComparer.Ordinal);
                var stateFields = new Dictionary<string, int>(StringComparer.Ordinal);
                var lexical = new HashSet<string>(StringComparer.Ordinal) { state.Name };
                if (state.Name == VariableScopeNames.Wildcard || !AddFields(fields, stateFields, first: 1))
                {
                    return false;
                }

                int eventIndex = -1;
                Pattern.Tuple? nested = null;
                for (int index = 1; index < envelope.Items.Count; index++)
                {
                    if (envelope.Items[index] is Pattern.Tuple record && nested is null)
                    {
                        nested = record;
                        eventIndex = index;
                    }
                    else if (!AddField(envelope.Items[index], outerFields, index))
                    {
                        return false;
                    }
                }

                if (nested is not { Items: [Pattern.Literal { Value: Atom inner }, Pattern.Literal { Value: Atom selector }, ..] }
                    || !AddFields(nested.Items, innerFields, first: 2)
                    || arms.Any(arm => arm.Selector.Equals(selector)))
                {
                    return false;
                }

                if (outerTag is null)
                {
                    outerTag = outer;
                    innerTag = inner;
                    stateTag = stateType;
                    reply = responseType;
                    outerArity = envelope.Items.Count;
                    innerArity = nested.Items.Count;
                    stateArity = fields.Count;
                    nestedIndex = eventIndex;
                }
                else if (!outerTag.Equals(outer) || !innerTag!.Equals(inner) || !stateTag!.Equals(stateType)
                    || !reply!.Equals(responseType) || outerArity != envelope.Items.Count
                    || innerArity != nested.Items.Count || stateArity != fields.Count || nestedIndex != eventIndex)
                {
                    return false;
                }

                if (body.Items is [var output, _] && outerFields.Count == 0 && innerFields.Count == 1
                    && stateFields.Count == 0 && ReadReport(output, innerFields.Single().Key) is { } direct)
                {
                    arms.Add(new(
                        selector,
                        direct,
                        null,
                        innerFields.Single().Value,
                        null,
                        []
                    ));
                    continue;
                }

                if (body.Items is not
                    [
                        Expr.Match
                    {
                        Pattern: Pattern.Variable value,
                        Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable source] } read
                    },
                        var statement,
                        _
                    ]
                    || value.Name == VariableScopeNames.Wildcard || !lexical.Add(value.Name)
                    || outerFields.Count != 1 || !outerFields.TryGetValue(source.Name, out int sourceIndex)
                    || objectIndex >= 0 && objectIndex != sourceIndex)
                {
                    return false;
                }

                objectIndex = sourceIndex;
                if (innerFields.Count == 0 && stateFields.Count == 0 && ReadReport(statement, value.Name) is { } projected)
                {
                    arms.Add(new(
                        selector,
                        projected,
                        read,
                        0,
                        null,
                        []
                    ));
                    continue;
                }

                if (statement is not Expr.Case { Value: Expr.Variable choice } decision
                    || stateFields.Count != 1 || innerFields.Count != 1
                    || !innerFields.TryGetValue(choice.Name, out int selectorIndex)
                    || choiceIndex >= 0 && choiceIndex != selectorIndex || decision.Clauses.Count < 2)
                {
                    return false;
                }

                choiceIndex = selectorIndex;
                var cases = new List<(Integer Key, EventReportMessage Report)>();
                foreach (var branch in decision.Clauses)
                {
                    if (branch is not { Guard: null, Patterns: [Pattern.Literal { Value: Integer key }] }
                        || cases.Any(item => item.Key.Equals(key))
                        || ReadReport(branch.Body, value.Name) is not { } message)
                    {
                        return false;
                    }

                    cases.Add((key, message));
                }

                arms.Add(new(
                    selector,
                    null,
                    read,
                    stateFields.Single().Value,
                    decision,
                    cases.ToArray()
                ));

                bool AddFields(IReadOnlyList<Pattern> patterns, Dictionary<string, int> bindings, int first)
                {
                    for (int index = first; index < patterns.Count; index++)
                    {
                        if (!AddField(patterns[index], bindings, index))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                bool AddField(Pattern pattern, Dictionary<string, int> bindings, int index) =>
                    pattern is Pattern.Variable variable && (variable.Name == VariableScopeNames.Wildcard
                        || lexical.Add(variable.Name) && bindings.TryAdd(variable.Name, index));

                EventReportMessage? ReadReport(Expr expression, string argument)
                {
                    if (expression is not
                        Expr.Call
                        {
                            Module: not null, DynamicRemote: false,
                            Arguments: [var config, var text, Expr.List { Items: [Expr.Variable term], Tail: null }]
                        } reportCall || term.Name != argument || !AttributeLiteral.TryNormalizeConstant(text, out Term message))
                    {
                        return null;
                    }

                    int field = config switch
                    {
                        Expr.RecordAccess { Value: Expr.Variable owner } access
                            when owner.Name == state.Name && access.Name == stateType.Name && access.Arity == fields.Count => access.Index,
                        Expr.Variable variable when stateFields.TryGetValue(variable.Name, out int index) => index,
                        _ => -1
                    };

                    return field < 0 || stateFields.Count != 0 && field != stateFields.Single().Value
                        ? null : new(reportCall, message, field);
                }
            }

            if (objectIndex < 0 || choiceIndex < 0 || !arms.Any(arm => arm.Read is null)
                || !arms.Any(arm => arm.Read is not null && arm.Decision is null))
            {
                return false;
            }

            EventReportArm[] ordered = arms.OrderByDescending(arm => arm.Selector,
                Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true))).ToArray();
            int failure = owner.nextLabel - 2;
            var labels = ordered.Select(_ => Label()).ToArray();
            EmitDetailRecordTest(
                0,
                outerArity,
                outerTag!,
                failure
            );
            owner.EmitTuplePart(0, nestedIndex, 2);
            EmitDetailRecordTest(
                2,
                innerArity,
                innerTag!,
                failure
            );
            owner.EmitTuplePart(2, 1, 3);
            owner.EmitTuplePart(2, choiceIndex, 4);
            owner.EmitTuplePart(0, objectIndex, 5);
            EmitSelection(
                XRegisterTag,
                3,
                failure,
                labels,
                ordered.Select(arm => (Term)arm.Selector).ToArray()
            );

            for (int index = 0; index < ordered.Length; index++)
            {
                EventReportArm arm = ordered[index];
                Mark(labels[index]);
                EmitDetailRecordTest(
                    1,
                    stateArity,
                    stateTag!,
                    failure
                );
                if (arm.Read is not { } read)
                {
                    EmitFrameAllocation(3, 1, ConsHeapWords);
                    owner.MoveRegister(
                        XRegisterTag,
                        1,
                        YRegisterTag,
                        0
                    );
                    owner.EmitTuplePart(2, arm.Projection, 0);
                    EmitMessage(
                        arm.Report!,
                        XRegisterTag,
                        1,
                        location,
                        allocateList: false
                    );
                    EmitReply(1);
                    continue;
                }

                int frame = arm.Decision is null ? 1 : 2;
                EmitFrameAllocation(6, frame);
                if (arm.Decision is not null)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        4,
                        YRegisterTag,
                        0
                    );
                }

                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    frame - 1
                );
                owner.MoveRegister(
                    XRegisterTag,
                    5,
                    XRegisterTag,
                    0
                );
                owner.EmitCallLine(read, location);
                owner.EmitStaticRemoteCall(read);
                var previous = CallLocation(read, location);
                if (arm.Decision is not { } decision)
                {
                    EmitMessage(
                        arm.Report!,
                        YRegisterTag,
                        0,
                        previous,
                        allocateList: true
                    );
                    EmitReply(1);
                    continue;
                }

                EmitSavedRecordField(1, arm.Projection, 1);
                var branches = arm.Choices.OrderByDescending(branch => branch.Key.Value).ToArray();
                int[] entries = branches.Select(_ => Label()).ToArray();
                int join = Label();
                int rejected = Label();
                EmitSelection(
                    YRegisterTag,
                    0,
                    rejected,
                    entries,
                    branches.Select(branch => (Term)branch.Key).ToArray()
                );
                for (int branch = 0; branch < branches.Length; branch++)
                {
                    Mark(entries[branch]);
                    EmitMessage(
                        branches[branch].Report,
                        XRegisterTag,
                        1,
                        previous,
                        allocateList: true,
                        clearSelector: true
                    );
                    if (branch != branches.Length - 1)
                    {
                        JumpTo(join);
                    }
                }

                Mark(join);
                EmitReply(2);
                Mark(rejected);
                owner.EmitLine(new BeamSourceLocation(decision.PreprocessorFile ?? location.File, decision.SourceLine));
                owner.Op(CaseEnd, YRegisterTag, 0);
            }

            return true;

            void EmitSelection(
                int tag,
                int register,
                int rejected,
                int[] entries,
                Term[] keys
            )
            {
                owner.Op(SelectValue, tag, register);
                owner.Operand(LabelTag, rejected);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 2 * keys.Length);
                for (int index = keys.Length - 1; index >= 0; index--)
                {
                    owner.LiteralOperand(keys[index]);
                    owner.Operand(LabelTag, entries[index]);
                }
            }

            void EmitMessage(
                EventReportMessage message,
                int stateRegisterKind,
                int stateRegister,
                BeamSourceLocation previous,
                bool allocateList,
                bool clearSelector = false
            )
            {
                if (allocateList)
                {
                    owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                    owner.Operand(BeamWire.UnsignedTag, clearSelector ? 2 : 1);
                }

                owner.Op(PutList, XRegisterTag, 0);
                owner.LiteralOperand(Nil.Value);
                owner.Operand(XRegisterTag, 2);
                if (clearSelector)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        1,
                        XRegisterTag,
                        0
                    );
                }
                else
                {
                    owner.EmitTuplePart(
                        stateRegister,
                        message.Field,
                        0,
                        inputTag: stateRegisterKind
                    );
                }

                owner.EmitConstant(message.Text, 1);
                if (clearSelector)
                {
                    EmitFrameInitialization(0, 1);
                }

                owner.EmitCallLine(message.Call, previous);
                owner.EmitStaticRemoteCall(message.Call);
            }

            void EmitReply(int frame)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, 0);
                owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(reply!.Name)), (YRegisterTag, frame - 1));
                owner.EmitFrameReturn(frame);
            }
        }
    }
}
