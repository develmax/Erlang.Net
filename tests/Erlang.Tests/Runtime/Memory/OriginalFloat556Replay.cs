using System.Reflection;
using System.Text.Json;
using static Erlang.Tests.OriginalFloat556Names;

namespace Erlang.Tests;

internal static class OriginalFloat556Replay
{
    internal static async Task Run(
        string repository,
        string admission,
        string admissionSha,
        string metadataCompletion,
        string metadataSha,
        bool owned,
        string output
    )
    {
        repository = System.IO.Path.GetFullPath(repository);
        output = System.IO.Path.GetFullPath(output);
        if (Directory.Exists(output))
            throw new InvalidDataException(Failure);
        OriginalFloat556Admission receipt = OriginalFloat556AdmissionReader.Read(repository, admission, admissionSha);
        RuntimeNativeMetadataImage metadata = OriginalFloat556Metadata.Read(
            repository,
            receipt,
            metadataCompletion,
            metadataSha
        );
        Directory.CreateDirectory(output);
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned, metadata);
        bool actualOwned = (bool)typeof(ProcessRuntime).GetProperty(
            OwnedGuest506Names.PrivateOption,
            BindingFlags.Instance | BindingFlags.NonPublic
        )!.GetValue(runtime)!;
        Require(actualOwned == owned);
        await DebugSame420HostAccess.Initialize(runtime);
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapDebugSharing425.Register(runtime, registry);
        });
        PreparedBeamImage subject = runtime.Modules.PrepareBeam(
            File.ReadAllBytes(OriginalFloat556AdmissionReader.Resolve(repository, receipt.BeamPath)),
            new BeamDependencySelection(new[] { Core, Debug })
        );
        PreparedBeamImage worker = runtime.Modules.PrepareBeam(
            File.ReadAllBytes(OriginalFloat556AdmissionReader.Resolve(repository, receipt.WorkerBeamPath)),
            new BeamDependencySelection(new[] { Core, Subject })
        );
        Require(subject.Name == Subject && worker.Name == Worker
            && !subject.HasOnLoad && !worker.HasOnLoad
            && subject.NativePrimitiveExports.Count == 0 && worker.NativePrimitiveExports.Count == 0
            && subject.OriginalBeamSha256 == receipt.BeamSha256
            && worker.OriginalBeamSha256 == receipt.WorkerBeamSha256);
        Require(runtime.Modules.PublishPreparedBeam(subject, worker).Status == BeamPublicationStatus.Published);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle controller = runtime.Spawn(async context =>
        {
            try
            {
                Require(ReferenceEquals(runtime.Modules.SourceOwner(Subject, OriginalFloat556Names.Run, 0), subject)
                    && ReferenceEquals(runtime.Modules.SourceOwner(Worker, OriginalFloat556Names.Run, 1), worker));
                Term actual = await runtime.Modules.Call(
                    context,
                    Worker,
                    OriginalFloat556Names.Run,
                    Term.A(Subject)
                );
                Term minor = Term.Tuple(Term.A(MinorVersion), Term.I(2));
                var raw = (BitString)await runtime.Modules.Call(
                    context,
                    Core,
                    Encode,
                    actual,
                    Term.List(minor)
                );
                var deterministic = (BitString)await runtime.Modules.Call(
                    context,
                    Core,
                    Encode,
                    actual,
                    Term.List(Term.A(Deterministic), minor)
                );
                byte[] actualRaw = raw.ToArray();
                byte[] actualDeterministic = deterministic.ToArray();
                await File.WriteAllBytesAsync(System.IO.Path.Combine(output, RawFile), actualRaw);
                await File.WriteAllBytesAsync(System.IO.Path.Combine(output, DeterministicFile), actualDeterministic);
                byte[] original = File.ReadAllBytes(OriginalFloat556AdmissionReader.Resolve(repository, receipt.DeterministicPath));
                byte[] originalRaw = File.ReadAllBytes(OriginalFloat556AdmissionReader.Resolve(repository, receipt.RawPath));
                bool rawEqual = actualRaw.AsSpan().SequenceEqual(originalRaw);
                bool deterministicEqual = actualDeterministic.AsSpan().SequenceEqual(original);
                Require(ReferenceEquals(runtime.Modules.SourceOwner(Subject, OriginalFloat556Names.Run, 0), subject)
                    && ReferenceEquals(runtime.Modules.SourceOwner(Worker, OriginalFloat556Names.Run, 1), worker));
                await File.WriteAllTextAsync(
                    System.IO.Path.Combine(output, ComparisonFile),
                    JsonSerializer.Serialize(new
                    {
                        Passed = rawEqual && deterministicEqual,
                        WholeDefaultEtfEqual = rawEqual,
                        WholeDeterministicEtfEqual = deterministicEqual,
                        OwnedProfile = actualOwned,
                        SourcePin = Pin,
                        SubjectBeamSha256 = receipt.BeamSha256,
                        WorkerBeamSha256 = receipt.WorkerBeamSha256,
                        WorkerSourceSha256 = receipt.WorkerSourceSha256,
                        EffectiveNativeMetadataSha256 = metadata.OriginalBeamSha256,
                        WholeRawValuesAndStacksCompared = true,
                        Comparison = "BothWholeDefaultAndDeterministicETFBytes",
                        RawEtfRetained = true,
                        RawEtfRewritten = false,
                        FullContractsClosed = 0
                    })
                );
                Require(rawEqual && deterministicEqual);
                completed.TrySetResult();
            }
            catch (Exception error)
            {
                try
                {
                    await File.WriteAllTextAsync(System.IO.Path.Combine(output, FailureFile), error.ToString());
                    completed.TrySetException(error);
                }
                catch (Exception evidenceError)
                {
                    completed.TrySetException(new AggregateException(error, evidenceError));
                }
            }

            return Nil.Value;
        });
        await completed.Task.WaitAsync(Deadline);
        await controller.Completion.WaitAsync(Deadline);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidDataException(Failure);
    }
}
