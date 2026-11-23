using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool EmitRequiredDefaultRequired(
            Expr.Match unpack,
            SequenceEncodePart first,
            DefaultEncodeField optional,
            SequenceEncodePart final,
            int finishEntry,
            IReadOnlyList<Expr.Binary> additions
        )
        {
            const int record = 3;
            const int outerTags = 2;
            const int optionalSlot = 1;
            const int firstPair = 0;
            int present = Label();
            int ready = Label();
            int inputFailure = Label();

            owner.EmitTupleCheck(0, 4, inputFailure);
            EmitFrameAllocation(arity: 2, size: 4);
            EmitFrameInitialization(0, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                outerTags
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                record
            );
            owner.EmitTuplePart(0, 1, 0);
            BeamSourceLocation previous = location;
            EmitMandatory(first);

            Project(2, YRegisterTag, optionalSlot);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                firstPair
            );
            owner.MoveToX0(YRegisterTag, optionalSlot);
            owner.EmitCallLine(optional.Predicate, previous);
            previous = CallLocation(optional.Predicate, previous);
            owner.EmitFieldCall(1, optional.PredicateEntry);
            owner.Op(ExactEqualTest, LabelTag, present);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(Term.A(ErlangBooleanAtoms.True));
            owner.instructions.WriteByte(BeamEmissionFormat.Move);
            owner.LiteralOperand(optional.Default);
            owner.Operand(YRegisterTag, optionalSlot);
            JumpTo(ready);

            Mark(present);
            owner.EmitConstant(optional.Tags, 1);
            owner.MoveToX0(YRegisterTag, optionalSlot);
            EmitFrameInitialization(optionalSlot, optionalSlot + 1);
            owner.EmitCallLine(optional.Encode, previous);
            previous = CallLocation(optional.Encode, previous);
            owner.EmitFieldCall(2, optional.EncodeEntry);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                optionalSlot
            );
            Mark(ready);

            Project(3, XRegisterTag, 0);
            for (int index = final.Constants.Count - 1; index >= 0; index--)
            {
                owner.EmitConstant(final.Constants[index], index + 1);
            }

            owner.MoveRegister(
                YRegisterTag,
                firstPair,
                YRegisterTag,
                record
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.EmitCallLine(final.Call, previous);
            previous = CallLocation(final.Call, previous);
            owner.EmitFieldCall(final.Constants.Count + 1, final.Entry);
            EmitSequencePairResult(
                3,
                3,
                finishEntry,
                additions,
                previous,
                pairSlots: [2, 0],
                outerTagSlot: 1,
                joinedLengthTypes:
                [
                    ReadNumericPairLengthType(first.Call),
                    ReadNumericPairLengthType(optional.Encode),
                    ReadNumericPairLengthType(final.Call)
                ]
            );
            Mark(inputFailure);
            previous = location;
            owner.EmitArithmeticLine(unpack.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            void EmitMandatory(SequenceEncodePart field)
            {
                for (int index = field.Constants.Count - 1; index >= 0; index--)
                {
                    owner.EmitConstant(field.Constants[index], index + 1);
                }

                owner.EmitCallLine(field.Call, previous);
                previous = CallLocation(field.Call, previous);
                owner.EmitFieldCall(field.Constants.Count + 1, field.Entry);
            }

            void Project(int index, int targetTag, int target)
            {
                owner.Op(GetTupleElement, YRegisterTag, record);
                owner.Operand(BeamWire.UnsignedTag, index);
                owner.Operand(targetTag, target);
            }
        }
    }
}
