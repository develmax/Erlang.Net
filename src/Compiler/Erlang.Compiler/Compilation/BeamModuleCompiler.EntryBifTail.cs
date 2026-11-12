using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private readonly record struct TailBifOperand(
            int Register,
            Integer? Literal = null,
            int Type = 0,
            long? Lower = null,
            long? Upper = null,
            bool Saved = false
        );

        private sealed record TailBif(
            string Name,
            byte Opcode,
            Expr Expression,
            int Destination,
            int Live,
            TailBifOperand[] Operands
        );

        private bool TryEmitEntryBifTail(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence
                {
                    Items: [
                        Expr.Match { Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding, Value: Expr.Call prefix },
                        Expr.Call tail
                    ]
                }
                || tail.Arguments.Count >= FirstReservedXRegister)
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(binding.Name, StringComparer.Ordinal)
                || ReadEntryCall(prefix, parameters) is not { } first)
            {
                return false;
            }

            bool returnsBif = ReadTailBifOpcode(tail) is not null;
            int argument = returnsBif ? 0 : Enumerable.Range(0, tail.Arguments.Count).FirstOrDefault(index =>
                tail.Arguments[index] is Expr.Binary or Expr.Call, -1);
            EntryCall? continuation = returnsBif || argument < 0 ? null : ReadEntryCall(
                tail,
                [binding.Name],
                parameters,
                preparedOperand: argument
            );
            if (!returnsBif && continuation is null)
            {
                return false;
            }

            var input = new TailBifOperand(0);
            if (first.Module == ErlangModuleNames.Module && prefix.Arguments.Count == 1)
            {
                input = prefix.Function switch
                {
                    AutoImportNames.IolistSize => input with { Type = IntegerTypeBits, Lower = 0 },
                    AutoImportNames.IolistToBinary or AutoImportNames.ListToBinary => input with { Type = MatchableTypeBits },
                    _ => input
                };
            }

            if (first.Module == "lists" && prefix.Arguments.Count == 1
                && prefix.Function is "reverse" or "sort" or "usort")
            {
                input = input with { Type = ProperListTypeBits };
            }

            bool keepsResult = tail.Arguments.OfType<Expr.Variable>().Any(variable => variable.Name == binding.Name);
            int destination = !returnsBif && argument == 0 && keepsResult ? 1 : argument;
            var occupied = new HashSet<int> { 0 };
            var operations = new List<TailBif>();
            if (Read(returnsBif ? tail : tail.Arguments[argument], destination) is null || operations.Count == 0)
            {
                return false;
            }

            if (input.Type == 0 && first.Module is null
                && operations.Any(operation => operation.Name == AutoImportNames.Length
                    && operation.Operands is [var operand] && IsCallResult(operand))
                && (InferPairFacts(prefix) & PairFacts.ProperList) != 0)
            {
                input = input with { Type = ProperListTypeBits };
            }

            var loads = continuation?.SavedInputs ?? [];
            int[] saved = loads.Select(load => load.Source)
                .Concat(operations.SelectMany(operation => operation.Operands)
                    .Where(operand => operand.Saved).Select(operand => operand.Register))
                .Distinct().OrderDescending().ToArray();
            int live = Math.Max(EntryCallLiveCount(first, parameters), saved.DefaultIfEmpty(-1).Max() + 1);
            EmitFrameAllocation(live, saved.Length);
            for (int slot = 0; slot < saved.Length; slot++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    saved[slot],
                    YRegisterTag,
                    slot
                );
            }

            EmitEntryCall(first);
            var previous = CallLocation(prefix, location);
            var provenInput = input;
            foreach (TailBif operation in operations)
            {
                if (operation.Expression is Expr.Call bif)
                {
                    owner.EmitCallLine(bif, previous);
                    previous = CallLocation(bif, previous);
                }
                else if (operation.Expression is Expr.Binary binary)
                {
                    owner.EmitArithmeticLine(binary.SourceLine, ref previous);
                }

                int arity = operation.Operands.Length;
                owner.Op(operation.Opcode, LabelTag, 0);
                if (operation.Opcode is GcBifOne or GcBifTwo or GcBifThree)
                {
                    owner.Operand(BeamWire.UnsignedTag, operation.Live);
                }
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, operation.Name, arity));
                foreach (TailBifOperand source in operation.Operands)
                {
                    TailBifOperand operand = IsCallResult(source) ? provenInput : source;
                    if (operand.Literal is { } literal)
                    {
                        owner.LiteralOperand(literal);
                    }
                    else if (operand.Saved)
                    {
                        owner.Operand(YRegisterTag, Array.IndexOf(saved, operand.Register));
                    }
                    else if (operand.Type != 0)
                    {
                        owner.EmitTypedRegister(
                            operand.Register,
                            operand.Type,
                            operand.Lower,
                            operand.Upper,
                            unit: operand.Type == MatchableTypeBits ? BitSyntaxDefaults.BinaryUnit : null
                        );
                    }
                    else
                    {
                        owner.Operand(XRegisterTag, operand.Register);
                    }
                }

                owner.Operand(XRegisterTag, operation.Destination);
                if (provenInput.Type == 0 && operation.Operands is [var checkedOperand] && IsCallResult(checkedOperand))
                {
                    provenInput = operation.Name switch
                    {
                        AutoImportNames.ByteSize or AutoImportNames.BitSize => provenInput with { Type = MatchableTypeBits },
                        AutoImportNames.Length => provenInput with { Type = ProperListTypeBits },
                        _ => provenInput
                    };
                }
            }

            if (continuation is null)
            {
                owner.EmitFrameReturn(saved.Length);
                return true;
            }

            if (destination != argument)
            {
                continuation.Moves.Add((argument, destination));
            }

            owner.EmitOrderedTailMoves(
                continuation.Moves,
                continuation.Constants,
                loads.Select(load => (load.Destination, Array.IndexOf(saved, load.Source))).ToList()
            );
            EmitPreparedFrameTail(continuation, previous, saved.Length);

            return true;

            TailBifOperand? Read(Expr expression, int? target = null)
            {
                if (expression is Expr.Variable variable && variable.Name != VariableScopeNames.Wildcard)
                {
                    if (variable.Name == binding.Name)
                    {
                        return input;
                    }

                    int parameter = Array.IndexOf(parameters, variable.Name);
                    return parameter < 0 ? null : new TailBifOperand(parameter, Saved: true);
                }

                if (AttributeLiteral.TryNormalizeConstant(expression, out Term value) && value is Integer literal)
                {
                    long? bound = literal.Value >= long.MinValue && literal.Value <= long.MaxValue ? (long)literal.Value : null;

                    return new(
                        -1,
                        literal,
                        IntegerTypeBits,
                        bound,
                        bound
                    );
                }

                string name;
                byte opcode;
                TailBifOperand[] operands;
                if (expression is Expr.Call call && ReadTailBifOpcode(call) is byte bifOpcode)
                {
                    name = call.Function;
                    opcode = bifOpcode;
                    operands = new TailBifOperand[call.Arguments.Count];
                    for (int index = 0; index < operands.Length; index++)
                    {
                        if (Read(call.Arguments[index]) is not { } operand)
                        {
                            return null;
                        }

                        operands[index] = operand;
                    }
                }
                else if (expression is Expr.Binary binary
                    && binary.Operator is ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Remainder
                    && Read(binary.Left) is { } left && Read(binary.Right) is { } right)
                {
                    name = binary.Operator;
                    opcode = GcBifTwo;
                    operands = [left, right];
                }
                else
                {
                    return null;
                }

                int required = occupied.Max() + 1;
                foreach (TailBifOperand sourceOperand in operands)
                {
                    if (!sourceOperand.Saved && sourceOperand.Register > 0)
                    {
                        occupied.Remove(sourceOperand.Register);
                    }
                }

                int register = target ?? Enumerable.Range(1, FirstReservedXRegister - 1)
                    .FirstOrDefault(candidate => !occupied.Contains(candidate), -1);
                if (register < 0)
                {
                    return null;
                }

                occupied.Add(register);
                operations.Add(new TailBif(
                    name,
                    opcode,
                    expression,
                    register,
                    required,
                    operands
                ));

                return ResultType(register, name, operands, returnsBif);
            }
        }

        private static bool IsCallResult(TailBifOperand operand) =>
            operand.Register == 0 && !operand.Saved && operand.Literal is null;

        private byte? ReadTailBifOpcode(Expr.Call call) =>
            call is { Module: null or ErlangModuleNames.Module, DynamicRemote: false }
            && owner.ResolvesErlangBif(call)
                ? (call.Function, call.Arguments.Count) switch
                {
                    (AutoImportNames.ByteSize or AutoImportNames.BitSize or AutoImportNames.Length, 1) => GcBifOne,
                    (AutoImportNames.Min or AutoImportNames.Max, 2) => BifTwo,
                    (AutoImportNames.BinaryPart, 3) => GcBifThree,
                    _ => null
                }
                : null;

        private static TailBifOperand ResultType(int register, string name, TailBifOperand[] operands, bool returnsBif)
        {
            if (operands is [_])
            {
                long? upperBound = returnsBif && name is AutoImportNames.ByteSize or AutoImportNames.BitSize
                    ? MaximumBitSize : null;
                return new(register, Type: IntegerTypeBits, Lower: 0, Upper: upperBound);
            }

            if (name == AutoImportNames.BinaryPart)
            {
                return new(register, Type: MatchableTypeBits);
            }

            if (name == AutoImportNames.Min && (operands[0].Upper is long || operands[1].Upper is long))
            {
                long? upperBound = operands[0].Upper ?? operands[1].Upper;
                if (operands[0].Upper is long first && operands[1].Upper is long second)
                {
                    upperBound = Math.Min(first, second);
                }

                return new(register, Type: NumberTypeBits, Upper: upperBound);
            }

            if (name is AutoImportNames.Min or AutoImportNames.Max)
            {
                return new(register);
            }

            var left = operands[0];
            var right = operands[1];
            if (name == ErlangOperators.Remainder && left.Lower >= 0 && right.Lower is > 0 and var divisor
                && right.Literal is not null)
            {
                return new(
                    register,
                    Type: IntegerTypeBits,
                    Lower: 0,
                    Upper: Math.Min(left.Upper ?? long.MaxValue, divisor - 1)
                );
            }

            if (left.Type != IntegerTypeBits || right.Type != IntegerTypeBits)
            {
                return new(register);
            }

            long? lower = name == ErlangOperators.Plus ? Combine(left.Lower, right.Lower, subtract: false)
                : name == ErlangOperators.Minus ? Combine(left.Lower, right.Upper, subtract: true) : null;
            long? upper = name == ErlangOperators.Plus ? Combine(left.Upper, right.Upper, subtract: false)
                : name == ErlangOperators.Minus ? Combine(left.Upper, right.Lower, subtract: true) : null;

            return new(
                register,
                Type: IntegerTypeBits,
                Lower: lower,
                Upper: upper
            );

            static long? Combine(long? left, long? right, bool subtract)
            {
                if (left is not long first || right is not long second)
                {
                    return null;
                }

                System.Numerics.BigInteger value = subtract
                    ? (System.Numerics.BigInteger)first - second : (System.Numerics.BigInteger)first + second;

                return value >= long.MinValue && value <= long.MaxValue ? (long)value : null;
            }
        }
    }
}
