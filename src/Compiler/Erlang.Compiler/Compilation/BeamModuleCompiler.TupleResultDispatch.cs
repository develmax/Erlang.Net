using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed class TupleDispatchBlock(int register, (Term Key, TupleDispatchBlock Target)[] choices, Term? result = null)
    {
        internal int Register { get; } = register;
        internal (Term Key, TupleDispatchBlock Target)[] Choices { get; } = choices;
        internal Term? Result { get; } = result;
        internal int Label { get; set; }
    }

    private bool TryEmitLiteralTupleReturns(FunctionDefinition function)
    {
        if (function.Clauses is not
            [
                { Guard: null, Patterns: [Pattern.Literal { Value: Atom tag }, ..], Body: Expr.Tuple } first,
                { Guard: null } fallback
            ]
            || !HasUnconditionalParameters(fallback)
            || Read(first, 1) is not { } accepted || Read(fallback, 0) is not { } rejected)
        {
            return false;
        }

        int alternative = nextLabel++;
        Op(ExactEqualTest, LabelTag, alternative);
        Operand(XRegisterTag, 0);
        LiteralOperand(tag);
        EmitReturn(accepted);
        Op(Label, BeamWire.UnsignedTag, alternative);
        EmitReturn(rejected);

        return true;

        (Term? Constant, int Register)[]? Read(Clause clause, int start)
        {
            var bindings = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = start; index < clause.Patterns.Count; index++)
            {
                if (clause.Patterns[index] is not Pattern.Variable variable
                    || variable.Name != VariableScopeNames.Wildcard && !bindings.TryAdd(variable.Name, index))
                {
                    return null;
                }
            }

            if (AttributeLiteral.TryNormalizeConstant(clause.Body, out Term literal))
            {
                return [(literal, -1)];
            }

            if (clause.Body is not Expr.Tuple tuple || tuple.Items.Count < 2)
            {
                return null;
            }

            var operands = new (Term? Constant, int Register)[tuple.Items.Count];
            for (int index = 0; index < operands.Length; index++)
            {
                Expr item = tuple.Items[index];
                if (AttributeLiteral.TryNormalizeConstant(item, out Term constant))
                {
                    operands[index] = (constant, -1);
                }
                else if (item is Expr.Variable variable && bindings.TryGetValue(variable.Name, out int register))
                {
                    operands[index] = (null, register);
                }
                else
                {
                    return null;
                }
            }

            return operands;
        }

        void EmitReturn((Term? Constant, int Register)[] operands)
        {
            if (operands is [(var literal, _)])
            {
                EmitConstant(literal!, 0);
            }
            else
            {
                Op(TestHeap, BeamWire.UnsignedTag, operands.Length + 1);
                Operand(BeamWire.UnsignedTag, operands.Max(operand => operand.Register) + 1);
                Op(PutTuple, XRegisterTag, 0);
                instructions.WriteByte(ExtendedListOperand);
                Operand(BeamWire.UnsignedTag, operands.Length);
                foreach (var (constant, register) in operands)
                {
                    if (constant is null)
                    {
                        Operand(XRegisterTag, register);
                    }
                    else
                    {
                        LiteralOperand(constant);
                    }
                }
            }

            instructions.WriteByte(Return);
        }
    }

    private bool TryEmitTupleResultDispatch(FunctionDefinition function)
    {
        if (function.Arity != 1 || function.Clauses.Count < 3
            || function.Clauses[^1] is not { Guard: null, Patterns: [Pattern.Variable fallback] } last
            || !AttributeLiteral.TryNormalizeConstant(last.Body, out Term fallbackResult))
        {
            return false;
        }

        var rows = new Dictionary<(Atom First, Atom Second, Integer Third), Term>();
        foreach (Clause clause in function.Clauses.SkipLast(1))
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [
                        Pattern.Literal { Value: Atom first }, Pattern.Literal { Value: Atom second },
                        Pattern.Literal { Value: Integer third }
                    ] }]
                }
                || !AttributeLiteral.TryNormalizeConstant(clause.Body, out Term result)
                || !rows.TryAdd((first, second, third), result))
            {
                return false;
            }
        }

        var order = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
        var returns = new Dictionary<Term, TupleDispatchBlock>();
        var checks = new Dictionary<(Integer Key, TupleDispatchBlock Target), TupleDispatchBlock>();
        var decisions = new List<TupleDispatchBlock>();
        var blocks = new List<TupleDispatchBlock>();
        var fallbackBlock = new TupleDispatchBlock(0, [], fallbackResult);
        returns.Add(fallbackResult, fallbackBlock);

        TupleDispatchBlock ReturnBlock(Term result)
        {
            if (!returns.TryGetValue(result, out var block))
            {
                block = new TupleDispatchBlock(0, [], result);
                returns.Add(result, block);
            }

            return block;
        }

        TupleDispatchBlock Decide(int register, (Term Key, TupleDispatchBlock Target)[] choices)
        {
            if (register == 0 && choices is [(Integer key, var target)])
            {
                if (!checks.TryGetValue((key, target), out var check))
                {
                    check = new TupleDispatchBlock(register, choices);
                    checks.Add((key, target), check);
                }

                return check;
            }

            var decision = decisions.Find(block => block.Register == register && block.Choices.SequenceEqual(choices));
            if (decision is null)
            {
                decision = new TupleDispatchBlock(register, choices);
                decisions.Add(decision);
            }

            return decision;
        }

        void Visit(TupleDispatchBlock block)
        {
            blocks.Add(block);
            foreach (var (_, target) in block.Choices.Reverse())
            {
                Visit(target);
            }
        }

        var root = Decide(1, rows.GroupBy(row => row.Key.First).OrderBy(group => group.Key, order)
            .Select(group => ((Term)group.Key, Decide(2, group.GroupBy(row => row.Key.Second).OrderBy(member => member.Key, order)
                .Select(member => ((Term)member.Key, Decide(0, member.OrderBy(row => row.Key.Third.Value)
                    .Select(row => ((Term)row.Key.Third, ReturnBlock(row.Value))).ToArray())))
                .ToArray()))).ToArray());
        foreach (var (_, target) in root.Choices.Reverse())
        {
            Visit(target);
        }
        blocks.Add(fallbackBlock);

        // Equivalent blocks are kept at the last occurrence so the final predecessor can fall through.
        var layout = blocks.AsEnumerable().Reverse().Distinct().Reverse().ToArray();
        var targets = new HashSet<TupleDispatchBlock>(root.Choices.Select(choice => choice.Target)) { fallbackBlock };
        for (int index = 0; index < layout.Length; index++)
        {
            var block = layout[index];
            if (block.Choices.Length != 1 || index + 1 >= layout.Length || block.Choices[0].Target != layout[index + 1])
            {
                targets.UnionWith(block.Choices.Select(choice => choice.Target));
            }
        }
        foreach (var block in layout)
        {
            if (targets.Contains(block))
            {
                block.Label = nextLabel++;
            }
        }

        int failure = fallbackBlock.Label;
        EmitTupleCheck(0, 3, failure);
        EmitTuplePart(0, 0, 1);
        EmitTuplePart(0, 1, 2);
        EmitTuplePart(0, 2, 0);
        EmitMembershipSelect(1, failure, root.Choices.Select(choice => (choice.Key, choice.Target.Label)));

        for (int index = 0; index < layout.Length; index++)
        {
            var block = layout[index];
            if (block.Label != 0)
            {
                Op(Label, BeamWire.UnsignedTag, block.Label);
            }

            if (block.Result is { } result)
            {
                EmitConstant(result, 0);
                instructions.WriteByte(Return);
            }
            else if (block.Choices is [var choice])
            {
                Op(ExactEqualTest, LabelTag, failure);
                Operand(XRegisterTag, block.Register);
                LiteralOperand(choice.Key);
                if (index + 1 >= layout.Length || choice.Target != layout[index + 1])
                {
                    Op(Jump, LabelTag, choice.Target.Label);
                }
            }
            else
            {
                EmitMembershipSelect(block.Register, failure, block.Choices.Select(choice => (choice.Key, choice.Target.Label)));
            }
        }

        return true;
    }
}
