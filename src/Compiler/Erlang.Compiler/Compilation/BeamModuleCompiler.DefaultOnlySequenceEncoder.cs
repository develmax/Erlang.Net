using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record DefaultEncodeField(
            Expr.Call Predicate,
            int PredicateEntry,
            Expr.Call Encode,
            int EncodeEntry,
            Term Tags,
            Term Default,
            Term? Names = null
        );

        private bool TryEmitDefaultOnlySequenceEncoder(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Variable input, Pattern.Variable tags],
                    Body: Expr.Sequence
                    {
                        Items:
                                [
                                    Expr.Match { Pattern: Pattern.Tuple tuple, Value: Expr.Variable raw } extract,
                                    ..,
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable bytes,
                                    Value: Expr.List { Tail: null } pieces
                                },
                                    Expr.Match { Pattern: Pattern.Variable total, Value: Expr.Binary sum },
                                    Expr.Call
                                {
                                    Module: null, DynamicRemote: false,
                                    Arguments: [Expr.Variable outputTags, Expr.Variable outputBytes, Expr.Variable outputTotal]
                                } finish
                                ]
                    } sequence
                }
                ]
                || !DistinctBindings(
                    input,
                    tags,
                    bytes,
                    total
                ) || raw.Name != input.Name
                || outputTags.Name != tags.Name || outputBytes.Name != bytes.Name || outputTotal.Name != total.Name
                || !entries.TryGetValue((finish.Function, 3), out int finishEntry))
            {
                return false;
            }

            int count = sequence.Items.Count - 4;
            if (count < 3 || count + 2 >= FirstReservedXRegister || count + 1 != tuple.Items.Count
                || count != pieces.Items.Count || tuple.Items[0] is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
            {
                return false;
            }

            var fields = new DefaultEncodeField[count];
            var lengths = new string[count];
            var defined = new HashSet<string>(StringComparer.Ordinal) { input.Name, tags.Name, bytes.Name, total.Name };
            for (int index = 0; index < count; index++)
            {
                if (tuple.Items[index + 1] is not Pattern.Variable value
                    || sequence.Items[index + 1] is not Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable encoded, Pattern.Variable length] },
                        Value: Expr.Case decision
                    }
                    || pieces.Items[index] is not Expr.Variable piece || piece.Name != encoded.Name
                    || value.Name == VariableScopeNames.Wildcard || encoded.Name == VariableScopeNames.Wildcard
                    || length.Name == VariableScopeNames.Wildcard || !defined.Add(value.Name)
                    || !defined.Add(encoded.Name) || !defined.Add(length.Name)
                    || ReadDefaultEncodeField(decision, value.Name, allowNamedOptions: true) is not { } field)
                {
                    return false;
                }

                fields[index] = field;
                lengths[index] = length.Name;
            }

            var additions = new Expr.Binary[count - 1];
            Expr current = sum;
            for (int index = count - 1; index > 0; index--)
            {
                if (current is not Expr.Binary
                    {
                        Operator: ErlangOperators.Plus, Right: Expr.Variable length
                    } add || length.Name != lengths[index])
                {
                    return false;
                }

                current = add.Left;
                additions[index - 1] = add;
            }

            if (current is not Expr.Variable first || first.Name != lengths[0])
            {
                return false;
            }

            int frame = count + 1;
            // OTP numbers each field branch and its continuation before the
            // delayed input-match error block.
            var labels = Enumerable.Range(0, count).Select(_ => (Present: Label(), Ready: Label())).ToArray();
            int badInput = Label();
            owner.EmitTupleCheck(0, count + 1, badInput);
            EmitFrameAllocation(arity: 2, size: frame);
            EmitFrameInitialization(0, count - 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                count - 1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                count
            );
            BeamSourceLocation previous = location;
            for (int index = 0; index < count; index++)
            {
                var field = fields[index];
                bool last = index == count - 1;
                int slot = last ? count : count - index - 2;
                var (present, ready) = labels[index];
                Project(
                    index == 0 ? XRegisterTag : YRegisterTag,
                    index == 0 ? 0 : count,
                    index + 1,
                    YRegisterTag,
                    slot
                );
                owner.MoveRegister(
                    YRegisterTag,
                    slot,
                    XRegisterTag,
                    0
                );
                EmitLocal(field.Predicate, field.PredicateEntry, 1);
                owner.Op(ExactEqualTest, LabelTag, present);
                owner.Operand(XRegisterTag, 0);
                owner.LiteralOperand(Term.A(ErlangBooleanAtoms.True));
                owner.instructions.WriteByte(BeamEmissionFormat.Move);
                owner.LiteralOperand(field.Default);
                owner.Operand(last ? XRegisterTag : YRegisterTag, last ? 0 : slot);
                JumpTo(ready);
                Mark(present);
                if (field.Names is { } names)
                {
                    owner.EmitConstant(names, 1);
                }

                owner.EmitConstant(field.Tags, field.Names is null ? 1 : 2);
                owner.MoveRegister(
                    YRegisterTag,
                    slot,
                    XRegisterTag,
                    0
                );
                EmitFrameInitialization(slot, slot + 1);
                EmitLocal(field.Encode, field.EncodeEntry, field.Encode.Arguments.Count);
                if (!last)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        slot
                    );
                }

                Mark(ready);
            }

            EmitSequencePairResult(
                count,
                frame,
                finishEntry,
                additions,
                previous
            );

            Mark(badInput);
            previous = location;
            owner.EmitArithmeticLine(extract.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            void EmitLocal(Expr.Call call, int entry, int arity)
            {
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                owner.Op(LocalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(LabelTag, entry);
            }

            void Project(
                int sourceTag,
                int source,
                int field,
                int targetTag,
                int target
            )
            {
                owner.Op(GetTupleElement, sourceTag, source);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(targetTag, target);
            }

        }

        private void EmitSequencePairResult(
            int count,
            int frame,
            int finishEntry,
            IReadOnlyList<Expr.Binary> additions,
            BeamSourceLocation previous,
            IReadOnlyList<int>? pairSlots = null,
            int? outerTagSlot = null,
            int secondLengthType = NumberTypeBits,
            IReadOnlyList<int>? joinedLengthTypes = null,
            bool naturalLengths = false
        )
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, count * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            var bytesRegisters = new int[count];
            int next = 1;
            int firstLength = 0;
            int secondLength = 0;
            for (int index = count - 2; index >= 0; index--)
            {
                int resultSlot = pairSlots?[index] ?? count - 2 - index;
                bytesRegisters[index] = next;
                Project(
                    YRegisterTag,
                    resultSlot,
                    0,
                    XRegisterTag,
                    next
                );
                next++;
                if (index == 1)
                {
                    secondLength = next;
                }
                else if (index == 0)
                {
                    firstLength = next;
                }
                else
                {
                    continue;
                }

                Project(
                    YRegisterTag,
                    resultSlot,
                    1,
                    XRegisterTag,
                    next
                );
                next++;
            }

            bytesRegisters[^1] = next;
            Project(
                XRegisterTag,
                0,
                0,
                XRegisterTag,
                next
            );
            owner.Op(PutList, XRegisterTag, next);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, next);
            int list = next;
            for (int index = count - 2; index >= 0; index--)
            {
                owner.Op(PutList, XRegisterTag, bytesRegisters[index]);
                owner.Operand(XRegisterTag, list);
                owner.Operand(XRegisterTag, 1);
                list = 1;
            }

            AddLength(
                additions[0],
                firstLength,
                secondLength,
                live: next,
                secondType: joinedLengthTypes?[1] ?? secondLengthType,
                firstType: joinedLengthTypes?[0] ?? NumberTypeBits
            );
            for (int index = 2; index < count - 1; index++)
            {
                Project(
                    YRegisterTag,
                    pairSlots?[index] ?? count - 2 - index,
                    1,
                    XRegisterTag,
                    3
                );
                AddLength(
                    additions[index - 1],
                    2,
                    3,
                    live: 4,
                    secondType: joinedLengthTypes?[index] ?? NumberTypeBits
                );
            }

            Project(
                XRegisterTag,
                0,
                1,
                XRegisterTag,
                0
            );
            AddLength(
                additions[additions.Count - 1],
                2,
                0,
                live: 3,
                secondType: joinedLengthTypes?[^1] ?? NumberTypeBits
            );
            owner.MoveRegister(
                YRegisterTag,
                outerTagSlot ?? count - 1,
                XRegisterTag,
                0
            );
            EmitTailLocal(finishEntry, 3, frame);

            void Project(
                int sourceTag,
                int source,
                int field,
                int targetTag,
                int target
            )
            {
                owner.Op(GetTupleElement, sourceTag, source);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(targetTag, target);
            }

            void AddLength(
                Expr.Binary source,
                int firstArg,
                int secondArg,
                int live,
                int secondType = NumberTypeBits,
                int firstType = NumberTypeBits
            )
            {
                owner.EmitArithmeticLine(source.SourceLine, ref previous);
                if (naturalLengths)
                {
                    var length = new PairAdditionType(PairFacts.Length);
                    EmitPairAddition(
                        owner.Import(ErlangModuleNames.Module, ErlangOperators.Plus, 2),
                        live,
                        firstArg,
                        secondArg,
                        length,
                        length
                    );

                    return;
                }

                owner.GcArithmetic(
                    GcBifTwo,
                    ErlangOperators.Plus,
                    live,
                    2,
                    (XRegisterTag, firstArg, firstType),
                    (XRegisterTag, secondArg, secondType)
                );
            }
        }

        private DefaultEncodeField? ReadDefaultEncodeField(
            Expr.Case decision,
            string value,
            bool allowNamedOptions = false
        )
        {
            if (decision is not
                {
                    Value: Expr.Call
                    {
                        Module: null, DynamicRemote: false, Arguments: [Expr.Variable predicateInput]
                    } predicate,
                    Clauses:
                        [
                        {
                            Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } }],
                            Body: var omitted
                        },
                        {
                            Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: ErlangBooleanAtoms.False } }],
                            Body: Expr.Call
                            {
                                Module: null, DynamicRemote: false, Arguments: [Expr.Variable encoderInput, ..]
                            } encode
                        }
                        ]
                }
                || predicateInput.Name != value || encoderInput.Name != value
                || !entries.TryGetValue((predicate.Function, 1), out int predicateEntry)
                || !localFunctions.TryGetValue((predicate.Function, 1), out var predicateDefinition)
                || owner.IsDeclaredNative((predicate.Function, 1))
                || !predicateDefinition.Clauses.All(clause => ReturnsBoolean(clause.Body))
                || encode.Arguments.Count != 2 && !(allowNamedOptions && encode.Arguments.Count == 3)
                || !entries.TryGetValue((encode.Function, encode.Arguments.Count), out int encodeEntry)
                || !AttributeLiteral.TryNormalizeConstant(omitted, out Term absent)
                || absent is not TupleTerm { Items: [Nil, Integer] }
                || !AttributeLiteral.TryNormalizeConstant(encode.Arguments[^1], out Term tags)
                || !ProducesNumericPair(encode) && (!allowNamedOptions || !ReturnsTuple(
                    encode,
                    2,
                    [],
                    exact: true
                )))
            {
                return null;
            }

            Term? names = null;
            if (encode.Arguments.Count == 3
                && (!AttributeLiteral.TryNormalizeConstant(encode.Arguments[1], out names) || !IsClosedNamedNumberList(names)))
            {
                return null;
            }

            return new(
                predicate,
                predicateEntry,
                encode,
                encodeEntry,
                tags,
                absent,
                names
            );

            static bool ReturnsBoolean(Expr body) => IsBooleanResult(body)
                || body is Expr.Try { Clauses.Count: > 0 } attempt
                && attempt.Clauses.Concat(attempt.Catches).All(branch => IsBooleanResult(branch.Body));
        }
    }
}
