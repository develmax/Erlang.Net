namespace Erlang;

public sealed class NativeRecordDefinition
{
    public string Name { get; }
    public bool IsExported { get; }
    public IReadOnlyList<KeyValuePair<Atom, Term?>> Fields { get; }

    public IReadOnlyList<NativeRecordFieldType> FieldTypes { get; }

    public NativeRecordDefinition(
        string name,
        bool isExported,
        IEnumerable<KeyValuePair<Atom, Term?>> fields,
        IEnumerable<NativeRecordFieldType>? fieldTypes = null
    )
    {
        ArgumentNullException.ThrowIfNull(name);
        var snapshot = fields.ToArray();
        if (snapshot.Select(member => member.Key).Distinct().Count() != snapshot.Length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        Name = name;
        IsExported = isExported;
        Fields = Array.AsReadOnly(snapshot);
        var types = fieldTypes?.ToArray() ?? [];
        if (types.Select(type => type.Field).Distinct().Count() != types.Length || types.Any(type => !snapshot.Any(field => field.Key.Equals(type.Field))))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        FieldTypes = Array.AsReadOnly(types);
    }
}
