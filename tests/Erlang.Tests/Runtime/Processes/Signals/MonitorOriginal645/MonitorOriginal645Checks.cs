using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Differential;
using Erlang.Tests;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.MonitorOriginal645.MonitorOriginal645Names;

namespace Erlang.Tests.MonitorOriginal645;

internal static class MonitorOriginal645Checks
{
    internal static async Task Run()
    {
        string folder = Path.GetFullPath(Path.Combine(Output, Guid.NewGuid().ToString(GuidFormat)));
        Directory.CreateDirectory(folder);
        var rows = new List<Term>();
        var errors = new List<Exception>();
        var completed = new List<string>();
        string host = typeof(MonitorOriginal645Checks).Assembly.Location;
        string runtime = typeof(ProcessRuntime).Assembly.Location;
        string hostBefore = Sha(host), runtimeBefore = Sha(runtime);
        foreach (var pair in new (string Family, Func<Task> Run)[]
        {
            (AtomicsFamily, () => Atomics(rows, folder)),
            (CountersFamily, () => Counters(rows, folder)),
            (RegexFamily, () => Regex(rows, folder)),
            (BinaryFamily, () => Binary(rows, folder)),
            (OrdinaryFamily, () => Ordinary(rows, folder))
        })
            try
            {
                await pair.Run();
                completed.Add(pair.Family);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        bool sourceEqual = Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(ObserverBase64))) == ObserverSha256;
        bool dllEqual = hostBefore == Sha(host) && runtimeBefore == Sha(runtime);
        bool outcomes = rows.Count == ExpectedRows && rows.All(row => row is TupleTerm { Items: [_, _, TupleTerm { Items: [Atom { Name: Value }, Atom { Name: ErlangBooleanAtoms.True }] }] });
        File.WriteAllText(
            Path.Combine(folder, Receipt),
            JsonSerializer.Serialize(new
            {
                Schema,
                OriginalPin,
                ObserverSha256,
                SourceBytesEqual = sourceEqual,
                HostSha256 = hostBefore,
                RuntimeSha256 = runtimeBefore,
                DllsUnchanged = dllEqual,
                ExpectedRows = MonitorOriginal645Names.ExpectedRows,
                CapturedRows = rows.Count,
                FamilyCleanupCompleted = completed,
                Passed = errors.Count == 0 && completed.Count == Families && sourceEqual && dllEqual && outcomes,
                Errors = errors.Select(error => error.ToString()).ToArray(),
                SourceOutcomeRowsAllSucceeded = outcomes,
                RawOutcomesRewritten = false,
                FrameProjection = false,
                OriginalPhysicalProof = false,
                OrdinaryReferenceIsNativeResource = false,
                NativeNifCompared = false,
                ApiDelta = 0,
                FullContractDelta = 0
            })
        );
        Require(errors.Count == 0 && completed.Count == Families && sourceEqual && dllEqual && outcomes);
    }

    private static async Task Atomics(List<Term> rows, string folder)
    {
        var fixture = new AtomicsP2a586Fixture();
        var errors = new List<Exception>();
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? producer = null;
        try
        {
            producer = fixture.Lease(AtomicsP2a586Names.ProducerKind);
            actor = new(fixture.Runtime);
            await Exercise(
                AtomicsFamily,
                fixture.Runtime,
                actor,
                fixture.Reference,
                () =>
 {
     fixture.ReleaseProducer();
     producer?.Dispose();
     producer = null;
 },
                fixture.Array,
                AtomicsP2a586Names.Retired,
                rows,
                folder,

                false,
                fixture.ReleaseInvocationForShutdown
            );
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            try
            {
                producer?.Dispose();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            await fixture.Drain(errors);
        }
        Finish(errors);
    }

    private static async Task Counters(List<Term> rows, string folder)
    {
        var fixture = new CountersCustody596Fixture();
        var errors = new List<Exception>();
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? producer = null;
        try
        {
            producer = fixture.Lease(CountersCustody596Names.ProducerKind);
            actor = new(fixture.Runtime);
            await Exercise(
                CountersFamily,
                fixture.Runtime,
                actor,
                fixture.Reference,
                () =>
 {
     fixture.ReleaseProducer();
     producer?.Dispose();
     producer = null;
 },
                fixture.Array,
                CountersCustody596Names.Retired,
                rows,
                folder,

                false,
                fixture.ReleaseInvocationForShutdown
            );
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            try
            {
                producer?.Dispose();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            await fixture.Drain(errors);
        }
        Finish(errors);
    }

    private static Task Regex(List<Term> rows, string folder) => RegexCustody597Fixture.Run(fixture => Exercise(
        RegexFamily,
        fixture.Runtime,
        fixture.Actor!,
        fixture.Reference,
        fixture.Release,
        fixture.Backing!,
        RegexCustody597Names.Retired,
        rows,
        folder,

        false
    ));

    private static Task Binary(List<Term> rows, string folder) => BinaryArena598Fixture.Run(
        new BitString([120]),
        fixture => Exercise(
            BinaryFamily,
            fixture.Runtime,
            fixture.Actor!,
            fixture.Reference,
            fixture.Release,
            fixture.BackingOwner,
            BinaryArena598Names.Retired,
            rows,
            folder,

            false
        )
    );

    private static async Task Ordinary(List<Term> rows, string folder)
    {
        var runtime = OwnedGuest506RuntimeFactory.Create();
        RuntimeNodeFoundationHostActor? actor = null;
        var errors = new List<Exception>();
        try
        {
            actor = new(runtime);
            await Exercise(
                OrdinaryFamily,
                runtime,
                actor,
                runtime.MakeReference(),
                () => { },
                null,
                string.Empty,
                rows,
                folder,
                false
            );
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        Finish(errors);
    }

    private static async Task Exercise(
        string family,
        ProcessRuntime runtime,
        RuntimeNodeFoundationHostActor actor,
        ReferenceTerm reference,
        Action releaseProducer,
        object? backing,
        string retiredName,
        List<Term> rows,
        string folder,
        bool nativeScope,
        Action? releaseInvocationForShutdown = null
    )
    {
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapDebugSharing425.Register(runtime, registry);
            PreparedBeamImage counters = CountersOriginal421Preparation.Prepare(runtime, registry);
            Require(registry.PublishPreparedBeam(counters).Status == BeamPublicationStatus.Published);
            CountersOriginal421Preparation.AssertPublished(registry, counters);
        });
        using var compiled = GeneratedModuleCompiler.Compile(Source, MonitorOriginal645Names.SourceFile);
        compiled.Definition.Register(runtime.Modules);
        Require(runtime.Modules.SourceOwner(MonitorOriginal645Names.Module, Observe, 1) is not null);
        try
        {
            foreach (string mode in new[] { Down, Cancel })
            {
                await actor.Invoke(context =>
                {
                    using NativeTermHostScope524? host = nativeScope ? new(runtime) : null;
                    context.Dictionary[Term.A(Stored)] = reference;

                    return true;
                });
                Term outcome = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    MonitorOriginal645Names.Module,
                    Observe,
                    Term.A(mode)
                ));
                Term row = Term.Tuple(Term.A(family), Term.A(mode), outcome);
                byte[] encoded = ExternalTermFormat.Encode(row,
                    new EtfEncodingOptions { MinorVersion = 2, FunctionOwner = runtime.InitializationPid });
                int index = Array.IndexOf(new[] { AtomicsFamily, CountersFamily, RegexFamily, BinaryFamily, OrdinaryFamily }, family) * Modes
                    + (mode == Down ? 0 : 1);
                File.WriteAllBytes(Path.Combine(folder, OutcomePrefix + index + EtfSuffix), encoded);
                File.WriteAllText(Path.Combine(folder, OutcomePrefix + index + JsonSuffix),
                    JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(row)));
                rows.Add(row);
            }
        }
        finally
        {
            releaseProducer();
            releaseInvocationForShutdown?.Invoke();
        }
    }

    private static string Sha(string file) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }

    private static void Finish(List<Exception> errors)
    {
        if (errors.Count == 1) ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count > 1) throw new AggregateException(Failure, errors);
    }
}
