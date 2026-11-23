namespace Erlang.Compiler;

public static partial class CodeGeneration
{
    /// <summary>Assemble BEAM instructions and optionally attach abstract forms from a companion source file.</summary>
    public static byte[] CompileBeamAssemblyFile(
        string assemblyPath,
        BeamReleaseBuildProfile buildProfile,
        string? abstractSourcePath = null,
        string? otpLibraryDirectory = null,
        IReadOnlyList<string>? includeDirectories = null
    )
    {
        var forms = new Parser(File.ReadAllText(assemblyPath)).ParseAssemblyTerms();
        string name = Path.GetFileNameWithoutExtension(assemblyPath);
        BeamMacroPreprocessor? companion = abstractSourcePath is null
            ? null
            : OpenBeamSource(
                Path.GetFullPath(abstractSourcePath),
                otpLibraryDirectory,
                includeDirectories,
                buildProfile
            );

        return new BeamModuleCompiler(name, maybeEnabled: false).EmitAssembly(
            forms,
            buildProfile,
            companion,
            Path.GetFileName(abstractSourcePath)
        );
    }
}
