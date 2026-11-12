using System.Text;
using System.Text.RegularExpressions;

internal static class Asn1BerCodecOrdering
{
    private static readonly Regex Signature = new(
        @"^(?<name>'[^']+'|[a-z][A-Za-z0-9_]*)\((?<parameters>[^()\r\n]*)\) ->",
        RegexOptions.Multiline | RegexOptions.CultureInvariant
    );

    internal static string Order(string source, IReadOnlyDictionary<string, Asn1BerSchema.Type> roots,
        IReadOnlyList<KeyValuePair<string, Asn1BerSchema.Type>> codecs)
    {
        var signatures = Signature.Matches(source).ToArray();
        var bodies = new Dictionary<(string Name, int Arity), StringBuilder>();
        for (int index = 0; index < signatures.Length; index++)
        {
            var signature = signatures[index];
            string name = signature.Groups["name"].Value.Trim('\'');
            string parameters = signature.Groups["parameters"].Value;
            int arity = parameters.Length == 0 ? 0 : parameters.Count(character => character == ',') + 1;
            var key = (name, arity);
            if (!bodies.TryGetValue(key, out var body))
            {
                bodies.Add(key, body = new());
            }
            int end = index + 1 < signatures.Length ? signatures[index + 1].Index : source.Length;
            body.Append(source.AsSpan(signature.Index, end - signature.Index));
        }

        var types = codecs.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
        var ordered = new StringBuilder();
        void Emit(string name, int arity)
        {
            if (bodies.Remove((name, arity), out var body))
            {
                ordered.Append(body);
            }
        }
        void EmitHeading(string name, bool nested)
        {
            if (nested)
            {
                ordered.AppendLine();
            }
            ordered.AppendLine()
                .AppendLine()
                .AppendLine("%%================================")
                .AppendLine($"%%  {name}")
                .AppendLine("%%================================");
        }
        void Visit(string prefix, string name, bool nested = false)
        {
            if (nested && prefix == "enc_")
            {
                EmitHeading(name, nested: true);
            }
            Emit(prefix + name, 1);
            Emit(prefix + name, 2);
            Emit(prefix + name + "_components", 3);
            var type = types[name];
            while (type.Kind == "tagged")
            {
                type = type.Element!;
            }
            if (type.Fields is { } fields)
            {
                foreach (var field in fields)
                {
                    string child = name + "_" + field.Name;
                    if (types.ContainsKey(child))
                    {
                        Visit(prefix, child, nested: true);
                    }
                }
            }
            if (type.Kind == "collection" && types.ContainsKey(name + "_SEQOF"))
            {
                Visit(prefix, name + "_SEQOF", nested: true);
            }
        }
        foreach (string name in roots.Keys)
        {
            EmitHeading(name, nested: false);
            Visit("enc_", name);
            ordered.AppendLine().AppendLine();
            Visit("dec_", name);
        }
        if (bodies.Count != 0)
        {
            throw new InvalidOperationException("Generated ASN.1 helper functions have no type owner.");
        }
        return ordered.ToString();
    }
}
