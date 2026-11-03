using Erlang;
using Erlang.Otp;
using static Erlang.Otp.Platform.Library.Tests.PlatformLibraryNames;

namespace Erlang.Otp.Platform.Library.Tests;

internal static class PlatformLibraryChecks
{
    internal static async Task Run(string[] args)
    {
        await RunCorpus(args, false);
        await RunCorpus(args, true);
    }

    private static async Task RunCorpus(string[] args, bool legacy)
    {
        int passed = 0;

        void Check(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException(Failure);
            passed++;
        }

        void Reject(Action action)
        {
            bool rejected = false;
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Check(rejected);
        }

        byte[] Resource(string name)
        {
            using Stream input = typeof(Erlang.Otp.Platform.Library.Tests.PlatformLibraryNames).Assembly.GetManifestResourceStream(Prefix + (legacy ? LegacyPrefix : string.Empty) + name)
                ?? throw new InvalidDataException(MissingResource);
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }

        void Inspect(IReadOnlyList<string> modules, Func<string, byte[]> read)
        {
            foreach (string module in modules)
            {
                byte[] image = read(module);
                BeamArtifactInfo info;
                try
                {
                    info = BeamArtifactInspector.Inspect(image);
                }
                catch (Exception error)
                {
                    throw new InvalidOperationException(module, error);
                }
                Check(info.Name == module);
                image[0] ^= byte.MaxValue;
                Check(BeamArtifactInspector.Inspect(read(module)) == info);
            }
        }
        Check(OtpOsMon.Modules.Count == 8);
        Check(OtpOdbc.Modules.Count == 3);
        Inspect(OtpOsMon.Modules, OtpOsMon.ReadImage);
        Inspect(OtpOdbc.Modules, OtpOdbc.ReadImage);
        foreach (Func<byte[]> read in new Func<byte[]>[] { OtpOsMon.ReadApplication, OtpOsMon.ReadApplicationUpgrade, OtpOdbc.ReadApplication, OtpOdbc.ReadApplicationUpgrade })
        {
            byte[] bytes = read();
            Check(bytes.Length > 0);
            bytes[0] ^= byte.MaxValue;
            Check(bytes[0] != read()[0]);
        }

        void Support(IReadOnlyList<string> names, Func<string, byte[]> read)
        {
            foreach (string name in names)
            {
                byte[] bytes = read(name);
                Check(bytes.AsSpan().SequenceEqual(read(name)));
                if (bytes.Length > 0)
                    bytes[0] ^= byte.MaxValue;
                Check(bytes.Length == 0 || bytes[0] != read(name)[0]);
            }
            Reject(() => read(Traversal));
            Reject(() => read(Unknown));
        }
        Check(OtpOsMon.SupportFiles.Count == 0);
        Check(OtpOdbc.SupportFiles.Count == 0);
        Support(OtpOsMon.SupportFiles, OtpOsMon.ReadSupportFile);
        Support(OtpOdbc.SupportFiles, OtpOdbc.ReadSupportFile);
        Reject(() => OtpOsMon.ReadImage(Traversal));
        Reject(() => OtpOdbc.ReadImage(Unknown));
        Reject(() => OtpOsMon.Beam(DiskSupervisor, null!));
        Reject(() => OtpOdbc.Beam(OdbcModule, null!));
        foreach (RuntimeModuleSelection selection in new[] { OtpOsMon.Beam(DiskSupervisor, [Lists]), OtpOdbc.Beam(OdbcModule, [Lists]) })
            Check(selection.Backend == RuntimeModuleBackend.Beam && selection.Dependencies.SequenceEqual([Lists]));
        Reject(() => OtpOsMon.Beam(DiskSupervisor, [Lists, Lists]));
        Reject(() => OtpOdbc.Beam(OdbcModule, [string.Empty]));
        var runtime = new ProcessRuntime();
        try
        {
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                (registry, scope) =>
            {
                string[] imports = OtpOsMon.Modules.Concat(OtpOdbc.Modules).Concat(OtpStdlib.Modules).Concat(PlatformLibraryNames.OriginalImports).Concat([Observer]).ToArray();
                var dependencies = new BeamDependencySelection(imports);
                byte[][] bytes = OtpOsMon.Modules.Select(module => OtpOsMon.ReadImage(module))
                    .Concat(OtpOdbc.Modules.Select(module => OtpOdbc.ReadImage(module)))
                    .Concat([OtpStdlib.ReadImage(Lists), OtpStdlib.ReadImage(IoLibrary), OtpStdlib.ReadImage(InputOutput), OtpStdlib.ReadImage(UnicodeUtilities), Resource(ObserverImage)]).ToArray();
                PreparedBeamImage[] images = bytes.Select(image =>
                {
                    try
                    {
                        return scope.Prepare(() => registry.PrepareBeam(image, dependencies));
                    }
                    catch (Exception error)
                    {
                        throw new InvalidOperationException(BeamArtifactInspector.Inspect(image).Name, error);
                    }
                }).ToArray();

                return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
                {
                    Check(registry.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
                    foreach (PreparedBeamImage image in images)
                    {
                        var exports = registry.Exports.Where(export => export.Module == image.Name).ToArray();
                        Check(exports.Length > 0 && exports.All(export => ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image)));
                    }
                });
            }
            );
            var completed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
            ProcessHandle actor = runtime.Spawn(async context =>
            {
                try
                {
                    Term result = await runtime.Modules.Call(
                        context,
                        Observer,
                        Worker,
                        context.Self
                    );
                    completed.TrySetResult(((TupleTerm)result).Items[1]);
                }
                catch (Exception error)
                {
                    completed.TrySetException(error);
                }

                return Nil.Value;
            });
            Term outcome = await completed.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await actor.Completion.WaitAsync(TimeSpan.FromSeconds(30));
            byte[] original = Resource(OriginalValues);
            Term expected = ExternalTermFormat.Decode(original);
            if (args.Length == 1)
            {
                string output = legacy ? Path.Combine(args[0], LegacyDirectory) : args[0];
                Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output, ManagedValues), ExternalTermFormat.Encode(outcome));
                File.WriteAllText(Path.Combine(output, ManagedText), outcome.ToString());
            }
            Check(outcome.Equals(expected));
            Check(ExternalTermFormat.Encode(outcome).AsSpan().SequenceEqual(original));
        }
        finally
        {
            await runtime.DisposeAsync();
        }
        Console.WriteLine($"Original os_mon/odbc packaging{(legacy ? LegacyPrefix : string.Empty)}: {passed} checks passed; 875 complete outcomes/stacks match OTP");
    }
}
