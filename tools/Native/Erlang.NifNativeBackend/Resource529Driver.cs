using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.NifNativeBackend;

internal static class Resource529Driver
{
    internal static async Task Run(
        string[] arguments,
        Func<bool, Task<ProcessRuntime>>? runtimeFactory = null
    )
    {
        Require(arguments.Length == 5 && OperatingSystem.IsWindows());
        Verify(arguments[0], arguments[1]);
        using var admission = JsonDocument.Parse(File.ReadAllBytes(arguments[0]));
        JsonElement row = admission.RootElement;
        Require(row.GetProperty("Status").GetString() == Resource529Names.Status);
        Require(row.GetProperty("Pin").GetString() == Resource529Names.Pin);
        foreach (var input in row.GetProperty("Inputs").EnumerateArray())
            Verify(input.GetProperty("Path").GetString()!, input.GetProperty("Sha256").GetString()!);
        string job = row.GetProperty("Job").GetString()!;
        Require(row.GetProperty("ProtocolSchemaVersion").GetInt32() == 2);
        Require(job is Resource529Names.Family or Resource529Names.FreshLoad or Resource529Names.Upgrade);
        string route = arguments[2];
        Require(route is Resource529Names.Ordinary or Resource529Names.Reviewed);
        bool owned = arguments[3] switch
        {
            Resource529Names.Ordinary => false,
            Resource529Names.Private => true,
            _ => throw new InvalidDataException(Resource529Names.Invalid)
        };
        string output = Path.GetFullPath(arguments[4]);
        Require(!Directory.Exists(output));
        Directory.CreateDirectory(output);
        var libraries = row.GetProperty("Libraries").EnumerateArray().ToArray();
        foreach (var library in libraries)
        {
            Verify(library.GetProperty("Path").GetString()!, library.GetProperty("Sha256").GetString()!);
            Verify(library.GetProperty("BuildReceiptPath").GetString()!, library.GetProperty("BuildReceiptSha256").GetString()!);
            Require(MapFamily504NativeModuleHandle.Find(library.GetProperty("Path").GetString()!) == 0);
        }
        Verify(row.GetProperty("OriginalInventoryPath").GetString()!, row.GetProperty("OriginalInventorySha256").GetString()!);
        using var inventory = JsonDocument.Parse(File.ReadAllBytes(row.GetProperty("OriginalInventoryPath").GetString()!));
        foreach (var input in inventory.RootElement.EnumerateArray())
            Verify(input.GetProperty("Path").GetString()!, input.GetProperty("Sha256").GetString()!);
        Require(new[] { "OriginalRawPath", "OriginalDeterministicPath" }.All(field =>
            inventory.RootElement.EnumerateArray().Any(input => input.GetProperty("Path").GetString() == row.GetProperty(field).GetString())));
        int envs = NifNativeCallEnvironment.ActiveCount;
        long buffers = NifNativeBinaryStorage.LiveBufferCount;
        int binaries = NifNativeBinaryRegistry.NativeOwnedCount;
        ProcessRuntime? runtime = null;
        var images = new List<PreparedBeamImage>();
        Exception? failure = null;
        byte[]? raw = null, deterministic = null;
        int policyCalls = 0;
        bool retired = false;
        try
        {
            runtime = runtimeFactory is null ? NativeOwned516Profile.Create(owned) : await runtimeFactory(owned);
            Require(runtime.OwnedTermsPilot506 == owned);
            await runtime.InitializeModulesAsync(registry =>
            {
                string[] dependencies = row.GetProperty("Dependencies").EnumerateArray().Select(value => value.GetString()!).ToArray();
                var publication = new List<PreparedBeamImage>();
                var errors = new List<Exception>();
                foreach (var image in row.GetProperty("Images").EnumerateArray())
                {
                    try
                    {
                        string path = image.GetProperty("Path").GetString()!;
                        string hash = image.GetProperty("Sha256").GetString()!;
                        string module = image.GetProperty("Module").GetString()!;
                        Verify(path, hash);
                        if (registry.SourceOwner(module, Resource529Names.ModuleInfo, 0) is PreparedBeamImage existing)
                        {
                            Require(existing.OriginalBeamSha256 == hash);
                            images.Add(existing);
                            continue;
                        }
                        PreparedBeamImage prepared = registry.PrepareBeam(File.ReadAllBytes(path), new BeamDependencySelection(dependencies));
                        Require(prepared.Name == module && prepared.OriginalBeamSha256 == hash);
                        images.Add(prepared);
                        publication.Add(prepared);
                    }
                    catch (Exception error)
                    {
                        errors.Add(error);
                    }
                }
                if (errors.Count != 0) throw new AggregateException(errors);
                if (publication.Count != 0)
                    Require(registry.PublishPreparedBeam(publication.ToArray()).Status == BeamPublicationStatus.Published);
            });
            if (route == Resource529Names.Reviewed)
                runtime.Modules.InstallReviewedNativeExecutionPolicy(path =>
                {
                    Interlocked.Increment(ref policyCalls);
                    var library = libraries.Single(item => string.Equals(Path.GetFullPath(path), item.GetProperty("Path").GetString(), StringComparison.OrdinalIgnoreCase));

                    return new NifNativeReviewedBasicAbi(path, library.GetProperty("Sha256").GetString()!, library.GetProperty("BuildReceiptSha256").GetString()!);
                });
            ProcessRuntime current = runtime;
            var owners = images.Select(image =>
            {
                var exports = current.Modules.Exports.Where(export => export.Module == image.Name).ToArray();
                Require(exports.Length != 0 && exports.All(export => ReferenceEquals(current.Modules.SourceOwner(export.Module, export.Function, export.Arity), image)));

                return new { image.Name, image.OriginalBeamSha256, Exports = exports.Select(export => new { export.Function, export.Arity }).ToArray() };
            }).ToArray();
            await File.WriteAllTextAsync(Path.Combine(output, Resource529Names.Owners), JsonSerializer.Serialize(owners));
            await NifNativeHarness.Context(
                current,
                async context =>
            {
                string first = libraries[0].GetProperty("Path").GetString()!;
                string second = libraries[^1].GetProperty("Path").GetString()!;
                string stub = job == Resource529Names.Family ? Resource529Names.FamilyModule : Resource529Names.HandoffModule;
                var admitted = row.GetProperty("Images").EnumerateArray().Single(image => image.GetProperty("Module").GetString() == stub);
                Term fresh = new BitString(File.ReadAllBytes(admitted.GetProperty("Path").GetString()!));
                string marker = Path.Combine(output, Resource529Names.Marker);
                Term firstPath = Term.String(first[..^Resource529Names.Dll.Length]);
                Term secondPath = Term.String(second[..^Resource529Names.Dll.Length]);
                Term observed = job == Resource529Names.Family
                    ? await current.Modules.Call(
                        context,
                        Resource529Names.Observer,
                        Resource529Names.Run,
                        Term.A(job),
                        firstPath,
                        secondPath,
                        fresh,
                        Term.String(marker)
                    )
                    : await current.Modules.Call(
                        context,
                        Resource529Names.TakeoverObserver,
                        Resource529Names.Run,
                        Term.A(job),
                        firstPath,
                        fresh,
                        secondPath,
                        Term.String(marker + Resource529Names.FirstMarkerSuffix),
                        Term.String(marker + Resource529Names.SecondMarkerSuffix)
                    );
                raw = await Encode(context, observed, false);
                deterministic = await Encode(context, observed, true);
                await File.WriteAllBytesAsync(Path.Combine(output, Resource529Names.Raw), raw);
                await File.WriteAllBytesAsync(Path.Combine(output, Resource529Names.Deterministic), deterministic);
                if (observed is TupleTerm { Items: [Atom { Name: Resource529Names.Raised }, _, _, _] })
                    throw new InvalidOperationException(Resource529Names.Invalid);
            }
            );
        }
        catch (Exception error)
        {
            failure = Combine(failure, error);
            try
            {
                await File.WriteAllTextAsync(Path.Combine(output, Resource529Names.Failure), error.ToString());
                if (error is ErlangException guest)
                    await File.WriteAllBytesAsync(
                        Path.Combine(output, Resource529Names.GuestFailure),
                        ExternalTermFormat.Encode(Term.Tuple(Term.A(guest.ExceptionClass), guest.Reason, guest.StackTraceTerm))
                    );
            }
            catch (Exception diagnostic)
            {
                failure = Combine(failure, diagnostic);
            }
        }
        finally
        {
            var retirement = new List<object>();
            foreach (PreparedBeamImage image in images.Where(image => image.Name is Resource529Names.FamilyModule or Resource529Names.HandoffModule))
            {
                try
                {
                    await image.ExecutionIdentity.Image.NifBindings.RetireAsync().WaitAsync(NifNativeHarnessNames.Deadline);
                    retirement.Add(new { image.Name, OriginalImage = image.OriginalBeamSha256, PhysicalRetirementCompleted = true });
                }
                catch (Exception error)
                {
                    retirement.Add(new { image.Name, OriginalImage = image.OriginalBeamSha256, PhysicalRetirementCompleted = false });
                    failure = Combine(failure, error);
                }
            }
            try
            {
                await File.WriteAllTextAsync(Path.Combine(output, Resource529Names.Retirement), JsonSerializer.Serialize(retirement));
            }
            catch (Exception error)
            {
                failure = Combine(failure, error);
            }
            try
            {
                if (runtime is not null) await runtime.DisposeAsync();
                retired = runtime is not null;
            }
            catch (Exception error)
            {
                failure = Combine(failure, error);
            }
        }
        bool clean = retired && envs == NifNativeCallEnvironment.ActiveCount && buffers == NifNativeBinaryStorage.LiveBufferCount
            && binaries == NifNativeBinaryRegistry.NativeOwnedCount
            && libraries.All(library => MapFamily504NativeModuleHandle.Find(library.GetProperty("Path").GetString()!) == 0);
        if (!clean) failure = Combine(failure, new InvalidOperationException(Resource529Names.Invalid));
        bool sameRaw = raw is not null && raw.AsSpan().SequenceEqual(File.ReadAllBytes(row.GetProperty("OriginalRawPath").GetString()!));
        bool sameDeterministic = deterministic is not null && deterministic.AsSpan().SequenceEqual(File.ReadAllBytes(row.GetProperty("OriginalDeterministicPath").GetString()!));
        if (!sameRaw || !sameDeterministic || policyCalls != (route == Resource529Names.Reviewed ? libraries.Length : 0))
            failure = Combine(failure, new InvalidOperationException(Resource529Names.Invalid));
        foreach (var library in libraries)
        {
            try
            {
                Verify(library.GetProperty("Path").GetString()!, library.GetProperty("Sha256").GetString()!);
            }
            catch (Exception error)
            {
                failure = Combine(failure, error);
            }
        }
        await File.WriteAllTextAsync(
            Path.Combine(output, Resource529Names.Completion),
            JsonSerializer.Serialize(
                new
                {
                    Captured = raw is not null && deterministic is not null,
                    RawWholeEqual = sameRaw,
                    DeterministicWholeEqual = sameDeterministic,
                    PhysicalCleanupPassed = clean,
                    AdmissionSha256 = arguments[1],
                    Route = route,
                    OwnedProfile = owned,
                    Job = job,
                    PlanSha256 = row.GetProperty("PlanSha256").GetString(),
                    PolicyCalls = policyCalls,
                    AllFiniteChecksPassed = failure is null,
                    FullCompatibilityClaimed = false,
                    WholeContractsClosed = 0,
                    NormalizationPerformed = false,
                    Error = failure?.ToString()
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static async Task<byte[]> Encode(ProcessContext context, Term value, bool deterministic)
    {
        Term minor = Term.Tuple(Term.A(Resource529Names.Minor), Term.I(2));
        Term options = deterministic ? Term.List(Term.A(Resource529Names.DeterministicOption), minor) : Term.List(minor);
        var encoded = (BitString)await context.Runtime.Modules.Call(
            context,
            Resource529Names.Erlang,
            Resource529Names.Encode,
            value,
            options
        );

        return encoded.ToArray();
    }

    private static void Verify(string path, string hash) => Require(Path.IsPathFullyQualified(path) && Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) == hash.ToUpperInvariant());

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidDataException(Resource529Names.Invalid);
    }

    private static Exception Combine(Exception? previous, Exception error) => previous is null ? error : new AggregateException(previous, error);
}
