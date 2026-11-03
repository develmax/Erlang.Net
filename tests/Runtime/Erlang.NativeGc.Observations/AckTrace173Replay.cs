using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class AckTrace173Replay
{
    internal static async Task Run(
        string directory,
        string outputDirectory,
        string profile,
        string inputSeal
    )
    {
        bool owned = profile switch
        {
            AckOriginal425ReplayNames.Ordinary => false,
            AckOriginal425ReplayNames.Private => true,
            _ => throw new ArgumentException(nameof(profile))
        };
        string output = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(output)) throw new IOException(AckOriginal425ReplayNames.OutputExists);
        Directory.CreateDirectory(output);
        string input = Path.GetFullPath(directory);
        byte[] manifestBytes = File.ReadAllBytes(Path.Combine(input, AckTrace173ReplayNames.Inputs));
        Require(Convert.ToHexString(SHA256.HashData(manifestBytes)) == inputSeal.ToUpperInvariant());
        using var manifest = JsonDocument.Parse(manifestBytes);
        var row = manifest.RootElement.GetProperty(AckOriginal425ReplayNames.Files).EnumerateArray().Single();
        Require(row.GetProperty(AckOriginal425ReplayNames.Name).GetString() == AckTrace173ReplayNames.Worker);
        byte[] bytes = File.ReadAllBytes(Path.Combine(input, AckTrace173ReplayNames.Worker));
        Require(bytes.LongLength == row.GetProperty(AckOriginal425ReplayNames.Bytes).GetInt64()
            && Convert.ToHexString(SHA256.HashData(bytes)) == row.GetProperty(AckOriginal425ReplayNames.Sha).GetString());
        var errors = new List<string>();
        bool matched = false;
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        PreparedBeamImage? image = null;
        try
        {
            runtime = OwnedGuest506RuntimeFactory.Create(owned);
            ProcessRuntime owner = runtime;
            image = owner.Modules.PrepareBeam(bytes, new BeamDependencySelection(AckTrace173ReplayNames.Dependencies));
            PreparedBeamImage prepared = image;
            await owner.InitializeModulesAsync(registry =>
            {
                Require(registry.PublishPreparedBeam([prepared]).Status == BeamPublicationStatus.Published);
                Require(prepared.NativePrimitiveExports.Count == AckTrace173ReplayNames.NativeExports);
                foreach (var export in registry.Exports.Where(export => export.Module == prepared.Name))
                    Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), prepared));
            });
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;
            Term observed = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                AckTrace173ReplayNames.Module, AckTrace173ReplayNames.Observe));
            Require(observed is TupleTerm { Items.Count: AckTrace173ReplayNames.WholeArity });
            var whole = (TupleTerm)observed;
            Term[] pairs = Cons.Items(whole.Items[AckTrace173ReplayNames.RawIndex]).ToArray();
            Require(pairs.Length == AckTrace173ReplayNames.PairCount);
            foreach (Term value in pairs)
            {
                Require(value is TupleTerm { Items.Count: AckTrace173ReplayNames.PairArity });
                var pair = (TupleTerm)value;
                // Complete actual/expected tuple equality in this actual VM,
                // independently rechecked after the witness's own assertion.
                Require(pair.Items[AckTrace173ReplayNames.ActualIndex].Equals(pair.Items[AckTrace173ReplayNames.ExpectedIndex]));
            }
            matched = true;
            foreach (var route in new[]
            {
                (AckTrace173ReplayNames.Values, whole.Items[AckTrace173ReplayNames.ValuesIndex]),
                (AckTrace173ReplayNames.Raw, whole.Items[AckTrace173ReplayNames.RawIndex]),
                (AckTrace173ReplayNames.Whole, observed)
            })
            {
                BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    AckOriginal425ReplayNames.Encode,
                    route.Item2,
                    Term.List(Term.Tuple(Term.A(AckOriginal425ReplayNames.MinorVersion), Term.I(AckOriginal425ReplayNames.MinorVersionValue)))
                ));
                File.WriteAllBytes(Path.Combine(output, route.Item1), wire.ToArray());
            }
        }
        catch (Exception error)
        {
            errors.Add(error.ToString());
        }
        finally
        {
            try
            {
                if (actor is not null) await actor.DisposeAsync();
            }
            catch (Exception error)
            {
                errors.Add(AckOriginal425ReplayNames.ActorCleanup + Environment.NewLine + error);
            }
            try
            {
                if (runtime is not null) await runtime.DisposeAsync();
            }
            catch (Exception error)
            {
                errors.Add(AckOriginal425ReplayNames.RuntimeCleanup + Environment.NewLine + error);
            }
            try
            {
                if (image is not null) await image.DisposeAsync();
            }
            catch (Exception error)
            {
                errors.Add(AckOriginal425ReplayNames.WorkerCleanup + Environment.NewLine + error);
            }
        }
        File.WriteAllText(
            Path.Combine(output, AckOriginal425ReplayNames.Receipt),
            JsonSerializer.Serialize(
                new
                {
                    Passed = errors.Count == 0 && matched,
                    Profile = profile,
                    ManagedInputSha256 = inputSeal,
                    Errors = errors,
                    CompleteOwnExpectedTracePairsEqual = matched,
                    WholeRawTraceAcrossVmsEqualClaimed = false,
                    RawActualAndExpectedPairsRetained = true,
                    PosthocIdentifierNormalization = false
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        Require(errors.Count == 0 && matched);
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(AckTrace173ReplayNames.Failure);
    }
}
