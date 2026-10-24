using Erlang;

namespace Erlang.Tests;

internal static class ProplistHashHostResources
{
    internal static Term Read(string name)
    {
        using Stream stream = typeof(ProplistHashHostResources).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidDataException(ProplistHashHostNames.InputFailure);
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return ExternalTermFormat.Decode(bytes.ToArray());
    }

    internal static string Source()
    {
        using Stream stream = typeof(ProplistHashHostResources).Assembly.GetManifestResourceStream(ProplistHashHostNames.SourceResource)
            ?? throw new InvalidDataException(ProplistHashHostNames.InputFailure);
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    internal static Dictionary<string, Term> Rows(Term value)
    {
        var result = new Dictionary<string, Term>(StringComparer.Ordinal);
        foreach (Term item in Cons.Items(value))
        {
            if (item is not TupleTerm { Items.Count: 2 } pair || pair.Items[0] is not Atom name
                || !result.TryAdd(name.Name, pair.Items[1]))
                throw new InvalidDataException(ProplistHashHostNames.InputFailure);
        }

        return result;
    }
}
