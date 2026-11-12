// SPDX-License-Identifier: Apache-2.0
// Based on OTP-29.1.1 erl_scan.erl, copyright Ericsson AB.

using System.Text;

namespace Erlang.Compiler;

internal static class TripleQuotedStringLexer
{
    private const int MinimumQuotes = 3;

    internal static bool StartsAt(string text, int start) => text.AsSpan(start).StartsWith("\"\"\"", StringComparison.Ordinal);

    internal static Token Read(string text, int start, ref int offset)
    {
        int delimiterEnd = start + MinimumQuotes;
        while (delimiterEnd < text.Length && text[delimiterEnd] == '"')
        {
            delimiterEnd++;
        }

        var delimiter = text.AsSpan(start, delimiterEnd - start);
        int firstNewline = text.IndexOf('\n', delimiterEnd);
        if (firstNewline < 0)
        {
            throw Invalid(LexerDiagnostics.UnterminatedQuotedLiteral, start);
        }
        for (int index = delimiterEnd; index < firstNewline; index++)
        {
            if (!LexerWhitespace.Is(text[index]))
            {
                throw Invalid(LexerDiagnostics.TripleQuotedOpeningLine, index);
            }
        }

        int contentStart = firstNewline + 1;
        for (int line = contentStart; line < text.Length;)
        {
            int quote = line;
            while (quote < text.Length && text[quote] != '\n' && LexerWhitespace.Is(text[quote]))
            {
                quote++;
            }
            if (text.AsSpan(quote).StartsWith(delimiter, StringComparison.Ordinal))
            {
                string value = StripIndentation(text, contentStart, line, text.AsSpan(line, quote - line));
                offset = quote + delimiter.Length;

                return new(
                    LexerTokenKinds.String,
                    value,
                    start,
                    offset
                );
            }

            int newline = text.IndexOf('\n', quote);
            if (newline < 0)
            {
                break;
            }
            line = newline + 1;
        }

        throw Invalid(LexerDiagnostics.UnterminatedQuotedLiteral, start);
    }

    private static string StripIndentation(
        string text,
        int start,
        int end,
        ReadOnlySpan<char> indent
    )
    {
        var result = new StringBuilder();
        for (int line = start; line < end;)
        {
            int newline = text.IndexOf('\n', line);
            int lineEnd = newline > line && text[newline - 1] == '\r' ? newline - 1 : newline;
            var content = text.AsSpan(line, lineEnd - line);
            if (!content.IsEmpty)
            {
                if (!content.StartsWith(indent, StringComparison.Ordinal))
                {
                    throw Invalid(LexerDiagnostics.TripleQuotedIndentation, line);
                }
                result.Append(content[indent.Length..]);
            }
            if (newline + 1 < end)
            {
                result.Append(text.AsSpan(lineEnd, newline + 1 - lineEnd));
            }
            line = newline + 1;
        }

        string value = result.ToString();
        for (int index = 0; index < value.Length;)
        {
            if (!Rune.TryGetRuneAt(value, index, out var rune) || rune.Value is 0xfffe or 0xffff)
            {
                throw Invalid(LexerDiagnostics.IllegalQuotedUnicode, start);
            }
            index += rune.Utf16SequenceLength;
        }

        return value;
    }

    private static CompileException Invalid(string message, int offset) => new(CompilerDiagnosticCodes.InvalidLiteral, message, offset);
}
