using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitForwardedTupleChoices(
            Expr.Case decision,
            Expr.Call prepare,
            int prepareEntry,
            string prepared
        )
        {
            if (decision.Clauses.Count < 3
                || decision.Clauses[^1] is not { Guard: null, Patterns: [Pattern.Variable fallback] } rejected
                || ReadTaggedExit(rejected.Body, fallback.Name) is not { } error)
            {
                return false;
            }

            var order = Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var branches = new SortedDictionary<Term, (
                Atom Tag, DecodeCall Decode, SequenceRead? Rename, bool Whole,
                ForwardedChoiceMeasurement? Measurement, TaggedBranchRange? Range
            )>(order);
            foreach (var clause in decision.Clauses.SkipLast(1))
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns: [var pattern],
                        Body: Expr.Tuple
                        {
                            Items:
                                [
                                    Expr.Literal { Value: Atom tag },
                                    var body
                                ]
                        }
                    }
                    || ReadForwardedChoicePattern(pattern) is not { } selected
                )
                {
                    return false;
                }

                var named = ReadSequenceNamedValue(body, selected.Binding, new(StringComparer.Ordinal) { selected.Binding });
                ForwardedChoiceMeasurement? measurement = null;
                TaggedBranchRange? range = null;
                var decoder = ReadDecodeCall(named?.Decode.Call ?? body, selected.Binding, allowRemote: true);
                if (decoder is null && !selected.Whole && selected.Binding != prepared && fallback.Name != prepared)
                {
                    measurement = ReadForwardedChoiceMeasurement(body, selected.Binding, prepared);
                    decoder = measurement?.Decoder;
                    var reserved = new HashSet<string>(StringComparer.Ordinal) { selected.Binding, prepared, fallback.Name };
                    if (decoder is null && ReadSequenceBounds(
                        body,
                        selected.Binding,
                        reserved
                    ) is { Bounds.Upper: not null } bounded && IsDecoderIntrinsic(bounded.Bounds.Exit))
                    {
                        decoder = new DecodeCall(
                            bounded.Read.Call,
                            null,
                            bounded.Read.Options,
                            bounded.Read.Entry
                        );
                        range = bounded.Bounds;
                    }
                }

                if (decoder is null || !branches.TryAdd(selected.Key, (tag, decoder, named?.Rename, selected.Whole, measurement, range)))
                {
                    return false;
                }
            }

            if (branches.Values.Any(branch => branch.Range is not null)
                && (!IsDecoderIntrinsic(error.Call) || branches.Values.Count(branch => branch.Range is not null) != 1
                    || branches.Values.Any(branch => branch.Whole || branch.Measurement is not null || branch.Rename is not null)))
            {
                return false;
            }

            if (branches.Values.Any(branch => branch.Measurement is not null)
                && (!IsDecoderIntrinsic(error.Call) || branches.Values.Any(branch => branch.Whole)
                    || decision.Clauses.SkipLast(1).Any(clause => ReadForwardedChoicePattern(clause.Patterns[0])?.Binding == prepared)))
            {
                return false;
            }

            EmitFrameAllocation(2, 0);
            owner.EmitCallLine(prepare, location);
            owner.EmitFieldCall(2, prepareEntry);
            EmitSingletonUnwrap();
            var schedule = branches.Reverse().Select(pair => (
                pair.Key, pair.Value, Label: Label(), Failure: pair.Value.Measurement is null && pair.Value.Range is null ? 0 : Label()
            )).ToArray();
            int failure = Label();
            owner.EmitTupleCheck(0, 2, failure);
            owner.EmitTuplePart(0, 0, 1);
            bool keepsWholeTuple = branches.Values.Any(branch => branch.Whole);
            if (!keepsWholeTuple)
            {
                owner.EmitTuplePart(0, 1, 2);
            }

            EmitAritySelect(1, schedule.Length, failure);
            foreach (var item in schedule.Reverse())
            {
                owner.LiteralOperand(item.Key);
                owner.Operand(LabelTag, item.Label);
            }

            BeamSourceLocation previous = CallLocation(prepare, location);
            foreach (var (_, branch, label, rangeFailure) in schedule)
            {
                Mark(label);
                if (keepsWholeTuple && !branch.Whole)
                {
                    owner.EmitTuplePart(0, 1, 0);
                }

                owner.EmitConstant(branch.Decode.Tags, 1);
                if (!keepsWholeTuple)
                {
                    owner.MoveToX0(XRegisterTag, 2);
                }

                EmitDecodeInvocation(branch.Decode, previous);
                if (branch.Measurement is { } measured)
                {
                    EmitForwardedChoiceMeasurement(
                        branch.Tag,
                        measured,
                        rangeFailure,
                        previous
                    );

                    continue;
                }

                Expr.Call returned = branch.Decode.Convert ?? branch.Decode.Decode;
                if (branch.Range is { } bounded)
                {
                    EmitRangeTests(
                        returned,
                        bounded,
                        rangeFailure,
                        provenInteger: true
                    );
                    EmitTaggedRegisterResult(branch.Tag);
                    owner.EmitFrameReturn(0);
                    Mark(rangeFailure);
                    var decoderLocation = CallLocation(returned, previous);
                    var exitLocation = CallLocation(bounded.Exit, decoderLocation);
                    EmitRangeExit(bounded, exitLocation == decoderLocation ? null : exitLocation, 0);
                    continue;
                }

                if (branch.Rename is { } rename)
                {
                    owner.EmitConstant(rename.Options, 1);
                    owner.EmitCallLine(rename.Call, CallLocation(returned, previous));
                    owner.EmitFieldCall(2, rename.Entry);
                    returned = rename.Call;
                }

                EmitTaggedCallResult(branch.Tag, returned);
                owner.EmitFrameReturn(0);
            }

            Mark(failure);
            EmitTaggedExit(
                error.Call,
                error.Tags,
                previous,
                framed: true,
                frameSlots: 0
            );

            return true;
        }

        private static (Integer Key, string Binding, bool Whole)? ReadForwardedChoicePattern(Pattern pattern)
        {
            if (pattern is Pattern.Tuple
                {
                    Items: [Pattern.Literal { Value: Integer key }, Pattern.Variable payload]
                } && payload.Name != VariableScopeNames.Wildcard)
            {
                return (key, payload.Name, false);
            }

            if (pattern is AliasPattern alias && TryReadPatternAlias(alias, out var binding, out var shape)
                && binding.Name != VariableScopeNames.Wildcard
                && shape is Pattern.Tuple
                {
                    Items:
                    [
                        Pattern.Literal { Value: Integer tag },
                        Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    ]
                })
            {
                return (tag, binding.Name, true);
            }

            return null;
        }

        private bool EmitBoundedRequiredFields(
            Expr.Call match,
            Atom record,
            IReadOnlyList<(Expr.Match Split, DecodeCall Call)> fields,
            TaggedBranchRange? range,
            TableUnexpectedFields extra,
            (ForwardedChoiceMeasurement First, ForwardedChoiceMeasurement Second)? measurements = null
        )
        {
            const int frame = 3;
            int leftovers = Label();
            int secondRangeFailure = measurements.HasValue ? Label() : 0;
            int rangeFailure = Label();
            int missingSecond = Label();
            int missingFirst = Label();
            EmitGeneralFrame(2, slots: frame);
            owner.EmitCallLine(match, location);
            Call(match.Module, match.Function, 2);
            var previous = CallLocation(match, location);
            Unpack(
                XRegisterTag,
                0,
                2,
                missingFirst
            );
            var first = fields[0].Call;
            owner.EmitConstant(first.Tags, 1);
            EmitDecodeInvocation(first, previous);
            previous = CallLocation(first.Decode, previous);
            if (measurements is { } measuredFirst)
            {
                MeasureAndConvert(measuredFirst.First, rangeFailure);
            }

            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            if (range is not null)
            {
                EmitRangeTests(first.Decode, range, rangeFailure);
            }

            Unpack(
                YRegisterTag,
                2,
                0,
                missingSecond
            );
            var second = fields[1].Call;
            owner.EmitConstant(second.Tags, 1);
            owner.InitStackSlot(2);
            EmitDecodeInvocation(second, previous);
            previous = CallLocation(second.Decode, previous);
            if (measurements is { } measuredSecond)
            {
                MeasureAndConvert(measuredSecond.Second, secondRangeFailure);
            }

            owner.EmitNilTest(YRegisterTag, 0, leftovers);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 3);
            owner.LiteralOperand(record);
            owner.Operand(YRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame);

            Mark(leftovers);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 9);
            owner.Operand(BeamWire.UnsignedTag, 0);
            Pair(extra.UnexpectedTag, YRegisterTag);
            Pair(extra.DetailsTag, XRegisterTag);
            Pair(extra.ErrorTag, XRegisterTag);
            owner.EmitCallLine(extra.Exit, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, extra.Exit.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, frame);
            if (measurements is { } checkedFields)
            {
                EmitMeasurementExit(checkedFields.Second, secondRangeFailure);
                EmitMeasurementExit(checkedFields.First, rangeFailure);
            }
            else if (range is not null)
            {
                Mark(rangeFailure);
                EmitRangeExit(range, CallLocation(range.Exit, previous), frame);
            }

            EmitDecoderFailures([(missingFirst, null, fields[0].Split), (missingSecond, 2, fields[1].Split)]);

            return true;

            void MeasureAndConvert(ForwardedChoiceMeasurement measurement, int failure)
            {
                previous = EmitForwardedChoiceMeasurementCheck(
                    measurement,
                    failure,
                    previous,
                    emitConversion: false
                );
                var convert = measurement.Convert!;
                owner.EmitCallLine(convert, previous);
                Call(ErlangModuleNames.Module, convert.Function, 1);
                previous = CallLocation(convert, previous);
            }

            void EmitMeasurementExit(ForwardedChoiceMeasurement measurement, int failure)
            {
                Mark(failure);
                owner.EmitConstant(measurement.Reason, 0);
                owner.EmitCallLine(measurement.Exit, location);
                EmitCheckedDecoderExit(measurement.Exit, frame);
            }

            void Unpack(
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

            void Pair(Atom tag, int kind)
            {
                owner.EmitTupleHeader(XRegisterTag, 2);
                owner.LiteralOperand(tag);
                owner.Operand(kind, 0);
            }
        }
    }
}
