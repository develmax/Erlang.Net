using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record OptionalJoinRequiredField(
            Expr.Match Split,
            DecodeCall Call,
            CheckedFieldSize? Size,
            IntegerCase? Enumeration,
            TaggedBranchRange? Range
        );

        private bool TryEmitOptionalBeforeRequiredField(
            IReadOnlyList<Expr> items,
            Pattern.Variable first,
            Pattern.Variable second,
            Pattern.Variable list,
            Expr.Call read
        )
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || items is not [_, .., var tailCheck,
                    Expr.Match
                {
                    Pattern: Pattern.Variable output,
                    Value: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, ..] } tuple
                },
                    Expr.Variable returned]
                || tuple.Items.Count < 3 || tuple.Items.Count >= FirstReservedXRegister
                || returned.Name != output.Name
                || !DistinctBindings(
                    first,
                    second,
                    list,
                    output
                ))
            {
                return false;
            }

            int requiredCount = items.Count - tuple.Items.Count - 3;
            int leading = CountLeadingRequiredFields(items);
            int trailing = requiredCount - leading;
            int optionalCount = tuple.Items.Count - requiredCount - 1;
            if (trailing is < 1 or > 2 || optionalCount < 1 || leading > 2
                || leading > 0 && optionalCount != 1 || leading == 2 && trailing != 1)
            {
                return false;
            }

            var bound = new HashSet<string>(StringComparer.Ordinal) { first.Name, second.Name, list.Name, output.Name };
            string current = list.Name;
            var prefix = new List<OptionalJoinRequiredField>();
            int index = 1;
            int member = 1;
            for (int offset = 0; offset < leading; offset++)
            {
                if (ReadRequired(
                    index,
                    member,
                    current,
                    allowEnumeration: leading == 1 && trailing == 2,
                    allowRange: false,
                    out current
                ) is not { Size: null } field)
                {
                    return false;
                }

                prefix.Add(field);
                index += 2;
                member++;
            }

            var optionals = new List<OptionalDecode>();
            for (int offset = 0; offset < optionalCount; offset++)
            {
                if (items[index++] is not Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable optional, Pattern.Variable afterOptional] },
                        Value: Expr.Case decision
                    }
                    || tuple.Items[member++] is not Expr.Variable returnedOptional || returnedOptional.Name != optional.Name
                    || !DistinctBindings(optional, afterOptional)
                    || !bound.Add(optional.Name) || !bound.Add(afterOptional.Name)
                    || ReadOptionalDecode(
                        decision,
                        current,
                        bound,
                        allowRemote: true
                    ) is not { Call.Convert: null } optionalDecode)
                {
                    return false;
                }

                optionals.Add(optionalDecode);
                current = afterOptional.Name;
            }

            var suffix = new List<OptionalJoinRequiredField>();
            for (int offset = 0; offset < trailing; offset++)
            {
                if (ReadRequired(
                    index,
                    member++,
                    current,
                    allowEnumeration: false,
                    allowRange: leading == 1 && trailing == 2 && offset == 0,
                    out current
                ) is not { } field
                    || trailing > 1 && field.Size is not null)
                {
                    return false;
                }

                suffix.Add(field);
                index += 2;
            }

            if (ReadCheckedDecoderTail(tailCheck, current) is not { } failure)
            {
                return false;
            }

            int frame = tuple.Items.Count - 1 + (trailing == 2 ? 1 : 0);
            int firstTail = frame - 1;
            int optionalSlot = leading == 2 ? 1 : firstTail;
            var enumeration = prefix.Count == 1 ? prefix[0].Enumeration : null;
            var choices = enumeration is null ? [] : ScheduleIntegerChoices(enumeration);
            int enumeratedJoin = enumeration is null ? 0 : Label();
            var labels = optionals.Select(_ => (Missing: Label(), Joined: Label())).ToArray();
            int extra = Label();
            int badSize = suffix[^1].Size is null ? 0 : Label();
            var range = suffix[0].Range;
            int badValue = enumeration is not null || range is not null ? Label() : 0;
            int badRange = enumeration is not null && range is not null ? Label() : badValue;
            int firstMissing = owner.nextLabel + suffix.Count + prefix.Count - 1;
            owner.nextLabel = firstMissing + 1;

            EmitGeneralFrame(2, slots: frame);
            owner.EmitCallLine(read, location);
            Call(read.Module, read.Function, 2);
            BeamSourceLocation previous = CallLocation(read, location);
            var badMatches = new List<(int Label, int? Slot, Expr.Match Match)>();
            var prefixResults = new List<int>();
            for (int offset = 0; offset < prefix.Count; offset++)
            {
                var field = prefix[offset];
                int missing = firstMissing - offset;
                int? source = offset == 0 ? null : firstTail;
                int saveTail = offset == 0 ? firstTail : optionalSlot;
                int result = offset == 0 ? (leading == 1 && trailing == 1 ? 1 : firstTail - 1) : firstTail;
                EmitSplit(missing, source, saveTail);
                owner.EmitConstant(field.Call.Tags, 1);
                if (source is { } killed)
                {
                    owner.InitStackSlot(killed);
                }

                EmitDecodeInvocation(field.Call, previous);
                previous = CallLocation(field.Call.Decode, previous);
                if (enumeration is not null)
                {
                    EmitIntegerSelect(choices, badValue);
                    foreach (var choice in choices)
                    {
                        Mark(choice.Label);
                        owner.instructions.WriteByte(BeamEmissionFormat.Move);
                        owner.LiteralOperand(choice.Value);
                        owner.Operand(YRegisterTag, result);
                        if (choice.Label != choices[^1].Label)
                        {
                            JumpTo(enumeratedJoin);
                        }
                    }

                    Mark(enumeratedJoin);
                }
                else
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        result
                    );
                }

                prefixResults.Add(result);
                badMatches.Add((missing, source, field.Split));
            }

            if (leading == 0)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    optionalSlot
                );
            }

            for (int offset = 0; offset < optionals.Count; offset++)
            {
                int slot = optionalSlot - offset;
                bool last = offset == optionals.Count - 1;
                var optionalDecode = optionals[offset];
                EmitCheckedOptionalField(
                    optionalDecode,
                    slot,
                    labels[offset].Missing,
                    labels[offset].Joined,
                    previous,
                    saveOptionalInInputSlot: true,
                    inputAlreadyInX0: leading == 0 && offset == 0,
                    keepTailInFrame: !last,
                    remainingTailSlot: last ? (trailing == 2 ? 2 : 0) : slot - 1
                );
                previous = CallLocation(optionalDecode.Call.Decode, previous);
            }

            for (int offset = 0; offset < suffix.Count; offset++)
            {
                var field = suffix[offset];
                int missing = firstMissing - leading - offset;
                bool last = offset + 1 == suffix.Count;
                int? source = offset == 0 ? null : 2;
                int next = last ? 0 : 2;
                EmitSplit(missing, source, next);

                owner.EmitConstant(field.Call.Tags, 1);
                if (offset > 0)
                {
                    owner.InitStackSlot(2);
                }

                EmitDecodeInvocation(field.Call, previous);
                previous = CallLocation(field.Call.Decode, previous);
                if (!last)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        1
                    );
                }

                if (field.Range is { } bounded)
                {
                    EmitRangeTests(field.Call.Decode, bounded, badRange);
                }

                if (field.Size is { } size)
                {
                    EmitCheckedFieldMeasurement(size.Measure, previous);
                    owner.Op(ExactEqualTest, LabelTag, badSize);
                    owner.EmitTypedRegister(
                        1,
                        IntegerTypeBits,
                        0,
                        MaximumBitSize
                    );
                    owner.LiteralOperand(size.Count);
                }

                badMatches.Add((missing, source, field.Split));
            }

            owner.EmitNilTest(YRegisterTag, 0, extra);
            owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, tuple.Items.Count);
            owner.LiteralOperand(tag);
            foreach (int result in prefixResults)
            {
                owner.Operand(YRegisterTag, result);
            }

            for (int offset = 0; offset < optionals.Count; offset++)
            {
                owner.Operand(YRegisterTag, optionalSlot - offset);
            }

            if (trailing == 2)
            {
                owner.Operand(YRegisterTag, 1);
            }

            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame);

            Mark(extra);
            EmitCheckedDecoderTailExit(
                failure.Call,
                failure.Tags,
                YRegisterTag,
                0,
                frame
            );
            if (suffix[^1].Size is { } measured)
            {
                Mark(badSize);
                owner.EmitConstant(measured.Reason, 0);
                owner.EmitCallLine(measured.Failure, location);
                EmitCheckedDecoderExit(measured.Failure, frame);
            }

            if (enumeration is not null)
            {
                Mark(badValue);
                EmitTaggedExit(
                    enumeration.Failure,
                    enumeration.Tags,
                    CallLocation(enumeration.Decode, location),
                    framed: true,
                    frameSlots: frame
                );
            }

            if (range is not null)
            {
                Mark(badRange);
                EmitRangeExit(range, CallLocation(range.Exit, location), frame);
            }

            EmitDecoderFailures(badMatches);

            return true;

            OptionalJoinRequiredField? ReadRequired(
                int start,
                int field,
                string source,
                bool allowEnumeration,
                bool allowRange,
                out string next
            )
            {
                next = source;
                if (items[start] is not Expr.Match
                    {
                        Pattern: Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail },
                        Value: Expr.Variable input
                    } split
                    || items[start + 1] is not Expr.Match { Pattern: Pattern.Variable boundValue, Value: var body }
                    || tuple.Items[field] is not Expr.Variable value || value.Name != boundValue.Name
                    || input.Name != source || !DistinctBindings(head, tail, boundValue)
                    || !bound.Add(head.Name) || !bound.Add(tail.Name) || !bound.Add(boundValue.Name))
                {
                    return null;
                }

                var checkedSize = ReadCheckedFieldSize(body, bound, out Expr decoder);
                var enumCase = allowEnumeration ? ReadIntegerCase(body, name => !bound.Contains(name)) : null;
                var bounded = allowRange ? ReadSequenceBounds(body, head.Name, bound) : null;
                if (ReadDecodeCall(
                    enumCase?.Decode ?? bounded?.Read.Call ?? decoder,
                    head.Name,
                    allowRemote: checkedSize is null
                ) is not { Convert: null } call)
                {
                    return null;
                }

                next = tail.Name;

                return new(
                    split,
                    call,
                    checkedSize,
                    enumCase,
                    bounded?.Bounds
                );
            }

            void EmitSplit(int missing, int? source, int tail)
            {
                int sourceTag = source.HasValue ? YRegisterTag : XRegisterTag;
                int sourceRegister = source.GetValueOrDefault();
                owner.Op(IsNonemptyList, LabelTag, missing);
                owner.Operand(sourceTag, sourceRegister);
                owner.Op(GetList, sourceTag, sourceRegister);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(YRegisterTag, tail);
            }
        }

        private static int CountLeadingRequiredFields(IReadOnlyList<Expr> items)
        {
            int index = 1;
            while (items[index] is Expr.Match { Pattern: Pattern.List { } })
            {
                index += 2;
            }

            return (index - 1) / 2;
        }
    }
}
