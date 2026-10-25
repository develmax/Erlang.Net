using System.Security.Cryptography;
using System.Text.Json;

namespace BeamFixtureExport;

internal static class OnLoadObserverExporter
{
    internal static async Task Append(string oracleRoot, string existingOutput)
    {
        string executableExtension = OperatingSystem.IsWindows() ? FixtureExportNames.ExecutableExtension : "";
        string erl = Path.Combine(
            Path.GetFullPath(oracleRoot),
            FixtureExportNames.BinaryDirectory,
            FixtureExportNames.OracleExecutable + executableExtension
        );
        string erlc = Path.Combine(
            Path.GetFullPath(oracleRoot),
            FixtureExportNames.BinaryDirectory,
            FixtureExportNames.CompilerExecutable + executableExtension
        );
        string output = Path.Combine(Path.GetFullPath(existingOutput), OnLoadObserverNames.Directory);
        Directory.CreateDirectory(output);
        var results = new List<object>();
        foreach (OnLoadObserverScenario scenario in OnLoadObserverNames.Scenarios)
        {
            string directory = Path.Combine(output, scenario.Name);
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(
                Path.Combine(directory, "predeclared-contract.json"),
                JsonSerializer.Serialize(
                    new
                    {
                        scenario.Name,
                        scenario.Module,
                        scenario.Succeeds,
                        scenario.HasOldImage,
                        IndependentExpectedExpression = OnLoadObserverSources.Expected(scenario),
                        NewSourceSha256 = Hash(System.Text.Encoding.UTF8.GetBytes(OnLoadObserverSources.New(scenario))),
                        OldSourceSha256 = scenario.HasOldImage ? Hash(System.Text.Encoding.UTF8.GetBytes(OnLoadObserverSources.Old(scenario))) : null,
                        OriginalExecutionPerformed = false
                    },
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );
            string oldDirectory = Path.Combine(directory, "old");
            string newDirectory = Path.Combine(directory, "new");
            Directory.CreateDirectory(newDirectory);
            string oldBeam = Path.Combine(oldDirectory, scenario.Module + FixtureExportNames.BeamExtension);
            if (scenario.HasOldImage)
            {
                Directory.CreateDirectory(oldDirectory);
                await Compile(
                    erl,
                    erlc,
                    oldDirectory,
                    scenario.Module,
                    OnLoadObserverSources.Old(scenario)
                );
            }
            await Compile(
                erl,
                erlc,
                newDirectory,
                scenario.Module,
                OnLoadObserverSources.New(scenario)
            );
            string newBeam = Path.Combine(newDirectory, scenario.Module + FixtureExportNames.BeamExtension);
            string evaluation = OnLoadObserverSources.Evaluation(
                scenario,
                oldBeam,
                newBeam,
                directory
            );
            await File.WriteAllTextAsync(Path.Combine(directory, "observer.eval.erl"), evaluation);
            await OracleCommand.Run(erl, [OracleCommandNames.NoShell, OracleCommandNames.Evaluate, evaluation], Path.Combine(directory, "observer"));
            byte[] actual = await File.ReadAllBytesAsync(Path.Combine(directory, "actual.etf"));
            byte[] expected = await File.ReadAllBytesAsync(Path.Combine(directory, "expected.etf"));
            if (!actual.AsSpan().SequenceEqual(expected))
            {
                throw new InvalidDataException(OnLoadObserverNames.InvalidEtf);
            }
            var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal).Select(path => new
            {
                File = Path.GetRelativePath(output, path).Replace('\\', '/'),
                Sha256 = Hash(File.ReadAllBytes(path)),
                Bytes = new FileInfo(path).Length
            }).ToArray();
            results.Add(new
            {
                scenario.Name,
                scenario.Module,
                scenario.Succeeds,
                scenario.HasOldImage,
                IndependentExpectedExpression = OnLoadObserverSources.Expected(scenario),
                ActualSummarySha256 = Hash(actual),
                ExpectedSummarySha256 = Hash(expected),
                OriginalObserverExecutionPerformed = true,
                CallbackPidNoticeRetainedRaw = true,
                CallbackMonitorDownReasonRetainedRaw = true,
                CallbackMonitorEstablishedBeforeAcknowledgement = true,
                CorrelatedEarlyWorkerDeathRetainedRaw = true,
                WorkerMonitorDownRetainedRaw = true,
                FreshPendingExportInvocationPerformed = false,
                ReplacementPendingOldExportInvocationPerformed = scenario.HasOldImage,
                ExitCode = 0,
                Files = files
            });
        }
        await File.WriteAllTextAsync(
            Path.Combine(output, OnLoadObserverNames.Manifest),
            JsonSerializer.Serialize(
                new
                {
                    SourceTag = OraclePin.Tag,
                    SourceCommit = OraclePin.Commit,
                    InstalledOtpRoot = Path.GetFullPath(oracleRoot),
                    ExistingSourceBuildEvidence = "../" + FixtureExportNames.BuildEvidenceFilename,
                    ExistingSixFixtureManifest = "../" + FixtureExportNames.ManifestFilename,
                    ScenarioCount = results.Count,
                    CompilerFlags = new[] { FixtureExportNames.WarningsAsErrors, FixtureExportNames.DebugInformation },
                    Scope = "Actual isolated code:load_binary observers; no CLR on_load or loader lifecycle claim",
                    Scenarios = results
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
    }

    private static async Task Compile(
        string oracle,
        string compiler,
        string directory,
        string module,
        string source
    )
    {
        string path = Path.Combine(directory, module + FixtureExportNames.SourceExtension);
        await File.WriteAllTextAsync(path, source);
        await OracleCommand.Run(
            compiler,
            [FixtureExportNames.WarningsAsErrors, FixtureExportNames.DebugInformation, FixtureExportNames.OutputDirectoryOption, directory, path],
            Path.Combine(directory, module + FixtureExportNames.CompilationLogSuffix)
        );
        string beam = Path.Combine(directory, module + FixtureExportNames.BeamExtension);
        await File.WriteAllTextAsync(
            Path.Combine(directory, "chunks.json"),
            JsonSerializer.Serialize(BeamChunkReader.Read(await File.ReadAllBytesAsync(beam)), new JsonSerializerOptions { WriteIndented = true })
        );
        string disassembly = Path.Combine(directory, module + FixtureExportNames.DisassemblyExtension);
        string expression = $"D=beam_disasm:file({OnLoadObserverSources.String(beam)}),case D of {{beam_file,_,_,_,_,_}}->ok=file:write_file({OnLoadObserverSources.String(disassembly)},unicode:characters_to_binary(io_lib:format(\"~tp.~n\",[D]))),halt(0);_->io:format(standard_error,\"~tp~n\",[D]),halt(1) end.";
        await File.WriteAllTextAsync(Path.Combine(directory, "disassembly.eval.erl"), expression + "\n");
        await OracleCommand.Run(
            oracle,
            [OracleCommandNames.NoShell, OracleCommandNames.Evaluate, expression],
            Path.Combine(directory, module + FixtureExportNames.DisassemblyLogSuffix)
        );
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}
