using System.Security.Cryptography;
using System.Text.Json;
using System.Runtime.ExceptionServices;

namespace Erlang.Validation.AsyncMicrostates;

internal static class AsyncQueuedPortCloseChecks
{
    internal static async Task<object> Run(JsonElement input)
    {
        ProcessRuntime? runtime = null;
        ProcessHandle? actor = null;
        Term? outcome = null;
        Exception? primary = null;
        Exception? actorFailure = null;
        Exception? runtimeFailure = null;
        try
        {
            string manifest = input.GetProperty(AsyncQueuedPortCloseNames.PackageManifestProperty).GetString()!;
            string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(manifest)));
            runtime = await ProcessRuntime.CreateWithOptionsAsync(
                new RuntimeLibrarySelection(Array.Empty<RuntimeModuleSelection>()),
                new RuntimeLibraryStartupOptions(
                    RuntimeNativePrimitives.None,
                    false,
                    null,
                    new RuntimeDriverFrontendOptions(
                        manifest,
                        hash,
                        asyncThreads: AsyncQueuedPortCloseNames.WorkerCount
                    )
                ),
                default
            );
            ProcessRuntime actual = runtime;
            await actual.InitializeModulesAsync(
                registry => RuntimeNativePrimitiveProviders.RegisterCatalog(actual, registry)
            );
            await Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                actual,
                (registry, scope) =>
            {
                var dependencies = new BeamDependencySelection(new[]
                {
                    AsyncQueuedPortCloseNames.ErlangModule,
                    AsyncQueuedPortCloseNames.ErtsInternalModule,
                    AsyncQueuedPortCloseNames.DdllModule,
                    AsyncQueuedPortCloseNames.ListsModule,
                    AsyncQueuedPortCloseNames.MapsModule,
                    AsyncQueuedPortCloseNames.BinaryModule,
                    AsyncQueuedPortCloseNames.ObserverModule,
                    AsyncQueuedPortCloseNames.FileModule,
                    AsyncQueuedPortCloseNames.IoLibraryModule,
                    AsyncQueuedPortCloseNames.StringModule,
                    AsyncQueuedPortCloseNames.InitModule,
                    AsyncQueuedPortCloseNames.IoModule
                });
                var ddll = scope.Prepare(() => registry.PrepareBeam(
                    File.ReadAllBytes(input.GetProperty(AsyncQueuedPortCloseNames.DdllBeamProperty).GetString()!),
                    dependencies
                ));
                var observer = scope.Prepare(() => registry.PrepareBeam(
                    File.ReadAllBytes(input.GetProperty(AsyncQueuedPortCloseNames.ObserverBeamProperty).GetString()!),
                    dependencies
                ));

                return ValueTask.FromResult<Action<ModuleRegistry>>(target =>
                {
                    Require(
                        target.PublishPreparedBeam(ddll, observer).Status == BeamPublicationStatus.Published,
                        AsyncQueuedPortCloseNames.PublicationFailure
                    );
                });
            }
            );
            var done = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
            actor = actual.Spawn(async context =>
            {
                try
                {
                    Term loaded = await actual.Modules.Call(
                        context,
                        AsyncQueuedPortCloseNames.DdllModule,
                        AsyncQueuedPortCloseNames.LoadFunction,
                        Term.String(input.GetProperty(AsyncQueuedPortCloseNames.DriverDirectoryProperty).GetString()!),
                        Term.String(AsyncQueuedPortCloseNames.DriverName)
                    );
                    Require(
                        loaded.Equals(Term.A(AsyncQueuedPortCloseNames.OkAtom)),
                        AsyncQueuedPortCloseNames.LoadFailure
                    );
                    Exception? bodyFailure = null;
                    Exception? unloadFailure = null;
                    try
                    {
                        try
                        {
                            outcome = Term.Tuple(
                                Term.A(AsyncQueuedPortCloseNames.ReturnAtom),
                                await actual.Modules.Call(
                                    context,
                                    AsyncQueuedPortCloseNames.ObserverModule,
                                    AsyncQueuedPortCloseNames.RunFunction
                                )
                            );
                        }
                        catch (ErlangException error)
                        {
                            outcome = Term.Tuple(
                                Term.A(AsyncQueuedPortCloseNames.ExceptionAtom),
                                Term.A(error.ExceptionClass),
                                error.Reason,
                                error.StackTraceTerm
                            );
                        }
                        // Preserve the complete return or full Erlang exception before comparison/unload failure.
                        File.WriteAllBytes(
                            input.GetProperty(AsyncQueuedPortCloseNames.OwnEtfProperty).GetString()!,
                            ExternalTermFormat.Encode(outcome)
                        );
                    }
                    catch (Exception error)
                    {
                        bodyFailure = error;
                    }
                    finally
                    {
                        try
                        {
                            Term unloaded = await actual.Modules.Call(
                                context,
                                AsyncQueuedPortCloseNames.DdllModule,
                                AsyncQueuedPortCloseNames.UnloadFunction,
                                Term.String(AsyncQueuedPortCloseNames.DriverName)
                            );
                            Require(
                                unloaded.Equals(Term.A(AsyncQueuedPortCloseNames.OkAtom)),
                                AsyncQueuedPortCloseNames.UnloadFailure
                            );
                        }
                        catch (Exception error)
                        {
                            unloadFailure = error;
                        }
                    }
                    ThrowFailures(bodyFailure, unloadFailure);
                    done.TrySetResult(
                        outcome ?? throw new InvalidDataException(AsyncQueuedPortCloseNames.MissingOutcome)
                    );
                }
                catch (Exception error)
                {
                    done.TrySetException(error);
                }

                return Nil.Value;
            });
            outcome = await done.Task.WaitAsync(AsyncQueuedPortCloseNames.Deadline);
            Term original = ExternalTermFormat.Decode(File.ReadAllBytes(
                input.GetProperty(AsyncQueuedPortCloseNames.OriginalProperty).GetString()!
            ));
            Compare(outcome, original);
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
                    await actor.Completion.WaitAsync(AsyncQueuedPortCloseNames.ActorJoinDeadline);
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
                    runtimeFailure = error;
                }
            }
        }
        ThrowFailures(primary, actorFailure, runtimeFailure);

        return new
        {
            Passed = true,
            WholeOwnOutcome = Convert.ToBase64String(ExternalTermFormat.Encode(
                outcome ?? throw new InvalidDataException(AsyncQueuedPortCloseNames.MissingOutcome)
            )),
            WholeOwnOutcomePath = input.GetProperty(AsyncQueuedPortCloseNames.OwnEtfProperty).GetString(),
            EveryFixedFieldEqual = true,
            DynamicNativeThreadRolesChecked = true,
            ActorCompleted = actor!.Completion.IsCompletedSuccessfully,
            RuntimeClosed = true
        };
    }

    private static void Compare(Term outcome, Term original)
    {
        Require(original is MapTerm, AsyncQueuedPortCloseNames.ShapeFailure);
        var wrapper = (MapTerm)original;
        Require(
            wrapper.Entries.Count == AsyncQueuedPortCloseNames.OriginalWrapperFieldCount,
            AsyncQueuedPortCloseNames.ShapeFailure
        );
        Require(
            At(wrapper, AsyncQueuedPortCloseNames.FixtureKey).Equals(Term.A(AsyncQueuedPortCloseNames.ObserverModule)),
            AsyncQueuedPortCloseNames.ShapeFailure
        );
        Term ok = Term.Tuple(Term.A(AsyncQueuedPortCloseNames.ReturnAtom), Term.A(AsyncQueuedPortCloseNames.OkAtom));
        Require(
            At(wrapper, AsyncQueuedPortCloseNames.LoadKey).Equals(ok) && At(wrapper, AsyncQueuedPortCloseNames.UnloadKey).Equals(ok),
            AsyncQueuedPortCloseNames.ShapeFailure
        );
        MapTerm actual = Returned(outcome);
        MapTerm expected = Returned(At(wrapper, AsyncQueuedPortCloseNames.OutcomeKey));
        CompareMap(actual, expected, null);
        Require(
            At(actual, AsyncQueuedPortCloseNames.PoolSizeKey).Equals(Term.I(AsyncQueuedPortCloseNames.WorkerCount)),
            AsyncQueuedPortCloseNames.PhysicalFailure
        );
        foreach (MapTerm map in new[] { actual, expected })
        {
            var baseline = (MapTerm)At(map, AsyncQueuedPortCloseNames.BaselineKey);
            var queued = (MapTerm)At(map, AsyncQueuedPortCloseNames.QueuedKey);
            var held = (MapTerm)At(map, AsyncQueuedPortCloseNames.HeldKey);
            var final = (MapTerm)At(map, AsyncQueuedPortCloseNames.FinalKey);
            Require(
                At(map, AsyncQueuedPortCloseNames.CloseReplyKey).Equals(Term.Tuple(
                    Term.A(AsyncQueuedPortCloseNames.ReturnAtom),
                    Term.A(AsyncQueuedPortCloseNames.TrueAtom)
                )) && At(map, AsyncQueuedPortCloseNames.DownKey).Equals(Term.A(AsyncQueuedPortCloseNames.NormalAtom)),
                AsyncQueuedPortCloseNames.PhysicalFailure
            );
            Require(
                At(held, AsyncQueuedPortCloseNames.ReleasedKey).Equals(Term.I(AsyncQueuedPortCloseNames.Zero))
                    && At(held, AsyncQueuedPortCloseNames.LocalJobsKey).Equals(Term.I(AsyncQueuedPortCloseNames.HeldSourceJobs)),
                AsyncQueuedPortCloseNames.PhysicalFailure
            );
            Require(
                At(held, AsyncQueuedPortCloseNames.NativeThreadKey) is Integer tid && tid.Value > AsyncQueuedPortCloseNames.Zero,
                AsyncQueuedPortCloseNames.NativeThreadFailure
            );
            foreach (MapTerm noInvoke in new[] { baseline, queued, final })
            {
                Require(
                    At(noInvoke, AsyncQueuedPortCloseNames.NativeThreadKey).Equals(Term.I(AsyncQueuedPortCloseNames.Zero)),
                    AsyncQueuedPortCloseNames.NativeThreadFailure
                );
            }
            Require(
                At(final, AsyncQueuedPortCloseNames.LiveJobsKey).Equals(Term.I(AsyncQueuedPortCloseNames.Zero))
                && At(final, AsyncQueuedPortCloseNames.LiveStatesKey).Equals(Term.I(AsyncQueuedPortCloseNames.ControllerStates)),
                AsyncQueuedPortCloseNames.PhysicalFailure
            );
            RequireDelta(
                final,
                baseline,
                AsyncQueuedPortCloseNames.InvokedKey,
                AsyncQueuedPortCloseNames.SubmittedJobs
            );
            RequireDelta(
                final,
                baseline,
                AsyncQueuedPortCloseNames.ReturnedKey,
                AsyncQueuedPortCloseNames.SubmittedJobs
            );
            RequireDelta(
                final,
                baseline,
                AsyncQueuedPortCloseNames.FreedKey,
                AsyncQueuedPortCloseNames.SubmittedJobs
            );
            RequireDelta(
                final,
                baseline,
                AsyncQueuedPortCloseNames.ReadyKey,
                AsyncQueuedPortCloseNames.SurvivingReadyCallbacks
            );
            Require(
                At(map, AsyncQueuedPortCloseNames.CloseBeforeReleaseKey).Equals(Term.A(AsyncQueuedPortCloseNames.TrueAtom))
                && At(map, AsyncQueuedPortCloseNames.DownBeforeReleaseKey).Equals(Term.A(AsyncQueuedPortCloseNames.TrueAtom)),
                AsyncQueuedPortCloseNames.PhysicalFailure
            );
        }
    }

    private static void CompareMap(MapTerm actual, MapTerm expected, string? snapshot)
    {
        Require(actual.Entries.Count == expected.Entries.Count, AsyncQueuedPortCloseNames.ShapeFailure);
        foreach (var entry in actual.Entries)
        {
            Require(expected.Entries.Any(other => other.Key.Equals(entry.Key)), AsyncQueuedPortCloseNames.ShapeFailure);
            Term original = expected.Get(entry.Key);
            string key = entry.Key is Atom atom
                ? atom.Name
                : throw new InvalidDataException(AsyncQueuedPortCloseNames.ShapeFailure);
            if (entry.Value is MapTerm child)
            {
                Require(original is MapTerm, AsyncQueuedPortCloseNames.ShapeFailure);
                CompareMap(child, (MapTerm)original, key);
            }
            else if (snapshot != AsyncQueuedPortCloseNames.HeldKey || key != AsyncQueuedPortCloseNames.NativeThreadKey)
            {
                Require(entry.Value.Equals(original), AsyncQueuedPortCloseNames.FixedFieldFailure + key);
            }
        }
    }

    private static MapTerm Returned(Term value)
    {
        Require(
            value is TupleTerm tuple && tuple.Items.Count == AsyncQueuedPortCloseNames.ReturnedArity
            && tuple.Items[AsyncQueuedPortCloseNames.OutcomeTagIndex].Equals(Term.A(AsyncQueuedPortCloseNames.ReturnAtom))
            && tuple.Items[AsyncQueuedPortCloseNames.OutcomeValueIndex] is MapTerm,
            AsyncQueuedPortCloseNames.ShapeFailure
        );

        return (MapTerm)((TupleTerm)value).Items[AsyncQueuedPortCloseNames.OutcomeValueIndex];
    }

    private static void RequireDelta(
        MapTerm final,
        MapTerm baseline,
        string key,
        int delta
    )
    {
        Require(At(final, key).Equals(Add(baseline, key, delta)), AsyncQueuedPortCloseNames.PhysicalFailure);
    }

    private static Term Add(MapTerm map, string key, int delta)
    {
        Require(At(map, key) is Integer, AsyncQueuedPortCloseNames.ShapeFailure);

        return new Integer(((Integer)At(map, key)).Value + delta);
    }

    private static Term At(MapTerm map, string key) => map.Get(Term.A(key));

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }

    private static void ThrowFailures(params Exception?[] failures)
    {
        Exception[] actual = failures.OfType<Exception>().ToArray();
        if (actual.Length == 1)
        {
            ExceptionDispatchInfo.Capture(actual[0]).Throw();
        }
        if (actual.Length != 0)
        {
            throw new AggregateException(actual);
        }
    }
}
