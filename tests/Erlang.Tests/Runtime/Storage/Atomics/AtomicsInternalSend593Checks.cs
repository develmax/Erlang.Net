using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class AtomicsInternalSend593Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task Run(
        string repository,
        string admission,
        string admissionSha,
        string metadataCompletion,
        string metadataSha,
        bool originalBeamWorker
    )
    {
        var receipt = OriginalAtomics588AdmissionReader.Read(repository, admission, admissionSha);
        var metadata = OriginalFloat556Metadata.ReadProvider(
            repository,
            receipt.Worker.ProviderBeforePath,
            metadataCompletion,
            metadataSha
        );
        const string module = AtomicsInternalSend593Names.Module;
        string fixture = Path.Combine(repository, AtomicsInternalSend593Names.Fixture);
        using var wrapper = GeneratedModuleCompiler.Compile(File.ReadAllText(fixture), Path.GetFileName(fixture));
        using var sourceWorker = originalBeamWorker ? null : GeneratedModuleCompiler.Compile(
            PrepareSourceWorker(File.ReadAllText(OriginalAtomics588AdmissionReader.Resolve(repository, receipt.Worker.WorkerSourcePath))),
            AtomicsInternalSend593Names.WorkerFile
        );
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? observer = null;
        ProcessHandle? child = null;
        object[] savedEntries = [];
        var failures = new List<Exception>();
        try
        {
            runtime = OwnedGuest506RuntimeFactory.Create(true, metadata);
            await DebugSame420HostAccess.Initialize(runtime);
            await runtime.InitializeModulesAsync(registry =>
{
    RuntimeBootstrapDebugSharing425.Register(runtime, registry);
    RuntimeBootstrapAtomics.Register(runtime, registry);
});
            PreparedBeamImage[] stocks = OriginalAtomics588Images.Prepare(runtime, repository, receipt);
            byte[] subjectBytes = File.ReadAllBytes(OriginalAtomics588AdmissionReader.Resolve(repository, receipt.Worker.BeamPath));
            var subject = runtime.Modules.PrepareBeam(subjectBytes, new BeamDependencySelection(DynamicApplyBoundary520Imports.Read(subjectBytes)));
            byte[] workerBytes = File.ReadAllBytes(OriginalAtomics588AdmissionReader.Resolve(repository, receipt.Worker.WorkerBeamPath));
            var worker = originalBeamWorker ? runtime.Modules.PrepareBeam(workerBytes, new BeamDependencySelection(DynamicApplyBoundary520Imports.Read(workerBytes))) : null;
            Require(subject.Name == OriginalAtomics588Names.Subject && subject.OriginalBeamSha256 == receipt.Worker.BeamSha256 && !subject.HasOnLoad && subject.NativePrimitiveExports.Count == 0);
            if (worker is not null) Require(worker.Name == OriginalReference563Names.Worker && worker.OriginalBeamSha256 == receipt.Worker.WorkerBeamSha256 && !worker.HasOnLoad && worker.NativePrimitiveExports.Count == 0);
            Require(runtime.Modules.PublishPreparedBeam(worker is null ? [.. stocks, subject] : [.. stocks, subject, worker]).Status == BeamPublicationStatus.Published);
            sourceWorker?.Definition.Register(runtime.Modules);
            wrapper.Definition.Register(runtime.Modules);
            observer = new(runtime);
            ProcessContext observing = await observer.Ready.WaitAsync(OriginalReference563Names.Deadline);
            ProcessRuntime actual = runtime;
            Pid parent = observing.Self;
            child = runtime.Spawn(async context => await actual.Modules.Call(
                context,
                module,
                "run",
                parent
            ));
            await observer.InvokeAsync(async context => await context.ReceiveAsync(
                message => message is TupleTerm { Items.Count: 2 } t
                && t.Items[0].Equals(Term.A(AtomicsInternalSend593Names.Held)) && t.Items[1].Equals(child.Pid) ? message : null,
                OriginalReference563Names.Deadline
            ));
            // Diagnostic references only. No host carrier/native pin is minted,
            // and no original resource term crosses the guest->host boundary.
            object resources = runtime.ReferenceResources;
            object gate = resources.GetType().GetField(AtomicsInternalSend593Names.Gate, Members)!.GetValue(resources)!;
            lock (gate)
            {
                var entries = (IDictionary)resources.GetType().GetField(AtomicsInternalSend593Names.Entries, Members)!.GetValue(resources)!;
                savedEntries = entries.Values.Cast<object>().ToArray();
            }
            Require(savedEntries.Length != 0 && runtime.AtomicsStorage413.LiveArrays != 0);
            Console.Error.WriteLine($"{AtomicsInternalSend593Names.Held}: entries={savedEntries.Length}, arrays={runtime.AtomicsStorage413.LiveArrays}, originalBeamWorker={originalBeamWorker}");
            runtime.Send(child.Pid, Term.A(AtomicsInternalSend593Names.Drop));
            Require((await child.Completion.WaitAsync(OriginalReference563Names.Deadline)).Equals(Term.A(AtomicsInternalSend593Names.NormalExit)));
            Console.Error.WriteLine(AtomicsInternalSend593Names.GuestCompleted);
            using var deadline = new CancellationTokenSource(OriginalReference563Names.Deadline);
            while (runtime.AtomicsStorage413.LiveArrays != 0) await Task.Delay(AtomicsInternalSend593Names.PollMilliseconds, deadline.Token);
            Require(runtime.AtomicsStorage413.OwnedNativeBytes == 0);
            GC.KeepAlive(savedEntries);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            // Whole-VM close invalidates legacy/public Host fallbacks and clears
            // queues; a red deterministic-retirement check must not hang cleanup.
            if (runtime is not null) try
                {
                    await runtime.DisposeAsync().AsTask().WaitAsync(OriginalReference563Names.Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (observer is not null) try
                {
                    await observer.DisposeAsync().AsTask().WaitAsync(OriginalReference563Names.Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            GC.KeepAlive(savedEntries);
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(failures);
    }

    private static string PrepareSourceWorker(string source)
    {
        int first = source.IndexOf(AtomicsInternalSend593Names.ModuleMacro, StringComparison.Ordinal);
        Require(first >= 0 && first == source.LastIndexOf(AtomicsInternalSend593Names.ModuleMacro, StringComparison.Ordinal));
        int dynamicCall = source.IndexOf(AtomicsInternalSend593Names.DynamicSubjectCall, StringComparison.Ordinal);
        Require(dynamicCall >= 0 && dynamicCall == source.LastIndexOf(AtomicsInternalSend593Names.DynamicSubjectCall, StringComparison.Ordinal));

        return source.Replace(AtomicsInternalSend593Names.ModuleMacro, OriginalReference563Names.Worker, StringComparison.Ordinal)
            .Replace(AtomicsInternalSend593Names.DynamicSubjectCall, AtomicsInternalSend593Names.AppliedSubjectCall, StringComparison.Ordinal);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidDataException(AtomicsInternalSend593Names.Failure);
    }
}
