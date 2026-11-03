namespace Erlang;

/// <summary>Snapshots a native-record declaration's field collections.</summary>
/// <remarks>
/// Field defaults and type Syntax Terms are caller-owned borrowed graphs; this
/// declaration does not acquire native roots or prolong their source custody.
/// Keep the supplying frame/image/Host lifetime valid through every use.
/// Canonical compiled defaults are prepared under their actual defining image
/// separately from this public metadata container.
/// </remarks>
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
