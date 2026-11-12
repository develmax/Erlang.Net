using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitConstantOptionDispatch(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable input], Body: Expr.Case {
                    Value: Expr.Call { Module: not null, DynamicRemote: false,
                        Arguments: [Expr.Literal key, Expr.Variable argument, Expr.Literal fallback] } call,
                    Clauses: [
                        { Guard: null, Patterns: [Pattern.Literal { Value: Atom first }], Body: var firstBody },
                        { Guard: null, Patterns: [Pattern.Literal { Value: Atom second }], Body: var secondBody }
                    ]
                }
            }] || input.Name == VariableScopeNames.Wildcard || input.Name != argument.Name || first.Equals(second)
                || owner.ResolvesErlangBif(call)
                || !AttributeLiteral.TryNormalizeConstant(firstBody, out Term firstValue)
                || !AttributeLiteral.TryNormalizeConstant(secondBody, out Term secondValue))
            {
                return false;
            }

            int other = Label();
            int initial = Label();
            int invalid = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            owner.EmitConstant(fallback.Value, 2);
            owner.EmitConstant(key.Value, 0);
            var previous = location;
            EmitWindowCall(call, ref previous);
            if (TermOrder.Compare(first, second, exact: true) < 0)
            {
                owner.EmitAtomSelection(0, invalid, first.Name, initial, second.Name, other);
            }
            else
            {
                owner.EmitAtomSelection(0, invalid, second.Name, other, first.Name, initial);
            }

            Mark(other);
            owner.EmitConstant(secondValue, 0);
            owner.EmitFrameReturn(0);
            Mark(initial);
            owner.EmitConstant(firstValue, 0);
            owner.EmitFrameReturn(0);
            Mark(invalid);
            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;
        }
    }
}
