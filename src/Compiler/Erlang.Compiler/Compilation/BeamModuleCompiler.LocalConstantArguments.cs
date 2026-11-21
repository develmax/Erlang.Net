namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly Dictionary<(string Name, int Arity), Term?[]> localSingletonArguments = [];

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

        var localEntries = functions.Select(function => (function.Name, function.Arity)).ToHashSet();
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
                incoming.Add(signature, call.Arguments.Select(ReadEntrySingleton).ToArray());
                continue;
            }

            for (int index = 0; index < constants.Length; index++)
            {
                if (ReadEntrySingleton(call.Arguments[index]) is not { } value || !value.Equals(constants[index]))
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
            if (SsaTypeInferenceEnabled && arguments.Any(argument => argument is Atom)
                && ReadSingleNestedListMapper(clause.Body) is not null)
            {
                localSingletonArguments.Add(signature, arguments);
            }

            string[] names = parameters[signature];
            bool reorderedRelay = IsReorderedRemoteRelay(clause.Body, names);
            // Like beam_ssa_type's singleton substitution, this keeps the local entry and call arity.
            Expr result = clause.Body is Expr.Sequence { Items.Count: > 0 } sequence
                && sequence.Items.SkipLast(1).All(item => item is Expr.Literal)
                ? sequence.Items[^1] : clause.Body;
            bool constructor = SsaTypeInferenceEnabled && IsLocalSingletonConstructorBody(result, names);
            Expr.Call? firstCall = SsaTypeInferenceEnabled ? ReadFirstPlainCall(clause.Body, localEntries) : null;
            Expr.Call? entryCall = firstCall is { Module: null }
                && localEntries.Contains((firstCall.Function, firstCall.Arguments.Count)) ? firstCall : null;
            var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
            for (int index = 0; index < arguments.Length; index++)
            {
                // A direct entry argument already occupies the matching register at the first local call.
                bool forwarded = entryCall is not null && index < entryCall.Arguments.Count
                    && entryCall.Arguments[index] is Expr.Variable read && read.Name == names[index];
                if (arguments[index] is { } value && names[index] != VariableScopeNames.Wildcard
                    && (firstCall is not null && !forwarded
                        && (value is not Atom || !IsSoleForwardedFirstAtom(
                            firstCall,
                            clause.Body,
                            index,
                            names[index]
                        ))
                        || constructor && value is not Integer
                        || value is Atom && (reorderedRelay || IsParameterLiveAcrossEntryCall(clause.Body, names[index]))))
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

        Term? ReadEntrySingleton(Expr expression) => ReadLocalSingletonArgument(expression)
            ?? (SsaTypeInferenceEnabled && expression is Expr.Literal { Value: Integer integer } ? integer : null);
    }

    private static Expr.ListComprehension? ReadSingleNestedListMapper(Expr body)
    {
        Expr mapper = body is Expr.Call { DynamicRemote: false, Arguments: [var input] } ? input : body;

        return mapper is Expr.ListComprehension
        {
            Items: [Expr.Tuple],
            Qualifiers:
            [
                ComprehensionQualifier.Generator { Strict: false, Pattern: Pattern.Tuple, Source: Expr.Variable },
                ComprehensionQualifier.Generator { Strict: false, Pattern: Pattern.Variable }
            ]
        } comprehension ? comprehension : null;
    }

    private static bool IsSoleForwardedFirstAtom(
        Expr.Call call,
        Expr body,
        int index,
        string parameter
    ) =>
        IsRegisterClobberingCall(call) && index < call.Arguments.Count
            && call.Arguments[index] is Expr.Variable read && read.Name == parameter
            && LocalFunctionEntries.Walk(body).OfType<Expr.Variable>().Count(variable => variable.Name == parameter) == 1;

    private static Expr.Call? ReadFirstPlainCall(Expr body, IReadOnlySet<(string Name, int Arity)> localEntries)
    {
        if (body is not (Expr.Sequence { Items.Count: > 1 } or Expr.List or Expr.Tuple)
            && !(body is Expr.Call nested && nested.Arguments.Any(argument =>
                argument is Expr.Call && ReadExternalFunctionLiteral(argument) is null)))
        {
            return null;
        }

        Expr.Call? first = null;
        bool ordinaryCall = false;

        return Visit(body) && ordinaryCall ? first : null;

        bool Visit(Expr expression)
        {
            if (expression is not (Expr.Call { DynamicRemote: false } or Expr.Variable or Expr.Literal
                or Expr.Tuple or Expr.List or Expr.Sequence or Expr.Match { Pattern: Pattern.Variable }))
            {
                return false;
            }

            if (!LocalFunctionEntries.Children(expression).All(Visit))
            {
                return false;
            }

            // A static external function reference lowers to a literal without an intervening call.
            if (expression is Expr.Call call && ReadExternalFunctionLiteral(call) is null)
            {
                first ??= call;
                ordinaryCall |= IsRegisterClobberingCall(call)
                    || call.Module is null && localEntries.Contains((call.Function, call.Arguments.Count));
            }

            return true;
        }
    }

    private sealed partial class GeneralEmitter
    {
        private Term? ReadClosedSingletonArgument(FunctionDefinition function, int register)
        {
            if (owner.OnLoadFunction is not null || owner.IsDeclaredNative((function.Name, function.Arity)))
            {
                return null;
            }

            Term? value = null;

            return HasOnlyLocalCalls(
                function,
                function.Arity,
                (_, call) =>
            {
                if (ReadLocalSingletonArgument(call.Arguments[register]) is not { } argument
                    || value is not null && !value.Equals(argument))
                {
                    return false;
                }

                value = argument;

                return true;
            }
            ) ? value : null;
        }
    }

    private static Term? ReadLocalSingletonArgument(Expr argument) => argument switch
    {
        Expr.Literal { Value: Atom or Nil } literal => literal.Value,
        Expr.List { Items.Count: 0, Tail: null } => Nil.Value,
        _ => null
    };

    private static bool IsLocalSingletonConstructorBody(Expr expression, IReadOnlyList<string> parameters)
    {
        if (expression is not Expr.Sequence { Items.Count: > 0 } sequence)
        {
            return expression is (Expr.Tuple or Expr.List) && IsLocalSingletonConstructor(expression);
        }

        var names = new HashSet<string>(parameters, StringComparer.Ordinal);
        foreach (var item in sequence.Items.SkipLast(1))
        {
            if (item is Expr.Literal)
            {
                continue;
            }

            if (item is not Expr.Match
                {
                    Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } variable,
                    Value: var value
                } || !IsLocalSingletonConstructor(value) || !names.Add(variable.Name))
            {
                return false;
            }
        }

        Expr result = sequence.Items[^1];

        return result is (Expr.Tuple or Expr.List) && IsLocalSingletonConstructor(result);
    }

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
