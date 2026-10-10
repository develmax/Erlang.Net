namespace Erlang.Compiler;

internal sealed record ComprehensionGenerator(
    Pattern Pattern,
    Expr Source,
    bool Strict,
    bool Binary = false,
    bool Map = false
)
{
    public static ComprehensionGenerator? From(ComprehensionQualifier qualifier) => qualifier switch
    {
        ComprehensionQualifier.Generator g => new(g.Pattern, g.Source, g.Strict),
        ComprehensionQualifier.BinaryGenerator g => new(
            g.Pattern,
            g.Source,
            g.Strict,
            Binary: true
        ),
        ComprehensionQualifier.MapGenerator g => new(
            g.Pattern,
            g.Source,
            g.Strict,
            Map: true
        ),
        _ => null
    };

    public static IEnumerable<ComprehensionGenerator> Flatten(IEnumerable<ComprehensionQualifier> qualifiers)
    {
        foreach (var qualifier in qualifiers)
        {
            if (qualifier is ComprehensionQualifier.Zip zip)
            {
                foreach (var generator in Flatten(zip.Generators))
                    yield return generator;
            }
            else if (From(qualifier) is { } generator)
                yield return generator;
        }
    }
}
