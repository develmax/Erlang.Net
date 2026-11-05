using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang.Tests;
using static Erlang.Tests.NativeResource524Access;
namespace Erlang.Validation.AsyncMicrostates;

internal static class RemoteMsaccNegativeChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    private static object Field(object owner, string name)
        => owner.GetType().GetField(name, Members)!.GetValue(owner)!;

    private static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException(RemoteMsaccNegativeNames.ContractFailurePrefix + name);
        }
    }

    private static async Task Until(Func<bool> predicate)
    {
        long stop = Environment.TickCount64 + (long)RemoteMsaccNegativeNames.Deadline.TotalMilliseconds;
        while (!predicate())
        {
            if (Environment.TickCount64 >= stop)
            {
                throw new TimeoutException(RemoteMsaccNegativeNames.WitnessTimeoutMessage);
            }
            await Task.Delay(RemoteMsaccNegativeNames.WitnessPollIntervalMilliseconds).ConfigureAwait(false);
        }
    }

    private static string Text(Term value)
        => Encoding.ASCII.GetString(value is BitString bits ? bits.ToArray()
            : Cons.Items(value).Select(item => checked((byte)((Integer)item).Value)).ToArray());

    private static long[] Snapshot(Term value)
    {
        long[] values = Text(value).Split(',').Select(part => long.Parse(part, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        Require(values.Length == RemoteMsaccNegativeNames.NativeSnapshotFieldCount, RemoteMsaccNegativeNames.SnapshotShapeAssertion);

        return values;
    }

    private sealed record Witness(
        RuntimeMicrostateOperation1171 Operation,
        StatisticsTrapRequest Trap,
        RuntimeSchedulerAuxiliary420 Held,
        ulong Token,
        string Generation,
        int Count,
        bool Open,
        bool SourceReady
    );

    // Read-only R -> D order matches actual publication; no grant, worker, ACK or flag is set here.

    private static Witness? Published(ProcessRuntime runtime, ProcessContext caller, int heldLane)
    {
        object r = Field(runtime, RemoteMsaccNegativeNames.RuntimeGateField);
        lock (r)
        {
            var requests = (IDictionary?)Field(runtime, RemoteMsaccNegativeNames.TrapRequestsField);
            if (requests is null || !requests.Contains(caller))
            {
                return null;
            }
            foreach (StatisticsTrapRequest trap in (IEnumerable)requests[caller]!)
            {
                var abort = (Action<Exception>?)Field(trap, RemoteMsaccNegativeNames.AbortPreparationField);
                if (abort?.Target is not RuntimeMicrostateOperation1171 operation
                    || operation.CaptureTask is not { IsCompletedSuccessfully: true } captureTask
                    || captureTask.Result is not { } capture
                    || operation.Batch is not { State: RuntimeSchedulerAuxiliaryState420.Published } batch
                    || !operation.SourceReady.IsCompletedSuccessfully || operation.SourceReady.Result is not null) continue;
                object dispatcher = Field(runtime, RemoteMsaccNegativeNames.DispatcherField);
                lock (Field(dispatcher, RemoteMsaccNegativeNames.DispatcherGateField))
                {
                    Array lanes = (Array)Field(dispatcher, RemoteMsaccNegativeNames.DispatcherLanesField);
                    object lane = lanes.GetValue(heldLane - RemoteMsaccNegativeNames.FirstNormalLane)!;
                    var queue = (LinkedList<RuntimeSchedulerAuxiliary420>)Property(lane, RemoteMsaccNegativeNames.LaneAuxiliaryProperty);
                    RuntimeSchedulerAuxiliary420 held = batch.Work[heldLane - RemoteMsaccNegativeNames.FirstNormalLane];
                    if (!ReferenceEquals(held.Node.List, queue) || held.Completion.IsCompleted)
                    {
                        continue;
                    }
                    lock (Field(capture, RemoteMsaccNegativeNames.CaptureGateField))
                    {
                        var metadata = (ErlDriverAsyncAccountingCaptureReply1151)Field(capture, RemoteMsaccNegativeNames.CaptureMetadataField);
                        Require(
                            metadata.Token != RemoteMsaccNegativeNames.InvalidCaptureToken
                            && metadata.Count == RemoteMsaccNegativeNames.AsyncWorkers
                            && metadata.Generation.Length == RemoteMsaccNegativeNames.GenerationTextLength,
                            RemoteMsaccNegativeNames.CaptureMetadataAssertion
                        );
                        Require(
                            ReferenceEquals(trap.PhysicalCompletion, operation.Completion)
                            && !trap.Retirement.IsCompleted,
                            RemoteMsaccNegativeNames.TrapPhysicalOwnerAssertion
                        );

                        return new Witness(
                            operation,
                            trap,
                            held,
                            metadata.Token,
                            metadata.Generation,
                            checked((int)metadata.Count),
                            capture.IsOpen1170,
                            operation.SourceReady.IsCompleted
                        );
                    }
                }
            }

            return null;
        }
    }

    private static bool ActualMailboxClosed1240(Mailbox mailbox)
    {
        // Read-only actual owner state; never invoke HaltRequested or set a field.
        lock (Field(mailbox, RemoteMsaccNegativeNames.MailboxGateField))
        {
            return (bool)Field(mailbox, RemoteMsaccNegativeNames.MailboxClosedField);
        }
    }

    private static bool ActualTokenlessMailboxHalt1240(OperationCanceledException error, Mailbox actualMailbox)
    {
        MethodInfo method = typeof(Mailbox).GetMethod(
            RemoteMsaccNegativeNames.MailboxHaltMethod,
            Members,
            null,
            Type.EmptyTypes,
            null
        )
            ?? throw new MissingMethodException(typeof(Mailbox).FullName, RemoteMsaccNegativeNames.MailboxHaltMethod);

        return error.CancellationToken == default && Equals(error.TargetSite, method)
            && ActualMailboxClosed1240(actualMailbox);
    }

    private static bool ExpectedTermination(
        Exception error,
        CancellationToken actualCancellation,
        Term actualReason,
        Mailbox actualMailbox
    )
    {
        if (error is AggregateException aggregate)
            return aggregate.InnerExceptions.Count != 0 && aggregate.InnerExceptions.All(item => ExpectedTermination(
                item,
                actualCancellation,
                actualReason,
                actualMailbox
            ));
        if (error is OperationCanceledException canceled)
            return actualCancellation.IsCancellationRequested
                && (canceled.CancellationToken == actualCancellation || ActualTokenlessMailboxHalt1240(canceled, actualMailbox));

        return error is ErlangException guest && guest.ExceptionClass == RemoteMsaccNegativeNames.ExitClassAtom && guest.Reason.Equals(actualReason);
    }

    private static int ActiveRequests(ProcessRuntime runtime)
    {
        lock (Field(runtime, RemoteMsaccNegativeNames.RuntimeGateField))
        {
            return ((IDictionary?)Field(runtime, RemoteMsaccNegativeNames.TrapRequestsField))?.Count ?? 0;
        }
    }

    internal static async Task<object> Run(JsonElement input, string mode)
    {
        // Reserve cleanup bookkeeping before effects; aggregate only after all independent releases.
        var failures = new Exception?[RemoteMsaccNegativeNames.CleanupFailureCapacity];
        int failuresCount = 0;
        Exception? primary = null, callerFailure = null, callbackFailure = null;
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? controller = null, firer = null;
        ProcessHandle? requester = null, killer = null;
        ProcessContext? caller = null;
        object? payload = null, bindings = null, actualResourceDomain = null;
        RuntimeDriverFrontend422? frontend = null;
        NativeResource524ImageOwner? image = null;
        Destructor? destructor = null;
        Task? closing = null;
        Term? port = null;
        bool loaded = false, releasedNormally = false, callbackReleased = false;
        int lane = 0, kind = RemoteMsaccNegativeNames.UnobservedNativeThreadType, calls = 0;
        bool actualAux = false;
        Witness? witness = null;
        var invocation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var killNow = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var killed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        byte[]? exceptionEtf = null;
        Term? physicalReason = null;
        CancellationToken actualCancellation = default;

        async Task Attempt(Func<Task> cleanup)
        {
            try
            {
                await cleanup().ConfigureAwait(false);
            }
            catch (Exception error)
            {
                failures[failuresCount++] = error;
            }
        }
        try
        {
            runtime = new ProcessRuntime(RemoteMsaccNegativeNames.NormalWorkers);
            ProcessRuntime actual = runtime;
            string manifest = input.GetProperty(RemoteMsaccNegativeNames.PackageManifestProperty).GetString()!;
            var options = new RuntimeDriverFrontendOptions(
                manifest,
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(manifest))),
                asyncThreads: RemoteMsaccNegativeNames.AsyncWorkers
            );
            await RuntimeBootstrapDriverFrontend422.Register(
                actual,
                actual.Modules,
                options.Bind(actual, CancellationToken.None),
                CancellationToken.None
            );
            frontend = (RuntimeDriverFrontend422)Field(actual, RemoteMsaccNegativeNames.FrontendField);
            await actual.InitializeModulesAsync(registry => RuntimeNativePrimitiveProviders.RegisterCatalog(actual, registry));
            await PreparedBeamFixtureScope171.RunAsync(
                actual,
                (registry, scope) =>
            {
                var dependencies = new BeamDependencySelection(new[] { RemoteMsaccNegativeNames.ErlangModule, RemoteMsaccNegativeNames.ErtsInternalModule, RemoteMsaccNegativeNames.DdllModule, RemoteMsaccNegativeNames.ListsModule, RemoteMsaccNegativeNames.MapsModule, RemoteMsaccNegativeNames.BinaryModule, RemoteMsaccNegativeNames.ObserverModule, RemoteMsaccNegativeNames.FileModule, RemoteMsaccNegativeNames.IoLibraryModule, RemoteMsaccNegativeNames.StringModule, RemoteMsaccNegativeNames.InitModule, RemoteMsaccNegativeNames.IoModule });
                var ddll = scope.Prepare(() => registry.PrepareBeam(File.ReadAllBytes(input.GetProperty(RemoteMsaccNegativeNames.DdllBeamProperty).GetString()!), dependencies));
                var observer = scope.Prepare(() => registry.PrepareBeam(File.ReadAllBytes(input.GetProperty(RemoteMsaccNegativeNames.ObserverBeamProperty).GetString()!), dependencies));

                return ValueTask.FromResult<Action<ModuleRegistry>>(r =>
                    Require(
                        r.PublishPreparedBeam(ddll, observer).Status == BeamPublicationStatus.Published,
                        RemoteMsaccNegativeNames.SourcePublicationAssertion
                    ));
            }
            );
            controller = new RuntimeNodeFoundationHostActor(actual);
            firer = new RuntimeNodeFoundationHostActor(actual);
            await controller.Ready.WaitAsync(RemoteMsaccNegativeNames.Deadline);
            await firer.Ready.WaitAsync(RemoteMsaccNegativeNames.Deadline);
            Require(
                (await controller.InvokeAsync(async context => await actual.Modules.Call(
                    context,
                    RemoteMsaccNegativeNames.DdllModule,
                    RemoteMsaccNegativeNames.LoadDriverFunction,
                    Term.String(input.GetProperty(RemoteMsaccNegativeNames.DriverDirectoryProperty).GetString()!),
                    Term.String(RemoteMsaccNegativeNames.Driver)
                ))).Equals(Term.A(RemoteMsaccNegativeNames.OkAtom)),
                RemoteMsaccNegativeNames.FixtureLoadAssertion
            );
            loaded = true;
            port = await controller.InvokeAsync(async context => await actual.Modules.Call(
                context,
                RemoteMsaccNegativeNames.ErlangModule,
                RemoteMsaccNegativeNames.OpenPortFunction,
                Term.Tuple(Term.A(RemoteMsaccNegativeNames.SpawnDriverAtom), Term.String(RemoteMsaccNegativeNames.Driver)),
                Cons.From(new[] { Term.A(RemoteMsaccNegativeNames.BinaryModule) })
            ));
            Term actualPort = port;

            Task<Term> Control(int command, string data = RemoteMsaccNegativeNames.EmptyControlData) => controller!.InvokeAsync(async context =>
                await actual.Modules.Call(
                    context,
                    RemoteMsaccNegativeNames.ErlangModule,
                    RemoteMsaccNegativeNames.PortControlFunction,
                    actualPort,
                    Term.I(command),
                    Term.String(data)
                ));
            long[] before = Snapshot(await Control(RemoteMsaccNegativeNames.SnapshotCommand));
            await Control(RemoteMsaccNegativeNames.SubmitHeldCommand, RemoteMsaccNegativeNames.NativeKeyZeroText);
            long[] heldNative = Snapshot(await Control(RemoteMsaccNegativeNames.SnapshotCommand));
            long nativeStop = Environment.TickCount64 + (long)RemoteMsaccNegativeNames.Deadline.TotalMilliseconds;
            while (heldNative[RemoteMsaccNegativeNames.InvokedSnapshotIndex] == before[RemoteMsaccNegativeNames.InvokedSnapshotIndex])
            {
                if (Environment.TickCount64 >= nativeStop)
                {
                    throw new TimeoutException(RemoteMsaccNegativeNames.InvokeTimeoutMessage);
                }
                await Task.Delay(RemoteMsaccNegativeNames.WitnessPollIntervalMilliseconds);
                heldNative = Snapshot(await Control(RemoteMsaccNegativeNames.SnapshotCommand));
            }
            Require(
                heldNative[RemoteMsaccNegativeNames.ReturnedSnapshotIndex] == before[RemoteMsaccNegativeNames.ReturnedSnapshotIndex]
                && heldNative[RemoteMsaccNegativeNames.ReleasedSnapshotIndex] == RemoteMsaccNegativeNames.NativeNotReleasedFlag,
                RemoteMsaccNegativeNames.HeldNativeInvokeAssertion
            );
            var provider = Provider(actual);
            bindings = provider.Bindings;
            image = provider.Owner;
            destructor = (environment, pointer) =>
            {
                try
                {
                    kind = NativeResource536Access.ThreadType();
                    lane = NativeResource536Access.AuxiliaryLane();
                    actualAux = NativeResource536Access.ActualSelectedAuxiliary(actual, lane);
                    entered.Set();
                    callbackReleased = release.Wait(global::Erlang.Tests.NativeResource524Names.Deadline);
                    Interlocked.Increment(ref calls);
                }
                catch (Exception error)
                {
                    callbackFailure = error;
                    entered.Set();
                }
            };
            payload = Allocate(actual, provider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
            BitString binary = Binary(actual, payload);
            actualResourceDomain = Property(actual, RemoteMsaccNegativeNames.NativeDomainProperty);
            object actualPayload = payload;
            await firer.Invoke(context =>
 {
     NativeResource536Access.ReleaseViaGuestGc(context, actualPayload, binary);

     return true;
 });
            Require(entered.Wait(global::Erlang.Tests.NativeResource524Names.Deadline), RemoteMsaccNegativeNames.DestructorEnteredAssertion);
            Require(
                callbackFailure is null
                && kind == RemoteMsaccNegativeNames.NormalNativeThreadType
                && lane is RemoteMsaccNegativeNames.FirstNormalLane or RemoteMsaccNegativeNames.SecondNormalLane
                && actualAux
                && calls == 0,
                RemoteMsaccNegativeNames.NormalAuxiliaryLaneAssertion
            );
            int freeLane = lane == RemoteMsaccNegativeNames.FirstNormalLane ? RemoteMsaccNegativeNames.SecondNormalLane : RemoteMsaccNegativeNames.FirstNormalLane;
            requester = actual.SpawnTraced(
                async context =>
            {
                try
                {
                    Require(RuntimeActorExecutionTurn.LaneIdentity == freeLane, RemoteMsaccNegativeNames.CallerLaneAssertion);
                    ready.TrySetResult(context);
                    await actual.Modules.Call(
                        context,
                        RemoteMsaccNegativeNames.ErlangModule,
                        RemoteMsaccNegativeNames.SystemFlagFunction,
                        Term.A(RemoteMsaccNegativeNames.Selector),
                        Term.A(RemoteMsaccNegativeNames.TrueAtom)
                    );
                }
                catch (Exception error)
                {
                    callerFailure = error;
                    ready.TrySetException(error);
                    if (error is ErlangException guest)
                        exceptionEtf = ExternalTermFormat.Encode(Term.Tuple(Term.A(guest.ExceptionClass), guest.Reason, guest.StackTraceTerm));
                }
                finally
                {
                    invocation.TrySetResult();
                }

                return Nil.Value;
            },
                null,
                false,
                null,
                schedulerBinding: freeLane
            );
            caller = await ready.Task.WaitAsync(RemoteMsaccNegativeNames.Deadline);
            actualCancellation = caller.Cancellation;
            await Until(() => (witness = Published(actual, caller, lane)) is not null);
            Require(witness is not null, RemoteMsaccNegativeNames.PublishedWitnessAssertion);
            Require(
                !invocation.Task.IsCompleted && !requester.Completion.IsCompleted && !release.IsSet,
                RemoteMsaccNegativeNames.QueuedAckHoldsProcessAssertion
            );
            if (mode == RemoteMsaccNegativeNames.CancelMode)
            {
                ProcessHandle target = requester;
                killer = actual.SpawnTraced(
                    async context =>
                {
                    await killNow.Task;
                    Require(RuntimeActorExecutionTurn.LaneIdentity == freeLane, RemoteMsaccNegativeNames.KillerLaneAssertion);
                    actual.Exit(context, target.Pid, Term.A(RemoteMsaccNegativeNames.KillSignalAtom));
                    killed.TrySetResult();

                    return Nil.Value;
                },
                    null,
                    false,
                    null,
                    schedulerBinding: freeLane
                );
                killNow.TrySetResult();
                await killed.Task.WaitAsync(RemoteMsaccNegativeNames.Deadline);
            }
            else closing = actual.DisposeAsync().AsTask();
            await Until(() => actualCancellation.IsCancellationRequested);
            Require(
                !requester.Completion.IsCompleted && !invocation.Task.IsCompleted
                && !witness!.Held.Completion.IsCompleted && calls == 0
                && (closing is null || !closing.IsCompleted),
                RemoteMsaccNegativeNames.TerminationRespectsAckAssertion
            );
            release.Set();
            await invocation.Task.WaitAsync(RemoteMsaccNegativeNames.Deadline);
            physicalReason = await requester.Completion.WaitAsync(RemoteMsaccNegativeNames.Deadline);
            Term expectedReason = Term.A(mode == RemoteMsaccNegativeNames.CancelMode ? RemoteMsaccNegativeNames.KilledReasonAtom : RemoteMsaccNegativeNames.ShutdownReasonAtom);
            Require(
                physicalReason.Equals(expectedReason) && caller.ExitReason is { } actualExit
                && actualExit.Equals(expectedReason),
                RemoteMsaccNegativeNames.PhysicalReasonAssertion
            );
            if (callerFailure is null || !ExpectedTermination(
                callerFailure,
                actualCancellation,
                expectedReason,
                caller.Mailbox
            ))
                throw new AggregateException(
                    RemoteMsaccNegativeNames.UnexpectedTerminationMessage,
                    callerFailure ?? new InvalidOperationException(RemoteMsaccNegativeNames.ReturnedAfterCancellationMessage)
                );
            // A genuine unrelated HOST throw has a real, different TargetSite.
            Exception foreignCancellation;
            try
            {
                throw new OperationCanceledException();
            }
            catch (OperationCanceledException foreign)
            {
                foreignCancellation = foreign;
            }
            using var wrongSource = new CancellationTokenSource();
            wrongSource.Cancel();
            Require(
                !ExpectedTermination(
                    foreignCancellation,
                    actualCancellation,
                    expectedReason,
                    caller.Mailbox
                )
                && !ExpectedTermination(
                    new OperationCanceledException(wrongSource.Token),
                    actualCancellation,
                    expectedReason,
                    caller.Mailbox
                )
                && !ExpectedTermination(
                    new InvalidOperationException(RemoteMsaccNegativeNames.UnrelatedFailureSentinelMessage),
                    actualCancellation,
                    expectedReason,
                    caller.Mailbox
                )
                && !ExpectedTermination(
                    new AggregateException(callerFailure, foreignCancellation),
                    actualCancellation,
                    expectedReason,
                    caller.Mailbox
                ),
                RemoteMsaccNegativeNames.UnrelatedFailuresRejectedAssertion
            );
            Require(
                callerFailure is not null && calls == RemoteMsaccNegativeNames.ExpectedDestructorCalls && callbackReleased && callbackFailure is null,
                RemoteMsaccNegativeNames.CancellationAndDestructorAssertion
            );
            await witness.Operation.Completion.WaitAsync(RemoteMsaccNegativeNames.Deadline);
            await witness.Trap.Retirement.WaitAsync(RemoteMsaccNegativeNames.Deadline);
            await ((Task)Property(payload, RemoteMsaccNegativeNames.PayloadRetirementProperty)).WaitAsync(RemoteMsaccNegativeNames.Deadline);
            if (mode == RemoteMsaccNegativeNames.CancelMode)
            {
                Require(
                    Text(await Control(RemoteMsaccNegativeNames.ReleaseCommand)) == RemoteMsaccNegativeNames.ReleasedReply,
                    RemoteMsaccNegativeNames.NativeReleaseAssertion
                );
                long[] end = Snapshot(await Control(RemoteMsaccNegativeNames.SnapshotCommand));
                long freeStop = Environment.TickCount64 + (long)RemoteMsaccNegativeNames.Deadline.TotalMilliseconds;
                while (end[RemoteMsaccNegativeNames.LocalJobsSnapshotIndex] != 0)
                {
                    if (Environment.TickCount64 >= freeStop)
                    {
                        throw new TimeoutException(RemoteMsaccNegativeNames.NativeFreeTimeoutMessage);
                    }
                    await Task.Delay(RemoteMsaccNegativeNames.WitnessPollIntervalMilliseconds);
                    end = Snapshot(await Control(RemoteMsaccNegativeNames.SnapshotCommand));
                }
                Require(
                    end[RemoteMsaccNegativeNames.ReturnedSnapshotIndex] > before[RemoteMsaccNegativeNames.ReturnedSnapshotIndex]
                    && end[RemoteMsaccNegativeNames.FreedSnapshotIndex] > before[RemoteMsaccNegativeNames.FreedSnapshotIndex],
                    RemoteMsaccNegativeNames.NativePhysicalFinishAssertion
                );
                await controller.InvokeAsync(async context => await actual.Modules.Call(
                    context,
                    RemoteMsaccNegativeNames.ErlangModule,
                    RemoteMsaccNegativeNames.PortCloseFunction,
                    actualPort
                ));
                port = null;
                Require(
                    (await controller.InvokeAsync(async context => await actual.Modules.Call(
                        context,
                        RemoteMsaccNegativeNames.DdllModule,
                        RemoteMsaccNegativeNames.UnloadDriverFunction,
                        Term.String(RemoteMsaccNegativeNames.Driver)
                    ))).Equals(Term.A(RemoteMsaccNegativeNames.OkAtom)),
                    RemoteMsaccNegativeNames.DriverUnloadAssertion
                );
                loaded = false;
                releasedNormally = true;
            }
        }
        catch (Exception error)
        {
            primary = error;
        }
        finally
        {
            release.Set();
            killNow.TrySetResult();
            if (payload is not null)
            {
                object captured = payload;
                await Attempt(() =>
 {
     bool owned;
     lock (Field(captured, RemoteMsaccNegativeNames.PayloadGateField))
     {
         owned = (long)Field(captured, RemoteMsaccNegativeNames.PayloadNativeCountField) > 0;
     }
     if (owned)
     {
         Call(captured, RemoteMsaccNegativeNames.ReleaseNativeMethod);
     }

     return Task.CompletedTask;
 });
                await Attempt(() => ((Task)Property(captured, RemoteMsaccNegativeNames.PayloadRetirementProperty)).WaitAsync(RemoteMsaccNegativeNames.Deadline));
            }
            if (bindings is not null)
            {
                await Attempt(() => ((Task)Call(bindings, RemoteMsaccNegativeNames.BindingRetireMethod)!).WaitAsync(RemoteMsaccNegativeNames.Deadline));
            }
            // VM close owns any still-open real port/native condition and image; it is always attempted.
            if (runtime is not null)
            {
                await Attempt(() => (closing ?? runtime.DisposeAsync().AsTask()).WaitAsync(RemoteMsaccNegativeNames.Deadline));
            }
            if (requester is not null)
            {
                await Attempt(() => requester.Completion.WaitAsync(RemoteMsaccNegativeNames.Deadline));
            }
            if (killer is not null)
            {
                await Attempt(() => killer.Completion.WaitAsync(RemoteMsaccNegativeNames.Deadline));
            }
            if (controller is not null)
            {
                await Attempt(() => controller.DisposeAsync().AsTask());
            }
            if (firer is not null)
            {
                await Attempt(() => firer.DisposeAsync().AsTask());
            }
            GC.KeepAlive(destructor);
        }
        if (primary is not null || failuresCount != 0)
        {
            if (failuresCount == 0)
            {
                ExceptionDispatchInfo.Capture(primary!).Throw();
            }
            var errors = new List<Exception>();
            if (primary is not null)
            {
                errors.Add(primary);
            }
            for (int i = 0; i < failuresCount; i++)
            {
                errors.Add(failures[i]!);
            }
            throw new AggregateException(errors);
        }
        Require(
            runtime is not null
            && ActiveRequests(runtime) == 0
            && image is { Disposed: true },
            RemoteMsaccNegativeNames.TrapAndImageDrainAssertion
        );
        Require(!loaded || mode == RemoteMsaccNegativeNames.CloseMode, RemoteMsaccNegativeNames.CancelDriverCleanupAssertion);
        ProcessRuntime finalRuntime = runtime!;
        object domain = actualResourceDomain ?? throw new InvalidOperationException(RemoteMsaccNegativeNames.MissingDomainMessage);
        Require(
            ReferenceEquals(domain, Field(finalRuntime, RemoteMsaccNegativeNames.NativeDomainField)),
            RemoteMsaccNegativeNames.SameDomainAssertion
        );
        object snapshot = Call(domain, RemoteMsaccNegativeNames.DomainSnapshotMethod)!;
        object heapSnapshot = Property(Property(caller!, RemoteMsaccNegativeNames.ProcessHeapProperty), RemoteMsaccNegativeNames.HeapSnapshotProperty);
        object dispatcher = Field(finalRuntime, RemoteMsaccNegativeNames.DispatcherField);
        lock (Field(dispatcher, RemoteMsaccNegativeNames.DispatcherGateField))
            Require(
                !((IEnumerable)Field(dispatcher, RemoteMsaccNegativeNames.SourceTailsField)).Cast<object>().Any(),
                RemoteMsaccNegativeNames.SourceTailDrainAssertion
            );
        Require(
            (long)Property(snapshot, RemoteMsaccNegativeNames.LiveNativeBytesProperty) == 0
                && (int)Property(snapshot, RemoteMsaccNegativeNames.LiveResourcesProperty) == 0

                && (int)Property(snapshot, RemoteMsaccNegativeNames.PendingResourcesProperty) == 0
                && (bool)Property(snapshot, RemoteMsaccNegativeNames.WorkerJoinedProperty)
            && (bool)Property(snapshot, RemoteMsaccNegativeNames.DomainCompletionProperty),
            RemoteMsaccNegativeNames.ResourcePhysicalDrainAssertion
        );
        Require(
            (int)Property(heapSnapshot, RemoteMsaccNegativeNames.RootFramesProperty) == 0
                && (long)Property(heapSnapshot, RemoteMsaccNegativeNames.LivePayloadWordsProperty) == 0

                && (int)Property(heapSnapshot, RemoteMsaccNegativeNames.HeapBlocksProperty) == 0
                && (int)Field(finalRuntime.Modules, RemoteMsaccNegativeNames.HelperUsesField) == 0,
            RemoteMsaccNegativeNames.HeapAndHelperDrainAssertion
        );
        Require(frontend is { RetirementCompletedSuccessfully: true }, RemoteMsaccNegativeNames.FrontendRetirementAssertion);
        object child = Field(frontend!, RemoteMsaccNegativeNames.FrontendChildField);
        Require(
            ((Task)Property(child, RemoteMsaccNegativeNames.ChildPhysicalExitProperty)).IsCompletedSuccessfully
            && ((Task)Property(child, RemoteMsaccNegativeNames.ChildPhysicalCleanupProperty)).IsCompletedSuccessfully,
            RemoteMsaccNegativeNames.ChildPhysicalCleanupAssertion
        );
        Witness actualWitness = witness ?? throw new InvalidOperationException(RemoteMsaccNegativeNames.MissingWitnessMessage);

        return new
        {
            Mode = mode,
            Passed = true,
            HeldLane = lane,
            Calls = calls,
            callbackReleased,
            Capture = new
            {
                actualWitness.Token,
                actualWitness.Generation,
                actualWitness.Count,
                OpenAtQueuedAck = actualWitness.Open,
                SourceReadyAtQueuedAck = actualWitness.SourceReady
            },
            ActualCanceledPhysicalProcessJoined = requester!.Completion.IsCompleted,
            ActualTrapAndOperationJoined = actualWitness.Trap.Retirement.IsCompleted && actualWitness.Operation.Completion.IsCompleted,
            PublicException = callerFailure!.GetType().FullName,
            ActualExceptionTarget = callerFailure.TargetSite?.DeclaringType?.FullName + RemoteMsaccNegativeNames.MethodDisplaySeparator + callerFailure.TargetSite?.Name,
            ActualMailboxClosed = ActualMailboxClosed1240(caller!.Mailbox),
            PhysicalExitReasonEtf = Convert.ToBase64String(ExternalTermFormat.Encode(physicalReason!)),
            FullGuestExceptionEtf = exceptionEtf is null ? null : Convert.ToBase64String(exceptionEtf),
            NativeReleasedViaPublicController = releasedNormally,
            VmClosed = true,
            Scope = RemoteMsaccNegativeNames.Scope
        };
    }
}
