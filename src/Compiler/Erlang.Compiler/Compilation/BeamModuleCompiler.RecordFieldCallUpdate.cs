using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private static (Expr.Tuple Tuple, Expr.RecordUpdate Update, Expr.Call Call)? ReadEntryRecordCall(Expr body)
        {
            Pattern.Variable? result = null;
            Expr.Call? call = null;
            if (body is Expr.Sequence
                {
                    Items: [
                        Expr.Match { Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding, Value: Expr.Call producer },
                        Expr.Tuple returned
                    ]
                })
            {
                result = binding;
                call = producer;
                body = returned;
            }

            if (body is not Expr.Tuple { Items.Count: >= 2 } tuple || tuple.Items[^1] is not Expr.RecordUpdate
                { Value: Expr.Variable, Fields: [var field] } update)
            {
                return null;
            }

            if (result is null && field.Value is Expr.Call inline)
            {
                return (tuple, update, inline);
            }

            return result is not null && field.Value is Expr.Variable replacement && result.Name == replacement.Name
                ? (tuple, update, call!)
                : null;
        }

        private Action? ReadEntryRecordUpdate(
            Expr body,
            IReadOnlyList<Pattern> patterns,
            IEnumerable<(string Name, int Input, int Field)> projected
        )
        {
            if (ReadEntryRecordCall(body) is not { } entry
                || entry.Update.Value is not Expr.Variable state
                || body is Expr.Sequence { Items: [Expr.Match { Pattern: Pattern.Variable resultBinding }, _] }
                    && patterns.SelectMany(Semantics.Variables).Contains(resultBinding.Name, StringComparer.Ordinal)
                || !TryResolveOrdinaryCallTarget(entry.Call, out string? module))
            {
                return null;
            }

            var (returned, update, call) = entry;
            int fieldIndex = update.Fields[0].Index;

            int source = -1;
            for (int index = 0; index < patterns.Count; index++)
            {
                if (patterns[index] is AliasPattern alias && TryReadPatternAlias(alias, out var binding, out var shape)
                    && binding.Name == state.Name && shape is Pattern.Tuple tuple && tuple.Items.Count == update.Arity
                    && HasTupleTag(tuple, out Atom tag) && tag.Name == update.Name)
                {
                    source = index;
                    break;
                }
            }

            if (source < 0)
            {
                return null;
            }

            var prefix = new Term[returned.Items.Count - 1];
            for (int index = 0; index < prefix.Length; index++)
            {
                if (!AttributeLiteral.TryNormalizeConstant(returned.Items[index], out prefix[index]))
                {
                    return null;
                }
            }

            var fields = projected.ToDictionary(field => field.Name, StringComparer.Ordinal);
            for (int index = 0; index < patterns.Count; index++)
            {
                if (patterns[index] is Pattern.Variable { Name: not VariableScopeNames.Wildcard } input)
                {
                    fields.TryAdd(input.Name, (input.Name, index, -1));
                }
            }

            var arguments = new List<(int Destination, int Input, int Field, Term? Constant)>();
            var overwritten = new HashSet<int>();
            for (int index = call.Arguments.Count - 1; index >= 0; index--)
            {
                Expr argument = call.Arguments[index];
                if (argument is Expr.Variable variable && fields.TryGetValue(variable.Name, out var projection)
                    && !overwritten.Contains(projection.Input))
                {
                    arguments.Add((index, projection.Input, projection.Field, null));
                }
                else if (AttributeLiteral.TryNormalizeConstant(argument, out Term constant))
                {
                    arguments.Add((index, -1, -1, constant));
                }
                else
                {
                    return null;
                }

                overwritten.Add(index);
            }

            // An in-place projection may overwrite a register needed by a later clause.
            // The entry tests are complete by the time this body is emitted.
            return () =>
            {
                EmitFrameAllocation(Math.Max(source, arguments.Select(argument => argument.Input).DefaultIfEmpty(-1).Max()) + 1, 1);
                owner.MoveRegister(XRegisterTag, source, YRegisterTag, 0);
                foreach (var (destination, input, index, constant) in arguments)
                {
                    if (constant is not null)
                    {
                        owner.EmitConstant(constant, destination);
                    }
                    else if (index >= 0)
                    {
                        owner.EmitTuplePart(input, index, destination);
                    }
                    else
                    {
                        owner.MoveRegister(XRegisterTag, input, XRegisterTag, destination);
                    }
                }

                owner.EmitCallLine(call, location);
                Call(module, call.Function, call.Arguments.Count);
                owner.Op(TestHeap, BeamWire.UnsignedTag, update.Arity + prefix.Length + 3);
                owner.Operand(BeamWire.UnsignedTag, 1);
                EmitSavedRecordUpdate(update.Arity, fieldIndex);
                EmitRecordUpdateWrapper(prefix);
                owner.EmitFrameReturn(1);
            };
        }


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

        private bool TryEmitRecordFieldCallAndReturn(FunctionDefinition function)
        {
            if (function.Clauses.Count is < 1 or > 2
                || function.Clauses[^1] is not { Guard: null, Body: Expr.Sequence { Items: [
                Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                    Expr.RecordAccess { Value: Expr.Variable source } field,
                    Expr.Literal { Value: var format },
                    Expr.List { Tail: null, Items: [Expr.Variable item] }
                ] } call,
                Expr.Tuple { Items: [.., Expr.Variable retained] } reply
            ] } } clause
                || !HasUnconditionalParameters(clause)
                || !TryResolveOrdinaryCallTarget(call, out _)
                || source.Name != retained.Name || source.Name == item.Name
                || source.Name == VariableScopeNames.Wildcard || item.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            var prefix = new List<Term>();
            foreach (Expr value in reply.Items.Take(reply.Items.Count - 1))
            {
                if (!AttributeLiteral.TryNormalizeConstant(value, out Term constant))
                {
                    return false;
                }

                prefix.Add(constant);
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            int record = Array.IndexOf(parameters, source.Name);
            int element = Array.IndexOf(parameters, item.Name);
            if (record < 0 || element < 0)
            {
                return false;
            }

            int failure;
            if (function.Clauses.Count == 2)
            {
                if (!TryEmitRetainedRecordCallAlternative(function.Clauses[0], record, element, field, out failure))
                {
                    return false;
                }
            }
            else
            {
                failure = Label();
            }

            EmitDetailRecordTest(record, field.Arity, Term.A(field.Name), failure);
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, Math.Max(record, element) + 1);
            owner.MoveRegister(XRegisterTag, record, YRegisterTag, 0);
            owner.Op(PutList, XRegisterTag, element);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 2);
            int recordTag = record == 2 ? YRegisterTag : XRegisterTag;
            owner.Op(GetTupleElement, recordTag, record == 2 ? 0 : record);
            owner.Operand(BeamWire.UnsignedTag, field.Index);
            owner.Operand(XRegisterTag, 0);
            owner.EmitConstant(format, 1);
            owner.EmitCallLine(call, location);
            owner.EmitStaticRemoteCall(call);
            EmitRetainedRecordReply(prefix);
            Mark(failure);
            owner.Op(BadRecord, XRegisterTag, record);

            return true;
        }

        private bool TryEmitRetainedRecordCallAlternative(
            Clause clause,
            int record,
            int selector,
            Expr.RecordAccess field,
            out int failure
        )
        {
            failure = 0;
            if (clause.Guard is not null || clause.Patterns[selector] is not Pattern.Literal { Value: Atom key }
                || clause.Patterns[record] is not AliasPattern alias
                || !TryReadPatternAlias(alias, out var retained, out var shape)
                || shape is not Pattern.Tuple tuple || tuple.Items.Count != field.Arity
                || tuple.Items[0] is not Pattern.Literal { Value: Atom tag } || tag.Name != field.Name
                || clause.Body is not Expr.Sequence { Items: [
                    Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable input] } call,
                    Expr.Tuple { Items: [.., Expr.Variable output] } reply
                ] } || output.Name != retained.Name || !TryResolveOrdinaryCallTarget(call, out _)
                || retained.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            var names = new HashSet<string>(StringComparer.Ordinal) { retained.Name, input.Name };
            if (names.Count != 2 || clause.Patterns.Where((_, index) => index != record && index != selector)
                .Any(pattern => pattern is not Pattern.Variable variable
                    || variable.Name != VariableScopeNames.Wildcard && !names.Add(variable.Name)))
            {
                return false;
            }

            int index = -1;
            for (int part = 1; part < tuple.Items.Count; part++)
            {
                if (tuple.Items[part] is not Pattern.Variable variable)
                {
                    return false;
                }

                if (variable.Name == VariableScopeNames.Wildcard)
                {
                    continue;
                }

                if (index >= 0 || variable.Name != input.Name || variable.Name == retained.Name)
                {
                    return false;
                }

                index = part;
            }

            var prefix = new List<Term>();
            foreach (Expr value in reply.Items.Take(reply.Items.Count - 1))
            {
                if (!AttributeLiteral.TryNormalizeConstant(value, out Term constant))
                {
                    return false;
                }

                prefix.Add(constant);
            }

            if (index < 0)
            {
                return false;
            }

            int fallback = Label();
            failure = Label();
            owner.Op(ExactEqualTest, LabelTag, fallback);
            owner.Operand(XRegisterTag, selector);
            owner.LiteralOperand(key);
            EmitDetailRecordTest(record, field.Arity, tag, failure);
            EmitFrameAllocation(record + 1, 1);
            owner.MoveRegister(XRegisterTag, record, YRegisterTag, 0);
            owner.EmitTuplePart(record, index, 0);
            owner.EmitCallLine(call, location);
            owner.EmitStaticRemoteCall(call);
            EmitRetainedRecordReply(prefix);
            Mark(fallback);

            return true;
        }

        private void EmitRetainedRecordReply(IReadOnlyList<Term> prefix)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, prefix.Count + 2);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTupleHeader(XRegisterTag, prefix.Count + 1);
            foreach (Term constant in prefix)
            {
                owner.LiteralOperand(constant);
            }
            owner.Operand(YRegisterTag, 0);
            owner.EmitFrameReturn(1);
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
