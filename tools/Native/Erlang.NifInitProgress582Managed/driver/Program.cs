using System.Diagnostics;
using System.Text.Json;
using Erlang;
using static InitProgressNames617;
using static InitProgressAdmission617;

internal static class Program
{
    private static TimeSpan Remaining(Stopwatch clock)
    {
        TimeSpan result = InvocationDeadline - clock.Elapsed;
        if (result <= TimeSpan.Zero) throw new TimeoutException(Failure);

        return result;
    }

    private static async Task<int> Main(string[] args)
    {
        Require(OperatingSystem.IsLinux() && args.Length == ArgumentCount);
        using var admission = new InitProgressAdmission617(args[0], args[1]);
        string output = System.IO.Path.GetFullPath(args[2]);
        Require(!Directory.Exists(output));
        foreach (var item in new[] { (LogEnvironment, LogFile), (AckEnvironment, AckFile), (ReadyEnvironment, ReadyFile) })
        {
            string actual = Environment.GetEnvironmentVariable(item.Item1)!;
            Require(actual is not null && System.IO.Path.GetFullPath(actual) == System.IO.Path.Combine(output, item.Item2)
                && !System.IO.File.Exists(actual));
        }
        Directory.CreateDirectory(output);
        string log = System.IO.Path.Combine(output, LogFile);
        byte[] originalBytes = System.IO.File.ReadAllBytes(admission.File(OriginalObserved));
        Term original = ExternalTermFormat.Decode(originalBytes);
        Term originalProfile = ExternalTermFormat.Decode(System.IO.File.ReadAllBytes(admission.File(OriginalProfile)));
        Term originalCleanup = ExternalTermFormat.Decode(System.IO.File.ReadAllBytes(admission.File(OriginalCleanup)));
        Require(original.Equals(InitProgressObservation617.Expected()) && originalProfile.Equals(InitProgressObservation617.ExpectedProfile()));
        var errors = new List<string>();
        Term? observed = null, profile = null, cleanup = null;
        IReadOnlyList<bool>? physicalPurgeResults = null;
        byte[]? observedBytes = null;
        bool runtimeClosed = false, controllerSettled = false;
        var clock = Stopwatch.StartNew();
        var runtime = new ProcessRuntime(LogicalLanes);
        try
        {
            PreparedBeamImage observer = runtime.Modules.PrepareBeam(
                System.IO.File.ReadAllBytes(admission.File(ObserverBeam)),
                new BeamDependencySelection([ErlangModule, AckModule, Subject, Observer])
            );
            await runtime.InitializeModulesAsync(registry =>
            {
                Require(observer.Name == Observer && registry.PublishPreparedBeam(observer).Status == BeamPublicationStatus.Published);
            }).AsTask().WaitAsync(Remaining(clock));
            InitProgressPolicy617.Install(runtime, admission);
            var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ProcessHandle controller = runtime.Spawn(async context =>
            {
                try
                {
                    try
                    {
                        Term value = await runtime.Modules.Call(
                            context,
                            Observer,
                            Capture,
                            new BitString(System.IO.File.ReadAllBytes(admission.File(AckBeam))),
                            new BitString(System.IO.File.ReadAllBytes(admission.File(Beam1))),
                            new BitString(System.IO.File.ReadAllBytes(admission.File(Beam2))),
                            Term.String(System.IO.Path.ChangeExtension(admission.NativePath(AckId), null)!),
                            Term.String(System.IO.Path.ChangeExtension(admission.NativePath(FirstId), null)!),
                            Term.String(System.IO.Path.ChangeExtension(admission.NativePath(SecondId), null)!)
                        );
                        observed = Term.Tuple(Term.A(Returned), value);
                    }
                    catch (ErlangException error)
                    {
                        observed = Term.Tuple(
                            Term.A(Raised),
                            Term.A(error.ExceptionClass),
                            error.Reason,
                            error.StackTraceTerm
                        );
                    }
                    if (observed is not null)
                    {
                        observedBytes = ExternalTermFormat.Encode(observed);
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(output, ObservedFile), observedBytes);
                    }
                    profile = await runtime.Modules.Call(
                        context,
                        ErlangModule,
                        Get,
                        Term.A(ProfileKey)
                    );
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(output, ProfileFile), ExternalTermFormat.Encode(profile));
                }
                catch (Exception failure)
                {
                    errors.Add(failure.ToString());
                }
                finally
                {
                    try
                    {
                        (cleanup, physicalPurgeResults) = await InitProgressObservation617.Cleanup(
                            runtime,
                            context,
                            log,
                            clock,
                            errors
                        );
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(output, CleanupFile), ExternalTermFormat.Encode(cleanup));
                    }
                    catch (Exception failure)
                    {
                        errors.Add(failure.ToString());
                    }
                    finished.TrySetResult();
                }

                return Nil.Value;
            });
            await finished.Task.WaitAsync(Remaining(clock));
            await controller.Completion.WaitAsync(Remaining(clock));
            controllerSettled = true;
        }
        catch (Exception failure)
        {
            errors.Add(failure.ToString());
        }
        finally
        {
            try
            {
                Task physicalCleanup = runtime.DisposeAsync().AsTask();
                await physicalCleanup.WaitAsync(Remaining(clock));
                runtimeClosed = true;
            }
            catch (Exception failure)
            {
                errors.Add(failure.ToString());
            }
            try
            {
                admission.Verify();
            }
            catch (Exception failure)
            {
                errors.Add(failure.ToString());
            }
        }
        string[] events = [];
        try
        {
            events = InitProgressObservation617.Events(log, complete: true);
        }
        catch (Exception failure)
        {
            errors.Add(failure.ToString());
        }
        var checks = new Dictionary<string, bool>();

        void Check(string name, Func<bool> predicate)
        {
            try
            {
                checks[name] = predicate();
            }
            catch (Exception error)
            {
                errors.Add(error.ToString());
                checks[name] = false;
            }
        }
        Check(WholeNine, () => observed is not null && observed.Equals(original));
        Check(WholeRawEqual, () => observedBytes is not null && observedBytes.SequenceEqual(originalBytes));
        Check(ActualProfile, () => profile is not null && profile.Equals(originalProfile));
        Check(CleanupPrefix, () => cleanup is not null && InitProgressObservation617.CleanupPrefixEquals(cleanup, originalCleanup));
        Check(PhysicalPurges, () => physicalPurgeResults is { Count: CleanupSteps } && physicalPurgeResults.All(value => value));
        Check(NativeEvents, () => InitProgressObservation617.EventContract(events));
        Check(RuntimeClosed, () => runtimeClosed);
        Check(ControllerSettled, () => controllerSettled);
        bool passed = errors.Count == 0 && checks.Values.All(value => value);
        System.IO.File.WriteAllText(
            System.IO.Path.Combine(output, CompletionFile),
            JsonSerializer.Serialize(
                new
                {
                    Passed = passed,
                    SubjectHead = admission.Head,
                    Route = admission.RouteValue,
                    Checks = checks,
                    Errors = errors,
                    EventValues = events,
                    LogicalActorLanes = LogicalLanes,
                    ProfileMeaning = ProfileMeaning,
                    OuterSeconds = InvocationDeadline.TotalSeconds,
                    ConstructorMilliseconds = ConstructorBound,
                    WholeCleanupRawEqualityClaimed = false,
                    PhysicalPurgeResults = physicalPurgeResults,
                    NativeFixturesDuplicated = false,
                    ApiDelta = 0,
                    FullContractDelta = 0
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );

        return passed ? 0 : 1;
    }
}
