using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Erlang.Differential;

internal static class AllocatorProfileObservation
{
    internal static async Task<int> Run(string oracle, string report)
    {
        CompiledModuleCase fixture = SystemAllocatorQueryCases.FullInfoBackendObservation;
        OracleProcessOutput? version = null;
        OracleProcessOutput? native = null;
        CompiledModuleStreamResult? sdk = null;
        Term? original = null;
        OracleProcessOutput? memoryNative = null;
        CompiledModuleStreamResult? memorySdk = null;
        Term? memoryOriginal = null;
        bool memoryNativeValid = false;
        bool memorySdkValid = false;
        bool versionVerified = false;
        bool timedOut = false;
        bool nativeValid = false;
        bool sdkValid = false;
        string? failure = null;
        try
        {
            version = await RunProcess(
                oracle,
                AllocatorProfileObservationNames.VersionCommand,
                CancellationToken.None,
                (output, timeout) =>
            {
                version = output;
                timedOut |= timeout;
            }
            );
            if (version.ExitCode != 0 || version.StandardOutput.Trim() != AllocatorProfileObservationNames.BaselineVersion)
                throw new InvalidOperationException(OracleDiagnostics.VersionMismatch(version.StandardOutput.Trim()));
            versionVerified = true;
            await foreach (var observation in OracleExecutionPipeline.Run(
                new[] { fixture },
                item => CompiledModuleProtocol.Command(item.Source, item.CallerContext, item.CompilerPolicy),
                async (command, cancellation) =>
                {
                    native = await RunProcess(
                        oracle,
                        command,
                        cancellation,
                        (output, timeout) =>
                    {
                        native = output;
                        timedOut |= timeout;
                    }
                    );
                    if (native.ExitCode != 0)
                        throw new InvalidOperationException(AllocatorProfileObservationNames.Failure);

                    return native.StandardOutput;
                },
                _ => { }
            ))
            {
                original = OracleResultProtocol.Decode(observation.Output);
                nativeValid = NativeContract(original);
            }
            using var compiled = GeneratedModuleCompiler.Compile(fixture.Source);
            sdk = await CompiledModuleExecution.RunCaptured(compiled, fixture.CallerContext);
            sdkValid = sdk.Result.Equals(fixture.Expected);
            await foreach (var memoryObservation in OracleExecutionPipeline.Run(
                new[] { ClrMemoryCaseNames.RawSnapshotSource },
                source => CompiledModuleProtocol.Command(source),
                async (command, cancellation) =>
                {
                    memoryNative = await RunProcess(
                        oracle,
                        command,
                        cancellation,
                        (output, timeout) =>
                    {
                        memoryNative = output;
                        timedOut |= timeout;
                    }
                    );
                    if (memoryNative.ExitCode != 0)
                        throw new InvalidOperationException(AllocatorProfileObservationNames.Failure);

                    return memoryNative.StandardOutput;
                },
                _ => { }
            ))
            {
                memoryOriginal = OracleResultProtocol.Decode(memoryObservation.Output);
                memoryNativeValid = ClrMemoryObservationContracts.Validate(memoryOriginal);
            }
            using var memoryCompiled = GeneratedModuleCompiler.Compile(ClrMemoryCaseNames.RawSnapshotSource);
            memorySdk = await CompiledModuleExecution.RunCaptured(memoryCompiled);
            memorySdkValid = ClrMemoryObservationContracts.Validate(memorySdk.Result);
            if (!nativeValid || !sdkValid || !memoryNativeValid || !memorySdkValid)
                throw new InvalidOperationException(AllocatorProfileObservationNames.Failure);
        }
        catch (Exception exception)
        {
            failure = exception.ToString();
        }
        bool passed = failure is null && versionVerified && nativeValid && sdkValid && memoryNativeValid && memorySdkValid;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report))!);
        await File.WriteAllTextAsync(
            report,
            JsonSerializer.Serialize(
                new
                {
                    Mode = AllocatorProfileObservationNames.Command,
                    AllocatorProfileObservationNames.BaselineVersion,
                    AllocatorProfileObservationNames.PinnedCommit,
                    AllocatorProfileObservationNames.CommitVerification,
                    VersionVerified = versionVerified,
                    Complete = passed,
                    ContractValidationPassed = passed,
                    DeclaredClrAdaptation = true,
                    RawMatchClaimed = false,
                    RawResultsEqual = original is not null && sdk is not null ? (bool?)original.Equals(sdk.Result) : null,
                    NativeContractValidated = nativeValid,
                    SdkContractValidated = sdkValid,
                    OriginalExpectedBackend = AllocatorProfileObservationNames.NativeBackend,
                    SdkExpectedBackend = SystemAllocatorQueryCaseNames.SdkBackend,
                    AllocatorProfileObservationNames.NativeExpectation,
                    AllocatorProfileObservationNames.SdkExpectation,
                    fixture.Name,
                    fixture.Source,
                    SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fixture.Source))),
                    OriginalOutcome = original?.ToString(),
                    SdkOutcome = sdk?.Result.ToString(),
                    SdkExpectedOutcome = fixture.Expected.ToString(),
                    VersionProcess = version,
                    OriginalProcess = native,
                    OriginalStandardOutputHex = native is null ? null : Convert.ToHexString(native.StandardOutputBytes.Span),
                    OriginalStandardErrorHex = native is null ? null : Convert.ToHexString(native.StandardErrorBytes.Span),
                    SdkStandardOutputHex = sdk?.StandardOutputHex,
                    SdkStandardErrorHex = sdk?.StandardErrorHex,
                    MemoryProfile = new
                    {
                        Source = ClrMemoryCaseNames.RawSnapshotSource,
                        SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ClrMemoryCaseNames.RawSnapshotSource))),
                        OriginalScope = ClrMemoryCaseNames.OriginalScope,
                        SdkScope = ClrMemoryCaseNames.SdkScope,
                        DeclaredClrAdaptation = true,
                        RawMatchClaimed = false,
                        RawResultsEqual = memoryOriginal is not null && memorySdk is not null ? (bool?)memoryOriginal.Equals(memorySdk.Result) : null,
                        KeyOrder = ClrMemoryCaseNames.OrderedKeys,
                        OriginalObservedMemoryKeyCount = memoryNativeValid ? (int?)ClrMemoryCaseNames.KeyCount : null,
                        SdkObservedMemoryKeyCount = memorySdkValid ? (int?)ClrMemoryCaseNames.KeyCount : null,
                        OriginalNineKeyNumericPartitionValidated = memoryNativeValid,
                        SdkNineKeyNumericPartitionValidated = memorySdkValid,
                        OriginalOutcome = memoryOriginal?.ToString(),
                        SdkOutcome = memorySdk?.Result.ToString(),
                        OriginalProcess = memoryNative,
                        OriginalStandardOutputHex = memoryNative is null ? null : Convert.ToHexString(memoryNative.StandardOutputBytes.Span),
                        OriginalStandardErrorHex = memoryNative is null ? null : Convert.ToHexString(memoryNative.StandardErrorBytes.Span),
                        SdkStandardOutputHex = memorySdk?.StandardOutputHex,
                        SdkStandardErrorHex = memorySdk?.StandardErrorHex
                    },
                    TimedOut = timedOut,
                    InfrastructureOrContractError = failure
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );

        return passed ? 0 : 1;
    }

    private static bool NativeContract(Term outcome)
    {
        if (outcome is not TupleTerm outer || outer.Items.Count != AllocatorProfileObservationNames.OutcomeSize ||
            !outer.Items[AllocatorProfileObservationNames.Tag].Equals(Term.A(OracleOutcomeTags.Success)) ||
            outer.Items[AllocatorProfileObservationNames.OutcomeValue] is not TupleTerm value || value.Items.Count != AllocatorProfileObservationNames.ObservationSize)
            return false;
        Term enabled = Term.Tuple(Term.A(SystemAllocatorQueryCaseNames.Enabled), Term.A(SystemAllocatorQueryCaseNames.True));
        Term backend = Term.Tuple(Term.A(SystemAllocatorQueryCaseNames.Backend), Term.A(AllocatorProfileObservationNames.NativeBackend));

        return value.Items[AllocatorProfileObservationNames.Tag].Equals(Term.A(SystemAllocatorQueryCaseNames.FullOptionsTag)) &&
            value.Items[AllocatorProfileObservationNames.AtomEnabled].Equals(enabled) &&
            value.Items[AllocatorProfileObservationNames.ListEnabled].Equals(enabled) &&
            value.Items[AllocatorProfileObservationNames.AtomBackend].Equals(backend) &&
            value.Items[AllocatorProfileObservationNames.ListBackend].Equals(backend) &&
            TuningTail(value.Items[AllocatorProfileObservationNames.AtomTail]) &&
            TuningTail(value.Items[AllocatorProfileObservationNames.ListTail]);
    }

    private static bool TuningTail(Term tail)
    {
        Term[] options = Cons.Items(tail).ToArray();
        if (options.Length > AllocatorProfileObservationNames.MaximumTuningOptions)
            return false;
        foreach (Term option in options)
        {
            if (option is not TupleTerm pair || pair.Items.Count != AllocatorProfileObservationNames.OptionSize ||
                pair.Items[AllocatorProfileObservationNames.OptionKey] is not Atom key ||
                key.Name is not (AllocatorProfileObservationNames.TrimThreshold or AllocatorProfileObservationNames.TopPad) ||
                pair.Items[AllocatorProfileObservationNames.OptionValue] is not Integer number || number.Value.Sign < 0)
                return false;
        }
        if (options.Length == AllocatorProfileObservationNames.MaximumTuningOptions)
            return ((TupleTerm)options[AllocatorProfileObservationNames.FirstTuningOption]).Items[AllocatorProfileObservationNames.OptionKey].Equals(Term.A(AllocatorProfileObservationNames.TrimThreshold)) &&
                ((TupleTerm)options[AllocatorProfileObservationNames.SecondTuningOption]).Items[AllocatorProfileObservationNames.OptionKey].Equals(Term.A(AllocatorProfileObservationNames.TopPad));

        return true;
    }

    private static async Task<OracleProcessOutput> RunProcess(
        string oracle,
        string command,
        CancellationToken cancellation,
        Action<OracleProcessOutput, bool> captured
    )
    {
        var start = new ProcessStartInfo(oracle)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string argument in new[] { AllocatorProfileObservationNames.NoShell, AllocatorProfileObservationNames.NoInput, AllocatorProfileObservationNames.Evaluate, command })
            start.ArgumentList.Add(argument);
        using var child = Process.Start(start)!;

        async Task<byte[]> Read(Stream stream)
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);

            return buffer.ToArray();
        }

        Task<byte[]> stdout = Read(child.StandardOutput.BaseStream);
        Task<byte[]> stderr = Read(child.StandardError.BaseStream);
        bool expired = false;
        try
        {
            await child.WaitForExitAsync(cancellation).WaitAsync(TimeSpan.FromSeconds(OracleExecutionLimits.ProcessTimeoutSeconds), cancellation);
        }
        catch (Exception exception) when (exception is TimeoutException or OperationCanceledException)
        {
            expired = exception is TimeoutException;
            if (!child.HasExited)
                child.Kill(true);
            await child.WaitForExitAsync();
            await Task.WhenAll(stdout, stderr);
            byte[] failedOutput = await stdout;
            byte[] failedError = await stderr;
            captured(
                new(Encoding.UTF8.GetString(failedOutput), Encoding.UTF8.GetString(failedError), child.ExitCode)
                {
                    StandardOutputBytes = failedOutput,
                    StandardErrorBytes = failedError
                },
                expired
            );
            if (expired)
                throw new TimeoutException(AllocatorProfileObservationNames.Timeout, exception);
            throw;
        }
        byte[] output = await stdout;
        byte[] error = await stderr;
        var result = new OracleProcessOutput(Encoding.UTF8.GetString(output), Encoding.UTF8.GetString(error), child.ExitCode)
        {
            StandardOutputBytes = output,
            StandardErrorBytes = error
        };
        captured(result, expired);

        return result;
    }
}
