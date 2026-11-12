using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitForwardedChildList(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Patterns: [Pattern.List { Tail: null, Items: [Pattern.Variable first, Pattern.Variable second, Pattern.Variable third] }],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable flags },
                    Expr.Match { Pattern: Pattern.Variable children },
                    Expr.Tuple
                ] }
            } clause] || !DistinctBindings(
                first,
                second,
                third,
                flags,
                children
            )
                || ReadCallListResult(clause) is not { } result
                || result.Calls.Items is not [
                    Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable single] } left,
                    Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable head, Expr.Variable tail] } right
                ] || single.Name != second.Name || head.Name != first.Name || tail.Name != third.Name
                || !entries.TryGetValue((left.Function, 1), out int leftEntry)
                || !entries.TryGetValue((right.Function, 2), out int rightEntry))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            owner.EmitNonemptyList(0, failure);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            for (int register = 3; register <= 4; register++)
            {
                owner.EmitNonemptyList(2, failure);
                owner.EmitListParts(
                    2,
                    XRegisterTag,
                    register,
                    XRegisterTag,
                    2
                );
            }

            owner.EmitNilTest(XRegisterTag, 2, failure);
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 5);
            owner.MoveRegister(
                XRegisterTag,
                4,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                3,
                XRegisterTag,
                0
            );
            var previous = location;
            EmitWindowLocal(left, leftEntry, ref previous);
            owner.Op(Swap, YRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            Load(0, 1);
            TrimDetailFrame(1, 1);
            EmitWindowLocal(right, rightEntry, ref previous);
            owner.EmitChildListResult(result.Tag, result.Flags, 2);
            owner.EmitFrameReturn(1);

            return true;
        }
    }
}
