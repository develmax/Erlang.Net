using System.Security.Cryptography;
using System.Text;

namespace Erlang.BeamBuild;

internal static class BeamApplicationParsedEvidenceValidator
{
    internal static BeamApplicationParsedEvidence Validate(
        ReadOnlySpan<byte> resultEtf,
        string applicationName,
        string version,
        IReadOnlyList<string> expectedModules,
        IReadOnlyList<string> orderedConfigLogicalPaths
    )
    {
        ArgumentNullException.ThrowIfNull(applicationName);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(expectedModules);
        ArgumentNullException.ThrowIfNull(orderedConfigLogicalPaths);
        byte[] bytes = resultEtf.ToArray();
        Term decoded;
        try
        {
            decoded = ExternalTermFormat.Decode(bytes);
        }
        catch (ErlangException exception)
        {
            throw Invalid(exception);
        }
        IReadOnlyList<Term> envelope = Tuple(decoded, 3);
        Require(AtomName(envelope[0]) == BeamApplicationParsedNames.Parsed);
        IReadOnlyList<Term> descriptor = Tuple(envelope[1], 3);
        Require(AtomName(descriptor[0]) == BeamApplicationParsedNames.Application);
        Require(AtomName(descriptor[1]) == applicationName);
        var propertyOrder = new List<string>();
        var properties = new Dictionary<string, Term>(StringComparer.Ordinal);
        foreach (Term property in ProperList(descriptor[2]))
        {
            IReadOnlyList<Term> pair = Tuple(property, 2);
            string key = AtomName(pair[0]);
            Require(properties.TryAdd(key, pair[1]));
            propertyOrder.Add(key);
        }
        Require(properties.TryGetValue(BeamApplicationParsedNames.Version, out Term? actualVersion));
        Require(Charlist(actualVersion!) == version);
        Require(properties.TryGetValue(BeamApplicationParsedNames.Modules, out Term? modules));
        var moduleOrder = new List<string>();
        var actualSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (Term item in ProperList(modules!))
        {
            string name = AtomName(item);
            Require(actualSet.Add(name));
            moduleOrder.Add(name);
        }
        var expectedSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in expectedModules)
        {
            Require(name is not null && expectedSet.Add(name));
        }
        Require(actualSet.SetEquals(expectedSet));
        var configNames = new List<string>();
        var uniqueConfigNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (Term item in ProperList(envelope[2]))
        {
            IReadOnlyList<Term> pair = Tuple(item, 2);
            string logical = Charlist(pair[0]);
            Require(configNames.Count < orderedConfigLogicalPaths.Count);
            Require(logical == orderedConfigLogicalPaths[configNames.Count]);
            Require(uniqueConfigNames.Add(logical));
            ValidateConfig(pair[1]);
            configNames.Add(logical);
        }
        Require(configNames.Count == orderedConfigLogicalPaths.Count);

        return new BeamApplicationParsedEvidence(
            Array.AsReadOnly(moduleOrder.ToArray()),
            Array.AsReadOnly(propertyOrder.ToArray()),
            Array.AsReadOnly(configNames.ToArray()),
            bytes.Length,
            Convert.ToHexString(SHA256.HashData(bytes))
        );
    }

    private static void ValidateConfig(Term config)
    {
        var apps = new HashSet<string>(StringComparer.Ordinal);
        foreach (Term item in ProperList(config))
        {
            IReadOnlyList<Term> app = Tuple(item, 2);
            Require(apps.Add(AtomName(app[0])));
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (Term property in ProperList(app[1]))
            {
                IReadOnlyList<Term> pair = Tuple(property, 2);
                Require(keys.Add(AtomName(pair[0])));
            }
        }
    }

    private static IReadOnlyList<Term> Tuple(Term value, int arity)
    {
        if (value is not TupleTerm tuple || tuple.Items.Count != arity)
            throw Invalid();

        return tuple.Items;
    }

    private static string AtomName(Term value)
    {
        if (value is not Atom atom)
            throw Invalid();

        return atom.Name;
    }

    private static IEnumerable<Term> ProperList(Term value)
    {
        while (value is Cons cell)
        {
            yield return cell.Head;
            value = cell.Tail;
        }
        Require(value is Nil);
    }

    private static string Charlist(Term value)
    {
        var text = new StringBuilder();
        foreach (Term item in ProperList(value))
        {
            if (item is not Integer integer || integer.Value < 0 || integer.Value > 0x10ffff)
                throw Invalid();
            int scalar = (int)integer.Value;
            Require(Rune.IsValid(scalar));
            text.Append(new Rune(scalar).ToString());
        }

        return text.ToString();
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw Invalid();
    }

    private static InvalidDataException Invalid(Exception? inner = null)
        => new(BeamApplicationParsedNames.InvalidEvidence, inner);
}
