// SPDX-License-Identifier: Apache-2.0
// String sigil rules follow OTP-29.1.1 erl_scan.erl and erl_parse.yrl.

using System.Text;

namespace Erlang.Compiler;

internal static class StringSigilLexer
{
    internal static bool TryRead(string text, int start, ref int offset, out Token? token)
    {
        token = null;
        int delimiter = offset;
        char prefix = '\0';
        if (delimiter < text.Length && text[delimiter] is 'b' or 'B' or 's' or 'S')
        {
            prefix = text[delimiter++];
        }
        if (delimiter >= text.Length)
        {
            return false;
        }

        char open = text[delimiter];
        char close = open switch
        {
            '(' => ')',
            '[' => ']',
            '{' => '}',
            '<' => '>',
            '/' or '|' or '#' or '`' or '\'' or '"' => open,
            _ => '\0'
        };
        if (close == '\0')
        {
            return false;
        }

        bool triple = open == '"' && TripleQuotedStringLexer.StartsAt(text, delimiter);
        bool escapes = prefix is 'b' or 's' || prefix == '\0' && !triple;
        string value;
        offset = delimiter + 1;
        if (triple)
        {
            value = TripleQuotedStringLexer.Read(text, delimiter, ref offset).Text;
            if (escapes)
            {
                value = DecodeEscapes(value, start);
            }
        }
        else
        {
            var builder = new StringBuilder();
            while (offset < text.Length && text[offset] != close)
            {
                char character = text[offset++];
                if (escapes && character == '\\' && offset < text.Length)
                {
                    builder.Append(char.ConvertFromUtf32(CharacterLiteralLexer.ReadEscape(text, ref offset, offset - 1)));
                }
                else
                {
                    builder.Append(character);
                }
            }
            if (offset == text.Length)
            {
                throw Invalid(LexerDiagnostics.UnterminatedQuotedLiteral, start);
            }

            offset++;
            value = builder.ToString();
        }

        if (offset < text.Length && (char.IsLetterOrDigit(text[offset]) || text[offset] is '_' or '@'))
        {
            throw Invalid(LexerDiagnostics.IllegalSigilSuffix, offset);
        }
        if (offset < text.Length && text[offset] == '"')
        {
            throw Invalid(LexerDiagnostics.StringSeparatorRequired, offset);
        }
        for (int index = 0; index < value.Length;)
        {
            if (!Rune.TryGetRuneAt(value, index, out var rune) || rune.Value is 0xfffe or 0xffff)
            {
                throw Invalid(LexerDiagnostics.IllegalQuotedUnicode, start);
            }

            index += rune.Utf16SequenceLength;
        }

        string kind = prefix is 's' or 'S' ? LexerTokenKinds.String : LexerTokenKinds.BinaryString;
        token = new Token(kind, value, start, offset) {
            IsStringSigil = true,
            SigilDelimiterOffset = delimiter - start
        };
        return true;
    }

    private static string DecodeEscapes(string value, int start)
    {
        var decoded = new StringBuilder();
        for (int offset = 0; offset < value.Length;)
        {
            char character = value[offset++];
            if (character == '\\')
            {
                decoded.Append(char.ConvertFromUtf32(CharacterLiteralLexer.ReadEscape(value, ref offset, start)));
            }
            else
            {
                decoded.Append(character);
            }
        }

        return decoded.ToString();
    }

    private static CompileException Invalid(string message, int offset) =>
        new(CompilerDiagnosticCodes.InvalidLiteral, message, offset);
}
