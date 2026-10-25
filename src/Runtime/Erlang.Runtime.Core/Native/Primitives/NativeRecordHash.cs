namespace Erlang;

internal static class NativeRecordHash
{
    public const uint InstanceMix = 0x36fbef9f;
    public const uint LegacyInstancePrime = 268440629;

    public static uint Definition(NativeRecordTerm record) => ErlangNativeHash.Portable2(new TupleTerm(
        new Term[] { record.Module, record.Name, CoreModules.Boolean(record.IsExported) }.Concat(record.FieldNames)
    ));

    public static IReadOnlyList<int> SortedIndexes(NativeRecordTerm record) => Enumerable.Range(0, record.FieldNames.Count)
        .OrderBy(index => AtomNameRegistry.IndexOf(record.FieldNames[index].Name)).ToArray();

    public static IReadOnlyList<Term> Values(NativeRecordTerm record) => SortedIndexes(record).Select(index => record.Values[index]).ToArray();

    public static TupleTerm LegacyDefinition(NativeRecordTerm record)
    {
        var indexes = SortedIndexes(record);
        var order = new Term[indexes.Count];
        for (int index = 0; index < indexes.Count; index++)
            order[indexes[index]] = Term.I(index);
        var fields = new Term[]
        {
            new TupleTerm(order),
            new Integer(Definition(record)),
            record.Module,
            record.Name,
            CoreModules.Boolean(record.IsExported)
        };

        return new TupleTerm(fields.Concat(indexes.Select(index => record.FieldNames[index])));
    }
}
