using System.Text.Json;
using Erlang;

namespace Erlang.BeamBuild;

internal static class BeamApplicationOrchestrator
{
    internal static async Task Run(BeamApplicationRequest request, string requestPath)
    {
        string output = request.OutputDirectory;
        BeamApplicationPaths.RequireCreationPath(output);
        Directory.CreateDirectory(output);
        string lockPath = BeamApplicationStateStorage.RequireDestination(output, BeamApplicationBuildNames.Lock);
        using var ownership = new FileStream(
            lockPath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None
        );
        var snapshot = new BeamApplicationSnapshot(request);
        BeamApplicationState? previous = BeamApplicationStateStorage.Read(output);
        foreach (string target in snapshot.Inventory.Select(row => row.OutputPath).Append(request.Layout + "/" + BeamApplicationNames.ManifestFile))
        {
            string destination = BeamApplicationStateStorage.RequireDestination(output, target);
            if (File.Exists(destination) && !(previous?.Files.Any(row => row.TargetPath == target) ?? false))
                throw new InvalidDataException(BeamApplicationNames.Invalid);
        }
        AdmittedCompiler compiler = BeamCompilerAdmission.Admit(new BeamCompilerProvider(
            request.InstalledRoot,
            request.ProvenanceKind,
            request.AdmissionReceipt,
            request.AdmissionReceiptSha256,
            request.CompilerSha256
        ));
        string stage = Path.Combine(output, BeamApplicationBuildNames.StagePrefix + Guid.NewGuid().ToString(BeamBuildArtifactNames.GuidCompactFormat));
        BeamApplicationPaths.RequireCreationPath(stage);
        string owned = Path.Combine(stage, BeamApplicationBuildNames.OwnedInputs);
        BeamApplicationDriverIdentity driver = await BeamApplicationDriver.Prepare(
            compiler,
            request.RuntimeSha256,
            output,
            stage
        );
        string toolSha = BeamBuildStorage.HashFile(typeof(BeamApplicationOrchestrator).Assembly.Location);

        byte[] Dependencies() => JsonSerializer.SerializeToUtf8Bytes(Directory.GetFiles(AppContext.BaseDirectory, BeamBuildArtifactNames.ManagedDependencyGlob)
            .Order(StringComparer.Ordinal).Select(path => new { Path = Path.GetFileName(path), Sha256 = BeamBuildStorage.HashFile(path) }).ToArray());
        byte[] dependencies = Dependencies();
        string configuration = BeamBuildStorage.Hash(JsonSerializer.SerializeToUtf8Bytes(new
        {
            Schema = BeamApplicationBuildNames.StateSchema,
            Inputs = snapshot.InputSha256,
            Driver = driver,
            ToolAssemblySha256 = toolSha,
            ToolDependenciesSha256 = BeamBuildStorage.Hash(dependencies),
            RemovedEnvironment = BeamBuildNames.RemovedEnvironment,
            EmulatorFlags = BeamBuildNames.EmulatorFlagsValue,
            CompilerServer = BeamBuildNames.CompilerServerDisabled
        }));

        void VerifyInputs(bool verifyOwned)
        {
            snapshot.VerifyLive();
            if (verifyOwned)
                snapshot.VerifyOwned(owned);
            BeamApplicationDriver.Validate(driver);
            if (BeamBuildStorage.Hash(BeamApplicationRead.Bytes(requestPath, BeamApplicationNames.MaximumRequestBytes)) != request.RequestSha256
                || BeamBuildStorage.HashFile(typeof(BeamApplicationOrchestrator).Assembly.Location) != toolSha
                || !Dependencies().AsSpan().SequenceEqual(dependencies)
                || !BeamBuildStorage.Hash(BeamApplicationRead.Bytes(request.AdmissionReceipt, BeamApplicationNames.MaximumFileBytes))
                    .Equals(request.AdmissionReceiptSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(BeamApplicationNames.Invalid);
        }
        IReadOnlyList<string> modules = request.Modules.Select(module => module.Module).ToArray();
        IReadOnlyList<string> configs = request.Configuration.Select(file => file.LogicalPath).ToArray();
        if (previous is not null && previous.ConfigurationSha256 == configuration && previous.RequestSha256 == request.RequestSha256
            && previous.ApplicationName == request.ApplicationName && previous.Version == request.Version
            && previous.Files.All(file => File.Exists(file.Path))
            && previous.NativeEvidence.ToolAssemblySha256 == toolSha
            && previous.NativeEvidence.DriverSourceSha256 == driver.SourceSha256
            && previous.NativeEvidence.DriverBeamSha256 == driver.BeamSha256
            && previous.NativeEvidence.RuntimeSha256.Equals(request.RuntimeSha256, StringComparison.OrdinalIgnoreCase)
            && previous.NativeEvidence.CompilerSha256.Equals(request.CompilerSha256, StringComparison.OrdinalIgnoreCase)
            && previous.NativeEvidence.AdmissionReceiptSha256.Equals(request.AdmissionReceiptSha256, StringComparison.OrdinalIgnoreCase)
            && Matches(previous, snapshot, request))
        {
            _ = BeamApplicationParsedEvidenceValidator.Validate(
                BeamApplicationRead.Bytes(previous.NativeEvidence.ParsedEtfPath,
                BeamApplicationNames.MaximumFileBytes),
                request.ApplicationName,
                request.Version,
                modules,
                configs
            );
            VerifyInputs(false);
            Console.WriteLine(BeamApplicationBuildNames.StateSchema + ": cached");

            return;
        }
        Directory.CreateDirectory(stage);
        snapshot.WriteOwnedTree(owned);
        string nativeRequest = Path.Combine(stage, BeamApplicationBuildNames.RequestFile);
        string result = Path.Combine(stage, BeamApplicationBuildNames.ResultFile);
        BeamApplicationInputIdentity descriptor = snapshot.Inventory.Single(row => row.Kind == BeamApplicationNames.DescriptorKind);
        Term native = Term.Tuple(
            Term.A(BeamApplicationBuildNames.NativeRequest),
            Term.String(BeamDeclaredPaths.Relative(owned, snapshot.OwnedPath(owned, descriptor))),
            Cons.From(snapshot.Configuration.Select((row, index) => (Term)Term.Tuple(Term.String(configs[index]),
                Term.String(BeamDeclaredPaths.Relative(owned, snapshot.OwnedPath(owned, row)))))),
            Term.String(result)
        );
        await File.WriteAllBytesAsync(nativeRequest, ExternalTermFormat.Encode(native));
        string nativeRequestSha = BeamBuildStorage.HashFile(nativeRequest);
        await BeamCompilerProcess.RunApplication(
            driver.Runtime,
            driver.Directory,
            nativeRequest,
            owned,
            stage,
            Path.Combine(stage, BeamBuildArtifactNames.CompilerLogStem)
        );
        if (BeamBuildStorage.HashFile(nativeRequest) != nativeRequestSha)
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        BeamApplicationParsedEvidence parsed = BeamApplicationParsedEvidenceValidator.Validate(
            BeamApplicationRead.Bytes(result, BeamApplicationNames.MaximumFileBytes),
            request.ApplicationName,
            request.Version,
            modules,
            configs
        );
        var evidence = new BeamApplicationNativeEvidence(
            request.AdmissionReceiptSha256.ToUpperInvariant(),
            request.CompilerSha256.ToUpperInvariant(),
            request.RuntimeSha256.ToUpperInvariant(),
            driver.SourceSha256,
            driver.BeamSha256,
            parsed.ResultSha256,
            result,
            toolSha
        );
        string manifest = Path.Combine(stage, BeamApplicationBuildNames.StagedManifest);
        BeamBuildStorage.AtomicJson(
            manifest,
            new
            {
                Schema = BeamApplicationBuildNames.PublicSchema,
                request.ApplicationName,
                request.Version,
                Modules = parsed.ModuleOrder,
                ConfigurationOrder = parsed.OrderedConfigNames,
                Files = snapshot.Inventory.Select(row => new
                {
                    Path = row.OutputPath[(request.Layout.Length + 1)..],
                    Kind = BeamApplicationStateStorage.Kind(row.Kind),
                    row.Sha256,
                    Bytes = row.Size
                }).ToArray(),
                Parser = new
                {
                    evidence.AdmissionReceiptSha256,
                    evidence.CompilerSha256,
                    evidence.RuntimeSha256,
                    evidence.DriverSourceSha256,
                    evidence.DriverBeamSha256,
                    evidence.ParsedEtfSha256,
                    evidence.ToolAssemblySha256
                }
            }
        );
        var sources = snapshot.Inventory.Select(row => (Source: snapshot.OwnedPath(owned, row),
            Output: new BeamApplicationOutput(
                BeamApplicationStateStorage.RequireDestination(output, row.OutputPath),
                row.OutputPath,
                BeamApplicationStateStorage.Kind(row.Kind),
                row.Sha256,
                row.Size
            ))).ToList();
        string manifestTarget = request.Layout + "/" + BeamApplicationNames.ManifestFile;
        sources.Add((manifest, new BeamApplicationOutput(
            BeamApplicationStateStorage.RequireDestination(output, manifestTarget),
            manifestTarget,
            BeamApplicationBuildNames.ManifestKind,
            BeamBuildStorage.HashFile(manifest),
            new FileInfo(manifest).Length
        )));
        VerifyInputs(true);
        // Revalidate the accepted ownership record immediately before any promotion.
        BeamApplicationState? current = BeamApplicationStateStorage.Read(output);
        if (!JsonSerializer.SerializeToUtf8Bytes(current).AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(previous)))
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        foreach (var source in sources)
        {
            BeamApplicationOutput? old = previous?.Files.SingleOrDefault(row => row.TargetPath == source.Output.TargetPath);
            if (old is null && File.Exists(source.Output.Path))
                throw new InvalidDataException(BeamApplicationNames.Invalid);
        }
        foreach (var source in sources)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(source.Output.Path)!);
            if (!File.Exists(source.Output.Path) || BeamBuildStorage.HashFile(source.Output.Path) != source.Output.Sha256)
            {
                string temporary = source.Output.Path + BeamBuildArtifactNames.TemporarySeparator + Guid.NewGuid().ToString(BeamBuildArtifactNames.GuidCompactFormat);
                try
                {
                    File.Copy(source.Source, temporary, false);
                    File.Move(temporary, source.Output.Path, true);
                }
                finally
                {
                    if (File.Exists(temporary)) File.Delete(temporary);
                }
            }
        }
        foreach (BeamApplicationOutput stale in previous?.Files ?? [])
            if (!sources.Any(source => source.Output.TargetPath == stale.TargetPath) && File.Exists(stale.Path))
                File.Delete(stale.Path);
        BeamBuildStorage.AtomicJson(
            Path.Combine(output, BeamApplicationBuildNames.State),
            new BeamApplicationState(
                BeamApplicationBuildNames.StateSchema,
                request.ApplicationName,
                request.Version,
                request.RequestSha256,
                configuration,
                sources.Select(source => source.Output).ToArray(),
                evidence
            )
        );
        Console.WriteLine(BeamApplicationBuildNames.StateSchema + ": parsed");
    }

    private static bool Matches(BeamApplicationState state, BeamApplicationSnapshot snapshot, BeamApplicationRequest request)
        => state.Files.Length == snapshot.Inventory.Count + 1
            && snapshot.Inventory.All(input => state.Files.Any(file => file.TargetPath == input.OutputPath
                && file.Kind == BeamApplicationStateStorage.Kind(input.Kind) && file.Sha256 == input.Sha256 && file.Bytes == input.Size))
            && state.Files.Count(file => file.TargetPath == request.Layout + "/" + BeamApplicationNames.ManifestFile
                && file.Kind == BeamApplicationBuildNames.ManifestKind) == 1;
}
