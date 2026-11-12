using System.Globalization;
using System.Text.RegularExpressions;

internal sealed class Asn1BerSchema
{
    private static readonly Regex Tokens = new(
        @"--[^\r\n]*?(?:--|(?=\r?$))|\s+|::=|\.\.\.|\.\.|[A-Za-z][A-Za-z0-9-]*|-?[0-9]+|[{}()\[\],]|.",
        RegexOptions.Multiline | RegexOptions.CultureInvariant
    );

    private readonly string[] tokens;
    private int position;

    internal string Name { get; }
    internal bool Extensible { get; }
    internal Dictionary<string, Type> Types { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, int> Values { get; } = new(StringComparer.Ordinal);

    internal sealed record Type(string Kind, int? Tag = null, string? Name = null, Type? Element = null,
        IReadOnlyList<Field>? Fields = null, IReadOnlyDictionary<string, int>? Numbers = null, bool Extensible = false, (string Lower, string Upper)? Range = null);

    internal sealed record Field(string Name, Type Type, string Presence = "required");

    internal Asn1BerSchema(string source)
    {
        tokens = Tokens.Matches(source).Select(match => match.Value)
            .Where(token => !char.IsWhiteSpace(token[0]) && !token.StartsWith("--", StringComparison.Ordinal)).ToArray();
        Name = Identifier();
        Expect("DEFINITIONS");
        Expect("IMPLICIT");
        Expect("TAGS");
        Extensible = Take("EXTENSIBILITY");
        if (Extensible)
        {
            Expect("IMPLIED");
        }
        Expect("::=");
        Expect("BEGIN");
        while (!Take("END"))
        {
            string name = Identifier();
            if (Take("INTEGER"))
            {
                Expect("::=");
                Values.Add(name, Number());
            }
            else
            {
                Expect("::=");
                Types.Add(name, ReadType(name));
            }
        }
        if (position != tokens.Length)
        {
            throw Unexpected("end of input");
        }
    }

    private Type ReadType(string name)
    {
        int? tag = null;
        if (Take("["))
        {
            int tagClass = Take("APPLICATION") ? 1 : 2;
            int number = Number();
            if (number < 0 || number >= 65536)
            {
                throw Unexpected("a supported BER tag number");
            }
            tag = (tagClass << 16) | number;
            Expect("]");
        }

        string kind = Identifier();
        Type result;
        switch (kind)
        {
            case "OCTET":
                Expect("STRING");
                result = new("octets", 4);
                break;
            case "INTEGER":
                result = new("integer", 2);
                if (Take("("))
                {
                    string lower = RangeBound();
                    Expect("..");
                    string upper = RangeBound();
                    Expect(")");
                    result = result with { Range = (lower, upper) };
                }
                break;
            case "BOOLEAN":
                result = new("boolean", 1);
                break;
            case "NULL":
                result = new("null", 5);
                break;
            case "ENUMERATED":
                result = ReadEnumeration();
                break;
            case "SEQUENCE":
            case "SET":
                TakeSize();
                if (Take("OF"))
                {
                    if (Peek is { Length: > 0 } label && char.IsAsciiLetterLower(label[0]))
                    {
                        Identifier();
                    }
                    result = new("collection", kind == "SET" ? 17 : 16, Element: ReadType(name + "_SEQOF"));
                    break;
                }
                if (kind == "SET")
                {
                    throw Unexpected("SET OF");
                }
                result = ReadFields("sequence", 16, name);
                break;
            case "CHOICE":
                result = ReadFields("choice", null, name);
                break;
            default:
                result = new("ref", Name: kind);
                break;
        }

        SkipConstraint();

        return tag is null ? result : new("tagged", tag, Element: result);
    }

    private Type ReadFields(string kind, int? tag, string name)
    {
        Expect("{");
        var fields = new List<Field>();
        bool extension = Extensible;
        do
        {
            if (Take("..."))
            {
                extension = true;
                continue;
            }
            if (Take("COMPONENTS"))
            {
                Expect("OF");
                string reference = Identifier();
                if (!Types.TryGetValue(reference, out Type? source) || source.Kind != "sequence")
                {
                    throw Unexpected("an earlier SEQUENCE after COMPONENTS OF");
                }
                fields.AddRange(source.Fields!);
                continue;
            }

            string field = Identifier();
            Type type = ReadType(name + "_" + field);
            string presence = "required";
            if (Take("OPTIONAL"))
            {
                presence = "optional";
            }
            else if (Take("DEFAULT"))
            {
                Expect("FALSE");
                presence = "default_false";
            }
            fields.Add(new(field, type, presence));
        }
        while (Take(","));
        Expect("}");

        return new(kind, tag, name, Fields: fields, Extensible: extension);
    }

    private Type ReadEnumeration()
    {
        Expect("{");
        var numbers = new Dictionary<string, int>(StringComparer.Ordinal);
        bool extension = false;
        do
        {
            if (Take("..."))
            {
                extension = true;
                continue;
            }
            string name = Identifier();
            Expect("(");
            numbers.Add(name, Number());
            Expect(")");
        }
        while (Take(","));
        Expect("}");

        return new("enumerated", 10, Numbers: numbers, Extensible: extension);
    }

    private void TakeSize()
    {
        if (Take("SIZE"))
        {
            SkipConstraint();
        }
    }

    private string RangeBound()
    {
        if (int.TryParse(Peek, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
        {
            position++;

            return value.ToString(CultureInfo.InvariantCulture);
        }

        return Identifier();
    }

    private void SkipConstraint()
    {
        if (!Take("("))
        {
            return;
        }

        int depth = 1;
        while (position < tokens.Length && depth != 0)
        {
            string token = tokens[position++];
            if (token == "(")
            {
                depth++;
            }
            else if (token == ")")
            {
                depth--;
            }
        }
        if (depth != 0)
        {
            throw Unexpected("')'");
        }
    }

    private string? Peek => position < tokens.Length ? tokens[position] : null;

    private bool Take(string token)
    {
        if (Peek != token)
        {
            return false;
        }

        position++;

        return true;
    }

    private void Expect(string token)
    {
        if (!Take(token))
        {
            throw Unexpected(token);
        }
    }

    private string Identifier()
    {
        if (Peek is not { Length: > 0 } token || !char.IsAsciiLetter(token[0]))
        {
            throw Unexpected("an identifier");
        }

        position++;

        return token;
    }

    private int Number()
    {
        if (!int.TryParse(Peek, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int number))
        {
            throw Unexpected("an integer");
        }

        position++;

        return number;
    }

    private InvalidOperationException Unexpected(string expected) =>
        new($"ASN.1 token {position + 1}: expected {expected}, found {Peek ?? "end of input"}.");
}
