using System.Security.Cryptography;
using System.Text.Json;
namespace Erlang.NifNativeBackend;

internal static class GcBinary818Driver
{
    internal static async Task Run(string[] args)
    {
        Require(args.Length == 4);
        Verify(args[0], args[1]);
        using var admission = JsonDocument.Parse(File.ReadAllBytes(args[0]));
        JsonElement plan = admission.RootElement;
        Require(Text(plan, NifDirty521Names.FieldStatus) == GcBinary818Names.Admission
            && Text(plan, NifDirty521Names.FieldPin) == NifDirty521Names.Pin);

        void CheckInputs()
        {
            Verify(args[0], args[1]);
            foreach (var row in plan.GetProperty(NifDirty521Names.FieldInputs).EnumerateArray())
                Verify(Text(row, NifDirty521Names.FieldPath), Text(row, NifDirty521Names.FieldSha256));
        }
        CheckInputs();
        Verify(Text(plan, GcBinary818Names.Beam), Text(plan, GcBinary818Names.BeamHash));
        Verify(Text(plan, GcBinary818Names.Coverage), Text(plan, GcBinary818Names.CoverageHash));
        bool owned = args[2] switch
        {
            NifDirty521Names.ProfileOrdinary => false,
            NifDirty521Names.ProfileOwned => true,
            _ => throw new InvalidDataException(GcBinary818Names.Failure)
        };
        string output = Path.GetFullPath(args[3]);
        Require(!Directory.Exists(output));
        Directory.CreateDirectory(output);
        var errors = new List<string>();
        var runtime = NativeOwned516Profile.Create(owned);
        bool equal = false, closed = false;
        int beforeExports = runtime.Modules.Exports.Count;
        try
        {
            Require(runtime.OwnedTermsPilot506 == owned);
            PreparedBeamImage image = runtime.Modules.PrepareBeam(
                File.ReadAllBytes(Text(plan, GcBinary818Names.Beam)),
                new BeamDependencySelection([ErlangModuleNames.Module, GcBinary818Names.Module])
            );
            await runtime.InitializeModulesAsync(registry =>
            {
                Require(image.Name == GcBinary818Names.Module && image.NativePrimitiveExports.Count == 0
                    && image.OriginalBeamSha256 == Text(plan, GcBinary818Names.BeamHash));
                Require(registry.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
                Require(ReferenceEquals(registry.SourceOwner(GcBinary818Names.Module, GcBinary818Names.Capture, 0), image));
                File.WriteAllText(
                    Path.Combine(output, GcBinary818Names.Provenance),
                    JsonSerializer.Serialize(new
                    {
                        image.OriginalBeamSha256,
                        CurrentRuntime = typeof(ProcessRuntime).Assembly.Location,
                        CurrentRuntimeSha256 = Hash(typeof(ProcessRuntime).Assembly.Location),
                        RuntimeMvid = typeof(ProcessRuntime).Assembly.ManifestModule.ModuleVersionId,
                        FixtureExports = registry.Exports.Count(export => export.Module == GcBinary818Names.Module),
                        NativeFixtureExports = image.NativePrimitiveExports.Count,
                        LibraryApiRegistryDelta = 0,
                        ActualMfaWorkerStartedByOriginalCompiledCapture = true
                    })
                );
            });
            await NifNativeHarness.Context(
                runtime,
                async context =>
            {
                // BOTH engines execute this SAME original capture/0. It starts
                // real spawn_monitor/3 MFA entry, so parent host/erl_eval frames
                // cannot contaminate the caught worker error stacks.
                Term rows = await runtime.Modules.Call(context, GcBinary818Names.Module, GcBinary818Names.Capture);
                byte[] raw = await Encode(context, rows, false);
                byte[] deterministic = await Encode(context, rows, true);
                File.WriteAllBytes(Path.Combine(output, NifDirty521Names.FileObservedEtf), raw);
                File.WriteAllBytes(Path.Combine(output, NifDirty521Names.FileObservedDeterministicEtf), deterministic);
                equal = raw.AsSpan().SequenceEqual(File.ReadAllBytes(Text(plan, NifDirty521Names.FieldOriginalRawPath)))
                    && deterministic.AsSpan().SequenceEqual(File.ReadAllBytes(Text(plan, NifDirty521Names.FieldOriginalDeterministicPath)));
                Require(equal);
            }
            );
            Require(runtime.Modules.Exports.Count(export => export.Module != GcBinary818Names.Module) == beforeExports);
        }
        catch (Exception error)
        {
            errors.Add(error.ToString());
            if (error is ErlangException guest)
                File.WriteAllBytes(
                    Path.Combine(output, NifDirty521Names.FileGuestFailure),
                    ExternalTermFormat.Encode(Term.Tuple(Term.A(guest.ExceptionClass), guest.Reason, guest.StackTraceTerm))
                );
        }
        finally
        {
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(NifNativeHarnessNames.Deadline);
                closed = true;
            }
            catch (Exception error)
            {
                errors.Add(error.ToString());
            }
            try
            {
                CheckInputs();
            }
            catch (Exception error)
            {
                errors.Add(error.ToString());
            }
        }
        File.WriteAllText(
            Path.Combine(output, NifDirty521Names.FileCompletionJson),
            JsonSerializer.Serialize(
                new
                {
                    Passed = errors.Count == 0 && equal && closed,
                    WholeRawAndDeterministicEqual = equal,
                    RuntimeClosed = closed,
                    Profile = args[2],
                    AdmissionSha256 = args[1],
                    SourceStacksProjected = false,
                    FramesDropped = false,
                    NormalizationPerformed = false,
                    LibraryApiRegistryDelta = 0,
                    FullCompatibilityClaimed = false,
                    Errors = errors
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        if (errors.Count != 0) throw new AggregateException(errors.Select(value => new InvalidOperationException(value)));
    }

    private static async Task<byte[]> Encode(ProcessContext context, Term rows, bool deterministic)
    {
        Term minor = Term.Tuple(Term.A(NifDirty521Names.MinorVersionOption), Term.I(NifDirty521Names.SerializationMinorVersion));
        Term options = deterministic ? Term.List(Term.A(NifDirty521Names.DeterministicOption), minor) : Term.List(minor);

        return ((BitString)await context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            NifDirty521Names.EncodeFunction,
            rows,
            options
        )).ToArray();
    }

    private static string Text(JsonElement root, string name) => root.GetProperty(name).GetString()!;

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Verify(string path, string expected) => Require(Path.IsPathFullyQualified(path) && Hash(path) == expected.ToUpperInvariant());

    private static void Require(bool value)
    {
        if (!value) throw new InvalidDataException(GcBinary818Names.Failure);
    }
}
