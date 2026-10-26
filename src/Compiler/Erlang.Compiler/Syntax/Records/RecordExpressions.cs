namespace Erlang.Compiler;

public static class RecordExpressions
{
    public static Expr.Tuple Create(Expr.RecordConstruction record) => new(new[] { (Expr)new Expr.Literal(Term.A(record.Definition.Name)) }.Concat(record.Definition.Fields.Select(field => record.Fields.FirstOrDefault(value => value.Name == field.Name)?.Value ?? record.Wildcard ?? field.Value)).ToArray());

    public static Pattern Pattern(Expr.RecordConstruction record) => new Erlang.Pattern.Tuple(new[] { (Erlang.Pattern)new Erlang.Pattern.Literal(Term.A(record.Definition.Name)) }.Concat(record.Definition.Fields.Select(field => Parser.ToPattern(record.Fields.FirstOrDefault(value => value.Name == field.Name)?.Value ?? record.Wildcard ?? new Expr.Variable(VariableScopeNames.Wildcard)))).ToArray());

    public static TupleTerm Require(Term value, string name, int arity)
    {
        if (value is not TupleTerm tuple || tuple.Items.Count != arity || tuple.Items[0] is not Atom tag || tag.Name != name)
            throw new ErlangException(Term.Tuple(Term.A(RecordAtoms.BadRecord), value));

        return tuple;
    }

    public static Term Access(
        Term value,
        string name,
        int arity,
        int index
    ) => Require(value, name, arity).Items[index];

    public static Term Update(Term value, Expr.RecordUpdate expression, IReadOnlyList<Term> updates)
    {
        var tuple = Require(value, expression.Name, expression.Arity);
        var fields = tuple.Items.ToArray();
        for (int index = 0; index < expression.Fields.Count; index++)
            fields[expression.Fields[index].Index] = updates[index];

        return new TupleTerm(fields);
    }
}
