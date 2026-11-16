using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public sealed record Token(
    string Kind,
    string Text,
    int Start,
    int End
)
{
    internal int PreprocessorLine { get; init; }
    internal string? PreprocessorFile { get; init; }
    internal bool HasExplicitFileLocation { get; init; }
    internal int? PreprocessorColumnStart { get; init; }
    internal bool IsCharacterLiteral { get; init; }
    internal bool IsStringSigil { get; init; }
    internal int SigilDelimiterOffset { get; init; }
}
