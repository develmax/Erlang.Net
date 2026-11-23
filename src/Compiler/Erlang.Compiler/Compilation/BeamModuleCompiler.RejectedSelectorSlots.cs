using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private Expr ReuseRejectedSelectorSlot(Expr selector, Expr accepted, Expr rejected)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || owner.receiveScratchRegisters is not null
                || selector is not Expr.Call { DynamicRemote: false, Arguments: [Expr.Variable previous] } probe
                || accepted is not Expr.Variable returned || previous.Name != returned.Name
                || !variables.TryGetValue(previous.Name, out int slot)
                || deferredPairMembers.Values.Any(member => member.Slot == slot)
                || rejected is not Expr.Sequence
                {
                    Items: [
                    Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } replacement,
                        Value: Expr.Call { DynamicRemote: false, Arguments: [Expr.Call inner, ..] } outer
                    },
                    Expr.Case { Value: Expr.Call { DynamicRemote: false, Arguments: [Expr.Variable test] } next } decision
                ]
                }
                || variables.ContainsKey(replacement.Name) || deferredPairMembers.ContainsKey(replacement.Name)
                || replacement.Name != test.Name || probe.Module != next.Module || probe.Function != next.Function
                || !IsBooleanCase(decision) || !ReturnsProbedValue(decision, replacement.Name)
                || !TryResolveOrdinaryCallTarget(inner, out string? innerModule)
                || !TryResolveOrdinaryCallTarget(outer, out string? outerModule)
                || ReadGroundArguments(inner.Arguments) is not { } first
                || ReadGroundArguments(outer.Arguments.Skip(1)) is not { } second)
            {
                return rejected;
            }

            string[] aliases = variables.Where(binding => binding.Value == slot).Select(binding => binding.Key).ToArray();
            Pattern[] bindings = aliases.Select(alias => (Pattern)new Pattern.Variable(alias)).ToArray();
            if (ReadUsedEntryParameters(new Clause(bindings, null, rejected)) is not { Count: 0 })
            {
                return rejected;
            }

            // The rejected value does not reach either the new producer or its
            // decision. Release its root while preparing the first call.
            for (int index = first.Length - 1; index > 0; index--)
            {
                owner.EmitConstant(first[index], index);
            }

            owner.EmitSlotKills([slot]);
            if (first.Length > 0)
            {
                owner.EmitConstant(first[0], 0);
            }

            owner.EmitCallLine(inner, location);
            Call(innerModule, inner.Function, first.Length);
            for (int index = second.Length - 1; index >= 0; index--)
            {
                owner.EmitConstant(second[index], index + 1);
            }

            owner.EmitCallLine(outer, CallLocation(inner, location));
            Call(outerModule, outer.Function, second.Length + 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                slot
            );
            foreach (string alias in aliases)
            {
                variables.Remove(alias);
            }

            variables.Add(replacement.Name, slot);

            return decision;
        }

        private static bool ReturnsProbedValue(Expr.Case decision, string name) => decision.Clauses.Any(branch => branch is
        {
            Patterns: [Pattern.Literal { Value: Atom { Name: "true" } }],
            Body: Expr.Variable returned
        } && returned.Name == name);

        private static Term[]? ReadGroundArguments(IEnumerable<Expr> arguments)
        {
            var values = new List<Term>();
            foreach (Expr argument in arguments)
            {
                if (!AttributeLiteral.TryNormalizeConstant(argument, out Term value))
                {
                    return null;
                }

                values.Add(value);
            }

            return values.ToArray();
        }
    }
}
