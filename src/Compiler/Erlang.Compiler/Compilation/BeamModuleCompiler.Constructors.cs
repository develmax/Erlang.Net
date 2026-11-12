using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool TryEmitConstantBoundTuple(Clause clause)
    {
        if (clause is not { Guard: null, Body: Expr.Sequence { Items: [_, .., Expr.Tuple tuple] } sequence }
            || !HasUnconditionalParameters(clause)
            || ReadConstantBindings(clause, sequence) is not { } bindings)
        {
            return false;
        }

        var items = new Expr[tuple.Items.Count];
        bool usesParameter = false;
        for (int index = 0; index < items.Length; index++)
        {
            Expr item = tuple.Items[index];
            if (AttributeLiteral.TryNormalizeConstant(item, out Term literal, bindings))
            {
                if (literal is not (Atom or Nil)
                    && (literal is not Integer integer || integer.Value < int.MinValue || integer.Value > int.MaxValue))
                {
                    return false;
                }

                items[index] = new Expr.Literal(literal);
            }
            else if (item is Expr.Variable variable && variable.Name != VariableScopeNames.Wildcard
                && clause.Patterns.Any(pattern => pattern is Pattern.Variable parameter && parameter.Name == variable.Name))
            {
                usesParameter = true;
                items[index] = item;
            }
            else
            {
                return false;
            }
        }

        if (!usesParameter || !TryEmitConstructor(clause with { Body = tuple with { Items = items } }))
        {
            return false;
        }

        instructions.WriteByte(Return);
        return true;
    }

    private bool TryEmitConstructor(Clause clause)
    {
        if (clause.Body is Expr.List { Items.Count: 0, Tail: null })
        {
            MoveToX0(AtomTag, NilAtomIndex);

            return true;
        }

        bool tuple = clause.Body is Expr.Tuple;
        IReadOnlyList<Expr>? items = clause.Body switch
        {
            Expr.Tuple value => value.Items,
            Expr.List { Items: [var head] } value => [head, value.Tail ?? new Expr.Literal(Nil.Value)],
            _ => null
        };
        if (items is null)
        {
            return false;
        }

        var operands = new List<(int Tag, BigInteger Value)>(items.Count);
        int live = 0;
        foreach (Expr expression in items)
        {
            switch (expression)
            {
                case Expr.Variable variable:
                    int register = ArgumentRegister(clause, variable.Name);
                    live = Math.Max(live, register + 1);
                    operands.Add((XRegisterTag, register));
                    break;
                case Expr.Literal { Value: Atom atom }:
                    operands.Add((AtomTag, Intern(atom.Name)));
                    break;
                case Expr.Literal { Value: Nil }:
                    operands.Add((AtomTag, NilAtomIndex));
                    break;
                case Expr.Literal { Value: Integer integer } when integer.Value >= int.MinValue && integer.Value <= int.MaxValue:
                    operands.Add((BeamWire.IntegerTag, integer.Value));
                    break;
                default:
                    throw new NotSupportedException(Unsupported);
            }
        }
        if (live == 0)
        {
            throw new NotSupportedException(Unsupported);
        }

        Op(TestHeap, BeamWire.UnsignedTag, tuple ? operands.Count + 1 : ConsHeapWords);
        Operand(BeamWire.UnsignedTag, live);
        if (tuple)
        {
            Op(PutTuple, XRegisterTag, 0);
            instructions.WriteByte(ExtendedListOperand);
            Operand(BeamWire.UnsignedTag, operands.Count);
        }
        else
        {
            instructions.WriteByte(PutList);
        }
        foreach (var operand in operands)
        {
            Operand(operand.Tag, operand.Value);
        }
        if (!tuple)
        {
            Operand(XRegisterTag, 0);
        }

        return true;
    }

    private sealed record ConstructorStep(bool IsTuple, Term?[] Operands, ConstructorStep? Input)
    {
        internal int HeapWords => IsTuple ? Operands.Length + 1 : ConsHeapWords;
    }

    private bool TryEmitSingletonListConstructor(
        FunctionDefinition function,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (function.Clauses is not [{
            Guard: null,
            Patterns: [Pattern.List { Items: [Pattern.Variable head], Tail: null }],
            Body: Expr.Sequence sequence
        }] || head.Name == VariableScopeNames.Wildcard)
        {
            return false;
        }

        var bindings = new Dictionary<string, (Term? Constant, ConstructorStep? Step)>(StringComparer.Ordinal)
        {
            [head.Name] = (null, null)
        };
        Expr.Call? call = null;
        int entry = 0;
        foreach (Expr item in sequence.Items.SkipLast(1))
        {
            if (item is not Expr.Match { Pattern: Pattern.Variable binding, Value: var value }
                || binding.Name == VariableScopeNames.Wildcard || binding.Name == head.Name)
            {
                return false;
            }

            if (call is null && value is Expr.List
                {
                    Items: [Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable argument] } invoke],
                    Tail: null
                }
                && argument.Name == head.Name && entries.TryGetValue((invoke.Function, 1), out entry)
                && bindings.Values.Count(operand => operand.Constant is null) == 1)
            {
                if (!bindings.TryAdd(binding.Name, (null, new ConstructorStep(false, [null, Nil.Value], null))))
                {
                    return false;
                }

                bindings.Remove(head.Name);
                call = invoke;
            }
            else if (!TryReadConstructor(value, bindings, out var operand) || !bindings.TryAdd(binding.Name, operand))
            {
                return false;
            }
        }

        if (!TryReadConstructor(sequence.Items[^1], bindings, out var result) || result.Step is null)
        {
            return false;
        }

        var steps = new Stack<ConstructorStep>();
        int heapWords = 0;
        for (ConstructorStep? step = result.Step; step is not null; step = step.Input)
        {
            steps.Push(step);
            heapWords = checked(heapWords + step.HeapWords);
        }

        int functionInfo = nextLabel - 2;
        Op(IsNonemptyList, LabelTag, functionInfo);
        Operand(XRegisterTag, 0);
        Op(GetList, XRegisterTag, 0);
        Operand(XRegisterTag, 1);
        Operand(XRegisterTag, 2);
        EmitNilTest(XRegisterTag, 2, functionInfo);
        if (call is not null)
        {
            Op(Allocate, BeamWire.UnsignedTag, 0);
            Operand(BeamWire.UnsignedTag, 2);
            MoveToX0(XRegisterTag, 1);
            EmitCallLine(call, location);
            EmitFieldCall(1, entry);
        }

        Op(TestHeap, BeamWire.UnsignedTag, heapWords);
        Operand(BeamWire.UnsignedTag, call is null ? 2 : 1);

        int source = call is null ? 1 : 0;
        foreach (ConstructorStep step in steps)
        {
            if (step.IsTuple)
            {
                Op(PutTuple, XRegisterTag, 0);
                instructions.WriteByte(ExtendedListOperand);
                Operand(BeamWire.UnsignedTag, step.Operands.Length);
            }
            else
            {
                instructions.WriteByte(PutList);
            }

            foreach (Term? operand in step.Operands)
            {
                if (operand is null)
                {
                    Operand(XRegisterTag, source);
                }
                else
                {
                    LiteralOperand(operand);
                }
            }

            if (!step.IsTuple)
            {
                Operand(XRegisterTag, 0);
            }

            source = 0;
        }

        if (call is not null)
        {
            Op(Deallocate, BeamWire.UnsignedTag, 0);
        }
        instructions.WriteByte(Return);

        return true;
    }

    private static bool TryReadConstructor(
        Expr expression,
        IReadOnlyDictionary<string, (Term? Constant, ConstructorStep? Step)> bindings,
        out (Term? Constant, ConstructorStep? Step) result
    )
    {
        if (expression is Expr.Variable variable)
        {
            return bindings.TryGetValue(variable.Name, out result);
        }

        if (AttributeLiteral.TryNormalizeConstant(expression, out Term literal))
        {
            result = (literal, null);
            return true;
        }

        result = default;
        bool tuple = expression is Expr.Tuple;
        IReadOnlyList<Expr>? items = expression switch
        {
            Expr.Tuple value => value.Items,
            Expr.List { Items: [var head] } value => [head, value.Tail ?? new Expr.Literal(Nil.Value)],
            _ => null
        };
        if (items is null)
        {
            return false;
        }

        var operands = new Term?[items.Count];
        ConstructorStep? input = null;
        bool hasRegister = false;
        for (int index = 0; index < items.Count; index++)
        {
            if (!TryReadConstructor(items[index], bindings, out var operand))
            {
                return false;
            }

            operands[index] = operand.Constant;
            if (operand.Constant is null)
            {
                if (hasRegister)
                {
                    return false;
                }

                hasRegister = true;
                input = operand.Step;
            }
        }

        if (hasRegister)
        {
            result = (null, new ConstructorStep(tuple, operands, input));
        }
        else
        {
            result = (tuple ? new TupleTerm(operands.Select(operand => operand!).ToArray())
                : Cons.From([operands[0]!], operands[1]), null);
        }

        return true;
    }

    private static int ArgumentRegister(Clause clause, string name)
    {
        for (int index = 0; index < clause.Patterns.Count; index++)
        {
            if (clause.Patterns[index] is Pattern.Variable parameter && parameter.Name == name)
            {
                return index;
            }
        }

        throw new NotSupportedException(Unsupported);
    }
}
