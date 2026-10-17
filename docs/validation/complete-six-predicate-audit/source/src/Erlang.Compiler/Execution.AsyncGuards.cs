namespace Erlang.Compiler;

public static partial class Execution
{
    internal static async ValueTask<bool> GuardAsync(
        Expr? expression,
        Dictionary<string, Term> bindings,
        ProcessContext context
    )
    {
        if (expression is null)
            return true;
        if (expression is Expr.GuardAlternatives alternatives)
        {
            foreach (var alternative in alternatives.Items)
                if (await GuardAsync(alternative, bindings, context))
                    return true;

            return false;
        }
        try
        {
            return (await GuardValueAsync(RecordGuards.Normalize(expression), bindings, context)).Equals(Term.A(ErlangBooleanAtoms.True));
        }
        catch (ErlangException)
        {
            return false;
        }
    }

    internal static ValueTask<Term> PatternKeyAsync(
        Expr expression,
        Dictionary<string, Term> bindings,
        ProcessContext? context
    ) => GuardValueAsync(expression, bindings, context);

    private static async ValueTask<Term> GuardValueAsync(
        Expr expression,
        Dictionary<string, Term> bindings,
        ProcessContext? context
    )
    {
        async ValueTask<Term> Value(Expr item) => await GuardValueAsync(item, bindings, context);

        async ValueTask<Term[]> Values(IReadOnlyList<Expr> items)
        {
            var result = new Term[items.Count];
            for (int index = 0; index < items.Count; index++)
                result[index] = await Value(items[index]);

            return result;
        }

        switch (expression)
        {
            case Expr.Literal literal:
                return literal.Value;
            case Expr.Variable variable:
                return bindings.TryGetValue(variable.Name, out var value) ? value : throw new ErlangException(Term.Tuple(Term.A(ExecutionErrorReasons.Unbound), Term.A(variable.Name)));
            case Expr.RecordTest record:
                return await Value(record.Value) is TupleTerm recordTuple && recordTuple.Items.Count == record.Arity && recordTuple.Items[0] is Atom recordTag && recordTag.Name == record.Name ? Term.A(ErlangBooleanAtoms.True) : throw new ErlangException(ErlangErrorReasons.BadArgument);
            case NativeRecordExpression { Operation: NativeRecordOperation.Access } record:
                return NativeRecordLanguage.Access(
                    await Value(record.Value!),
                    record.Module,
                    record.Name,
                    record.Caller,
                    record.External,
                    Term.A(record.Fields[0].Name)
                );
            case Expr.RecordConstruction record:
                return await Value(RecordExpressions.Create(record));
            case Expr.RecordAccess record:
                return RecordExpressions.Access(
                    await Value(record.Value),
                    record.Name,
                    record.Arity,
                    record.Index
                );
            case Expr.Tuple tuple:
                return new TupleTerm(await Values(tuple.Items));
            case Expr.List list:
                {
                    var items = await Values(list.Items);
                    Term? tail = list.Tail is null ? null : await Value(list.Tail);

                    return Cons.From(items, tail);
                }
            case Expr.Map map:
                {
                    Term basis = map.Base is null ? new MapTerm([]) : await Value(map.Base);
                    var fields = new List<(Term, Term, bool)>();
                    foreach (var field in map.Fields)
                        fields.Add((await Value(field.Key), await Value(field.Value), field.Exact));

                    return EvaluateMap(basis, fields.ToArray());
                }
            case Expr.Bits bits:
                {
                    var fields = new List<(Term, Term?, BitSegment)>();
                    foreach (var segment in bits.Segments)
                        fields.Add((await Value(segment.Value), segment.Size is null ? null : await Value(segment.Size), segment));

                    return BitConstruction.Create(fields.ToArray());
                }
            case Expr.Unary unary:
                return Unary(unary.Operator, await Value(unary.Operand));
            case Expr.Binary { Operator: ErlangOperators.AndAlso } binary:
                return CoreModules.Bool(await Value(binary.Left)) ? await Value(binary.Right) : Term.A(ErlangBooleanAtoms.False);
            case Expr.Binary { Operator: ErlangOperators.OrElse } binary:
                return CoreModules.Bool(await Value(binary.Left)) ? Term.A(ErlangBooleanAtoms.True) : await Value(binary.Right);
            case Expr.Binary binary:
                return Binary(
                    binary.Operator,
                    await Value(binary.Left),
                    await Value(binary.Right),
                    context
                );
            case Expr.Call call when RecordPredicateSyntax.HasLiteralNonpositiveSize(call):
                return Term.A(ErlangBooleanAtoms.False);
            case Expr.Call call when RecordPredicateSyntax.TupleTest(call) is { } predicate:
                return RecordPredicateSyntax.Test(await Value(call.Arguments[0]), predicate);
            case Expr.Call call when call.Module == ErlangModuleNames.Module && call.Arguments.Count == 1 && ComprehensionGuard.IsOperator(call.Function, 1):
                return Unary(call.Function, await Value(call.Arguments[0]));
            case Expr.Call call when call.Module == ErlangModuleNames.Module && call.Arguments.Count == 2 && ComprehensionGuard.IsOperator(call.Function, 2):
                return Binary(
                    call.Function,
                    await Value(call.Arguments[0]),
                    await Value(call.Arguments[1]),
                    context
                );
            case Expr.Call call when context is not null && Semantics.GuardBifs.Contains((call.Function, call.Arguments.Count)):
                return await context.Runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    call.Function,
                    await Values(call.Arguments)
                );
            default:
                throw new ErlangException(ExecutionErrorReasons.IllegalGuard);
        }
    }
}
