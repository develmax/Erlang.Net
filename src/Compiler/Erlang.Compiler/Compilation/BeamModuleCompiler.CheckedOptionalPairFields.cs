using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool EmitEagerDefaultOptionalPair(
            Expr.Match unpack,
            DefaultEncodeField first,
            DeferredOptional second,
            Expr.Binary sum,
            int finish
        )
        {
            const int frame = 3;
            int present = Label();
            int ready = Label();
            int badInput = owner.nextLabel + 2;
            owner.EmitTupleCheck(0, 3, badInput);
            EmitFrameAllocation(arity: 2, size: frame);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                2
            );
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 1);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(YRegisterTag, 0);
            owner.MoveToX0(YRegisterTag, 1);
            owner.EmitCallLine(first.Predicate, location);
            owner.EmitFieldCall(1, first.PredicateEntry);
            owner.Op(ExactEqualTest, LabelTag, present);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(Term.A(ErlangBooleanAtoms.True));
            owner.EmitConstant(first.Default, 1, YRegisterTag);
            JumpTo(ready);

            Mark(present);
            owner.EmitConstant(first.Tags, 1);
            owner.MoveToX0(YRegisterTag, 1);
            owner.InitStackSlot(1);
            owner.EmitCallLine(first.Encode, CallLocation(first.Predicate, location));
            owner.EmitFieldCall(2, first.EncodeEntry);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            Mark(ready);
            EmitDeferredOptional(
                second,
                -1,
                0,
                inputSlot: 0
            );
            EmitTwoDeferredPairResult(
                sum,
                finish,
                frame,
                2,
                default,
                default,
                firstPairSlot: 1
            );
            Mark(Label());
            var previous = location;
            owner.EmitArithmeticLine(unpack.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private void EmitCheckedOptionalPairFields(
            Expr.Match unpack,
            SequenceEncodePart first,
            SequenceEncodePart second,
            DeferredOptional? firstOptional,
            DeferredOptional? secondOptional,
            Expr.Binary sum,
            int finish
        )
        {
            const int Frame = 3;
            int afterFields = owner.nextLabel + (firstOptional is null ? 0 : 2) + (secondOptional is null ? 0 : 2);
            int badSecond = second.Call.Module is null ? 0 : afterFields++;
            int badFirst = first.Call.Module is null ? 0 : afterFields++;
            int badInput = afterFields;
            owner.EmitTupleCheck(0, 3, badInput);
            EmitGeneralFrame(2, initializedByMoves: 2, slots: Frame);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                2
            );
            owner.EmitTuplePart(0, 1, 1);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(YRegisterTag, 1);
            if (firstOptional is null)
            {
                EmitFirstProjectedPairArguments(first.Call);
                owner.EmitCallLine(first.Call, location);
                Call(first.Call.Module, first.Call.Function, first.Call.Arguments.Count);
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    0
                );
            }
            else
            {
                EmitDeferredOptional(
                    firstOptional,
                    0,
                    null,
                    inputRegister: 1
                );
            }

            int firstCheckTag = firstOptional is null ? XRegisterTag : YRegisterTag;
            if (badFirst != 0)
            {
                EmitTupleShapeTest(
                    firstCheckTag,
                    0,
                    2,
                    badFirst
                );
            }

            int trim = 0;
            if (secondOptional is not null)
            {
                EmitDeferredOptional(
                    secondOptional,
                    -1,
                    1,
                    inputSlot: 1
                );
            }
            else
            {
                owner.EmitConstant(second.Constants[0], 1);
                Load(1, 0);
                owner.MoveRegister(
                    YRegisterTag,
                    0,
                    YRegisterTag,
                    1
                );
                owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, Frame - 1);
                owner.EmitCallLine(second.Call, location);
                Call(second.Call.Module, second.Call.Function, 2);
                trim = 1;
            }
            if (badSecond != 0)
            {
                EmitTupleShapeTest(
                    XRegisterTag,
                    0,
                    2,
                    badSecond
                );
            }

            EmitTwoDeferredPairResult(
                sum,
                finish,
                Frame - trim,
                2 - trim,
                default,
                default
            );
            if (badSecond != 0)
            {
                Mark(Label());
                EmitPairFailure(second.MatchLine, XRegisterTag);
            }

            if (badFirst != 0)
            {
                Mark(Label());
                EmitPairFailure(first.MatchLine, firstCheckTag);
            }

            Mark(Label());
            EmitPairFailure(unpack.SourceLine, XRegisterTag);

            void EmitPairFailure(int line, int tag)
            {
                var previous = location;
                owner.EmitArithmeticLine(line, ref previous);
                owner.Op(BadMatch, tag, 0);
            }
        }
    }
}
