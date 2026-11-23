namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private IReadOnlyList<FunctionDefinition> PropagateLocalConstantArguments(IReadOnlyList<FunctionDefinition> functions)
    {
        if (ModuleOptimizationDisabled || OnLoadFunction is not null)
        {
            return functions;
        }

        var parameters = new Dictionary<(string Name, int Arity), string[]>();
        foreach (var function in functions)
        {
            var signature = (function.Name, function.Arity);
            if (function.Arity == 0 || function.IsInlineFailure || IsDeclaredNative(signature)
                || exportedFunctions.Contains(signature) || function.Clauses is not [{ Guard: null } clause]
                || clause.Patterns.Any(pattern => pattern is not Pattern.Variable))
            {
                continue;
            }

            string[] names = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            var bindings = names.Where(name => name != VariableScopeNames.Wildcard).ToArray();
            if (bindings.Length == bindings.Distinct(StringComparer.Ordinal).Count())
            {
                parameters.Add(signature, names);
            }
        }

        if (parameters.Count == 0)
        {
            return functions;
        }

        var escaped = new HashSet<(string Name, int Arity)>();
        var incoming = new Dictionary<(string Name, int Arity), Atom?[]>();
        foreach (Expr expression in functions.SelectMany(function =>
            LocalFunctionEntries.ClauseExpressions(function.Clauses)).SelectMany(LocalFunctionEntries.Walk))
        {
            if (expression is Expr.FunctionReference reference)
            {
                escaped.Add((reference.Name, reference.Arity));
                continue;
            }

            if (expression is not Expr.Call { Module: null, DynamicRemote: false } call)
            {
                continue;
            }

            var signature = (call.Function, call.Arguments.Count);
            if (!parameters.ContainsKey(signature))
            {
                continue;
            }

            if (!incoming.TryGetValue(signature, out var constants))
            {
                incoming.Add(signature, call.Arguments.Select(argument => (argument as Expr.Literal)?.Value as Atom).ToArray());
                continue;
            }

            for (int index = 0; index < constants.Length; index++)
            {
                if (call.Arguments[index] is not Expr.Literal { Value: Atom atom } || !atom.Equals(constants[index]))
                {
                    constants[index] = null;
                }
            }
        }

        return functions.Select(function =>
        {
            var signature = (function.Name, function.Arity);
            if (escaped.Contains(signature) || !incoming.TryGetValue(signature, out var atoms))
            {
                return function;
            }

            Clause clause = function.Clauses[0];
            string[] names = parameters[signature];
            var constants = new Dictionary<string, Atom>(StringComparer.Ordinal);
            for (int index = 0; index < atoms.Length; index++)
            {
                if (atoms[index] is { } atom && names[index] != VariableScopeNames.Wildcard
                    && IsParameterLiveAcrossEntryCall(clause.Body, names[index]))
                {
                    constants.Add(names[index], atom);
                }
            }

            if (constants.Count == 0)
            {
                return function;
            }

            Expr body = clause.Body is Expr.Case decision
                ? decision with
                {
                    Value = RewriteSelector(decision.Value),
                    Clauses = decision.Clauses.Select(branch => branch with { Body = RewriteBody(branch.Body) }).ToArray()
                }
                : RewriteBody(clause.Body);

            return function with { Clauses = [clause with { Body = body }] };

            Expr RewriteSelector(Expr value)
            {
                if (value is not Expr.Call { DynamicRemote: false } selector)
                {
                    return value;
                }

                // A forwarded operand already occupies its outgoing X register.
                return selector with
                {
                    Arguments = selector.Arguments.Select((argument, index) =>
                        argument is Expr.Variable read && (index >= names.Length || names[index] != read.Name)
                            && constants.TryGetValue(read.Name, out Atom? known)
                            ? new Expr.Literal(known)
                            : argument).ToArray()
                };
            }

            // Closures and comprehensions can introduce a binding with the same name.
            Expr RewriteBody(Expr expression) => BeamTargetedInlining.Rewrite(
                expression,
                item => item is Expr.Variable variable && constants.TryGetValue(variable.Name, out var value)
                    ? new Expr.Literal(value)
                    : item,
                rewriteScope: item => item is Expr.Fun or Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension
                    ? item
                    : null
            );
        }).ToArray();
    }

    private static bool IsParameterLiveAcrossEntryCall(Expr body, string parameter)
    {
        // Call arguments are consumed together after their nested calls have returned.
        if (body is Expr.Call call)
        {
            return call.Arguments.Any(ReadsParameter)
                && call.Arguments.OfType<Expr.Call>().Any(IsRegisterClobberingCall);
        }

        // A selector call runs before any continuation reads the parameters.
        return body is Expr.Case { Value: Expr.Call selector } decision
            && IsRegisterClobberingCall(selector)
            && decision.Clauses.All(branch => branch.Guard is null
                && LocalFunctionEntries.Walk(branch.Body).All(expression =>
                    expression is not (Expr.Case or Expr.If or Expr.Receive or Expr.Try or Expr.Catch or Expr.Maybe
                        or Expr.Fun or Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension)))
            && decision.Clauses.Any(branch => LocalFunctionEntries.Walk(branch.Body).Any(ReadsParameter));

        bool ReadsParameter(Expr expression) => expression is Expr.Variable variable && variable.Name == parameter;
    }

    private static bool IsRegisterClobberingCall(Expr.Call call) =>
        call is { DynamicRemote: false, Module: { } module }
        && (module != ErlangModuleNames.Module
            || call.Function != AutoImportNames.Get
                && !Semantics.GuardBifs.Contains((call.Function, call.Arguments.Count))
                && !BeamTrimBifs.IsPure(module, call.Function, call.Arguments.Count)
                && !BeamTrimBifs.IsExit(module, call.Function, call.Arguments.Count));
}
