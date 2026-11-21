using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCaughtFunctionContainer(Expr.Case decision, Expr.Call call, string[] parameters)
        {
            if (!owner.SsaTypeInferenceEnabled)
            {
                return false;
            }

            for (int register = 0; register < call.Arguments.Count; register++)
            {
                Expr argument = call.Arguments[register];
                if (argument is not (Expr.Tuple or Expr.List)
                    || ReadFunctionContainer(argument) is not { } container
                    || LocalFunctionEntries.Walk(argument).OfType<Expr.FunctionReference>().ToArray() is not [var reference]
                    || !entries.TryGetValue((reference.Name, reference.Arity), out int target)
                    || ReadEntryCall(call, parameters, preparedOperand: register) is not { Moves.Count: 0 } prepared)
                {
                    continue;
                }

                // The closure and its constant container cannot fail. Schedule both
                // into the outgoing register before the handler, leaving other inputs in place.
                Clause[] arms = decision.Clauses.Select(PrepareCaughtFunctionContainerArm).ToArray();
                if (ReadRegisterCase(
                    decision with { Clauses = arms },
                    new Dictionary<string, int>(StringComparer.Ordinal),
                    allowUnmatchedLiterals: true,
                    callSequenceSlots: 1
                ) is not { } result)
                {
                    return false;
                }

                int handler = Label();
                owner.Op(AllocateHeap, BeamWire.UnsignedTag, 1);
                owner.EmitClosureHeap(container.Words, EntryCallLiveCount(prepared, parameters), 1);
                owner.EmitDeclaredFunctionReference(reference, target, register);
                owner.EmitFunctionContainer(container.Steps, register);
                EmitEntryCallArguments(prepared);
                EnterCatch(0, handler);
                owner.EmitCallLine(call, location);
                Call(prepared.Module, call.Function, call.Arguments.Count);
                EndCatch(0, handler);
                EmitRegisterCase(result, 1, CallLocation(call, location));

                return true;
            }

            return false;
        }

        private static Clause PrepareCaughtFunctionContainerArm(Clause arm)
        {
            // Reuse the linear call planner when an otherwise constant result
            // tuple wraps one call; evaluating those literals has no effect.
            if (arm.Body is not Expr.Tuple tuple
                || tuple.Items.Count(item => item is Expr.Call) != 1
                || tuple.Items.Any(item => item is not Expr.Call && !AttributeLiteral.TryNormalizeConstant(item, out _)))
            {
                return arm;
            }

            return arm with { Body = new Expr.Sequence(FlattenCallArguments([], result: tuple)) };
        }
    }
}
