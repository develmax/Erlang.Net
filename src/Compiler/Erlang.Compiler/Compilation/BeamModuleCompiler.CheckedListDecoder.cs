using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record CheckedFieldSize(
            Expr.Call Measure,
            Integer Count,
            Expr.Call Failure,
            Term Reason
        );

        private sealed record CheckedSequenceDecode(
            Expr.Match Split,
            DecodeCall Call,
            CheckedFieldSize? Size,
            TaggedBranchRange? Range,
            SequenceRead? Rename,
            IntegerCase? Enumeration
        );

        private bool TryEmitCheckedListFields(
            IReadOnlyList<Expr> items,
            Pattern.Variable first,
            Pattern.Variable second,
            Pattern.Variable list,
            Expr.Call read
        )
        {
            if (items is not [_, .., var tailCheck,
                Expr.Match
                {
                    Pattern: Pattern.Variable result, Value: Expr.Tuple
                    { Items: [Expr.Literal { Value: Atom tag }, ..] } tuple
                },
                Expr.Variable returned]
                || tuple.Items.Count < 2
                || !DistinctBindings(
                    first,
                    second,
                    list,
                    result
                )
                || returned.Name != result.Name)
            {
                return false;
            }

            bool canSpecialize = owner.SsaTypeInferenceEnabled && !owner.ModuleOptimizationDisabled;
            int optionalCount = tuple.Items.Count * 2 + 2 - items.Count;
            bool hasOptionalField = optionalCount != 0;
            int requiredCount = tuple.Items.Count - 1 - optionalCount;
            if (requiredCount < 1 || optionalCount < 0 || tuple.Items.Count >= FirstReservedXRegister
                || optionalCount > 2 && !canSpecialize
                || hasOptionalField && !canSpecialize && (optionalCount != 1 || tuple.Items.Count != 4))
            {
                return false;
            }

            var bound = new HashSet<string>(StringComparer.Ordinal) { first.Name, second.Name, list.Name, result.Name };
            var fields = new List<CheckedSequenceDecode>();
            string current = list.Name;
            foreach (Expr member in tuple.Items.Skip(1).Take(requiredCount))
            {
                int index = 1 + fields.Count * 2;
                if (items[index] is not Expr.Match
                    {
                        Pattern: Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable rest },
                        Value: Expr.Variable source
                    } split
                    || source.Name != current
                    || items[index + 1] is not Expr.Match { Pattern: Pattern.Variable value, Value: var body }
                    || member is not Expr.Variable field
                    || field.Name != value.Name
                    || !DistinctBindings(head, rest, value)
                    || !bound.Add(head.Name)
                    || !bound.Add(rest.Name)
                    || !bound.Add(value.Name))
                {
                    return false;
                }

                var size = ReadCheckedFieldSize(body, bound, out Expr decoderBody);
                TaggedBranchRange? range = null;
                SequenceRead? rename = null;
                IntegerCase? enumeration = null;
                if (canSpecialize && size is null && ReadSequenceBounds(body, head.Name, bound) is { } bounded)
                {
                    decoderBody = bounded.Read.Call;
                    range = bounded.Bounds;
                }
                else if (canSpecialize && size is null && ReadSequenceNamedValue(body, head.Name, bound) is { } named)
                {
                    decoderBody = named.Decode.Call;
                    rename = named.Rename;
                }

                bool finalEnumAfterForeign = requiredCount == 3 && optionalCount == 3 && fields.Count == 2
                    && fields[0].Call is { Module: not null, Tags: Nil, Convert: null }
                    && fields[1].Call is { Module: null, Convert: null };
                if (canSpecialize && hasOptionalField && (requiredCount == 1 || finalEnumAfterForeign) && size is null
                    && range is null && rename is null
                    && ReadIntegerCase(body, name => !bound.Contains(name)) is { } integerCase
                    && (!finalEnumAfterForeign || IsDecoderIntrinsic(integerCase.Failure)
                        && (InferPairFacts(integerCase.Decode) & PairFacts.Integer) != 0))
                {
                    decoderBody = integerCase.Decode;
                    enumeration = integerCase;
                }

                if (ReadDecodeCall(decoderBody, head.Name, allowRemote: canSpecialize) is not { } decoder
                    || decoder.Convert is not null && (!canSpecialize || size is not null || range is not null || rename is not null))
                {
                    return false;
                }

                fields.Add(new(
                    split,
                    decoder,
                    size,
                    range,
                    rename,
                    enumeration
                ));
                current = rest.Name;
            }

            bool namedRequiredPrefix = fields[0].Rename is not null;
            bool foreignBeforeChoices = optionalCount == 3 && fields is
                [{ Call: { Module: not null, Tags: Nil, Convert: null }, Size: null, Range: null, Rename: null, Enumeration: null }];
            bool foreignBeforeRequiredEnum = optionalCount == 3 && fields is
                [
                { Call: { Module: not null, Tags: Nil, Convert: null }, Size: null, Range: null, Rename: null },
                { Call: { Module: null, Convert: null }, Size: null, Range: null, Rename: null },
                { Call: { Module: null, Convert: null }, Enumeration: not null }
                ];
            if (optionalCount > 2 && !namedRequiredPrefix && !foreignBeforeChoices && !foreignBeforeRequiredEnum
                || optionalCount > 2 && namedRequiredPrefix && requiredCount < 2)
            {
                return false;
            }

            int sizeCheckCount = fields.Count(field => field.Size is not null);
            if (sizeCheckCount != 0 && (hasOptionalField || fields.Count != 2))
            {
                return false;
            }

            var optionals = new List<OptionalDecode>();
            for (int index = 0; index < optionalCount; index++)
            {
                if (items[1 + requiredCount * 2 + index] is not Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable member, Pattern.Variable outputTail] },
                        Value: Expr.Case decision
                    }
                    || tuple.Items[1 + requiredCount + index] is not Expr.Variable optionalResult
                    || optionalResult.Name != member.Name
                    || !DistinctBindings(member, outputTail)
                    || !bound.Add(member.Name)
                    || !bound.Add(outputTail.Name)
                    || (ReadOptionalDecode(
                        decision,
                        current,
                        bound,
                        allowEnumeration: canSpecialize && optionalCount == 1 && requiredCount == 1,
                        allowNamedOptions: canSpecialize && (namedRequiredPrefix && optionalCount == 2
                            || foreignBeforeChoices && index == optionalCount - 1)
                    ) ?? (canSpecialize ? ReadWholeOptionalChoice(decision, current, bound) : null)) is not { } field
                    || field.Call.Convert is not null && !canSpecialize)
                {
                    return false;
                }

                optionals.Add(field);
                current = outputTail.Name;
            }

            if (foreignBeforeChoices && (optionals is not
                [
                { Choices: { Count: > 1 } foreignChoices, Call: var firstChoice },
                { Call: { Module: null, Convert: not null }, Choices: null, Enumeration: null },
                { Call: { Module: null, Names: not null, Convert: null }, Choices: null, Enumeration: null }
                ] || firstChoice.Module is null || foreignChoices.Any(choice => !choice.Whole
                    || choice.Call is not { Tags: Nil, Convert: null }
                    || choice.Call.Module != firstChoice.Module || choice.Call.Decode.Function != firstChoice.Decode.Function))
                || foreignBeforeRequiredEnum && optionals.Any(optional => optional is not
                { Call.Module: null, Choices: null, Enumeration: null }))
            {
                return false;
            }

            if (ReadCheckedDecoderTail(tailCheck, current) is not { } failure)
            {
                return false;
            }

            int frame = tuple.Items.Count == 2 ? 1 : tuple.Items.Count;
            var casePlan = fields.Select(field => field.Enumeration).FirstOrDefault(plan => plan is not null);
            var caseSchedule = casePlan is null ? null : ScheduleIntegerChoices(casePlan);
            int caseJoined = caseSchedule is null ? 0 : Label();
            var optionalEnum = optionals.Count == 1 ? optionals[0].Enumeration : null;
            var optionalChoices = optionalEnum is null ? null : ScheduleIntegerChoices(optionalEnum);
            int optionalFailure = optionalChoices is null ? 0 : Label();
            var optionalLabels = optionals.Select(optional =>
            {
                var choiceLabels = optional.Choices?.Select(choice => (Choice: choice, Label: Label())).ToArray();

                return (Choices: choiceLabels, Missing: Label(), Joined: Label());
            }).ToArray();
            int exit = Label();
            int caseFailure = caseSchedule is null ? 0 : Label();
            int checkCount = sizeCheckCount + fields.Count(field => field.Range is not null);
            int nextSizeFailure = owner.nextLabel + checkCount - 1;
            int firstFailure = owner.nextLabel + fields.Count - 1 + checkCount;
            owner.nextLabel = firstFailure + 1;
            var failures = new List<(int Label, int? Slot, Expr.Match Match)>();
            var results = new List<int>();
            EmitGeneralFrame(2, slots: frame);
            owner.EmitCallLine(read, location);
            Call(read.Module, read.Function, 2);

            var previous = CallLocation(read, location);
            int? sourceSlot = null;
            for (int index = 0; index < fields.Count; index++)
            {
                var (split, decoder, size, range, rename, enumeration) = fields[index];
                int nextTail = index == 0 ? frame - 1 : frame - index - 2;
                int failureLabel = firstFailure - index;
                int sourceTag = sourceSlot.HasValue ? YRegisterTag : XRegisterTag;
                int sourceRegister = sourceSlot.GetValueOrDefault();
                failures.Add((failureLabel, sourceSlot, split));

                owner.Op(IsNonemptyList, LabelTag, failureLabel);
                owner.Operand(sourceTag, sourceRegister);
                owner.Op(GetList, sourceTag, sourceRegister);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(YRegisterTag, nextTail);
                owner.EmitConstant(decoder.Tags, 1);
                if (sourceSlot is int dead)
                {
                    owner.InitStackSlot(dead);
                }

                EmitDecodeInvocation(decoder, previous);
                if (rename is { } named)
                {
                    owner.EmitConstant(named.Options, 1);
                    owner.EmitCallLine(named.Call, CallLocation(decoder.Decode, previous));
                    owner.EmitFieldCall(2, named.Entry);
                }

                if (size is not null)
                {
                    EmitCheckedFieldMeasurement(size.Measure, CallLocation(decoder.Decode, previous));
                }

                if (hasOptionalField || index < fields.Count - 1)
                {
                    // The first split reserves two Y homes. Each later decoder
                    // can reuse the preceding tail slot once its list is split.
                    int resultSlot = sourceSlot ?? frame - 2;
                    if (enumeration is not null)
                    {
                        EmitIntegerSelect(caseSchedule!, caseFailure);
                        for (int branch = 0; branch < caseSchedule!.Length; branch++)
                        {
                            var item = caseSchedule[branch];
                            Mark(item.Label);
                            owner.EmitConstant(item.Value, resultSlot, YRegisterTag);
                            if (branch + 1 < caseSchedule.Length)
                            {
                                JumpTo(caseJoined);
                            }
                        }

                        Mark(caseJoined);
                    }
                    else
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            0,
                            YRegisterTag,
                            resultSlot
                        );
                    }

                    results.Add(resultSlot);
                }

                if (size is not null)
                {
                    owner.Op(ExactEqualTest, LabelTag, nextSizeFailure--);
                    owner.EmitTypedRegister(
                        1,
                        IntegerTypeBits,
                        0,
                        MaximumBitSize
                    );
                    owner.LiteralOperand(size.Count);
                }
                else if (range is not null)
                {
                    EmitRangeTests(decoder.Decode, range, nextSizeFailure--);
                }

                sourceSlot = nextTail;
            }

            int tail = sourceSlot.GetValueOrDefault();
            int tailTag = YRegisterTag;
            for (int index = 0; index < optionals.Count; index++)
            {
                bool last = index + 1 == optionals.Count;
                var labels = optionalLabels[index];
                var optional = optionals[index];
                int nextTail = last ? 0 : tail - (index == 0 && fields.Count == 1 ? 2 : 1);
                if (labels.Choices is { } choices)
                {
                    EmitTaggedOptionalChoices(
                        optional,
                        tail,
                        nextTail,
                        last,
                        labels.Missing,
                        labels.Joined,
                        choices,
                        previous
                    );
                }
                else if (optionalChoices is null)
                {
                    EmitCheckedOptionalField(
                        optional,
                        tail,
                        labels.Missing,
                        labels.Joined,
                        previous,
                        saveOptionalInInputSlot: !last,
                        keepTailInFrame: !last,
                        remainingTailSlot: nextTail
                    );
                }
                else
                {
                    EmitEnumeratedCheck(
                        optional,
                        tail,
                        (labels.Missing, labels.Joined),
                        optionalChoices,
                        optionalFailure,
                        frame,
                        previous
                    );
                }

                if (last)
                {
                    tail = 1;
                    tailTag = XRegisterTag;
                }
                else
                {
                    results.Add(tail);
                    tail = nextTail;
                }
            }

            owner.Op(IsNil, LabelTag, exit);
            owner.Operand(tailTag, tail);
            owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
            owner.LiteralOperand(tag);
            foreach (int slot in results)
            {
                owner.Operand(YRegisterTag, slot);
            }

            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame);
            Mark(exit);
            EmitCheckedDecoderTailExit(
                failure.Call,
                failure.Tags,
                tailTag,
                tail,
                frame
            );
            if (casePlan is not null)
            {
                Mark(caseFailure);
                EmitTaggedExit(
                    casePlan.Failure,
                    casePlan.Tags,
                    CallLocation(casePlan.Decode, previous),
                    framed: true,
                    frameSlots: frame
                );
            }

            foreach (var field in fields.AsEnumerable().Reverse())
            {
                if (field.Size is { } size)
                {
                    Mark(++nextSizeFailure);
                    owner.EmitConstant(size.Reason, 0);
                    owner.EmitCallLine(size.Failure, location);
                    EmitCheckedDecoderExit(size.Failure, frame);
                }
                else if (field.Range is { } range)
                {
                    Mark(++nextSizeFailure);
                    EmitRangeExit(range, CallLocation(range.Exit, location), frame);
                }
            }

            EmitDecoderFailures(failures);

            return true;
        }

        private (SequenceRead Decode, SequenceRead Rename)? ReadSequenceNamedValue(
            Expr expression,
            string input,
            HashSet<string> reserved
        )
        {
            if (expression is not Expr.Block
                {
                    Body: Expr.Sequence
                    {
                        Items: [Expr.Match { Pattern: Pattern.Variable value, Value: var read }, var rename]
                    }
                }
                || !DistinctBindings(value) || value.Name == input || !reserved.Add(value.Name)
                || ReadSequenceCall(read, input) is not { } decoder
                || ReadSequenceCall(rename, value.Name) is not { } conversion
                || !IsClosedNamedNumberList(conversion.Options))
            {
                return null;
            }

            return (decoder, conversion);
        }

        private CheckedFieldSize? ReadCheckedFieldSize(Expr expression, HashSet<string> bound, out Expr decoder)
        {
            decoder = expression;
            if (expression is not Expr.Block
                {
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match { Pattern: Pattern.Variable value, Value: var body },
                            Expr.Match
                        {
                            Pattern: Pattern.Variable measured,
                            Value: Expr.Call
                            {
                                DynamicRemote: false,
                                Function: AutoImportNames.ByteSize,
                                Arguments: [Expr.Variable source]
                            } measure
                        },
                            Expr.If
                        {
                            Clauses:
                                [
                                {
                                    Guard: Expr.Binary
                                    {
                                        Operator: ErlangOperators.ExactEqual,
                                        Left: Expr.Variable size,
                                        Right: Expr.Literal { Value: Integer count }
                                    },
                                    Body: Expr.Variable returned
                                },
                                {
                                    Guard: Expr.Literal { Value: Atom { Name: "true" } },
                                    Body: Expr.Call
                                    {
                                        DynamicRemote: false,
                                        Function: AutoImportNames.Exit,
                                        Arguments: [var reason]
                                    } exit
                                }
                                ]
                        }
                        ]
                    }
                }
                || source.Name != value.Name
                || returned.Name != value.Name
                || size.Name != measured.Name
                || count.Value < 0 || count.Value > MaximumBitSize
                || !DistinctBindings(value, measured)
                || !bound.Add(value.Name)
                || !bound.Add(measured.Name)
                || !IsDecoderIntrinsic(measure)
                || !IsDecoderIntrinsic(exit)
                || !AttributeLiteral.TryNormalizeConstant(reason, out Term error))
            {
                return null;
            }

            decoder = body;

            return new(
                measure,
                count,
                exit,
                error
            );
        }

        private void EmitCheckedFieldMeasurement(Expr.Call measure, BeamSourceLocation previous)
        {
            owner.EmitCallLine(measure, previous);
            owner.Op(GcBifOne, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, measure.Function, 1));
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
        }

        private bool IsDecoderIntrinsic(Expr.Call call) =>
            (call.Module ?? (entries.ContainsKey((call.Function, 1))
                ? null : owner.ResolveExternalModule(call.Function, 1))) == ErlangModuleNames.Module;

        private (Expr.Call Call, IReadOnlyList<Atom> Tags)? ReadCheckedDecoderTail(Expr expression, string tail)
        {
            if (expression is not Expr.Case
                {
                    Value: Expr.Variable source,
                    Clauses:
                    [
                    {
                        Guard: null, Patterns: [Pattern.List { Items.Count: 0, Tail: null }],
                        Body: Expr.Literal { Value: Atom { Name: "true" } }
                    },
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                        Body: Expr.Call { DynamicRemote: false, Function: AutoImportNames.Exit, Arguments: [var reason] } exit
                    }
                    ]
                }
                || source.Name != tail
                || !IsDecoderIntrinsic(exit))
            {
                return null;
            }

            return ReadDecoderExitTags(reason, tail) is { } tags ? (exit, tags) : null;
        }

        private static IReadOnlyList<Atom>? ReadDecoderExitTags(Expr reason, string retained)
        {
            var tags = new List<Atom>();
            while (reason is Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, var inner] })
            {
                tags.Add(tag);
                reason = inner;
            }

            return tags.Count != 0 && reason is Expr.Variable variable && variable.Name == retained
                ? tags : null;
        }

        private void EmitCheckedOptionalField(
            OptionalDecode optional,
            int input,
            int missing,
            int joined,
            BeamSourceLocation previous,
            bool saveOptionalInInputSlot = false,
            bool inputAlreadyInX0 = false,
            bool keepTailInFrame = false,
            int remainingTailSlot = 0
        )
        {
            int joinedTailRegister = saveOptionalInInputSlot ? 0 : 1;
            int sourceTag = inputAlreadyInX0 ? XRegisterTag : YRegisterTag;
            int sourceRegister = inputAlreadyInX0 ? 0 : input;
            owner.Op(IsNonemptyList, LabelTag, missing);
            owner.Operand(sourceTag, sourceRegister);
            owner.Op(GetList, sourceTag, sourceRegister);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, remainingTailSlot);
            owner.EmitTupleCheck(0, 2, missing);
            owner.EmitTupleElement(0, 1);
            owner.Op(ExactEqualTest, LabelTag, missing);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(optional.Tag);
            owner.EmitTupleElement(1, 0);
            EmitDecodeOptions(optional.Call);
            owner.InitStackSlot(input);
            EmitDecodeInvocation(optional.Call, previous);
            if (saveOptionalInInputSlot)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    input
                );
            }

            if (!keepTailInFrame)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    remainingTailSlot,
                    XRegisterTag,
                    joinedTailRegister
                );
            }

            JumpTo(joined);

            Mark(missing);
            owner.MoveRegister(
                YRegisterTag,
                input,
                keepTailInFrame ? YRegisterTag : XRegisterTag,
                keepTailInFrame ? remainingTailSlot : joinedTailRegister
            );
            if (saveOptionalInInputSlot)
            {
                owner.instructions.WriteByte(BeamEmissionFormat.Move);
                owner.LiteralOperand(optional.Absent);
                owner.Operand(YRegisterTag, input);
            }
            else
            {
                owner.EmitConstant(optional.Absent, 0);
            }

            Mark(joined);
        }

        private void EmitEnumeratedCheck(
            OptionalDecode optional,
            int input,
            (int Missing, int Joined) labels,
            (int Key, Atom Value, int Label)[] choices,
            int failure,
            int frame,
            BeamSourceLocation previous
        )
        {
            owner.Op(IsNonemptyList, LabelTag, labels.Missing);
            owner.Operand(YRegisterTag, input);
            owner.Op(GetList, YRegisterTag, input);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, 0);
            owner.EmitTupleCheck(0, 2, labels.Missing);
            owner.EmitTupleElement(0, 1);
            owner.Op(ExactEqualTest, LabelTag, labels.Missing);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(optional.Tag);
            owner.EmitTupleElement(1, 0);
            owner.EmitConstant(optional.Call.Tags, 1);
            owner.InitStackSlot(input);
            EmitDecodeInvocation(optional.Call, previous);
            EmitIntegerSelect(choices, failure);
            foreach (var branch in choices)
            {
                Mark(branch.Label);
                owner.MoveRegister(
                    YRegisterTag,
                    0,
                    XRegisterTag,
                    1
                );
                owner.EmitConstant(branch.Value, 0);
                JumpTo(labels.Joined);
            }

            var reject = optional.Enumeration!;
            Mark(failure);
            EmitTaggedExit(
                reject.Failure,
                reject.Tags,
                CallLocation(reject.Decode, previous),
                framed: true,
                frameSlots: frame
            );
            Mark(labels.Missing);
            owner.MoveRegister(
                YRegisterTag,
                input,
                XRegisterTag,
                1
            );
            owner.EmitConstant(optional.Absent, 0);
            Mark(labels.Joined);
        }

        private void EmitCheckedDecoderTailExit(
            Expr.Call exit,
            IReadOnlyList<Atom> tags,
            int tailTag,
            int tail,
            int frame
        )
        {
            const int TaggedPairHeapWords = 3;
            owner.Op(TestHeap, BeamWire.UnsignedTag, tags.Count * TaggedPairHeapWords);
            owner.Operand(BeamWire.UnsignedTag, tailTag == XRegisterTag ? tail + 1 : 0);
            for (int index = tags.Count - 1; index >= 0; index--)
            {
                owner.Op(PutTuple, XRegisterTag, 0);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.LiteralOperand(tags[index]);
                bool innermost = index == tags.Count - 1;
                owner.Operand(innermost ? tailTag : XRegisterTag, innermost ? tail : 0);
            }

            owner.EmitCallLine(exit, location);
            EmitCheckedDecoderExit(exit, frame);
        }

        private void EmitCheckedDecoderExit(Expr.Call exit, int frame)
        {
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, exit.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, frame);
        }
    }
}
