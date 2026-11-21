using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool HasDirectOptionalLengthProof(IReadOnlyList<SequenceEncodePart> fields)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || fields.Count < 3
                || fields[^1] is not { Omits: null, Call: { Module: null, DynamicRemote: false } }
                || fields[0] is not { Omits: Atom marker, Call: { Module: null, DynamicRemote: false } encoder })
            {
                return false;
            }

            if (!fields.Take(fields.Count - 1).All(field => field is
                {
                    Omits: Atom omitted,
                    Omitted: TupleTerm { Items: [BitString { BitLength: 0 }, Integer size] },
                    Call: { Module: null, DynamicRemote: false }
                }
                && omitted.Equals(marker) && field.Call.Function == encoder.Function && size.Value.IsZero))
            {
                return false;
            }

            // The absent alternatives contribute zero. Each evaluated encoder has a proven length in its own call context.
            return fields.All(field => (InferPairFacts(field.Call) & PairFacts.Length) != 0);
        }

        private bool EmitDefaultPrefixedSequence(
            Expr.Match unpack,
            IReadOnlyList<SequenceEncodePart> fields,
            DefaultEncodeField? first,
            int finishEntry,
            IReadOnlyList<Expr.Binary> additions,
            IReadOnlyList<DefaultEncodeField?>? retainedPredicates = null,
            RequiredPairEnumeration? requiredEnumeration = null
        )
        {
            int count = fields.Count;
            bool trimFinal = first is null && fields[^1].Omits is null;
            bool trimBeforeForeignAfterDefault = trimFinal && fields[^1].Call.Module is not null
                && retainedPredicates is [null, null, { Names: null }, null];
            bool reuseRecordSlot = fields[^1].Omits is not null
                && fields[^2] is { Omits: null, Call.Module: null };
            int frame = reuseRecordSlot ? count : count + 1;
            int recordSlot = frame - 1;
            int tagsSlot = frame - 2;
            int firstSlot = frame - 3;

            bool reverseLeadingSlots = retainedPredicates is not null && fields[0].Omits is null
                && retainedPredicates[1] is not null;

            int PairSlot(int index)
            {
                if (trimBeforeForeignAfterDefault && index is 1 or 2)
                {
                    return firstSlot - (3 - index);
                }

                if (reverseLeadingSlots && index < 2)
                {
                    return firstSlot - (1 - index);
                }

                return reuseRecordSlot && index == count - 2 ? recordSlot : firstSlot - index;
            }

            var optional = new Dictionary<int, (int Encode, int Continue)>();
            for (int index = 0; index < count; index++)
            {
                if (fields[index].Omits is not null)
                {
                    optional.Add(index, (Label(), Label()));
                }
            }

            var failedReturns = new Dictionary<int, int>();
            for (int index = count - 1; index >= (first is null ? 0 : 1); index--)
            {
                if (fields[index].Call.Module is not null)
                {
                    failedReturns.Add(index, Label());
                }
            }

            int badEnumeration = requiredEnumeration is null ? 0 : Label();
            int badInput = Label();
            owner.EmitTupleCheck(0, count + 1, badInput);
            EmitFrameAllocation(arity: 2, size: frame);
            EmitFrameInitialization(0, firstSlot + (first is null ? 1 : 0));
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                tagsSlot
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                recordSlot
            );
            BeamSourceLocation previous = location;
            if (first is not null)
            {
                owner.Op(GetTupleElement, XRegisterTag, 0);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Operand(YRegisterTag, firstSlot);
                owner.MoveToX0(YRegisterTag, firstSlot);
                owner.EmitCallLine(first.Predicate, location);
                owner.EmitFieldCall(1, first.PredicateEntry);
                previous = CallLocation(first.Predicate, location);

                var firstBranch = optional[0];
                CompareMarker(0, Term.A(ErlangBooleanAtoms.True), firstBranch.Encode);
                MoveOmitted(first.Default, YRegisterTag, firstSlot);
                JumpTo(firstBranch.Continue);
                Mark(firstBranch.Encode);
                owner.EmitConstant(first.Names!, 1);
                owner.EmitConstant(first.Tags, 2);
                owner.MoveToX0(YRegisterTag, firstSlot);
                owner.InitStackSlot(firstSlot);
                owner.EmitCallLine(first.Encode, previous);
                previous = CallLocation(first.Encode, previous);
                owner.EmitFieldCall(3, first.EncodeEntry);
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    firstSlot
                );
                Mark(firstBranch.Continue);
            }

            bool pending = false;
            for (int index = first is null ? 0 : 1; index < count; index++)
            {
                var field = fields[index];
                int slot = PairSlot(index);
                bool last = index == count - 1;
                var predicate = retainedPredicates?[index];
                if (index == 2 && requiredEnumeration is not null)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        PairSlot(index - 1)
                    );
                    owner.Op(GetTupleElement, YRegisterTag, recordSlot);
                    owner.Operand(BeamWire.UnsignedTag, index + 1);
                    owner.Operand(XRegisterTag, 0);
                    EmitRequiredPairEnumeration(requiredEnumeration, slot, badEnumeration);
                    pending = false;
                    continue;
                }

                int projectedRegister = predicate is null ? pending ? 1 : 0 : last ? recordSlot : slot;
                owner.Op(GetTupleElement, index == 0 ? XRegisterTag : YRegisterTag, index == 0 ? 0 : recordSlot);
                owner.Operand(BeamWire.UnsignedTag, index + 1);
                owner.Operand(predicate is null ? XRegisterTag : YRegisterTag, projectedRegister);
                if (pending)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        PairSlot(index - 1)
                    );
                }

                if (predicate is not null)
                {
                    var branch = optional[index];
                    owner.MoveToX0(YRegisterTag, projectedRegister);
                    owner.EmitCallLine(predicate.Predicate, previous);
                    previous = CallLocation(predicate.Predicate, previous);
                    owner.EmitFieldCall(1, predicate.PredicateEntry);
                    CompareMarker(0, Term.A(ErlangBooleanAtoms.True), branch.Encode);
                    MoveOmitted(predicate.Default, last ? XRegisterTag : YRegisterTag, last ? 0 : slot);
                    JumpTo(branch.Continue);
                    Mark(branch.Encode);
                    if (predicate.Names is { } names)
                    {
                        owner.EmitConstant(names, 1);
                    }

                    owner.EmitConstant(predicate.Tags, predicate.Names is null ? 1 : 2);
                    owner.MoveToX0(YRegisterTag, projectedRegister);
                    owner.InitStackSlot(projectedRegister);
                    owner.EmitCallLine(field.Call, previous);
                    previous = CallLocation(field.Call, previous);
                    owner.EmitFieldCall(field.Call.Arguments.Count, field.Entry);
                    if (!last)
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            0,
                            YRegisterTag,
                            slot
                        );
                    }

                    Mark(branch.Continue);
                    pending = false;
                    continue;
                }

                if (field.Omits is { } omitted)
                {
                    var branch = optional[index];
                    CompareMarker(projectedRegister, omitted, branch.Encode);
                    MoveOmitted(field.Omitted!, last ? XRegisterTag : YRegisterTag, last ? 0 : slot);
                    JumpTo(branch.Continue);
                    Mark(branch.Encode);
                    EmitEncoder(field, projectedRegister, last);
                    if (!last)
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            0,
                            YRegisterTag,
                            slot
                        );
                    }

                    Mark(branch.Continue);
                    if (failedReturns.TryGetValue(index, out int invalidOptional))
                    {
                        EmitTupleShapeTest(
                            last ? XRegisterTag : YRegisterTag,
                            last ? 0 : slot,
                            2,
                            invalidOptional
                        );
                    }

                    pending = false;
                    continue;
                }

                EmitEncoder(field, projectedRegister, last);
                pending = true;
                if (failedReturns.TryGetValue(index, out int invalid))
                {
                    if (!last)
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            0,
                            YRegisterTag,
                            slot
                        );
                    }

                    owner.EmitTupleCheck(0, 2, invalid);
                    pending = false;
                }
            }

            int[] lengths = first is null ? new int[count] : fields.Select(field => ReadNumericPairLengthType(field.Call)).ToArray();
            bool naturalLengths = first is null && requiredEnumeration is null
                && retainedPredicates?.Any(predicate => predicate is not null) != true
                && HasDirectOptionalLengthProof(fields);
            int[]? pairSlots = null;
            if (reuseRecordSlot || trimFinal || reverseLeadingSlots)
            {
                int displacedPair = trimBeforeForeignAfterDefault ? count - 3 : count - 2;
                pairSlots = Enumerable.Range(0, count).Select(index => trimFinal
                    ? (index == displacedPair ? recordSlot : PairSlot(index)) - 1 : PairSlot(index)).ToArray();
            }

            int trimmed = trimFinal ? 1 : 0;
            EmitSequencePairResult(
                count,
                frame - trimmed,
                finishEntry,
                additions,
                previous,
                pairSlots: pairSlots,
                outerTagSlot: tagsSlot - trimmed,
                joinedLengthTypes: lengths,
                naturalLengths: naturalLengths
            );
            if (requiredEnumeration is not null)
            {
                Mark(badEnumeration);
                EmitTaggedExit(
                    requiredEnumeration.Failure,
                    requiredEnumeration.Tags,
                    location,
                    framed: true,
                    frameSlots: frame
                );
            }

            foreach (var failure in failedReturns)
            {
                Mark(failure.Value);
                var context = location;
                var field = fields[failure.Key];
                int line = field.Omits is null || field.MatchLine == 0 ? field.Call.SourceLine : field.MatchLine;
                owner.EmitArithmeticLine(line, ref context);
                if (field.Omits is not null && failure.Key != count - 1)
                {
                    owner.Op(BadMatch, YRegisterTag, PairSlot(failure.Key));
                }
                else
                {
                    owner.Op(BadMatch, XRegisterTag, 0);
                }
            }

            Mark(badInput);
            previous = location;
            owner.EmitArithmeticLine(unpack.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            void CompareMarker(int register, Term marker, int label)
            {
                owner.Op(ExactEqualTest, LabelTag, label);
                owner.Operand(XRegisterTag, register);
                owner.LiteralOperand(marker);
            }

            void MoveOmitted(Term value, int tag, int register)
            {
                owner.instructions.WriteByte(BeamEmissionFormat.Move);
                owner.LiteralOperand(value);
                owner.Operand(tag, register);
            }

            void EmitEncoder(SequenceEncodePart field, int source, bool last)
            {
                if (source != 0)
                {
                    owner.MoveToX0(XRegisterTag, source);
                }

                for (int option = field.Constants.Count - 1; option >= 0; option--)
                {
                    owner.EmitConstant(field.Constants[option], option + 1);
                }

                if (last && trimFinal)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        0,
                        YRegisterTag,
                        recordSlot
                    );
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                    owner.Operand(BeamWire.UnsignedTag, frame - 1);
                }
                else if (last && !reuseRecordSlot)
                {
                    owner.InitStackSlot(recordSlot);
                }

                owner.EmitCallLine(field.Call, previous);
                previous = CallLocation(field.Call, previous);
                int arity = field.Constants.Count + 1;
                if (field.Call.Module is { } module)
                {
                    Call(module, field.Call.Function, arity);
                }
                else
                {
                    owner.EmitFieldCall(arity, field.Entry);
                }
            }
        }
    }
}
