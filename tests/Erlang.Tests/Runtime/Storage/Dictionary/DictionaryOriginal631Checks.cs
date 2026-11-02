using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang.Differential;
using static Erlang.Tests.DictionaryOriginal631Names;

namespace Erlang.Tests;

internal static class DictionaryOriginal631Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(Output, System.Guid.NewGuid().ToString(DictionaryOriginal631Names.GuidFormat)));
        Directory.CreateDirectory(output);
        var failures = new List<Exception>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        string runtimeSha = string.Empty, hostSha = string.Empty;
        string? actualSha = null;
        bool actorDone = false, disposed = false, sourceEqual = false, captured = false;
        var images = new List<object>();
        try
        {
            byte[] originalSource = Convert.FromBase64String(SourceBase64);
            if (Convert.ToHexString(SHA256.HashData(originalSource)) != SourceSha256)
                throw new InvalidOperationException(DictionaryOriginal631Names.SourceMismatch);
            System.IO.File.WriteAllBytes(Path.Combine(output, DictionaryOriginal631Names.File), originalSource);
            byte[] captureSource = Convert.FromBase64String(CaptureBase64);
            System.IO.File.WriteAllBytes(Path.Combine(output, CaptureFile), captureSource);
            runtimeSha = Convert.ToHexString(SHA256.HashData(System.IO.File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location)));
            hostSha = Convert.ToHexString(SHA256.HashData(System.IO.File.ReadAllBytes(typeof(DictionaryOriginal631Checks).Assembly.Location)));
            runtime = OwnedGuest506RuntimeFactory.Create();
            ProcessRuntime actualRuntime = runtime;
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                async (registry, scope) =>
            {
                await runtime.InitializeModulesAsync(_ =>
                {
                    RuntimeBootstrapAtomics.Register(actualRuntime, registry);
                });
                var dependencies = new BeamDependencySelection([
                    AtomicsHost413Names.Runtime, AtomicsHost413Names.Internal,
                                AtomicsHost413Names.Atomics, AtomicsHost413Names.Lists, AtomicsHost413Names.Maps
                ]);
                PreparedBeamImage[] originals = new[] {
                                AtomicsHost413Names.Atomics, AtomicsHost413Names.Lists, AtomicsHost413Names.Maps
                            }.Select(module => scope.Prepare(() => registry.PrepareBeam(
                                AtomicsHost413Access.Resource(module + AtomicsHost413Names.Beam), dependencies
                            ))).ToArray();

                return _ =>
                {
                    if (registry.PublishPreparedBeam(originals).Status != BeamPublicationStatus.Published)
                        throw new InvalidOperationException(DictionaryOriginal631Names.PublicationFailure);
                    foreach (PreparedBeamImage image in originals)
                    {
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            if (!ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image))
                                throw new InvalidOperationException(DictionaryOriginal631Names.OwnerMismatch);
                        images.Add(new
                        {
                            Module = image.Name,
                            ImageSha256 = Convert.ToHexString(SHA256.HashData(
                        AtomicsHost413Access.Resource(image.Name + AtomicsHost413Names.Beam)))
                        });
                    }
                };
            }
            );
            using var observer = GeneratedModuleCompiler.Compile(Encoding.UTF8.GetString(originalSource), DictionaryOriginal631Names.File);
            using var capture = GeneratedModuleCompiler.Compile(Encoding.UTF8.GetString(captureSource), CaptureFile);
            observer.Definition.Register(runtime.Modules);
            capture.Definition.Register(runtime.Modules);
            actor = new RuntimeNodeFoundationHostActor(runtime);
            Term outcome = await actor.InvokeAsync(async context => await actualRuntime.Modules.Call(
                context, CaptureModule, Entry
            ));
            byte[] bytes = ExternalTermFormat.Encode(outcome, new EtfEncodingOptions
            {
                MinorVersion = 2,
                FunctionOwner = runtime.InitializationPid
            });
            System.IO.File.WriteAllBytes(Path.Combine(output, DictionaryOriginal631Names.OutcomeFile), bytes);
            actualSha = Convert.ToHexString(SHA256.HashData(bytes));
            captured = true;
            sourceEqual = System.IO.File.ReadAllBytes(Path.Combine(output, DictionaryOriginal631Names.File)).AsSpan().SequenceEqual(originalSource);
            if (!sourceEqual)
                throw new InvalidOperationException(DictionaryOriginal631Names.SavedMismatch);
            if (outcome is not TupleTerm tuple || tuple.Items.Count != 2 || !tuple.Items[0].Equals(Term.A(DictionaryOriginal631Names.OutcomeOk)))
                throw new InvalidOperationException(DictionaryOriginal631Names.OutcomeFailure);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                if (actor is not null)
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                    actorDone = true;
                }
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                if (runtime is not null)
                {
                    await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
                    disposed = true;
                }
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            System.IO.File.WriteAllText(
                Path.Combine(output, DictionaryOriginal631Names.ReceiptFile),
                JsonSerializer.Serialize(
                    new
                    {
                        RuntimeSha256 = runtimeSha,
                        HostSha256 = hostSha,
                        ObserverSourceSha256 = SourceSha256,
                        CaptureSourceSha256 = Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(CaptureBase64))),
                        OriginalDependencyImages = images,
                        ActualSha256 = actualSha,
                        WholeOutcomeCaptured = captured,
                        ObserverSourceBytesEqual = sourceEqual,
                        ActorDone = actorDone,
                        RuntimeDisposeCompleted = disposed,
                        Passed = failures.Count == 0 && captured && sourceEqual && actorDone && disposed,
                        RawEtfRewritten = false,
                        FrameProjection = false,
                        GoldenCompared = false,
                        PhysicalRetirementInstrumented = false,
                        Errors = failures.Select(error => error.ToString()).ToArray()
                    },
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(failures);
    }
}
