using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Differential;
using static Erlang.Tests.MapRouting517TestNames;

namespace Erlang.Tests;

// Genuine producers, not callbacks pretending to produce Erlang maps. Reflection
// only inspects private storage while the real child is held at receive.
internal static class MapRouting517Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task SourceProducer()
    {
        string repository = Repository();
        using var compiled = GeneratedModuleCompiler.Compile(File.ReadAllText(Path.Combine(repository, FixturePath)), Path.GetFileName(FixturePath));
        foreach (bool owned in new[] { false, true })
        {
            MapTerm portable;
            await using (var sender = OwnedGuest506RuntimeFactory.Create(owned))
            {
                await Initialize526(sender);
                compiled.Definition.Register(sender.Modules);
                portable = await Exercise(sender, owned, null);
            }
            CheckPortable(portable);
            await using var receiver = OwnedGuest506RuntimeFactory.Create(owned);
            await Initialize526(receiver);
            compiled.Definition.Register(receiver.Modules);
            MapTerm forwarded = await Exercise(receiver, owned, portable);
            CheckPortable(forwarded);
        }
    }

    internal static async Task OriginalBeamProducer(string admissionPath, string admissionSha256)
    {
        string repository = Repository();
        OriginalMap517Admission receipt = OriginalMap517AdmissionReader.Read(repository, admissionPath, admissionSha256);
        foreach (bool owned in new[] { false, true })
        {
            MapTerm portable;
            await using (var sender = OwnedGuest506RuntimeFactory.Create(owned))
            {
                await Initialize526(sender);
                LoadOriginal(
                    sender,
                    repository,
                    receipt,
                    true
                );
                await CompareOriginalWorker(sender, repository, receipt);
                portable = await Exercise(sender, owned, null);
            }
            CheckPortable(portable);
            await using var receiver = OwnedGuest506RuntimeFactory.Create(owned);
            await Initialize526(receiver);
            LoadOriginal(
                receiver,
                repository,
                receipt,
                false
            );
            MapTerm forwarded = await Exercise(receiver, owned, portable);
            CheckPortable(forwarded);
        }
    }

    private static async Task Initialize526(ProcessRuntime runtime)
    {
        // Reuse the existing source-admitted native same/2 facet, not a C#
        // replacement of the fixture or the original monitored worker.
        await DebugSame420HostAccess.Initialize(runtime);
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapMapKeys426.Register(runtime, registry);
            RuntimeBootstrapHamtMap430.Register(runtime, registry);
            RuntimeBootstrapDebugSharing425.Register(runtime, registry);
        });
    }

    private static void LoadOriginal(
        ProcessRuntime runtime,
        string repository,
        OriginalMap517Admission receipt,
        bool worker
    )
    {
        var images = new List<PreparedBeamImage>
        {
            runtime.Modules.PrepareBeam(
                File.ReadAllBytes(OriginalMap517AdmissionReader.Resolve(repository, receipt.BeamPath)),
                new BeamDependencySelection(new[] { CoreModule, InternalModule, DebugModule526 })
            )
        };
        if (worker)
            images.Add(runtime.Modules.PrepareBeam(
                File.ReadAllBytes(OriginalMap517AdmissionReader.Resolve(repository, receipt.WorkerBeamPath)),
                new BeamDependencySelection(new[] { CoreModule, GuestModule })
            ));
        Check(runtime.Modules.PublishPreparedBeam(images.ToArray()).Status == BeamPublicationStatus.Published);
    }

    private static async Task CompareOriginalWorker(ProcessRuntime runtime, string repository, OriginalMap517Admission receipt)
    {
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await controller.InvokeAsync(async context =>
        {
            Term actual = await runtime.Modules.Call(
                context,
                WorkerModule,
                Run,
                Term.A(GuestModule)
            );
            string output = Path.Combine(repository, ArtifactDirectory, Guid.NewGuid().ToString());
            Directory.CreateDirectory(output);
            Term minor = Term.Tuple(Term.A(MinorVersion), Term.I(2));
            var ordinary = (BitString)await runtime.Modules.Call(
                context,
                CoreModule,
                Encode,
                actual,
                Term.List(minor)
            );
            var deterministic = (BitString)await runtime.Modules.Call(
                context,
                CoreModule,
                Encode,
                actual,
                Term.List(Term.A(Deterministic), minor)
            );
            await File.WriteAllBytesAsync(Path.Combine(output, ActualDefault), ordinary.ToArray());
            await File.WriteAllBytesAsync(Path.Combine(output, ActualDeterministic), deterministic.ToArray());
            // Official deterministic serialization was requested in both VMs.
            // Whole default bytes remain retained, with no own-state reordering.
            Check(deterministic.ToArray().AsSpan().SequenceEqual(File.ReadAllBytes(
                OriginalMap517AdmissionReader.Resolve(repository, receipt.DeterministicPath))));

            return true;
        });
    }

    private static async Task<MapTerm> Exercise(ProcessRuntime runtime, bool owned, MapTerm? incoming)
    {
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var returned = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        Term token = Term.I(InputToken);
        ProcessHandle child = runtime.Spawn(async context =>
        {
            ready.TrySetResult(context);
            try
            {
                Term result = incoming is null
                    ? await runtime.Modules.Call(
                        context,
                        GuestModule,
                        Hold,
                        token
                    )
                    : await runtime.Modules.Call(
                        context,
                        GuestModule,
                        HoldValue,
                        token,
                        incoming
                    );
                returned.TrySetResult(result);

                return Nil.Value;
            }
            catch (Exception error)
            {
                returned.TrySetException(error);
                throw;
            }
        });
        ProcessContext owner = await ready.Task.WaitAsync(Deadline);
        try
        {
            var clock = Stopwatch.StartNew();
            MapTerm? resident = null;
            TupleTerm? alias = null;
            TupleTerm? flatAlias = null;
            TupleTerm? rawAlias = null;
            while (clock.Elapsed < Deadline)
            {
                if (returned.Task.IsCompleted)
                    _ = await returned.Task;
                object gate = Field(owner.Dictionary, GateField);
                lock (gate)
                {
                    var data = (Dictionary<Term, Term>)Field(owner.Dictionary, DataField);
                    if (data.TryGetValue(Term.A(Phase), out var phase) && phase.Equals(token))
                    {
                        resident = (MapTerm)data[Term.A(MapKey)];
                        alias = (TupleTerm)data[Term.A(AliasKey)];
                        flatAlias = (TupleTerm)data[Term.A(FlatAliasKey)];
                        rawAlias = (TupleTerm)data[Term.A(RawAliasKey526)];
                    }
                }
                if (resident is not null)
                {
                    Term status = await controller.InvokeAsync(async context =>
                        await runtime.Modules.Call(
                            context,
                            CoreModule,
                            ProcessInfo,
                            child.Pid,
                            Term.A(Status)
                        ));
                    if (status.Equals(Term.Tuple(Term.A(Status), Term.A(Waiting))))
                        break;
                }
                await Task.Delay(PollInterval);
            }
            Check(resident is not null && alias is not null && flatAlias is not null && rawAlias is not null && clock.Elapsed < Deadline);
            MapTerm flatMap = (MapTerm)flatAlias!.Items[0];
            object flatKeys = Get(flatMap, FlatKeysProperty);
            Check(ReferenceEquals(flatKeys, flatAlias.Items[1]) && ReferenceEquals(flatKeys, flatAlias.Items[2]));
            Check(resident!.Entries.Count == MapCount && resident.Get(Term.A(TokenKey)).Equals(token));
            object heap = Get(owner, Heap);
            var rawMap = (MapTerm)rawAlias!.Items[0];
            using (var rawRead = (IDisposable)Call(runtime, AcquireIndex526, rawMap)!)
            {
                object rawHead = Get(Get(rawRead, Index526), Root526);
                object rawLast = ((System.Collections.IEnumerable)Get(rawHead, Children526)).Cast<object>().Last();
                Check(ReferenceEquals(Get(rawLast, StoredTerm526), rawAlias.Items[1]));
            }
            if (owned)
            {
                Check((long)Get(heap, MeasuredQueries526) >= RequiredMeasuredQueries526);
                object payload = Get(resident, Storage);
                object root = Get(payload, RootPayload);
                Check(ReferenceEquals(Get(root, Owner), heap));
                object flatRoot = Get(Get(flatMap, Storage), RootPayload);
                Check(ReferenceEquals(Get(flatRoot, Owner), heap));
                Check(ReferenceEquals(alias!.Items[0], resident) && ReferenceEquals(alias.Items[1], resident));
                Call(heap, Collect);
                Check(resident.Get(Term.A(TokenKey)).Equals(token));
            }
            runtime.Send(child.Pid, Term.Tuple(Term.A(Release), token));
            Term result = await returned.Task.WaitAsync(Deadline);
            await child.Completion.WaitAsync(Deadline);
            if (owned)
                Check((int)Get(Get(heap, Snapshot), Blocks) == 0);
            var portable = (MapTerm)((TupleTerm)result).Items[0];
            CheckPortable(portable);

            return portable;
        }
        finally
        {
            runtime.Send(child.Pid, Term.Tuple(Term.A(Release), token));
            if (!child.Completion.IsCompleted)
                await controller.Invoke(context =>
 {
     runtime.Exit(context, child.Pid, Term.A(Kill));

     return true;
 });
            await child.Completion.WaitAsync(Deadline);
        }
    }

    private static void CheckPortable(MapTerm map)
    {
        Check(map.GetType().GetProperty(Storage, Members)!.GetValue(map) is null);
        Check(map.Entries.Count == MapCount && map.Get(Term.A(TokenKey)).Equals(Term.I(InputToken)));
    }

    private static string Repository()
    {
        for (DirectoryInfo? current = new(Environment.CurrentDirectory); current is not null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, FixturePath)))
                return current.FullName;

        throw new InvalidOperationException(AdmissionMissing);
    }

    private static object Get(object target, string property) => target.GetType().GetProperty(property, Members)!.GetValue(target)!;

    private static object Field(object target, string field) => target.GetType().GetField(field, Members)!.GetValue(target)!;

    private static object? Call(object target, string method, params object?[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(method, Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(CheckFailed);
    }
}
