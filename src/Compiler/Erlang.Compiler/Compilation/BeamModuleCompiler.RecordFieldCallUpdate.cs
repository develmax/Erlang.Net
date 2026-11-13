using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitDeferredRecordCallUpdate(Clause clause, int failure)
        {
            if (clause is not
                {
                    Guard: null,
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match { Pattern: Pattern.Variable firstResult, Value: Expr.Call first },
                            Expr.Match { Pattern: Pattern.Variable secondResult, Value: Expr.Call second },
                            Expr.Tuple returned
                        ]
                    }
                }
                || first.Arguments is not [Expr.Literal firstConstant, Expr.Variable firstInput]
                || second.Arguments is not
                    [Expr.Variable key, Expr.Literal secondConstant, Expr.Variable value,
                        Expr.Tuple { Items: [Expr.Variable tupleKey, Expr.Variable tupleValue] }]
                || returned.Items.Count < 2
                || !returned.Items.Take(returned.Items.Count - 1).All(item => AttributeLiteral.TryNormalizeConstant(item, out _))
                || returned.Items[^1] is not Expr.RecordUpdate
                    { Value: Expr.Variable state, Fields: [{ Value: Expr.Variable replacement } field] } update
                || key.Name != tupleKey.Name || firstResult.Name != tupleValue.Name || secondResult.Name != replacement.Name
                || !TryResolveOrdinaryCallTarget(first, out string? firstModule)
                || !TryResolveOrdinaryCallTarget(second, out string? secondModule))
            {
                return false;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            var fields = new Dictionary<string, (int Input, int Field)>(StringComparer.Ordinal);
            var records = new List<(int Input, int Arity, Atom Tag)>();
            int stateInput = -1;
            for (int input = 0; input < clause.Patterns.Count; input++)
            {
                Pattern pattern = clause.Patterns[input];
                if (pattern is Pattern.Variable parameter)
                {
                    if (!Bind(parameter))
                    {
                        return false;
                    }

                    continue;
                }

                if (pattern is AliasPattern { Left: Pattern.Tuple aliased, Right: Pattern.Variable alias }
                    && alias.Name == state.Name && stateInput < 0 && Bind(alias))
                {
                    stateInput = input;
                    pattern = aliased;
                }

                if (pattern is not Pattern.Tuple tuple || !HasTupleTag(tuple, out Atom tag))
                {
                    return false;
                }

                records.Add((input, tuple.Items.Count, tag));
                for (int index = 1; index < tuple.Items.Count; index++)
                {
                    if (tuple.Items[index] is not Pattern.Variable member || !Bind(member))
                    {
                        return false;
                    }

                    if (member.Name != VariableScopeNames.Wildcard)
                    {
                        fields.Add(member.Name, (input, index));
                    }
                }
            }

            if (firstResult.Name == VariableScopeNames.Wildcard || secondResult.Name == VariableScopeNames.Wildcard
                || !Bind(firstResult) || !Bind(secondResult)
                || records.Count != 2 || stateInput < 0
                || !records.Any(record => record.Input == stateInput && record.Arity == update.Arity && record.Tag.Name == update.Name)
                || !fields.TryGetValue(firstInput.Name, out var firstField) || firstField.Input == stateInput
                || !fields.TryGetValue(key.Name, out var keyField) || keyField.Input != firstField.Input || keyField == firstField
                || !fields.TryGetValue(value.Name, out var valueField) || valueField.Input != stateInput
                || field.Index <= 0 || field.Index >= update.Arity)
            {
                return false;
            }

            foreach (var (input, arity, tag) in records)
            {
                EmitDetailRecordTest(
                    input,
                    arity,
                    tag,
                    failure
                );
            }

            EmitFrameAllocation(Math.Max(stateInput, firstField.Input) + 1, 2);
            owner.MoveRegister(XRegisterTag, stateInput, YRegisterTag, 0);
            owner.MoveRegister(XRegisterTag, firstField.Input, YRegisterTag, 1);
            owner.EmitTuplePart(firstField.Input, firstField.Field, 1);
            owner.EmitConstant(firstConstant.Value, 0);
            owner.EmitCallLine(first, location);
            Call(firstModule, first.Function, first.Arguments.Count);

            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitSavedField(1, keyField.Field, 1);
            owner.EmitTupleHeader(XRegisterTag, 2, 3);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            EmitSavedField(0, valueField.Field, 2);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            owner.EmitConstant(secondConstant.Value, 1);
            owner.MoveRegister(YRegisterTag, 0, YRegisterTag, 1);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitCallLine(second, CallLocation(first, location));
            Call(secondModule, second.Function, second.Arguments.Count);

            owner.Op(TestHeap, BeamWire.UnsignedTag, update.Arity + returned.Items.Count + 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitSavedRecordUpdate(update.Arity, field.Index);
            owner.EmitTupleHeader(XRegisterTag, returned.Items.Count);
            foreach (Expr item in returned.Items.Take(returned.Items.Count - 1))
            {
                AttributeLiteral.TryNormalizeConstant(item, out Term constant);
                owner.LiteralOperand(constant);
            }

            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);

            return true;

            bool Bind(Pattern.Variable variable) => variable.Name == VariableScopeNames.Wildcard || names.Add(variable.Name);

            void EmitSavedField(int slot, int index, int register)
            {
                owner.Op(GetTupleElement, YRegisterTag, slot);
                owner.Operand(BeamWire.UnsignedTag, index);
                owner.Operand(XRegisterTag, register);
            }
        }

        private void EmitSavedRecordUpdate(int arity, int index)
        {
            owner.Op(UpdateRecord, AtomTag, owner.Intern(arity < RecordUpdateArityLimit ? "reuse" : "copy"));
            owner.Operand(BeamWire.UnsignedTag, arity);
            owner.EmitTypedRegister(0, TupleTypeBits, registerTag: YRegisterTag);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, index + 1);
            owner.Operand(XRegisterTag, 0);
        }

        private bool TryEmitRecordFieldCallUpdate(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [
                    Pattern.Tuple { Items: [Pattern.Literal { Value: Atom messageTag }, Pattern.Variable key] },
                    AliasPattern { Left: Pattern.Tuple statePattern, Right: Pattern.Variable state }
                  ], Body: Expr.Sequence
                  {
                      Items: [
                    Expr.Match
                      {
                          Pattern: Pattern.Variable result, Value: Expr.Call
                          {
                              Module: not null, DynamicRemote: false,
                              Arguments: [Expr.Variable inputKey, Expr.Literal { Value: Integer index }, Expr.Variable inputField]
                          } call
                      },
                    Expr.Tuple
                      {
                          Items: [Expr.Literal { Value: Atom reply }, Expr.RecordUpdate
                          {
                              Value: Expr.Variable oldState, Fields: [var field]
                          } update]
                      }
                  ]
                  }
                },
                {
                    Guard: null, Patterns: [Pattern.Variable message, Pattern.Variable fallback],
                    Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Call
                        {
                            Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Literal { Value: var format }, Expr.List
                            {
                                Tail: null, Items: [
                            Expr.Literal { Value: Atom module }, Expr.Literal { Value: Integer line }, Expr.Variable logged
                        ]
                            }
                    ]
                        } log,
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom otherReply }, Expr.Variable returned] }
                  ]
                    }
                }
            ] || statePattern.Items is not [Pattern.Literal { Value: Atom recordTag }, ..]
                || update.Name != recordTag.Name || update.Arity != statePattern.Items.Count
                || field.Index <= 0 || field.Index >= update.Arity
                || statePattern.Items[field.Index] is not Pattern.Variable bound
                || statePattern.Items.Skip(1).Where((_, offset) => offset + 1 != field.Index)
                    .Any(pattern => pattern is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                || !DistinctBindings(
                    key,
                    state,
                    bound,
                    result
                ) || !DistinctBindings(message, fallback)
                || inputKey.Name != key.Name || inputField.Name != bound.Name || oldState.Name != state.Name
                || field.Value is not Expr.Variable replacement || replacement.Name != result.Name
                || logged.Name != message.Name || returned.Name != fallback.Name || !reply.Equals(otherReply))
            {
                return false;
            }

            int other = Label();
            EmitDetailRecordTest(
                0,
                2,
                messageTag,
                other
            );
            EmitDetailRecordTest(
                1,
                update.Arity,
                recordTag,
                other
            );
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitTuplePart(1, field.Index, 2);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitConstant(index, 1);
            var previous = location;
            EmitWindowCall(call, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, update.Arity + 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitSavedRecordUpdate(update.Arity, field.Index);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(reply);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);

            Mark(other);
            owner.EmitLoggedReply(log, format, module, line, reply, previous, stateRegister: 1);

            return true;
        }
    }
}
