using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record PrefixedDecodeField(
            SequenceRequired Source,
            Expr.Call Call,
            int Entry,
            Term Tags,
            SequenceRead? Rename = null,
            Expr.Call? Convert = null
        );

        private bool TryEmitDefaultPrefixedSequenceDecoder(
            IReadOnlyList<Expr> items,
            Pattern.Variable first,
            Pattern.Variable second,
            Pattern.Variable list,
            Expr.Call read
        )
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || items is not
                [
                    _, Expr.Match
                {
                    Pattern: Pattern.Tuple { Items: [Pattern.Variable initial, Pattern.Variable continuation] },
                    Value: Expr.Case decision
                }, .., var tailCheck,
                    Expr.Match
                {
                    Pattern: Pattern.Variable output,
                    Value: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable firstResult, ..] } tuple
                },
                    Expr.Variable returned
                ]
                || tuple.Items.Count < 5
                || firstResult.Name != initial.Name || returned.Name != output.Name
                || !DistinctBindings(
                    first,
                    second,
                    list,
                    initial,
                    continuation,
                    output
                ))
            {
                return false;
            }

            var reserved = new HashSet<string>(StringComparer.Ordinal)
                { first.Name, second.Name, list.Name, initial.Name, continuation.Name, output.Name };
            int optionalCount = 2 * tuple.Items.Count - items.Count + 1;
            int requiredCount = tuple.Items.Count - optionalCount - 2;
            bool hasLeadingOptional = optionalCount == 2 && items[2] is Expr.Match
            {
                Pattern: Pattern.Tuple { Items: [Pattern.Variable { }, Pattern.Variable { }] }, Value: Expr.Case
            };
            int trailingCount = optionalCount - (hasLeadingOptional ? 1 : 0);
            var initialField = ReadNamedDefault(decision, list.Name, reserved);
            DefaultSequenceField? ordinaryPrefix = null;
            if (initialField is null && optionalCount == 1 && requiredCount == 2)
            {
                reserved = new HashSet<string>(StringComparer.Ordinal)
                    { first.Name, second.Name, list.Name, initial.Name, continuation.Name, output.Name };
                ordinaryPrefix = ReadDefaultSequenceField(decision, list.Name, reserved);
            }

            if (initialField is null && ordinaryPrefix is null
                || initialField is not null && (optionalCount is not (1 or 2) || requiredCount < (hasLeadingOptional ? 1 : 3)))
            {
                return false;
            }

            var required = new List<PrefixedDecodeField>();
            int member = 2;
            int item = 2;
            string current = continuation.Name;
            DefaultSequenceField? leading = null;
            if (hasLeadingOptional)
            {
                if (items[item++] is not Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable value, Pattern.Variable next] },
                        Value: Expr.Case test
                    }
                    || tuple.Items[member++] is not Expr.Variable result || result.Name != value.Name
                    || !DistinctBindings(value, next) || !reserved.Add(value.Name) || !reserved.Add(next.Name)
                    || ReadDefaultSequenceField(
                        test,
                        current,
                        reserved,
                        allowRemote: true
                    ) is not { } optional)
                {
                    return false;
                }

                leading = optional;
                current = next.Name;
            }

            while (member < tuple.Items.Count - trailingCount)
            {
                if (ReadRequiredPart(
                    items[item],
                    items[item + 1],
                    current,
                    tuple.Items[member],
                    reserved
                ) is not { } field
                    || ReadDecodeCall(field.Read, field.Payload.Name, allowRemote: true) is not { } decode)
                {
                    return false;
                }

                required.Add(new(
                    field,
                    decode.Decode with { Module = decode.Module },
                    decode.Entry,
                    decode.Tags,
                    Convert: decode.Convert
                ));
                current = field.Rest.Name;
                member++;
                item += 2;
            }

            var suffix = new DefaultSequenceField[trailingCount];
            for (int index = 0; index < suffix.Length; index++)
            {
                if (items[item++] is not Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable value, Pattern.Variable next] },
                        Value: Expr.Case test
                    }
                    || tuple.Items[member++] is not Expr.Variable result || result.Name != value.Name
                    || !DistinctBindings(value, next) || !reserved.Add(value.Name) || !reserved.Add(next.Name)
                    || ReadDefaultSequenceField(
                        test,
                        current,
                        reserved,
                        allowRemote: ordinaryPrefix is not null
                    ) is not { } optional
                    || ordinaryPrefix is not null && optional.Module is null)
                {
                    return false;
                }

                suffix[index] = optional;
                current = next.Name;
            }

            if (ReadCheckedDecoderTail(tailCheck, current) is not { } extra)
            {
                return false;
            }

            int frame = tuple.Items.Count;
            var leadingLabels = (Missing: Label(), Joined: Label());
            var firstLabels = optionalCount == 2 ? (Missing: Label(), Joined: Label()) : default;
            var lastLabels = (Missing: Label(), Joined: Label());
            int unexpected = Label();
            int firstFailure = owner.nextLabel + required.Count - 1;
            owner.nextLabel = firstFailure + 1;

            EmitGeneralFrame(2, slots: frame);
            owner.EmitCallLine(read, location);
            Call(read.Module, read.Function, 2);
            BeamSourceLocation previous = CallLocation(read, location);
            int initialSlot = frame - 1;
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                initialSlot
            );
            previous = EmitTaggedOptionalDecoder(
                ordinaryPrefix ?? initialField!.Value.Decoder,
                initialField?.Convert,
                initialSlot,
                initialSlot - 1,
                leadingLabels,
                previous,
                sourceInX0: true,
                leaveInRegisters: leading is null
            );

            var results = new List<int> { initialSlot };
            var failures = new List<(int Label, int? Slot, Expr.Match Match)>();
            int tailSlot = initialSlot - 1;
            if (leading is not null)
            {
                results.Add(tailSlot);
                previous = EmitTaggedOptionalDecoder(
                    leading,
                    null,
                    tailSlot,
                    tailSlot - 1,
                    firstLabels,
                    previous,
                    false,
                    true
                );
                tailSlot--;
            }

            var requiredTail = EmitRequiredSequencePrefix(
                required,
                tailSlot,
                firstFailure,
                previous,
                results,
                failures
            );
            previous = requiredTail.Location;
            int optionalSlot = requiredTail.Tail;
            if (trailingCount == 2)
            {
                results.Add(optionalSlot);
                previous = EmitTaggedOptionalDecoder(
                    suffix[0],
                    null,
                    optionalSlot,
                    optionalSlot - 1,
                    firstLabels,
                    previous,
                    false,
                    false
                );
                optionalSlot--;
            }

            EmitTaggedOptionalDecoder(
                suffix[^1],
                null,
                optionalSlot,
                0,
                lastLabels,
                previous,
                false,
                true,
                finalField: true
            );
            owner.EmitNilTest(XRegisterTag, 1, unexpected);
            owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, tuple.Items.Count);
            owner.LiteralOperand(tag);
            foreach (int result in results)
            {
                owner.Operand(YRegisterTag, result);
            }

            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame);
            Mark(unexpected);
            EmitCheckedDecoderTailExit(
                extra.Call,
                extra.Tags,
                XRegisterTag,
                1,
                frame
            );
            EmitDecoderFailures(failures);

            return true;

        }

        private (int Tail, BeamSourceLocation Location) EmitRequiredSequencePrefix(
            IReadOnlyList<PrefixedDecodeField> required,
            int nextSlot,
            int firstFailure,
            BeamSourceLocation previous,
            List<int> results,
            List<(int Label, int? Slot, Expr.Match Match)> failures
        )
        {
            int? currentSlot = null;
            for (int index = 0; index < required.Count; index++)
            {
                var field = required[index];
                int failure = firstFailure - index;
                failures.Add((failure, currentSlot, field.Source.Split));
                int sourceTag = currentSlot.HasValue ? YRegisterTag : XRegisterTag;
                int source = currentSlot.GetValueOrDefault();
                owner.Op(IsNonemptyList, LabelTag, failure);
                owner.Operand(sourceTag, source);
                owner.Op(GetList, sourceTag, source);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(YRegisterTag, nextSlot);
                owner.EmitConstant(field.Tags, 1);
                if (currentSlot is { } dead)
                {
                    owner.InitStackSlot(dead);
                }

                owner.EmitCallLine(field.Call, previous);
                previous = CallLocation(field.Call, previous);
                if (field.Call.Module is { } module)
                {
                    Call(module, field.Call.Function, 2);
                }
                else
                {
                    owner.EmitFieldCall(2, field.Entry);
                }

                if (field.Convert is { } convert)
                {
                    owner.EmitCallLine(convert, previous);
                    Call(convert.Module, convert.Function, 1);
                    previous = CallLocation(convert, previous);
                }

                if (field.Rename is { } rename)
                {
                    owner.EmitConstant(rename.Options, 1);
                    owner.EmitCallLine(rename.Call, previous);
                    previous = CallLocation(rename.Call, previous);
                    owner.EmitFieldCall(2, rename.Entry);
                }

                int resultSlot = currentSlot ?? nextSlot - 1;
                results.Add(resultSlot);
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    resultSlot
                );
                currentSlot = nextSlot;
                nextSlot -= index == 0 ? 2 : 1;
            }

            return (currentSlot.GetValueOrDefault(), previous);
        }

        private BeamSourceLocation EmitTaggedOptionalDecoder(
            DefaultSequenceField field,
            SequenceRead? convert,
            int saved,
            int tail,
            (int Missing, int Joined) labels,
            BeamSourceLocation previous,
            bool sourceInX0,
            bool leaveInRegisters,
            bool finalField = false
        )
        {
            int sourceTag = sourceInX0 ? XRegisterTag : YRegisterTag;
            int source = sourceInX0 ? 0 : saved;
            owner.Op(IsNonemptyList, LabelTag, labels.Missing);
            owner.Operand(sourceTag, source);
            owner.Op(GetList, sourceTag, source);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, tail);
            owner.EmitTupleCheck(0, 2, labels.Missing);
            owner.EmitTupleElement(0, 1);
            owner.Op(ExactEqualTest, LabelTag, labels.Missing);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(field.Tag);
            owner.EmitTupleElement(1, 0);
            owner.EmitConstant(field.Tags, 1);
            owner.InitStackSlot(saved);
            owner.EmitCallLine(field.Decoder, previous);
            previous = CallLocation(field.Decoder, previous);
            if (field.Module is { } module)
            {
                Call(module, field.Decoder.Function, 2);
            }
            else
            {
                owner.EmitFieldCall(2, field.Entry);
            }

            if (convert is { } rename)
            {
                owner.EmitConstant(rename.Options, 1);
                owner.EmitCallLine(rename.Call, previous);
                previous = CallLocation(rename.Call, previous);
                owner.EmitFieldCall(2, rename.Entry);
            }

            int tailRegister = finalField ? 1 : 0;
            if (!finalField)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    saved
                );
            }

            if (leaveInRegisters)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    tail,
                    XRegisterTag,
                    tailRegister
                );
            }

            JumpTo(labels.Joined);
            Mark(labels.Missing);
            owner.MoveRegister(
                YRegisterTag,
                saved,
                leaveInRegisters ? XRegisterTag : YRegisterTag,
                leaveInRegisters ? tailRegister : tail
            );
            if (finalField)
            {
                owner.EmitConstant(field.Default, 0);
            }
            else
            {
                owner.instructions.WriteByte(BeamEmissionFormat.Move);
                owner.LiteralOperand(field.Default);
                owner.Operand(YRegisterTag, saved);
            }

            Mark(labels.Joined);

            return previous;
        }

        private (DefaultSequenceField Decoder, SequenceRead Convert)? ReadNamedDefault(
            Expr.Case choice,
            string previous,
            HashSet<string> reserved
        )
        {
            if (choice.Clauses is not
                [
                {
                    Patterns: [Pattern.List { Items: [Pattern.Tuple { Items: [_, Pattern.Variable member] }] }],
                    Body: Expr.Tuple
                    {
                        Items:
                                [
                                    Expr.Block
                                {
                                    Body: Expr.Sequence
                                    {
                                        Items: [Expr.Match { Pattern: Pattern.Variable value, Value: var call }, var renamed]
                                    }
                                }, var remaining
                                ]
                    } tuple
                } match,
                    var fallback
                ]
                || value.Name == member.Name || !DistinctBindings(value) || !reserved.Add(value.Name)
                || ReadSequenceCall(call, member.Name) is not { } decode
                || ReadSequenceCall(renamed, value.Name) is not { } conversion
                || !IsClosedNamedNumberList(conversion.Options)
                || ReadDefaultSequenceField(
                    choice with { Clauses = [match with { Body = tuple with { Items = [decode.Call, remaining] } }, fallback] },
                    previous,
                    reserved
                ) is not { } field)
            {
                return null;
            }

            return (field, conversion);
        }
    }
}
