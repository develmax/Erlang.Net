using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record DefaultSequenceField(
            Integer Tag,
            Expr.Call Decoder,
            int Entry,
            Term Tags,
            Term Default,
            SequenceRead? Rename = null,
            Expr.Call? Convert = null,
            string? Module = null,
            TaggedBranchRange? Bounds = null,
            Term? MiddleArgument = null
        );

        private sealed record OptionalAliasField(Term Default, IReadOnlyList<(Integer Tag, DecodeCall Decode)> Arms);

        private bool TryEmitDefaultOnlySequenceDecoder(FunctionDefinition function)
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
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable initial,
                                    Value: Expr.Call
                                    {
                                        Module: null, DynamicRemote: false,
                                        Arguments: [Expr.Variable source, Expr.Variable tagInput]
                                    } matcher
                                },
                                    ..,
                                    Expr.Case extra,
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable result,
                                    Value: Expr.Tuple { Items: [Expr.Literal { Value: Atom recordTag }, ..] } resultTuple
                                },
                                    Expr.Variable returned
                                ]
                    } sequence
                }
                ]
                || !DistinctBindings(
                    input,
                    tags,
                    initial,
                    result
                )
                || source.Name != input.Name || tagInput.Name != tags.Name || result.Name != returned.Name
                || !entries.TryGetValue((matcher.Function, 2), out int matchEntry))
            {
                return false;
            }

            int count = sequence.Items.Count - 4;
            if (count < 2 || count + 1 != resultTuple.Items.Count || count + 1 >= FirstReservedXRegister)
            {
                return false;
            }

            var plans = new (DefaultSequenceField? Direct, OptionalAliasField? Aliased)[count];
            var defined = new HashSet<string>(StringComparer.Ordinal) { input.Name, tags.Name, initial.Name, result.Name };
            string previous = initial.Name;
            for (int index = 0; index < count; index++)
            {
                if (sequence.Items[index + 1] is not Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable field, Pattern.Variable rest] },
                        Value: Expr.Case choice
                    }
                    || resultTuple.Items[index + 1] is not Expr.Variable value || value.Name != field.Name
                    || field.Name == VariableScopeNames.Wildcard || rest.Name == VariableScopeNames.Wildcard
                    || !defined.Add(field.Name) || !defined.Add(rest.Name))
                {
                    return false;
                }

                var plan = ReadDefaultSequenceField(
                    choice,
                    previous,
                    defined,
                    allowConversion: true,
                    allowRemote: true,
                    allowLiteralMiddleArgument: true
                ) ?? ReadBoundedTaggedSequenceField(choice, previous, defined);
                OptionalAliasField? aliased = null;
                if (plan is null)
                {
                    if (ReadNamedDefault(choice, previous, defined) is { } named)
                    {
                        plan = named.Decoder with { Rename = named.Convert };
                    }
                    else if (ReadOptionalAliasField(choice, previous, defined) is { } aliases)
                    {
                        aliased = aliases;
                    }
                    else
                    {
                        return false;
                    }
                }

                plans[index] = (plan, aliased);
                previous = rest.Name;
            }

            if (ReadTableUnexpectedFields(extra, previous) is not { } unexpected)
            {
                return false;
            }

            int frame = plans.Length + 1;
            BeamSourceLocation previousLine = location;
            EmitFrameAllocation(arity: 2, size: frame);
            EmitFrameInitialization(0, frame);
            EmitLocal(matcher, matchEntry);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                plans.Length
            );

            for (int index = 0; index < plans.Length; index++)
            {
                var (direct, aliased) = plans[index];
                int slot = plans.Length - index;
                int sourceTag = index == 0 ? XRegisterTag : YRegisterTag;
                int sourceIndex = index == 0 ? 0 : slot;
                bool last = index == plans.Length - 1;
                int invalid = direct?.Bounds is null ? 0 : Label();
                var branchLabels = aliased?.Arms.Select(_ => Label()).ToArray();
                int absent = Label();
                int ready = Label();

                owner.Op(IsNonemptyList, LabelTag, absent);
                owner.Operand(sourceTag, sourceIndex);
                owner.Op(GetList, sourceTag, sourceIndex);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(YRegisterTag, slot - 1);
                owner.EmitTupleCheck(0, 2, absent);
                owner.EmitTuplePart(0, 0, 1);
                if (aliased is not null)
                {
                    EmitOptionalAliasDispatch(
                        aliased,
                        branchLabels!,
                        slot,
                        last,
                        absent,
                        ready,
                        ref previousLine
                    );
                }
                else
                {
                    var plan = direct!;
                    owner.Op(ExactEqualTest, LabelTag, absent);
                    owner.Operand(XRegisterTag, 1);
                    owner.LiteralOperand(plan.Tag);
                    owner.EmitTuplePart(0, 1, 0);
                    EmitOptionalFieldArguments(plan.Tags, plan.MiddleArgument);
                    EmitFrameInitialization(slot, slot + 1);
                    EmitSequenceDecoderCall(
                        plan.Decoder,
                        plan.Module,
                        plan.Entry,
                        ref previousLine
                    );
                    if (plan.Convert is { } convert)
                    {
                        owner.EmitCallLine(convert, previousLine);
                        previousLine = CallLocation(convert, previousLine);
                        Call(convert.Module, convert.Function, 1);
                    }

                    if (plan.Rename is { } named)
                    {
                        owner.EmitConstant(named.Options, 1);
                        EmitLocal(named.Call, named.Entry);
                    }

                    if (plan.Bounds is { } bounds)
                    {
                        EmitRangeTests(
                            plan.Decoder,
                            bounds,
                            invalid,
                            provenInteger: true
                        );
                    }

                    EmitOptionalJoinResult(slot, last, ready);
                    if (plan.Bounds is { } rejected)
                    {
                        Mark(invalid);
                        var exitLine = CallLocation(rejected.Exit, previousLine);
                        EmitRangeExit(rejected, exitLine == previousLine ? null : exitLine, frame);
                    }
                }

                Mark(absent);
                int destinationTag = last ? XRegisterTag : YRegisterTag;
                int rest = last ? 1 : slot - 1;
                owner.MoveRegister(
                    YRegisterTag,
                    slot,
                    destinationTag,
                    rest
                );
                owner.instructions.WriteByte(BeamEmissionFormat.Move);
                owner.LiteralOperand(aliased?.Default ?? direct!.Default);
                owner.Operand(destinationTag, last ? 0 : slot);
                Mark(ready);
            }

            int failure = Label();
            owner.EmitNilTest(XRegisterTag, 1, failure);
            owner.Op(TestHeap, BeamWire.UnsignedTag, plans.Length + 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, plans.Length + 1);
            owner.LiteralOperand(recordTag);
            for (int slot = plans.Length; slot > 1; slot--)
            {
                owner.Operand(YRegisterTag, slot);
            }

            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame);

            Mark(failure);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 9);
            owner.Operand(BeamWire.UnsignedTag, 2);
            TaggedPair(unexpected.UnexpectedTag, 1);
            TaggedPair(unexpected.DetailsTag, 0);
            TaggedPair(unexpected.ErrorTag, 0);
            owner.EmitCallLine(unexpected.Exit, previousLine);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, unexpected.Exit.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, frame);

            return true;

            void EmitLocal(Expr.Call call, int entry)
            {
                owner.EmitCallLine(call, previousLine);
                previousLine = CallLocation(call, previousLine);
                owner.Op(LocalCall, BeamWire.UnsignedTag, 2);
                owner.Operand(LabelTag, entry);
            }

            void TaggedPair(Atom tag, int register)
            {
                owner.EmitTupleHeader(XRegisterTag, 2);
                owner.LiteralOperand(tag);
                owner.Operand(XRegisterTag, register);
            }
        }

        private OptionalAliasField? ReadOptionalAliasField(
            Expr.Case choice,
            string previous,
            IReadOnlySet<string> bindings
        )
        {
            if (choice.Value is not Expr.Variable tested || tested.Name != previous || choice.Clauses.Count < 3
                || choice.Clauses[^1] is not
                {
                    Guard: null,
                    Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: Expr.Tuple { Items: [var fallback, Expr.Variable unchanged] }
                }
                || unchanged.Name != previous
                || !AttributeLiteral.TryNormalizeConstant(fallback, out Term defaultValue))
            {
                return null;
            }

            var tags = new HashSet<Integer>();
            var lines = new HashSet<BeamSourceLocation>();
            var arms = new List<(Integer Tag, DecodeCall Decode)>();
            foreach (var clause in choice.Clauses.Take(choice.Clauses.Count - 1))
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns: [Pattern.List { Items: [AliasPattern alias], Tail: Pattern.Variable tail }],
                        Body: Expr.Tuple { Items: [var expression, Expr.Variable remaining] }
                    }
                    || !TryReadPatternAlias(alias, out var payload, out var shape)
                    || shape is not Pattern.Tuple
                    {
                        Items: [Pattern.Literal { Value: Integer tag }, Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                    }
                    || remaining.Name != tail.Name || !DistinctBindings(payload, tail)
                    || bindings.Contains(payload.Name) || bindings.Contains(tail.Name)
                    || ReadDecodeCall(expression, payload.Name, allowRemote: true) is not { Convert: null } decode
                    || !tags.Add(tag) || !lines.Add(CallLocation(decode.Decode, location)))
                {
                    return null;
                }

                arms.Add((tag, decode));
            }

            return new(defaultValue, arms.OrderByDescending(arm => arm.Tag.Value).ToArray());
        }

        private void EmitOptionalAliasDispatch(
            OptionalAliasField field,
            int[] labels,
            int slot,
            bool last,
            int absent,
            int ready,
            ref BeamSourceLocation previous
        )
        {
            owner.Op(SelectValue, XRegisterTag, 1);
            owner.Operand(LabelTag, absent);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, field.Arms.Count * 2);
            for (int index = field.Arms.Count - 1; index >= 0; index--)
            {
                owner.LiteralOperand(field.Arms[index].Tag);
                owner.Operand(LabelTag, labels[index]);
            }

            for (int index = 0; index < field.Arms.Count; index++)
            {
                var (_, decode) = field.Arms[index];
                Mark(labels[index]);
                owner.EmitConstant(decode.Tags, 1);
                EmitFrameInitialization(slot, slot + 1);
                EmitSequenceDecoderCall(
                    decode.Decode,
                    decode.Module,
                    decode.Entry,
                    ref previous
                );
                EmitOptionalJoinResult(slot, last, ready);
            }
        }

        private void EmitOptionalJoinResult(int slot, bool last, int ready)
        {
            if (last)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    0,
                    XRegisterTag,
                    1
                );
            }
            else
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    slot
                );
            }

            JumpTo(ready);
        }

        private DefaultSequenceField? ReadBoundedTaggedSequenceField(
            Expr.Case choice,
            string previous,
            IReadOnlySet<string> bindings
        )
        {
            if (choice.Clauses is not
                [
                {
                    Patterns: [Pattern.List
                    {
                        Items: [Pattern.Tuple { Items: [_, Pattern.Variable member] }],
                        Tail: Pattern.Variable tail
                    }],
                    Body: Expr.Tuple { Items: [var checkedValue, var rest] } result
                } arm,
                    var fallback
                ]
                || ReadSequenceBounds(
                    checkedValue,
                    member.Name,
                    bindings,
                    tail.Name
                ) is not { } bounded
                || !IsDecoderIntrinsic(bounded.Bounds.Exit)
                || ReadDefaultSequenceField(choice with
                {
                    Clauses = [arm with { Body = result with { Items = [bounded.Read.Call, rest] } }, fallback]
                }, previous, bindings) is not { } tagged)
            {
                return null;
            }

            return tagged with { Bounds = bounded.Bounds };
        }

        private void EmitOptionalFieldArguments(Term options, Term? middle)
        {
            if (middle is null)
            {
                owner.EmitConstant(options, 1);

                return;
            }

            owner.EmitConstant(options, 2);
            owner.EmitConstant(middle, 1);
        }

        private void EmitSequenceDecoderCall(
            Expr.Call call,
            string? module,
            int entry,
            ref BeamSourceLocation previous
        )
        {
            owner.EmitCallLine(call, previous);
            previous = CallLocation(call, previous);
            int arity = call.Arguments.Count;
            if (module is null)
            {
                owner.EmitFieldCall(arity, entry);
            }
            else
            {
                Call(module, call.Function, arity);
            }
        }

        private DefaultSequenceField? ReadDefaultSequenceField(
            Expr.Case choice,
            string previous,
            IReadOnlySet<string> defined,
            bool allowConversion = false,
            bool allowRemote = false,
            bool allowLiteralMiddleArgument = false
        )
        {
            if (choice is not
                {
                    Value: Expr.Variable tested,
                    Clauses:
                        [
                        {
                            Guard: null,
                            Patterns:
                                    [
                                        Pattern.List
                                    {
                                        Items: [Pattern.Tuple { Items: [Pattern.Literal { Value: Integer tag }, Pattern.Variable payload] }],
                                        Tail: Pattern.Variable tail
                                    }
                                    ],
                            Body: Expr.Tuple
                            {
                                Items:
                                        [
                                            var body,
                                            Expr.Variable remaining
                                        ]
                            }
                        },
                        {
                            Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                            Body: Expr.Tuple { Items: [var defaultValue, Expr.Variable unchanged] }
                        }
                        ]
                }
                || tested.Name != previous || unchanged.Name != previous
                || remaining.Name != tail.Name || !DistinctBindings(payload, tail)
                || defined.Contains(payload.Name) || defined.Contains(tail.Name)
                || !AttributeLiteral.TryNormalizeConstant(defaultValue, out Term fallback))
            {
                return null;
            }

            if (allowLiteralMiddleArgument && body is Expr.Call
                {
                    Module: null, DynamicRemote: false, Arguments: [Expr.Variable data, var middle, var options]
                } literalCall
                && data.Name == payload.Name
                && entries.TryGetValue((literalCall.Function, literalCall.Arguments.Count), out int literalEntry)
                && AttributeLiteral.TryNormalizeConstant(middle, out Term middleArgument)
                && AttributeLiteral.TryNormalizeConstant(options, out Term finalArgument))
            {
                return new(
                    tag,
                    literalCall,
                    literalEntry,
                    finalArgument,
                    fallback,
                    MiddleArgument: middleArgument
                );
            }

            if (ReadDecodeCall(body, payload.Name, allowRemote) is not { } decoder
                || decoder.Convert is not null && !allowConversion)
            {
                return null;
            }

            return new(
                tag,
                decoder.Decode,
                decoder.Entry,
                decoder.Tags,
                fallback,
                Convert: decoder.Convert,
                Module: decoder.Module
            );
        }
    }
}
