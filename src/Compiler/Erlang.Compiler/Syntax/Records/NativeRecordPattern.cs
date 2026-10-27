namespace Erlang.Compiler;

public sealed record NativeRecordPattern(
    string Module,
    string? Name,
    string Caller,
    bool External,
    IReadOnlyList<NativeRecordPatternField> Fields
) : Pattern, ICanonicalPattern507
{
    protected override bool MatchCore(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context = null,
        Dictionary<string, Term>? keyScope = null
    )
    {
        NativeRecordTerm record;
        try
        {
            record = NativeRecordLanguage.Require(
                value,
                Module,
                Name,
                Caller,
                External,
                Fields.Count != 0
            );
        }
        catch (ErlangException)
        {
            return false;
        }
        foreach (var field in Fields)
        {
            int index = record.FieldNames.ToList().FindIndex(key => key.Name == field.Name);
            if (index < 0 || !field.Value.Match(
                record.Values[index],
                bindings,
                context,
                keyScope
            ))
                return false;
        }

        return true;
    }

    protected override async ValueTask<bool> MatchCoreAsync(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context = null,
        Dictionary<string, Term>? keyScope = null
    )
    {
        NativeRecordTerm record;
        try
        {
            record = NativeRecordLanguage.Require(
                value,
                Module,
                Name,
                Caller,
                External,
                Fields.Count != 0
            );
        }
        catch (ErlangException)
        {
            return false;
        }
        foreach (var field in Fields)
        {
            int index = record.FieldNames.ToList().FindIndex(key => key.Name == field.Name);
            if (index < 0 || !await field.Value.MatchAsync(
                record.Values[index],
                bindings,
                context,
                keyScope
            ))
                return false;
        }

        return true;
    }
}
