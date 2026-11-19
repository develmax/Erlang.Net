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
        var incoming = new Dictionary<(string Name, int Arity), Term?[]>();
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
                incoming.Add(signature, call.Arguments.Select(ReadLocalSingletonArgument).ToArray());
                continue;
            }

            for (int index = 0; index < constants.Length; index++)
            {
                if (ReadLocalSingletonArgument(call.Arguments[index]) is not { } value || !value.Equals(constants[index]))
                {
                    constants[index] = null;
                }
            }
        }

        return functions.Select(function =>
        {
            var signature = (function.Name, function.Arity);
            if (escaped.Contains(signature) || !incoming.TryGetValue(signature, out var arguments))
            {
                return function;
            }

            Clause clause = function.Clauses[0];
            string[] names = parameters[signature];
            bool reorderedRelay = IsReorderedRemoteRelay(clause.Body, names);
            // Like beam_ssa_type's singleton substitution, this keeps the local entry and call arity.
            bool constructor = SsaTypeInferenceEnabled && clause.Body is (Expr.Tuple or Expr.List)
                && IsLocalSingletonConstructor(clause.Body);
            var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
            for (int index = 0; index < arguments.Length; index++)
            {
                if (arguments[index] is { } value && names[index] != VariableScopeNames.Wildcard
                    && (constructor || (value is Atom && (reorderedRelay || IsParameterLiveAcrossEntryCall(clause.Body, names[index])))))
                {
                    constants.Add(names[index], value);
                }
            }

            if (constants.Count == 0)
            {
                return function;
            }

            if (reorderedRelay)
            {
                return function with { Clauses = [clause with { Body = RewriteForwardedAtoms(clause.Body) }] };
            }

            Expr body = clause.Body is Expr.Case decision
                ? decision with
                {
                    Value = RewriteForwardedAtoms(decision.Value),
                    Clauses = decision.Clauses.Select(branch => branch with { Body = RewriteBody(branch.Body) }).ToArray()
                }
                : RewriteBody(clause.Body);

            return function with { Clauses = [clause with { Body = body }] };

            Expr RewriteForwardedAtoms(Expr value)
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
                            && constants.TryGetValue(read.Name, out Term? known)
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

    private static Term? ReadLocalSingletonArgument(Expr argument) => argument switch
    {
        Expr.Literal { Value: Atom or Nil } literal => literal.Value,
        Expr.List { Items.Count: 0, Tail: null } => Nil.Value,
        _ => null
    };

    private static bool IsLocalSingletonConstructor(Expr expression) => expression switch
    {
        Expr.Literal or Expr.Variable => true,
        Expr.Tuple tuple => tuple.Items.All(IsLocalSingletonConstructor),
        Expr.List list => list.Items.All(IsLocalSingletonConstructor)
            && (list.Tail is null || IsLocalSingletonConstructor(list.Tail)),
        Expr.Binary { Operator: ErlangOperators.Append, Left: Expr.List { Tail: null } prefix, Right: var tail }
            => prefix.Items.All(IsLocalSingletonConstructor) && IsLocalSingletonConstructor(tail),
        _ => false
    };

    private static bool IsReorderedRemoteRelay(Expr body, string[] parameters)
    {
        if (body is not Expr.Call { Module: { }, DynamicRemote: false } call
            || call.Arguments.Count != parameters.Length || parameters.Length < 2
            || parameters.Contains(VariableScopeNames.Wildcard))
        {
            return false;
        }

        var available = new HashSet<string>(parameters, StringComparer.Ordinal);
        bool reordered = false;
        for (int index = 0; index < call.Arguments.Count; index++)
        {
            if (call.Arguments[index] is not Expr.Variable argument || !available.Remove(argument.Name))
            {
                return false;
            }

            reordered |= argument.Name != parameters[index];
        }

        return reordered;
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
