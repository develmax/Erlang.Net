using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BeamFixtureExport;

internal static class LoadNifOpcodeFixtureExporter
{
    internal static async Task Append(string oracleRoot, string existingOutput)
    {
        string root = Path.GetFullPath(oracleRoot);
        string parent = Path.GetFullPath(existingOutput);
        string output = Path.Combine(parent, LoadNifOpcodeFixtureNames.Directory);
        if (Directory.Exists(output))
            throw new IOException(FixtureExportDiagnostics.ExistingOutput);
        Directory.CreateDirectory(output);
        string extension = OperatingSystem.IsWindows() ? FixtureExportNames.ExecutableExtension : string.Empty;
        string erl = Path.Combine(root, FixtureExportNames.BinaryDirectory, FixtureExportNames.OracleExecutable + extension);
        string erlc = Path.Combine(root, FixtureExportNames.BinaryDirectory, FixtureExportNames.CompilerExecutable + extension);
        string source = Path.Combine(output, LoadNifOpcodeFixtureNames.Module + FixtureExportNames.SourceExtension);
        await File.WriteAllTextAsync(source, LoadNifOpcodeFixtureNames.Source.Replace(LoadNifOpcodeFixtureNames.CarriageReturn + LoadNifOpcodeFixtureNames.LineFeed, LoadNifOpcodeFixtureNames.LineFeed) + LoadNifOpcodeFixtureNames.LineFeed, new UTF8Encoding(false));
        string[] roles = [LoadNifOpcodeFixtureNames.NonTailRole, LoadNifOpcodeFixtureNames.LastRole, LoadNifOpcodeFixtureNames.OnlyRole];
        var declared = new
        {
            SourcePin = OraclePin.Commit,
            SourceTag = OraclePin.Tag,
            LoadNifOpcodeFixtureNames.Module,
            SourceSha256 = Hash(await File.ReadAllBytesAsync(source)),
            ExpectedRoles = roles,
            LoadNifOpcodeFixtureNames.Purpose,
            OriginalCompilationPerformed = false,
            OriginalBodyExecutionPerformed = false,
            NativeLoadingExecutionPerformed = false
        };
        await File.WriteAllTextAsync(Path.Combine(output, LoadNifOpcodeFixtureNames.Predeclared), JsonSerializer.Serialize(declared, new JsonSerializerOptions { WriteIndented = true }));
        string[] arguments = [FixtureExportNames.WarningsAsErrors, FixtureExportNames.DebugInformation, FixtureExportNames.OutputDirectoryOption, output, source];
        await OracleCommand.Run(erlc, arguments, Path.Combine(output, LoadNifOpcodeFixtureNames.CompilerPrefix));
        string beamPath = Path.Combine(output, LoadNifOpcodeFixtureNames.Module + FixtureExportNames.BeamExtension);
        byte[] beam = await File.ReadAllBytesAsync(beamPath);
        string functions = Path.Combine(output, LoadNifOpcodeFixtureNames.FunctionsEtf);
        string disassembly = Path.Combine(output, LoadNifOpcodeFixtureNames.Disassembly);
        string expression = LoadNifOpcodeFixtureNames.DisassemblyExpressionPrefix + ErlangString(beamPath)
            + LoadNifOpcodeFixtureNames.DisassemblyExpressionMiddle + ErlangString(functions)
            + LoadNifOpcodeFixtureNames.DisassemblyExpressionEtf + ErlangString(disassembly)
            + LoadNifOpcodeFixtureNames.DisassemblyExpressionEnd;
        await File.WriteAllTextAsync(Path.Combine(output, LoadNifOpcodeFixtureNames.DisassemblyEvaluation), expression + LoadNifOpcodeFixtureNames.LineFeed);
        await OracleCommand.Run(erl, [OracleCommandNames.NoShell, OracleCommandNames.Evaluate, expression], Path.Combine(output, LoadNifOpcodeFixtureNames.DisassemblyPrefix));
        var fixture = new FixtureDefinition(LoadNifOpcodeFixtureNames.Module, LoadNifOpcodeFixtureNames.Source, LoadNifOpcodeFixtureNames.Purpose, null);
        FixtureMetadataEvidence metadata = await FixtureMetadata.Run(erl, output, fixture, beamPath);
        var files = new List<object>();
        foreach (string path in Directory.GetFiles(output).Order(StringComparer.Ordinal))
        {
            byte[] contents = await File.ReadAllBytesAsync(path);
            files.Add(new { File = Path.GetFileName(path), Bytes = contents.Length, Sha256 = Hash(contents) });
        }
        var manifest = new
        {
            SourcePin = OraclePin.Commit,
            SourceTag = OraclePin.Tag,
            InstalledVersion = OraclePin.Version,
            InstalledOtpRoot = root,
            LoadNifOpcodeFixtureNames.Module,
            LoadNifOpcodeFixtureNames.Purpose,
            OriginalCompilationPerformed = true,
            FileOnlyDisassemblyPerformed = true,
            OriginalModuleLoadPerformed = false,
            OriginalBodyExecutionPerformed = false,
            NativeLoadingExecutionPerformed = false,
            OpcodeShapesVerified = false,
            OpcodeShapeStatus = LoadNifOpcodeFixtureNames.ShapeStatus,
            ExpectedRoles = roles,
            CompilerArguments = arguments,
            CompilerExecutableSha256 = Hash(await File.ReadAllBytesAsync(erlc)),
            OracleExecutableSha256 = Hash(await File.ReadAllBytesAsync(erl)),
            ParentManifestSha256 = Hash(await File.ReadAllBytesAsync(Path.Combine(parent, FixtureExportNames.ManifestFilename))),
            SourceBuildEvidenceSha256 = Hash(await File.ReadAllBytesAsync(Path.Combine(parent, FixtureExportNames.BuildEvidenceFilename))),
            SourceBuildLogSha256 = Hash(await File.ReadAllBytesAsync(Path.Combine(parent, FixtureExportNames.BuildLogFilename))),
            Chunks = BeamChunkReader.Read(beam),
            Metadata = metadata,
            Files = files
        };
        await File.WriteAllTextAsync(Path.Combine(output, LoadNifOpcodeFixtureNames.Manifest), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static string ErlangString(string value) => LoadNifOpcodeFixtureNames.Quote
        + value.Replace(LoadNifOpcodeFixtureNames.Backslash, LoadNifOpcodeFixtureNames.EscapedBackslash)
            .Replace(LoadNifOpcodeFixtureNames.Quote, LoadNifOpcodeFixtureNames.EscapedQuote)
            .Replace(LoadNifOpcodeFixtureNames.CarriageReturn, LoadNifOpcodeFixtureNames.EscapedCarriageReturn)
            .Replace(LoadNifOpcodeFixtureNames.LineFeed, LoadNifOpcodeFixtureNames.EscapedLineFeed)
        + LoadNifOpcodeFixtureNames.Quote;
}
