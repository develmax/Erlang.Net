using System.Text.Json;
using System.Text.Json.Serialization;

namespace Erlang.BeamBuild;

internal static class BeamApplicationDriver
{
    internal static async Task<BeamApplicationDriverIdentity> Prepare(
        AdmittedCompiler compiler,
        string expectedRuntimeSha256,
        string output,
        string stage
    )
    {
        CompilerExecutionClosure.Validate(compiler);
        string executable = Path.Combine(
            compiler.InstalledRoot,
            BeamCompilerLayoutNames.BinaryDirectory,
            OperatingSystem.IsWindows() ? BeamEppDriverNames.RuntimeWindows : BeamEppDriverNames.RuntimeUnix
        );
        string runtimeSha = BeamBuildStorage.HashFile(executable);
        if (!runtimeSha.Equals(expectedRuntimeSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(BeamBuildNames.Admission);
        AdmittedCompiler runtime = compiler with { Executable = executable, Sha256 = runtimeSha };
        using Stream resource = typeof(BeamApplicationDriver).Assembly.GetManifestResourceStream(BeamApplicationDriverNames.Resource)
            ?? throw new InvalidDataException(BeamBuildNames.Admission);
        using var memory = new MemoryStream();
        await resource.CopyToAsync(memory);
        byte[] source = memory.ToArray();
        string sourceSha = BeamBuildStorage.Hash(source);
        string configuration = BeamBuildStorage.Hash(JsonSerializer.SerializeToUtf8Bytes(new
        {
            Identity = BeamApplicationDriverNames.BootstrapIdentity,
            Compiler = compiler,
            RuntimeSha256 = runtimeSha,
            SourceSha256 = sourceSha,
            RemovedEnvironment = BeamBuildNames.RemovedEnvironment,
            EmulatorFlags = BeamBuildNames.EmulatorFlagsValue,
            CompilerServer = BeamBuildNames.CompilerServerDisabled
        }));
        string directory = Path.Combine(output, BeamApplicationDriverNames.CacheDirectory);
        BeamApplicationPaths.RequireCreationPath(directory);
        Directory.CreateDirectory(directory);
        string beamName = BeamApplicationDriverNames.Module + BeamBuildNames.BeamExtension;
        string beam = Path.Combine(directory, beamName);
        string state = Path.Combine(directory, BeamApplicationDriverNames.CacheState);
        foreach (string path in Directory.EnumerateFileSystemEntries(directory))
        {
            BeamApplicationPaths.RequireActualCase(path);
            if (Directory.Exists(path) || Path.GetFileName(path) != beamName
                && Path.GetFileName(path) != BeamApplicationDriverNames.CacheState)
                throw new InvalidDataException(BeamBuildNames.Admission);
        }
        BeamApplicationDriverCache? previous = File.Exists(state)
            ? JsonSerializer.Deserialize<BeamApplicationDriverCache>(
                BeamApplicationRead.Bytes(state, BeamApplicationNames.MaximumRequestBytes),
                new JsonSerializerOptions
                {
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
                }
            ) : null;
        if (previous is not null && (!System.Text.RegularExpressions.Regex.IsMatch(previous.ConfigurationSha256 ?? string.Empty, BeamBuildValidationNames.Sha256)
            || !System.Text.RegularExpressions.Regex.IsMatch(previous.BeamSha256 ?? string.Empty, BeamBuildValidationNames.Sha256)))
            throw new InvalidDataException(BeamBuildNames.Admission);
        if (File.Exists(state) && previous is null
            || File.Exists(beam) && (previous is null || BeamBuildStorage.HashFile(beam) != previous.BeamSha256))
            throw new InvalidDataException(BeamBuildNames.Admission);
        if (previous is not null && previous.ConfigurationSha256 == configuration && File.Exists(beam)
            && BeamBuildStorage.HashFile(beam) == previous.BeamSha256)
        {
            var retained = new BeamApplicationDriverIdentity(
                runtime,
                directory,
                sourceSha,
                previous.BeamSha256,
                configuration
            );
            Validate(retained);

            return retained;
        }
        string bootstrap = Path.Combine(stage, BeamApplicationDriverNames.BootstrapDirectory);
        BeamApplicationPaths.RequireCreationPath(bootstrap);
        Directory.CreateDirectory(bootstrap);
        string sourcePath = Path.Combine(bootstrap, BeamApplicationDriverNames.Module + BeamBuildNames.SourceExtension);
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
        BeamDeclaredPaths.RequireNoReparse(beam);
        BeamDeclaredPaths.RequireNoReparse(state);
        File.Move(generated, beam, true);
        BeamBuildStorage.AtomicJson(state, new BeamApplicationDriverCache(configuration, beamSha));
        var result = new BeamApplicationDriverIdentity(
            runtime,
            directory,
            sourceSha,
            beamSha,
            configuration
        );
        Validate(result);

        return result;
    }

    internal static void Validate(BeamApplicationDriverIdentity driver)
    {
        CompilerExecutionClosure.Validate(driver.Runtime);
        BeamApplicationPaths.RequireActualCase(driver.Directory);
        string beamName = BeamApplicationDriverNames.Module + BeamBuildNames.BeamExtension;
        string beam = Path.Combine(driver.Directory, beamName);
        if (BeamBuildStorage.HashFile(driver.Runtime.Executable) != driver.Runtime.Sha256
            || BeamBuildStorage.HashFile(beam) != driver.BeamSha256)
            throw new InvalidDataException(BeamBuildNames.Admission);
        string[] entries = Directory.GetFileSystemEntries(driver.Directory);
        foreach (string path in entries)
            BeamApplicationPaths.RequireActualCase(path);
        if (entries.Length != 2 || entries.Any(path => Directory.Exists(path)
            || Path.GetFileName(path) != beamName && Path.GetFileName(path) != BeamApplicationDriverNames.CacheState))
            throw new InvalidDataException(BeamBuildNames.Admission);
        var cache = JsonSerializer.Deserialize<BeamApplicationDriverCache>(
            File.ReadAllBytes(Path.Combine(
            driver.Directory,
            BeamApplicationDriverNames.CacheState
        )),
            new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow }
        );
        if (cache is null || cache.ConfigurationSha256 != driver.ConfigurationSha256 || cache.BeamSha256 != driver.BeamSha256)
            throw new InvalidDataException(BeamBuildNames.Admission);
    }
}
