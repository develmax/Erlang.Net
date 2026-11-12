namespace Erlang.Compiler;

public sealed record BeamReleaseBuildProfile(
    string CompilerVersion,
    string SourcePath,
    string WorkingDirectory,
    IReadOnlyList<string> IncludeDirectories,
    IReadOnlyList<string> WarningOptions,
    string? InsertedVersion = null,
    string InsertedAttributeName = "vsn",
    int? WarningsBeforeInsertion = null,
    IReadOnlyDictionary<string, Term>? RecordedDefinitions = null,
    IReadOnlyList<Term>? RecordedCompileOptions = null,
    bool EmitAbstractCode = true,
    bool RecordSourceFileInLineTable = false,
    IReadOnlyList<Term>? RecordedDebugOptions = null,
    string? RecordedSourceFile = null,
    IReadOnlyDictionary<string, string>? RecordedIncludeFiles = null,
    IReadOnlyDictionary<string, Term>? PreprocessorDefinitions = null,
    bool AllowIncompleteAbstractCode = false,
    bool EmitDocumentation = true
);
