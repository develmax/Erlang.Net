namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitAdjacentCallCase(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence
                {
                    Items: [..,
                        Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } result,
                        Value: Expr.Call producer
                    },
                        Expr.Case { Value: Expr.Call selector } decision
                    ]
                } sequence
                || sequence.Items.SkipLast(2).Any(item => !AttributeLiteral.TryNormalizeConstant(item, out _)))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(result.Name, StringComparer.Ordinal)
                || ReadEntryCall(producer, parameters) is not { } first
                || ReadUsedEntryParameters(new Clause(
                    [.. clause.Patterns, result],
                    null,
                    decision with { Value = new Expr.Literal(Nil.Value) }
                )) is not { Count: 0 })
            {
                return false;
            }

            var reads = selector.Arguments.SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Variable>()
                .Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
            int[] inputs = Enumerable.Range(0, parameters.Length).Reverse()
                .Where(register => reads.Contains(parameters[register])).ToArray();
            var saved = inputs.Select((register, slot) => (Name: parameters[register], Slot: slot))
                .ToDictionary(input => input.Name, input => input.Slot, StringComparer.Ordinal);
            if (saved.Count == 0 || ReadStraightConstructedCall(selector, result.Name, saved) is not
                {
                    RegisterConstructions:
                    {
                        Live: 1, Loads.Count: 0, Moves: [( > 0, 0)],
                        Constructions.Count: > 0, Constants: [{ Destination: 0, Value: var leading }]
                    } arguments
                } next)
            {
                return false;
            }

            int live = Math.Max(EntryCallLiveCount(first, parameters), inputs.Max() + 1);
            EmitFrameAllocation(live, frameSize);
            EmitFrameInitialization(inputs.Length, frameSize);
            for (int index = 0; index < inputs.Length; index++)
            {
                owner.MoveRegister(
                    BeamEmissionFormat.XRegisterTag,
                    inputs[index],
                    BeamEmissionFormat.YRegisterTag,
                    Slot()
                );
            }

            EmitEntryCall(first);
            EmitCallArgumentConstructions(arguments with { Constants = [] }, -1);
            // The constructors and the X0 result move have consumed the last
            // reads. Set up the new X0 next to the call after clearing those roots.
            owner.EmitSlotKills(Enumerable.Range(0, inputs.Length).ToArray());
            owner.EmitConstant(leading, 0);
            owner.EmitCallLine(next.Call, CallLocation(first.Call, location));
            Call(next.Module, next.Call.Function, next.Call.Arguments.Count);

            nextSlot = 0;
            if (!TryEmitTerminalTaggedCase(decision, requireNestedPayload: true))
            {
                EmitCase(
                    decision,
                    null,
                    inputPrepared: true,
                    reuseBranchSlots: true,
                    terminalCase: true
                );
                owner.EmitFrameReturn(frameSize);
            }

            return true;
        }
    }
}
