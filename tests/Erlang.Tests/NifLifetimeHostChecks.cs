using System.Collections;
using System.Reflection;
using System.Security.Cryptography;

namespace Erlang.Tests;

public static partial class NifLifetimeHostChecks
{
    public static async Task Run()
    {
        await PurgeAfterActualUnwind();
        await PurgeCleanupFailure();
        await ShutdownActiveCall(false);
        await ShutdownActiveCall(true);
        await ShutdownCancellationFailure();
        await ReservedAndClosedAdmission();
        await ShutdownInitializingOwner();
        await HighLevelPendingDiscard();
        await CleanupSelfJoinRefused();
        await HaltActiveCall();
        await InitializerDeathOwnsRetirement();
        await RetirementRequestsImmediateHalt();
        await RegistryHeldNativeShutdownRefused();
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifLifetimeHostNames.Broken);
    }

    private static object Field(object value, string name)
        => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;

    private static (object Owner, ModuleCodeImage Image) Register(ModuleRegistry registry, string module = NifLifetimeHostNames.Module)
    {
        var owner = new object();
        registry.RegisterSource(
            module,
            NifLifetimeHostNames.Function,
            0,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value),
            owner
        );
        registry.PublishModuleCode(
            module,
            new ModuleCodeDescriptor(
                [(NifLifetimeHostNames.Function, 0)],
                [(NifLifetimeHostNames.Function, 0)],
                new BitString(new byte[MD5.HashSizeInBytes]),
                Nil.Value,
                sourceOwner: owner
            )
        );

        return (owner, registry.CurrentCodeImage(module)!);
    }

    private static async Task Install(
        ProcessRuntime runtime,
        ModuleCodeImage image,
        object sourceOwner,
        NifLifetimeTestOwner owner,
        ErlangFunction body,
        string function = NifLifetimeHostNames.Function
    )
    {
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            using var flow = context.EnterSourceCode(image, sourceOwner);
            object reservation = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
            await NifAdmissionHostBridge.Initialize(
                reservation,
                _ => ValueTask.FromResult(NifAdmissionHostBridge.Initialized(owner,
                new KeyValuePair<(string Name, int Arity), ErlangFunction>[] { new((function, 0), body) }
            ))
            ).WaitAsync(NifLifetimeHostNames.Deadline);
            await NifAdmissionHostBridge.Dispose(reservation).WaitAsync(NifLifetimeHostNames.Deadline);
        }
        );
    }

    private static async ValueTask<Term> Invoke(ProcessContext context, ModuleCodeImage image, object owner)
    {
        using var flow = context.EnterSourceCode(image, owner);
        object table = NifAdmissionHostBridge.Property(image, NifAdmissionHostNames.NifBindings);
        using var call = (IDisposable)NifAdmissionHostBridge.Call(
            table,
            NifLifetimeHostNames.Admit,
            NifLifetimeHostNames.Function,
            0
        )!;

        return await (ValueTask<Term>)NifAdmissionHostBridge.Call(
            call,
            NifLifetimeHostNames.Invoke,
            context,
            Array.Empty<Term>()
        )!;
    }

    private static async ValueTask CheckCleanupBoundary(ProcessRuntime runtime)
    {
        Require(!Monitor.IsEntered(Field(runtime, NifLifetimeHostNames.Gate)));
        Require(!Monitor.IsEntered(Field(runtime.Modules, NifLifetimeHostNames.Gate)));
        await Task.Run(() => runtime.Modules.Apply(_ => { })).WaitAsync(NifLifetimeHostNames.Deadline);
    }

    private static async Task PurgeAfterActualUnwind()
    {
        var runtime = new ProcessRuntime();
        var source = Register(runtime.Modules);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwinding = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new NifLifetimeTestOwner(async () =>
        {
            await CheckCleanupBoundary(runtime);
            cleanupEntered.TrySetResult();
            await cleanupRelease.Task.WaitAsync(NifLifetimeHostNames.Deadline);
        });
        ProcessHandle? actor = null;
        Task? purge = null;
        Task? retirement = null;
        var failures = new List<Exception>();
        try
        {
            await Install(
                runtime,
                source.Image,
                source.Owner,
                owner,
                async (context, _) =>
            {
                entered.TrySetResult();
                try
                {
                    // This managed asynchronous body must admit the purger's code-check controls.
                    await context.ReceiveAsync(_ => (TupleTerm?)null);

                    return Nil.Value;
                }
                finally
                {
                    unwinding.TrySetResult();
                    await release.Task.WaitAsync(NifLifetimeHostNames.Deadline);
                }
            }
            );
            actor = runtime.Spawn(context => Invoke(context, source.Image, source.Owner));
            await entered.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            runtime.Modules.DeleteModule(NifLifetimeHostNames.Module);
            Require(owner.Calls == 0 && runtime.Modules.HasOldCode(NifLifetimeHostNames.Module));
            purge = NifAdmissionHostBridge.Context(
                runtime,
                async context =>
            {
                Term result = await runtime.Modules.Call(
                    context,
                    NifLifetimeHostNames.Erlang,
                    NifLifetimeHostNames.Purge,
                    Term.A(NifLifetimeHostNames.Module)
                );
                Require(result.Equals(Term.A(ErlangBooleanAtoms.True)));
            }
            );
            await unwinding.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            Require(!purge.IsCompleted && owner.Calls == 0);
            release.TrySetResult();
            await actor.Completion.WaitAsync(NifLifetimeHostNames.Deadline);
            await cleanupEntered.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            retirement = (Task)NifAdmissionHostBridge.Call(runtime.Modules, NifLifetimeHostNames.Retire, source.Image)!;
            Require(!retirement.IsCompleted);
            await purge.WaitAsync(NifLifetimeHostNames.Deadline);
            Require(!retirement.IsCompleted && owner.Calls == 1);
            Require(!runtime.Modules.HasOldCode(NifLifetimeHostNames.Module) && runtime.Modules.PendingPurge(source.Image) is null);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        finally
        {
            release.TrySetResult();
            cleanupRelease.TrySetResult();
            await CaptureCleanupFailure(purge, failures);
            await CaptureCleanupFailure(actor?.Completion, failures);
            await CaptureCleanupFailure(retirement, failures);
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(NifLifetimeHostNames.Deadline);
            }
            catch (Exception cleanupFailure)
            {
                failures.Add(cleanupFailure);
            }
        }
        if (failures.Count == 1)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);
        Require(owner.Calls == 1 && !runtime.Modules.HasOldCode(NifLifetimeHostNames.Module));
    }

    private static async Task CaptureCleanupFailure(Task? operation, List<Exception> failures)
    {
        if (operation is null)
            return;
        try
        {
            await operation.WaitAsync(NifLifetimeHostNames.Deadline);
        }
        catch (Exception failure)
        {
            if (!failures.Contains(failure))
                failures.Add(failure);
        }
    }

    private static bool Contains(Exception error, string message)
        => error.Message == message || error is AggregateException aggregate && aggregate.InnerExceptions.Any(inner => Contains(inner, message));

    private static async Task PurgeCleanupFailure()
    {
        var runtime = new ProcessRuntime();
        var source = Register(runtime.Modules);
        var cleanupEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new NifLifetimeTestOwner(async () =>
        {
            cleanupEntered.TrySetResult();
            await cleanupRelease.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            throw new InvalidOperationException(NifLifetimeHostNames.CleanupFailure);
        });
        await Install(
            runtime,
            source.Image,
            source.Owner,
            owner,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        runtime.Modules.DeleteModule(NifLifetimeHostNames.Module);
        Task? first = null;
        try
        {
            Task purge = NifAdmissionHostBridge.Context(
                runtime,
                async context =>
            {
                Term result = await runtime.Modules.Call(
                    context,
                    NifLifetimeHostNames.Erlang,
                    NifLifetimeHostNames.Purge,
                    Term.A(NifLifetimeHostNames.Module)
                );
                Require(result.Equals(Term.A(ErlangBooleanAtoms.True)));
            }
            );
            await cleanupEntered.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            first = (Task)NifAdmissionHostBridge.Call(runtime.Modules, NifLifetimeHostNames.Retire, source.Image)!;
            Task second = (Task)NifAdmissionHostBridge.Call(runtime.Modules, NifLifetimeHostNames.Retire, source.Image)!;
            Require(ReferenceEquals(first, second) && !first.IsCompleted);
            await purge.WaitAsync(NifLifetimeHostNames.Deadline);
            Require(owner.Calls == 1 && !runtime.Modules.HasOldCode(NifLifetimeHostNames.Module));
            Require(runtime.Modules.PendingPurge(source.Image) is null && !first.IsCompleted);
        }
        finally
        {
            cleanupRelease.TrySetResult();
            if (first is not null)
                await ExpectFailure(first, NifLifetimeHostNames.CleanupFailure);
            await ExpectFailure(runtime.DisposeAsync().AsTask(), NifLifetimeHostNames.CleanupFailure);
        }
        await ExpectFailure(runtime.DisposeAsync().AsTask(), NifLifetimeHostNames.CleanupFailure);
        Require(owner.Calls == 1);
    }

    private static async Task ExpectFailure(Task operation, string message)
    {
        try
        {
            await operation.WaitAsync(NifLifetimeHostNames.Deadline);
        }
        catch (Exception failure) when (Contains(failure, message))
        {
            return;
        }
        throw new InvalidOperationException(NifLifetimeHostNames.Broken);
    }

    private static async Task ShutdownActiveCall(bool failCleanup)
    {
        var runtime = new ProcessRuntime();
        var source = Register(runtime.Modules);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwinding = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new NifLifetimeTestOwner(async () =>
        {
            await CheckCleanupBoundary(runtime);
            if (failCleanup)
                throw new InvalidOperationException(NifLifetimeHostNames.CleanupFailure);
        });
        await Install(
            runtime,
            source.Image,
            source.Owner,
            owner,
            async (context, _) =>
        {
            entered.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, context.Cancellation);

                return Nil.Value;
            }
            finally
            {
                unwinding.TrySetResult();
                await release.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            }
        }
        );
        ProcessHandle actor = runtime.Spawn(context => Invoke(context, source.Image, source.Owner));
        Task? shutdown = null;
        try
        {
            await entered.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            shutdown = runtime.DisposeAsync().AsTask();
            await unwinding.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            Require(!shutdown.IsCompleted && owner.Calls == 0);
        }
        finally
        {
            release.TrySetResult();
            shutdown ??= runtime.DisposeAsync().AsTask();
            if (failCleanup)
                await ExpectFailure(shutdown, NifLifetimeHostNames.CleanupFailure);
            else
                await shutdown.WaitAsync(NifLifetimeHostNames.Deadline);
            await actor.Completion.WaitAsync(NifLifetimeHostNames.Deadline);
        }
        Require(owner.Calls == 1);
        if (failCleanup)
            await ExpectFailure(runtime.DisposeAsync().AsTask(), NifLifetimeHostNames.CleanupFailure);
        else
            await runtime.DisposeAsync().AsTask().WaitAsync(NifLifetimeHostNames.Deadline);
        Require(owner.Calls == 1);
    }

    private static async Task ReservedAndClosedAdmission()
    {
        await using var runtime = new ProcessRuntime();
        var source = Register(runtime.Modules);
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            using var flow = context.EnterSourceCode(source.Image, source.Owner);
            object reservation = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
            Task close = (Task)NifAdmissionHostBridge.Call(runtime.Modules, NifLifetimeHostNames.Close)!;
            await close.WaitAsync(NifLifetimeHostNames.Deadline);
            await NifAdmissionHostBridge.Dispose(reservation).WaitAsync(NifLifetimeHostNames.Deadline);
            await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Reserve(runtime.Modules, context), NifLifetimeHostNames.RuntimeClosing);
        }
        );
    }

    private static async Task ShutdownCancellationFailure()
    {
        var runtime = new ProcessRuntime();
        var source = Register(runtime.Modules);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwinding = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new NifLifetimeTestOwner(() => CheckCleanupBoundary(runtime));
        await Install(
            runtime,
            source.Image,
            source.Owner,
            owner,
            async (context, _) =>
        {
            using var throwing = context.Cancellation.Register(() => throw new InvalidOperationException(NifLifetimeHostNames.CancellationFailure));
            entered.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, context.Cancellation);

                return Nil.Value;
            }
            finally
            {
                unwinding.TrySetResult();
                await release.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            }
        }
        );
        ProcessHandle actor = runtime.Spawn(context => Invoke(context, source.Image, source.Owner));
        Task? shutdown = null;
        try
        {
            await entered.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            shutdown = runtime.DisposeAsync().AsTask();
            await unwinding.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            Require(!shutdown.IsCompleted && owner.Calls == 0);
        }
        finally
        {
            release.TrySetResult();
            await ExpectFailure(shutdown ?? runtime.DisposeAsync().AsTask(), NifLifetimeHostNames.CancellationFailure);
            await ExpectFailure(actor.Completion, NifLifetimeHostNames.CancellationFailure);
        }
        Require(owner.Calls == 1);
    }

    private static async Task ShutdownInitializingOwner()
    {
        var runtime = new ProcessRuntime();
        var source = Register(runtime.Modules);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new NifLifetimeTestOwner(async () =>
        {
            Require(!Monitor.IsEntered(Field(runtime.Modules, NifLifetimeHostNames.Gate)));
            cleanupEntered.TrySetResult();
            await cleanupRelease.Task.WaitAsync(NifLifetimeHostNames.Deadline);
        });
        Task actor = NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            using var flow = context.EnterSourceCode(source.Image, source.Owner);
            object reservation = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
            try
            {
                await NifAdmissionHostBridge.Initialize(
                    reservation,
                    async _ =>
                {
                    entered.TrySetResult();
                    await release.Task.WaitAsync(NifLifetimeHostNames.Deadline);

                    return NifAdmissionHostBridge.Initialized(
                        owner,
                        new KeyValuePair<(string Name, int Arity), ErlangFunction>[] {
                        new((NifLifetimeHostNames.Function, 0), (_, _) => ValueTask.FromResult<Term>(Nil.Value))
                    }
                    );
                }
                );
            }
            catch (OperationCanceledException) when (context.Cancellation.IsCancellationRequested)
            {
            }
            finally
            {
                try
                {
                    await NifAdmissionHostBridge.Dispose(reservation);
                }
                catch (OperationCanceledException) when (context.Cancellation.IsCancellationRequested)
                {
                }
            }
        }
        );
        Task? shutdown = null;
        try
        {
            await entered.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            shutdown = runtime.DisposeAsync().AsTask();
            Require(!shutdown.IsCompleted && owner.Calls == 0);
            release.TrySetResult();
            await cleanupEntered.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            Require(!shutdown.IsCompleted && owner.Calls == 1);
        }
        finally
        {
            release.TrySetResult();
            cleanupRelease.TrySetResult();
            await actor.WaitAsync(NifLifetimeHostNames.Deadline);
            await (shutdown ?? runtime.DisposeAsync().AsTask()).WaitAsync(NifLifetimeHostNames.Deadline);
        }
        Require(owner.Calls == 1);
    }

    private static async Task HighLevelPendingDiscard()
    {
        BeamOnLoadResourceHashChecks.Run();
        await using var runtime = new ProcessRuntime();
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle observer = runtime.Spawn(async context =>
        {
            ready.TrySetResult(context);
            await finish.Task.WaitAsync(NifLifetimeHostNames.Deadline);

            return Nil.Value;
        });
        ProcessContext controller = await ready.Task.WaitAsync(NifLifetimeHostNames.Deadline);
        runtime.Register(BeamOnLoadHostNames.Observer, observer.Pid);
        Assembly assembly = typeof(NifLifetimeHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(NifLifetimeHostNames.Resource, StringComparison.Ordinal));
        using Stream resource = assembly.GetManifestResourceStream(name)!;
        using var bytes = new MemoryStream();
        resource.CopyTo(bytes);
        var cleanupEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new NifLifetimeTestOwner(async () =>
        {
            await CheckCleanupBoundary(runtime);
            cleanupEntered.TrySetResult();
            await cleanupRelease.Task.WaitAsync(NifLifetimeHostNames.Deadline);
        });
        Task<BeamOnLoadResult> loading = runtime.Modules.LoadPreparedOnLoadAsync(runtime, runtime.Modules.PrepareBeam(bytes.ToArray()));
        Pid? callback = null;
        try
        {
            var notice = (TupleTerm)(await controller.ReceiveAsync(
                term => term is TupleTerm { Items.Count: 5 } tuple
                && tuple.Items[0].Equals(Term.A(BeamOnLoadHostNames.Notice)) ? tuple : null,
                NifLifetimeHostNames.Deadline
            ))!;
            callback = (Pid)notice.Items[2];
            BeamOnLoadTicket ticket;
            lock (Field(runtime.Modules, NifLifetimeHostNames.Gate))
                ticket = (BeamOnLoadTicket)((IDictionary)Field(runtime.Modules, NifLifetimeHostNames.Pending))[BeamOnLoadHostNames.FreshFailureModule]!;
            var image = (ModuleCodeImage)NifAdmissionHostBridge.Property(ticket, NifAdmissionHostNames.CodeImage);
            await Install(
                runtime,
                image,
                ticket,
                owner,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value),
                BeamOnLoadHostNames.Run
            );
            runtime.Send(callback, Term.Tuple(Term.A(BeamOnLoadHostNames.Continue), observer.Pid));
            await cleanupEntered.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            Require(!loading.IsCompleted && owner.Calls == 1);
        }
        finally
        {
            if (callback is not null)
                runtime.Send(callback, Term.Tuple(Term.A(BeamOnLoadHostNames.Continue), observer.Pid));
            cleanupRelease.TrySetResult();
            Require(!(await loading.WaitAsync(NifLifetimeHostNames.Deadline)).Published);
            finish.TrySetResult();
            await observer.Completion.WaitAsync(NifLifetimeHostNames.Deadline);
        }
        Require(owner.Calls == 1);
    }

    private static async Task CleanupSelfJoinRefused()
    {
        await using var runtime = new ProcessRuntime();
        var source = Register(runtime.Modules);
        var owner = new NifLifetimeTestOwner(async () =>
        {
            await NifAdmissionHostBridge.Failure(runtime.DisposeAsync().AsTask(), NifLifetimeHostNames.CleanupReentry);
        });
        await Install(
            runtime,
            source.Image,
            source.Owner,
            owner,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        await ((Task)NifAdmissionHostBridge.Call(runtime.Modules, NifLifetimeHostNames.Retire, source.Image)!).WaitAsync(NifLifetimeHostNames.Deadline);
        Require(owner.Calls == 1);
    }

    private static async Task HaltActiveCall()
    {
        var runtime = new ProcessRuntime();
        var source = Register(runtime.Modules);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwinding = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new NifLifetimeTestOwner(() => CheckCleanupBoundary(runtime));
        await Install(
            runtime,
            source.Image,
            source.Owner,
            owner,
            async (context, _) =>
        {
            entered.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, context.Cancellation);

                return Nil.Value;
            }
            finally
            {
                unwinding.TrySetResult();
                await release.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            }
        }
        );
        ProcessHandle actor = runtime.Spawn(context => Invoke(context, source.Image, source.Owner));
        try
        {
            await entered.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            await runtime.RequestImmediateHalt(0).WaitAsync(NifLifetimeHostNames.Deadline);
            await unwinding.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            Require(!runtime.HaltQuiescence.IsCompleted && owner.Calls == 0);
        }
        finally
        {
            release.TrySetResult();
            await runtime.HaltQuiescence.WaitAsync(NifLifetimeHostNames.Deadline);
            await actor.Completion.WaitAsync(NifLifetimeHostNames.Deadline);
            await runtime.DisposeAsync().AsTask().WaitAsync(NifLifetimeHostNames.Deadline);
        }
        Require(owner.Calls == 1);
    }

    private static async Task InitializerDeathOwnsRetirement()
    {
        var runtime = new ProcessRuntime();
        var source = Register(runtime.Modules);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwinding = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleaned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new NifLifetimeTestOwner(async () =>
        {
            await CheckCleanupBoundary(runtime);
            cleaned.TrySetResult();
        });
        await Install(
            runtime,
            source.Image,
            source.Owner,
            owner,
            async (context, _) =>
        {
            entered.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, context.Cancellation);

                return Nil.Value;
            }
            finally
            {
                unwinding.TrySetResult();
                await release.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            }
        }
        );
        ProcessHandle actor = runtime.Spawn(context => Invoke(context, source.Image, source.Owner));
        try
        {
            await entered.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            ProcessHandle killer = runtime.Spawn(context =>
            {
                runtime.Exit(context, runtime.InitializationPid, Term.A(NifLifetimeHostNames.Kill));

                return ValueTask.FromResult<Term>(Nil.Value);
            });
            await killer.Completion.WaitAsync(NifLifetimeHostNames.Deadline);
            await unwinding.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            Require(owner.Calls == 0);
            release.TrySetResult();
            // No explicit Dispose/Close request may supply the missing lifecycle work.
            await cleaned.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            Require(owner.Calls == 1);
        }
        finally
        {
            release.TrySetResult();
            await runtime.DisposeAsync().AsTask().WaitAsync(NifLifetimeHostNames.Deadline);
            await actor.Completion.WaitAsync(NifLifetimeHostNames.Deadline);
        }
    }

    private static async Task RegistryHeldNativeShutdownRefused()
    {
        var runtime = new ProcessRuntime();
        var source = Register(runtime.Modules);
        var owner = new NifLifetimeTestOwner();
        try
        {
            await Install(
                runtime,
                source.Image,
                source.Owner,
                owner,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value)
            );
            runtime.Modules.Apply(_ =>
            {
                NifAdmissionHostBridge.Failure(
                    runtime.DisposeAsync().AsTask(),
                    NifLifetimeHostNames.RegistryReentry
                ).GetAwaiter().GetResult();
                Require(runtime.Lifecycle == RuntimeLifecycleState.Running && owner.Calls == 0);
            });
            Require(runtime.Lifecycle == RuntimeLifecycleState.Running && owner.Calls == 0);
        }
        finally
        {
            await runtime.DisposeAsync().AsTask().WaitAsync(NifLifetimeHostNames.Deadline);
        }
        Require(owner.Calls == 1);
        await runtime.DisposeAsync().AsTask().WaitAsync(NifLifetimeHostNames.Deadline);
        Require(owner.Calls == 1);
    }
}
