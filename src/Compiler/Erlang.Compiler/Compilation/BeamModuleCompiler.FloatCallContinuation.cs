using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitFloatCallContinuation(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable index, Pattern.Variable target, Pattern.Variable saved],
                        Body: Expr.Sequence
                        {
                            Items: [
                                Expr.Call
                            {
                                Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Variable adjusted, var first, var second]
                            } prepare,
                                Expr.Call
                            {
                                Module: not null, DynamicRemote: false,
                                Arguments: [
                                        Expr.Variable translated,
                                        var fixedOffset,
                                        Expr.Binary { Operator: ErlangOperators.Multiply } multiplication
                                    ]
                            } applyOffset,
                                Expr.Call
                            {
                                Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Variable finalized, Expr.Variable payload]
                            } finish
                            ]
                        }
                    }
                ] || !DistinctBindings(index, target, saved)
                || adjusted.Name != target.Name || translated.Name != target.Name || finalized.Name != target.Name
                || payload.Name != saved.Name || multiplication.Right is not Expr.Variable source || source.Name != index.Name
                || !AttributeLiteral.TryNormalizeConstant(multiplication.Left, out Term scale) || scale is not FloatTerm
                || !AttributeLiteral.TryNormalizeConstant(first, out Term initial) || initial is not FloatTerm
                || !AttributeLiteral.TryNormalizeConstant(second, out Term ending) || ending is not FloatTerm
                || !AttributeLiteral.TryNormalizeConstant(fixedOffset, out Term offset) || offset is not FloatTerm)
            {
                return false;
            }

            EmitFrameAllocation(3, 3);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                2
            );
            owner.EmitConstant(ending, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitConstant(initial, 1);
            owner.EmitCallLine(prepare, location);
            owner.EmitStaticRemoteCall(prepare);
            var previous = CallLocation(prepare, location);
            owner.EmitArithmeticLine(multiplication.SourceLine, ref previous);
            owner.Op(FloatConvert, YRegisterTag, 2);
            EmitFloatRegister(0);
            owner.instructions.WriteByte(FloatMove);
            owner.LiteralOperand(scale);
            EmitFloatRegister(1);
            EmitFloatArithmeticResult(FloatMultiply, live: 0, destination: 2);
            owner.EmitConstant(offset, 1);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                2
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(applyOffset, previous);
            owner.EmitStaticRemoteCall(applyOffset);
            owner.MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(finish, CallLocation(applyOffset, previous));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(finish.Module!, finish.Function, 2));
            owner.Operand(BeamWire.UnsignedTag, 2);

            return true;
        }
    }
}
