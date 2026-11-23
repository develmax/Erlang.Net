using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record SortedListPreparation(
            Expr.Call Source,
            int SourceEntry,
            Expr.Fun Callback,
            Expr.Call Map,
            int MapEntry,
            Expr.Call Sort,
            Expr.Call Project,
            int ProjectEntry,
            string Result,
            HashSet<string> Bound
        );

        private SortedListPreparation? ReadSortedListPreparation(Clause clause, IReadOnlyList<Expr> items)
        {
            if (clause.Patterns is not [Pattern.Variable input, Pattern.Variable tags]
                || items is not
                [
                    Expr.Match
                {
                    Pattern: Pattern.Variable prepared,
                    Value: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable actualInput, Expr.Variable actualTags]
                    } source
                },
                    Expr.Match
                {
                    Pattern: Pattern.Variable callback,
                    Value: Expr.Fun { SelfName: null, CoreOrdinal: not null, Clauses: [{ Guard: null, Patterns: [Pattern.Variable { }] } body] } closure
                },
                    Expr.Match
                {
                    Pattern: Pattern.Variable mapped,
                    Value: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable mapInput, Expr.Variable mapCallback]
                    } map
                },
                    Expr.Match
                {
                    Pattern: Pattern.Variable projected,
                    Value: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments:
                            [
                                Expr.Call { Module: "lists", Function: "sort", DynamicRemote: false, Arguments: [Expr.Variable ranked] } sort
                            ]
                    } project
                },
                    ..
                ]
                || !DistinctBindings(
                    input,
                    tags,
                    prepared,
                    callback,
                    mapped,
                    projected
                )
                || actualInput.Name != input.Name || actualTags.Name != tags.Name
                || mapInput.Name != prepared.Name || mapCallback.Name != callback.Name || ranked.Name != mapped.Name
                || ClosureCaptures.ReadEntryReferences(body, [input.Name, tags.Name, prepared.Name]) is not { Count: 0 }
                || !entries.TryGetValue((source.Function, 2), out int sourceEntry)
                || !entries.TryGetValue((map.Function, 2), out int mapEntry)
                || !entries.TryGetValue((project.Function, 1), out int projectEntry)
                || !localFunctions.TryGetValue((map.Function, 2), out var mapping) || mapping.StaticListClosureTarget is null
                || !localFunctions.TryGetValue((project.Function, 1), out var projection) || !projection.HasProvenRecursiveTupleInput)
            {
                return null;
            }

            return new(
                source,
                sourceEntry,
                closure,
                map,
                mapEntry,
                sort,
                project,
                projectEntry,
                projected.Name,
                new(StringComparer.Ordinal) { input.Name, tags.Name, prepared.Name, callback.Name, mapped.Name, projected.Name }
            );
        }

        private BeamSourceLocation EmitSortedListPreparation(SortedListPreparation plan)
        {
            owner.EmitCallLine(plan.Source, location);
            owner.EmitFieldCall(2, plan.SourceEntry);
            variables = new(StringComparer.Ordinal);
            owner.EmitClosureAllocation(0, 1, 1);
            EmitGeneralClosure(plan.Callback, heapChecked: true, destination: 1);
            BeamSourceLocation previous = CallLocation(plan.Source, location);
            owner.EmitCallLine(plan.Map, previous);
            owner.EmitFieldCall(2, plan.MapEntry);
            previous = CallLocation(plan.Map, previous);
            owner.EmitCallLine(plan.Sort, previous);
            Call(plan.Sort.Module, plan.Sort.Function, 1);
            previous = CallLocation(plan.Sort, previous);
            owner.EmitCallLine(plan.Project, previous);
            owner.EmitFieldCall(1, plan.ProjectEntry);

            return CallLocation(plan.Project, previous);
        }

        private bool TryEmitSortedOptionalDecoderPair(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Clauses is not [{ Guard: null, Body: Expr.Sequence sequence } clause]
                || sequence.Items is not
                [
                    _, _, _, _,
                    Expr.Match { Pattern: Pattern.Tuple { Items: [Pattern.Variable left, Pattern.Variable leftTail] }, Value: Expr.Case first },
                    Expr.Match { Pattern: Pattern.Tuple { Items: [Pattern.Variable right, Pattern.Variable rightTail] }, Value: Expr.Case second },
                    Expr.Case tailCheck,
                    Expr.Match
                {
                    Pattern: Pattern.Variable result,
                    Value: Expr.Tuple
                    {
                        Items: [Expr.Literal { Value: Atom record }, Expr.Variable firstResult, Expr.Variable lastResult]
                    }
                },
                    Expr.Variable returned
                ]
                || left.Name != firstResult.Name || right.Name != lastResult.Name || result.Name != returned.Name
                || ReadSortedListPreparation(clause, sequence.Items) is not { } sorted
                || !DistinctBindings(
                    left,
                    leftTail,
                    right,
                    rightTail,
                    result
                )
                || !sorted.Bound.Add(left.Name) || !sorted.Bound.Add(leftTail.Name) || !sorted.Bound.Add(right.Name)
                || !sorted.Bound.Add(rightTail.Name) || !sorted.Bound.Add(result.Name)
                || ReadOptionalDecode(
                    first,
                    sorted.Result,
                    sorted.Bound,
                    allowMeasurement: true
                ) is not { } leftField
                || ReadOptionalDecode(
                    second,
                    leftTail.Name,
                    sorted.Bound,
                    allowMeasurement: true
                ) is not
                { Measurement: not null } rightField
                || ReadTableUnexpectedFields(tailCheck, rightTail.Name) is not { } extra)
            {
                return false;
            }

            const int frame = 3;
            EmitGeneralFrame(2, slots: frame);
            BeamSourceLocation previous = EmitSortedListPreparation(sorted);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                2
            );
            previous = EmitSortedOptionalField(
                leftField,
                source: 2,
                sourceInX: true,
                tail: 1,
                result: 2,
                frame,
                previous
            );
            previous = EmitSortedOptionalField(
                rightField,
                source: 1,
                sourceInX: false,
                tail: 0,
                result: null,
                frame,
                previous
            );
            EmitSortedOptionalRecord(
                record,
                [2],
                extra,
                frame,
                previous
            );

            return true;
        }

        private bool TryEmitSortedRequiredDecoder(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Clauses is not [{ Guard: null, Body: Expr.Sequence sequence } clause]
                || sequence.Items is not
                [
                    _, _, _, _,
                    Expr.Match
                {
                    Pattern: Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail },
                    Value: Expr.Variable input
                } split,
                    Expr.Match { Pattern: Pattern.Variable required, Value: Expr.Block body },
                    var first, var second, var third,
                    Expr.Case tailCheck,
                    Expr.Match
                {
                    Pattern: Pattern.Variable result,
                    Value: Expr.Tuple
                    {
                        Items:
                            [
                                Expr.Literal { Value: Atom record },
                                Expr.Variable requiredResult,
                                Expr.Variable firstResult,
                                Expr.Variable secondResult,
                                Expr.Variable thirdResult
                            ]
                    }
                },
                    Expr.Variable returned
                ]
                || requiredResult.Name != required.Name || returned.Name != result.Name
                || ReadSortedListPreparation(clause, sequence.Items) is not { } sorted || input.Name != sorted.Result
                || body.Body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match { Pattern: Pattern.Variable decoded },
                        Expr.Match { Pattern: Pattern.Variable measured }, _
                    ]
                }
                || !DistinctBindings(
                    head,
                    tail,
                    required,
                    result,
                    decoded,
                    measured
                )
                || !sorted.Bound.Add(head.Name) || !sorted.Bound.Add(tail.Name) || !sorted.Bound.Add(required.Name)
                || !sorted.Bound.Add(result.Name) || !sorted.Bound.Add(decoded.Name) || !sorted.Bound.Add(measured.Name)
                || ReadForwardedChoiceMeasurement(body, head.Name, tail.Name) is not
                { Measure.Function: AutoImportNames.ByteSize, Convert: not null } requiredField)
            {
                return false;
            }

            string remainder = tail.Name;
            var fields = new List<OptionalDecode>();
            foreach (var (expression, output) in new[] { (first, firstResult), (second, secondResult), (third, thirdResult) })
            {
                if (expression is not Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable value, Pattern.Variable rest] },
                        Value: Expr.Case decision
                    }
                    || value.Name != output.Name || !DistinctBindings(value, rest) || !sorted.Bound.Add(value.Name)
                    || !sorted.Bound.Add(rest.Name)
                    || ReadOptionalDecode(
                        decision,
                        remainder,
                        sorted.Bound,
                        allowMeasurement: true
                    ) is not
                    { Measurement: not null } optional)
                {
                    return false;
                }

                fields.Add(optional);
                remainder = rest.Name;
            }

            if (ReadTableUnexpectedFields(tailCheck, remainder) is not { } extra)
            {
                return false;
            }

            const int frame = 5;
            EmitGeneralFrame(2, slots: frame);
            var previous = EmitSortedListPreparation(sorted);
            // Each measured optional uses three labels. The required failures follow them and the final tail check.
            int requiredRange = owner.nextLabel + fields.Count * 3 + 1;
            int missingRequired = requiredRange + 1;
            owner.Op(IsNonemptyList, LabelTag, missingRequired);
            owner.Operand(XRegisterTag, 0);
            owner.EmitListParts(
                0,
                XRegisterTag,
                0,
                YRegisterTag,
                4
            );
            EmitDecodeCall(requiredField.Decoder, previous);
            previous = EmitForwardedChoiceMeasurementCheck(
                requiredField,
                requiredRange,
                CallLocation(requiredField.Decoder.Decode, previous)
            );
            previous = CallLocation(requiredField.Convert!, previous);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                3
            );

            previous = EmitSortedOptionalField(
                fields[0],
                source: 4,
                sourceInX: false,
                tail: 2,
                result: 4,
                frame,
                previous
            );
            previous = EmitSortedOptionalField(
                fields[1],
                source: 2,
                sourceInX: false,
                tail: 1,
                result: 2,
                frame,
                previous
            );
            previous = EmitSortedOptionalField(
                fields[2],
                source: 1,
                sourceInX: false,
                tail: 0,
                result: null,
                frame,
                previous
            );
            EmitSortedOptionalRecord(
                record,
                [3, 4, 2],
                extra,
                frame,
                previous
            );
            Mark(Label());
            owner.EmitConstant(requiredField.Reason, 0);
            owner.EmitCallLine(requiredField.Exit, previous);
            EmitCheckedDecoderExit(requiredField.Exit, frame);
            Mark(Label());
            var badMatchLocation = location;
            owner.EmitArithmeticLine(split.SourceLine, ref badMatchLocation);
            // A failed nonempty-list test on the proven projection can only see nil.
            owner.Op(BadMatch, AtomTag, NilAtomIndex);
            ListFailureCount = 1;

            return true;
        }

        private bool TryEmitPreparedOptionalFields(
            IReadOnlyList<Expr> items,
            Pattern.Variable first,
            Pattern.Variable second,
            Pattern.Variable list,
            Expr.Call read
        )
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || items is not
                [
                    _, .., Expr.Case tailCheck,
                    Expr.Match
                {
                    Pattern: Pattern.Variable output,
                    Value: Expr.Tuple { Items: [Expr.Literal { Value: Atom record }, ..] and var values }
                },
                    Expr.Variable returned
                ]
                || values.Count < 4 || items.Count != values.Count + 3 || returned.Name != output.Name
                || !DistinctBindings(
                    first,
                    second,
                    list,
                    output
                )
                || !entries.TryGetValue((read.Function, 2), out int reader))
            {
                return false;
            }

            HashSet<string> bound = new(StringComparer.Ordinal) { first.Name, second.Name, list.Name, output.Name };
            var fields = new List<OptionalDecode>();
            string remainder = list.Name;
            for (int index = 0; index < values.Count - 1; index++)
            {
                if (items[index + 1] is not Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable value, Pattern.Variable tail] },
                        Value: Expr.Case decision
                    }
                    || values[index + 1] is not Expr.Variable actual || actual.Name != value.Name
                    || !DistinctBindings(value, tail) || !bound.Add(value.Name) || !bound.Add(tail.Name)
                    || ReadOptionalDecode(
                        decision,
                        remainder,
                        bound,
                        allowMeasurement: true
                    ) is not { Call: { Module: null, Tags: Nil, Convert: null } } field)
                {
                    return false;
                }

                fields.Add(field);
                remainder = tail.Name;
            }

            if (!fields.Any(field => field.Measurement is not null)
                || ReadTableUnexpectedFields(tailCheck, remainder) is not { } extra)
            {
                return false;
            }

            int frame = fields.Count + 1;
            EmitGeneralFrame(2, slots: frame);
            owner.EmitCallLine(read, location);
            owner.EmitFieldCall(2, reader);
            var previous = CallLocation(read, location);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                frame - 1
            );
            var slots = new List<int>();
            for (int index = 0; index < fields.Count; index++)
            {
                int source = frame - 1 - index;
                bool terminal = index == fields.Count - 1;
                previous = EmitSortedOptionalField(
                    fields[index],
                    source,
                    sourceInX: index == 0,
                    tail: source - 1,
                    result: terminal ? null : source,
                    frame,
                    previous
                );
                if (!terminal)
                {
                    slots.Add(source);
                }
            }

            EmitSortedOptionalRecord(
                record,
                slots,
                extra,
                frame,
                previous,
                properList: false
            );

            return true;
        }

        private bool TryEmitRequiredAndSizedOptional(
            IReadOnlyList<Expr> items,
            Pattern.Variable first,
            Pattern.Variable second,
            Pattern.Variable list,
            Expr.Call read
        )
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || items is not
                [
                    _,
                    Expr.Match
                {
                    Pattern: Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail }
                } split,
                    Expr.Match { Pattern: Pattern.Variable required, Value: Expr.Block body },
                    Expr.Match
                {
                    Pattern: Pattern.Tuple { Items: [Pattern.Variable value, Pattern.Variable rest] },
                    Value: Expr.Case decision
                },
                    Expr.Case tailCheck,
                    Expr.Match
                {
                    Pattern: Pattern.Variable result,
                    Value: Expr.Tuple
                    {
                        Items: [Expr.Literal { Value: Atom record }, Expr.Variable left, Expr.Variable right]
                    }
                },
                    Expr.Variable returned
                ]
                || left.Name != required.Name || right.Name != value.Name || returned.Name != result.Name
                || body.Body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match { Pattern: Pattern.Variable decoded },
                        Expr.Match { Pattern: Pattern.Variable measured }, _
                    ]
                }
                || !DistinctBindings(
                    first,
                    second,
                    list,
                    head,
                    tail,
                    required,
                    value,
                    rest,
                    result,
                    decoded,
                    measured
                )
                || ReadForwardedChoiceMeasurement(body, head.Name, tail.Name) is not
                { Measure.Function: AutoImportNames.ByteSize, Convert: not null } requiredField
                || ReadOptionalDecode(
                    decision,
                    tail.Name,
                    new(StringComparer.Ordinal)
                    {
                        first.Name, second.Name, list.Name, head.Name, tail.Name, required.Name, value.Name,
                        rest.Name, result.Name, decoded.Name, measured.Name
                    },
                    allowMeasurement: true
                ) is not { Measurement: not null } optional
                || ReadTableUnexpectedFields(tailCheck, rest.Name) is not { } extra
                || !entries.TryGetValue((read.Function, 2), out int reader))
            {
                return false;
            }

            const int frame = 3;
            EmitGeneralFrame(2, slots: frame);
            owner.EmitCallLine(read, location);
            owner.EmitFieldCall(2, reader);
            var previous = CallLocation(read, location);
            // The optional field and final tail check allocate the first four labels.
            int requiredRange = owner.nextLabel + 4;
            int missingRequired = requiredRange + 1;
            owner.Op(IsNonemptyList, LabelTag, missingRequired);
            owner.Operand(XRegisterTag, 0);
            owner.EmitListParts(
                0,
                XRegisterTag,
                0,
                YRegisterTag,
                2
            );
            EmitDecodeCall(requiredField.Decoder, previous);
            previous = EmitForwardedChoiceMeasurementCheck(
                requiredField,
                requiredRange,
                CallLocation(requiredField.Decoder.Decode, previous)
            );
            previous = CallLocation(requiredField.Convert!, previous);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            previous = EmitSortedOptionalField(
                optional,
                source: 2,
                sourceInX: false,
                tail: 0,
                result: null,
                frame,
                previous
            );
            EmitSortedOptionalRecord(
                record,
                [1],
                extra,
                frame,
                previous,
                properList: false
            );
            Mark(Label());
            owner.EmitConstant(requiredField.Reason, 0);
            owner.EmitCallLine(requiredField.Exit, previous);
            EmitCheckedDecoderExit(requiredField.Exit, frame);
            EmitDecoderFailures([(Label(), null, split)]);

            return true;
        }

        private void EmitSortedOptionalRecord(
            Atom record,
            IReadOnlyList<int> fields,
            TableUnexpectedFields extra,
            int frame,
            BeamSourceLocation previous,
            bool properList = true
        )
        {
            int unexpected = Label();
            owner.Op(IsNil, LabelTag, unexpected);
            if (properList)
            {
                owner.EmitTypedRegister(1, ProperListTypeBits);
            }
            else
            {
                owner.Operand(XRegisterTag, 1);
            }
            int arity = fields.Count + 2;
            owner.Op(TestHeap, BeamWire.UnsignedTag, arity + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, arity);
            owner.LiteralOperand(record);
            foreach (int slot in fields)
            {
                owner.Operand(YRegisterTag, slot);
            }

            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame);

            Mark(unexpected);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 9);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitProtectedPair(AtomTag, owner.Intern(extra.UnexpectedTag.Name), 1);
            owner.EmitProtectedPair(AtomTag, owner.Intern(extra.DetailsTag.Name));
            owner.EmitProtectedPair(AtomTag, owner.Intern(extra.ErrorTag.Name));
            owner.EmitCallLine(extra.Exit, previous);
            EmitCheckedDecoderExit(extra.Exit, frame);
        }

        private BeamSourceLocation EmitSortedOptionalField(
            OptionalDecode field,
            int source,
            bool sourceInX,
            int tail,
            int? result,
            int frame,
            BeamSourceLocation previous
        )
        {
            int invalid = field.Measurement is null ? 0 : Label();
            int missing = Label();
            int done = Label();
            int sourceRegister = sourceInX ? 0 : source;
            int sourceTag = sourceInX ? XRegisterTag : YRegisterTag;
            owner.Op(IsNonemptyList, LabelTag, missing);
            owner.Operand(sourceTag, sourceRegister);
            owner.Op(GetList, sourceTag, sourceRegister);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, tail);
            owner.EmitTupleCheck(0, 2, missing);
            owner.EmitTupleElement(0, 1);
            owner.Op(ExactEqualTest, LabelTag, missing);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(field.Tag);
            owner.EmitTupleElement(1, 0);
            owner.EmitConstant(field.Call.Tags, 1);
            owner.InitStackSlot(source);
            EmitDecodeInvocation(field.Call, previous);
            previous = CallLocation(field.Call.Convert ?? field.Call.Decode, previous);
            if (field.Measurement is { } measurement)
            {
                previous = EmitForwardedChoiceMeasurementCheck(measurement, invalid, previous);
                previous = CallLocation(measurement.Convert!, previous);
            }

            if (result is int saved)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    saved
                );
            }
            else
            {
                Load(tail, 1);
            }

            JumpTo(done);
            if (field.Measurement is { } checkedLength)
            {
                Mark(invalid);
                owner.EmitConstant(checkedLength.Reason, 0);
                owner.EmitCallLine(checkedLength.Exit, previous);
                EmitCheckedDecoderExit(checkedLength.Exit, frame);
            }

            Mark(missing);
            if (result is int fallback)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    source,
                    YRegisterTag,
                    tail
                );
                owner.EmitConstant(field.Absent, fallback, YRegisterTag);
            }
            else
            {
                Load(source, 1);
                owner.EmitConstant(field.Absent, 0);
            }

            Mark(done);

            return previous;
        }
    }
}
