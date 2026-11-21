using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record InterleavedDecodePart(
            SequenceRequired? Required,
            SequenceRead? Reader,
            DefaultSequenceField? Optional
        );

        private bool TryEmitInterleavedNamedSequenceDecoder(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable input, Pattern.Variable tags],
                        Body: Expr.Sequence
                        {
                            Items: [
                                Expr.Match
                            {
                                Pattern: Pattern.Variable parsed,
                                Value: Expr.Call
                                {
                                    Module: null, DynamicRemote: false,
                                    Arguments: [Expr.Variable source, Expr.Variable tagInput]
                                } matcher
                            },
                                ..,
                                var tailCheck,
                                Expr.Match
                            {
                                Pattern: Pattern.Variable output,
                                Value: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, ..] } result
                            },
                                Expr.Variable returned
                            ]
                        } sequence
                    }
                ]
                || !DistinctBindings(
                    input,
                    tags,
                    parsed,
                    output
                )
                || source.Name != input.Name || tagInput.Name != tags.Name || returned.Name != output.Name
                || !entries.TryGetValue((matcher.Function, 2), out int matcherEntry))
            {
                return false;
            }

            int count = result.Items.Count - 1;
            if (count < 3 || count + 1 >= FirstReservedXRegister)
            {
                return false;
            }

            var bindings = new HashSet<string>(StringComparer.Ordinal) { input.Name, tags.Name, parsed.Name, output.Name };
            var parts = new InterleavedDecodePart[count];
            SequenceRead? rename = null;
            string remaining = parsed.Name;
            int item = 1;
            int end = sequence.Items.Count - 3;
            for (int index = 0; index < count; index++)
            {
                if (item >= end)
                {
                    return false;
                }

                Expr member = result.Items[index + 1];
                if (index > 0 && sequence.Items[item] is Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable value, Pattern.Variable rest] },
                        Value: Expr.Case choice
                    })
                {
                    if (member is not Expr.Variable outputMember || outputMember.Name != value.Name
                        || !DistinctBindings(value, rest) || !bindings.Add(value.Name) || !bindings.Add(rest.Name)
                        || ReadDefaultSequenceField(choice, remaining, bindings) is not { } optional)
                    {
                        return false;
                    }

                    parts[index] = new(null, null, optional);
                    remaining = rest.Name;
                    item++;
                    continue;
                }

                if (item + 1 >= end || ReadRequiredPart(
                    sequence.Items[item],
                    sequence.Items[item + 1],
                    remaining,
                    member,
                    bindings
                ) is not { } required)
                {
                    return false;
                }

                SequenceRead? reader;
                if (index == 0)
                {
                    if (ReadSequenceNamedValue(required.Read, required.Payload.Name, bindings) is not { } named)
                    {
                        return false;
                    }

                    reader = named.Decode;
                    rename = named.Rename;
                }
                else
                {
                    reader = ReadSequenceCall(required.Read, required.Payload.Name);
                }

                if (reader is null)
                {
                    return false;
                }

                parts[index] = new(required, reader, null);
                remaining = required.Rest.Name;
                item += 2;
            }

            var unexpected = ReadCheckedDecoderTail(tailCheck, remaining);
            if (item != end || (unexpected is null
                    ? !IsIgnoredListTail(tailCheck, remaining)
                    : parts.All(part => part.Optional is null)))
            {
                return false;
            }

            int frame = unexpected is null ? 0 : 1;
            for (int index = 0; index < count - 1; index++)
            {
                if (parts[index].Optional is not null)
                {
                    frame += parts[index + 1].Optional is null ? 0 : 1;
                }
                else
                {
                    frame += index == 0 || parts[index - 1].Optional is not null ? 2 : 1;
                }
            }

            var branches = new (int Missing, int Joined)[count];
            for (int index = 0; index < count; index++)
            {
                if (parts[index].Optional is not null)
                {
                    branches[index] = (Label(), Label());
                }
            }

            int hasExtra = unexpected is null ? 0 : Label();
            var missing = new int[count];
            for (int index = count - 1; index >= 0; index--)
            {
                if (parts[index].Required is not null)
                {
                    missing[index] = Label();
                }
            }

            EmitGeneralFrame(2, slots: frame);
            BeamSourceLocation previous = location;
            Invoke(new(matcher, matcherEntry, Nil.Value), withOptions: false);
            var failures = new List<(int Label, int? Slot, Expr.Match Match)>();
            var outputSlots = new List<int>();
            int? inputSlot = null;
            int nextSlot = frame - 1;
            int trimmed = 0;

            for (int index = 0; index < count; index++)
            {
                var part = parts[index];
                bool last = index == count - 1;
                if (part.Optional is { } optional)
                {
                    int saved = inputSlot!.Value;
                    bool continueInX0 = !last && parts[index + 1].Optional is null;
                    if (last && unexpected is null)
                    {
                        EmitIgnoredTailOptional(
                            optional,
                            saved,
                            branches[index],
                            previous
                        );
                    }
                    else
                    {
                        previous = EmitTaggedOptionalDecoder(
                            optional,
                            null,
                            saved,
                            nextSlot,
                            branches[index],
                            previous,
                            sourceInX0: false,
                            leaveInRegisters: continueInX0 || last,
                            finalField: last
                        );
                    }

                    if (!last)
                    {
                        outputSlots.Add(saved);
                    }

                    inputSlot = continueInX0 ? null : nextSlot--;
                    continue;
                }

                int kind = inputSlot.HasValue ? YRegisterTag : XRegisterTag;
                int register = inputSlot.GetValueOrDefault();
                failures.Add((missing[index], inputSlot, part.Required!.Split));
                owner.Op(IsNonemptyList, LabelTag, missing[index]);
                owner.Operand(kind, register);
                bool discardTail = last && unexpected is null;
                owner.Op(discardTail ? GetHead : GetList, kind, register);
                owner.Operand(XRegisterTag, 0);
                if (!discardTail)
                {
                    owner.Operand(YRegisterTag, nextSlot);
                }

                owner.EmitConstant(part.Reader!.Options, 1);
                if (discardTail && inputSlot == 0)
                {
                    trimmed = 1;
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                    owner.Operand(BeamWire.UnsignedTag, frame - 1);
                }
                else if (inputSlot is int dead)
                {
                    owner.InitStackSlot(dead);
                }

                Invoke(part.Reader, withOptions: false);
                if (index == 0)
                {
                    Invoke(rename!, withOptions: true);
                }

                if (!last)
                {
                    int outputSlot = inputSlot ?? nextSlot - 1;
                    outputSlots.Add(outputSlot);
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        outputSlot
                    );
                    inputSlot = nextSlot;
                    nextSlot = Math.Min(nextSlot, outputSlot) - 1;
                }
            }

            bool extraInRegister = parts[^1].Optional is not null;
            int extraKind = extraInRegister ? XRegisterTag : YRegisterTag;
            int extraRegister = extraInRegister ? 1 : nextSlot;
            if (unexpected is not null)
            {
                owner.EmitNilTest(extraKind, extraRegister, hasExtra);
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, count + 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, count + 1);
            owner.LiteralOperand(tag);
            foreach (int slot in outputSlots)
            {
                owner.Operand(YRegisterTag, slot - trimmed);
            }

            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame - trimmed);
            if (unexpected is { } error)
            {
                Mark(hasExtra);
                EmitCheckedDecoderTailExit(
                    error.Call,
                    error.Tags,
                    extraKind,
                    extraRegister,
                    frame
                );
            }

            EmitDecoderFailures(failures);

            return true;

            void Invoke(SequenceRead call, bool withOptions)
            {
                if (withOptions)
                {
                    owner.EmitConstant(call.Options, 1);
                }

                owner.EmitCallLine(call.Call, previous);
                previous = CallLocation(call.Call, previous);
                owner.EmitFieldCall(2, call.Entry);
            }
        }

        private void EmitIgnoredTailOptional(
            DefaultSequenceField field,
            int saved,
            (int Missing, int Joined) labels,
            BeamSourceLocation previous
        )
        {
            owner.Op(IsNonemptyList, LabelTag, labels.Missing);
            owner.Operand(YRegisterTag, saved);
            owner.Op(GetHead, YRegisterTag, saved);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTupleCheck(0, 2, labels.Missing);
            owner.EmitTupleElement(0, 1);
            owner.Op(ExactEqualTest, LabelTag, labels.Missing);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(field.Tag);
            owner.EmitTupleElement(1, 0);
            owner.EmitConstant(field.Tags, 1);
            owner.InitStackSlot(saved);
            owner.EmitCallLine(field.Decoder, previous);
            owner.EmitFieldCall(2, field.Entry);
            JumpTo(labels.Joined);
            Mark(labels.Missing);
            owner.EmitConstant(field.Default, 0);
            Mark(labels.Joined);
        }
    }
}
