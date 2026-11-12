using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static bool IsNumericEquality(Expr.Binary binary) =>
        binary.Operator is ErlangOperators.NumericEqual or ErlangOperators.NumericNotEqual;

    private static bool HasAtomOperand(Expr.Binary binary) =>
        binary.Left is Expr.Literal { Value: Atom } || binary.Right is Expr.Literal { Value: Atom };

    private static string NumericComparisonBif(Expr.Binary binary) => HasAtomOperand(binary)
        ? binary.Operator == ErlangOperators.NumericEqual ? ErlangOperators.ExactEqual : ErlangOperators.ExactNotEqual
        : binary.Operator;

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNumericComparison(FunctionDefinition function)
        {
            if (function.Clauses is [{ Guard: null, Body: Expr.Binary comparison } clause]
                && HasUnconditionalParameters(clause) && IsNumericEquality(comparison)
                && IsCallOperand(comparison.Left) && IsCallOperand(comparison.Right))
            {
                owner.Op(BifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, NumericComparisonBif(comparison), 2));
                EmitDirectOperand(clause, comparison.Left);
                EmitDirectOperand(clause, comparison.Right);
                owner.Operand(XRegisterTag, 0);
                owner.instructions.WriteByte(Return);
                return true;
            }

            if (function.Clauses is not [
                { Guard: Expr.Binary condition, Body: Expr.Literal success } accepted,
                { Guard: null, Body: Expr.Literal fallback } rejected
            ] || !HasUnconditionalParameters(accepted) || !HasUnconditionalParameters(rejected)
                || !IsNumericEquality(condition) || !IsCallOperand(condition.Left) || !IsCallOperand(condition.Right))
            {
                return false;
            }

            bool invert = condition.Operator == ErlangOperators.NumericNotEqual;
            int alternate = Label();
            owner.Op(HasAtomOperand(condition) ? ExactEqualTest : NumericEqualTest, LabelTag, alternate);
            EmitDirectOperand(accepted, condition.Left);
            EmitDirectOperand(accepted, condition.Right);
            owner.EmitConstant(invert ? fallback.Value : success.Value, 0);
            owner.instructions.WriteByte(Return);
            Mark(alternate);
            owner.EmitConstant(invert ? success.Value : fallback.Value, 0);
            owner.instructions.WriteByte(Return);
            return true;
        }

        private void EmitDirectOperand(Clause clause, Expr expression)
        {
            if (expression is Expr.Variable variable)
            {
                owner.Operand(XRegisterTag, ArgumentRegister(clause, variable.Name));
            }
            else
            {
                AttributeLiteral.TryNormalizeConstant(expression, out Term value);
                owner.LiteralOperand(value);
            }
        }

        private void EmitNumericComparison(Expr.Binary binary, int? failure) =>
            EmitComparisonBif(binary, NumericComparisonBif(binary), failure);

        private void EmitComparisonBif(Expr.Binary binary, string operation, int? failure)
        {
            int previousSlot = nextSlot;
            int? left = GuardSlot(binary.Left, failure);
            int? right = GuardSlot(binary.Right, failure);
            owner.Op(BifTwo, LabelTag, failure ?? 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, operation, 2));
            GuardOperand(binary.Left, left);
            GuardOperand(binary.Right, right);
            owner.Operand(XRegisterTag, 0);
            nextSlot = Math.Max(previousSlot, variables.Values.Select(DecoderSlot).DefaultIfEmpty(-1).Max() + 1);
        }
    }
}
