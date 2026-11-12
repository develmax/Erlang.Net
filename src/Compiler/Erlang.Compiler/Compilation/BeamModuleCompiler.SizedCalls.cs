using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record SizedCall(Expr.Call Lookup, Expr.Call Convert, Expr.Call Classify, Expr.Call Serialize, Expr.Call Size);

    private static SizedCall? ReadSizedCall(Clause clause)
    {
        if (clause is not
            {
                Guard: null,
                Patterns: [Pattern.Variable first, Pattern.Variable second, Pattern.Variable third],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable shape, Value: Expr.Call { Module: null, Arguments: [Expr.Variable key] } lookup },
                    Expr.Match { Pattern: Pattern.Variable converted, Value: Expr.Call { Module: null, Arguments: [Expr.Variable shape1, Expr.Variable input] } convert },
                    Expr.Match { Pattern: Pattern.Variable bytes, Value: Expr.Call { Module: null, Arguments: [
                        Expr.Call { Module: null, Arguments: [Expr.Variable shape2] } classify,
                        Expr.Variable data, Expr.Variable tags] } serialize },
                    Expr.Tuple { Items: [Expr.Variable result, Expr.Call { Module: null or ErlangModuleNames.Module,
                        Function: "byte_size", DynamicRemote: false, Arguments: [Expr.Variable sized] } size] }
                ] }
            } || !HasUnconditionalParameters(clause)
            || !IsFresh(clause, shape) || !IsFresh(clause, converted) || !IsFresh(clause, bytes)
            || shape.Name == converted.Name || shape.Name == bytes.Name || converted.Name == bytes.Name
            || key.Name != first.Name || input.Name != second.Name || tags.Name != third.Name
            || shape.Name != shape1.Name || shape.Name != shape2.Name || converted.Name != data.Name
            || bytes.Name != result.Name || bytes.Name != sized.Name)
        {
            return null;
        }

        return new(lookup, convert, classify, serialize, size);
    }

    private void EmitSizedCall(
        SizedCall flow,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (!entries.TryGetValue((flow.Lookup.Function, 1), out int lookup)
            || !entries.TryGetValue((flow.Convert.Function, 2), out int convert)
            || !entries.TryGetValue((flow.Classify.Function, 1), out int classify)
            || !entries.TryGetValue((flow.Serialize.Function, 3), out int serialize)
            || flow.Size.Module is null && entries.ContainsKey(("byte_size", 1)))
        {
            throw new NotSupportedException(Unsupported);
        }

        Op(Allocate, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 3);
        MoveRegister(XRegisterTag, 2, YRegisterTag, 0);
        MoveRegister(XRegisterTag, 1, YRegisterTag, 1);
        EmitCallLine(flow.Lookup, location);
        Op(LocalCall, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, lookup);

        MoveRegister(YRegisterTag, 1, XRegisterTag, 1);
        MoveRegister(XRegisterTag, 0, YRegisterTag, 1);
        EmitCallLine(flow.Convert, CallLocation(flow.Lookup, location));
        Op(LocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, convert);

        Op(Swap, YRegisterTag, 1);
        Operand(XRegisterTag, 0);
        EmitCallLine(flow.Classify, CallLocation(flow.Convert, location));
        Op(LocalCall, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, classify);

        MoveRegister(YRegisterTag, 0, XRegisterTag, 2);
        MoveRegister(YRegisterTag, 1, XRegisterTag, 1);
        Op(TrimFrame, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 0);
        EmitCallLine(flow.Serialize, CallLocation(flow.Classify, location));
        Op(LocalCall, BeamWire.UnsignedTag, 3);
        Operand(LabelTag, serialize);

        EmitCallLine(flow.Size, CallLocation(flow.Serialize, location));
        Op(GcBifOne, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, "byte_size", 1));
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 1);
        Op(TestHeap, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 2);
        Op(PutTuple, XRegisterTag, 0);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 1);
        EmitFrameReturn();
    }
}
