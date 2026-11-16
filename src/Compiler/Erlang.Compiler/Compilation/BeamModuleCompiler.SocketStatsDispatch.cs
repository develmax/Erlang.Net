using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSocketStatsDispatch(FunctionDefinition function)
        {
            if (function.Clauses is not [var pidClause, var nestedClause, var pairClause, var plainClause, {
                Guard: null, Patterns: [Pattern.Variable module, Pattern.Variable socket, Pattern.Variable options],
                Body: Expr.Call { DynamicRemote: true, Arguments: [
                    Expr.Variable target, Expr.Literal { Value: Atom member },
                    Expr.List { Tail: null, Items: [Expr.Variable socketArg, Expr.Variable optionArg] }
                ] } dynamicCall
            }] || !DistinctBindings(module, socket, options)
                || target.Name != module.Name || socketArg.Name != socket.Name || optionArg.Name != options.Name
                || pidClause is not { Guard: Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                    Function: AutoImportNames.IsPid, Arguments: [Expr.Variable checkedPid] } pidTest }
                || !owner.ResolvesErlangBif(pidTest)
                || nestedClause.Guard is not null || pairClause.Guard is not null || plainClause.Guard is not null
                || ReadStatsTail(pidClause, 0) is not { } pid
                || ReadStatsTail(nestedClause, 2) is not { } nested
                || ReadStatsTail(pairClause, 1) is not { } pair
                || ReadStatsTail(plainClause, 0) is not { } plain
                || checkedPid.Name != pid.Socket.Name || !pid.Transport.Equals(nested.Transport)
                || !pid.Transport.Equals(pair.Transport) || !pid.Transport.Equals(plain.Transport))
            {
                return false;
            }

            int tuple = Label();
            int direct = Label();
            int noTuple = Label();
            int dynamic = Label();
            EmitConfigurationAtomTest(pid.Transport, dynamic);
            owner.Op(IsPid, LabelTag, tuple);
            owner.Operand(XRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            EmitSocketExternalTail(pid.Call);
            Mark(tuple);
            owner.EmitTupleCheck(1, 2, noTuple);
            owner.EmitTuplePart(1, 1, 0);
            owner.EmitTupleCheck(0, 2, direct);
            owner.EmitTuplePart(0, 1, 0);
            EmitSocketExternalTail(nested.Call);
            Mark(direct);
            EmitSocketExternalTail(pair.Call);
            Mark(noTuple);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            EmitSocketExternalTail(plain.Call);
            Mark(dynamic);
            EmitDynamicSocketTail(dynamicCall, member);

            return true;
        }

        private (Atom Transport, Pattern.Variable Socket, Expr.Call Call)? ReadStatsTail(Clause clause, int depth)
        {
            if (clause is not { Patterns: [Pattern.Literal { Value: Atom transport }, var pattern, Pattern.Variable options],
                Body: Expr.Call { Module: not null, DynamicRemote: false,
                    Arguments: [Expr.Variable socketArg, Expr.Variable optionArg] } call }
                || optionArg.Name != options.Name || owner.ResolvesErlangBif(call))
            {
                return null;
            }

            for (int level = 0; level < depth; level++)
            {
                if (pattern is not Pattern.Tuple { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, var tail] })
                {
                    return null;
                }

                pattern = tail;
            }

            return pattern is Pattern.Variable socket && socket.Name == socketArg.Name && DistinctBindings(socket, options)
                ? (transport, socket, call) : null;
        }

    }
}
