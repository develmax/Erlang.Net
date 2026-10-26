using System.Text.Json;
using System.Text.RegularExpressions;
using Erlang;

namespace Erlang.BeamBuild;

internal static class BeamBuildOrchestrator
{
    internal static async Task Run(BeamBuildRequest request)
    {
        AdmittedCompiler compiler = BeamCompilerAdmission.Admit(request);
        string output = Path.GetFullPath(request.OutputDirectory);
        Directory.CreateDirectory(output);
        using var ownership = new FileStream(
            Path.Combine(output, BeamBuildNames.Lock),
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None
        );
        string manifest = Path.Combine(output, BeamBuildNames.Manifest);
        BeamBuildState? previous = File.Exists(manifest)
            ? JsonSerializer.Deserialize<BeamBuildState>(File.ReadAllBytes(manifest)) : null;
        if (previous is not null && (previous.Outputs is null || previous.Outputs.Any(row =>
            row is null || !Regex.IsMatch(row.Module ?? string.Empty, BeamBuildValidationNames.ModuleIdentifier))
            || previous.Outputs.Select(row => row.Module).Distinct(StringComparer.Ordinal).Count() != previous.Outputs.Length))
            throw new InvalidDataException(BeamBuildNames.Invalid);
        string tool = typeof(BeamBuildOrchestrator).Module.ModuleVersionId.ToString();
        string stage = Path.Combine(output, BeamBuildArtifactNames.StagePrefix + Guid.NewGuid().ToString(BeamBuildArtifactNames.GuidCompactFormat));
        BeamDeclaredInputSnapshot? declared = request.SourcePolicy is null ? null : new BeamDeclaredInputSnapshot(request);
        string ownedInputs = Path.Combine(stage, BeamEppDriverNames.InputsDirectory);
        BeamEppDriverIdentity? driver = null;
        if (declared is not null)
        {
            driver = await BeamEppDriver.Prepare(
                request,
                compiler,
                output,
                stage
            );
        }
        string configuration = BeamBuildStorage.Hash(JsonSerializer.SerializeToUtf8Bytes(new
        {
            Compiler = compiler,
            ToolModuleVersionId = tool,
            ToolDependencies = Directory.GetFiles(AppContext.BaseDirectory, BeamBuildArtifactNames.ManagedDependencyGlob).Order(StringComparer.Ordinal).Select(path => new { Path = Path.GetFileName(path), Sha256 = BeamBuildStorage.HashFile(path) }).ToArray(),
            Arguments = new[] { BeamCompilerArgumentNames.OutputDirectory, BeamCompilerArgumentNames.OwnedStage, BeamCompilerArgumentNames.OwnedSource },
            SourcePolicy = request.SourcePolicy ?? BeamSourcePolicyNames.Identity,
            DeclaredInputs = declared?.ConfigurationSha256,
            Driver = driver,
            RemovedEnvironment = BeamBuildNames.RemovedEnvironment,
            EmulatorFlags = BeamBuildNames.EmulatorFlagsValue,
            CompilerServer = BeamBuildNames.CompilerServerDisabled
        }));
        var outputs = new List<BeamBuildOutput>();
        var promotions = new List<(string Source, string Destination)>();
        int executed = 0;
        bool inputsMaterialized = false;
        foreach (BeamBuildSource source in request.Sources.OrderBy(value => value.Module, StringComparer.Ordinal))
        {
            byte[]? owned = declared is null ? BeamBuildSources.Snapshot(source) : null;
            string sourceHash = declared is null ? BeamBuildStorage.Hash(owned!)
                : declared.Inventory.Single(file => file.LogicalPath == BeamDeclaredPaths.Relative(request.SourceRoot!, source.Path)).Sha256;
            string destination = Path.Combine(output, source.Module + BeamBuildNames.BeamExtension);
            BeamBuildOutput? old = previous?.Outputs.SingleOrDefault(value => value.Module == source.Module);
            if (old is null && File.Exists(destination))
                throw new InvalidDataException(BeamBuildNames.Invalid);
            if (old is not null && old.SourcePath == Path.GetFullPath(source.Path)
                && old.SourceSha256 == sourceHash && old.ConfigurationSha256 == configuration
                && File.Exists(destination) && BeamBuildStorage.HashFile(destination) == old.OutputSha256
                && (declared is null || BeamEppCachedEvidence.Validate(
                    old.NativeEvidence,
                    request,
                    declared,
                    source,
                    destination
                )))
            {
                RequireModule(File.ReadAllBytes(destination), source.Module);
                outputs.Add(old);
                continue;
            }
            Directory.CreateDirectory(stage);
            if (declared is not null && !inputsMaterialized)
            {
                declared.WriteOwnedTree(ownedInputs);
                inputsMaterialized = true;
            }
            string moduleStage = declared is null ? Path.Combine(stage, source.Module)
                : Path.Combine(stage, BeamEppDriverNames.ModulesDirectory, source.Module);
            Directory.CreateDirectory(moduleStage);
            string generated;
            BeamEppNativeEvidence? evidence = null;
            if (declared is null)
            {
                string snapshot = Path.Combine(moduleStage, source.Module + BeamBuildNames.SourceExtension);
                await File.WriteAllBytesAsync(snapshot, owned!);
                await BeamCompilerProcess.Run(
                    compiler,
                    snapshot,
                    moduleStage,
                    Path.Combine(moduleStage, BeamBuildArtifactNames.CompilerLogStem)
                );
                generated = Path.Combine(moduleStage, source.Module + BeamBuildNames.BeamExtension);
            }
            else
            {
                BeamEppCompilation compilation = await BeamEppDriver.Compile(
                    driver!,
                    request,
                    declared,
                    ownedInputs,
                    source,
                    moduleStage
                );
                generated = compilation.BeamPath;
                evidence = compilation.Evidence;
            }
            executed++;
            string[] generatedFiles = Directory.GetFiles(Path.GetDirectoryName(generated)!, "*" + BeamBuildNames.BeamExtension);
            if (generatedFiles.Length != 1 || generatedFiles[0] != generated)
                throw new InvalidDataException(BeamBuildNames.Invalid);
            byte[] beam = await File.ReadAllBytesAsync(generated);
            RequireModule(beam, source.Module);
            outputs.Add(new(
                source.Module,
                Path.GetFullPath(source.Path),
                sourceHash,
                configuration,
                BeamBuildStorage.Hash(beam),
                evidence
            ));
            promotions.Add((generated, destination));
        }
        CompilerExecutionClosure.Validate(compiler);
        if (declared is not null)
        {
            if (inputsMaterialized)
                declared.ValidateOwnedTree(ownedInputs);
            declared.ValidateLiveInputs();
            BeamEppDriver.Validate(driver!);
        }
        // Preflight every stale owned destination BEFORE promotion or deletion.
        foreach (BeamBuildOutput stale in previous?.Outputs ?? [])
            if (!outputs.Any(value => value.Module == stale.Module))
            {
                string stalePath = Path.Combine(output, stale.Module + BeamBuildNames.BeamExtension);
                if (File.Exists(stalePath) && BeamBuildStorage.HashFile(stalePath) != stale.OutputSha256)
                    throw new InvalidDataException(BeamBuildNames.Invalid);
            }
        // All child compilations and module checks finish before any final output changes.
        foreach (var promotion in promotions)
            File.Move(promotion.Source, promotion.Destination, true);
        foreach (BeamBuildOutput stale in previous?.Outputs ?? [])
            if (!outputs.Any(value => value.Module == stale.Module))
            {
                string oldOutput = Path.Combine(output, stale.Module + BeamBuildNames.BeamExtension);
                if (File.Exists(oldOutput) && BeamBuildStorage.HashFile(oldOutput) == stale.OutputSha256)
                    File.Delete(oldOutput);
                else if (File.Exists(oldOutput))
                    throw new InvalidDataException(BeamBuildNames.Invalid);
            }
        BeamBuildStorage.AtomicJson(
            manifest,
            new BeamBuildState(
                compiler.Sha256,
                compiler.EvidenceSha256,
                compiler.ProviderEvidenceSha256,
                tool,
                outputs.ToArray(),
                declared is null ? null : new BeamBuildPreprocessing(
                    BeamDeclaredInputNames.Policy,
                    declared.ConfigurationSha256,
                    driver!.Runtime.Sha256,
                    driver.SourceSha256,
                    driver.BeamSha256,
                    declared.Inventory,
                    declared.Includes
                )
            )
        );
        if (Directory.Exists(stage))
            BeamBuildStorage.AtomicJson(
                Path.Combine(stage, BeamBuildArtifactNames.CompletionFile),
                new { Status = BeamBuildArtifactNames.BuildOnlyStatus, ExecutedCompilers = executed, Skipped = outputs.Count - executed, Manifest = manifest, ConfigurationSha256 = configuration }
            );
        Console.WriteLine(JsonSerializer.Serialize(new { ExecutedCompilers = executed, Skipped = outputs.Count - executed, Manifest = manifest }));
    }

    private static void RequireModule(byte[] bytes, string expected)
    {
        BeamArtifactInfo prepared = BeamArtifactInspector.Inspect(bytes);
        if (prepared.Name != expected || prepared.HasOnLoad)
            throw new InvalidDataException(BeamBuildNames.Invalid);
    }
}
