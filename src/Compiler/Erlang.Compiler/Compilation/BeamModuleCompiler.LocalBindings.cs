using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record LocalBinding(Expr.Call Input, Expr.Call? Tail, int Preserved = -1);

    private static LocalBinding? ReadLocalBinding(Clause clause)
    {
        if (clause is not
            {
                Guard: null,
                Patterns: [Pattern.Variable first, Pattern.Variable second, Pattern.Variable third],
                Body: Expr.Sequence { Items: [Expr.Match match, var result] }
            } || !HasUnconditionalParameters(clause))
        {
            return null;
        }

        if (match is
            {
                Pattern: Pattern.Variable bound,
                Value: Expr.Call { Module: null, Arguments: [Expr.Variable input] } call
            }
            && result is Expr.Call { Module: null, Arguments: [Expr.Variable a, Expr.Variable b, Expr.Variable c] } tail
            && IsFresh(clause, bound) && input.Name == first.Name
            && a.Name == bound.Name && b.Name == second.Name && c.Name == third.Name)
        {
            return new(call, tail);
        }

        if (match is
            {
                Pattern: Pattern.Tuple { Items: [Pattern.Variable head, Pattern.Variable rest] },
                Value: Expr.Call { Module: null, Arguments: [Expr.Variable left, Expr.Variable right] } pair
            }
            && result is Expr.Tuple
            {
                Items: [Expr.List { Items: [Expr.Variable item], Tail: Expr.Variable items }, Expr.Variable remainder]
            }
            && IsFresh(clause, head) && IsFresh(clause, rest) && head.Name != rest.Name
            && left.Name == first.Name && right.Name == third.Name
            && item.Name == second.Name && items.Name == head.Name && remainder.Name == rest.Name)
        {
            return new(pair, null, 1);
        }

        return null;
    }

    private void EmitLocalBinding(
        LocalBinding binding,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (!entries.TryGetValue((binding.Input.Function, binding.Input.Arguments.Count), out int input))
        {
            throw new NotSupportedException(Unsupported);
        }

        if (binding.Tail is { } tail)
        {
            if (!entries.TryGetValue((tail.Function, 3), out int output))
            {
                throw new NotSupportedException(Unsupported);
            }

            Op(Allocate, BeamWire.UnsignedTag, 2);
            Operand(BeamWire.UnsignedTag, 3);
            MoveRegister(XRegisterTag, 2, YRegisterTag, 0);
            MoveRegister(XRegisterTag, 1, YRegisterTag, 1);
            EmitCallLine(binding.Input, location);
            Op(LocalCall, BeamWire.UnsignedTag, 1);
            Operand(LabelTag, input);
            MoveRegister(YRegisterTag, 0, XRegisterTag, 2);
            MoveRegister(YRegisterTag, 1, XRegisterTag, 1);
            Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 3);
            Operand(LabelTag, output);
            Operand(BeamWire.UnsignedTag, 2);

            return;
        }

        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 3);
        MoveRegister(XRegisterTag, binding.Preserved, YRegisterTag, 0);
        MoveRegister(XRegisterTag, 2, XRegisterTag, 1);
        EmitCallLine(binding.Input, location);
        Op(LocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, input);

        // A local callee is not assumed to return a pair.
        int failure = nextLabel++;
        Op(IsTuple, LabelTag, failure);
        Operand(XRegisterTag, 0);
        Op(TestArity, LabelTag, failure);
        Operand(XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 2);
        Op(TestHeap, BeamWire.UnsignedTag, 5);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTupleElement(0, 1);
        EmitTupleElement(1, 0);
        Op(PutList, YRegisterTag, 0);
        Operand(XRegisterTag, 1);
        Operand(XRegisterTag, 1);
        Op(PutTuple, XRegisterTag, 0);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(XRegisterTag, 1);
        Operand(XRegisterTag, 0);
        EmitFrameReturn(1);

        Op(Label, BeamWire.UnsignedTag, failure);
        Op(BadMatch, XRegisterTag, 0);
    }

    private (long End, int X, int Y)? lastStackStore;

    private void MoveRegister(int sourceTag, int source, int destinationTag, int destination)
    {
        if (sourceTag == YRegisterTag && destinationTag == XRegisterTag
            && lastStackStore is { } store && store.End == instructions.Position
            && store.X == destination && store.Y == source)
        {
            return;
        }

        Op(Move, sourceTag, source);
        Operand(destinationTag, destination);
        if (sourceTag == XRegisterTag && destinationTag == YRegisterTag)
        {
            lastStackStore = (instructions.Position, source, destination);
        }
    }
}
