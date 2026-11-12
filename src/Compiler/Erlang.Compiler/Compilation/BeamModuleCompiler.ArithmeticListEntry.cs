using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitArithmeticListEntry(FunctionDefinition function, int functionInfo)
        {
            int input = ReadNilBeforeConsInput(function.Clauses, out var ordered);
            if (input < 0 && function.Clauses.Count == 2)
            {
                input = ReadNilBeforeConsInput(function.Clauses.Reverse().ToArray(), out ordered);
            }

            if (input < 0 || function.Arity + 1 >= FirstReservedXRegister
                || ordered is not
                [
                { Guard: null, Body: Expr.Call tail } cons,
                { Guard: null, Body: Expr.Tuple tuple } empty
                ]
                || cons.Patterns[input] is not Pattern.List
                {
                    Items: [Pattern.Variable head],
                    Tail: Pattern.Variable rest
                }
                || tail.Arguments.Count >= FirstReservedXRegister
                || !TryResolveOrdinaryCallTarget(tail, out string? module))
            {
                return false;
            }

            var consInputs = ReadArithmeticListInputs(cons, input);
            var emptyInputs = ReadArithmeticListInputs(empty, input);
            if (consInputs is null || emptyInputs is null)
            {
                return false;
            }

            if (head.Name != VariableScopeNames.Wildcard && !consInputs.TryAdd(head.Name, function.Arity)
                || rest.Name != VariableScopeNames.Wildcard && !consInputs.TryAdd(rest.Name, input))
            {
                return false;
            }

            var consPlan = PlanRegisterArithmetic(
                tail.Arguments,
                consInputs,
                function.Arity + 1,
                tail: true
            );
            var emptyPlan = PlanRegisterArithmetic(
                tuple.Items,
                emptyInputs,
                function.Arity,
                tail: false
            );
            if (consPlan is null || emptyPlan is null || consPlan.Steps.Count + emptyPlan.Steps.Count == 0)
            {
                return false;
            }

            var arguments = ReadLocalArgumentFacts(function, listArithmetic: true);
            PairFacts listFacts = arguments?[input] ?? PairFacts.Unknown;
            if (arguments is not null)
            {
                var consFacts = new Dictionary<int, PairFacts>();
                var emptyFacts = new Dictionary<int, PairFacts>();
                for (int register = 0; register < arguments.Length; register++)
                {
                    if (register != input && (arguments[register] & PairFacts.Integer) != 0)
                    {
                        consFacts.Add(register, arguments[register]);
                        emptyFacts.Add(register, arguments[register]);
                    }
                }

                if ((listFacts & PairFacts.BitList) == PairFacts.BitList)
                {
                    consFacts.Add(function.Arity, PairFacts.BitValue);
                }

                if (consFacts.Count != 0)
                {
                    consPlan = PlanRegisterArithmetic(
                        tail.Arguments,
                        consInputs,
                        function.Arity + 1,
                        tail: true,
                        consFacts
                    )!;
                }

                if (emptyFacts.Count != 0)
                {
                    emptyPlan = PlanRegisterArithmetic(
                        tuple.Items,
                        emptyInputs,
                        function.Arity,
                        tail: false,
                        emptyFacts
                    )!;
                }
            }

            int fallback = Label();
            owner.EmitNonemptyList(input, fallback);
            bool readsHead = consPlan.Reads(function.Arity);
            bool readsTail = consPlan.Reads(input);
            if (readsHead && readsTail)
            {
                owner.Op(GetList, XRegisterTag, input);
                owner.Operand(XRegisterTag, function.Arity);
                owner.Operand(XRegisterTag, input);
            }
            else if (readsHead || readsTail)
            {
                owner.Op(readsHead ? GetHead : GetTail, XRegisterTag, input);
                owner.Operand(XRegisterTag, readsHead ? function.Arity : input);
            }

            var previous = EmitRegisterArithmetic(consPlan, location);
            var moves = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            for (int argument = 0; argument < consPlan.Outputs.Count; argument++)
            {
                var operand = consPlan.Outputs[argument];
                if (operand.Constant is { } value)
                {
                    constants.Add((argument, value));
                }
                else if (operand.Register != argument)
                {
                    moves.Add((argument, operand.Register));
                }
            }

            owner.EmitOrderedTailMoves(moves, constants);
            EmitRegisterTailTarget(tail with { Module = module }, tail.Arguments.Count, previous);

            Mark(fallback);
            if ((listFacts & PairFacts.ProperList) == 0 && !HasProperListEntry(function, input))
            {
                owner.EmitNilTest(XRegisterTag, input, functionInfo);
            }

            EmitRegisterArithmetic(emptyPlan, owner.lastEmittedLine);
            owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Items.Count + 1);
            int live = emptyPlan.Outputs.Select(operand => operand.Register).DefaultIfEmpty(-1).Max() + 1;
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.EmitTupleHeader(XRegisterTag, tuple.Items.Count);
            foreach (var operand in emptyPlan.Outputs)
            {
                EmitRegisterArithmeticOperand(operand);
            }

            owner.instructions.WriteByte(Return);

            return true;
        }

        private static Dictionary<string, int>? ReadArithmeticListInputs(Clause clause, int list)
        {
            var inputs = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int register = 0; register < clause.Patterns.Count; register++)
            {
                if (register == list)
                {
                    continue;
                }

                if (clause.Patterns[register] is not Pattern.Variable variable
                    || variable.Name != VariableScopeNames.Wildcard && !inputs.TryAdd(variable.Name, register))
                {
                    return null;
                }
            }

            return inputs;
        }

        private sealed record RegisterArithmeticOperand(
            int Register,
            Term? Constant = null,
            int Type = 0,
            bool Bit = false
        );

        private sealed record RegisterArithmeticStep(
            Expr.Binary Expression,
            RegisterArithmeticOperand Left,
            RegisterArithmeticOperand Right,
            int Live,
            int Destination
        );

        private sealed record RegisterArithmeticPlan(
            IReadOnlyList<RegisterArithmeticStep> Steps,
            IReadOnlyList<RegisterArithmeticOperand> Outputs,
            IReadOnlySet<int> ReadInputs
        )
        {
            internal bool Reads(int register) => ReadInputs.Contains(register);
        }

        private static RegisterArithmeticPlan? PlanRegisterArithmetic(
            IReadOnlyList<Expr> expressions,
            IReadOnlyDictionary<string, int> inputs,
            int firstResult,
            bool tail,
            IReadOnlyDictionary<int, PairFacts>? inputFacts = null
        )
        {
            var operations = new List<(Expr.Binary Expression, RegisterArithmeticOperand Left,
                RegisterArithmeticOperand Right, int Preferred)>();
            var outputs = new List<RegisterArithmeticOperand>();
            for (int index = 0; index < expressions.Count; index++)
            {
                if (Read(expressions[index], tail ? index : 0) is not { } value)
                {
                    return null;
                }

                outputs.Add(value);
            }

            var uses = operations.SelectMany(operation => new[] { operation.Left, operation.Right })
                .Concat(outputs).Where(operand => operand.Constant is null)
                .GroupBy(operand => operand.Register).ToDictionary(group => group.Key, group => group.Count());
            var registers = uses.Keys.Where(value => value < firstResult).ToDictionary(value => value);
            var readInputs = registers.Keys.ToHashSet();
            var types = new Dictionary<int, int>();
            if (inputFacts is not null)
            {
                foreach (var (register, facts) in inputFacts)
                {
                    if ((facts & PairFacts.Integer) != 0)
                    {
                        types.Add(register, IntegerTypeBits);
                    }
                }
            }
            var steps = new List<RegisterArithmeticStep>();
            for (int index = 0; index < operations.Count; index++)
            {
                var (expression, left, right, preferred) = operations[index];
                int live = registers.Values.DefaultIfEmpty(-1).Max() + 1;
                var lhs = Resolve(left);
                var rhs = Resolve(right);
                Consume(left);
                Consume(right);
                int destination = preferred >= 0 && preferred <= live && !registers.ContainsValue(preferred)
                    ? preferred
                    : new[] { lhs.Register, rhs.Register }.Where(register => register >= 0 && !registers.ContainsValue(register))
                        .DefaultIfEmpty(-1).Min();
                if (destination < 0)
                {
                    destination = Enumerable.Range(0, live + 1).First(register => !registers.ContainsValue(register));
                }

                if (destination >= FirstReservedXRegister)
                {
                    return null;
                }

                int result = firstResult + index;
                registers.Add(result, destination);
                bool integer = IsIntegerRegisterArithmetic(expression.Operator);
                Refine(left, integer);
                Refine(right, integer);
                types.Add(
                    result,
                    integer || expression.Operator != ErlangOperators.Divide
                    && IsIntegerOperand(lhs) && IsIntegerOperand(rhs) ? IntegerTypeBits : NumberTypeBits
                );
                steps.Add(new(
                    expression,
                    lhs,
                    rhs,
                    live,
                    destination
                ));
            }

            return new(steps, outputs.Select(Resolve).ToArray(), readInputs);

            RegisterArithmeticOperand? Read(Expr expression, int preferred)
            {
                if (AttributeLiteral.TryNormalizeConstant(expression, out Term constant))
                {
                    return new(-1, constant);
                }

                if (expression is Expr.Variable variable && inputs.TryGetValue(variable.Name, out int register))
                {
                    return new(register);
                }

                if (expression is Expr.Binary binary && IsRegisterArithmeticOperator(binary.Operator)
                    && Read(binary.Left, -1) is { } left && Read(binary.Right, -1) is { } right)
                {
                    int result = firstResult + operations.Count;
                    operations.Add((binary, left, right, preferred));

                    return new(result);
                }

                return null;
            }

            RegisterArithmeticOperand Resolve(RegisterArithmeticOperand value) =>
                value.Constant is null
                    ? value with
                    {
                        Register = registers[value.Register],
                        Type = types.GetValueOrDefault(value.Register),
                        Bit = ((inputFacts?.GetValueOrDefault(value.Register) ?? PairFacts.Unknown) & PairFacts.BitValue)
                            == PairFacts.BitValue
                    }
                    : value;

            void Refine(RegisterArithmeticOperand value, bool integer)
            {
                if (value.Constant is null && (integer || !types.ContainsKey(value.Register)))
                {
                    types[value.Register] = integer ? IntegerTypeBits : NumberTypeBits;
                }
            }

            static bool IsIntegerOperand(RegisterArithmeticOperand value) =>
                value.Constant is Integer || value.Type == IntegerTypeBits;

            void Consume(RegisterArithmeticOperand value)
            {
                if (value.Constant is null && --uses[value.Register] == 0)
                {
                    registers.Remove(value.Register);
                }
            }
        }

        private static bool IsRegisterArithmeticOperator(string name) =>
            name is ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply or ErlangOperators.Divide
                or ErlangOperators.IntegerDivide or ErlangOperators.Remainder or ErlangOperators.BitwiseAnd
                or ErlangOperators.BitwiseOr or ErlangOperators.BitwiseXor or ErlangOperators.ShiftLeft
                or ErlangOperators.ShiftRight;

        private static bool IsIntegerRegisterArithmetic(string name) =>
            name is ErlangOperators.IntegerDivide or ErlangOperators.Remainder or ErlangOperators.BitwiseAnd
                or ErlangOperators.BitwiseOr or ErlangOperators.BitwiseXor or ErlangOperators.ShiftLeft
                or ErlangOperators.ShiftRight;

        private BeamSourceLocation EmitRegisterArithmetic(RegisterArithmeticPlan plan, BeamSourceLocation previous)
        {
            foreach (var step in plan.Steps)
            {
                owner.EmitArithmeticLine(step.Expression.SourceLine, ref previous);
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, step.Live);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, step.Expression.Operator, 2));
                EmitRegisterArithmeticOperand(step.Left, typed: true);
                EmitRegisterArithmeticOperand(step.Right, typed: true);
                owner.Operand(XRegisterTag, step.Destination);
            }

            return previous;
        }

        private void EmitRegisterArithmeticOperand(RegisterArithmeticOperand operand, bool typed = false)
        {
            if (operand.Constant is { } constant)
            {
                owner.LiteralOperand(constant);
            }
            else if (typed && operand.Type != 0)
            {
                owner.EmitTypedRegister(
                    operand.Register,
                    operand.Type,
                    lower: operand.Bit ? 0 : null,
                    upper: operand.Bit ? 1 : null
                );
            }
            else
            {
                owner.Operand(XRegisterTag, operand.Register);
            }
        }
    }
}
