using System.Security.Cryptography;
using System.Text.Json;
using static Erlang.Tests.DynamicApplyBoundary520Names;

namespace Erlang.Tests;

internal static class DynamicApplyBoundary520Checks
{
    internal static async Task Run(
        string repository,
        string completion,
        string expectedSha,
        bool owned,
        string output
    )
    {
        repository = Path.GetFullPath(repository);
        completion = Path.GetFullPath(completion);
        if (!File.Exists(completion) || Digest(completion) != expectedSha.ToUpperInvariant() || Directory.Exists(output))
            throw new InvalidDataException(MissingAdmission);
        using var receipt = JsonDocument.Parse(File.ReadAllBytes(completion));
        JsonElement root = receipt.RootElement;
        Require(root.GetProperty(PinProperty).GetString() == Pin && root.GetProperty(OriginalOnlyProperty).GetBoolean()
            && root.GetProperty(AcquisitionPassedProperty).GetBoolean());
        foreach (string phaseName in new[] { CompilePhase, ObserverPhase })
        {
            JsonElement phase = root.GetProperty(PhasesProperty).GetProperty(phaseName);
            Require(phase.GetProperty(ExitCodeProperty).GetInt32() == 0);
            bool settled = phase.TryGetProperty(SettledProperty, out var value) && value.GetBoolean();
            settled |= phase.TryGetProperty(ParentReapedProperty, out value) && value.GetBoolean();
            Require(settled);
        }
        string original = Path.GetDirectoryName(completion)!;
        foreach (var file in root.GetProperty(FilesProperty).EnumerateArray())
        {
            string name = file.GetProperty(PathProperty).GetString()!;
            string path = Path.GetFullPath(Path.Combine(original, name));
            Require(path.StartsWith(original + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
            Require(File.Exists(path) && new FileInfo(path).Length == file.GetProperty(BytesProperty).GetInt64()
                && Digest(path) == file.GetProperty(ShaProperty).GetString());
        }
        foreach (var input in root.GetProperty(SourceInputsProperty).EnumerateArray())
        {
            string path = Path.GetFullPath(Path.Combine(repository, input.GetProperty(PathProperty).GetString()!));
            Require(path.StartsWith(repository + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && File.Exists(path) && SourceDigest(path, input) == input.GetProperty(ShaProperty).GetString());
        }
        var images = root.GetProperty(ImagesProperty).EnumerateArray().ToDictionary(image => image.GetProperty(ModuleProperty).GetString()!, StringComparer.Ordinal);
        Require(images.Count == 2 && images.ContainsKey(Caller) && images.ContainsKey(Target));

        byte[] Image(string module)
        {
            JsonElement image = images[module];
            string[] dependencies = image.GetProperty(StaticModulesProperty).EnumerateArray().Select(value => value.GetString()!).ToArray();
            Require(dependencies.SequenceEqual(new[] { ErlangModule }));
            string name = image.GetProperty(NameProperty).GetString()!;
            Require(Path.GetFileName(name) == name);
            string path = Path.Combine(original, name);
            Require(File.Exists(path) && Digest(path) == image.GetProperty(ShaProperty).GetString());

            return File.ReadAllBytes(path);
        }
        // Read genuine admitted BEAM and raw originals before entering a VM.
        byte[] callerBytes = Image(Caller), targetBytes = Image(Target);
        JsonElement loader = root.GetProperty(LoaderImageProperty);
        Require(loader.GetProperty(ModuleProperty).GetString() == LoaderModule);
        string loaderName = loader.GetProperty(NameProperty).GetString()!;
        Require(Path.GetFileName(loaderName) == loaderName);
        string loaderPath = Path.Combine(original, loaderName);
        string loaderSha = loader.GetProperty(ShaProperty).GetString()!;
        Require(File.Exists(loaderPath) && Digest(loaderPath) == loaderSha);
        byte[] loaderBytes = File.ReadAllBytes(loaderPath);
        JsonElement nativeMetadata = root.GetProperty(DynamicApplyNativeMetadata527Names.Field);
        Require(nativeMetadata.GetProperty(ModuleProperty).GetString() == ErlangModule);
        string metadataName = nativeMetadata.GetProperty(NameProperty).GetString()!;
        Require(Path.GetFileName(metadataName) == metadataName);
        string metadataPath = Path.Combine(original, metadataName);
        Require(File.Exists(metadataPath) && Digest(metadataPath) == nativeMetadata.GetProperty(ShaProperty).GetString());
        byte[] metadataBytes = File.ReadAllBytes(metadataPath);
        RuntimeNativeMetadataImage metadataProfile = DynamicApplyNativeMetadata527Admission.Verify(original, nativeMetadata, metadataBytes);
        Require(metadataProfile.OriginalBeamSha256 == nativeMetadata.GetProperty(ShaProperty).GetString());
        byte[] expectedRaw = File.ReadAllBytes(Path.Combine(original, Raw));
        byte[] expectedDeterministic = File.ReadAllBytes(Path.Combine(original, Deterministic));
        string[] callerImports = DynamicApplyBoundary520Imports.Read(callerBytes);
        string[] targetImports = DynamicApplyBoundary520Imports.Read(targetBytes);
        Require(callerImports.SequenceEqual(new[] { ErlangModule }) && targetImports.SequenceEqual(new[] { ErlangModule }));
        string[] selected = DynamicApplyBoundary520Imports.Read(loaderBytes).Append(ErlangModule)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        Require(!selected.Contains(Target, StringComparer.Ordinal));
        Directory.CreateDirectory(output);
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned, metadataProfile);
        PreparedBeamImage? caller = null, target = null;
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            (registry, scope) =>
        {
            // Actual caller static imports remain [erlang]. Explicit loader
            // authority grants ONLY the admitted ODBC image's imported modules.
            // The dynamic target is absent from that declared selection.
            PreparedBeamImage prepared = scope.Prepare(() => registry.PrepareBeam(callerBytes, new BeamDependencySelection(selected)));
            caller = prepared;
            Require(!registry.HasExport(Target, TargetEcho, 1));

            return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
            {
                Require(registry.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
            });
        }
        );
        // Separate late publication after the caller has already been prepared.
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            (registry, scope) =>
        {
            PreparedBeamImage prepared = scope.Prepare(() => registry.PrepareBeam(targetBytes, new BeamDependencySelection(new[] { ErlangModule })));
            target = prepared;

            return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
            {
                Require(registry.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
            });
        }
        );
        Require(caller is not null && target is not null);
        bool hostSelectionRejected = false;
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunOutsideAsync(
            runtime,
            (registry, scope) =>
        {
            Require(!registry.HasExport(LoaderModule, LoaderFunction, 3));
            try
            {
                scope.Prepare(() => registry.PrepareBeam(loaderBytes, new BeamDependencySelection(new[] { ErlangModule })));
            }
            catch (NotSupportedException)
            {
                hostSelectionRejected = true;
            }

            return ValueTask.CompletedTask;

        }
        );
        // Both host and guest loader retain declared dependency authority.
        // Guest loader success below inherits the nearest actual caller's
        // admitted selection; this control keeps the narrow host refusal.
        Require(hostSelectionRejected);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term spawned = await runtime.Modules.Call(
                context,
                ErlangModule,
                SpawnMonitor,
                Term.A(Caller),
                Term.A(Worker),
                Term.List(Term.Tuple(context.Self, Term.A(LoaderModule), new BitString(loaderBytes)))
            );
            Require(spawned is TupleTerm { Items: [Pid, ReferenceTerm] });
            var pair = (TupleTerm)spawned;
            var pid = (Pid)pair.Items[0];
            var monitor = (ReferenceTerm)pair.Items[1];
            bool downSeen = false;
            Exception? observedFailure = null;
            try
            {
                Term? reply = await context.ReceiveAsync(message => IsReply(message, pid) || IsDown(message, pid, monitor) ? message : null, Deadline);
                if (reply is not null && IsDown(reply, pid, monitor))
                {
                    downSeen = true;
                    await File.WriteAllBytesAsync(Path.Combine(output, WorkerFailure), await Encode(reply, false));
                }
                Require(reply is not null && IsReply(reply, pid));
                Term observed = ((TupleTerm)reply!).Items[2];
                Require(Cons.Items(observed).Count() == Cases);
                Term? down = await context.ReceiveAsync(message => IsDown(message, pid, monitor) ? message : null, Deadline);
                downSeen = down is not null && IsDown(down, pid, monitor);
                if (downSeen)
                    await File.WriteAllBytesAsync(Path.Combine(output, WorkerDown), await Encode(down!, false));
                Require(down is TupleTerm { Items.Count: 5 } && ((TupleTerm)down).Items[4].Equals(Term.A(NormalReason)));
                Term alive = await runtime.Modules.Call(
                    context,
                    ErlangModule,
                    "is_process_alive",
                    pid
                );
                Require(alive.Equals(Term.A(FalseAtom)));
                byte[] raw = await Encode(observed, false);
                byte[] deterministic = await Encode(observed, true);
                await File.WriteAllBytesAsync(Path.Combine(output, Raw), raw);
                await File.WriteAllBytesAsync(Path.Combine(output, Deterministic), deterministic);
                Term witness = Term.Tuple(
                    context.Self,
                    pid,
                    monitor,
                    down!,
                    alive
                );
                await File.WriteAllBytesAsync(Path.Combine(output, Witness), await Encode(witness, false));
                Require(raw.AsSpan().SequenceEqual(expectedRaw) && deterministic.AsSpan().SequenceEqual(expectedDeterministic));
                Require(ReferenceEquals(runtime.Modules.SourceOwner(Caller, Worker, 1), caller)
                    && ReferenceEquals(runtime.Modules.SourceOwner(Target, TargetEcho, 1), target));
                Require(runtime.Modules.SourceOwner(LoaderModule, LoaderFunction, 3) is PreparedBeamImage loaded
                    && loaded.OriginalBeamSha256 == loaderSha);
            }
            catch (Exception error)
            {
                observedFailure = error;
                throw;
            }
            finally
            {
                try
                {
                    if (!downSeen)
                    {
                        _ = await runtime.Modules.Call(
                            context,
                            ErlangModule,
                            ExitFunction,
                            pid,
                            Term.A(KillReason)
                        );
                        Term? cleanupDown = await context.ReceiveAsync(message => IsDown(message, pid, monitor) ? message : null, Deadline);
                        Require(cleanupDown is not null);
                    }
                    _ = await runtime.Modules.Call(
                        context,
                        ErlangModule,
                        Demonitor,
                        monitor,
                        Term.List(Term.A(FlushOption))
                    );
                }
                catch (Exception cleanup)
                {
                    if (observedFailure is not null)
                        throw new AggregateException(observedFailure, cleanup);
                    throw;
                }
            }

            return true;

            async Task<byte[]> Encode(Term value, bool deterministic)
            {
                Term minor = Term.Tuple(Term.A(MinorVersionOption), Term.I(SerializationMinorVersion));
                Term options = deterministic ? Term.List(Term.A(DeterministicOption), minor) : Term.List(minor);
                var binary = (BitString)await runtime.Modules.Call(
                    context,
                    ErlangModule,
                    EncodeFunction,
                    value,
                    options
                );

                return binary.ToArray();
            }
        });
    }

    private static bool IsReply(Term value, Pid pid) => value is TupleTerm { Items.Count: 3 } tuple
        && tuple.Items[0].Equals(Term.A(Reply)) && tuple.Items[1].Equals(pid);

    private static bool IsDown(Term value, Pid pid, ReferenceTerm monitor) => value is TupleTerm { Items.Count: 5 } tuple
        && tuple.Items[0].Equals(Term.A(DownTag)) && tuple.Items[1].Equals(monitor) && tuple.Items[3].Equals(pid);

    internal static string SourceDigest(string path, JsonElement input)
    {
        string mode = input.TryGetProperty(HashModeProperty, out JsonElement value) ? value.GetString()! : RawMode;
        Require(mode is RawMode or TextLfMode);
        byte[] bytes = File.ReadAllBytes(path);
        if (mode == TextLfMode)
        {
            byte[] original = bytes;
            bytes = original.Where((value, index) => value != 13 || index + 1 == original.Length || original[index + 1] != 10).ToArray();
        }

        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static string Digest(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
