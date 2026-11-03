using System.Security.Cryptography;
using System.Text.Json;
namespace Erlang.Validation.AsyncMicrostates;

internal static class AsyncDriverPositiveChecks
{
    internal static async Task<object> Run(JsonElement input, int count)
    {
        var observations = new List<object>();
        byte[]? driverEtf = null;
        string manifest = input.GetProperty(AsyncDriverPositiveNames.PackageManifestProperty).GetString()!;
        string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(manifest)));
        ProcessRuntime? runtime = null;
        ProcessHandle? actor = null;
        Exception? bodyFailure = null, actorFailure = null, runtimeFailure = null;
        try
        {
            runtime = await ProcessRuntime.CreateWithOptionsAsync(
                new RuntimeLibrarySelection(Array.Empty<RuntimeModuleSelection>()),
                new RuntimeLibraryStartupOptions(
                    RuntimeNativePrimitives.None,
                    false,
                    null,
                    new RuntimeDriverFrontendOptions(manifest, hash, asyncThreads: count)
                ),
                default
            );
            ProcessRuntime actual = runtime;

            await actual.InitializeModulesAsync(registry => RuntimeNativePrimitiveProviders.RegisterCatalog(actual, registry));
            await Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                actual,
                (registry, scope) =>
            {
                var dependencies = new BeamDependencySelection(new[] { AsyncDriverPositiveNames.ErlangModule, AsyncDriverPositiveNames.ErtsInternalModule, AsyncDriverPositiveNames.DdllModule, AsyncDriverPositiveNames.ListsModule, AsyncDriverPositiveNames.MapsModule, AsyncDriverPositiveNames.BinaryModule, AsyncDriverPositiveNames.ObserverModule, AsyncDriverPositiveNames.FileModule, AsyncDriverPositiveNames.IoLibraryModule, AsyncDriverPositiveNames.StringModule, AsyncDriverPositiveNames.InitModule, AsyncDriverPositiveNames.IoModule });
                var ddll = scope.Prepare(() => registry.PrepareBeam(File.ReadAllBytes(input.GetProperty(AsyncDriverPositiveNames.DdllBeamProperty).GetString()!), dependencies));
                var observer = scope.Prepare(() => registry.PrepareBeam(File.ReadAllBytes(input.GetProperty(AsyncDriverPositiveNames.ObserverBeamProperty).GetString()!), dependencies));

                return ValueTask.FromResult<Action<ModuleRegistry>>(r =>
    {
        if (r.PublishPreparedBeam(ddll, observer).Status != BeamPublicationStatus.Published)
        {
            throw new InvalidOperationException(AsyncDriverPositiveNames.PublicationFailureMessage);
        }
    });
            }
            );
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            actor = actual.Spawn(async context =>
            {
                try
                {
                    async Task<Term> Call(string name, params Term[] values)
                    {
                        Term value = await actual.Modules.Call(
                            context,
                            AsyncDriverPositiveNames.ErlangModule,
                            name,
                            values
                        );
                        var observation = new { Count = count, Function = name, Arguments = values.Select(v => Convert.ToBase64String(ExternalTermFormat.Encode(v))).ToArray(), Value = Convert.ToBase64String(ExternalTermFormat.Encode(value)) };
                        observations.Add(observation);
                        Console.WriteLine(JsonSerializer.Serialize(observation));

                        return value;
                    }
                    var selector = Term.A(AsyncDriverPositiveNames.SelectorAtom);

                    MapTerm[] AsyncRows(Term rows) => Cons.Items(rows).Cast<MapTerm>().Where(r => r.Get(Term.A(AsyncDriverPositiveNames.TypeKey)).Equals(Term.A(AsyncDriverPositiveNames.AsyncTypeAtom))).ToArray();

                    void Require(bool yes)
                    {
                        if (!yes)
                        {
                            throw new InvalidOperationException(AsyncDriverPositiveNames.ContractFailureMessage);
                        }
                    }
                    var disabled = AsyncRows(await Call(AsyncDriverPositiveNames.StatisticsFunction, selector));
                    Require(disabled.Length == count);
                    Require(disabled.All(r => ((MapTerm)r.Get(Term.A(AsyncDriverPositiveNames.CountersKey))).Entries.All(e => e.Value.Equals(Term.I(0)))));
                    Require((await Call(AsyncDriverPositiveNames.SystemFlagFunction, selector, Term.A(AsyncDriverPositiveNames.TrueAtom))).Equals(Term.A(AsyncDriverPositiveNames.FalseAtom)));
                    await Task.Delay(AsyncDriverPositiveNames.ObservationIntervalMilliseconds);
                    var enabled = AsyncRows(await Call(AsyncDriverPositiveNames.StatisticsFunction, selector));
                    Require(enabled.Length == count);
                    Require(enabled.All(r => ((MapTerm)r.Get(Term.A(AsyncDriverPositiveNames.CountersKey))).Entries.Any(e => e.Value is Integer i
                        && i.Value > 0)));
                    Require((await Call(AsyncDriverPositiveNames.SystemFlagFunction, selector, Term.A(AsyncDriverPositiveNames.FalseAtom))).Equals(Term.A(AsyncDriverPositiveNames.TrueAtom)));
                    var frozen = await Call(AsyncDriverPositiveNames.StatisticsFunction, selector);
                    await Task.Delay(AsyncDriverPositiveNames.ObservationIntervalMilliseconds);
                    var again = await Call(AsyncDriverPositiveNames.StatisticsFunction, selector);
                    var frozenAsync = AsyncRows(frozen);
                    var againAsync = AsyncRows(again);
                    Require(frozenAsync.Length == count && againAsync.Length == count);
                    Require(frozenAsync.Select(r => Convert.ToBase64String(ExternalTermFormat.Encode(r))).SequenceEqual(againAsync.Select(r => Convert.ToBase64String(ExternalTermFormat.Encode(r)))));
                    await Call(AsyncDriverPositiveNames.SystemFlagFunction, selector, Term.A(AsyncDriverPositiveNames.ResetAtom));
                    var reset = AsyncRows(await Call(AsyncDriverPositiveNames.StatisticsFunction, selector));
                    Require(reset.Length == count
                        && reset.All(r => ((MapTerm)r.Get(Term.A(AsyncDriverPositiveNames.CountersKey))).Entries.All(e => e.Value.Equals(Term.I(0)))));

                    var loaded = await actual.Modules.Call(
                        context,
                        AsyncDriverPositiveNames.DdllModule,
                        AsyncDriverPositiveNames.LoadDriverFunction,
                        Term.String(input.GetProperty(AsyncDriverPositiveNames.DriverDirectoryProperty).GetString()!),
                        Term.String(AsyncDriverPositiveNames.DriverName)
                    );
                    Require(loaded.Equals(Term.A(AsyncDriverPositiveNames.OkAtom)));
                    Exception? driverFailure = null, unloadFailure = null;
                    try
                    {
                        Term driverValue = await actual.Modules.Call(context, AsyncDriverPositiveNames.ObserverModule, AsyncDriverPositiveNames.ObserverRunFunction);
                        driverEtf = ExternalTermFormat.Encode(driverValue);
                        Console.WriteLine(JsonSerializer.Serialize(new { DriverCount = count, Value = Convert.ToBase64String(driverEtf) }));
                        AsyncOriginalComparison.Compare(
                            driverValue,
                            ExternalTermFormat.Decode(File.ReadAllBytes(input.GetProperty(count == AsyncDriverPositiveNames.SingleAsyncWorkerCount ? AsyncDriverPositiveNames.OriginalSingleWorkerProperty : AsyncDriverPositiveNames.OriginalTwoWorkerProperty).GetString()!)),
                            count
                        );
                        var driverMap = (MapTerm)driverValue;
                        Require(driverMap.Get(Term.A(AsyncDriverPositiveNames.AllCasesReturnedKey)).Equals(Term.A(AsyncDriverPositiveNames.TrueAtom)));
                        Require(driverMap.Get(Term.A(AsyncDriverPositiveNames.FinalLiveJobsZeroKey)).Equals(Term.A(AsyncDriverPositiveNames.TrueAtom)));
                        Require(driverMap.Get(Term.A(AsyncDriverPositiveNames.FinalOnlyControllerStateKey)).Equals(Term.A(AsyncDriverPositiveNames.TrueAtom)));
                    }
                    catch (Exception error)
                    {
                        driverFailure = error;
                    }
                    finally
                    {
                        try
                        {
                            Term unloaded = await actual.Modules.Call(
                                context,
                                AsyncDriverPositiveNames.DdllModule,
                                AsyncDriverPositiveNames.UnloadDriverFunction,
                                Term.String(AsyncDriverPositiveNames.DriverName)
                            );
                            Require(unloaded.Equals(Term.A(AsyncDriverPositiveNames.OkAtom)));
                        }
                        catch (Exception error)
                        {
                            unloadFailure = error;
                        }
                    }
                    if (driverFailure is not null && unloadFailure is not null)
                    {
                        throw new AggregateException(driverFailure, unloadFailure);
                    }
                    if (driverFailure is not null)
                    {
                        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(driverFailure).Throw();
                    }
                    if (unloadFailure is not null)
                    {
                        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(unloadFailure).Throw();
                    }
                    done.TrySetResult();

                }
                catch (Exception e)
                {
                    Console.WriteLine(AsyncDriverPositiveNames.PrimaryFailurePrefix + e.ToString());
                    if (e is ErlangException ee)
                    {
                        Console.WriteLine(JsonSerializer.Serialize(new { ErrorClass = ee.ExceptionClass, Reason = ee.Reason.ToString(), Stack = ee.StackTraceTerm.ToString(), Outcome = Convert.ToBase64String(ExternalTermFormat.Encode(Term.Tuple(Term.A(ee.ExceptionClass), ee.Reason, ee.StackTraceTerm))) }));
                    }
                    done.TrySetException(e);
                }

                return Nil.Value;
            });
            await done.Task.WaitAsync(AsyncDriverPositiveNames.Deadline);
        }
        catch (Exception error)
        {
            bodyFailure = error;
        }
        finally
        {
            if (actor is not null)
                try
                {
                    await actor.Completion.WaitAsync(AsyncDriverPositiveNames.Deadline);
                }
                catch (Exception error)
                {
                    actorFailure = error;
                }
            if (runtime is not null)
                try
                {
                    await runtime.DisposeAsync();
                }
                catch (Exception error)
                {
                    runtimeFailure = error;
                }
        }
        // All physical ownership actions were attempted before allocating an aggregate.
        Exception[] failures = new[] { bodyFailure, actorFailure, runtimeFailure }.OfType<Exception>().ToArray();
        if (failures.Length == 1)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }
        if (failures.Length != 0)
        {
            throw new AggregateException(failures);
        }

        return new { Count = count, Passed = true, Observations = observations, DriverEtf = Convert.ToBase64String(driverEtf ?? throw new InvalidDataException(AsyncDriverPositiveNames.MissingObservationMessage)) };
    }
}
