using System.Reflection;

namespace Erlang.Tests;

public static class NifAdmissionHostChecks
{
    public static async Task Run()
    {
        await CallerIdentityAndOldUpgrade();
        await PermissionAcrossInitialization();
        await AbandonmentWaitsForCleanup();
        await DefensiveGenerationCheck();
        await ReentryRefusesSelfJoinAndWriter();
        await ExactPendingTicket();
    }

    private static ModuleCodeDescriptor Descriptor(object owner) => new(
        [(NifAdmissionHostNames.Function, 0)],
        [(NifAdmissionHostNames.Function, 0)],
        new BitString(new byte[System.Security.Cryptography.MD5.HashSizeInBytes]),
        Nil.Value,
        sourceOwner: owner
    );

    private static (object Owner, ModuleCodeImage Image) Register(ModuleRegistry registry, string module = NifAdmissionHostNames.Module)
    {
        var owner = new object();
        registry.RegisterSource(
            module,
            NifAdmissionHostNames.Function,
            0,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value),
            owner
        );
        registry.PublishModuleCode(module, Descriptor(owner));

        return (owner, registry.CurrentCodeImage(module)!);
    }

    private static IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>> Bindings(string name = NifAdmissionHostNames.Function) =>
        [new((name, 0), (_, _) => ValueTask.FromResult<Term>(Nil.Value))];

    private static async Task CallerIdentityAndOldUpgrade()
    {
        await using var runtime = new ProcessRuntime();
        var original = Register(runtime.Modules);
        var owner = new NifAdmissionTestOwner();
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Reserve(runtime.Modules, context), NifAdmissionHostNames.NoCaller);
            using var flow = context.EnterSourceCode(original.Image, original.Owner);
            await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Reserve(new ModuleRegistry(), context), NifAdmissionHostNames.WrongRuntime);
            object reservation = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
            Require(ReferenceEquals(NifAdmissionHostBridge.Property(reservation, NifAdmissionHostNames.Image), original.Image));
            Require(NifAdmissionHostBridge.Property(reservation, NifAdmissionHostNames.Origin).ToString() == NifAdmissionHostNames.Current);
            await NifAdmissionHostBridge.Initialize(reservation, _ => ValueTask.FromResult(NifAdmissionHostBridge.Initialized(owner, Bindings())));
            await ((IAsyncDisposable)reservation).DisposeAsync();
            Require(owner.Calls == 0);
            await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Reserve(runtime.Modules, context), NifAdmissionHostNames.Reload);
        }
        );
        runtime.Modules.DeleteModule(NifAdmissionHostNames.Module);
        var current = Register(runtime.Modules);
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            using (context.EnterSourceCode(original.Image, original.Owner))
                await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Reserve(runtime.Modules, context), NifAdmissionHostNames.OldCode);
            using (context.EnterSourceCode(current.Image, current.Owner))
                await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Reserve(runtime.Modules, context), NifAdmissionHostNames.Upgrade);
        }
        );
        await NifAdmissionHostBridge.Retire(original.Image).WaitAsync(NifAdmissionHostNames.Deadline);
        Require(owner.Calls == 1);
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            using var flow = context.EnterSourceCode(current.Image, current.Owner);
            var reservation = (IAsyncDisposable)await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
            await reservation.DisposeAsync();
            await reservation.DisposeAsync();
        }
        );
    }

    private static async Task PermissionAcrossInitialization()
    {
        await using var runtime = new ProcessRuntime();
        var source = Register(runtime.Modules);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new NifAdmissionTestOwner();
        Task actor = NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            using var flow = context.EnterSourceCode(source.Image, source.Owner);
            object reservation = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
            await NifAdmissionHostBridge.Initialize(
                reservation,
                async _ =>
            {
                object registryGate = typeof(ModuleRegistry).GetField(NifAdmissionHostNames.Gate, NifAdmissionHostNames.Members)!.GetValue(runtime.Modules)!;
                bool unlocked = await Task.Run(() =>
                {
                    if (!Monitor.TryEnter(registryGate))
                        return false;
                    Monitor.Exit(registryGate);

                    return true;
                });
                Require(unlocked);
                entered.TrySetResult();
                await release.Task;

                return NifAdmissionHostBridge.Initialized(owner, Bindings());
            }
            );
            await ((IAsyncDisposable)reservation).DisposeAsync();
        }
        );
        Task? writer = null;
        try
        {
            await entered.Task.WaitAsync(NifAdmissionHostNames.Deadline);
            writer = Task.Run(() => runtime.Modules.DeleteModule(NifAdmissionHostNames.Module));
            await NifAdmissionHostBridge.WaitWriter(runtime.Modules).WaitAsync(NifAdmissionHostNames.Deadline);
            Require(!writer.IsCompleted && ReferenceEquals(runtime.Modules.CurrentCodeImage(NifAdmissionHostNames.Module), source.Image));
        }
        finally
        {
            release.TrySetResult();
            await actor.WaitAsync(NifAdmissionHostNames.Deadline);
            if (writer is not null)
                await writer.WaitAsync(NifAdmissionHostNames.Deadline);
            await NifAdmissionHostBridge.Retire(source.Image).WaitAsync(NifAdmissionHostNames.Deadline);
        }
        Require(owner.Calls == 1);
    }

    private static async Task AbandonmentWaitsForCleanup()
    {
        await using var runtime = new ProcessRuntime();
        var source = Register(runtime.Modules);
        var reservationReady = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseInitializer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCleanup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new NifAdmissionTestOwner(async () =>
        {
            cleanupEntered.TrySetResult();
            await releaseCleanup.Task;
        });
        Task actor = NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            using var flow = context.EnterSourceCode(source.Image, source.Owner);
            object reservation = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
            Task initialization = NifAdmissionHostBridge.Initialize(
                reservation,
                async _ =>
            {
                reservationReady.TrySetResult(reservation);
                await releaseInitializer.Task;

                return NifAdmissionHostBridge.Initialized(owner, Bindings());
            }
            );
            await NifAdmissionHostBridge.Failure(initialization, NifAdmissionHostNames.Abandoned);
        }
        );
        Task? dispose = null;
        Task? writer = null;
        try
        {
            object reservation = await reservationReady.Task.WaitAsync(NifAdmissionHostNames.Deadline);
            dispose = ((IAsyncDisposable)reservation).DisposeAsync().AsTask();
            Require(!dispose.IsCompleted && owner.Calls == 0);
            writer = Task.Run(() => runtime.Modules.DeleteModule(NifAdmissionHostNames.Module));
            await NifAdmissionHostBridge.WaitWriter(runtime.Modules).WaitAsync(NifAdmissionHostNames.Deadline);
            releaseInitializer.TrySetResult();
            await cleanupEntered.Task.WaitAsync(NifAdmissionHostNames.Deadline);
            Require(owner.Calls == 1 && !writer.IsCompleted && !dispose.IsCompleted);
        }
        finally
        {
            releaseInitializer.TrySetResult();
            releaseCleanup.TrySetResult();
            if (dispose is not null)
                await NifAdmissionHostBridge.Failure(dispose, NifAdmissionHostNames.Abandoned);
            await actor.WaitAsync(NifAdmissionHostNames.Deadline);
            if (writer is not null)
                await writer.WaitAsync(NifAdmissionHostNames.Deadline);
        }
        Require(owner.Calls == 1);
    }

    private static async Task DefensiveGenerationCheck()
    {
        await using var runtime = new ProcessRuntime();
        var source = Register(runtime.Modules);
        var owner = new NifAdmissionTestOwner();
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            using var flow = context.EnterSourceCode(source.Image, source.Owner);
            object reservation = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
            await NifAdmissionHostBridge.Failure(
                NifAdmissionHostBridge.Initialize(
                    reservation,
                    _ =>
            {
                // Controlled fault injection represents a future writer omitting its
                // permission protocol. It is not an implementation mutation path.
                object gate = typeof(ModuleRegistry).GetField(NifAdmissionHostNames.Gate, NifAdmissionHostNames.Members)!.GetValue(runtime.Modules)!;
                lock (gate)
                {
                    var descriptors = (Dictionary<string, ModuleCodeDescriptor>)typeof(ModuleRegistry).GetField(NifAdmissionHostNames.Descriptors, NifAdmissionHostNames.Members)!.GetValue(runtime.Modules)!;
                    descriptors[NifAdmissionHostNames.Module] = Descriptor(source.Owner);
                }

                return ValueTask.FromResult(NifAdmissionHostBridge.Initialized(owner, Bindings()));
            }
                ),
                NifAdmissionHostNames.Changed
            );
            var retry = (IAsyncDisposable)await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
            await retry.DisposeAsync();
        }
        );
        Require(owner.Calls == 1);
    }

    private static async Task ExactPendingTicket()
    {
        await using var runtime = new ProcessRuntime();
        var current = Register(runtime.Modules, NifAdmissionHostNames.PendingModule);
        var assembly = typeof(NifAdmissionHostChecks).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith(NifAdmissionHostNames.PendingModule + NifAdmissionHostNames.Extension, StringComparison.Ordinal));
        using var input = assembly.GetManifestResourceStream(resource)!;
        using var bytes = new MemoryStream();
        input.CopyTo(bytes);
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(bytes.ToArray());
        BeamOnLoadTicket pending = runtime.Modules.StagePreparedOnLoad(prepared);
        var pendingImage = (ModuleCodeImage)NifAdmissionHostBridge.Property(pending, NifAdmissionHostNames.CodeImage);
        var owner = new NifAdmissionTestOwner();
        try
        {
            await NifAdmissionHostBridge.Context(
                runtime,
                async context =>
            {
                using (context.EnterSourceCode(current.Image, current.Owner))
                    await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Reserve(runtime.Modules, context), NifAdmissionHostNames.Ambiguous);
                using var pendingFlow = context.EnterSourceCode(pendingImage, pending);
                object reservation = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
                Require(ReferenceEquals(NifAdmissionHostBridge.Property(reservation, NifAdmissionHostNames.Image), pendingImage));
                Require(NifAdmissionHostBridge.Property(reservation, NifAdmissionHostNames.Origin).ToString() == NifAdmissionHostNames.Pending);
                var descriptor = (ModuleCodeDescriptor)NifAdmissionHostBridge.Property(prepared, NifAdmissionHostNames.Descriptor);
                var function = descriptor.Functions.First(signature => signature.Arity == 0 && signature.Name != NifAdmissionHostNames.ModuleInfo);
                await NifAdmissionHostBridge.Initialize(reservation, _ => ValueTask.FromResult(NifAdmissionHostBridge.Initialized(owner, Bindings(function.Name))));
                Require(ReferenceEquals(runtime.Modules.CurrentCodeImage(NifAdmissionHostNames.PendingModule), current.Image));
            }
            );
            Require(owner.Calls == 0);
        }
        finally
        {
            // Automatic pending retirement/native cleanup is a separate integration
            // obligation. This dormant primitive test explicitly owns its cleanup.
            await NifAdmissionHostBridge.Retire(pendingImage).WaitAsync(NifAdmissionHostNames.Deadline);
            runtime.Modules.FinishPendingOnLoad(pending, false);
        }
        Require(owner.Calls == 1);
    }

    private static async Task ReentryRefusesSelfJoinAndWriter()
    {
        await using var runtime = new ProcessRuntime();
        var source = Register(runtime.Modules);
        runtime.Modules.PublishModule(NifAdmissionHostNames.ActorModule);
        var abandonedOwner = new NifAdmissionTestOwner();
        var cleanupOwner = new NifAdmissionTestOwner();
        var committedOwner = new NifAdmissionTestOwner();
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            using var flow = context.EnterSourceCode(source.Image, source.Owner);
            object abandoned = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
            await NifAdmissionHostBridge.Failure(
                NifAdmissionHostBridge.Initialize(
                    abandoned,
                    async _ =>
            {
                await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Dispose(abandoned), NifAdmissionHostNames.Reentry);

                return NifAdmissionHostBridge.Initialized(abandonedOwner, Bindings());
            }
                ),
                NifAdmissionHostNames.Abandoned
            );
            Require(abandonedOwner.Calls == 1);

            object invalid = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
            cleanupOwner = new NifAdmissionTestOwner(() => ((IAsyncDisposable)invalid).DisposeAsync());
            try
            {
                await NifAdmissionHostBridge.Initialize(
                    invalid,
                    _ => ValueTask.FromResult(NifAdmissionHostBridge.Initialized(cleanupOwner, Bindings(NifAdmissionHostNames.Missing)))
                );
                Require(false);
            }
            catch (AggregateException failure)
            {
                Require(failure.InnerExceptions.Count == 2 && failure.InnerExceptions[0] is ArgumentException
                    && failure.InnerExceptions[1].GetType().GetProperty(NifAdmissionHostNames.Failure, NifAdmissionHostNames.Members)!.GetValue(failure.InnerExceptions[1])!.ToString() == NifAdmissionHostNames.Reentry);
            }
            Require(cleanupOwner.Calls == 1);

            object valid = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
            ProcessHandle? independent = null;
            await NifAdmissionHostBridge.Initialize(
                valid,
                async _ =>
            {
                await NifAdmissionHostBridge.Failure(Task.Run(() => runtime.Modules.PublishModule(NifAdmissionHostNames.Other)), NifAdmissionHostNames.Reentry);
                independent = runtime.Spawn(child => runtime.Modules.Call(
                    child,
                    ErlangModuleNames.Module,
                    NifAdmissionHostNames.DeleteModule,
                    Term.A(NifAdmissionHostNames.ActorModule)
                ));

                return NifAdmissionHostBridge.Initialized(committedOwner, Bindings());
            }
            );
            await independent!.Completion.WaitAsync(NifAdmissionHostNames.Deadline);
            Require(!runtime.Modules.IsModuleLoaded(NifAdmissionHostNames.ActorModule));
            Require(!runtime.Modules.IsModuleLoaded(NifAdmissionHostNames.Other) && committedOwner.Calls == 0);
        }
        );
        await NifAdmissionHostBridge.Retire(source.Image).WaitAsync(NifAdmissionHostNames.Deadline);
        Require(committedOwner.Calls == 1);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifAdmissionHostNames.Broken);
    }
}
