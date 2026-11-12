using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record RoundedCaseResult(
            CaseTupleOperand? Value = null,
            CaseTuplePlan? Tuple = null,
            Expr.Binary? Arithmetic = null,
            CaseTupleOperand Left = default,
            CaseTupleOperand Right = default
        );

        private bool TryEmitRoundedCallCase(FunctionDefinition function)
        {
            if (function.Clauses is not
                [{
                    Guard: null,
                    Body: Expr.Sequence
                    {
                        Items: [
                            Expr.Match { Pattern: Pattern.Variable source, Value: Expr.Call read },
                            Expr.Case
                            {
                                Value: Expr.Call { Arguments: [Expr.Variable input] } convert,
                                Clauses: [
                                    { Patterns: [Pattern.Variable tested], Guard: Expr.Binary comparison, Body: var accepted },
                                    { Patterns: [Pattern.Variable alternate], Guard: null, Body: var rejected }
                                ]
                            }
                        ]
                    }
                } clause]
                || source.Name == VariableScopeNames.Wildcard || input.Name != source.Name
                || !IsGcUnaryGuardCall(convert)
                || convert.Function is not (AutoImportNames.Trunc or AutoImportNames.Round or AutoImportNames.Ceil or AutoImportNames.Floor)
                || GuardComparisonOpcode(comparison) == 0
                || function.Arity + 1 >= FirstReservedXRegister)
            {
                return false;
            }

            string[] parameters = Enumerable.Repeat(VariableScopeNames.Wildcard, function.Arity + 1).ToArray();
            var names = new HashSet<string>(StringComparer.Ordinal);
            int listInput = -1;
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                Pattern pattern = clause.Patterns[index];
                int register = index;
                if (pattern is Pattern.List { Items: [Pattern.Variable item], Tail: null } && listInput < 0)
                {
                    listInput = index;
                    register = function.Arity;
                    pattern = item;
                }

                if (pattern is not Pattern.Variable variable
                    || variable.Name != VariableScopeNames.Wildcard && !names.Add(variable.Name))
                {
                    return false;
                }

                parameters[register] = variable.Name;
            }

            if (names.Contains(source.Name) || tested.Name == source.Name || alternate.Name == source.Name
                || names.Contains(tested.Name) || names.Contains(alternate.Name)
                || ReadEntryCall(read, parameters) is not { } plan)
            {
                return false;
            }

            var (left, right) = EntryComparisonOperands(comparison);
            if (ReadOperand(left, tested.Name) is not { } lhs || ReadOperand(right, tested.Name) is not { } rhs
                || ReadResult(accepted, tested.Name) is not { } success
                || ReadResult(rejected, alternate.Name) is not { } fallback)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            if (listInput >= 0)
            {
                owner.EmitNonemptyList(listInput, functionInfo);
                owner.EmitListParts(listInput, XRegisterTag, function.Arity, XRegisterTag, function.Arity + 1);
                owner.EmitNilTest(XRegisterTag, function.Arity + 1, functionInfo);
            }

            EmitFrameAllocation(EntryCallLiveCount(plan, parameters), 0);
            EmitEntryCall(plan);
            var selectorLine = CallLocation(convert, CallLocation(read, location));
            owner.EmitCallLine(convert, CallLocation(read, location));
            EmitSizeInstruction(convert.Function, 0, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);

            int failure = Label();
            owner.Op(GuardComparisonOpcode(comparison), LabelTag, failure);
            EmitTypedOperand(lhs);
            EmitTypedOperand(rhs);
            EmitResult(success, selectorLine);
            Mark(failure);
            EmitResult(fallback, selectorLine);

            return true;

            CaseTupleOperand? ReadOperand(Expr expression, string selector)
            {
                if (expression is Expr.Variable variable)
                {
                    if (variable.Name == source.Name)
                    {
                        return new CaseTupleOperand(XRegisterTag, 0, null);
                    }

                    return selector != VariableScopeNames.Wildcard && variable.Name == selector
                        ? new CaseTupleOperand(XRegisterTag, 1, null)
                        : null;
                }

                return AttributeLiteral.TryNormalizeConstant(expression, out Term literal)
                    ? new CaseTupleOperand(0, 0, literal)
                    : null;
            }

            RoundedCaseResult? ReadResult(Expr expression, string selector)
            {
                if (ReadOperand(expression, selector) is { } value)
                {
                    return new RoundedCaseResult(Value: value);
                }

                if (expression is Expr.RecordConstruction record)
                {
                    expression = RecordExpressions.Create(record);
                }

                if (expression is Expr.Binary operation)
                {
                    return ReadArithmetic(operation);
                }

                if (expression is not Expr.Tuple tuple)
                {
                    return null;
                }

                var saved = new Dictionary<string, int>(StringComparer.Ordinal);
                if (ReadCaseTuple(tuple, selector, saved, selectorRegister: 1) is { } selected)
                {
                    return new RoundedCaseResult(Tuple: selected);
                }

                if (ReadCaseTuple(tuple, source.Name, saved) is { } original)
                {
                    return new RoundedCaseResult(Tuple: original);
                }

                if (tuple.Items.OfType<Expr.Binary>().ToArray() is not [var computed]
                    || ReadArithmetic(computed) is not { } arithmetic)
                {
                    return null;
                }

                const string result = "\0case-arithmetic";
                var rewritten = tuple with
                {
                    Items = tuple.Items.Select(item => ReferenceEquals(item, computed) ? new Expr.Variable(result) : item).ToArray()
                };
                return ReadCaseTuple(rewritten, result, saved) is { } constructed
                    ? arithmetic with { Tuple = constructed }
                    : null;

                RoundedCaseResult? ReadArithmetic(Expr.Binary binary) =>
                    binary.Operator is ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply
                    && ReadOperand(binary.Left, selector) is { } first && ReadOperand(binary.Right, selector) is { } second
                    && (first.Literal is null || second.Literal is null)
                    && (first.Literal is null or Integer or FloatTerm) && (second.Literal is null or Integer or FloatTerm)
                        ? new RoundedCaseResult(Arithmetic: binary, Left: first, Right: second)
                        : null;
            }

            void EmitTypedOperand(CaseTupleOperand operand)
            {
                if (operand.Literal is null)
                {
                    owner.EmitTypedRegister(operand.Register, operand.Register == 0 ? NumberTypeBits : IntegerTypeBits);
                }
                else
                {
                    owner.LiteralOperand(operand.Literal);
                }
            }

            void EmitResult(RoundedCaseResult result, BeamSourceLocation line)
            {
                if (result.Arithmetic is { } arithmetic)
                {
                    owner.EmitArithmeticLine(arithmetic.SourceLine, ref line);
                    owner.Op(GcBifTwo, LabelTag, 0);
                    int first = result.Left.Literal is null ? result.Left.Register + 1 : 0;
                    int second = result.Right.Literal is null ? result.Right.Register + 1 : 0;
                    owner.Operand(BeamWire.UnsignedTag, Math.Max(first, second));
                    owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, arithmetic.Operator, 2));
                    EmitTypedOperand(result.Left);
                    EmitTypedOperand(result.Right);
                    owner.Operand(XRegisterTag, 0);
                }

                if (result.Tuple is { } tuple)
                {
                    EmitCaseTuple(tuple);
                }
                else if (result.Value is { } value)
                {
                    if (value.Literal is not null)
                    {
                        owner.EmitConstant(value.Literal, 0);
                    }
                    else if (value.Register != 0)
                    {
                        owner.MoveRegister(XRegisterTag, value.Register, XRegisterTag, 0);
                    }
                }

                owner.EmitFrameReturn();
            }
        }
    }
}
