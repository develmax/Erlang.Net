// SPDX-License-Identifier: Apache-2.0
// Match specification encoding follows OTP-29.1.1 lib/stdlib/src/ms_transform.erl.

using System.Globalization;

namespace Erlang.Compiler;

internal static class MatchSpecSourceTransform
{
    private const string WholeObject = "$_";
    private const string Wildcard = "_";
    private const string Constant = "const";
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Expr.List, Expr.Call> originalCalls = new();

    internal static Expr.Call? OriginalCall(Expr.List replacement) =>
        originalCalls.TryGetValue(replacement, out var call) ? call : null;

    internal static bool IsOption(Term option) =>
        option is TupleTerm { Items: [Atom { Name: "parse_transform" }, Atom { Name: "ms_transform" }] };

    internal static FunctionDefinition Apply(FunctionDefinition function) => function with
    {
        Clauses = function.Clauses.Select(clause => clause with
        {
            Guard = clause.Guard is null ? null : Rewrite(clause.Guard),
            Body = Rewrite(clause.Body)
        }).ToArray()
    };

    private static Expr Rewrite(Expr expression) =>
        BeamTargetedInlining.Rewrite(expression, RewriteCall, rewriteComprehensions: true,
            rewriteScope: subtree => LocalFunctionEntries.Walk(subtree).Any(IsMatchSpecCall) ? null : subtree);

    private static bool IsMatchSpecCall(Expr expression) =>
        expression is Expr.Call { DynamicRemote: false, Module: "ets" or "dbg", Function: "fun2ms" };

    private static Expr RewriteCall(Expr expression)
    {
        if (expression is not Expr.Call { DynamicRemote: false, Module: "ets" or "dbg", Function: "fun2ms" } call)
        {
            return expression;
        }
        if (call.Module != "ets" || call.Arguments is not [Expr.Fun { SelfName: null } function])
        {
            throw Unsupported(call.SourceLine);
        }

        var replacement = new Expr.List(function.Clauses.SelectMany(clause => ConvertClause(clause, call.SourceLine)).ToArray());
        originalCalls.Add(replacement, call);
        return replacement;
    }

    private static IEnumerable<Expr> ConvertClause(Clause clause, int line)
    {
        if (clause.Patterns is not [var pattern])
        {
            throw Unsupported(line);
        }

        var bindings = new Dictionary<string, string>(StringComparer.Ordinal);
        if (pattern is AliasPattern alias)
        {
            var (variable, value) = alias.Left is Pattern.Variable
                ? (alias.Left, alias.Right) : (alias.Right, alias.Left);
            if (variable is not Pattern.Variable name)
            {
                throw Unsupported(line);
            }
            if (name.Name != Wildcard)
            {
                bindings.Add(name.Name, WholeObject);
            }
            pattern = value;
        }

        int next = 0;
        Expr Head(Pattern item) => item switch
        {
            Pattern.Variable { Name: Wildcard } => AtomValue(Wildcard),
            Pattern.Variable variable => AtomValue(Bind(variable.Name)),
            Pattern.Literal literal => new Expr.Literal(literal.Value),
            Pattern.Tuple tuple => new Expr.Tuple(tuple.Items.Select(Head).ToArray()),
            Pattern.List list => new Expr.List(list.Items.Select(Head).ToArray(), list.Tail is null ? null : Head(list.Tail)),
            _ => throw Unsupported(line)
        };

        string Bind(string name)
        {
            if (!bindings.TryGetValue(name, out string? binding))
            {
                binding = "$" + (++next).ToString(CultureInfo.InvariantCulture);
                bindings.Add(name, binding);
            }

            return binding;
        }

        Expr Value(Expr item) => item switch
        {
            Expr.Variable variable => bindings.TryGetValue(variable.Name, out string? binding)
                ? AtomValue(binding)
                : new Expr.Tuple([AtomValue(Constant), variable]),
            Expr.Literal literal => literal,
            Expr.Tuple tuple => new Expr.Tuple([new Expr.Tuple(tuple.Items.Select(Value).ToArray())]),
            Expr.List list => new Expr.List(list.Items.Select(Value).ToArray(), list.Tail is null ? null : Value(list.Tail)),
            Expr.Binary binary => Operation(binary.Operator, [binary.Left, binary.Right]),
            Expr.Unary unary => Operation(unary.Operator, [unary.Operand]),
            Expr.Call { DynamicRemote: false, Module: null or ErlangModuleNames.Module } call
                => Operation(call.Function, call.Arguments),
            _ => throw Unsupported(line)
        };

        Expr Operation(string name, IReadOnlyList<Expr> arguments)
        {
            if (!ComprehensionGuard.IsTest(new Expr.Call(ErlangModuleNames.Module, name, arguments), null)
                && name is not (ErlangOperators.AndAlso or ErlangOperators.OrElse))
            {
                throw Unsupported(line);
            }

            return new Expr.Tuple([AtomValue(name), .. arguments.Select(Value)]);
        }

        static IEnumerable<Expr> Conjunction(Expr guard) => guard is Expr.Binary { Operator: ErlangOperators.AndAlso, SourceLine: 0 } binary
            ? Conjunction(binary.Left).Concat(Conjunction(binary.Right)) : [guard];

        Expr head = Head(pattern);
        Expr[] actions = (clause.Body is Expr.Sequence body ? body.Items : [clause.Body]).Select(Value).ToArray();
        IEnumerable<Expr?> alternatives = clause.Guard is Expr.GuardAlternatives guards ? (IEnumerable<Expr?>)guards.Items : [clause.Guard];
        foreach (Expr? guard in alternatives)
        {
            yield return new Expr.Tuple([head, new Expr.List(guard is null ? [] : Conjunction(guard).Select(Value).ToArray()), new Expr.List(actions)]);
        }
    }

    private static Expr.Literal AtomValue(string name) => new(Term.A(name));

    private static NotSupportedException Unsupported(int line) =>
        new($"Unsupported ms_transform source form at line {line}.");
}
