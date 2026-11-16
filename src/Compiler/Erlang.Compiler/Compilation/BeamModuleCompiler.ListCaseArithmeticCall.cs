using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEntryArithmeticCall(
            FunctionDefinition function,
            Clause clause,
            Expr body,
            string[] parameters,
            int selector,
            Term? selectorValue,
            bool validateOnly = false
        )
        {
            var list = body as Expr.List;
            if ((list?.Tail ?? body) is not Expr.Call call
                || call.Arguments.OfType<Expr.Binary>().ToArray() is not [var arithmetic]
                || arithmetic.Operator is not (ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply
                    or ErlangOperators.Divide or ErlangOperators.IntegerDivide or ErlangOperators.Remainder)
                || ReadOperand(arithmetic.Left) is not { } left || ReadOperand(arithmetic.Right) is not { } right)
            {
                return false;
            }

            int argument = call.Arguments.ToList().FindIndex(value => ReferenceEquals(value, arithmetic));
            if (ReadEntryCall(
                call,
                parameters,
                preparedOperand: argument,
                allowControlTransfer: list is null
            ) is not { } plan)
            {
                return false;
            }

            var heads = new List<(int Register, Term? Literal)>();
            foreach (Expr item in list?.Items ?? [])
            {
                if (ReadOperand(item) is not { } head)
                {
                    return false;
                }

                heads.Add(head);
            }

            int[] saved = heads.Where(head => head.Register >= 0)
                .Select(head => head.Register).Distinct().OrderDescending().ToArray();
            var preserved = call.Arguments.OfType<Expr.Variable>().Select(variable => Array.IndexOf(parameters, variable.Name))
                .Concat(saved).ToHashSet();
            int arithmeticLive = Math.Max(left.Register, right.Register) + 1;
            int live = Math.Max(preserved.DefaultIfEmpty(-1).Max() + 1, arithmeticLive);
            var prepared = call.Arguments.Select((value, index) => (value, index))
                .Where(operand => operand.value is Expr.Variable).Select(operand => operand.index)
                .Concat(saved).ToHashSet();
            int preparedLive = Math.Max(prepared.DefaultIfEmpty(-1).Max() + 1, arithmeticLive);
            bool moveBeforeArithmetic = preparedLive < live && plan.Moves.All(move =>
                move.Destination != left.Register && move.Destination != right.Register && !saved.Contains(move.Destination));
            if (moveBeforeArithmetic)
            {
                preserved = prepared;
                live = preparedLive;
            }

            int destination = argument <= live && !preserved.Contains(argument) ? argument : live;
            if (destination >= FirstReservedXRegister)
            {
                return false;
            }

            if (validateOnly)
            {
                return true;
            }

            if (moveBeforeArithmetic)
            {
                owner.EmitOrderedTailMoves(plan.Moves, []);
            }

            for (int register = function.Arity; register < live; register++)
            {
                if ((register >= parameters.Length || parameters[register] == VariableScopeNames.Wildcard)
                    && (!moveBeforeArithmetic || !plan.Moves.Any(move => move.Destination == register)))
                {
                    owner.EmitConstant(Nil.Value, register);
                }
            }

            if (moveBeforeArithmetic)
            {
                plan.Moves.Clear();
            }

            var previous = location;
            if (call.PreprocessorFile is not null)
            {
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
            }
            else
            {
                owner.EmitArithmeticLine(arithmetic.SourceLine, ref previous);
            }

            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, arithmetic.Operator, 2));
            EmitOperand(left);
            EmitOperand(right);
            owner.Operand(XRegisterTag, destination);
            if (destination != argument)
            {
                plan.Moves.Add((argument, destination));
            }

            if (list is null || heads.Count == 0)
            {
                EmitEntryCallArguments(plan);
                EmitRegisterTailTarget(plan.Call with { Module = plan.Module }, call.Arguments.Count, previous);

                return true;
            }

            int frame = saved.Length;
            EmitFrameAllocation(Math.Max(live, destination + 1), frame);
            for (int slot = 0; slot < frame; slot++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    saved[slot],
                    YRegisterTag,
                    slot
                );
            }

            EmitEntryCall(plan, previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords * heads.Count);
            owner.Operand(BeamWire.UnsignedTag, 1);
            for (int index = heads.Count - 1; index >= 0; index--)
            {
                var head = heads[index];
                if (head.Literal is { } literal)
                {
                    owner.instructions.WriteByte(PutList);
                    owner.LiteralOperand(literal);
                }
                else
                {
                    owner.Op(PutList, YRegisterTag, Array.IndexOf(saved, head.Register));
                }

                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }

            owner.EmitFrameReturn(frame);

            return true;

            (int Register, Term? Literal)? ReadOperand(Expr expression)
            {
                if (expression is Expr.Variable { Name: not VariableScopeNames.Wildcard } variable
                    && Array.IndexOf(parameters, variable.Name) is >= 0 and var register)
                {
                    return register == selector && selectorValue is not null ? (-1, selectorValue) : (register, null);
                }

                return AttributeLiteral.TryNormalizeConstant(expression, out Term constant) ? (-1, constant) : null;
            }

            void EmitOperand((int Register, Term? Literal) operand)
            {
                if (operand.Literal is { } literal)
                {
                    owner.LiteralOperand(literal);
                }
                else if (operand.Register < function.Arity
                    && function.Clauses.FirstOrDefault(original => ReferenceEquals(original.Body, clause.Body))
                        ?.Patterns[operand.Register] is Pattern.Variable variable
                    && variable.Name == parameters[operand.Register]
                    && (NaturalArgumentLowerBound(function, operand.Register)
                        ?? ReadForwardedNaturalLowerBound(function, operand.Register)) is long lower)
                {
                    owner.EmitTypedRegister(operand.Register, IntegerTypeBits, lower: lower);
                }
                else
                {
                    owner.Operand(XRegisterTag, operand.Register);
                }
            }
        }

        private long? ReadForwardedNaturalLowerBound(FunctionDefinition function, int parameter)
        {
            long? lower = null;
            var visited = new HashSet<(string Name, int Arity, int Parameter)>();
            var pending = new Stack<(FunctionDefinition Function, int Position)>();
            pending.Push((function, parameter));
            while (pending.TryPop(out var entry))
            {
                var (target, position) = entry;
                if (!visited.Add((target.Name, target.Arity, position)))
                {
                    continue;
                }

                if (!HasOnlyLocalCalls(target, target.Arity, (caller, call) => ReadArgument(caller, call, position)))
                {
                    return null;
                }
            }

            return lower;

            bool ReadArgument(FunctionDefinition caller, Expr.Call call, int position)
            {
                Expr argument = call.Arguments[position];
                if (argument is Expr.Literal { Value: Integer seed } && seed.Value >= 0 && seed.Value <= long.MaxValue)
                {
                    long value = (long)seed.Value;
                    lower = lower is long previous ? Math.Min(previous, value) : value;

                    return true;
                }

                Expr forwarded = argument switch
                {
                    Expr.Binary
                    {
                        Operator: ErlangOperators.Plus, Left: var input,
                        Right: Expr.Literal { Value: Integer increment }
                    } when increment.Value >= 0 => input,
                    Expr.Binary
                    {
                        Operator: ErlangOperators.Plus, Right: var input,
                        Left: Expr.Literal { Value: Integer increment }
                    } when increment.Value >= 0 => input,
                    _ => argument
                };
                var clause = caller.Clauses.FirstOrDefault(candidate => ReferenceEquals(candidate.Body, call));
                if (forwarded is not Expr.Variable { Name: not VariableScopeNames.Wildcard } variable || clause is null)
                {
                    return false;
                }

                for (int index = 0; index < clause.Patterns.Count; index++)
                {
                    if (clause.Patterns[index] is Pattern.Variable input && input.Name == variable.Name)
                    {
                        pending.Push((caller, index));

                        return true;
                    }
                }

                return false;
            }
        }
    }
}
