using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRetainedCallResultConstruction(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not [{ Guard: null, Body: Expr.Sequence sequence } clause]
                || !HasUnconditionalParameters(clause) || sequence.Items.Count < 3
                || sequence.Items[0] is not Expr.Match
                {
                    Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } result,
                    Value: Expr.Call producer
                } || sequence.Items[^1] is not Expr.Tuple returned)
            {
                return false;
            }

            string[] parameters = clause.Patterns.OfType<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            var names = parameters.ToHashSet(StringComparer.Ordinal);
            if (!names.Add(result.Name) || ReadEntryCall(producer, parameters) is not { } call)
            {
                return false;
            }

            var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
            var constructions = new List<(string Name, Expr Value)>();
            foreach (Expr expression in sequence.Items.Skip(1).SkipLast(1))
            {
                if (expression is not Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding,
                        Value: var value
                    } || names.Contains(binding.Name))
                {
                    return false;
                }

                Expr prepared = SubstituteConstants(value, constants);
                if (AttributeLiteral.TryNormalizeConstant(prepared, out Term constant))
                {
                    constants.Add(binding.Name, constant);
                }
                else if (LocalFunctionEntries.Walk(prepared).All(item =>
                    item is Expr.Literal or Expr.Tuple or Expr.List
                    || item is Expr.Variable read && names.Contains(read.Name)))
                {
                    constructions.Add((binding.Name, prepared));
                }
                else
                {
                    return false;
                }

                names.Add(binding.Name);
            }

            var terminal = SubstituteConstants(returned, constants);
            var live = LocalFunctionEntries.Walk(terminal).OfType<Expr.Variable>()
                .Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
            if (!live.Contains(result.Name))
            {
                return false;
            }

            var statements = new List<(string Name, Expr Value)>();
            foreach (var statement in constructions.AsEnumerable().Reverse())
            {
                if (live.Contains(statement.Name))
                {
                    statements.Add(statement);
                    live.UnionWith(LocalFunctionEntries.Walk(statement.Value).OfType<Expr.Variable>().Select(variable => variable.Name));
                }
            }

            var retained = parameters.Select((name, register) => (name, register))
                .Where(parameter => parameter.name != VariableScopeNames.Wildcard && live.Contains(parameter.name))
                .Reverse().ToArray();
            if (statements.Count == 0 || retained.Length == 0)
            {
                return false;
            }

            var fields = retained.Select((parameter, slot) => (parameter.name, Slot: slot))
                .ToDictionary(parameter => parameter.name, parameter => parameter.Slot, StringComparer.Ordinal);
            var resultInput = new Dictionary<string, int>(StringComparer.Ordinal) { [result.Name] = 0 };
            statements.Reverse();
            if (ReadCallArgumentConstructions(
                [terminal],
                -1,
                fields,
                inputs: resultInput,
                bindings: statements
            ) is not { } plan)
            {
                return false;
            }

            // The producer overwrites X registers. Give the incoming arguments
            // their reverse Y slot order and keep the produced value in X0.
            int incoming = Math.Max(retained[0].register + 1, EntryCallLiveCount(call, parameters));
            EmitFrameAllocation(incoming, retained.Length);
            for (int slot = 0; slot < retained.Length; slot++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    retained[slot].register,
                    YRegisterTag,
                    slot
                );
            }

            EmitEntryCall(call);
            EmitCallArgumentConstructions(plan, -1);
            owner.EmitFrameReturn(retained.Length);

            return true;
        }
    }
}
