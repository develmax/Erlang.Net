namespace Erlang.BeamBuild;

internal static class BeamDeclaredInputNames
{
    internal const string Policy = "DeclaredIncludesV1";
    internal const string Schema = "declared-input-tree-v1";
    internal const string FilenamePolicy = "owned-root-cwd-stable-relative-v1";
    internal const string HeaderExtension = ".hrl";
    internal const string AtomValue = "Atom";
    internal const string IntegerValue = "Integer";
    internal const string StringValue = "String";
    internal const string MacroNamePattern = @"\A[A-Za-z_][A-Za-z0-9_@]*\z";
    internal const string StringToken = "string";
    internal const string KeywordToken = "keyword";
    internal const string EndToken = "eof";
    internal const string Include = "include";
    internal const int MaximumAtomCodepoints = 255;
    internal static readonly HashSet<string> Attributes = new(StringComparer.Ordinal)
    {
        BeamSourcePolicyNames.Module,
        BeamSourcePolicyNames.Export,
        Include,
        "define",
        "undef",
        "ifdef",
        "ifndef",
        "if",
        "elif",
        "else",
        "endif"
    };
}
