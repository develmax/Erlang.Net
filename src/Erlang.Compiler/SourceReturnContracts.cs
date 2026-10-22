using System.Runtime.CompilerServices;

namespace Erlang.Compiler;

internal static class SourceReturnContracts
{
    private static readonly ConditionalWeakTable<ModuleDefinition, HashSet<(string Name, int Arity)>> Plans = new();

    internal static bool IsTerminal(ModuleDefinition module, ProcessContext context, FunctionTerm function)
    {
        if (!SourceFunctionTailDispatch.IsCompilerSource(function)
            || SourceReturnSafety.HasNativeOverrides(module, context) || function.LocalIdentity is not { } identity
            || identity.Module.Name != module.Name
            || LocalFunctionEntries.Resolve(module, identity, function.Arity) is not { } resolved)
        {
            return false;
        }
        var plan = Plans.GetValue(module, Build);

        return resolved.Named is { } named ? plan.Contains((named, function.Arity))
            : resolved.Anonymous is { } anonymous && Clauses(
                module,
                anonymous.Clauses,
                plan,
                []
            );
    }

    private static HashSet<(string Name, int Arity)> Build(ModuleDefinition module)
    {
        var candidates = module.Functions.Where(value => SourceReturnSafety.Allows(module, value.Name, value.Arity))
            .Select(value => (value.Name, value.Arity)).ToHashSet();
        for (int pass = 0; pass < ComprehensionReturnParameters.MaximumFixedPointPasses; pass++)
        {
            var rejected = module.Functions.Where(value => candidates.Contains((value.Name, value.Arity))
                && !Clauses(
                    module,
                    value.Clauses,
                    candidates,
                    []
                )).Select(value => (value.Name, value.Arity)).ToArray();
            if (rejected.Length == 0)
            {
                return candidates;
            }
            candidates.ExceptWith(rejected);
        }

        return [];
    }

    private static bool Clauses(
        ModuleDefinition module,
        IReadOnlyList<Clause> clauses,
        HashSet<(string Name, int Arity)> candidates,
        Dictionary<string, Expr> incoming
    )
    {
        if (clauses.Count == 0)
        {
            return false;
        }
        foreach (var clause in clauses)
        {
            var local = new Dictionary<string, Expr>(incoming, StringComparer.Ordinal);
            foreach (string name in clause.Patterns.SelectMany(Semantics.Variables))
            {
                local.Remove(name);
            }
            if (!Terminal(
                module,
                clause.Body,
                candidates,
                local
            ))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Terminal(
        ModuleDefinition module,
        Expr expression,
        HashSet<(string Name, int Arity)> candidates,
        Dictionary<string, Expr> bindings
    )
    {
        switch (expression)
        {
            case Expr.Call call when call.Module == ErlangModuleNames.Module:
                {
                    return call.Function == ErlangModuleNames.Error && call.Arguments.Count is >= 1 and <= 3
                        || call.Function is ErlangModuleNames.Throw or ErlangModuleNames.Exit && call.Arguments.Count == 1;
                }
            case Expr.Call { Module: null } call:
                {
                    return call.Arguments.All(SourceReturnPurity.IsPure) && candidates.Contains((call.Function, call.Arguments.Count));
                }
            case Expr.Apply apply:
                {
                    if (!apply.Arguments.All(SourceReturnPurity.IsPure))
                    {
                        return false;
                    }
                    Expr target = apply.Function;
                    if (target is Expr.Variable variable)
                    {
                        if (!bindings.TryGetValue(variable.Name, out var bound))
                        {
                            return false;
                        }
                        target = bound;
                    }

                    return target is Expr.FunctionReference reference && candidates.Contains((reference.Name, reference.Arity))
                        || target is Expr.Fun function && Clauses(
                            module,
                            function.Clauses,
                            candidates,
                            bindings
                        );
                }
            case Expr.Sequence sequence:
                {
                    foreach (var item in sequence.Items)
                    {
                        if (Terminal(
                            module,
                            item,
                            candidates,
                            bindings
                        ))
                        {
                            return true;
                        }
                        if (!SourceReturnPurity.IsPure(item))
                        {
                            return false;
                        }
                    }

                    return false;
                }
            case Expr.Match match:
                {
                    if (Terminal(
                        module,
                        match.Value,
                        candidates,
                        bindings
                    ))
                    {
                        return true;
                    }
                    if (match.Pattern is Pattern.Variable variable)
                    {
                        Expr value = match.Value;
                        if (value is Expr.Variable source && bindings.TryGetValue(source.Name, out var previous))
                        {
                            value = previous;
                        }
                        bindings[variable.Name] = value;
                    }

                    return false;
                }
            case Expr.Block block:
                {
                    return Terminal(
                        module,
                        block.Body,
                        candidates,
                        bindings
                    );
                }
            case Expr.Case conditional:
                {
                    return SourceReturnPurity.IsPure(conditional.Value) && Clauses(
                        module,
                        conditional.Clauses,
                        candidates,
                        bindings
                    );
                }
            case Expr.If conditional:
                {
                    return Clauses(
                        module,
                        conditional.Clauses,
                        candidates,
                        bindings
                    );
                }
            default:
                {
                    return false;
                }
        }
    }
}
