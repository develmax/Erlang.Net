using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Erlang.Validation.AsyncMicrostates;

internal static class AsyncChildRollbackChecks
{
    internal static async Task<object> Run(JsonElement input)
    {
        ProcessRuntime? runtime = null;
        ProcessHandle? actor = null;
        object? result = null;
        Exception? primary = null;
        Exception? actorFailure = null;
        Exception? closeFailure = null;
        try
        {
            string manifest = input.GetProperty(AsyncChildRollbackNames.PackageManifest).GetString()!;
            runtime = await ProcessRuntime.CreateWithOptionsAsync(
                new RuntimeLibrarySelection(Array.Empty<RuntimeModuleSelection>()),
                new RuntimeLibraryStartupOptions(
                    RuntimeNativePrimitives.None,
                    false,
                    null,
                    new RuntimeDriverFrontendOptions(
                        manifest,
                        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(manifest))),
                        asyncThreads: AsyncChildRollbackNames.WorkerCount
                    )
                ),
                default
            );
            ProcessRuntime actual = runtime;
            await actual.InitializeModulesAsync(registry => RuntimeNativePrimitiveProviders.RegisterCatalog(actual, registry));
            await Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                actual,
                (registry, scope) =>
            {
                var dependencies = new BeamDependencySelection(new[]
                {
                    AsyncChildRollbackNames.Erlang,
                    AsyncChildRollbackNames.ErtsInternal,
                    AsyncChildRollbackNames.Ddll,
                    AsyncChildRollbackNames.Lists,
                    AsyncChildRollbackNames.Maps,
                    AsyncChildRollbackNames.Binary,
                    AsyncChildRollbackNames.File,
                    AsyncChildRollbackNames.IoLibrary,
                    AsyncChildRollbackNames.String,
                    AsyncChildRollbackNames.Init,
                    AsyncChildRollbackNames.Io
                });
                var ddll = scope.Prepare(() => registry.PrepareBeam(
                    File.ReadAllBytes(input.GetProperty(AsyncChildRollbackNames.DdllBeam).GetString()!),
                    dependencies
                ));

                return ValueTask.FromResult<Action<ModuleRegistry>>(target =>
                {
                    Require(target.PublishPreparedBeam(ddll).Status == BeamPublicationStatus.Published);
                });
            }
            );
            var done = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            actor = actual.Spawn(async context =>
            {
                Term? creator = null;
                bool loaded = false;
                Exception? bodyFailure = null;
                Exception? portFailure = null;
                Exception? unloadFailure = null;
                object? observed = null;
                try
                {
                    Term load = await actual.Modules.Call(
                        context,
                        AsyncChildRollbackNames.Ddll,
                        AsyncChildRollbackNames.Load,
                        Term.String(input.GetProperty(AsyncChildRollbackNames.DriverDirectory).GetString()!),
                        Term.String(AsyncChildRollbackNames.DriverName)
                    );
                    Require(load.Equals(Term.A(AsyncChildRollbackNames.Ok)));
                    loaded = true;
                    creator = await actual.Modules.Call(
                        context,
                        AsyncChildRollbackNames.Erlang,
                        AsyncChildRollbackNames.Open,
                        Term.Tuple(Term.A(AsyncChildRollbackNames.SpawnDriver), Term.String(AsyncChildRollbackNames.DriverName)),
                        Term.List(Term.A(AsyncChildRollbackNames.Binary))
                    );
                    Require(creator is PortTerm);
                    observed = await Prove(actual, context, (PortTerm)creator);
                }
                catch (Exception error)
                {
                    bodyFailure = error;
                }
                finally
                {
                    if (creator is PortTerm)
                    {
                        try
                        {
                            Require((await actual.Modules.Call(
                                context,
                                AsyncChildRollbackNames.Erlang,
                                AsyncChildRollbackNames.ClosePort,
                                creator
                            )).Equals(Term.A(AsyncChildRollbackNames.True)));
                        }
                        catch (Exception error)
                        {
                            portFailure = error;
                        }
                    }
                    if (loaded)
                    {
                        try
                        {
                            Require((await actual.Modules.Call(
                                context,
                                AsyncChildRollbackNames.Ddll,
                                AsyncChildRollbackNames.Unload,
                                Term.String(AsyncChildRollbackNames.DriverName)
                            )).Equals(Term.A(AsyncChildRollbackNames.Ok)));
                        }
                        catch (Exception error)
                        {
                            unloadFailure = error;
                        }
                    }
                }
                try
                {
                    ThrowFailures(bodyFailure, portFailure, unloadFailure);
                    done.TrySetResult(observed ?? throw new InvalidDataException(AsyncChildRollbackNames.Failure));
                }
                catch (Exception error)
                {
                    done.TrySetException(error);
                }

                return Nil.Value;
            });
            result = await done.Task.WaitAsync(AsyncChildRollbackNames.Deadline);
        }
        catch (Exception error)
        {
            primary = error;
        }
        finally
        {
            if (actor is not null)
            {
                try
                {
                    await actor.Completion.WaitAsync(AsyncChildRollbackNames.Deadline);
                }
                catch (Exception error)
                {
                    actorFailure = error;
                }
            }
            if (runtime is not null)
            {
                try
                {
                    await runtime.DisposeAsync();
                }
                catch (Exception error)
                {
                    closeFailure = error;
                }
            }
        }
        ThrowFailures(primary, actorFailure, closeFailure);

        return new { Passed = true, ActualRollback = result, ActorJoined = true, VmClosed = true };
    }

    private static async Task<object> Prove(ProcessRuntime runtime, ProcessContext context, PortTerm creator)
    {
        RuntimeDriverFrontend422 frontend = runtime.DriverFrontendForPort422(creator)
            ?? throw new InvalidDataException(AsyncChildRollbackNames.Failure);
        object gate = Field<object>(frontend, AsyncChildRollbackNames.GateField);
        var registry = Field<NativeDriverPortRegistry422>(frontend, AsyncChildRollbackNames.RegistryField);
        var drivers = Field<RuntimeDdllParent422>(frontend, AsyncChildRollbackNames.DriversField);
        var actors = Field<DriverParentActorHandles422>(frontend, AsyncChildRollbackNames.ActorsField);
        var transport = Field<DriverParentPeerTransport422>(frontend, AsyncChildRollbackNames.TransportField);
        var native = Field<Dictionary<ErlDriverPortIdentity420, NativeDriverPortRow422>>(
            registry,
            AsyncChildRollbackNames.NativeRowsField
        );
        var driverRows = Field<Dictionary<ulong, DdllParentRow422>>(drivers, AsyncChildRollbackNames.DriverRowsField);
        NativeDriverPortRow422 parent;
        NativeDriverPortRow422 child;
        ErlDriverChildReservation455 reservation;
        int physicalBefore;
        int activeBefore;
        ErlDriverChildReservation455? rollbackReservation = null;
        ErlDriverPortIdentity420? rollbackCreator = null;
        Exception? operationFailure = null;
        RuntimeProcessPayload641? reasonOrigin = null;
        RuntimeProcessPayload641? reasonBorrow = null;
        try
        {
            lock (gate)
            {
                parent = native.Values.Single(row => row.Identity.Equals(creator));
                DdllParentRow422 image = driverRows[parent.Native.Driver];
                physicalBefore = image.Ports.Count;
                activeBefore = image.ActivePortCount1311;
                reservation = registry.ReserveChild455(
                    parent.Native,
                    new ErlDriverChildReserve455(
                        AsyncChildRollbackNames.Attempt,
                        actors.Get(context.Self),
                        Encoding.UTF8.GetBytes(AsyncChildRollbackNames.ChildName)
                    ),
                    context.Self
                ) ?? throw new InvalidDataException(AsyncChildRollbackNames.Failure);
                rollbackReservation = reservation;
                rollbackCreator = parent.Native;
                child = native[new(parent.Native.Driver, reservation.Port)];
                Require(!child.Closed1311.Task.IsCompleted && !child.Stopped.Task.IsCompleted);
                Require(image.Ports.Count == physicalBefore + AsyncChildRollbackNames.ReservationCount);
                Require(image.ActivePortCount1311 == activeBefore + AsyncChildRollbackNames.ReservationCount);
                Reject(() => registry.AbortChild455(
                    parent.Native,
                    new(AsyncChildRollbackNames.Attempt, checked(reservation.Port + AsyncChildRollbackNames.WrongIncrement), false)
                ));
                Reject(() => registry.AbortChild455(
                    parent.Native,
                    new(checked(AsyncChildRollbackNames.Attempt + AsyncChildRollbackNames.WrongIncrement), reservation.Port, false)
                ));
                Require(!child.Closed1311.Task.IsCompleted && !child.Stopped.Task.IsCompleted);
            }
            Term reason = Term.Tuple(Term.A(AsyncChildRollbackNames.ReasonAtom), Term.I(AsyncChildRollbackNames.ReasonValue));
            reasonOrigin = RuntimeProcessPayload641.Prepare641(runtime, context, reason);
            lock (gate)
            {
                child.SetReason641(reason, reasonOrigin);
                reasonBorrow = child.Reason641 ?? throw new InvalidDataException(AsyncChildRollbackNames.Failure);
                Require(!ReferenceEquals(reasonBorrow, reasonOrigin));
                Require(!reasonBorrow.Completion641.IsCompleted);
            }
            reasonOrigin.Close641();
            MethodInfo close = typeof(NativeDriverPortRegistry422).GetMethod(
                AsyncChildRollbackNames.CloseMethod,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(NativeDriverPortRow422), typeof(DriverParentPeerTransport422) },
                null
            ) ?? throw new MissingMethodException(AsyncChildRollbackNames.CloseMethod);
            Task actualClose = (Task)close.Invoke(registry, new object[] { child, transport })!;
            var transition = new ErlDriverChildTransition455(AsyncChildRollbackNames.Attempt, reservation.Port, false);
            // Real reservation, real Close invocation and real authenticated rollback
            // entry. Reflection only captures witnesses/invokes the existing entry;
            // no dictionary, flag, native pointer or completion is forged.
            registry.AbortChild455(parent.Native, transition);
            {
                await actualClose.WaitAsync(AsyncChildRollbackNames.FenceDeadline);
                lock (gate)
                {
                    Require(child.Closed1311.Task.IsCompletedSuccessfully && child.Stopped.Task.IsCompletedSuccessfully);
                    Require(child.NativeReferenceReleased && !native.ContainsKey(child.Native));
                    Require(child.Reason641 is null);
                    Require(child.ChildAttempt455 is { Aborted: true, Row: null });
                    DdllParentRow422 image = driverRows[parent.Native.Driver];
                    Require(image.Ports.Count == physicalBefore && image.ActivePortCount1311 == activeBefore);
                    registry.AbortChild455(parent.Native, transition);
                    Require(image.Ports.Count == physicalBefore && image.ActivePortCount1311 == activeBefore);
                    Require(registry.ReserveChild455(
                        parent.Native,
                        new(AsyncChildRollbackNames.Attempt, actors.Get(context.Self), Encoding.UTF8.GetBytes(AsyncChildRollbackNames.ChildName)),
                        context.Self
                    ) is null);
                    Require(!child.LogicalStopReceived1311 && !child.PhysicallyStopped);
                }
            }

            await reasonBorrow.Completion641.WaitAsync(AsyncChildRollbackNames.FenceDeadline);
            Require(reasonBorrow.Completion641.IsCompletedSuccessfully);

            return new
            {
                GenuineCreator = true,
                ActualReserve = true,
                WrongPortAndAttemptRejected = true,
                ActualCloseJoined = actualClose.IsCompletedSuccessfully,
                LogicalAndPhysicalFencesSettled = true,
                PhysicalDdllCountRestored = true,
                RepeatedAbortIdempotent = true,
                ReserveRetryReturnsAbortedTombstone = true,
                NativeCertificateNotFabricated = true,
                ActualReasonBorrowReleased = true,
                ActualReasonBorrowPhysicallySettled = reasonBorrow.Completion641.IsCompletedSuccessfully
            };
        }
        catch (Exception error)
        {
            operationFailure = error;
            throw;
        }
        finally
        {
            Exception? rollbackFailure = null;
            Exception? originFailure = null;
            Exception? borrowFailure = null;
            try
            {
                if (rollbackReservation is not null && rollbackCreator is not null)
                {
                    registry.AbortChild455(
                        rollbackCreator,
                        new(AsyncChildRollbackNames.Attempt, rollbackReservation.Port, false)
                    );
                }
            }
            catch (Exception error)
            {
                rollbackFailure = error;
            }
            try
            {
                reasonOrigin?.Close641();
            }
            catch (Exception error)
            {
                originFailure = error;
            }
            try
            {
                // Failure teardown occurs after success assertions, so an old
                // orphan can be retired without hiding the recorded RED.
                reasonBorrow?.Close641();
            }
            catch (Exception error)
            {
                borrowFailure = error;
            }
            if (rollbackFailure is not null || originFailure is not null || borrowFailure is not null)
            {
                ThrowFailures(
                    operationFailure,
                    rollbackFailure,
                    originFailure,
                    borrowFailure
                );
            }
        }
    }

    private static T Field<T>(object owner, string name)
    {
        return (T)(owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(owner.GetType().FullName, name)).GetValue(owner)!;
    }

    private static void Reject(Action action)
    {
        bool rejected = false;
        try
        {
            action();
        }
        catch (InvalidDataException)
        {
            rejected = true;
        }
        Require(rejected);
    }

    private static void Require(bool condition)
    {
        if (!condition)
        {
            throw new InvalidDataException(AsyncChildRollbackNames.Failure);
        }
    }

    private static void ThrowFailures(params Exception?[] errors)
    {
        Exception[] failures = errors.OfType<Exception>().ToArray();
        if (failures.Length == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }
        if (failures.Length > 1)
        {
            throw new AggregateException(failures);
        }
    }
}
