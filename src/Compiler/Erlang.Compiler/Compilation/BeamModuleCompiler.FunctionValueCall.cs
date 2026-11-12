using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static bool HasFunctionValueCall(Expr expression) =>
        expression is Expr.Apply
        || expression is not Expr.Fun { SelfName: not null }
            && LocalFunctionEntries.Children(expression).Any(HasFunctionValueCall);

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitDirectFunctionValueCall(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Apply { Function: Expr.Variable target } apply } clause]
                || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            var registers = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                if (clause.Patterns[index] is Pattern.Variable { Name: not VariableScopeNames.Wildcard } parameter)
                {
                    registers.Add(parameter.Name, index);
                }
            }

            if (!registers.TryGetValue(target.Name, out int targetRegister))
            {
                return false;
            }

            var moves = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            for (int index = 0; index < apply.Arguments.Count; index++)
            {
                Expr argument = apply.Arguments[index];
                if (argument is Expr.Variable variable && registers.TryGetValue(variable.Name, out int source))
                {
                    if (source != index)
                    {
                        moves.Add((index, source));
                    }
                }
                else if (AttributeLiteral.TryNormalizeConstant(argument, out Term value))
                {
                    constants.Add((index, value));
                }
                else
                {
                    return false;
                }
            }

            int arity = apply.Arguments.Count;
            if (targetRegister != arity)
            {
                moves.Add((arity, targetRegister));
            }

            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, function.Arity);
            owner.EmitArgumentMoves(moves, constants);
            EmitFunctionValueCallOpcode(apply);
            owner.EmitFrameReturn(0);

            return true;
        }

        private void EmitFunctionValueCall(Expr.Apply apply)
        {
            if (IsClosureSelfCall(apply))
            {
                EmitClosureSelfCall(apply, tail: false);
                return;
            }

            int operandBase = nextSlot;
            int target = ValueSlot(apply.Function, null);
            LoadCallArguments(apply.Arguments, null);
            Load(target, apply.Arguments.Count);
            EmitFunctionValueCallOpcode(apply);
            ReleaseOperandSlots(operandBase);
        }

        private void EmitFunctionValueCallOpcode(Expr.Apply apply)
        {
            var previous = location;
            owner.EmitArithmeticLine(apply.SourceLine, ref previous);
            owner.Op(CallFun, BeamWire.UnsignedTag, apply.Arguments.Count);
        }
    }
}
