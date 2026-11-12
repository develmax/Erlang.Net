using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static IReadOnlyList<Expr>? AppendPrefix(Expr expression) => expression switch
    {
        Expr.List { Tail: null } list => list.Items,
        Expr.Literal { Value: Nil } => [],
        _ => null
    };

    private static bool IsDirectAppend(Expr.Binary append) =>
        IsCallOperand(append.Left) && IsCallOperand(append.Right);

    private static bool IsCallOperand(Expr expression) =>
        expression is Expr.Variable || AttributeLiteral.TryNormalizeConstant(expression, out _);

    private bool TryEmitAppendPair(Clause clause, BeamSourceLocation location)
    {
        if (clause is not
            {
                Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second],
                Body: Expr.Tuple
                {
                    Items: [
                        Expr.Binary { Operator: ErlangOperators.Append, Left: Expr.Variable left, Right: Expr.Variable right } append,
                        Expr.Variable carried
                    ]
                }
            } || !DistinctBindings(first, second) || left.Name != first.Name || right.Name != second.Name
            || carried.Name != first.Name && carried.Name != second.Name)
        {
            return false;
        }

        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 2);
        MoveRegister(XRegisterTag, carried.Name == first.Name ? 0 : 1, YRegisterTag, 0);
        EmitArithmeticLine(append.SourceLine, ref location);
        Op(ExternalCall, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, ErlangOperators.Append, 2));
        Op(TestHeap, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTagMatchTuple(0, (XRegisterTag, 0), (YRegisterTag, 0));
        EmitFrameReturn(1);

        return true;
    }

    private bool TryEmitDirectAppend(
        Clause clause,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (clause.Body is not Expr.Binary { Operator: ErlangOperators.Append or ErlangOperators.SubtractList } append || !HasUnconditionalParameters(clause))
        {
            return false;
        }
        if (append.Operator == ErlangOperators.Append && AppendPrefix(append.Left) is { } prefix
            && prefix.All(item => AttributeLiteral.TryNormalizeConstant(item, out _)))
        {
            if (AttributeLiteral.TryNormalizeConstant(new Expr.List(prefix, append.Right), out Term list))
            {
                EmitConstant(list, 0);
            }
            else if (append.Right is Expr.Variable tail)
            {
                int register = ArgumentRegister(clause, tail.Name);
                if (prefix.Count != 0)
                {
                    Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords * prefix.Count);
                    Operand(BeamWire.UnsignedTag, register + 1);
                }
                if (register != 0)
                {
                    MoveToX0(XRegisterTag, register);
                }
                foreach (Expr item in prefix.Reverse())
                {
                    AttributeLiteral.TryNormalizeConstant(item, out Term constant);
                    instructions.WriteByte(PutList);
                    LiteralOperand(constant);
                    Operand(XRegisterTag, 0);
                    Operand(XRegisterTag, 0);
                }
            }
            else
            {
                return false;
            }
            instructions.WriteByte(Return);
            return true;
        }
        if (!IsDirectAppend(append))
        {
            return false;
        }

        var call = new Expr.Call(ErlangModuleNames.Module, append.Operator, [append.Left, append.Right])
        {
            SourceLine = append.SourceLine,
            PreprocessorFile = location.File
        };
        EmitCall(clause, location, call, entries);
        return true;
    }
}
