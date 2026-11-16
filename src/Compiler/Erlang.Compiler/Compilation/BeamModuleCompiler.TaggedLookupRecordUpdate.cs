using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTaggedLookupRecordUpdate(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                { Guard: null, Patterns: [Pattern.Tuple firstMessage, AliasPattern firstRecord], Body: Expr.Case lookup },
                { Guard: null, Patterns: [Pattern.Tuple secondMessage, AliasPattern secondRecord], Body: var deletion },
                {
                    Guard: null, Patterns: [Pattern.Variable fallbackMessage, Pattern.Variable fallbackState],
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom reply }, Expr.Variable fallbackValue] }
                }
                ]
                || !DistinctBindings(fallbackMessage, fallbackState) || fallbackState.Name != fallbackValue.Name
                || !HasTupleTag(firstMessage, out Atom messageTag) || !HasTupleTag(secondMessage, out Atom secondTag)
                || !messageTag.Equals(secondTag) || firstMessage.Items.Count != secondMessage.Items.Count
                || !TryReadPatternAlias(firstRecord, out var state, out var firstShape)
                || !TryReadPatternAlias(secondRecord, out var deleteState, out var secondShape)
                || firstShape is not Pattern.Tuple record || secondShape is not Pattern.Tuple deleteRecord
                || record.Items.Count != deleteRecord.Items.Count || !HasTupleTag(record, out Atom recordTag)
                || !HasTupleTag(deleteRecord, out Atom deleteRecordTag) || !recordTag.Equals(deleteRecordTag))
            {
                return false;
            }

            if (lookup is not
                {
                    Value: Expr.Call { Arguments: [Expr.Variable key, Expr.Literal index, Expr.Variable collection] } lookupCall,
                    Clauses:
                    [
                    {
                        Guard: null,
                        Patterns: [Pattern.Tuple
                        {
                            Items: [Pattern.Literal { Value: Atom found },
                                Pattern.Tuple { Items: [Pattern.Variable value, Pattern.Variable foundKey] },
                                Pattern.Variable remaining]
                        }],
                        Body: Expr.Sequence
                        {
                            Items: [var unused, Expr.Tuple
                            { Items: [Expr.Literal hitReply, Expr.RecordUpdate hitUpdate] }]
                        }
                    },
                    {
                        Guard: null, Patterns: [Pattern.Literal { Value: Atom absent }],
                        Body: Expr.Tuple { Items: [Expr.Literal missReply, Expr.Variable missState] }
                    }
                    ]
                }
                || !DistinctBindings(
                    state,
                    new Pattern.Variable(key.Name),
                    new Pattern.Variable(collection.Name),
                    value,
                    remaining
                )
                || key.Name != foundKey.Name || state.Name != missState.Name
                || !reply.Equals(hitReply.Value) || !reply.Equals(missReply.Value)
                || !ReadUpdate(hitUpdate, state.Name, out var replacement) || replacement is not Expr.Variable rest || rest.Name != remaining.Name
                || UnwrapWildcardMatch(unused) is not Expr.Case
                {
                    Value: Expr.Call { Arguments: [Expr.Variable probeKey] } probe,
                    Clauses:
                    [
                    {
                        Guard: null, Patterns: [Pattern.Tuple
                        {
                            Items: [Pattern.Literal { Value: Atom present },
                            Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                        }], Body: Expr.Call
                        { Arguments: [Expr.Variable effectKey] } effect
                    },
                    { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: Expr.Literal }
                    ]
                }
                || probeKey.Name != value.Name || effectKey.Name != value.Name
                || deletion is not Expr.Tuple
                || ReadEntryRecordCall(deletion) is not { Tuple.Items: [Expr.Literal deleteReply, _] } deleted
                || !reply.Equals(deleteReply.Value) || !ReadUpdate(deleted.Update, deleteState.Name, out _)
                || deleted.Update.Fields[0].Index != hitUpdate.Fields[0].Index
                || deleted.Call.Arguments is not [Expr.Variable deleteKey, Expr.Literal deleteIndex, Expr.Variable deleteCollection]
                || !index.Value.Equals(deleteIndex.Value)
                || !TryResolveOrdinaryCallTarget(lookupCall, out string? lookupModule)
                || !TryResolveOrdinaryCallTarget(probe, out string? probeModule)
                || !TryResolveOrdinaryCallTarget(effect, out string? effectModule)
                || !TryResolveOrdinaryCallTarget(deleted.Call, out string? deleteModule))
            {
                return false;
            }

            int field = hitUpdate.Fields[0].Index;
            if (field <= 0 || field >= record.Items.Count
                || !IsSingleFieldRecord(record, field, collection.Name)
                || !IsSingleFieldRecord(deleteRecord, field, deleteCollection.Name)
                || !DistinctBindings(deleteState, new Pattern.Variable(deleteKey.Name), new Pattern.Variable(deleteCollection.Name)))
            {
                return false;
            }

            int keyField = -1;
            int selectorField = -1;
            Term? selector = null;
            var sharedTests = new List<(int Field, Term Value)>();
            for (int indexField = 1; indexField < firstMessage.Items.Count; indexField++)
            {
                var first = firstMessage.Items[indexField];
                var second = secondMessage.Items[indexField];
                switch (first, second)
                {
                    case (Pattern.Variable left, Pattern.Variable right) when left.Name == key.Name && right.Name == deleteKey.Name && keyField < 0:
                        keyField = indexField;
                        break;
                    case (Pattern.Literal literal, Pattern.Variable unusedSelector) when selectorField < 0
                        && unusedSelector.Name != deleteKey.Name && unusedSelector.Name != deleteState.Name
                        && unusedSelector.Name != deleteCollection.Name:
                        selectorField = indexField;
                        selector = literal.Value;
                        break;
                    case (Pattern.Literal left, Pattern.Literal right) when left.Value.Equals(right.Value):
                        sharedTests.Add((indexField, left.Value));
                        break;
                    case (Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable { Name: VariableScopeNames.Wildcard }):
                        break;
                    default:
                        return false;
                }
            }

            if (keyField < 0 || selectorField < 0)
            {
                return false;
            }

            int update = Label();
            int missing = Label();
            int delete = Label();
            int fallback = Label();
            int caseFailure = Label();
            EmitDetailRecordTest(
                0,
                firstMessage.Items.Count,
                messageTag,
                fallback
            );
            foreach (var (position, constant) in sharedTests)
            {
                owner.EmitTuplePart(0, position, 2);
                Equal(
                    XRegisterTag,
                    2,
                    constant,
                    fallback
                );
            }

            owner.EmitTuplePart(0, keyField, 2);
            owner.EmitTuplePart(0, selectorField, 0);
            Equal(
                XRegisterTag,
                0,
                selector!,
                delete
            );
            EmitDetailRecordTest(
                1,
                record.Items.Count,
                recordTag,
                fallback
            );

            // Keep the original key for the match, then reuse its slot for the effect argument.
            EmitFrameAllocation(3, 3);
            Initialize(0);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                2
            );
            owner.EmitTuplePart(1, field, 2);
            owner.EmitConstant(index.Value, 1);
            Load(1);
            owner.EmitCallLine(lookupCall, location);
            Call(lookupModule, lookupCall.Function, 3);

            owner.Op(IsTuple, LabelTag, missing);
            owner.Operand(XRegisterTag, 0);
            EmitDetailRecordTest(
                0,
                3,
                found,
                caseFailure,
                TupleTypeBits
            );
            owner.EmitTuplePart(0, 1, 1);
            TuplePartToSlot(0, 2, 0);
            EmitTupleShapeTest(
                XRegisterTag,
                1,
                2,
                caseFailure
            );
            owner.EmitTuplePart(1, 1, 2);
            owner.Op(ExactEqualTest, LabelTag, caseFailure);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(YRegisterTag, 1);
            TuplePartToSlot(1, 0, 1);
            Load(1);
            owner.EmitCallLine(probe, CallLocation(lookupCall, location));
            Call(probeModule, probe.Function, 1);
            EmitDetailRecordTest(
                0,
                2,
                present,
                update
            );
            Load(1);
            Initialize(1);
            owner.EmitCallLine(effect, CallLocation(probe, location));
            Call(effectModule, effect.Function, 1);

            Mark(update);
            UpdateAndReturn(
                2,
                YRegisterTag,
                0,
                0,
                3
            );

            Mark(missing);
            Equal(
                XRegisterTag,
                0,
                absent,
                caseFailure
            );
            ReturnState(
                YRegisterTag,
                2,
                0,
                3
            );

            Mark(delete);
            EmitDetailRecordTest(
                1,
                record.Items.Count,
                recordTag,
                fallback
            );
            EmitFrameAllocation(3, 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitTuplePart(1, field, 0);
            owner.EmitConstant(index.Value, 1);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 2);
            owner.EmitCallLine(deleted.Call, location);
            Call(deleteModule, deleted.Call.Function, 3);
            UpdateAndReturn(
                0,
                XRegisterTag,
                0,
                1,
                1
            );

            Mark(fallback);
            ReturnState(
                XRegisterTag,
                1,
                2,
                0
            );

            Mark(caseFailure);
            owner.EmitLine(CallLocation(lookupCall, location));
            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;

            bool ReadUpdate(Expr.RecordUpdate expression, string original, out Expr? value)
            {
                value = null;
                if (expression.Value is not Expr.Variable source || source.Name != original
                    || expression.Arity != record.Items.Count || expression.Name != recordTag.Name
                    || expression.Fields is not [var fieldUpdate])
                {
                    return false;
                }

                value = fieldUpdate.Value;

                return true;
            }

            static bool IsSingleFieldRecord(Pattern.Tuple shape, int index, string name) =>
                shape.Items[index] is Pattern.Variable member && member.Name == name
                && shape.Items.Skip(1).Where((_, fieldIndex) => fieldIndex + 1 != index)
                    .All(item => item is Pattern.Variable { Name: VariableScopeNames.Wildcard });

            void Equal(
                int tag,
                int register,
                Term constant,
                int failure
            )
            {
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(tag, register);
                owner.LiteralOperand(constant);
            }

            void Initialize(int slot)
            {
                owner.instructions.WriteByte(InitYRegisters);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Operand(YRegisterTag, slot);
            }

            void TuplePartToSlot(int input, int position, int slot)
            {
                owner.Op(GetTupleElement, XRegisterTag, input);
                owner.Operand(BeamWire.UnsignedTag, position);
                owner.Operand(YRegisterTag, slot);
            }

            void ReturnState(
                int tag,
                int register,
                int live,
                int frame
            )
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(reply.Name)), (tag, register));
                if (frame == 0)
                {
                    owner.instructions.WriteByte(Return);
                }
                else
                {
                    owner.EmitFrameReturn(frame);
                }
            }

            void UpdateAndReturn(
                int source,
                int replacementTag,
                int replacementRegister,
                int live,
                int frame
            )
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, record.Items.Count + 4);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Op(UpdateRecord, AtomTag, owner.Intern(record.Items.Count < RecordUpdateArityLimit ? "reuse" : "copy"));
                owner.Operand(BeamWire.UnsignedTag, record.Items.Count);
                owner.EmitTypedRegister(source, TupleTypeBits, registerTag: YRegisterTag);
                owner.Operand(XRegisterTag, 0);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.Operand(BeamWire.UnsignedTag, field + 1);
                owner.Operand(replacementTag, replacementRegister);
                owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(reply.Name)), (XRegisterTag, 0));
                owner.EmitFrameReturn(frame);
            }
        }
    }
}
