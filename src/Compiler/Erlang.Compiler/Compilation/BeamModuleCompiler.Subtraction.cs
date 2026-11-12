using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private (EntryCall Call, Expr Other)? ReadEntryCaseArithmetic(Expr.Binary binary, string[] parameters)
        {
            if (!IsRegisterArithmeticOperator(binary.Operator) || binary.Operator == ErlangOperators.Divide)
            {
                return null;
            }

            return ReadEntryBinaryCall(binary, parameters) is { } input
                && ReadEntryCall(input.Call, parameters, allowConstructedArgument: true) is { } entry
                ? (entry, input.Other) : null;
        }

        private void EmitEntryCaseArithmetic(Expr.Binary binary, Expr.Call call)
        {
            Expr other = ReferenceEquals(call, binary.Left) ? binary.Right : binary.Left;
            int? slot = other is Expr.Variable variable ? variables[variable.Name] : null;
            EmitEntryBinaryArithmetic(binary, call, slot);
        }

        private static (Expr.Call Call, Expr Other)? ReadEntryBinaryCall(Expr.Binary binary, string[] parameters)
        {
            Expr.Call? call = binary.Left as Expr.Call ?? binary.Right as Expr.Call;
            Expr other = ReferenceEquals(call, binary.Left) ? binary.Right : binary.Left;
            bool simple = other is Expr.Variable variable
                ? variable.Name != VariableScopeNames.Wildcard && parameters.Contains(variable.Name)
                : AttributeLiteral.TryNormalizeConstant(other, out _);

            return simple && call is not null ? (call, other) : null;
        }

        private bool TryEmitEntryBinaryReturn(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause) || clause.Body is not Expr.Binary binary)
            {
                return false;
            }

            bool listOperation = binary.Operator is ErlangOperators.Append or ErlangOperators.SubtractList;
            if (!listOperation && !IsRegisterArithmeticOperator(binary.Operator)
                || binary.Operator == ErlangOperators.Append && AppendPrefix(binary.Left) is not null)
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (ReadEntryBinaryCall(binary, parameters) is not { } input)
            {
                return false;
            }

            int[] saved = input.Other is Expr.Variable variable ? [Array.IndexOf(parameters, variable.Name)] : [];
            if (ReadEntrySelector(input.Call, parameters, saved) is not { } selector)
            {
                return false;
            }

            int trimmed = EmitEntrySelector(selector, input.Call, parameters);
            int frame = selector.FrameSize - trimmed;
            int? slot = saved.Length == 0 ? null : Array.IndexOf(selector.Saved, saved[0]) - trimmed;
            if (listOperation)
            {
                EmitEntryListOperator(
                    binary,
                    input.Call,
                    input.Other,
                    slot,
                    frame
                );
            }
            else
            {
                EmitEntryBinaryArithmetic(binary, input.Call, slot);
                owner.EmitFrameReturn(frame);
            }

            return true;
        }

        private void EmitEntryListOperator(
            Expr.Binary binary,
            Expr.Call call,
            Expr other,
            int? slot,
            int frame
        )
        {
            int result = ReferenceEquals(binary.Left, call) ? 0 : 1;
            if (result == 1)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    XRegisterTag,
                    1
                );
            }

            if (slot is int saved)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    saved,
                    XRegisterTag,
                    1 - result
                );
            }
            else
            {
                owner.EmitConstant(other, 1 - result);
            }

            var previous = CallLocation(call, location);
            owner.EmitArithmeticLine(binary.SourceLine, ref previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, binary.Operator, 2));
            owner.Operand(BeamWire.UnsignedTag, frame);
        }

        private void EmitEntryBinaryArithmetic(Expr.Binary binary, Expr.Call call, int? otherSlot)
        {
            var previous = CallLocation(call, location);
            owner.EmitArithmeticLine(binary.SourceLine, ref previous);
            if (binary.Operator == ErlangOperators.Divide)
            {
                owner.instructions.WriteByte(FloatConvert);
                EmitOperand(binary.Left);
                EmitFloatRegister(0);
                owner.instructions.WriteByte(FloatConvert);
                EmitOperand(binary.Right);
                EmitFloatRegister(1);
                EmitFloatDivisionResult(live: 0, destination: 0);

                return;
            }

            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, binary.Operator, 2));
            EmitOperand(binary.Left);
            EmitOperand(binary.Right);
            owner.Operand(XRegisterTag, 0);

            void EmitOperand(Expr operand)
            {
                if (ReferenceEquals(operand, call))
                {
                    owner.Operand(XRegisterTag, 0);
                }
                else
                {
                    GuardOperand(operand, otherSlot);
                }
            }
        }

        private bool TryEmitEntryArithmeticCase(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Patterns.Count >= FirstReservedXRegister
                || clause.Body is not Expr.Case
                {
                    Value: Expr.Binary selector,
                    Clauses:
                    [
                        { Patterns: [Pattern.Literal { Value: Atom or Integer } match], Guard: null, Body: var success },
                        { Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Guard: null, Body: var fallback }
                    ]
                }
                || !IsDirect(selector.Left) || !IsDirect(selector.Right) || !CanEmit(selector)
                || !CanEmit(success) || !CanEmit(fallback))
            {
                return false;
            }

            int scratch = clause.Patterns.Count;
            var integerInputs = new HashSet<int>();
            int unequal = Label();
            var previous = location;
            Emit(selector, scratch, scratch, ref previous);
            if (IsIntegerOperation(selector))
            {
                foreach (Expr.Variable variable in new[] { selector.Left, selector.Right }.OfType<Expr.Variable>())
                {
                    integerInputs.Add(Register(variable));
                }
            }

            owner.Op(ExactEqualTest, LabelTag, unequal);
            Operand(null, scratch, IsIntegerOperation(selector));
            owner.LiteralOperand(match.Value);
            EmitBranch(success, previous);
            Mark(unequal);
            EmitBranch(fallback, previous);

            return true;

            bool IsDirect(Expr expression) => expression is Expr.Variable variable
                ? Register(variable) >= 0
                : AttributeLiteral.TryNormalizeConstant(expression, out _);

            bool CanEmit(Expr expression) => IsDirect(expression)
                || expression is Expr.Binary
                {
                    Operator: ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply
                        or ErlangOperators.IntegerDivide or ErlangOperators.Remainder
                } binary && (IsDirect(binary.Left) && CanEmit(binary.Right) || IsDirect(binary.Right) && CanEmit(binary.Left));

            int Register(Expr expression)
            {
                if (expression is Expr.Variable { Name: not VariableScopeNames.Wildcard } variable)
                {
                    for (int index = 0; index < clause.Patterns.Count; index++)
                    {
                        if (clause.Patterns[index] is Pattern.Variable parameter && parameter.Name == variable.Name)
                        {
                            return index;
                        }
                    }
                }

                return -1;
            }

            void Operand(Expr? expression, int register, bool integer)
            {
                if (register >= 0)
                {
                    if (integer)
                    {
                        owner.EmitTypedRegister(register, IntegerTypeBits);
                    }
                    else
                    {
                        owner.Operand(XRegisterTag, register);
                    }
                }
                else
                {
                    EmitDirectOperand(clause, expression!);
                }
            }

            void Emit(Expr.Binary binary, int destination, int minimumLive, ref BeamSourceLocation line)
            {
                int left = Register(binary.Left);
                int right = Register(binary.Right);
                bool leftInteger = integerInputs.Contains(left);
                bool rightInteger = integerInputs.Contains(right);
                if (!IsDirect(binary.Left) && binary.Left is Expr.Binary leftExpression)
                {
                    left = right == destination ? scratch : destination;
                    Emit(leftExpression, left, Math.Max(minimumLive, right + 1), ref line);
                    leftInteger = IsIntegerOperation(leftExpression);
                }
                else if (!IsDirect(binary.Right) && binary.Right is Expr.Binary rightExpression)
                {
                    right = left == destination ? scratch : destination;
                    Emit(rightExpression, right, Math.Max(minimumLive, left + 1), ref line);
                    rightInteger = IsIntegerOperation(rightExpression);
                }

                owner.EmitArithmeticLine(binary.SourceLine, ref line);
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, Math.Max(minimumLive, Math.Max(left, right) + 1));
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, binary.Operator, 2));
                Operand(binary.Left, left, leftInteger);
                Operand(binary.Right, right, rightInteger);
                owner.Operand(XRegisterTag, destination);
            }

            void EmitBranch(Expr expression, BeamSourceLocation line)
            {
                if (AttributeLiteral.TryNormalizeConstant(expression, out Term value))
                {
                    owner.EmitConstant(value, 0);
                }
                else if (expression is Expr.Variable)
                {
                    owner.MoveRegister(XRegisterTag, Register(expression), XRegisterTag, 0);
                }
                else
                {
                    Emit((Expr.Binary)expression, 0, 0, ref line);
                }

                owner.instructions.WriteByte(Return);
            }

            static bool IsIntegerOperation(Expr.Binary binary) =>
                binary.Operator is ErlangOperators.IntegerDivide or ErlangOperators.Remainder;
        }

        private bool TryEmitComplementaryArithmeticClauses(Clause first, Clause second, bool last, int functionInfo)
        {
            if (ReadEntryArithmetic(first) is not { Tests: [var firstTest, var firstRemaining] } left
                || ReadEntryArithmetic(second) is not { Tests: [var secondTest, var secondRemaining] } right
                || !Complementary(first, firstTest, second, secondTest))
            {
                return false;
            }

            int other = Label();
            int failure = last ? functionInfo : Label();
            EmitEntryComparison(first, firstTest, other);
            EmitEntryComparison(first, firstRemaining, failure);
            EmitEntryArithmeticReturn(first, left.Arithmetic, left.Tests);
            Mark(other);
            EmitEntryComparison(second, secondRemaining, failure);
            EmitEntryArithmeticReturn(second, right.Arithmetic, right.Tests);
            if (!last)
            {
                Mark(failure);
            }

            return true;
        }

        private static bool Complementary(Clause first, Expr.Binary left, Clause second, Expr.Binary right)
        {
            byte leftOpcode = GuardComparisonOpcode(left);
            byte rightOpcode = GuardComparisonOpcode(right);
            if ((leftOpcode, rightOpcode) is not ((IsLess, IsGreaterOrEqual) or (IsGreaterOrEqual, IsLess)))
            {
                return false;
            }

            var (leftFirst, leftSecond) = EntryComparisonOperands(left);
            var (rightFirst, rightSecond) = EntryComparisonOperands(right);
            return SameOperand(leftFirst, rightFirst) && SameOperand(leftSecond, rightSecond);

            bool SameOperand(Expr a, Expr b) => a is Expr.Variable firstVariable && b is Expr.Variable secondVariable
                ? ArgumentRegister(first, firstVariable.Name) == ArgumentRegister(second, secondVariable.Name)
                : AttributeLiteral.TryNormalizeConstant(a, out Term x)
                    && AttributeLiteral.TryNormalizeConstant(b, out Term y) && x.Equals(y);
        }

        private bool TryEmitEntryArithmeticClause(Clause clause, bool last, int functionInfo)
        {
            if (ReadEntryArithmetic(clause) is not { } plan)
            {
                return false;
            }

            int failure = last ? functionInfo : Label();
            foreach (Expr.Binary comparison in plan.Tests)
            {
                EmitEntryComparison(clause, comparison, failure);
            }

            EmitEntryArithmeticReturn(clause, plan.Arithmetic);
            if (!last)
            {
                Mark(failure);
            }

            return true;
        }

        private static (Expr.Binary Arithmetic, Expr.Binary[] Tests)? ReadEntryArithmetic(Clause clause)
        {
            if (!HasUnconditionalParameters(clause) || clause.Guard is null
                || clause.Body is not Expr.Binary
                {
                    Operator: ErlangOperators.Minus or ErlangOperators.Plus or ErlangOperators.Multiply
                        or ErlangOperators.IntegerDivide or ErlangOperators.Remainder
                } arithmetic
                || !IsEntryOperand(arithmetic.Left) || !IsEntryOperand(arithmetic.Right)
                || arithmetic.Left is not Expr.Variable && arithmetic.Right is not Expr.Variable)
            {
                return null;
            }

            var comparisons = new List<Expr.Binary>();
            return ReadComparisons(clause.Guard) ? (arithmetic, comparisons.ToArray()) : null;

            bool IsEntryOperand(Expr expression) => expression is Expr.Variable variable
                ? variable.Name != VariableScopeNames.Wildcard
                    && clause.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == variable.Name)
                : AttributeLiteral.TryNormalizeConstant(expression, out _);

            bool ReadComparisons(Expr expression)
            {
                if (expression is Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction)
                {
                    return ReadComparisons(conjunction.Left) && ReadComparisons(conjunction.Right);
                }

                if (expression is not Expr.Binary comparison || GuardComparisonOpcode(comparison) == 0
                    || !IsEntryOperand(comparison.Left) || !IsEntryOperand(comparison.Right))
                {
                    return false;
                }

                comparisons.Add(comparison);
                return true;
            }
        }

        private void EmitEntryComparison(Clause clause, Expr.Binary comparison, int failure)
        {
            var (left, right) = EntryComparisonOperands(comparison);
            owner.Op(GuardComparisonOpcode(comparison), LabelTag, failure);
            EmitDirectOperand(clause, left);
            EmitDirectOperand(clause, right);
        }

        private static (Expr Left, Expr Right) EntryComparisonOperands(Expr.Binary comparison) =>
            comparison.Operator is ErlangOperators.Greater or ErlangOperators.LessOrEqual
                ? (comparison.Right, comparison.Left)
                : (comparison.Left, comparison.Right);

        private void EmitEntryArithmeticReturn(Clause clause, Expr.Binary arithmetic, Expr.Binary[]? facts = null)
        {
            var previous = location;
            owner.EmitArithmeticLine(arithmetic.SourceLine, ref previous);
            EmitDirectArithmetic(clause, [arithmetic.Left, arithmetic.Right], arithmetic.Operator, 0, facts);
            owner.instructions.WriteByte(Return);
        }

        private bool TryEmitDirectArithmetic(FunctionDefinition function)
        {
            if (function.Clauses is [{ Guard: null } clause] && HasUnconditionalParameters(clause))
            {
                Expr[]? operands = clause.Body switch
                {
                    Expr.Binary { Operator: ErlangOperators.Minus or ErlangOperators.Multiply or ErlangOperators.IntegerDivide
                        or ErlangOperators.Divide or ErlangOperators.Remainder or ErlangOperators.ShiftLeft
                        or ErlangOperators.BitwiseXor or ErlangOperators.Xor } binary => [binary.Left, binary.Right],
                    Expr.Unary { Operator: ErlangOperators.Minus or ErlangOperators.BitwiseNot, Operand: Expr.Variable variable } => [variable],
                    _ => null
                };
                if (operands is not null && operands.All(IsCallOperand) && operands.Any(operand => operand is Expr.Variable))
                {
                    int line = clause.Body is Expr.Binary binary ? binary.SourceLine : ((Expr.Unary)clause.Body).SourceLine;
                    var previous = location;
                    owner.EmitArithmeticLine(line, ref previous);
                    if (clause.Body is Expr.Binary { Operator: ErlangOperators.Divide } division)
                    {
                        EmitDirectFloatDivision(clause, division);
                    }
                    else
                    {
                        EmitDirectArithmetic(clause, operands, clause.Body is Expr.Binary operation ? operation.Operator : ((Expr.Unary)clause.Body).Operator, 0);
                    }
                    owner.instructions.WriteByte(Return);
                    return true;
                }
            }

            if (function.Clauses is not [
                { Guard: Expr.Binary { Operator: ErlangOperators.Greater,
                    Left: Expr.Binary { Operator: ErlangOperators.Minus or ErlangOperators.IntegerDivide or ErlangOperators.Divide } subtraction,
                    Right: Expr.Literal { Value: Integer or FloatTerm } bound },
                    Body: Expr.Literal success } accepted,
                { Guard: null, Body: Expr.Literal fallback } rejected
            ] || !HasUnconditionalParameters(accepted) || !HasUnconditionalParameters(rejected)
                || !IsCallOperand(subtraction.Left) || !IsCallOperand(subtraction.Right)
                || subtraction.Left is not Expr.Variable && subtraction.Right is not Expr.Variable)
            {
                return false;
            }

            int failure = Label();
            EmitDirectArithmetic(accepted, [subtraction.Left, subtraction.Right], subtraction.Operator, failure);
            if (subtraction.Operator == ErlangOperators.IntegerDivide && bound.Value is Integer integer)
            {
                owner.Op(IsGreaterOrEqual, LabelTag, failure);
                owner.EmitTypedRegister(0, IntegerTypeBits);
                owner.LiteralOperand(new Integer(integer.Value + 1));
            }
            else
            {
                owner.Op(IsLess, LabelTag, failure);
                owner.LiteralOperand(bound.Value);
                int type = subtraction.Operator switch
                {
                    ErlangOperators.IntegerDivide => IntegerTypeBits,
                    ErlangOperators.Divide => FloatTypeBits,
                    _ => NumberTypeBits
                };
                owner.EmitTypedRegister(0, type);
            }
            owner.EmitConstant(success.Value, 0);
            owner.instructions.WriteByte(Return);
            Mark(failure);
            owner.EmitConstant(fallback.Value, 0);
            owner.instructions.WriteByte(Return);
            return true;
        }

        private void EmitDirectArithmetic(
            Clause clause,
            IReadOnlyList<Expr> operands,
            string operation,
            int failure,
            Expr.Binary[]? facts = null
        )
        {
            if (operation == ErlangOperators.Xor)
            {
                owner.Op(BifTwo, LabelTag, failure);
            }
            else
            {
                int live = operands.OfType<Expr.Variable>()
                    .Select(variable => ArgumentRegister(clause, variable.Name) + 1).Max();
                owner.Op(operands.Count == 1 ? GcBifOne : GcBifTwo, LabelTag, failure);
                owner.Operand(BeamWire.UnsignedTag, live);
            }

            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, operation, operands.Count));
            foreach (Expr operand in operands)
            {
                if (facts is not null && operand is Expr.Variable variable
                    && operation is ErlangOperators.IntegerDivide or ErlangOperators.Remainder)
                {
                    bool belowZero = facts.Any(test =>
                    {
                        var (left, right) = EntryComparisonOperands(test);
                        return GuardComparisonOpcode(test) == IsLess
                            && left is Expr.Variable tested && tested.Name == variable.Name
                            && AttributeLiteral.TryNormalizeConstant(right, out Term bound)
                            && bound is Integer integer && integer.Value.IsZero;
                    });
                    int register = ArgumentRegister(clause, variable.Name);
                    owner.EmitTypedRegister(register, belowZero ? NumberTypeBits : AnyTypeBits, upper: belowZero ? 0L : null);
                }
                else
                {
                    EmitDirectOperand(clause, operand);
                }
            }
            owner.Operand(XRegisterTag, 0);
        }

        private void EmitUnaryArithmetic(Expr.Unary expression, int? failure)
        {
            int operandBase = nextSlot;
            int? operand = GuardSlot(expression.Operand, failure);
            if (failure is null)
            {
                var previous = location;
                owner.EmitArithmeticLine(expression.SourceLine, ref previous);
            }
            owner.Op(GcBifOne, LabelTag, failure ?? 0);
            owner.Operand(BeamWire.UnsignedTag, expression.Operator == ErlangOperators.BitwiseNot ? ReceiveGuardLiveRegisters : 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, expression.Operator, 1));
            GuardOperand(expression.Operand, operand);
            owner.Operand(XRegisterTag, 0);
            nextSlot = Math.Max(operandBase, variables.Values.Select(DecoderSlot).DefaultIfEmpty(-1).Max() + 1);
        }
    }
}
