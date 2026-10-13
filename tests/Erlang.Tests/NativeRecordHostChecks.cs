namespace Erlang.Tests;

internal static class NativeRecordHostChecks
{
    public static Task Run()
    {
        var fields = new[] { new KeyValuePair<Atom, Term>(Term.A(NativeRecordHostNames.Field), Term.I(1)) };
        var record = new NativeRecordTerm(
            Term.A(NativeRecordHostNames.Module),
            Term.A(NativeRecordHostNames.Record),
            false,
            fields
        );
        fields[0] = new(Term.A(NativeRecordHostNames.Field), Term.I(2));
        Require(record.Values[0].Equals(Term.I(1)));
        Require(record.Values is IList<Term> { IsReadOnly: true });
        Require(record.FieldNames is IList<Atom> { IsReadOnly: true });
        byte[] bytes = ExternalTermFormat.Encode(record);
        Require(ExternalTermFormat.Decode(bytes).Equals(record));
        for (int length = 0; length < bytes.Length; length++)
            Reject(() => ExternalTermFormat.Decode(bytes.AsSpan(0, length)));
        Reject(() => ExternalTermFormat.Decode(new byte[] { 131, 67, 0, 0, 0, 2, 0, 119, 1, 116, 119, 1, 97, 119, 1, 120, 119, 1, 120, 97, 1, 97, 2 }));
        Reject(() => ExternalTermFormat.Decode(new byte[] { 131, 67, 0, 0, 0, 1, 0, 119, 1, 116, 119, 1, 97, 97, 1, 97, 2 }));

        return Task.CompletedTask;
    }

    private static void Reject(Action action)
    {
        try
        {
            action();
        }
        catch (ErlangException exception) when (exception.Reason is Atom { Name: ErlangErrorReasons.BadArgument })
        {
            return;
        }

        throw new InvalidOperationException(NativeRecordHostNames.Broken);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NativeRecordHostNames.Broken);
    }
}
