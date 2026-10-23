namespace Erlang;

public sealed class NativeRecordTerm : Term
{
    public Atom Module { get; }

    public Atom Name { get; }

    public bool IsExported { get; }

    public IReadOnlyList<Atom> FieldNames { get; }

    public IReadOnlyList<Term> Values { get; }

    public NativeRecordTerm(
        Atom module,
        Atom name,
        bool isExported,
        IEnumerable<KeyValuePair<Atom, Term>> fields
    )
    {
        var snapshot = fields.ToArray();
        if (snapshot.Select(field => field.Key).Distinct().Count() != snapshot.Length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        Module = module;
        Name = name;
        IsExported = isExported;
        FieldNames = Array.AsReadOnly(snapshot.Select(field => field.Key).ToArray());
        Values = Array.AsReadOnly(snapshot.Select(field => field.Value).ToArray());
    }

    public override string ToString() => NativeRecordSyntax.Prefix + Module + NativeRecordSyntax.ModuleSeparator + Name +
        NativeRecordSyntax.OpenFields + string.Join(
            NativeRecordSyntax.FieldSeparator,
            FieldNames.Select((field, index) => field + NativeRecordSyntax.FieldAssignment + Values[index])
        ) + NativeRecordSyntax.CloseFields;
}
