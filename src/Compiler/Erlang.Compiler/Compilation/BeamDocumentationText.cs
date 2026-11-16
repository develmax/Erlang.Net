using System.Globalization;
using System.Text;

namespace Erlang.Compiler;

internal static class BeamDocumentationText
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
    // OTP string:trim/1 uses Pattern_White_Space, including the CRLF grapheme.
    private const string PatternWhitespace = "\t\n\v\f\r \u0085\u200e\u200f\u2028\u2029";

    internal static (string? Signature, Term Value) Read(Term value, (string Name, int Arity)? target = null)
    {
        if (value is Atom { Name: "false" or "hidden" })
        {
            return (null, Term.A("hidden"));
        }

        string text = Trim(ReadText(value));
        string? signature = null;
        if (target is { } key)
        {
            int newline = text.IndexOf('\n');
            string first = newline < 0 ? text : text[..newline];
            if (MatchesSignature(first, key))
            {
                signature = first;
                text = newline < 0 ? string.Empty : Trim(text[(newline + 1)..]);
            }
        }

        return (signature, new MapTerm([
            new KeyValuePair<Term, Term>(Binary("en"), Binary(text))
        ]));
    }

    internal static string ReadText(Term value)
    {
        if (value is BitString { IsBinary: true } binary)
        {
            try
            {
                return StrictUtf8.GetString(binary.ToArray());
            }
            catch (DecoderFallbackException error)
            {
                throw new NotSupportedException(BeamEmissionFormat.Unsupported, error);
            }
        }
        if (value is not (Cons or Nil))
        {
            throw new NotSupportedException(BeamEmissionFormat.Unsupported);
        }

        var text = new StringBuilder();
        foreach (Term item in Cons.Items(value))
        {
            if (item is not Integer integer || integer.Value < 0 || integer.Value > int.MaxValue
                || !Rune.TryCreate((int)integer.Value, out var rune))
            {
                throw new NotSupportedException(BeamEmissionFormat.Unsupported);
            }
            text.Append(rune.ToString());
        }

        return text.ToString();
    }

    private static string Trim(string text)
    {
        int start = 0;
        int end = 0;
        for (int index = 0; index < text.Length;)
        {
            int length = StringInfo.GetNextTextElementLength(text.AsSpan(index));
            bool whitespace = length == 1 && PatternWhitespace.Contains(text[index])
                || length == 2 && text.AsSpan(index, length).SequenceEqual("\r\n");
            index += length;
            if (!whitespace)
            {
                end = index;
            }
            else if (end == 0)
            {
                start = index;
            }
        }

        return end == 0 ? string.Empty : text[start..end];
    }

    private static bool MatchesSignature(string line, (string Name, int Arity) target)
    {
        if (!line.Contains('('))
        {
            return false;
        }
        try
        {
            return new Parser(line).ParseExpression() is Expr.Call { Module: null, DynamicRemote: false } call
                && call.Function == target.Name && call.Arguments.Count == target.Arity;
        }
        catch (CompileException)
        {
            return false;
        }
    }

    private static BitString Binary(string text) => new(Encoding.UTF8.GetBytes(text));
}
