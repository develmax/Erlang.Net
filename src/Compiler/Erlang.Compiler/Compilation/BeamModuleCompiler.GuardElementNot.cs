using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static bool ContainsAdditionalOperation(Expr expression, bool includeElement) =>
        expression is Expr.Unary { Operator: ErlangOperators.Not or ErlangOperators.BitwiseNot }
            or Expr.Binary { Operator: ErlangOperators.Multiply or ErlangOperators.IntegerDivide or ErlangOperators.Divide
                or ErlangOperators.SubtractList or ErlangOperators.ShiftLeft or ErlangOperators.Remainder
                or ErlangOperators.BitwiseXor or ErlangOperators.Xor }
        || includeElement && expression is Expr.Call
        {
            Function: AutoImportNames.Element, Module: null or ErlangModuleNames.Module, Arguments.Count: 2, DynamicRemote: false
        }
        || expression is not Expr.Fun { SelfName: not null }
            && LocalFunctionEntries.Children(expression).Any(child => ContainsAdditionalOperation(child, includeElement));

    private sealed partial class GeneralEmitter
    {
        private bool IsElementGuardCall(Expr.Call call) =>
            call is { Function: AutoImportNames.Element, Arguments.Count: 2, DynamicRemote: false }
            && (call.Module == ErlangModuleNames.Module || call.Module is null && !entries.ContainsKey((AutoImportNames.Element, 2)));

        private void EmitElementGuard(Expr.Call call, int failure)
        {
            Expr index = call.Arguments[0];
            Expr tuple = call.Arguments[1];
            int? indexSlot = GuardSlot(index, failure);
            int? tupleSlot = GuardSlot(tuple, failure);
            EmitElementInstruction(failure);
            GuardOperand(index, indexSlot);
            GuardOperand(tuple, tupleSlot);
            owner.Operand(XRegisterTag, 0);
        }

        private void EmitElementInstruction(int failure)
        {
            owner.Op(BifTwo, LabelTag, failure);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Element, 2));
        }

        private void EmitBooleanNot(Expr.Unary expression, int? failure)
        {
            int? slot = GuardSlot(expression.Operand, failure);
            if (failure is null)
            {
                var previous = location;
                owner.EmitArithmeticLine(expression.SourceLine, ref previous);
            }
            EmitBooleanNotInstruction(failure ?? 0);
            GuardOperand(expression.Operand, slot);
            owner.Operand(XRegisterTag, 0);
        }

        private void EmitBooleanNotInstruction(int failure)
        {
            owner.Op(BifOne, LabelTag, failure);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Not, 1));
        }

        private bool TryEmitDirectBooleanNot(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Body: Expr.Unary { Operator: ErlangOperators.Not, Operand: Expr.Variable input } expression } clause
            ] || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            var previous = location;
            owner.EmitArithmeticLine(expression.SourceLine, ref previous);
            EmitBooleanNotInstruction(0);
            EmitDirectOperand(clause, input);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private bool TryEmitDirectValueGuard(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: { } guard, Body: Expr.Literal accepted } first,
                { Guard: null, Body: Expr.Literal rejected } fallback
            ] || !HasUnconditionalParameters(first) || !HasUnconditionalParameters(fallback))
            {
                return false;
            }

            if (guard is Expr.Unary { Operator: ErlangOperators.Not, Operand: Expr.Variable input })
            {
                int failure = Label();
                owner.EmitAtomTest(ArgumentRegister(first, input.Name), "false", failure);
                EmitConstantGuardBranches(accepted, rejected, failure);

                return true;
            }

            if (first.Patterns.Count == 1 && guard is Expr.Binary
                {
                    Operator: ErlangOperators.ExactEqual,
                    Left: Expr.Call { Function: AutoImportNames.Node } currentNode,
                    Right: Expr.Variable node
                } && IsZeroGuardBif(currentNode))
            {
                int failure = Label();
                EmitZeroGuardBif(AutoImportNames.Node, 1);
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.EmitTypedRegister(1, AtomTypeBits);
                EmitDirectOperand(first, node);
                EmitConstantGuardBranches(accepted, rejected, failure);

                return true;
            }

            if (guard is Expr.Binary
                {
                    Operator: ErlangOperators.ExactEqual,
                    Left: Expr.Call extract,
                    Right: Expr.Variable expectedValue
                } && (IsGuardBif(extract, AutoImportNames.Tl) || IsBinaryPartGuard(extract))
                && extract.Arguments.All(IsCallOperand)
                && extract.Arguments[0] is Expr.Variable inputValue
                && ArgumentRegister(first, inputValue.Name) == 0
                && ArgumentRegister(first, expectedValue.Name) > 0)
            {
                int failure = Label();
                bool binaryPart = extract.Arguments.Count == 3;
                if (binaryPart)
                {
                    owner.Op(GcBifThree, LabelTag, failure);
                    owner.Operand(BeamWire.UnsignedTag, function.Arity);
                    owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, extract.Function, 3));
                }
                else
                {
                    EmitUnaryGuardInstruction(extract.Function, failure);
                }

                foreach (Expr argument in extract.Arguments)
                {
                    EmitDirectOperand(first, argument);
                }

                owner.Operand(XRegisterTag, 0);
                owner.Op(ExactEqualTest, LabelTag, failure);
                if (binaryPart)
                {
                    owner.EmitTypedRegister(0, MatchableTypeBits, unit: BitSyntaxDefaults.BinaryUnit);
                }
                else
                {
                    owner.Operand(XRegisterTag, 0);
                }

                EmitDirectOperand(first, expectedValue);
                EmitConstantGuardBranches(accepted, rejected, failure);
                return true;
            }

            var (call, expected) = guard switch
            {
                Expr.Binary { Operator: ErlangOperators.ExactEqual, Left: Expr.Call compared,
                    Right: Expr.Literal { Value: Atom atom } } => (compared, atom.Name),
                Expr.Call head when IsGuardBif(head, AutoImportNames.Hd) => (head, "true"),
                _ => (null, null)
            };
            if (call is null || expected is null || !call.Arguments.All(IsCallOperand))
            {
                return false;
            }

            bool element = IsElementGuardCall(call);
            bool nodeResult = IsGuardBif(call, AutoImportNames.Node);
            if (!element && !nodeResult && !IsGuardBif(call, AutoImportNames.Hd))
            {
                return false;
            }

            int failureTarget = Label();
            if (element)
            {
                EmitElementInstruction(failureTarget);
            }
            else
            {
                EmitUnaryGuardInstruction(call.Function, failureTarget);
            }
            foreach (Expr argument in call.Arguments)
            {
                EmitDirectOperand(first, argument);
            }
            owner.Operand(XRegisterTag, 0);
            if (nodeResult)
            {
                owner.Op(ExactEqualTest, LabelTag, failureTarget);
                owner.EmitTypedRegister(0, AtomTypeBits);
                owner.LiteralOperand(Term.A(expected));
            }
            else
            {
                owner.EmitAtomTest(0, expected, failureTarget);
            }
            EmitConstantGuardBranches(accepted, rejected, failureTarget);

            return true;
        }

        private void EmitConstantGuardBranches(Expr.Literal accepted, Expr.Literal rejected, int failure)
        {
            owner.EmitConstant(accepted.Value, 0);
            owner.instructions.WriteByte(Return);
            Mark(failure);
            owner.EmitConstant(rejected.Value, 0);
            owner.instructions.WriteByte(Return);
        }
    }
}
