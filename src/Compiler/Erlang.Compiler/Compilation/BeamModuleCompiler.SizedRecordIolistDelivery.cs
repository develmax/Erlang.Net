using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSizedRecordIolistDelivery(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable record],
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Match { Pattern: Pattern.Variable input, Value: Expr.RecordAccess header },
                                Expr.Match { Pattern: Pattern.Variable body, Value: Expr.Call source },
                                Expr.Match
                            {
                                Pattern: Pattern.Variable length, Value: Expr.Call
                                {
                                    Function: AutoImportNames.Length, DynamicRemote: false,
                                    Arguments: [Expr.Binary
                                    {
                                        Operator: ErlangOperators.Append, Left: Expr.RecordAccess seed,
                                        Right: Expr.Variable appended
                                    } append]
                                } measure
                            },
                                Expr.Match { Pattern: Pattern.Variable payload, Value: Expr.List { Tail: null } response },
                                Expr.Call
                            {
                                Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                                Arguments: [Expr.RecordAccess first, Expr.RecordAccess second, Expr.Variable delivered]
                            } deliver,
                                Expr.Tuple
                            {
                                Items: [Expr.Literal { Value: Atom reply },
                                        Expr.List
                                {
                                    Items: [Expr.Tuple
                                    {
                                        Items: [Expr.Literal { Value: Atom result },
                                                Expr.Tuple
                                        {
                                            Items: [Expr.Literal { Value: Atom resultTag }, var status,
                                                    Expr.Variable returnedLength]
                                        }]
                                    }], Tail: Expr.RecordAccess tail
                                }]
                            }
                            ]
                    }
                }
                ]
                || !DistinctBindings(
                    record,
                    input,
                    body,
                    length,
                    payload
                )
                || source is not { Module: null, DynamicRemote: false, Arguments: [Expr.Variable usedInput] }
                || usedInput.Name != input.Name || appended.Name != body.Name || delivered.Name != payload.Name
                || returnedLength.Name != length.Name || !owner.ResolvesErlangBif(measure)
                || !entries.TryGetValue((source.Function, 1), out int bodyEntry)
                || !ProvesProperListReturn(source, [], includeFlatten: true)
                || !AttributeLiteral.TryNormalizeConstant(status, out Term resultValue)
                || response.Items.ToArray() is not [.. var decoration, Expr.RecordAccess echoed, Expr.Variable lastBody]
                || lastBody.Name != body.Name || echoed.Index != seed.Index
                || !new[] { header, seed, first, second, tail, echoed }.All(access =>
                    access.Name == header.Name && access.Arity == header.Arity
                    && access.Value is Expr.Variable value && value.Name == record.Name))
            {
                return false;
            }

            var beforeLength = new List<Term>();
            var afterLength = new List<Term>();
            Expr.Call? convert = null;
            foreach (Expr part in decoration)
            {
                if (convert is null && part is Expr.Call
                    {
                        Function: AutoImportNames.IntegerToList, DynamicRemote: false, Arguments: [Expr.Variable value]
                    } call && value.Name == length.Name && owner.ResolvesErlangBif(call))
                {
                    convert = call;
                }
                else if (AttributeLiteral.TryNormalizeConstant(part, out Term literal))
                {
                    (convert is null ? beforeLength : afterLength).Add(literal);
                }
                else
                {
                    return false;
                }
            }

            if (convert is null || beforeLength.Count == 0)
            {
                return false;
            }

            const int retainedRecord = 2;
            const int retainedBody = 1;
            const int retainedLength = 0;
            int badRecord = Label();
            EmitDetailRecordTest(
                0,
                header.Arity,
                new Atom(header.Name),
                badRecord
            );
            EmitFrameAllocation(1, 3);
            EmitFrameInitialization(0, retainedRecord);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                retainedRecord
            );
            owner.EmitTuplePart(0, header.Index, 0);
            owner.EmitCallLine(source, location);
            owner.EmitFieldCall(1, bodyEntry);
            ReadSavedField(retainedRecord, seed.Index, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                retainedBody
            );
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            var previous = CallLocation(source, location);
            owner.EmitArithmeticLine(append.SourceLine, ref previous);
            Call(ErlangModuleNames.Module, ErlangOperators.Append, 2);

            owner.EmitCallLine(measure, previous);
            owner.Op(GcBifOne, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Length, 1));
            owner.EmitTypedRegister(0, ProperListTypeBits);
            owner.Operand(YRegisterTag, retainedLength);
            Load(retainedLength);
            owner.EmitCallLine(convert, CallLocation(measure, previous));
            Call(ErlangModuleNames.Module, AutoImportNames.IntegerToList, 1);

            owner.Op(TestHeap, BeamWire.UnsignedTag, response.Items.Count * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            ReadSavedField(retainedRecord, seed.Index, 1);
            owner.Op(PutList, YRegisterTag, retainedBody);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 2);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
            for (int index = afterLength.Count - 1; index >= 0; index--)
            {
                Prepend(afterLength[index], 1, 1);
            }

            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            for (int index = beforeLength.Count - 1; index >= 0; index--)
            {
                Prepend(beforeLength[index], 0, index == 0 ? 2 : 0);
            }

            ReadSavedField(retainedRecord, first.Index, 0);
            ReadSavedField(retainedRecord, second.Index, 1);
            owner.MoveRegister(
                YRegisterTag,
                retainedLength,
                YRegisterTag,
                retainedBody
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitCallLine(deliver, CallLocation(convert, previous));
            owner.EmitStaticRemoteCall(deliver);

            owner.Op(TestHeap, BeamWire.UnsignedTag, 10 + ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 0);
            ReadSavedField(1, tail.Index, 0);
            TupleHeader(1, 3);
            owner.LiteralOperand(resultTag);
            owner.LiteralOperand(resultValue);
            owner.Operand(YRegisterTag, 0);
            TupleHeader(1, 2);
            owner.LiteralOperand(result);
            owner.Operand(XRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            TupleHeader(0, 2);
            owner.LiteralOperand(reply);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(2);

            Mark(badRecord);
            if (header.SourceLine > 0)
            {
                owner.EmitLine(new(header.PreprocessorFile ?? location.File, header.SourceLine));
            }

            owner.Op(BadRecord, XRegisterTag, 0);

            return true;

            void ReadSavedField(int slot, int field, int destination)
            {
                owner.Op(GetTupleElement, YRegisterTag, slot);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(XRegisterTag, destination);
            }

            void Prepend(Term member, int tailRegister, int destination)
            {
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(member);
                owner.Operand(XRegisterTag, tailRegister);
                owner.Operand(XRegisterTag, destination);
            }
        }
    }
}
