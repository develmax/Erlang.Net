using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool TryEmitNestedTupleDynamicTail(FunctionDefinition function, BeamSourceLocation location)
    {
        if (function.Clauses is not [
            {
                Guard: null,
                Patterns: [
                    Pattern.Variable module,
                    Pattern.Tuple { Items: [
                        Pattern.Tuple { Items: [Pattern.Variable first, Pattern.Variable second] },
                        Pattern.Variable third
                    ] },
                    Pattern.Variable last
                ],
                Body: Expr.Call { DynamicRemote: true, Arguments: [
                    Expr.Variable target, Expr.Literal { Value: Atom member },
                    Expr.List { Tail: null, Items: [
                        Expr.Variable arg0, Expr.Variable arg1, Expr.Variable arg2, Expr.Variable arg3
                    ] }
                ] } call
            }
        ] || !DistinctBindings(
            module,
            first,
            second,
            third,
            last
        ) || target.Name != module.Name || arg0.Name != third.Name || arg1.Name != first.Name
            || arg2.Name != second.Name || arg3.Name != last.Name)
        {
            return false;
        }

        int failure = nextLabel - 2;
        EmitTupleCheck(1, 2, failure);
        EmitTuplePart(1, 0, 3);
        EmitTupleCheck(3, 2, failure);
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 4);
        EmitTuplePart(3, 0, 4);
        EmitTuplePart(3, 1, 3);
        EmitTuplePart(1, 1, 1);
        EmitConstant(member, 5);
        Op(Swap, XRegisterTag, 2);
        Operand(XRegisterTag, 3);
        Op(Swap, XRegisterTag, 4);
        Operand(XRegisterTag, 1);
        Op(Swap, XRegisterTag, 0);
        Operand(XRegisterTag, 4);
        EmitCallLine(call, location);
        Op(TailDynamicApply, BeamWire.UnsignedTag, 4);
        Operand(BeamWire.UnsignedTag, 0);

        return true;
    }
}
