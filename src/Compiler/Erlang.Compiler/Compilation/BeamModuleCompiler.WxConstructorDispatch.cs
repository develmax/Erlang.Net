using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private static bool IsWxChardataTest(ForwardGuard guard) =>
            guard is ForwardGuard.Disjunction
            {
                Left: ForwardGuard.Test { Opcode: IsList, Register: 0 },
                Right: ForwardGuard.Test { Opcode: IsBinary, Register: 0 }
            };

        private bool TryEmitWxTupleConstructorForward(
            FunctionDefinition function,
            GuardedLocalForward first,
            int failure
        )
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || first.Shapes.Count != 0
                || !first.AddsEmptyList)
            {
                return false;
            }

            if (first.Arity == 1 && IsWxChardataTest(first.Tests) && function.Clauses.Count is 2 or 3
                && ReadGuardedLocalForward(function.Clauses[1]) is { AddsEmptyList: true } dimensions
                && dimensions.Entry == first.Entry && dimensions.Shapes is [(0, 2)]
                && FlattenForwardConjunction(dimensions.Tests).SequenceEqual(
                    new ForwardGuard[] { new ForwardGuard.FieldTest(
                        IsInteger,
                        0,
                        0,
                        1
                    ), new ForwardGuard.FieldTest(
                        IsInteger,
                        0,
                        1,
                        1
                    ) }))
            {
                return EmitWxUnaryTupleConstructor(
                    function,
                    first,
                    dimensions,
                    failure
                );
            }

            if (function.Clauses is [_, var alternate]
                && TryEmitWxScalarTupleForward(first, alternate, failure))
            {
                return true;
            }

            if (first.Arity != 2 || function.Clauses is not [_, var text, var tuple, var record]
                || !FlattenForwardConjunction(first.Tests).SequenceEqual(
                    new ForwardGuard[] { new ForwardGuard.Test(IsInteger, 0), new ForwardGuard.Test(IsInteger, 1) })
                || ReadWxClassUnionObject(record.Patterns[0]) is null)
            {
                return false;
            }

            var textArm = function with { Clauses = [text] };
            var tupleArm = function with { Clauses = [tuple] };
            var recordArm = function with { Clauses = [record] };
            if (!TryEmitWxObjectCommand(textArm, validateOnly: true, tupleConstructorArm: true)
                || !TryEmitWxStaticScalarOptions(
                    tupleArm,
                    failure,
                    validateOnly: true,
                    dimensionTuple: true
                )
                || !TryEmitWxObjectCommand(
                    recordArm,
                    validateOnly: true,
                    checkedRecordPrefix: 1,
                    tupleConstructorArm: true
                ))
            {
                return false;
            }

            int next = Label();
            EmitGuardedLocalForward(first, next);
            Mark(next);
            // The text guard has two edges followed by the frame's missing environment edge.
            int dispatch = owner.nextLabel + 3;
            TryEmitWxObjectCommand(
                textArm,
                guardFailure: dispatch,
                functionFailure: failure,
                tupleConstructorArm: true
            );
            Mark(Label());
            // The record arm has one class check and one missing environment edge.
            EmitWxConstructorTupleSwitch(failure, armsBeforeDimensions: 2);
            TryEmitWxObjectCommand(
                recordArm,
                functionFailure: failure,
                checkedRecordPrefix: 1,
                tupleConstructorArm: true
            );
            Mark(Label());
            TryEmitWxStaticScalarOptions(
                tupleArm,
                failure,
                validateOnly: false,
                dimensionTuple: true
            );

            return true;
        }

        private bool TryEmitWxScalarTupleForward(GuardedLocalForward scalar, Clause alternate, int failure)
        {
            if (scalar.Arity < 2 || ReadGuardedLocalForward(alternate) is not { AddsEmptyList: true } tuple
                || tuple.Entry != scalar.Entry || tuple.Shapes is not [(0, 2)])
            {
                return false;
            }

            var shared = Enumerable.Range(1, scalar.Arity - 1)
                .Select(register => new ForwardGuard.Test(IsInteger, register)).ToArray();
            ForwardGuard[] scalarTests = [new ForwardGuard.Test(IsInteger, 0), .. shared];
            ForwardGuard[] tupleTests =
            [
                new ForwardGuard.FieldTest(
                    IsInteger,
                    0,
                    0,
                    scalar.Arity
                ),
                new ForwardGuard.FieldTest(
                    IsInteger,
                    0,
                    1,
                    scalar.Arity
                ),
                .. shared
            ];
            if (!FlattenForwardConjunction(scalar.Tests).SequenceEqual(scalarTests)
                || !FlattenForwardConjunction(tuple.Tests).SequenceEqual(tupleTests))
            {
                return false;
            }

            int alternateEntry = Label();
            EmitForwardGuard(scalarTests[0], alternateEntry);
            foreach (var test in shared)
            {
                EmitForwardGuard(test, failure);
            }

            owner.EmitConstant(Nil.Value, scalar.Arity);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, scalar.Arity + 1);
            owner.Operand(LabelTag, scalar.Entry);
            Mark(alternateEntry);
            EmitGuardedLocalForward(tuple, failure);

            return true;
        }

        private bool EmitWxUnaryTupleConstructor(
            FunctionDefinition function,
            GuardedLocalForward text,
            GuardedLocalForward dimensions,
            int failure
        )
        {
            FunctionDefinition? union = null;
            if (function.Clauses.Count == 3)
            {
                union = function with { Clauses = [function.Clauses[2]] };
                if (!TryEmitWxClassUnionCommand(union, validateOnly: true, unaryTagValidated: true))
                {
                    return false;
                }
            }

            int next = owner.nextLabel + ForwardGuardLabelCount(text.Tests);
            EmitGuardedLocalForward(text, next);
            Mark(Label());
            if (union is not null)
            {
                // Two protected checks use four labels each. Class selection and error paths use four more.
                EmitWxConstructorTupleSwitch(failure, armsBeforeDimensions: 12, scratch: 1);
                TryEmitWxClassUnionCommand(union, functionFailure: failure, unaryTagValidated: true);
                Mark(Label());
                dimensions = dimensions with { Shapes = [] };
            }

            EmitGuardedLocalForward(dimensions, failure);

            return true;
        }

        private void EmitWxConstructorTupleSwitch(int failure, int armsBeforeDimensions, int scratch = 2)
        {
            owner.Op(IsTuple, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            int record = Label();
            int dimensions = owner.nextLabel + armsBeforeDimensions;
            owner.instructions.WriteByte(SelectTupleArity);
            owner.EmitTypedRegister(0, TupleTypeBits);
            owner.Operand(LabelTag, failure);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, dimensions);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.Operand(LabelTag, record);
            Mark(record);
            owner.EmitTuplePart(0, 0, scratch);
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, scratch);
            owner.LiteralOperand(Term.A("wx_ref"));
        }
    }
}
