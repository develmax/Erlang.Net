using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private EntryIntegerRanges? entryIntegerRanges;

    private (long Lower, long Upper)? IntegerEntryBounds(
        FunctionDefinition function,
        int register,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> functions
    )
    {
        var signature = (function.Name, function.Arity);
        if (exportedFunctions.Contains(signature) || IsDeclaredNative(signature))
        {
            return null;
        }

        entryIntegerRanges ??= new EntryIntegerRanges(this, name, functions);

        return entryIntegerRanges.Bounds(signature, register);
    }

    private sealed class EntryIntegerRanges
    {
        private enum Kind
        {
            Unreachable,
            Unknown,
            Integer,
            EmptyList,
            IntegerList
        }

        private readonly record struct Fact(Kind Kind, long Lower = 0, long Upper = 0)
        {
            internal static readonly Fact Unknown = new(Kind.Unknown);
            internal static readonly Fact EmptyList = new(Kind.EmptyList);

            internal Fact Head => Kind switch
            {
                Kind.IntegerList => this with { Kind = Kind.Integer },
                Kind.EmptyList or Kind.Unreachable => default,
                _ => Unknown
            };

            internal Fact Tail => Kind switch
            {
                Kind.IntegerList or Kind.Unreachable => this,
                Kind.EmptyList => default,
                _ => Unknown
            };

            internal Fact Cons(Fact head) => head.Kind == Kind.Integer
                ? Join(head with { Kind = Kind.IntegerList }) : Unknown;

            internal Fact Join(Fact other)
            {
                if (Kind is Kind.Unreachable || this == other)
                {
                    return other;
                }
                if (other.Kind == Kind.Unreachable || Kind == Kind.IntegerList && other.Kind == Kind.EmptyList)
                {
                    return this;
                }
                if (Kind == Kind.EmptyList && other.Kind == Kind.IntegerList)
                {
                    return other;
                }

                return Kind == other.Kind && Kind is Kind.Integer or Kind.IntegerList
                    ? new(Kind, Math.Min(Lower, other.Lower), Math.Max(Upper, other.Upper)) : Unknown;
            }
        }

        private readonly BeamModuleCompiler owner;
        private readonly string module;
        private readonly IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> functions;
        private readonly Dictionary<(string Name, int Arity), Fact[]> incoming = [];
        private readonly HashSet<(string Name, int Arity)> pending = [];
        private readonly Queue<(string Name, int Arity)> work = [];

        internal EntryIntegerRanges(
            BeamModuleCompiler owner,
            string module,
            IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> functions
        )
        {
            this.owner = owner;
            this.module = module;
            this.functions = functions;

            var roots = owner.exportedFunctions.Concat(functions.Keys.Where(owner.IsDeclaredNative)).Concat(functions.Values
                .SelectMany(function => LocalFunctionEntries.ClauseExpressions(function.Clauses))
                .SelectMany(LocalFunctionEntries.Walk).OfType<Expr.FunctionReference>()
                .Select(reference => (reference.Name, reference.Arity)));
            if (owner.OnLoadFunction is { } onLoad)
            {
                roots = roots.Append((onLoad, 0));
            }

            foreach (var root in roots)
            {
                AddCall(root, Enumerable.Repeat(Fact.Unknown, root.Arity).ToArray());
            }

            while (work.TryDequeue(out var signature))
            {
                pending.Remove(signature);
                ReadClauses(functions[signature].Clauses, incoming[signature], new(StringComparer.Ordinal));
            }
        }

        internal (long Lower, long Upper)? Bounds((string Name, int Arity) signature, int register) =>
            incoming.TryGetValue(signature, out var arguments) && arguments[register] is { Kind: Kind.Integer } fact
                ? (fact.Lower, fact.Upper) : null;

        private void AddCall((string Name, int Arity) target, Fact[] arguments)
        {
            if (!functions.ContainsKey(target) || arguments.Any(argument => argument.Kind == Kind.Unreachable))
            {
                return;
            }

            bool changed = !incoming.TryGetValue(target, out var previous);
            if (previous is null)
            {
                incoming.Add(target, arguments);
            }
            else
            {
                for (int index = 0; index < arguments.Length; index++)
                {
                    var joined = previous[index].Join(arguments[index]);
                    changed |= joined != previous[index];
                    previous[index] = joined;
                }
            }

            if (changed && pending.Add(target))
            {
                work.Enqueue(target);
            }
        }

        private Fact Read(Expr expression, Dictionary<string, Fact> scope)
        {
            switch (expression)
            {
                case Expr.Literal literal:
                    return ReadLiteral(literal.Value);
                case Expr.Variable variable:
                    return scope.GetValueOrDefault(variable.Name, Fact.Unknown);
                case Expr.Match match:
                    var matched = Read(match.Value, scope);
                    Bind(match.Pattern, matched, scope);

                    return matched;
                case Expr.Sequence sequence:
                    Fact result = Fact.Unknown;
                    foreach (var item in sequence.Items)
                    {
                        result = Read(item, scope);
                        if (result.Kind == Kind.Unreachable)
                        {
                            break;
                        }
                    }

                    return result;
                case Expr.Block block:
                    return Read(block.Body, scope);
                case Expr.List list:
                    var items = list.Items.Select(item => Read(item, scope)).ToArray();
                    var tail = list.Tail is null ? Fact.EmptyList : Read(list.Tail, scope);
                    foreach (var item in items.Reverse())
                    {
                        tail = tail.Cons(item);
                    }

                    return tail;
                case Expr.Call call:
                    var arguments = call.Arguments.Select(argument => Read(argument, scope)).ToArray();
                    if (!call.DynamicRemote && (call.Module is null || call.Module == module))
                    {
                        AddCall((call.Function, arguments.Length), arguments);
                    }
                    if (arguments is [var input] && owner.ResolvesErlangBif(call))
                    {
                        return call.Function switch
                        {
                            ErlangModuleNames.Head => input.Head,
                            ErlangModuleNames.Tail => input.Tail,
                            _ => Fact.Unknown
                        };
                    }

                    return Fact.Unknown;
                case Expr.Case decision:
                    return ReadClauses(decision.Clauses, [Read(decision.Value, scope)], scope);
                case Expr.If decision:
                    return ReadClauses(decision.Clauses, [], scope);
                case Expr.Try tried:
                    var bodyScope = new Dictionary<string, Fact>(scope, StringComparer.Ordinal);
                    Fact body = Read(tried.Body, bodyScope);
                    Fact normal = tried.Clauses.Count == 0 ? body : ReadClauses(tried.Clauses, [body], bodyScope);
                    var caught = ReadClauses(tried.Catches, [], scope);
                    if (tried.After is { } cleanup)
                    {
                        Read(cleanup, new(scope, StringComparer.Ordinal));
                    }

                    return normal.Join(caught);
                case Expr.Fun closure:
                    ReadClauses(closure.Clauses, [], new(StringComparer.Ordinal));

                    return Fact.Unknown;
                default:
                    var source = expression is Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension
                        ? new Dictionary<string, Fact>(StringComparer.Ordinal) : scope;
                    foreach (var child in LocalFunctionEntries.Children(expression))
                    {
                        Read(child, new(source, StringComparer.Ordinal));
                    }

                    return Fact.Unknown;
            }
        }

        private Fact ReadClauses(
            IReadOnlyList<Clause> clauses,
            IReadOnlyList<Fact> arguments,
            Dictionary<string, Fact> outer
        )
        {
            Fact result = default;
            foreach (var clause in clauses)
            {
                var scope = new Dictionary<string, Fact>(outer, StringComparer.Ordinal);
                bool possible = true;
                for (int index = 0; index < clause.Patterns.Count; index++)
                {
                    var argument = index < arguments.Count ? arguments[index] : Fact.Unknown;
                    var pattern = clause.Patterns[index];
                    if (argument.Kind == Kind.Unreachable
                        || argument.Kind == Kind.Integer && ReadIntegerPattern(pattern) is { } value
                            && (value.Value < argument.Lower || value.Value > argument.Upper))
                    {
                        possible = false;
                        break;
                    }

                    Bind(pattern, argument, scope);
                }
                if (!possible)
                {
                    continue;
                }

                if (clause.Guard is { } guard)
                {
                    Read(guard, new(scope, StringComparer.Ordinal));
                }

                result = result.Join(Read(clause.Body, scope));
            }

            return result;
        }

        private static void Bind(Pattern pattern, Fact fact, Dictionary<string, Fact> scope)
        {
            switch (pattern)
            {
                case Pattern.Variable variable when variable.Name != VariableScopeNames.Wildcard:
                    scope.TryAdd(variable.Name, fact);
                    break;
                case Pattern.List list:
                    foreach (var item in list.Items)
                    {
                        Bind(item, fact.Head, scope);
                    }
                    if (list.Tail is { } tail)
                    {
                        Bind(tail, fact.Tail, scope);
                    }
                    break;
                case AliasPattern alias:
                    var shared = ReadIntegerPattern(alias) is { } literal ? ReadLiteral(literal) : fact;
                    Bind(alias.Left, shared, scope);
                    Bind(alias.Right, shared, scope);
                    break;
                default:
                    foreach (string variable in Semantics.Variables(pattern))
                    {
                        scope.TryAdd(variable, Fact.Unknown);
                    }
                    break;
            }
        }

        private static Integer? ReadIntegerPattern(Pattern pattern) => pattern switch
        {
            Pattern.Literal { Value: Integer integer } => integer,
            AliasPattern alias => ReadIntegerPattern(alias.Left) ?? ReadIntegerPattern(alias.Right),
            _ => null
        };

        private static Fact ReadLiteral(Term value) => value switch
        {
            Integer integer when integer.Value >= long.MinValue && integer.Value <= long.MaxValue
                => new(Kind.Integer, (long)integer.Value, (long)integer.Value),
            Nil => Fact.EmptyList,
            _ => Fact.Unknown
        };
    }
}
