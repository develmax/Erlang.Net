using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitWxTupleArityForward(
            GuardedLocalForward forward,
            FunctionDefinition continuation,
            int failure
        )
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || !forward.AddsEmptyList || forward.Arity != 3
                || forward.Shapes is not [(1, 2), (2, 2)]
                || continuation.Clauses is not [{ Patterns: [var receiver, var alternate, _] }]
                || ReadWxClassUnionObject(receiver) is null || ReadWxClassUnionObject(alternate) is null
                || FlattenForwardConjunction(forward.Tests).ToArray() is not
                [
                    ForwardGuard.Test { Opcode: IsTaggedTuple, Register: 0, Arity: 4, Tag.Name: "wx_ref" } receiverTest,
                    .. var dimensionTests
                ]
                || !dimensionTests.SequenceEqual(
                    forward.Shapes.SelectMany(shape => Enumerable.Range(0, shape.Arity).Select(field =>
                        new ForwardGuard.FieldTest(
                            IsInteger,
                            shape.Register,
                            field,
                            forward.Arity
                        ))))
                || !TryEmitWxObjectCommand(continuation, validateOnly: true, checkedRecordPrefix: 2))
            {
                return false;
            }

            // The two widths are disjoint. After a single receiver check, the
            // tagged record arm and the pair-forwarding arm cannot overlap.
            int record = Label();
            int pair = Label();
            EmitForwardGuard(receiverTest, failure);
            owner.EmitTupleArityDispatch(failure, [(2, pair), (receiverTest.Arity, record)], 1);
            Mark(record);
            owner.EmitTuplePart(1, 0, forward.Arity);
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, forward.Arity);
            owner.LiteralOperand(receiverTest.Tag!);
            TryEmitWxObjectCommand(
                continuation,
                functionFailure: failure,
                checkedRecordPrefix: 2
            );

            Mark(pair);
            EmitTupleShapeTest(
                XRegisterTag,
                2,
                2,
                failure
            );
            foreach (var test in dimensionTests)
            {
                EmitForwardGuard(test, failure);
            }

            owner.EmitConstant(Nil.Value, forward.Arity);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, forward.Arity + 1);
            owner.Operand(LabelTag, forward.Entry);

            return true;
        }
    }
}
