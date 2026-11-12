namespace Erlang.Compiler;

public abstract record ComprehensionQualifier
{
    public int SourceLine { get; init; }

    public sealed record Generator(Pattern Pattern, Expr Source, bool Strict = false) : ComprehensionQualifier
    {
        internal int GeneratorLine { get; init; }
        internal string? GeneratorFile { get; init; }
    }
    public sealed record BinaryGenerator(BitPattern Pattern, Expr Source, bool Strict = false) : ComprehensionQualifier;
    public sealed record MapGenerator(Pattern Pattern, Expr Source, bool Strict = false) : ComprehensionQualifier;
    public sealed record Filter(Expr Expression) : ComprehensionQualifier;
    public sealed record Zip(IReadOnlyList<ComprehensionQualifier> Generators) : ComprehensionQualifier;
}
