namespace Erlang;

public sealed class NativeRecordTerm : Term
{
    private readonly Atom? portableModule;
    private readonly Atom? portableName;
    private readonly bool portableExported;
    private readonly IReadOnlyList<Atom>? portableFieldNames;
    private readonly IReadOnlyList<Term>? portableValues;
    internal IOwnedNativeRecordStorage? OwnedRecordStorage { get; }

    public Atom Module => OwnedRecordStorage?.ReadRecordModule() ?? portableModule!;

    public Atom Name => OwnedRecordStorage?.ReadRecordName() ?? portableName!;

    public bool IsExported => OwnedRecordStorage?.ReadRecordExported() ?? portableExported;

    public IReadOnlyList<Atom> FieldNames => OwnedRecordStorage?.ReadRecordFieldNames() ?? portableFieldNames!;

    public IReadOnlyList<Term> Values => OwnedRecordStorage?.ReadRecordValues() ?? portableValues!;

    internal NativeRecordTerm(IOwnedNativeRecordStorage storage) => OwnedRecordStorage = storage;

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
        portableModule = module;
        portableName = name;
        portableExported = isExported;
        portableFieldNames = Array.AsReadOnly(snapshot.Select(field => field.Key).ToArray());
        portableValues = Array.AsReadOnly(snapshot.Select(field => field.Value).ToArray());
    }

    public override string ToString() => NativeRecordSyntax.Prefix + Module + NativeRecordSyntax.ModuleSeparator + Name +
        NativeRecordSyntax.OpenFields + string.Join(
            NativeRecordSyntax.FieldSeparator,
            FieldNames.Select((field, index) => field + NativeRecordSyntax.FieldAssignment + Values[index])
        ) + NativeRecordSyntax.CloseFields;
}
