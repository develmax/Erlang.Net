using System.Text.Json;

namespace Erlang.BeamBuild;

internal static class BeamEppDriver
{
    internal static async Task<BeamEppDriverIdentity> Prepare(
        BeamBuildRequest request,
        AdmittedCompiler compiler,
        string output,
        string stage
    )
    {
        string executable = Path.Combine(
            compiler.InstalledRoot,
            BeamCompilerLayoutNames.BinaryDirectory,
            OperatingSystem.IsWindows() ? BeamEppDriverNames.RuntimeWindows : BeamEppDriverNames.RuntimeUnix
        );
        string runtimeSha = BeamBuildStorage.HashFile(executable);
        if (!runtimeSha.Equals(request.RuntimeSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(BeamBuildNames.Admission);
        AdmittedCompiler runtime = compiler with { Executable = executable, Sha256 = runtimeSha };
        using Stream resource = typeof(BeamEppDriver).Assembly.GetManifestResourceStream(BeamEppDriverNames.Resource)
            ?? throw new InvalidDataException(BeamBuildNames.Admission);
        using var memory = new MemoryStream();
        await resource.CopyToAsync(memory);
        byte[] source = memory.ToArray();
        string sourceSha = BeamBuildStorage.Hash(source);
        string configuration = BeamBuildStorage.Hash(JsonSerializer.SerializeToUtf8Bytes(new
        {
            Identity = BeamEppDriverNames.BootstrapIdentity,
            Compiler = compiler,
            RuntimeSha256 = runtimeSha,
            SourceSha256 = sourceSha,
            RemovedEnvironment = BeamBuildNames.RemovedEnvironment,
            EmulatorFlags = BeamBuildNames.EmulatorFlagsValue,
            CompilerServer = BeamBuildNames.CompilerServerDisabled
        }));
        string cacheDirectory = Path.Combine(output, BeamEppDriverNames.CacheDirectory);
        BeamDeclaredPaths.RequireNoReparse(cacheDirectory);
        Directory.CreateDirectory(cacheDirectory);
        string beamName = BeamEppDriverNames.Module + BeamBuildNames.BeamExtension;
        string cacheBeam = Path.Combine(cacheDirectory, beamName);
        string statePath = Path.Combine(cacheDirectory, BeamEppDriverNames.CacheState);
        foreach (string path in Directory.EnumerateFileSystemEntries(cacheDirectory))
        {
            BeamDeclaredPaths.RequireNoReparse(path);
            if (Directory.Exists(path) || Path.GetFileName(path) is not (BeamEppDriverNames.CacheState)
                && Path.GetFileName(path) != beamName)
                throw new InvalidDataException(BeamBuildNames.Admission);
        }
        BeamEppDriverCache? previous = File.Exists(statePath)
            ? JsonSerializer.Deserialize<BeamEppDriverCache>(File.ReadAllBytes(statePath)) : null;
        if (previous is not null && previous.ConfigurationSha256 == configuration && File.Exists(cacheBeam)
            && BeamBuildStorage.HashFile(cacheBeam) == previous.BeamSha256)
        {
            var cached = new BeamEppDriverIdentity(
                runtime,
                cacheDirectory,
                sourceSha,
                previous.BeamSha256,
                configuration
            );
            Validate(cached);

            return cached;
        }
        string bootstrap = Path.Combine(stage, BeamEppDriverNames.BootstrapDirectory);
        Directory.CreateDirectory(bootstrap);
        string sourcePath = Path.Combine(bootstrap, BeamEppDriverNames.Module + BeamBuildNames.SourceExtension);
        await File.WriteAllBytesAsync(sourcePath, source);
        await BeamCompilerProcess.Run(
            compiler,
            sourcePath,
            bootstrap,
            Path.Combine(bootstrap, BeamBuildArtifactNames.CompilerLogStem)
        );
        if (BeamBuildStorage.HashFile(sourcePath) != sourceSha)
            throw new InvalidDataException(BeamBuildNames.Admission);
        string generated = Path.Combine(bootstrap, beamName);
        string beamSha = BeamBuildStorage.HashFile(generated);
        File.Move(generated, cacheBeam, true);
        BeamBuildStorage.AtomicJson(statePath, new BeamEppDriverCache(configuration, beamSha));
        var prepared = new BeamEppDriverIdentity(
            runtime,
            cacheDirectory,
            sourceSha,
            beamSha,
            configuration
        );
        Validate(prepared);

        return prepared;
    }

    internal static void Validate(BeamEppDriverIdentity driver)
    {
        CompilerExecutionClosure.Validate(driver.Runtime);
        BeamDeclaredPaths.RequireNoReparse(driver.Directory);
        if (BeamBuildStorage.HashFile(driver.Runtime.Executable) != driver.Runtime.Sha256
            || BeamBuildStorage.HashFile(Path.Combine(driver.Directory, BeamEppDriverNames.Module + BeamBuildNames.BeamExtension)) != driver.BeamSha256)
            throw new InvalidDataException(BeamBuildNames.Admission);
        string[] entries = Directory.GetFileSystemEntries(driver.Directory);
        foreach (string path in entries)
            BeamDeclaredPaths.RequireNoReparse(path);
        if (entries.Length != 2 || entries.Any(path => Directory.Exists(path)
            || Path.GetFileName(path) != BeamEppDriverNames.CacheState
                && Path.GetFileName(path) != BeamEppDriverNames.Module + BeamBuildNames.BeamExtension))
            throw new InvalidDataException(BeamBuildNames.Admission);
    }

    internal static async Task<BeamEppCompilation> Compile(
        BeamEppDriverIdentity driver,
        BeamBuildRequest request,
        BeamDeclaredInputSnapshot snapshot,
        string ownedInputs,
        BeamBuildSource source,
        string moduleStage
    )
    {
        snapshot.ValidateOwnedTree(ownedInputs);
        Validate(driver);
        string output = Path.Combine(moduleStage, BeamEppDriverNames.OutputDirectory);
        string requestFile = Path.Combine(moduleStage, BeamEppDriverNames.RequestFile);
        BeamBuildStorage.AtomicJson(
            requestFile,
            new
            {
                Source = BeamDeclaredPaths.Relative(request.SourceRoot!, source.Path),
                source.Module,
                Includes = request.IncludeDirectories,
                request.Defines,
                DeclaredFiles = snapshot.Inventory.Select(file => new { Path = file.LogicalPath, file.Sha256 }).ToArray(),
                Output = output
            }
        );
        string requestSha = BeamBuildStorage.HashFile(requestFile);
        await BeamCompilerProcess.RunEpp(
            driver.Runtime,
            driver.Directory,
            requestFile,
            ownedInputs,
            output,
            Path.Combine(moduleStage, BeamBuildArtifactNames.CompilerLogStem)
        );
        snapshot.ValidateOwnedTree(ownedInputs);
        Validate(driver);
        if (BeamBuildStorage.HashFile(requestFile) != requestSha)
            throw new InvalidDataException(BeamBuildNames.Invalid);
        string beam = Path.Combine(output, source.Module + BeamBuildNames.BeamExtension);
        BeamEppNativeEvidence evidence = BeamEppEvidenceValidator.Validate(
            requestFile,
            requestSha,
            Path.Combine(output, BeamEppDriverNames.ReportFile),
            ownedInputs,
            BeamDeclaredPaths.Relative(request.SourceRoot!, source.Path),
            source.Module,
            request.IncludeDirectories!,
            request.Defines!,
            snapshot.Inventory,
            beam
        );

        return new BeamEppCompilation(beam, evidence);
    }
}
