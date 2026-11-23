using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTaggedRecordActionAndReport(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [
                            Pattern.Tuple { Items: [Pattern.Literal { Value: Atom outerTag }, ..] } outer,
                            AliasPattern actionState
                        ],
                    Body: Expr.Sequence
                    {
                        Items: [
                                Expr.Call
                        {
                            Module: null, DynamicRemote: false,
                            Arguments: [Expr.RecordAccess { Value: Expr.Variable accessOwner } access, Expr.Variable eventValue]
                        } invoke,
                                var firstResult
                            ]
                    }
                },
                {
                    Guard: null, Patterns: [AliasPattern originalEvent, AliasPattern originalState],
                    Body: Expr.Sequence { Items: [var log, var lastResult] }
                }
                ] || !entries.TryGetValue((invoke.Function, 2), out int entry)
                || !ReadEventState(
                    actionState,
                    out var state,
                    out var fields
                ) || !ReadEventState(
                    originalEvent,
                    out var fallbackEvent,
                    out var outerFields
                ) || !ReadEventState(
                    originalState,
                    out var fallbackState,
                    out var fallbackFields
                )
                || state.Name != accessOwner.Name || state.Name == VariableScopeNames.Wildcard
                || state.Name == eventValue.Name || !DistinctBindings(fallbackEvent, fallbackState)
                || fields is not [Pattern.Literal { Value: Atom stateTag }, ..]
                || !HasWildFields(fallbackFields, stateTag, fields.Count)
                || !HasWildFields(outerFields, outerTag, outer.Items.Count)
                || !HasWildFields(fields, stateTag, fields.Count)
                || access.Name != stateTag.Name || access.Arity != fields.Count
                || ReadRetainedEventResult(firstResult, state.Name) is not { } reply
                || !reply.Equals(ReadRetainedEventResult(lastResult, fallbackState.Name))
                || ReadNestedEventFormat(log, fallbackState.Name, fallbackEvent.Name) is not { } format
                || format.Access.Name != stateTag.Name || format.Access.Arity != fields.Count)
            {
                return false;
            }

            int eventIndex = -1;
            Pattern.Tuple? eventTuple = null;
            for (int index = 1; index < outer.Items.Count; index++)
            {
                if (outer.Items[index] is Pattern.Tuple pattern && eventTuple is null)
                {
                    eventIndex = index;
                    eventTuple = pattern;
                }
                else if (outer.Items[index] is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    return false;
                }
            }

            if (eventTuple is not { Items: [Pattern.Literal { Value: Atom eventTag }, Pattern.Literal { Value: Atom selector }, ..] })
            {
                return false;
            }

            int payload = -1;
            for (int index = 2; index < eventTuple.Items.Count; index++)
            {
                if (eventTuple.Items[index] is not Pattern.Variable field
                    || field.Name != VariableScopeNames.Wildcard && (field.Name != eventValue.Name || payload >= 0))
                {
                    return false;
                }

                if (field.Name != VariableScopeNames.Wildcard)
                {
                    payload = index;
                }
            }

            if (payload < 0 || eventValue.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int fallback = Label();
            EmitDetailRecordTest(
                0,
                outer.Items.Count,
                outerTag,
                failure
            );
            owner.EmitTuplePart(0, eventIndex, 2);
            EmitDetailRecordTest(
                2,
                eventTuple.Items.Count,
                eventTag,
                fallback
            );
            owner.EmitTuplePart(2, 1, 3);
            owner.EmitAtomTest(3, selector.Name, fallback);
            EmitDetailRecordTest(
                1,
                fields.Count,
                stateTag,
                failure
            );
            EmitFrameAllocation(3, 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitTuplePart(1, access.Index, 0);
            owner.EmitTuplePart(2, payload, 1);
            owner.EmitCallLine(invoke, location);
            owner.Op(LocalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);
            EmitReply();

            Mark(fallback);
            EmitDetailRecordTest(
                1,
                fields.Count,
                stateTag,
                failure
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
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 2);
            owner.EmitTuplePart(1, format.Access.Index, 0);
            owner.EmitConstant(format.Message, 1);
            owner.EmitCallLine(format.Call, location);
            owner.EmitStaticRemoteCall(format.Call);
            EmitReply();

            return true;

            void EmitReply()
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, 0);
                owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(reply.Name)), (YRegisterTag, 0));
                owner.EmitFrameReturn(1);
            }

            static bool HasWildFields(IReadOnlyList<Pattern> patterns, Atom expected, int arity) =>
                patterns is [Pattern.Literal { Value: Atom type }, ..] && expected.Equals(type)
                    && patterns.Count == arity
                    && patterns.Skip(1).All(field => field is Pattern.Variable { Name: VariableScopeNames.Wildcard });
        }
    }
}
