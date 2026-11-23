using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        internal static bool MayPlanOrderedAtoms(FunctionDefinition function) =>
            function.Clauses.Count >= 2 && function.Clauses.All(clause => clause.Guard is null)
            && function.Clauses.Any(clause => clause.Patterns.Any(pattern => ReadAtomPattern(pattern).Key is not null))
            && function.Clauses.Any(clause => LocalFunctionEntries.Walk(clause.Body).Any(expression => expression is Expr.Call));

        internal bool TryEmitPlannedOrderedAtoms(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || recursiveClosure is not null || !MayPlanOrderedAtoms(function))
            {
                return false;
            }

            int selection = -1;
            var branches = new List<(VirtualTerminalCaseArm Arm, string[] Parameters)>();
            var keys = new HashSet<Atom>();
            (VirtualTerminalCaseArm Arm, string[] Parameters)? remaining = null;
            foreach (Clause clause in function.Clauses)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                var parameters = new string[function.Arity];
                Atom? key = null;
                string? alias = null;
                for (int argument = 0; argument < function.Arity; argument++)
                {
                    Pattern pattern = clause.Patterns[argument];
                    if (pattern is Pattern.Variable variable)
                    {
                        parameters[argument] = variable.Name;
                    }
                    else
                    {
                        if (key is not null || selection >= 0 && selection != argument
                            || ReadAtomPattern(pattern) is not ({ } literal, var binding))
                        {
                            return false;
                        }

                        key = literal;
                        alias = binding;
                        selection = argument;
                        parameters[argument] = alias ?? VariableScopeNames.Wildcard;
                    }

                    if (parameters[argument] != VariableScopeNames.Wildcard && !names.Add(parameters[argument]))
                    {
                        return false;
                    }
                }

                if (remaining is not null || key is not null && !keys.Add(key)
                    || LowerTerminalCaseArm(
                        key,
                        clause.Body,
                        parameters,
                        alias
                    ) is not { } arm)
                {
                    return false;
                }

                if (key is null)
                {
                    remaining = (arm, parameters);
                }
                else
                {
                    branches.Add((arm, parameters));
                }
            }

            if (branches.Count == 0 || branches.Count == 1 && remaining is null)
            {
                return false;
            }

            if (selection == 0 && remaining is not null && branches.Count == 2 && TryEmitPathOptionList(function))
            {
                return true;
            }

            bool closedBoolean = remaining is null && branches.Count == 2
                && branches.All(branch => branch.Arm.Key is { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False })
                && (ReadClosedBooleanParameter(function, selection)
                    || owner.ReadStructuralEntryAtoms((function.Name, function.Arity), selection) is { Count: > 0 } values
                    && values.All(value => value.Name is ErlangBooleanAtoms.True or ErlangBooleanAtoms.False));

            bool closedPair = closedBoolean || remaining is null && branches.Count == 2
                && ReadFiniteAtomArguments(function, selection, admitGuardFacts: true) is { } pair
                && pair.SetEquals(keys);

            bool exhaustive = remaining is null && branches.Count >= 3
                && ReadFiniteAtomArguments(function, selection, requireInferredAtomType: false) is { } atoms && atoms.SetEquals(keys);

            return EmitPlannedAtomBranches(
                selection,
                branches,
                remaining,
                out _,
                closedPair: closedPair,
                exhaustive: exhaustive
            );
        }

        private static (Atom? Key, string? Alias) ReadAtomPattern(Pattern pattern) => pattern switch
        {
            Pattern.Literal { Value: Atom atom } => (atom, null),
            AliasPattern { Left: Pattern.Literal { Value: Atom atom }, Right: Pattern.Variable alias } =>
                (atom, alias.Name),
            AliasPattern { Left: Pattern.Variable alias, Right: Pattern.Literal { Value: Atom atom } } =>
                (atom, alias.Name),
            _ => default
        };
    }
}
