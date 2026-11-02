using Erlang;
using Erlang.Otp;
using static Erlang.Otp.Megaco.Library.Tests.MegacoLibraryNames;

namespace Erlang.Otp.Megaco.Library.Tests;

internal static class MegacoLibraryChecks
{
    internal static async Task Run(string[] args)
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
            using Stream input = typeof(Erlang.Otp.Megaco.Library.Tests.MegacoLibraryNames).Assembly.GetManifestResourceStream(Prefix + name)
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
        Check(OtpMegaco.Modules.Count == 65);
        Inspect(OtpMegaco.Modules, OtpMegaco.ReadImage);
        foreach (Func<byte[]> read in new Func<byte[]>[] { OtpMegaco.ReadApplication, OtpMegaco.ReadApplicationUpgrade })
        {
            byte[] bytes = read();
            Check(bytes.Length > 0);
            bytes[0] ^= byte.MaxValue;
            Check(bytes[0] != read()[0]);
        }
        Reject(() => OtpMegaco.ReadImage(Traversal));
        Reject(() => OtpMegaco.ReadImage(Unknown));
        Reject(() => OtpMegaco.Beam(TimerModule, null!));
        RuntimeModuleSelection selection = OtpMegaco.Beam(TimerModule, [Configuration]);
        Check(selection.Backend == RuntimeModuleBackend.Beam && selection.Dependencies.SequenceEqual([Configuration]));
        Reject(() => OtpMegaco.Beam(TimerModule, [Configuration, Configuration]));
        Reject(() => OtpMegaco.Beam(TimerModule, [string.Empty]));
        var runtime = new ProcessRuntime();
        try
        {
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                (registry, scope) =>
            {
                string[] imports = OtpMegaco.Modules.Concat(OtpStdlib.Modules).Concat([Observer, Internal, ErlangModule]).ToArray();
                var dependencies = new BeamDependencySelection(imports);
                byte[][] bytes = [OtpStdlib.ReadImage(Lists), OtpMegaco.ReadImage(TimerModule), OtpMegaco.ReadImage(Configuration), OtpMegaco.ReadImage(TerminationId), OtpMegaco.ReadImage(GeneratedTerminationId), Resource(ObserverImage)];
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
                Directory.CreateDirectory(args[0]);
                File.WriteAllBytes(Path.Combine(args[0], ManagedValues), ExternalTermFormat.Encode(outcome));
                File.WriteAllText(Path.Combine(args[0], ManagedText), outcome.ToString());
            }
            Check(outcome.Equals(expected));
            Check(ExternalTermFormat.Encode(outcome).AsSpan().SequenceEqual(original));
        }
        finally
        {
            await runtime.DisposeAsync();
        }
        Console.WriteLine($"Original megaco packaging: {passed} checks passed; 2003 complete outcomes/stacks match OTP");
    }
}
