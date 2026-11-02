using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using static CatalogValidationNames;

if (args.Length != 4)
{
    Console.Error.WriteLine(Usage);

    return 2;
}

string repository = Path.GetFullPath(args[0]);
string admission = Path.GetFullPath(args[1]);
string manifest = Path.GetFullPath(args[2]);
string output = Path.GetFullPath(args[3]);
Directory.CreateDirectory(output);
var preparations = new List<object>();
var comparisons = new List<object>();
var errors = new List<string>();
var owned = new List<Process>();
var children = new List<object>();
int prepared = 0, portable = 0;
bool protocolMatched = false, rawKeysEqual = false, published = false;
bool capturesMatched = false;
ProcessRuntime? runtime = null;
RuntimeRegexEngine411? regex = null;
try
{
    CatalogImage[] inventory = CatalogInventory.Read(repository);
    string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(manifest)));
    runtime = await ProcessRuntime.CreateWithOptionsAsync(
        new RuntimeLibrarySelection(Array.Empty<RuntimeModuleSelection>()),
        new RuntimeLibraryStartupOptions(
            RuntimeNativePrimitives.None,
            false,
            null,
            new RuntimeDriverFrontendOptions(manifest, hash, asyncThreads: 1)
        ),
        default
    );
    string childPath = Path.Combine(Path.GetDirectoryName(manifest)!, DriverExecutable);
    foreach (Process process in Process.GetProcessesByName(DriverHost))
    {
        if (string.Equals(process.MainModule?.FileName, childPath, StringComparison.OrdinalIgnoreCase))
        {
            _ = process.SafeHandle;
            owned.Add(process);
        }
        else
            process.Dispose();
    }
    if (owned.Count != 1)
        throw new InvalidOperationException(Failure);
    regex = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules, admission);
    await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
        runtime,
        async (registry, scope) =>
    {
        await runtime.InitializeModulesAsync(_ =>
        {
            RuntimeNativePrimitiveProviders.RegisterCatalog(runtime, registry);
        });
        var dependencies = new BeamDependencySelection(inventory.Select(image => image.Module).Append(CatalogValidationNames.Erlang));
        var execution = new List<PreparedBeamImage>();
        foreach (CatalogImage entry in inventory)
        {
            try
            {
                PreparedBeamImage image = scope.Prepare(() => registry.PrepareBeam(File.ReadAllBytes(entry.Path), dependencies));
                prepared++;
                preparations.Add(new
                {
                    entry.Module,
                    entry.Sha256,
                    Prepared = true,
                    Natives = image.NativePrimitiveExports.Select(native => new { native.Name, native.Arity }).ToArray()
                });
                if (ExecutionModules.Contains(entry.Module, StringComparer.Ordinal))
                    execution.Add(image);
            }
            catch (Exception error)
            {
                errors.Add(entry.Module + Environment.NewLine + error);
                preparations.Add(new { entry.Module, entry.Sha256, Prepared = false, Error = error.ToString() });
            }
        }
        if (execution.Count != ExecutionModules.Length)
            throw new InvalidOperationException(Failure);
        foreach (var (directory, observer) in new[] { (Indexed, IndexedObserver), (Protocol, ProtocolObserver) })
            execution.Add(scope.Prepare(() => registry.PrepareBeam(File.ReadAllBytes(Path.Combine(
                repository,
                Fixtures,
                directory,
                observer + Beam
            )), dependencies)));
        execution.Add(scope.Prepare(() => registry.PrepareBeam(File.ReadAllBytes(Path.Combine(repository, CaptureFixtures, CaptureObserver + Beam)), dependencies)));

        return _ =>
        {
            if (registry.PublishPreparedBeam(execution.ToArray()).Status != BeamPublicationStatus.Published)
                throw new InvalidOperationException(Failure);
            foreach (PreparedBeamImage image in execution)
            {
                var exports = registry.Exports.Where(export => export.Module == image.Name).ToArray();
                if (exports.Length == 0 || exports.Any(export => !ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image)))
                    throw new InvalidOperationException(Failure);
            }
            published = true;
        };
    }
    );
    for (int index = 1; index <= RawKeyGroup; index++)
    {
        try
        {
            TupleTerm result = await Invoke(IndexedObserver, index);
            byte[] actual = ExternalTermFormat.Encode(result.Items[2]);
            File.WriteAllBytes(Path.Combine(output, ManagedPrefix + index + Etf), actual);
            byte[] original = File.ReadAllBytes(Path.Combine(
                repository,
                Fixtures,
                Indexed,
                OriginalPrefix + index + Etf
            ));
            bool equal = actual.AsSpan().SequenceEqual(original);
            if (index <= PortableGroups)
            {
                if (equal)
                    portable++;
                else
                    errors.Add(Failure + index);
            }
            else
                rawKeysEqual = equal;
            comparisons.Add(new { Index = index, WholeRawEqual = equal, OwnStateKeyTuple = index == RawKeyGroup });
        }
        catch (Exception error)
        {
            errors.Add(error.ToString());
            comparisons.Add(new { Index = index, Error = error.ToString(), Skipped = false });
        }
    }
    try
    {
        TupleTerm result = await Invoke(ProtocolObserver, null);
        byte[] actual = ExternalTermFormat.Encode(result.Items[1]);
        File.WriteAllBytes(Path.Combine(output, ManagedPrefix + OriginalProtocol), actual);
        File.WriteAllBytes(Path.Combine(output, ManagedPrefix + OriginalKeys), ExternalTermFormat.Encode(result.Items[2]));
        protocolMatched = actual.AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(
            repository,
            Fixtures,
            Protocol,
            OriginalProtocol
        )));
    }
    catch (Exception error)
    {
        errors.Add(error.ToString());
    }
    var captures = new List<Term>();
    for (int index = 1; index <= CaptureCount; index++)
    {
        try
        {
            TupleTerm result = await Invoke(CaptureObserver, index);
            captures.Add(result.Items[2]);
        }
        catch (Exception error)
        {
            errors.Add(error.ToString());
            captures.Add(Nil.Value);
        }
    }
    byte[] encodedCaptures = ExternalTermFormat.Encode(Term.List(captures.ToArray()));
    File.WriteAllBytes(Path.Combine(output, CaptureOutput), encodedCaptures);
    capturesMatched = encodedCaptures.AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(repository, CaptureFixtures, CaptureExpected)));
}
catch (Exception error)
{
    errors.Add(error.ToString());
}
finally
{
    if (runtime is not null)
    {
        try
        {
            await runtime.DisposeAsync();
        }
        catch (Exception error)
        {
            errors.Add(error.ToString());
        }
    }
    foreach (Process child in owned)
    {
        try
        {
            bool exited = child.WaitForExit(ShutdownMilliseconds);
            int? code = exited ? child.ExitCode : null;
            children.Add(new { child.Id, HasExited = exited, ExitCode = code });
            if (!exited || code != 0)
                errors.Add(Failure);
            if (!exited)
            {
                child.Kill(true);
                child.WaitForExit(ShutdownMilliseconds);
            }
        }
        finally
        {
            child.Dispose();
        }
    }
}

bool cleanup = regex is not null && regex.Report.Closed && regex.Report.LibraryFreed
    && regex.Report.Active == 0 && regex.Report.RetainedCodes == 0
    && regex.Report.CodesCreated == regex.Report.CodesFreed
    && regex.Report.MatchDataCreated == regex.Report.MatchDataFreed;
bool passed = errors.Count == 0 && prepared == ImagesCount && portable == PortableGroups && protocolMatched && capturesMatched && cleanup && children.Count == 1;
File.WriteAllText(
    Path.Combine(output, Receipt),
    JsonSerializer.Serialize(
        new
        {
            Passed = passed,
            Expected = ImagesCount,
            Prepared = prepared,
            Applications = ApplicationsCount,
            ExplicitTypedComposition = true,
            NativeImagesPublishedWithSourceOwners = published,
            SupplementaryImagesOutsidePrimaryCatalog = SupplementaryImages,
            Preparations = preparations,
            PortableNativeOutcomes = portable,
            Comparisons = comparisons,
            RawKeyTupleEqual = rawKeysEqual,
            OwnStateMapProtocolEqual = protocolMatched,
            CaptureCount,
            CapturesWholeRawEqual = capturesMatched,
            Cleanup = regex?.Report,
            CleanupPassed = cleanup,
            OwnedChildren = children,
            Errors = errors,
            WholeModuleCompatibilityClaimed = false
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine($"Configured catalog: {prepared}/{ImagesCount}; native outcomes: {portable}/{PortableGroups}; own-state map protocol: {protocolMatched}; cleanup: {cleanup}");

return passed ? 0 : 1;

async Task<TupleTerm> Invoke(string observer, int? index)
{
    var done = new TaskCompletionSource<TupleTerm>(TaskCreationOptions.RunContinuationsAsynchronously);
    var actor = runtime!.Spawn(async context =>
    {
        try
        {
            Term argument = index is null ? context.Self : Term.Tuple(context.Self, Term.I(index.Value));
            done.TrySetResult((TupleTerm)await runtime.Modules.Call(
                context,
                observer,
                Worker,
                argument
            ));
        }
        catch (Exception error)
        {
            done.TrySetException(error);
        }

        return Nil.Value;
    });
    try
    {
        return await done.Task.WaitAsync(TimeSpan.FromSeconds(TimeoutSeconds));
    }
    finally
    {
        await actor.Completion.WaitAsync(TimeSpan.FromSeconds(TimeoutSeconds));
    }
}
