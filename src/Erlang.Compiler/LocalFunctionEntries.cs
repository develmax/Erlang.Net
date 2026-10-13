using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Erlang.Compiler;

internal static class LocalFunctionEntries
{
    private const string InterpretedModule = "erl_eval";
    private const string InterpretedOwner = "anonymous";
    private const string AnonymousSeparator = "-fun-";
    private static readonly ConditionalWeakTable<ModuleDefinition, ModuleEntries> Modules = new();
    private static readonly ConditionalWeakTable<Expr.Fun, LocalFunctionIdentity> Interpreted = new();

    public static LocalFunctionIdentity GetNamed(ModuleDefinition module, string name, int arity) =>
        Modules.GetValue(module, value => new ModuleEntries(value)).NamedEntries[(name, arity)];

    public static Expr.Fun Anonymous(ModuleDefinition module, int index) =>
        Modules.GetValue(module, value => new ModuleEntries(value)).Entries.Single(entry => entry.Value.Index == index).Key;

    public static LocalFunctionIdentity Get(ModuleDefinition? module, Expr.Fun expression, ProcessContext context)
    {
        if (module is not null)
            return Modules.GetValue(module, value => new ModuleEntries(value)).Entries[expression];

        return Interpreted.GetValue(
            expression,
            value => Identity(
                InterpretedModule,
                context.CurrentFunction ?? InterpretedOwner,
                context.CurrentFunctionArity,
                0,
                MD5.HashData(Encoding.UTF8.GetBytes(CodeGeneration.ExpressionCode(value)))
            )
        );
    }

    private static LocalFunctionIdentity Identity(
        string module,
        string owner,
        int ownerArity,
        int index,
        byte[] unique
    ) => new(
        Term.A(module),
        Term.A("-" + owner + "/" + ownerArity.ToString(CultureInfo.InvariantCulture) + AnonymousSeparator + index.ToString(CultureInfo.InvariantCulture) + "-"),
        index,
        (int)(BinaryPrimitives.ReadUInt32BigEndian(unique) >> (FunctionCodeLayout.UniquePrefixBits - FunctionCodeLayout.OldUniqueBits)),
        new BitString(unique)
    );

    private sealed class ModuleEntries
    {
        public Dictionary<Expr.Fun, LocalFunctionIdentity> Entries { get; } = new(ReferenceEqualityComparer.Instance);
        public Dictionary<(string Name, int Arity), LocalFunctionIdentity> NamedEntries { get; } = new();

        public ModuleEntries(ModuleDefinition module)
        {
            byte[] unique = MD5.HashData(Encoding.UTF8.GetBytes(CodeGeneration.ModuleCode(module, string.Empty, false)));
            foreach (var function in module.Functions)
                foreach (var expression in Clauses(function.Clauses).SelectMany(Walk))
                    if (expression is Expr.Fun fun && !Entries.ContainsKey(fun))
                        Entries.Add(fun, Identity(
                            module.Name,
                            function.Name,
                            function.Arity,
                            Entries.Count,
                            unique
                        ));
            foreach (var function in module.Functions)
                NamedEntries.Add(
                    (function.Name, function.Arity),
                    new(
                        Term.A(module.Name),
                        Term.A(function.Name),
                        Entries.Count + NamedEntries.Count,
                        (int)(BinaryPrimitives.ReadUInt32BigEndian(unique) >> (FunctionCodeLayout.UniquePrefixBits - FunctionCodeLayout.OldUniqueBits)),
                        new BitString(unique)
                    )
                );
        }
    }

    private static IEnumerable<Expr> Walk(Expr expression)
    {
        yield return expression;
        foreach (var child in Children(expression))
            foreach (var item in Walk(child))
                yield return item;
    }

    private static IEnumerable<Expr> Clauses(IEnumerable<Clause> clauses) => clauses.SelectMany(clause => clause.Patterns.SelectMany(PatternExpressions).Concat(clause.Guard is null ? [] : new[] { clause.Guard }).Append(clause.Body));

    private static IEnumerable<Expr> PatternExpressions(Pattern pattern) => pattern switch
    {
        NativeRecordPattern record => record.Fields.SelectMany(field => PatternExpressions(field.Value)),
        AliasPattern alias => PatternExpressions(alias.Left).Concat(PatternExpressions(alias.Right)),
        Pattern.Tuple tuple => tuple.Items.SelectMany(PatternExpressions),
        Pattern.List list => list.Items.SelectMany(PatternExpressions).Concat(list.Tail is null ? [] : PatternExpressions(list.Tail)),
        MapPattern map => map.Fields.SelectMany(field => new[] { field.Key }.Concat(PatternExpressions(field.Value))),
        BitPattern bits => bits.Segments.SelectMany(segment => (segment.Specification.Size is null ? Enumerable.Empty<Expr>() : new[] { segment.Specification.Size }).Concat(PatternExpressions(segment.Value))),
        _ => []
    };

    private static IEnumerable<Expr> Qualifiers(IEnumerable<ComprehensionQualifier> qualifiers) => qualifiers.SelectMany(qualifier => qualifier switch
    {
        ComprehensionQualifier.Filter filter => new[] { filter.Expression },
        ComprehensionQualifier.Generator generator => PatternExpressions(generator.Pattern).Append(generator.Source),
        ComprehensionQualifier.BinaryGenerator generator => PatternExpressions(generator.Pattern).Append(generator.Source),
        ComprehensionQualifier.MapGenerator generator => PatternExpressions(generator.Pattern).Append(generator.Source),
        ComprehensionQualifier.Zip zip => Qualifiers(zip.Generators),
        _ => throw new NotSupportedException()
    });

    private static IEnumerable<Expr> Children(Expr expression) => expression switch
    {
        Expr.Literal or Expr.Variable or Expr.FunctionReference => [],
        NativeRecordExpression record => record.Children,
        Expr.RecordConstruction record => Children(RecordExpressions.Create(record)),
        Expr.RecordAccess record => [record.Value],
        Expr.RecordTest record => [record.Value],
        Expr.RecordUpdate record => new[] { record.Value }.Concat(record.Fields.Select(field => field.Value)),
        Expr.Tuple tuple => tuple.Items,
        Expr.List list => list.Tail is null ? list.Items : list.Items.Append(list.Tail),
        Expr.ListComprehension list => list.Items.Concat(Qualifiers(list.Qualifiers)),
        Expr.BinaryComprehension binary => new[] { binary.Body }.Concat(Qualifiers(binary.Qualifiers)),
        Expr.MapComprehension map => map.Fields.SelectMany(field => new[] { field.Key, field.Value }).Concat(Qualifiers(map.Qualifiers)),
        Expr.Map map => MapExpressionBindings.Expressions(map),
        Expr.Bits bits => bits.Segments.SelectMany(segment => segment.Size is null ? new[] { segment.Value } : new[] { segment.Value, segment.Size }),
        Expr.Call call => call.Arguments,
        Expr.Apply apply => new[] { apply.Function }.Concat(apply.Arguments),
        Expr.Unary unary => [unary.Operand],
        Expr.Binary binary => [binary.Left, binary.Right],
        Expr.Match match => PatternExpressions(match.Pattern).Append(match.Value),
        Expr.MaybeMatch match => PatternExpressions(match.Pattern).Append(match.Value),
        Expr.Maybe maybe => maybe.Items.Concat(Clauses(maybe.Clauses)),
        Expr.Sequence sequence => sequence.Items,
        Expr.Block block => [block.Body],
        Expr.Catch caught => [caught.Operand],
        Expr.Try tried => new[] { tried.Body }.Concat(Clauses(tried.Clauses)).Concat(Clauses(tried.Catches)).Concat(tried.After is null ? [] : new[] { tried.After }),
        Expr.Case @case => new[] { @case.Value }.Concat(Clauses(@case.Clauses)),
        Expr.If @if => Clauses(@if.Clauses),
        Expr.Receive receive => Clauses(receive.Clauses).Concat(receive.Timeout is null ? [] : new[] { receive.Timeout }).Concat(receive.After is null ? [] : new[] { receive.After }),
        Expr.Fun fun => Clauses(fun.Clauses),
        Expr.GuardAlternatives alternatives => alternatives.Items,
        _ => throw new NotSupportedException()
    };
}
