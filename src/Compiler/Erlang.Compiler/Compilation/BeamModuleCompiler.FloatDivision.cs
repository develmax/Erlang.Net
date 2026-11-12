using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private void EmitDirectFloatDivision(Clause clause, Expr.Binary division)
        {
            EmitDirectFloatOperand(clause, division.Left, 0);
            EmitDirectFloatOperand(clause, division.Right, 1);
            EmitFloatDivisionResult(live: 0, destination: 0);
        }

        private void EmitDirectFloatOperand(Clause clause, Expr operand, int register)
        {
            owner.instructions.WriteByte(FloatConvert);
            EmitDirectOperand(clause, operand);
            EmitFloatRegister(register);
        }

        private void EmitFloatDivision(Expr.Binary division)
        {
            int operandBase = nextSlot;
            int? left = GuardSlot(division.Left, null);
            int? right = GuardSlot(division.Right, null);
            var previous = location;
            owner.EmitArithmeticLine(division.SourceLine, ref previous);

            owner.instructions.WriteByte(FloatConvert);
            GuardOperand(division.Left, left);
            EmitFloatRegister(0);
            owner.instructions.WriteByte(FloatConvert);
            GuardOperand(division.Right, right);
            EmitFloatRegister(1);
            EmitFloatDivisionResult(live: 0, destination: 0);
            ReleaseOperandSlots(operandBase);
        }
    }
}
