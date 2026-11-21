using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitConstructedEntryEffect(Clause clause, string[] parameters)
        {
            if (!HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence { Items: [Expr.Call effect, Expr.Tuple returned] }
                || !TryResolveOrdinaryCallTarget(effect, out string? module))
            {
                return false;
            }

            var empty = new Dictionary<string, int>(StringComparer.Ordinal);
            var inputs = parameters.Select((name, register) => (name, register))
                .Where(input => input.name != VariableScopeNames.Wildcard)
                .ToDictionary(input => input.name, input => input.register, StringComparer.Ordinal);
            if (ReadUsedEntryParameters(clause with { Body = returned }) is not { } retained
                || ReadCallArgumentConstructions(
                    effect.Arguments,
                    -1,
                    empty,
                    inputs: inputs
                ) is not { } arguments)
            {
                return false;
            }

            // Only values read by the continuation need stack roots across the call.
            var roots = inputs.Where(input => retained.Contains(input.Key)).OrderByDescending(input => input.Value)
                .Select((input, slot) => (Name: input.Key, Register: input.Value, Slot: slot)).ToArray();
            var saved = roots.ToDictionary(root => root.Name, root => root.Slot, StringComparer.Ordinal);
            if (ReadCallArgumentConstructions(
                [returned],
                -1,
                saved,
                inputs: empty
            ) is not { } continuation)
            {
                return false;
            }

            int live = Math.Max(arguments.Live, roots.Select(root => root.Register + 1).DefaultIfEmpty(0).Max());
            int heap = arguments.Constructions.Sum(instruction => instruction.HeapWords);
            EmitFrameAllocation(live, roots.Length, heap);
            foreach (var root in roots)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    root.Register,
                    YRegisterTag,
                    root.Slot
                );
            }

            EmitCallArgumentConstructions(arguments, -1, heapChecked: true);
            owner.EmitCallLine(effect, location);
            Call(module, effect.Function, effect.Arguments.Count);
            EmitCallArgumentConstructions(continuation, -1);
            owner.EmitFrameReturn(roots.Length);

            return true;
        }
    }
}
