using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

internal static class Program
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    private static void Require(bool value)
    {
        if (!value) throw new InvalidDataException(Names.Failure);
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Install<T>(ModuleRegistry registry, IReadOnlyDictionary<string, object> capabilities)
    {
        Func<string, T> policy = path =>
        {
            Require(capabilities.TryGetValue(path, out object? value));

            return (T)value!;
        };
        typeof(ModuleRegistry).GetMethod(Names.InstallPolicy, Flags)!.Invoke(registry, [policy]);
    }

    private static object Capability(JsonElement image, string? bridge, string? bridgeHash)
    {
        string Text(string key) => image.GetProperty(key).GetString()!;
        Type type = typeof(ProcessRuntime).Assembly.GetType(Names.ReviewedType, true)!;

        return Activator.CreateInstance(
            type,
            Flags,
            null,
            [Text(Names.PathField), Text(Names.ShaField), Text(Names.BuildHashField), bridge, bridgeHash, null],
            null
        )!;
    }

    private static async ValueTask<Term> Purge(ProcessRuntime runtime, ProcessContext context)
    {
        MethodInfo method = typeof(ProcessRuntime).GetMethod(
            Names.PurgeMethod,
            Flags,
            null,
            [typeof(ProcessContext), typeof(string)],
            null
        )!;
        bool purged = await (ValueTask<bool>)method.Invoke(runtime, [context, Names.Subject])!;

        return Term.A(purged ? Names.TrueAtom : Names.FalseAtom);
    }

    private static int OldUnloadCount709(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete
        );
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(line => line.TrimEnd('\r') == "unload 1");
    }

    private static async Task<int> WaitOldUnload709(string path)
    {
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        while (System.Diagnostics.Stopwatch.GetElapsedTime(start) < TimeSpan.FromSeconds(Names.OldWaitSeconds709))
        {
            try
            {
                int count = OldUnloadCount709(path);
                if (count == 1) return count;
                Require(count == 0);
            }
            catch (IOException)
            {
                // The native writer owns a short append/flush/close interval.
                // Its actual raw record must appear before this phase succeeds.
            }
            await Task.Delay(TimeSpan.FromMilliseconds(Names.OldPollMilliseconds709));
        }
        throw new TimeoutException(Names.OldPhaseTimeout709);
    }

    private static async Task<int> Main(string[] args)
    {
        Require(args.Length == Names.CommandArguments && (OperatingSystem.IsWindows() || OperatingSystem.IsLinux()));
        string admission = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[2]);
        Require(Hash(admission) == args[1] && !Directory.Exists(output));
        using var document = JsonDocument.Parse(File.ReadAllBytes(admission));
        JsonElement plan = document.RootElement;
        Require(plan.GetProperty(Names.CleanupProtocolField709).GetString() == Names.CleanupProtocol709);

        string Text(string key) => plan.GetProperty(key).GetString()!;
        Require(Text(Names.PinField) == Names.Pin && Text(Names.StatusField) == Names.Admission);
        Require(plan.GetProperty(Names.CurrentMarkerField).GetBoolean());
        string retained = Text(Names.RetainedField), retainedHash = Text(Names.RetainedHashField);
        Require(Hash(retained) == retainedHash);
        using var retainedDocument = JsonDocument.Parse(File.ReadAllBytes(retained));
        JsonElement[] retainedFiles = retainedDocument.RootElement.GetProperty(Names.FilesField).EnumerateArray().ToArray();
        JsonElement runtimeFile = retainedFiles.Single(row => Path.GetFileName(row.GetProperty(Names.PathField).GetString()) == Names.RuntimeDll);
        Require(Hash(typeof(ProcessRuntime).Assembly.Location) == runtimeFile.GetProperty(Names.ShaField).GetString());
        JsonElement[] inputs = plan.GetProperty(Names.InputsField).EnumerateArray().ToArray();

        void VerifyInputs()
        {
            Require(Hash(admission) == args[1] && Hash(retained) == retainedHash);
            foreach (JsonElement row in inputs)
                Require(Hash(row.GetProperty(Names.PathField).GetString()!) == row.GetProperty(Names.ShaField).GetString());
        }
        VerifyInputs();
        string route = Text(Names.RouteField);
        Require(route is Names.Ordinary or Names.Reviewed);
        JsonElement[] images = plan.GetProperty(Names.ImagesField).EnumerateArray().ToArray();
        Require(images.Length == Names.NativeImages && images.Select(row => row.GetProperty(Names.PathField).GetString()).Distinct().Count() == Names.NativeImages);
        foreach (JsonElement image in images)
        {
            string library = image.GetProperty(Names.PathField).GetString()!;
            Require(Hash(library) == image.GetProperty(Names.ShaField).GetString());
            Require(library.EndsWith(OperatingSystem.IsWindows() ? Names.WindowsSuffix : Names.LinuxSuffix, StringComparison.Ordinal));
            Require(Hash(image.GetProperty(Names.BuildField).GetString()!) == image.GetProperty(Names.BuildHashField).GetString());
        }
        string? bridge = null, bridgeHash = null;
        if (OperatingSystem.IsLinux())
        {
            JsonElement proof = plan.GetProperty(Names.BridgeField);
            bridge = proof.GetProperty(Names.PathField).GetString()!;
            bridgeHash = proof.GetProperty(Names.ShaField).GetString()!;
            string receipt = proof.GetProperty(Names.ReceiptPathField).GetString()!;
            Require(Hash(bridge) == bridgeHash && Hash(receipt) == proof.GetProperty(Names.ReceiptHashField).GetString());
            using var bridgeDocument = JsonDocument.Parse(File.ReadAllBytes(receipt));
            JsonElement producer = bridgeDocument.RootElement;
            Require(producer.GetProperty(Names.StatusField).GetString() == Names.BridgeStatus
                && producer.GetProperty(Names.PinField).GetString() == Text(Names.PinField)
                && producer.GetProperty(Names.BridgePathField).GetString() == bridge
                && producer.GetProperty(Names.BridgeHashField).GetString() == bridgeHash);
        }
        Directory.CreateDirectory(output);
        string events = Path.Combine(output, Names.EventsFile);
        File.WriteAllBytes(events, []);
        var errors = new List<string>();
        var runtime = new ProcessRuntime();
        bool captured = false, closed = false;
        try
        {
            PreparedBeamImage? observer = null;
            await runtime.InitializeModulesAsync(registry =>
            {
                observer = registry.PrepareBeam(
                    File.ReadAllBytes(Text(Names.ObserverBeamField)),
                    new BeamDependencySelection([Names.ErlangModule, Names.Subject, Names.Observer])
                );
                Require(observer.Name == Names.Observer && registry.PublishPreparedBeam(observer).Status == BeamPublicationStatus.Published);
            });
            Require(runtime.Modules.CurrentCodeImage(Names.Subject) is null);
            Require(ReferenceEquals(runtime.Modules.SourceOwner(Names.Observer, Names.Capture, Names.CaptureArity), observer)
                && ReferenceEquals(runtime.Modules.SourceOwner(Names.Observer, Names.Worker, Names.WorkerArity), observer));
            if (route == Names.Reviewed)
            {
                var capabilities = images.ToDictionary(row => row.GetProperty(Names.PathField).GetString()!, row => Capability(row, bridge, bridgeHash), StringComparer.Ordinal);
                Type type = typeof(ProcessRuntime).Assembly.GetType(Names.ReviewedType, true)!;
                typeof(Program).GetMethod(nameof(Install), Flags)!.MakeGenericMethod(type).Invoke(null, [runtime.Modules, capabilities]);
            }
            else if (OperatingSystem.IsLinux())
            {
                // The ordinary loader binds the actual VM callbacks in its namespace.
                Require(bridge is not null
                    && Hash(Path.Combine(AppContext.BaseDirectory, Path.GetFileName(bridge))) == bridgeHash);
            }
            var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ProcessHandle controller = runtime.Spawn(async context =>
            {
                try
                {
                    string NativeStem(int index) => Path.ChangeExtension(images[index].GetProperty(Names.PathField).GetString()!, null)!;
                    Term result = await runtime.Modules.Call(
                        context,
                        Names.Observer,
                        Names.Capture,
                        new BitString(File.ReadAllBytes(Text(Names.Beam1Field))),
                        new BitString(File.ReadAllBytes(Text(Names.Beam2Field))),
                        Term.String(NativeStem(0)),
                        Term.String(events),
                        Term.I(plan.GetProperty(Names.ModeField).GetInt32())
                    );
                    File.WriteAllBytes(Path.Combine(output, Names.ObservedFile), ExternalTermFormat.Encode(result));
                    captured = true;
                    Term oldPurge = await Purge(runtime, context);
                    int oldUnloads = await WaitOldUnload709(events);
                    File.WriteAllBytes(
                        Path.Combine(output, Names.OldPhaseFile709),
                        ExternalTermFormat.Encode(Term.Tuple(Term.A(Names.OldPhaseAtom709), Term.I(oldUnloads)))
                    );
                    Term deleted = runtime.Modules.DeleteModule(Names.Subject);
                    Term currentPurge = await Purge(runtime, context);
                    File.WriteAllBytes(Path.Combine(output, Names.CodeCleanupFile), ExternalTermFormat.Encode(Term.Tuple(oldPurge, deleted, currentPurge)));
                    finished.TrySetResult();
                }
                catch (Exception error)
                {
                    finished.TrySetException(error);
                }

                return Nil.Value;
            });
            await finished.Task.WaitAsync(TimeSpan.FromSeconds(Names.InvocationSeconds));
            await controller.Completion.WaitAsync(TimeSpan.FromSeconds(Names.CleanupSeconds));
        }
        catch (Exception error)
        {
            errors.Add(error.ToString());
        }
        finally
        {
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(Names.CleanupSeconds));
                closed = true;
            }
            catch (Exception error)
            {
                errors.Add(Names.RuntimeErrorPrefix + error);
            }
            try
            {
                VerifyInputs();
            }
            catch (Exception error)
            {
                errors.Add(Names.IntegrityErrorPrefix + error);
            }
        }
        string[][] values = [];
        bool nativeClosed = false;
        try
        {
            values = File.ReadAllLines(events).Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToArray()).ToArray();
            Require(values.All(row => row.Length == Names.EventWidth));
            int accepted = values.Count(row => row[0] is Names.LoadEvent or Names.AcceptEvent), unloaded = values.Count(row => row[0] == Names.UnloadEvent);
            nativeClosed = accepted > 0 && accepted == unloaded;
        }
        catch (Exception error)
        {
            errors.Add(Names.EventsErrorPrefix + error);
        }
        bool passed = captured && closed && nativeClosed && errors.Count == 0;
        File.WriteAllText(
            Path.Combine(output, Names.CompletionFile),
            JsonSerializer.Serialize(
                new
                {
                    Passed = passed,
                    Captured = captured,
                    RuntimeClosed = closed,
                    AllAcceptedNativeOwnersUnloaded = nativeClosed,
                    Events = values,
                    CleanupProtocol = Names.CleanupProtocol709,
                    Route = route,
                    SubjectHead = Text(Names.SubjectHeadField711),
                    PortableN2 = true,
                    RuntimeManifestSha256 = retainedHash,
                    CurrentSourceReplay701 = true,
                    SourceFreezeSha256 = Text(Names.SourceFreezeHashField),
                    LoadedRuntimeSha256 = Hash(typeof(ProcessRuntime).Assembly.Location),
                    Errors = errors,
                    WholeN2Ready = false,
                    WholeApiCredits = 0
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );

        return passed ? 0 : 1;
    }
}
