using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Tests;

namespace Erlang.Validation.AsyncMicrostates;

internal static class PendingRpcMatrixChecks1441
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static object Field(object owner, string name)
        => owner.GetType().GetField(name, Members)!.GetValue(owner)!;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(AsyncPendingRpcNames1441.FailurePrefix + message);
    }

    private static (StatisticsTrapRequest Trap, RuntimeMicrostateOperation1171 Operation)? Owner(
        ProcessRuntime runtime,
        ProcessContext caller
    )
    {
        lock (Field(runtime, RemoteMsaccNegativeNames.RuntimeGateField))
        {
            var requests = (IDictionary)Field(runtime, RemoteMsaccNegativeNames.TrapRequestsField);
            if (!requests.Contains(caller)) return null;
            foreach (StatisticsTrapRequest trap in (IEnumerable)requests[caller]!)
            {
                var abort = (Action<Exception>?)Field(trap, RemoteMsaccNegativeNames.AbortPreparationField);
                if (abort?.Target is RuntimeMicrostateOperation1171 operation
                    && operation.CaptureTask is not null
                    && ReferenceEquals(trap.PhysicalCompletion, operation.Completion))
                    return (trap, operation);
            }

            return null;
        }
    }

    private static int Leases(ProcessRuntime runtime, RuntimeDriverFrontend422 frontend)
    {
        lock (Field(runtime, RemoteMsaccNegativeNames.RuntimeGateField))
            return (int)Field(frontend, AsyncPendingRpcNames1441.FrontendLeaseCountField);
    }

    // Reuse the existing strict actual-token/mailbox-origin termination check.
    // It reads real failure/custody only and cannot grant an actor or response.
    private static bool Expected(
        Exception error,
        ProcessContext caller,
        Term reason
    ) => (bool)typeof(RemoteMsaccNegativeChecks).GetMethod(
        AsyncPendingRpcNames1441.ExpectedTerminationMethod,
        BindingFlags.Static | BindingFlags.NonPublic
    )!.Invoke(null, new object[] { error, caller.Cancellation, reason, caller.Mailbox })!;

    private sealed record CaptureFlags(
        bool Applied,
        bool AppliedSuccessfully,
        bool Finished,
        bool Busy,
        bool Disposing,
        bool RunningPending
    );

    private static CaptureFlags Flags(RuntimeAsyncAccountingCapture1170 capture)
    {
        lock (Field(capture, RemoteMsaccNegativeNames.CaptureGateField))
        {
            return new(
                (bool)Field(capture, AsyncPendingRpcNames1441.AppliedField),
                (bool)Field(capture, AsyncPendingRpcNames1441.AppliedSuccessfullyField),
                (bool)Field(capture, AsyncPendingRpcNames1441.FinishedField),
                (bool)Field(capture, AsyncPendingRpcNames1441.BusyField),
                (bool)Field(capture, AsyncPendingRpcNames1441.DisposingField),
                Field(capture, AsyncPendingRpcNames1441.RunningField) is Task { IsCompleted: false }
            );
        }
    }

    internal static async Task<object> Run(JsonElement input, string mode, bool noOp = false)
    {
        (ErlDriverHostOperation420 selected, bool kill) = mode switch
        {
            AsyncPendingRpcNames1441.CaptureKill => (ErlDriverHostOperation420.AsyncAccountingCapture1151, true),
            AsyncPendingRpcNames1441.CaptureClose => (ErlDriverHostOperation420.AsyncAccountingCapture1151, false),
            AsyncPendingRpcNames1441.ApplyKill => (ErlDriverHostOperation420.AsyncAccountingApply1151, true),
            AsyncPendingRpcNames1441.ApplyClose => (ErlDriverHostOperation420.AsyncAccountingApply1151, false),
            AsyncPendingRpcNames1441.FinishKill => (ErlDriverHostOperation420.AsyncAccountingFinish1151, true),
            AsyncPendingRpcNames1441.FinishClose => (ErlDriverHostOperation420.AsyncAccountingFinish1151, false),
            AsyncPendingRpcNames1441.ReleaseKill => (ErlDriverHostOperation420.AsyncAccountingRelease1151, true),
            AsyncPendingRpcNames1441.ReleaseClose => (ErlDriverHostOperation420.AsyncAccountingRelease1151, false),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        if (noOp && selected != ErlDriverHostOperation420.AsyncAccountingRelease1151)
            throw new ArgumentOutOfRangeException(nameof(noOp));
        using var responseGate = new ErlDriverHostResponseGate1427(selected);
        var cleanupFailures = new List<Exception>();
        Exception? primary = null, callerFailure = null, producerFailure = null;
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? controller = null;
        RuntimeDriverFrontend422? frontend = null;
        ErlDriverHostChild420? child = null;
        ProcessHandle? requester = null;
        ProcessContext? caller = null;
        Task? closing = null;
        StatisticsTrapRequest? ownedTrap = null;
        RuntimeMicrostateOperation1171? ownedOperation = null;
        ErlDriverHostResponseWitness1427? envelope = null;
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var invocation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration cancellationRegistration = default;
        Term? physicalReason = null;
        ulong epoch = 0;
        int childPid = 0;
        string? childImageSha256 = null;
        bool heldAfterCancellation = false, realPreparationOwned = false;
        CaptureFlags? phaseFlags = null;
        var expectedCleanupFailures = new List<string>();

        async Task Attempt(Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception error)
            {
                cleanupFailures.Add(error);
            }
        }

        async Task JoinOwned(Task task)
        {
            try
            {
                await task.WaitAsync(RemoteMsaccNegativeNames.Deadline).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                Term reason = Term.A(kill ? RemoteMsaccNegativeNames.KilledReasonAtom : RemoteMsaccNegativeNames.ShutdownReasonAtom);
                if (caller is not null && Expected(error, caller, reason))
                    expectedCleanupFailures.Add(error.ToString());
                else cleanupFailures.Add(error);
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
                asyncThreads: AsyncPendingRpcNames1441.AsyncWorkers
            );
            await RuntimeBootstrapDriverFrontend422.Register(
                actual,
                actual.Modules,
                options.Bind(actual, CancellationToken.None),
                CancellationToken.None,
                responseGate
            );
            frontend = (RuntimeDriverFrontend422)Field(actual, RemoteMsaccNegativeNames.FrontendField);
            child = (ErlDriverHostChild420)Field(frontend, RemoteMsaccNegativeNames.FrontendChildField);
            var process = (Process)Field(child, AsyncPendingRpcNames1441.ChildProcessField);
            string childImage = Path.Combine(Path.GetDirectoryName(manifest)!, AsyncPendingRpcNames1441.ChildExecutable);
            Require(
                !process.HasExited && process.Id != Environment.ProcessId
                && string.Equals(process.MainModule!.FileName, childImage, StringComparison.OrdinalIgnoreCase),
                AsyncPendingRpcNames1441.ChildIdentity
            );
            childPid = process.Id;
            childImageSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(childImage)));
            await actual.InitializeModulesAsync(registry => RuntimeNativePrimitiveProviders.RegisterCatalog(actual, registry));
            await PreparedBeamFixtureScope171.RunAsync(
                actual,
                (registry, scope) =>
            {
                var dependencies = new BeamDependencySelection(new[]
                {
                    RemoteMsaccNegativeNames.ErlangModule, RemoteMsaccNegativeNames.ErtsInternalModule,
                    RemoteMsaccNegativeNames.DdllModule, RemoteMsaccNegativeNames.ListsModule,
                    RemoteMsaccNegativeNames.MapsModule, RemoteMsaccNegativeNames.BinaryModule,
                    RemoteMsaccNegativeNames.ObserverModule, RemoteMsaccNegativeNames.FileModule,
                    RemoteMsaccNegativeNames.IoLibraryModule, RemoteMsaccNegativeNames.StringModule,
                    RemoteMsaccNegativeNames.InitModule, RemoteMsaccNegativeNames.IoModule
                });
                var ddll = scope.Prepare(() => registry.PrepareBeam(
                    File.ReadAllBytes(input.GetProperty(RemoteMsaccNegativeNames.DdllBeamProperty).GetString()!), dependencies));
                var observer = scope.Prepare(() => registry.PrepareBeam(
                    File.ReadAllBytes(input.GetProperty(RemoteMsaccNegativeNames.ObserverBeamProperty).GetString()!), dependencies));

                return ValueTask.FromResult<Action<ModuleRegistry>>(r => Require(
                    r.PublishPreparedBeam(ddll, observer).Status == BeamPublicationStatus.Published,
                    RemoteMsaccNegativeNames.SourcePublicationAssertion
                ));
            }
            );
            controller = new RuntimeNodeFoundationHostActor(actual);
            await controller.Ready.WaitAsync(RemoteMsaccNegativeNames.Deadline);
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
            _ = await controller.InvokeAsync(async context => await actual.Modules.Call(
                context,
                RemoteMsaccNegativeNames.ErlangModule,
                RemoteMsaccNegativeNames.OpenPortFunction,
                Term.Tuple(Term.A(RemoteMsaccNegativeNames.SpawnDriverAtom), Term.String(RemoteMsaccNegativeNames.Driver)),
                Cons.From(new[] { Term.A(RemoteMsaccNegativeNames.BinaryModule) })
            ));
            // No held native job/destructor: its release would require this reader.
            requester = actual.SpawnTraced(
                async context =>
            {
                try
                {
                    ready.TrySetResult(context);
                    await actual.Modules.Call(
                        context,
                        RemoteMsaccNegativeNames.ErlangModule,
                        RemoteMsaccNegativeNames.SystemFlagFunction,
                        Term.A(RemoteMsaccNegativeNames.Selector),
                        Term.A(noOp ? ErlangBooleanAtoms.False : RemoteMsaccNegativeNames.TrueAtom)
                    );
                }
                catch (Exception error)
                {
                    callerFailure = error;
                    ready.TrySetException(error);
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
                schedulerBinding: RemoteMsaccNegativeNames.FirstNormalLane
            );
            caller = await ready.Task.WaitAsync(RemoteMsaccNegativeNames.Deadline);
            cancellationRegistration = caller.Cancellation.Register(() => canceled.TrySetResult());
            envelope = await responseGate.Reached.WaitAsync(RemoteMsaccNegativeNames.Deadline);
            Require(
                envelope.Operation == selected && envelope.Request != 0
                && !envelope.PendingCompletion.IsCompleted && envelope.PayloadSha256?.Length == AsyncPendingRpcNames1441.Sha256TextLength,
                AsyncPendingRpcNames1441.Marker
            );
            long stop = Environment.TickCount64 + (long)RemoteMsaccNegativeNames.Deadline.TotalMilliseconds;
            while (Owner(actual, caller) is not { } owner)
            {
                if (Environment.TickCount64 >= stop) throw new TimeoutException(AsyncPendingRpcNames1441.Owner);
                await Task.Delay(RemoteMsaccNegativeNames.WitnessPollIntervalMilliseconds);
            }
            var actualOwner = Owner(actual, caller) ?? throw new InvalidOperationException(AsyncPendingRpcNames1441.Owner);
            ownedTrap = actualOwner.Trap;
            ownedOperation = actualOwner.Operation;
            lock (Field(actual, RemoteMsaccNegativeNames.RuntimeGateField))
                epoch = (ulong)Field(actual, AsyncPendingRpcNames1441.RuntimeEpochField);
            Require(
                epoch == AsyncPendingRpcNames1441.InitialEpoch && Leases(actual, frontend) == AsyncPendingRpcNames1441.SingleLease
                && !ownedOperation.Completion.IsCompleted && !ownedTrap.Retirement.IsCompleted && !process.HasExited,
                AsyncPendingRpcNames1441.Owner
            );
            if (noOp)
            {
                Require(
                    ownedOperation.CaptureTask is { IsCompletedSuccessfully: true }
                    && ownedOperation.CaptureTask.Result is not null
                    && ownedOperation.Request is { NoOp: true, Previous: false, Action: RuntimeMicrostateAction.Disable }
                    && !(bool)Field(ownedOperation, AsyncPendingRpcNames1441.PublishedField)
                    && ownedOperation.Batch is { State: RuntimeSchedulerAuxiliaryState420.Aborted }
                    && ownedOperation.Tail is not null,
                    AsyncPendingRpcNames1441.NoOpReleaseDisposal
                );
                RuntimeAsyncAccountingCapture1170 capture = ownedOperation.CaptureTask!.Result!;
                phaseFlags = Flags(capture);
                Require(
                    !phaseFlags.Applied && !phaseFlags.AppliedSuccessfully && !phaseFlags.Finished
                    && !phaseFlags.Busy && phaseFlags.Disposing && !phaseFlags.RunningPending
                    && !capture.IsOpen1170,
                    AsyncPendingRpcNames1441.NoOpReleaseDisposal
                );
                object actualDispatcher = Field(actual, RemoteMsaccNegativeNames.DispatcherField);
                lock (Field(actualDispatcher, RemoteMsaccNegativeNames.DispatcherGateField))
                    Require(
                        ownedOperation.Tail!.State == RuntimeMicrostateSourceTail1149.Phase.Terminal
                        && ownedOperation.Tail.Lane == AsyncPendingRpcNames1441.UnpublishedLane,
                        AsyncPendingRpcNames1441.NoOpReleaseDisposal
                    );
            }
            else if (selected == ErlDriverHostOperation420.AsyncAccountingCapture1151)
            {
                Require(
                    ownedOperation.CaptureTask is { IsCompleted: false }
                    && ownedOperation.Batch is null && !ownedOperation.SourceReady.IsCompleted,
                    AsyncPendingRpcNames1441.CapturePreparation
                );
                realPreparationOwned = true;
            }
            else
            {
                Require(
                    ownedOperation.CaptureTask is { IsCompletedSuccessfully: true }
                    && ownedOperation.CaptureTask.Result is not null
                    && ownedOperation.Batch is not null && ownedOperation.Tail is not null,
                    AsyncPendingRpcNames1441.Owner
                );
                RuntimeAsyncAccountingCapture1170 capture = ownedOperation.CaptureTask!.Result!;
                phaseFlags = Flags(capture);
                if (selected == ErlDriverHostOperation420.AsyncAccountingApply1151)
                    Require(
                        phaseFlags.Applied && !phaseFlags.AppliedSuccessfully && !phaseFlags.Finished
                        && phaseFlags.Busy && !phaseFlags.Disposing && phaseFlags.RunningPending
                        && capture.IsOpen1170 && !ownedOperation.SourceReady.IsCompleted
                        && !ownedOperation.Tail!.FirstCompletion.IsCompleted,
                        AsyncPendingRpcNames1441.ApplyHeld
                    );
                else if (selected == ErlDriverHostOperation420.AsyncAccountingFinish1151)
                    Require(
                        phaseFlags.Applied && phaseFlags.AppliedSuccessfully && phaseFlags.Finished
                        && phaseFlags.Busy && !phaseFlags.Disposing && phaseFlags.RunningPending
                        && capture.IsOpen1170 && !ownedOperation.SourceReady.IsCompleted
                        && ownedOperation.Tail!.FirstCompletion.IsCompletedSuccessfully
                        && !ownedOperation.Tail.Completion.IsCompleted,
                        AsyncPendingRpcNames1441.FinishHeld
                    );
                else
                    Require(
                        phaseFlags.Applied && phaseFlags.AppliedSuccessfully && phaseFlags.Finished
                        && !phaseFlags.Busy && phaseFlags.Disposing && !phaseFlags.RunningPending
                        && !capture.IsOpen1170 && ownedOperation.SourceReady.IsCompletedSuccessfully
                        && ownedOperation.Tail!.Completion.IsCompletedSuccessfully,
                        AsyncPendingRpcNames1441.ReleaseDisposal
                    );
            }
            if (kill)
            {
                ProcessHandle target = requester;
                await controller.Invoke(context =>
                {
                    actual.Exit(context, target.Pid, Term.A(RemoteMsaccNegativeNames.KillSignalAtom));

                    return true;
                });
            }
            else closing = actual.DisposeAsync().AsTask();
            await canceled.Task.WaitAsync(RemoteMsaccNegativeNames.Deadline);
            Require(
                !invocation.Task.IsCompleted && !requester.Completion.IsCompleted
                && !ownedOperation.Completion.IsCompleted && !envelope.PendingCompletion.IsCompleted
                && Leases(actual, frontend) == AsyncPendingRpcNames1441.SingleLease
                && (closing is null || !closing.IsCompleted),
                AsyncPendingRpcNames1441.CancellationFence
            );
            heldAfterCancellation = true;
            responseGate.Release();
            await invocation.Task.WaitAsync(RemoteMsaccNegativeNames.Deadline);
            physicalReason = await requester.Completion.WaitAsync(RemoteMsaccNegativeNames.Deadline);
            Term expectedReason = Term.A(kill ? RemoteMsaccNegativeNames.KilledReasonAtom : RemoteMsaccNegativeNames.ShutdownReasonAtom);
            Require(
                physicalReason.Equals(expectedReason) && caller.ExitReason is { } exitReason && exitReason.Equals(expectedReason),
                RemoteMsaccNegativeNames.PhysicalReasonAssertion
            );
            if (callerFailure is null) throw new InvalidOperationException(AsyncPendingRpcNames1441.UnexpectedReturn);
            if (!Expected(callerFailure, caller, expectedReason))
                throw new AggregateException(new InvalidOperationException(AsyncPendingRpcNames1441.UnexpectedFailure), callerFailure);
            try
            {
                await ownedOperation.Completion.WaitAsync(RemoteMsaccNegativeNames.Deadline);
            }
            catch (Exception error)
            {
                producerFailure = error;
                if (!Expected(error, caller, expectedReason))
                    throw new AggregateException(new InvalidOperationException(AsyncPendingRpcNames1441.UnexpectedFailure), error);
            }
            await ownedTrap.Retirement.WaitAsync(RemoteMsaccNegativeNames.Deadline);
        }
        catch (Exception error)
        {
            primary = error;
        }
        finally
        {
            // Host release must precede all joins that may depend on this reader.
            responseGate.Release();
            responseGate.Dispose();
            cancellationRegistration.Dispose();
            if (runtime is not null)
                await Attempt(() => (closing ?? runtime.DisposeAsync().AsTask()).WaitAsync(RemoteMsaccNegativeNames.Deadline));
            if (requester is not null)
            {
                await JoinOwned(invocation.Task);
                await Attempt(async () =>
 {
     _ = await requester.Completion.WaitAsync(RemoteMsaccNegativeNames.Deadline);
 });
            }
            if (ownedOperation is not null) await JoinOwned(ownedOperation.Completion);
            if (ownedTrap is not null) await Attempt(() => ownedTrap.Retirement.WaitAsync(RemoteMsaccNegativeNames.Deadline));
            if (child is not null)
            {
                await Attempt(() => child.PhysicalExit.WaitAsync(RemoteMsaccNegativeNames.Deadline));
                await Attempt(() => child.PhysicalCleanup.WaitAsync(RemoteMsaccNegativeNames.Deadline));
            }
            if (controller is not null) await Attempt(() => controller.DisposeAsync().AsTask().WaitAsync(RemoteMsaccNegativeNames.Deadline));
        }
        if (primary is not null || cleanupFailures.Count != 0)
        {
            var failures = new List<Exception>();
            if (primary is not null) failures.Add(primary);
            failures.AddRange(cleanupFailures);
            if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
            throw new AggregateException(failures);
        }
        Require(
            runtime is not null && frontend is { RetirementCompletedSuccessfully: true }
            && child is not null && child.PhysicalExit.IsCompletedSuccessfully && child.PhysicalCleanup.IsCompletedSuccessfully
            && Leases(runtime, frontend) == 0 && ownedOperation!.Completion.IsCompleted
            && ownedTrap!.Retirement.IsCompletedSuccessfully && envelope!.PendingCompletion.IsCompleted,
            AsyncPendingRpcNames1441.FinalOwnership
        );
        lock (Field(runtime!, RemoteMsaccNegativeNames.RuntimeGateField))
            Require(
                ((IDictionary)Field(runtime!, RemoteMsaccNegativeNames.TrapRequestsField)).Count == 0,
                AsyncPendingRpcNames1441.FinalOwnership
            );
        object dispatcher = Field(runtime!, RemoteMsaccNegativeNames.DispatcherField);
        lock (Field(dispatcher, RemoteMsaccNegativeNames.DispatcherGateField))
            Require(
                !((IEnumerable)Field(dispatcher, RemoteMsaccNegativeNames.SourceTailsField)).Cast<object>().Any(),
                RemoteMsaccNegativeNames.SourceTailDrainAssertion
            );
        Require(
            (int)Field(runtime!.Modules, RemoteMsaccNegativeNames.HelperUsesField) == 0,
            RemoteMsaccNegativeNames.HeapAndHelperDrainAssertion
        );

        // Explicit scalar projection only: never serialize witness/Task/frame/session.
        return new
        {
            Passed = true,
            Mode = mode,
            NoOpRequested = noOp,
            Operation = selected.ToString(),
            Request = envelope!.Request,
            EpochCounterAtHeldPublicRequest = epoch,
            PayloadSha256 = envelope.PayloadSha256,
            ChildPid = childPid,
            ChildImageSha256 = childImageSha256,
            ActualPreparationOwned = realPreparationOwned,
            ActualPhaseFlagsAtHeldReply = phaseFlags,
            ExpectedCleanupFailures = expectedCleanupFailures.ToArray(),
            HeldAfterRealCancellation = heldAfterCancellation,
            ActualExitReason = physicalReason!.ToString(),
            ActualProducerJoined = ownedOperation!.Completion.IsCompleted,
            ActualTrapRetired = ownedTrap!.Retirement.IsCompletedSuccessfully,
            FrontendLeases = Leases(runtime!, frontend!),
            ChildPhysicallyExited = child!.PhysicalExit.IsCompletedSuccessfully,
            ChildPhysicallyCleaned = child.PhysicalCleanup.IsCompletedSuccessfully,
            PublicFailure = callerFailure!.ToString(),
            ProducerFailure = producerFailure?.ToString(),
            Scope = AsyncPendingRpcNames1441.Scope
        };
    }
}
