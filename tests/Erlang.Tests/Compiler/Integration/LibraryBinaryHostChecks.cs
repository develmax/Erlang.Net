using System.Security.Cryptography;
using Erlang;
using static Erlang.Tests.LibraryBinaryNames;

namespace Erlang.Tests;

internal static class LibraryBinaryHostChecks
{
    internal static async Task Run()
    {
        string output = Path.Combine(OutputBase, Guid.NewGuid().ToString(GuidFormat));
        Directory.CreateDirectory(output);
        var failures = new List<Exception>();
        object? owner = null;
        object[] backings = [];
        bool disposed = false, cleanup = false, originalImageOwned = false;
        int nativePrimitives = 0;
        var streams = new List<object>();
        var runtime = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([
            RuntimeModuleSelection.Beam(Module, Read(Module + BeamExtension), [Lists]),
            RuntimeModuleSelection.Beam(Lists, Read(Lists + BeamExtension))
        ]));
        string runtimeSha = string.Empty;
        try
        {
            runtimeSha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location)));
            foreach (RuntimeLibraryModuleSnapshot selected in runtime.LibrarySelection.Modules)
            {
                Require(selected.Backend == RuntimeModuleBackend.Beam && selected.InputSha256 == Digests[selected.Module + BeamExtension]);
                Require(ReferenceEquals(selected.Image, runtime.Modules.CurrentCodeImage(selected.Module)) && !runtime.Modules.HasOldCode(selected.Module));
                foreach (BeamFunctionSignature export in selected.Exports)
                    Require(runtime.Modules.SourceOwner(selected.Module, export.Name, export.Arity) is PreparedBeamImage image && image.Name == selected.Module);
            }
            nativePrimitives = runtime.LibrarySelection.Modules.Single(module => module.Module == Module).NativePrimitives.Count;
            Require(nativePrimitives == NativePrimitiveCount);
            originalImageOwned = true;
            PreparedBeamImage observer = runtime.Modules.PrepareBeam(Read(Observer + BeamExtension), new BeamDependencySelection([Module, Lists, ErlangModuleNames.Module]));
            Require(runtime.Modules.PublishPreparedBeam(observer).Status == BeamPublicationStatus.Published);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(async context =>
            {
                foreach ((string entry, string golden, string actualFile, int count) in new[]
                {
                    (RunEntry, ValuesFile, ActualValuesFile, ValuesCount),
                    (Stacks, StacksFile, ActualStacksFile, StacksCount)
                })
                    try
                    {
                        Term actual = await runtime.Modules.Call(context, Observer, entry);
                        byte[] original = Read(golden);
                        byte[] encoded = ExternalTermFormat.Encode(actual, new EtfEncodingOptions { MinorVersion = 2, FunctionOwner = runtime.InitializationPid });
                        File.WriteAllBytes(Path.Combine(output, actualFile), encoded);
                        bool rawEqual = encoded.AsSpan().SequenceEqual(original);
                        bool typedEqual = actual.Equals(runtime.ImportExternalTerm(original).Term);
                        streams.Add(new { Entry = entry, CompleteOutcomes = count, ActualFile = actualFile, ActualSha256 = Convert.ToHexString(SHA256.HashData(encoded)), OriginalSha256 = Digests[golden], WholeRawEqual = rawEqual, WholeTypedEqual = typedEqual });
                        Require(rawEqual && typedEqual);
                    }
                    catch (Exception error)
                    {
                        failures.Add(error);
                    }

                return true;
            });
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            // Real owner/backing inspection only; no estimate or fake counters.
            try
            {
                owner = typeof(ProcessRuntime).GetField(BinaryOwner, Members)!.GetValue(runtime);
                Require(owner is not null);
                lock (Field(owner!, Gate))
                    backings = ((System.Collections.IEnumerable)Field(owner!, Live)).Cast<object>().ToArray();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                await runtime.DisposeAsync();
                disposed = true;
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                bool ownerClosed = false, ownerEmpty = false, buildsEmpty = false, noFailures = false;
                if (owner is not null)
                    lock (Field(owner, Gate))
                    {
                        ownerClosed = (bool)Field(owner, Closed);
                        ownerEmpty = !((System.Collections.IEnumerable)Field(owner, Live)).Cast<object>().Any();
                        buildsEmpty = (int)Field(owner, ActiveBuilds) == 0;
                        noFailures = owner.GetType().GetField(Failures, Members)!.GetValue(owner) is null;
                    }
                bool allPhysicalFreed = backings.All(backing => (bool)backing.GetType().GetProperty(PhysicalFreed, Members)!.GetValue(backing)!);
                cleanup = disposed && ownerClosed && ownerEmpty && buildsEmpty && noFailures && allPhysicalFreed;
                File.WriteAllText(
                    Path.Combine(output, ComparisonFile),
                    System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            RuntimeSha256 = runtimeSha,
                            SourceImageSha256 = Digests[Module + BeamExtension],
                            ObserverImageSha256 = Digests[Observer + BeamExtension],
                            OriginalWholeImageOwned = originalImageOwned,
                            NativePrimitives = nativePrimitives,
                            ExpectedCompleteOutcomes = ValuesCount + StacksCount,
                            Streams = streams,
                            Errors = failures.Select(error => error.ToString()).ToArray(),
                            Cleanup = new { DisposeCompleted = disposed, OwnerClosed = ownerClosed, OwnerEmpty = ownerEmpty, ActiveBuildsZero = buildsEmpty, NoRetirementFailures = noFailures, ObservedLiveBackingsBeforeDispose = backings.Length, AllObservedPhysicalFreed = allPhysicalFreed },
                            CleanupPassed = cleanup,
                            FrameFieldsProjected = false,
                            ApiCompatibilityPromotion = false
                        },
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                    )
                );
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count == 1)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count != 0)
            throw new AggregateException(failures);
        Require(cleanup && streams.Count == 2);
    }

    private static object Field(object owner, string name)
        => owner.GetType().GetField(name, Members)!.GetValue(owner)!;

    private static byte[] Read(string name)
    {
        using Stream stream = typeof(LibraryBinaryHostChecks).Assembly.GetManifestResourceStream(Prefix + name)!;
        using var output = new MemoryStream();
        stream.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == Digests[name]);

        return bytes;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
