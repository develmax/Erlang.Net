using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Tests;

namespace Erlang.Validation.AsyncMicrostates;

internal static class PendingRpcGateControls1465
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static object Field(object owner, string name)
        => owner.GetType().GetField(name, Members)!.GetValue(owner)!;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(AsyncGateControlNames1465.FailurePrefix + message);
    }

    private static IEnumerable<Exception> Leaves(Exception error)
        => error is AggregateException aggregate
            ? aggregate.InnerExceptions.SelectMany(Leaves)
            : new[] { error };

    private static bool ActualClosedOrigin(Exception error)
    {
        if (error is not IOException || error.Message != DriverParentWireNames422.Closed) return false;

        return new StackTrace(error).GetFrames().Any(frame =>
        {
            Type? owner = frame.GetMethod()?.DeclaringType;

            string? method = frame.GetMethod()?.Name;

            return owner?.DeclaringType == typeof(RuntimeDriverFrontend422)
                && (owner.Name.StartsWith(AsyncGateControlNames1465.CloseMethod, StringComparison.Ordinal)
                    || owner.Name.StartsWith(AsyncGateControlNames1465.SuperviseMethod, StringComparison.Ordinal))
                || owner == typeof(RuntimeDriverFrontend422)
                && (method == AsyncGateControlNames1465.FrontendWaitMethod
                    || method == AsyncGateControlNames1465.FrontendSuperviseMethod);
        });
    }

    internal static async Task<object> Run(JsonElement input, string mode)
    {
        if (mode is not (AsyncGateControlNames1465.Unarmed or AsyncGateControlNames1465.WrongOperation
            or AsyncGateControlNames1465.DisposeBefore or AsyncGateControlNames1465.DuplicateRelease
            or AsyncGateControlNames1465.ChildDeath)) throw new ArgumentOutOfRangeException(nameof(mode));
        bool death = mode == AsyncGateControlNames1465.ChildDeath;
        using var responseGate = new ErlDriverHostResponseGate1427(
            ErlDriverHostOperation420.AsyncAccountingCapture1151,
            mode != AsyncGateControlNames1465.Unarmed
        );
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? controller = null;
        RuntimeDriverFrontend422? frontend = null;
        ErlDriverHostChild420? child = null;
        Term? port = null;
        Task<Term>? original = null;
        RuntimeMicrostateOperation1171? producer = null;
        ErlDriverHostResponseWitness1427? envelope = null;
        Exception? primary = null, operationFailure = null, pendingFailure = null;
        var cleanupFailures = new List<Exception>();
        var expectedFailures = new List<string>();
        var expectedLeaves = new HashSet<Exception>(ReferenceEqualityComparer.Instance);
        bool actualKillIssued = false, joinedBeforeManualRelease = false;
        int childPid = 0, actualCompletionContinuations = 0;
        string? childImageSha256 = null;
        string? wrongReplyType = null, wrongReplyEtf = null, wrongReplyBytesSha256 = null;
        long[]? wrongReplyFields = null;

        void CaptureKnownFailures()
        {
            if (child is null) return;
            if (Field(child.Peer, AsyncGateControlNames1465.PeerFailureField) is Exception peerFailure)
                foreach (Exception leaf in Leaves(peerFailure)) expectedLeaves.Add(leaf);
            if (Field(frontend!, AsyncGateControlNames1465.FatalField) is Exception fatal)
                foreach (Exception leaf in Leaves(fatal))
                    if (ActualClosedOrigin(leaf)
                        || leaf is IOException && leaf.Message == DriverParentWireNames422.Closed
                        && leaf.InnerException is null && child.Peer.Completion.IsCompleted)
                        expectedLeaves.Add(leaf);
            if (child.Completion.Exception is { } childFailure)
                foreach (Exception leaf in Leaves(childFailure))
                    if (leaf is IOException && leaf.Message == ErlDriverHostNames420.UnexpectedExit)
                        expectedLeaves.Add(leaf);
        }

        bool ExpectedTransport(Exception error)
        {
            if (!death || !actualKillIssued || child is null || !child.PhysicalExit.IsCompletedSuccessfully) return false;
            CaptureKnownFailures();
            if (error is AggregateException aggregate)
                return aggregate.InnerExceptions.Count != 0 && aggregate.InnerExceptions.All(ExpectedTransport);
            if (expectedLeaves.Contains(error) || ActualClosedOrigin(error)) return true;
            if (error is IOException && error.Message == ErlDriverHostNames420.Closed
                && error.InnerException is { } actualPeerFailure && ExpectedTransport(actualPeerFailure))
            {
                return new StackTrace(error).GetFrames().Any(frame =>
                    frame.GetMethod()?.DeclaringType == typeof(ErlDriverHostPeer420)
                    && frame.GetMethod()?.Name == AsyncGateControlNames1465.PeerClosedMethod);
            }

            return error is IOException && error.Message == DriverParentWireNames422.Closed
                && error.InnerException is { } inner && ExpectedTransport(inner);
        }

        async Task Join(Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception error)
            {
                bool expected = false;
                try
                {
                    expected = ExpectedTransport(error);
                }
                catch (Exception classifier)
                {
                    cleanupFailures.Add(new AggregateException(error, classifier));

                    return;
                }
                if (expected) expectedFailures.Add(error.ToString());
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
                asyncThreads: AsyncGateControlNames1465.AsyncWorkers
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
            var process = (Process)Field(child, AsyncGateControlNames1465.ChildProcessField);
            string childImage = Path.Combine(Path.GetDirectoryName(manifest)!, AsyncGateControlNames1465.ChildExecutable);
            Require(
                !process.HasExited && process.Id != Environment.ProcessId
                && string.Equals(process.MainModule!.FileName, childImage, StringComparison.OrdinalIgnoreCase),
                AsyncGateControlNames1465.ChildIdentity
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
            port = await controller.InvokeAsync(async context => await actual.Modules.Call(
                context,
                RemoteMsaccNegativeNames.ErlangModule,
                RemoteMsaccNegativeNames.OpenPortFunction,
                Term.Tuple(Term.A(RemoteMsaccNegativeNames.SpawnDriverAtom), Term.String(RemoteMsaccNegativeNames.Driver)),
                Cons.From(new[] { Term.A(RemoteMsaccNegativeNames.BinaryModule) })
            ));

            Task<Term> PublicFlag() => controller.InvokeAsync(async context => await actual.Modules.Call(
                context,
                RemoteMsaccNegativeNames.ErlangModule,
                RemoteMsaccNegativeNames.SystemFlagFunction,
                Term.A(RemoteMsaccNegativeNames.Selector),
                Term.A(RemoteMsaccNegativeNames.TrueAtom)
            ));

            if (mode == AsyncGateControlNames1465.DisposeBefore) responseGate.Dispose();
            if (mode == AsyncGateControlNames1465.WrongOperation)
            {
                Term value = await controller.InvokeAsync(async context => await actual.Modules.Call(
                    context,
                    RemoteMsaccNegativeNames.ErlangModule,
                    RemoteMsaccNegativeNames.PortControlFunction,
                    port!,
                    Term.I(RemoteMsaccNegativeNames.SnapshotCommand),
                    Term.String(RemoteMsaccNegativeNames.EmptyControlData)
                )).WaitAsync(RemoteMsaccNegativeNames.Deadline);
                wrongReplyType = value.GetType().FullName;
                wrongReplyEtf = Convert.ToBase64String(ExternalTermFormat.Encode(value));
                byte[] bytes = value is BitString bits ? bits.ToArray()
                    : Cons.Items(value).Select(item => checked((byte)((Integer)item).Value)).ToArray();
                wrongReplyBytesSha256 = Convert.ToHexString(SHA256.HashData(bytes));
                wrongReplyFields = System.Text.Encoding.ASCII.GetString(bytes).Split(',')
                    .Select(part => long.Parse(part, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                Require(wrongReplyFields.SequenceEqual(new long[] { 0, 0, 0, 0, 1, 0, 0, 0, 0 }),
                    AsyncGateControlNames1465.WrongReplyShape);
                Require(!responseGate.Reached.IsCompleted, AsyncGateControlNames1465.NoMarker);
            }
            else
            {
                original = PublicFlag();
                if (mode is AsyncGateControlNames1465.Unarmed or AsyncGateControlNames1465.DisposeBefore)
                {
                    Require(
                        (await original.WaitAsync(RemoteMsaccNegativeNames.Deadline)).Equals(
                        Term.A(ErlangBooleanAtoms.False)),
                        AsyncGateControlNames1465.PublicSuccess
                    );
                    Require(!responseGate.Reached.IsCompletedSuccessfully, AsyncGateControlNames1465.NoMarker);
                    if (mode == AsyncGateControlNames1465.Unarmed)
                        Require(!responseGate.Reached.IsCompleted, AsyncGateControlNames1465.NoMarker);
                    else Require(responseGate.Reached.IsFaulted, AsyncGateControlNames1465.NoMarker);
                }
                else
                {
                    envelope = await responseGate.Reached.WaitAsync(RemoteMsaccNegativeNames.Deadline);
                    Require(
                        envelope.Operation == ErlDriverHostOperation420.AsyncAccountingCapture1151
                        && !envelope.PendingCompletion.IsCompleted && !original.IsCompleted,
                        AsyncGateControlNames1465.Marker
                    );
                    ProcessContext requester = await controller.Ready;
                    lock (Field(actual, RemoteMsaccNegativeNames.RuntimeGateField)!)
                    {
                        var traps = (IDictionary)Field(actual, RemoteMsaccNegativeNames.TrapRequestsField)!;
                        foreach (StatisticsTrapRequest trap in (IEnumerable)traps[requester]!)
                        {
                            var abort = (Action<Exception>?)Field(trap, RemoteMsaccNegativeNames.AbortPreparationField);
                            if (abort?.Target is RuntimeMicrostateOperation1171 operation
                                && ReferenceEquals(trap.PhysicalCompletion, operation.Completion)) producer = operation;
                        }
                    }
                    Require(producer is { CaptureTask.IsCompleted: false }, AsyncGateControlNames1465.Owner);
                    Task continuation = envelope.PendingCompletion.ContinueWith(
                        _ => Interlocked.Increment(ref actualCompletionContinuations),
                        CancellationToken.None,
                        TaskContinuationOptions.None,
                        TaskScheduler.Default
                    );
                    if (death)
                    {
                        // Exact Process instance was admitted from this frontend and image above.
                        Require(!process.HasExited && process.Id == childPid,
                            AsyncGateControlNames1465.ChildIdentity);
                        actualKillIssued = true;
                        process.Kill(entireProcessTree: true);
                        await child.PhysicalExit.WaitAsync(RemoteMsaccNegativeNames.Deadline);
                        try
                        {
                            await envelope.PendingCompletion.WaitAsync(RemoteMsaccNegativeNames.Deadline);
                        }
                        catch (Exception error)
                        {
                            pendingFailure = error;
                        }
                        Require(
                            pendingFailure is not null && envelope.PendingCompletion.IsFaulted
                            && ExpectedTransport(pendingFailure!),
                            AsyncGateControlNames1465.PendingFailure
                        );
                        try
                        {
                            _ = await original.WaitAsync(RemoteMsaccNegativeNames.Deadline);
                        }
                        catch (Exception error)
                        {
                            operationFailure = error;
                        }
                        Require(operationFailure is not null && ExpectedTransport(operationFailure!),
                            AsyncGateControlNames1465.PendingFailure);
                        await Join(() => producer!.Completion.WaitAsync(RemoteMsaccNegativeNames.Deadline));
                        joinedBeforeManualRelease = true;
                    }
                    else
                    {
                        responseGate.Release();
                        responseGate.Release();
                        Require(
                            (await original.WaitAsync(RemoteMsaccNegativeNames.Deadline)).Equals(
                            Term.A(ErlangBooleanAtoms.False)),
                            AsyncGateControlNames1465.PublicSuccess
                        );
                        await producer!.Completion.WaitAsync(RemoteMsaccNegativeNames.Deadline);
                    }
                    await continuation.WaitAsync(RemoteMsaccNegativeNames.Deadline);
                    Require(actualCompletionContinuations == 1 && envelope.PendingCompletion.IsCompleted,
                        AsyncGateControlNames1465.FinalDrain);
                }
            }
        }
        catch (Exception error)
        {
            primary = error;
        }
        finally
        {
            responseGate.Release();
            responseGate.Dispose();
            if (original is not null) await Join(async () =>
 {
     _ = await original.WaitAsync(RemoteMsaccNegativeNames.Deadline);
 });
            if (producer is not null) await Join(() => producer.Completion.WaitAsync(RemoteMsaccNegativeNames.Deadline));
            if (runtime is not null) await Join(() => runtime.DisposeAsync().AsTask().WaitAsync(RemoteMsaccNegativeNames.Deadline));
            if (child is not null)
            {
                await Join(() => child.PhysicalExit.WaitAsync(RemoteMsaccNegativeNames.Deadline));
                await Join(() => child.PhysicalCleanup.WaitAsync(RemoteMsaccNegativeNames.Deadline));
            }
            if (controller is not null) await Join(() => controller.DisposeAsync().AsTask().WaitAsync(RemoteMsaccNegativeNames.Deadline));
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
            child is not null && child.PhysicalExit.IsCompletedSuccessfully
            && child.PhysicalCleanup.IsCompletedSuccessfully && runtime is not null && frontend is not null,
            AsyncGateControlNames1465.FinalDrain
        );
        lock (Field(runtime!, RemoteMsaccNegativeNames.RuntimeGateField)!)
            Require(
                (int)Field(frontend!, AsyncGateControlNames1465.FrontendLeaseCountField)! == 0
                && (((IDictionary?)Field(runtime!, RemoteMsaccNegativeNames.TrapRequestsField))?.Count ?? 0) == 0,
                AsyncGateControlNames1465.FinalDrain
            );

        return new
        {
            Passed = true,
            Mode = mode,
            ChildPid = childPid,
            ChildImageSha256 = childImageSha256,
            WrongOperationReplyType = wrongReplyType,
            WrongOperationReplyEtf = wrongReplyEtf,
            WrongOperationReplyBytesSha256 = wrongReplyBytesSha256,
            WrongOperationNativeFields = wrongReplyFields,
            ActualKillIssued = actualKillIssued,
            OriginalPendingFault = pendingFailure?.ToString(),
            OriginalPublicFault = operationFailure?.ToString(),
            OriginalTaskJoinedBeforeManualRelease = joinedBeforeManualRelease,
            ActualPendingCompletionContinuations = actualCompletionContinuations,
            ReachedActualResponse = envelope is not null,
            ExpectedOwnedTransportFailures = expectedFailures.ToArray(),
            ChildPhysicalExit = child!.PhysicalExit.IsCompletedSuccessfully,
            ChildPhysicalCleanup = child.PhysicalCleanup.IsCompletedSuccessfully,
            Scope = AsyncGateControlNames1465.FaultScope
        };
    }
}
