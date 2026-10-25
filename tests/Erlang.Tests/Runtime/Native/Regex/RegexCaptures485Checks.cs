using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using static Erlang.Tests.RegexCaptures485Names;

namespace Erlang.Tests;

internal static class RegexCaptures485Checks
{
    private static byte[] Read(string name)
    {
        byte[] Raw(string file)
        {
            using Stream input = typeof(RegexCaptures485Checks).Assembly.GetManifestResourceStream(Prefix + file)
                ?? throw new InvalidOperationException(Failure);
            using var bytes = new MemoryStream();
            input.CopyTo(bytes);

            return bytes.ToArray();
        }

        using JsonDocument manifest = JsonDocument.Parse(Raw(Manifest));
        byte[] value = Raw(name);
        if (Convert.ToHexString(SHA256.HashData(value)) != manifest.RootElement.GetProperty(Files).GetProperty(name).GetString())
            throw new InvalidOperationException(Failure);

        return value;
    }

    internal static async Task Whole()
    {
        Directory.CreateDirectory(Output);
        var runtime = new ProcessRuntime();
        RuntimeRegexEngine411? engine = null;
        var actual = new List<Term>();
        var errors = new List<object>();
        try
        {
            engine = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules, RegexNative411Access.Admission());
            await runtime.InitializeModulesAsync(registry =>
            {
                Erlang.Otp.OtpModules.Register(registry);
                var dependencies = new BeamDependencySelection(RegexThreeOriginal413Names.OriginalModules.Concat(Modules).Append(RegexCaptures485Names.Erlang));
                PreparedBeamImage[] images = Modules.Select(name => registry.PrepareBeam(Read(name + Beam), dependencies)).ToArray();
                if (registry.PublishPreparedBeam(images).Status != BeamPublicationStatus.Published)
                    throw new InvalidOperationException(Failure);
                foreach (PreparedBeamImage image in images)
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        if (!ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image))
                            throw new InvalidOperationException(Failure);
            });
            for (int index = 1; index <= Count; index++)
            {
                int current = index;
                var done = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
                var actor = runtime.Spawn(async context =>
                {
                    try
                    {
                        var result = (TupleTerm)await runtime.Modules.Call(
                            context,
                            Observer,
                            Worker,
                            Term.Tuple(context.Self, Term.I(current))
                        );
                        done.TrySetResult(result.Items[2]);
                    }
                    catch (Exception error)
                    {
                        done.TrySetException(error);
                    }

                    return Nil.Value;
                });
                try
                {
                    actual.Add(await done.Task.WaitAsync(TimeSpan.FromSeconds(TimeoutSeconds)));
                }
                catch (Exception error)
                {
                    errors.Add(new { Index = current, Error = error.ToString() });
                    actual.Add(Nil.Value);
                }
                finally
                {
                    await actor.Completion.WaitAsync(TimeSpan.FromSeconds(TimeoutSeconds));
                }
            }
        }
        finally
        {
            await runtime.DisposeAsync();
        }

        byte[] encoded = ExternalTermFormat.Encode(Term.List(actual.ToArray()));
        File.WriteAllBytes(Path.Combine(Output, Actual), encoded);
        byte[] expected = Read(Expected);
        bool equal = encoded.AsSpan().SequenceEqual(expected);
        bool cleaned = engine is not null && engine.Report.Closed && engine.Report.LibraryFreed
            && engine.Report.Active == 0 && engine.Report.RetainedCodes == 0
            && engine.Report.CodesCreated == engine.Report.CodesFreed
            && engine.Report.MatchDataCreated == engine.Report.MatchDataFreed;
        File.WriteAllText(
            Path.Combine(Output, Receipt),
            JsonSerializer.Serialize(
                new
                {
                    Count = actual.Count,
                    WholeRawEqual = equal,
                    Errors = errors,
                    Cleanup = engine?.Report,
                    CleanupPassed = cleaned
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        if (!equal || !cleaned || errors.Count != 0 || actual.Count != Count)
            throw new InvalidOperationException(Failure);
    }
}
