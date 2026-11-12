using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEntryDynamicApplyTail(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence
                {
                    Items: [
                        Expr.Match { Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding, Value: Expr.Call prefix },
                        Expr.Call tail
                    ]
                })
            {
                return false;
            }

            var apply = ReadFixedApply(tail);
            if (apply is null && !(IsApplyBif(tail) && tail.Arguments[2] is Expr.Variable))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(binding.Name, StringComparer.Ordinal)
                || ReadEntryCall(prefix, parameters) is not { } first)
            {
                return false;
            }

            IReadOnlyList<Expr> operands = apply is { } dynamic
                ? [.. dynamic.Arguments, dynamic.Module, dynamic.Function] : tail.Arguments;
            var registerMoves = new List<(int Destination, int Source)>();
            var stackMoves = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            for (int destination = 0; destination < operands.Count; destination++)
            {
                switch (operands[destination])
                {
                    case Expr.Variable variable when variable.Name == binding.Name:
                        if (destination != 0)
                        {
                            registerMoves.Add((destination, 0));
                        }
                        break;
                    case Expr.Variable { Name: not VariableScopeNames.Wildcard } variable
                        when Array.IndexOf(parameters, variable.Name) is >= 0 and var source:
                        stackMoves.Add((destination, source));
                        break;
                    default:
                        if (!AttributeLiteral.TryNormalizeConstant(operands[destination], out Term constant))
                        {
                            return false;
                        }

                        constants.Add((destination, constant));
                        break;
                }
            }

            int[] saved = stackMoves.Select(move => move.Source).Distinct().OrderDescending().ToArray();
            int live = Math.Max(EntryCallLiveCount(first, parameters), saved.DefaultIfEmpty(-1).Max() + 1);
            EmitFrameAllocation(live, saved.Length);
            for (int slot = 0; slot < saved.Length; slot++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    saved[slot],
                    YRegisterTag,
                    slot
                );
            }

            EmitEntryCall(first);
            owner.EmitOrderedTailMoves(
                registerMoves,
                constants,
                stackMoves.Select(move => (move.Destination, Array.IndexOf(saved, move.Source))).ToList()
            );
            var previous = CallLocation(prefix, location);
            if (apply is { } fixedApply)
            {
                owner.EmitCallLine(tail, previous);
                owner.Op(TailDynamicApply, BeamWire.UnsignedTag, fixedApply.Arguments.Count);
                owner.Operand(BeamWire.UnsignedTag, saved.Length);
            }
            else
            {
                EmitPreparedFrameTail(new EntryCall(
                    tail,
                    ErlangModuleNames.Module,
                    registerMoves,
                    constants
                ), previous, saved.Length);
            }

            return true;
        }

        private (Expr Module, Expr Function, IReadOnlyList<Expr> Arguments)? ReadFixedApply(Expr.Call call)
        {
            if (call.Arguments is not [
                var module,
                var function,
                Expr.List { Tail: null } arguments
            ] || module is not (Expr.Variable or Expr.Literal { Value: Atom })
                || function is not (Expr.Variable or Expr.Literal { Value: Atom })
                || arguments.Items.Count + 2 > FirstReservedXRegister
                || module is Expr.Literal && function is Expr.Literal)
            {
                return null;
            }

            if (!call.DynamicRemote && !IsApplyBif(call))
            {
                return null;
            }

            return (module, function, arguments.Items);
        }

        private bool IsApplyBif(Expr.Call call) =>
            !call.DynamicRemote && call.Function == AutoImportNames.Apply && call.Arguments.Count == 3
            && (call.Module == ErlangModuleNames.Module
                || call.Module is null && !entries.ContainsKey((call.Function, call.Arguments.Count))
                    && owner.ResolveExternalModule(call.Function, call.Arguments.Count) == ErlangModuleNames.Module);
    }
}
