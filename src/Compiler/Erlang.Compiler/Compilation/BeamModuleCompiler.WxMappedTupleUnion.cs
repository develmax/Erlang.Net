using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitWxMappedTupleUnionForward(
            GuardedLocalForward forward,
            FunctionDefinition continuation,
            int failure
        )
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || !forward.AddsEmptyList || forward.Arity != 4 || forward.Shapes is not [(3, 2)]
                || FlattenForwardConjunction(forward.Tests).ToArray() is not
                [
                    ForwardGuard.Test { Opcode: IsTaggedTuple, Register: 0, Arity: 4, Tag.Name: "wx_ref" } receiver,
                    ForwardGuard.Test { Opcode: IsInteger, Register: 1 } width,
                    ForwardGuard.Test { Opcode: IsInteger, Register: 2 } height,
                    ForwardGuard.FieldTest { Opcode: IsInteger, Register: 3, Index: 0 } row,
                    ForwardGuard.FieldTest { Opcode: IsInteger, Register: 3, Index: 1 } column
                ]
                || continuation.Clauses is not [{ Guard: { } guard } command]
                || ReadWxIndexExtras(command, includeMappedTuple: true) is not
                { Tuple: true, Mapped: true, Guards: var names }
                || ReadForwardGuard(guard, names) is not { } guards
                || !FlattenForwardConjunction(guards).SequenceEqual(
                    [new ForwardGuard.Test(IsInteger, 0), new ForwardGuard.Test(IsInteger, 1),
                        new ForwardGuard.Test(IsList, 2)])
                || !TryEmitWxClassUnionCommand(continuation, validateOnly: true, checkedMappedTuple: true))
            {
                return false;
            }

            int fallback = Label();
            EmitForwardGuard(receiver, failure);
            EmitTupleShapeTest(
                XRegisterTag,
                3,
                2,
                fallback
            );
            EmitForwardGuard(width, fallback);
            EmitForwardGuard(height, fallback);
            EmitForwardGuard(row, fallback);
            EmitForwardGuard(column, fallback);
            owner.EmitConstant(Nil.Value, forward.Arity);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, forward.Arity + 1);
            owner.Operand(LabelTag, forward.Entry);

            Mark(fallback);
            EmitDetailRecordTest(
                1,
                receiver.Arity,
                receiver.Tag!,
                failure
            );
            EmitTupleShapeTest(
                XRegisterTag,
                2,
                2,
                failure
            );
            EmitForwardGuard(row with { Register = 2 }, failure);
            EmitForwardGuard(column with { Register = 2 }, failure);
            EmitForwardGuard(new ForwardGuard.Test(IsList, 3), failure);

            return TryEmitWxClassUnionCommand(
                continuation,
                functionFailure: failure,
                checkedMappedTuple: true
            );
        }
    }
}
