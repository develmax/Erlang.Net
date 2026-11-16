using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool TryEmitNestedTupleArgument(Clause clause, BeamSourceLocation location)
    {
        if (clause is not
            {
                Guard: null, Patterns: [Pattern.Variable value, Pattern.Variable first, Pattern.Variable second],
                Body: Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false, Arguments: [
                    Expr.Literal { Value: Atom target }, Expr.Tuple { Items: [
                        Expr.Tuple { Items: [Expr.Variable left, Expr.Variable right] }, Expr.Variable payload
                    ] }
                ] } call
            }
            || !DistinctBindings(value, first, second)
            || value.Name != payload.Name || first.Name != left.Name || second.Name != right.Name)
        {
            return false;
        }

        Op(TestHeap, BeamWire.UnsignedTag, 6);
        Operand(BeamWire.UnsignedTag, 3);
        EmitTagMatchTuple(1, (XRegisterTag, 1), (XRegisterTag, 2));
        EmitTagMatchTuple(1, (XRegisterTag, 1), (XRegisterTag, 0));
        EmitConstant(target, 0);
        EmitCallLine(call, location);
        Op(TailExternalCall, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import(call.Module!, call.Function, 2));
        return true;
    }
}
