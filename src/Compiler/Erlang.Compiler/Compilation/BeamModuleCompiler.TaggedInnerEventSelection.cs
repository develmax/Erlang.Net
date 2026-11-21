using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record NestedEventPattern(
            Atom Tag,
            int Arity,
            int NestedField,
            Atom EventTag,
            int EventArity,
            Atom Selector,
            IReadOnlyDictionary<string, int> Fields
        );

        private bool TryEmitTaggedInnerEventSelection(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not [var direct, var reported, var fallback]
                || direct is not { Guard: null, Patterns: [var directEvent, AliasPattern directState] }
                || reported is not { Guard: null, Patterns: [var reportEvent, AliasPattern reportState] }
                || fallback is
                {
                    Guard: not null
                } || fallback.Patterns is not [Pattern.Variable eventValue, Pattern.Variable stateValue]
                || !DistinctBindings(eventValue, stateValue)
                || ReadNestedEventPattern(directEvent) is not { } action
                || ReadNestedEventPattern(reportEvent) is not { } report
                || action.Arity != report.Arity || !action.Tag.Equals(report.Tag)
                || action.NestedField != report.NestedField || action.EventArity <= report.EventArity
                || !ReadEventState(
                    directState,
                    out var actionState,
                    out var actionFields
                ) || !ReadEventState(
                    reportState,
                    out var reportStateName,
                    out var reportFields
                )
                || direct.Body is not Expr.Sequence
                {
                    Items: [
                        Expr.Call
                    {
                        Module: not null, DynamicRemote: false,
                        Arguments: [Expr.Variable actionObject, Expr.Variable actionInput]
                    } actionCall,
                        var directResult
                    ]
                }
                || reported.Body is not Expr.Sequence
                {
                    Items: [
                        Expr.Match
                    {
                        Pattern: Pattern.Variable reportedValue,
                        Value: Expr.Call
                        {
                            Module: not null, DynamicRemote: false,
                            Arguments: [Expr.Variable reportObject, Expr.Variable reportInput]
                        } readReport
                    },
                        var format,
                        var reportResult
                    ]
                }
                || fallback.Body is not Expr.Sequence { Items: [var unexpected, var fallbackResult] }
                || ReadRetainedEventResult(directResult, actionState.Name) is not { } reply
                || !reply.Equals(ReadRetainedEventResult(reportResult, reportStateName.Name))
                || !reply.Equals(ReadRetainedEventResult(fallbackResult, stateValue.Name))
                || ReadNestedEventFormat(format, reportStateName.Name, reportedValue.Name) is not { } formatCall
                || ReadNestedEventFormat(unexpected, stateValue.Name, eventValue.Name) is not { } reportUnexpected
                || !action.Fields.TryGetValue(actionObject.Name, out int objectField)
                || action.Fields.Count != 1 || report.Fields.Count != 2
                || !report.Fields.TryGetValue(reportObject.Name, out int sameObject) || sameObject != objectField
                || !report.Fields.TryGetValue(reportInput.Name, out int inputField) || inputField == objectField
                || actionFields is not [Pattern.Literal { Value: Atom actionStateTag }, ..]
                || reportFields is not [Pattern.Literal { Value: Atom reportStateTag }, ..]
                || !actionStateTag.Equals(reportStateTag) || actionFields.Count != reportFields.Count
                || !formatCall.Access.Name.Equals(reportStateTag.Name, StringComparison.Ordinal)
                || formatCall.Access.Arity != reportFields.Count || formatCall.Access.Name != reportUnexpected.Access.Name
                || formatCall.Access.Arity != reportUnexpected.Access.Arity || formatCall.Access.Index != reportUnexpected.Access.Index)
            {
                return false;
            }

            int actionField = -1;
            for (int index = 1; index < actionFields.Count; index++)
            {
                if (actionFields[index] is not Pattern.Variable field
                    || field.Name != VariableScopeNames.Wildcard && (field.Name != actionInput.Name || actionField >= 0))
                {
                    return false;
                }

                if (field.Name != VariableScopeNames.Wildcard)
                {
                    actionField = index;
                }
            }
            if (actionField < 0 || !DistinctBindings(actionState, new Pattern.Variable(actionObject.Name), new Pattern.Variable(actionInput.Name))
                || !DistinctBindings(
                    reportStateName,
                    new Pattern.Variable(reportObject.Name),
                    new Pattern.Variable(reportInput.Name),
                    reportedValue
                )
                || reportFields.Skip(1).Any(field => field is not Pattern.Variable { Name: VariableScopeNames.Wildcard }))
            {
                return false;
            }

            int actionEntry = Label();
            int reportEntry = Label();
            int fallbackEntry = Label();
            int badState = Label();
            EmitDetailRecordTest(
                0,
                action.Arity,
                action.Tag,
                fallbackEntry
            );
            owner.EmitTuplePart(0, action.NestedField, 2);
            owner.Op(IsTuple, LabelTag, fallbackEntry);
            owner.Operand(XRegisterTag, 2);
            owner.EmitTuplePart(0, objectField, 3);
            owner.instructions.WriteByte(SelectTupleArity);
            owner.EmitTypedRegister(2, TupleTypeBits);
            owner.Operand(LabelTag, fallbackEntry);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, report.EventArity);
            owner.Operand(LabelTag, reportEntry);
            owner.Operand(BeamWire.UnsignedTag, action.EventArity);
            owner.Operand(LabelTag, actionEntry);

            Mark(actionEntry);
            EmitEventTests(action);
            owner.EmitTuplePart(1, actionField, 1);
            Move(XRegisterTag, 3, 0);
            owner.EmitCallLine(actionCall, location);
            owner.EmitStaticRemoteCall(actionCall);
            EmitEventReply();

            Mark(reportEntry);
            EmitEventTests(report);
            owner.EmitTuplePart(0, inputField, 1);
            Move(XRegisterTag, 3, 0);
            owner.EmitCallLine(readReport, location);
            owner.EmitStaticRemoteCall(readReport);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitEventReport(
                formatCall,
                YRegisterTag,
                0,
                CallLocation(readReport, location)
            );
            EmitEventReply();

            Mark(fallbackEntry);
            EmitDetailRecordTest(
                1,
                actionFields.Count,
                actionStateTag,
                badState
            );
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            EmitEventReport(
                reportUnexpected,
                XRegisterTag,
                1,
                location
            );
            EmitEventReply();

            Mark(badState);
            owner.Op(BadRecord, XRegisterTag, 1);

            return true;

            void EmitEventTests(NestedEventPattern pattern)
            {
                owner.EmitTuplePart(2, 0, 4);
                owner.EmitAtomTest(4, pattern.EventTag.Name, fallbackEntry);
                owner.EmitTuplePart(2, 1, 2);
                owner.EmitAtomTest(2, pattern.Selector.Name, fallbackEntry);
                EmitDetailRecordTest(
                    1,
                    actionFields.Count,
                    actionStateTag,
                    badState
                );
                EmitFrameAllocation(4, 1);
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    0
                );
            }

            void EmitEventReport(
                (Expr.Call Call, Expr.RecordAccess Access, Term Message) plan,
                int inputTag,
                int value,
                BeamSourceLocation previous
            )
            {
                owner.Op(PutList, XRegisterTag, 0);
                owner.LiteralOperand(Nil.Value);
                owner.Operand(XRegisterTag, 2);
                owner.Op(GetTupleElement, inputTag, value);
                owner.Operand(BeamWire.UnsignedTag, plan.Access.Index);
                owner.Operand(XRegisterTag, 0);
                owner.EmitConstant(plan.Message, 1);
                owner.EmitCallLine(plan.Call, previous);
                owner.EmitStaticRemoteCall(plan.Call);
            }

            void EmitEventReply()
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, 0);
                owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(reply.Name)), (YRegisterTag, 0));
                owner.EmitFrameReturn(1);
            }

            void Move(int inputTag, int from, int to) => owner.MoveRegister(
                inputTag,
                from,
                XRegisterTag,
                to
            );
        }

        private static NestedEventPattern? ReadNestedEventPattern(Pattern input)
        {
            if (input is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } outer)
            {
                return null;
            }

            int nestedIndex = -1;
            Pattern.Tuple? inner = null;
            var fields = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 1; index < outer.Items.Count; index++)
            {
                if (outer.Items[index] is Pattern.Tuple nested && inner is null)
                {
                    nestedIndex = index;
                    inner = nested;
                }
                else if (outer.Items[index] is not Pattern.Variable binding
                    || binding.Name != VariableScopeNames.Wildcard && !fields.TryAdd(binding.Name, index))
                {
                    return null;
                }
            }

            if (inner is not { Items: [Pattern.Literal { Value: Atom innerTag }, Pattern.Literal { Value: Atom selector }, ..] }
                || inner.Items.Skip(2).Any(field => field is not Pattern.Variable { Name: VariableScopeNames.Wildcard }))
            {
                return null;
            }

            return new(
                tag,
                outer.Items.Count,
                nestedIndex,
                innerTag,
                inner.Items.Count,
                selector,
                fields
            );
        }

        private static bool ReadEventState(AliasPattern alias, out Pattern.Variable binding, out IReadOnlyList<Pattern> fields)
        {
            if ((alias.Left as Pattern.Variable ?? alias.Right as Pattern.Variable) is { } name
                && (alias.Left is Pattern.Variable ? alias.Right : alias.Left) is Pattern.Tuple record)
            {
                binding = name;
                fields = record.Items;

                return true;
            }

            binding = null!;
            fields = [];

            return false;
        }

        private static Atom? ReadRetainedEventResult(Expr expression, string state) =>
            expression is Expr.Tuple
            {
                Items: [Expr.Literal { Value: Atom tag }, Expr.Variable retained]
            } && retained.Name == state ? tag : null;

        private static (Expr.Call Call, Expr.RecordAccess Access, Term Message)? ReadNestedEventFormat(
            Expr expression,
            string state,
            string value
        ) =>
            expression is Expr.Call
            {
                Module: not null, DynamicRemote: false, Arguments:
                [
                    Expr.RecordAccess { Value: Expr.Variable owner } field,
                    var message,
                    Expr.List { Items: [Expr.Variable entry], Tail: null }
                ]
            } call && owner.Name == state && entry.Name == value
                && AttributeLiteral.TryNormalizeConstant(message, out Term formatted)
                ? (call, field, formatted) : null;
    }
}
