using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitConstructedCallContinuation(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                        {
                            Pattern: Erlang.Pattern.Variable binding,
                            Value: Expr.Tuple or Expr.List
                        } assignment,
                        Expr.Call tail
                    ]
                }
                || binding.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(binding.Name, StringComparer.Ordinal)
                || FlattenCallArguments([], new Expr.Tuple([assignment.Value])) is not
                    [Expr.Match { Pattern: Erlang.Pattern.Variable result, Value: Expr.Call producer },
                        Expr.Tuple { Items: [var constructor] }]
                || ReadEntryCall(producer, parameters) is not { } first
                || !TryResolveOrdinaryCallTarget(tail, out string? module)
                || ReadUsedEntryParameters(clause with
                {
                    Body = new Expr.Tuple([constructor, .. tail.Arguments])
                }) is not { } used)
            {
                return false;
            }

            int[] saved = Enumerable.Range(0, parameters.Length).Reverse()
                .Where(register => used.Contains(parameters[register])).ToArray();
            var slots = saved.Select((register, slot) => (Name: parameters[register], Slot: slot))
                .ToDictionary(input => input.Name, input => input.Slot, StringComparer.Ordinal);
            var inputs = new Dictionary<string, int>(StringComparer.Ordinal) { [result.Name] = 0 };
            if (ReadCallArgumentConstructions(
                tail.Arguments,
                -1,
                slots,
                inputs: inputs,
                bindings: [(binding.Name, constructor)]
            ) is not { } plan)
            {
                return false;
            }

            int live = Math.Max(EntryCallLiveCount(first, parameters), saved.DefaultIfEmpty(-1).Max() + 1);
            EmitFrameAllocation(live, saved.Length);
            for (int slot = 0; slot < saved.Length; slot++)
            {
                owner.MoveRegister(XRegisterTag, saved[slot], YRegisterTag, slot);
            }

            EmitEntryCall(first);
            EmitCallArgumentConstructions(plan, -1);
            EmitPreparedFrameTail(new EntryCall(tail, module, [], []), CallLocation(producer, location), saved.Length);

            return true;
        }
    }
}
