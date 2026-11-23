using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record SequenceRead(
            Expr.Call Call,
            int Entry,
            Term Options,
            string? Module = null,
            Term? MiddleArgument = null
        );

        private sealed record SequenceRequired(
            Expr.Match Split,
            Pattern.Variable Payload,
            Pattern.Variable Rest,
            Pattern.Variable Output,
            Expr Read
        );

        private sealed record SequenceRange(SequenceRead Read, SequenceRead Name, TaggedBranchRange Range);

        private sealed record SequenceOptional(
            SequenceRead Read,
            Integer? Tag,
            Term Omitted,
            TaggedBranchRange? Bounds = null
        );

        private bool TryEmitRequiredOptionalSequenceDecoder(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable tags],
                    Body: Expr.Sequence
                    {
                        Items:
                                [
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
                                    Expr.Case extra,
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

            int fields = result.Items.Count - 1;
            int requiredCount = sequence.Items.Count - fields - 4;
            int count = fields - requiredCount;
            bool singleRequired = requiredCount == 1;
            if (requiredCount < 1 || count < 1 || fields + 1 >= FirstReservedXRegister)
            {
                return false;
            }

            var bindings = new HashSet<string>(StringComparer.Ordinal) { input.Name, tags.Name, parsed.Name, output.Name };
            var required = new (SequenceRequired Field, SequenceRead Read)[requiredCount];
            SequenceRange? range = null;
            TaggedBranchRange? boundedPrefix = null;
            string remaining = parsed.Name;
            for (int index = 0; index < requiredCount; index++)
            {
                if (ReadRequiredPart(
                    sequence.Items[2 * index + 1],
                    sequence.Items[2 * index + 2],
                    remaining,
                    result.Items[index + 1],
                    bindings
                ) is not { } part)
                {
                    return false;
                }

                if (index == 0 && !singleRequired)
                {
                    range = ReadNamedRange(part, bindings);
                }

                var read = singleRequired
                    ? ReadSingleRequiredSequenceCall(part.Read, part.Payload.Name)
                    : index == 0 && range is not null ? range.Read : ReadSequenceCall(part.Read, part.Payload.Name);
                if (read is null && index == 1 && range is null && requiredCount == 2
                    && ReadSequenceBounds(part.Read, part.Payload.Name, bindings) is { } bounded)
                {
                    read = bounded.Read;
                    boundedPrefix = bounded.Bounds;
                }

                if (read is null)
                {
                    return false;
                }

                required[index] = (part, read);
                remaining = part.Rest.Name;
            }

            if (!singleRequired && (range is not null
                    ? requiredCount != 2
                    : boundedPrefix is null && (requiredCount < 3 || required.Skip(1).Any(field => field.Read.Entry != required[0].Read.Entry
                        || !field.Read.Options.Equals(required[0].Read.Options)))))
            {
                return false;
            }

            var plans = new SequenceOptional[count];
            for (int index = 0; index < count; index++)
            {
                if (sequence.Items[2 * requiredCount + index + 1] is not Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable field, Pattern.Variable rest] },
                        Value: Expr.Case choice
                    }
                    || result.Items[requiredCount + index + 1] is not Expr.Variable value || value.Name != field.Name
                    || field.Name == VariableScopeNames.Wildcard || rest.Name == VariableScopeNames.Wildcard
                    || !bindings.Add(field.Name) || !bindings.Add(rest.Name)
                    || (singleRequired
                        ? ReadOptionalAfterSingleRequired(choice, remaining, bindings)
                        : ReadOptionalSequencePart(choice, remaining, bindings)) is not { } plan)
                {
                    return false;
                }

                plans[index] = plan;
                remaining = rest.Name;
            }

            if (!singleRequired && (boundedPrefix is null && plans.Any(plan => plan.Bounds is not null)
                || boundedPrefix is not null && (count < 2 || boundedPrefix.Upper is not null
                    || plans[0].Bounds is not { Upper: null } optionalBound
                    || !optionalBound.Lower.Equals(boundedPrefix.Lower) || plans[0].Read.Entry != required[1].Read.Entry
                    || plans.Skip(1).Any(plan => plan.Bounds is not null))))
            {
                return false;
            }

            if (ReadTableUnexpectedFields(extra, remaining) is not { } unexpected)
            {
                return false;
            }

            var labels = plans.Select(plan => (Invalid: plan.Bounds is null ? 0 : Label(), Omitted: Label(), Ready: Label())).ToArray();
            int hasExtra = Label();
            int outsideRange = range is null && boundedPrefix is null ? 0 : Label();
            var missingRequired = new int[requiredCount];
            for (int index = requiredCount - 1; index >= 0; index--)
            {
                missingRequired[index] = Label();
            }

            int frame = fields + 1;
            var previous = location;
            EmitFrameAllocation(arity: 2, size: frame);
            EmitFrameInitialization(0, frame);
            EmitLocal(matcher, matcherEntry);
            var requiredInputs = new int[requiredCount];
            var requiredOutputs = new int[requiredCount];
            int currentRest = 0;
            for (int index = 0; index < requiredCount; index++)
            {
                var reader = required[index].Read;
                int nextRest = index == 0 ? fields : fields - index - 1;
                requiredInputs[index] = currentRest;
                Split(
                    index == 0 ? XRegisterTag : YRegisterTag,
                    currentRest,
                    nextRest,
                    missingRequired[index]
                );
                owner.EmitConstant(reader.Options, 1);
                if (index != 0)
                {
                    EmitFrameInitialization(currentRest, currentRest + 1);
                }

                EmitSequenceRead(reader);
                if (index == 0 && range is not null)
                {
                    EmitRangeTests(range.Read.Call, range.Range, outsideRange);
                    owner.EmitConstant(range.Name.Options, 1);
                    owner.EmitConstant(range.Range.Lower, 0);
                    EmitLocal(range.Name.Call, range.Name.Entry);
                }

                int outputSlot = index == 0 ? fields - 1 : currentRest;
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    outputSlot
                );
                if (index == 1 && boundedPrefix is not null)
                {
                    EmitRangeTests(reader.Call, boundedPrefix, outsideRange);
                }

                requiredOutputs[index] = outputSlot;
                currentRest = nextRest;
            }

            for (int index = 0; index < plans.Length; index++)
            {
                var plan = plans[index];
                int tailSlot = count - index - 1;
                int slot = singleRequired && index == 0 ? fields : tailSlot + 1;
                bool last = index == plans.Length - 1;
                var (invalid, missing, ready) = labels[index];
                Split(
                    YRegisterTag,
                    slot,
                    tailSlot,
                    missing
                );
                if (plan.Tag is { } selected)
                {
                    owner.EmitTupleCheck(0, 2, missing);
                    owner.EmitTuplePart(0, 0, 1);
                    owner.Op(ExactEqualTest, LabelTag, missing);
                    owner.Operand(XRegisterTag, 1);
                    owner.LiteralOperand(selected);
                    owner.EmitTuplePart(0, 1, 0);
                }

                EmitOptionalFieldArguments(plan.Read.Options, plan.Read.MiddleArgument);
                EmitFrameInitialization(slot, slot + 1);
                EmitSequenceRead(plan.Read);
                if (plan.Bounds is { } bounds)
                {
                    EmitRangeTests(plan.Read.Call, bounds, invalid);
                }

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
                if (plan.Bounds is { } outOfRange)
                {
                    Mark(invalid);
                    EmitRangeExit(outOfRange, CallLocation(outOfRange.Exit, previous), frame);
                }

                Mark(missing);
                int destination = last ? XRegisterTag : YRegisterTag;
                owner.MoveRegister(
                    YRegisterTag,
                    slot,
                    destination,
                    last ? 1 : tailSlot
                );
                owner.instructions.WriteByte(BeamEmissionFormat.Move);
                owner.LiteralOperand(plan.Omitted);
                owner.Operand(destination, last ? 0 : slot);
                Mark(ready);
            }

            owner.EmitNilTest(XRegisterTag, 1, hasExtra);
            owner.Op(TestHeap, BeamWire.UnsignedTag, fields + 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, fields + 1);
            owner.LiteralOperand(tag);
            foreach (int slot in requiredOutputs)
            {
                owner.Operand(YRegisterTag, slot);
            }

            if (singleRequired && count > 1)
            {
                owner.Operand(YRegisterTag, fields);
            }

            for (int slot = singleRequired ? count - 1 : count; slot > 1; slot--)
            {
                owner.Operand(YRegisterTag, slot);
            }

            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame);

            Mark(hasExtra);
            EmitTaggedExit(
                unexpected.Exit,
                [unexpected.ErrorTag, unexpected.DetailsTag, unexpected.UnexpectedTag],
                previous,
                framed: true,
                live: 2,
                source: 1,
                frameSlots: frame
            );
            if (range is not null)
            {
                Mark(outsideRange);
                EmitRangeExit(range.Range, CallLocation(range.Range.Exit, location), frame);
            }

            if (boundedPrefix is not null)
            {
                Mark(outsideRange);
                EmitRangeExit(boundedPrefix, CallLocation(boundedPrefix.Exit, location), frame);
            }

            for (int index = requiredCount - 1; index >= 0; index--)
            {
                Mark(missingRequired[index]);
                previous = location;
                owner.EmitArithmeticLine(required[index].Field.Split.SourceLine, ref previous);
                owner.Op(BadMatch, index == 0 ? XRegisterTag : YRegisterTag, requiredInputs[index]);
            }

            return true;

            void EmitSequenceRead(SequenceRead read)
            {
                EmitSequenceDecoderCall(
                    read.Call,
                    read.Module,
                    read.Entry,
                    ref previous
                );
            }

            void EmitLocal(Expr.Call call, int entry)
            {
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                owner.EmitFieldCall(2, entry);
            }

            void Split(
                int kind,
                int input,
                int rest,
                int failure
            )
            {
                owner.Op(IsNonemptyList, LabelTag, failure);
                owner.Operand(kind, input);
                owner.Op(GetList, kind, input);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(YRegisterTag, rest);
            }
        }

        private static SequenceRequired? ReadRequiredPart(
            Expr split,
            Expr read,
            string previous,
            Expr result,
            HashSet<string> reserved
        )
        {
            if (split is not Expr.Match
                {
                    Pattern: Pattern.List { Items: [Pattern.Variable payload], Tail: Pattern.Variable rest },
                    Value: Expr.Variable input
                } unpack
                || read is not Expr.Match { Pattern: Pattern.Variable output, Value: var operation }
                || result is not Expr.Variable returned || returned.Name != output.Name || input.Name != previous
                || !DistinctBindings(payload, rest, output)
                || !reserved.Add(payload.Name) || !reserved.Add(rest.Name) || !reserved.Add(output.Name))
            {
                return null;
            }

            return new(
                unpack,
                payload,
                rest,
                output,
                operation
            );
        }

        private SequenceRange? ReadNamedRange(SequenceRequired field, HashSet<string> reserved)
        {
            Expr expression = field.Read is Expr.Block block ? block.Body : field.Read;
            if (expression is not Expr.Sequence
                {
                    Items:
                        [
                            Expr.Match { Pattern: Pattern.Variable decoded, Value: var read },
                            Expr.If { Clauses: [var accepted, var rejected] } condition
                        ]
                }
                || accepted.Body is not Expr.Sequence
                {
                    Items:
                        [
                            Expr.Match { Pattern: Pattern.Variable bound, Value: Expr.Variable previous },
                            var name
                        ]
                }
                || !DistinctBindings(decoded, bound) || !reserved.Add(decoded.Name) || !reserved.Add(bound.Name)
                || previous.Name != decoded.Name || ReadSequenceCall(read, field.Payload.Name) is not { } readValue
                || ReadSequenceCall(name, bound.Name) is not { } rename
                || (InferPairFacts(readValue.Call) & PairFacts.Integer) == 0
                || ReadTaggedBranchRange(condition with
                {
                    Clauses = [accepted with { Body = new Expr.Variable(decoded.Name) }, rejected]
                }, decoded.Name) is not { } range
                || range.Upper is not { } upper || !range.Lower.Value.Equals(upper.Value) || !owner.ResolvesErlangBif(range.Exit))
            {
                return null;
            }

            return new(readValue, rename, range);
        }

        private SequenceRead? ReadSequenceCall(Expr expression, string source)
        {
            return expression is Expr.Call
            {
                Module: null, DynamicRemote: false, Arguments: [Expr.Variable input, var tag]
            } call && input.Name == source && entries.TryGetValue((call.Function, 2), out int entry)
                && AttributeLiteral.TryNormalizeConstant(tag, out Term options)
                    ? new(call, entry, options)
                    : null;
        }

        private (SequenceRead Read, TaggedBranchRange Bounds)? ReadSequenceBounds(
            Expr expression,
            string source,
            IReadOnlySet<string> bindings,
            string? containingBinding = null
        )
        {
            if (expression is not Expr.Block
                {
                    Body: Expr.Sequence
                    {
                        Items: [Expr.Match { Pattern: Pattern.Variable value, Value: var decode }, Expr.If test]
                    }
                }
                || value.Name == VariableScopeNames.Wildcard || value.Name == source || value.Name == containingBinding
                || bindings.Contains(value.Name)
                || ReadSequenceCall(decode, source) is not { } decoder
                || (InferPairFacts(decoder.Call) & PairFacts.Integer) == 0
                || ReadTaggedBranchRange(test, value.Name, integerResult: true) is not { } bounds)
            {
                return null;
            }

            return (decoder, bounds);
        }

        private SequenceRead? ReadSingleRequiredSequenceCall(Expr expression, string payload)
        {
            return ReadDecodeCall(expression, payload, allowRemote: true) is { Convert: null } read
                ? new(
                    read.Decode,
                    read.Entry,
                    read.Tags,
                    read.Module
                )
                : null;
        }

        private SequenceOptional? ReadOptionalAfterSingleRequired(
            Expr.Case choice,
            string previous,
            IReadOnlySet<string> bindings
        )
        {
            var field = ReadDefaultSequenceField(
                choice,
                previous,
                bindings,
                allowRemote: true,
                allowLiteralMiddleArgument: true
            )
                ?? ReadBoundedTaggedSequenceField(choice, previous, bindings);

            return field is null
                ? null
                : new(
                    new(
                        field.Decoder,
                        field.Entry,
                        field.Tags,
                        field.Module,
                        field.MiddleArgument
                    ),
                    field.Tag,
                    field.Default,
                    field.Bounds
                );
        }

        private SequenceOptional? ReadOptionalSequencePart(Expr.Case choice, string previous, IReadOnlySet<string> bindings)
        {
            if (ReadDefaultSequenceField(choice, previous, bindings) is { } tagged)
            {
                return new(new(tagged.Decoder, tagged.Entry, tagged.Tags), tagged.Tag, tagged.Default);
            }

            if (ReadBoundedTaggedSequenceField(choice, previous, bindings) is { } bounded)
            {
                return new(
                    new(bounded.Decoder, bounded.Entry, bounded.Tags),
                    bounded.Tag,
                    bounded.Default,
                    bounded.Bounds
                );
            }

            if (choice is not
                {
                    Value: Expr.Variable tested,
                    Clauses:
                        [
                        {
                            Guard: null, Patterns: [Pattern.List { Items: [Pattern.Variable payload], Tail: Pattern.Variable tail }],
                            Body: Expr.Tuple { Items: [var call, Expr.Variable kept] }
                        },
                        {
                            Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                            Body: Expr.Tuple { Items: [var empty, Expr.Variable unchanged] }
                        }
                        ]
                }
                || tested.Name != previous || unchanged.Name != previous || kept.Name != tail.Name
                || !DistinctBindings(payload, tail) || bindings.Contains(payload.Name) || bindings.Contains(tail.Name)
                || ReadSequenceCall(call, payload.Name) is not { } decoder
                || !AttributeLiteral.TryNormalizeConstant(empty, out Term omitted))
            {
                return null;
            }

            return new(decoder, null, omitted);
        }
    }
}
