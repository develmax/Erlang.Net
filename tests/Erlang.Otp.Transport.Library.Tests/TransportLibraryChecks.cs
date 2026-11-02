using Erlang;
using Erlang.Otp;
using static Erlang.Otp.Transport.Library.Tests.TransportLibraryNames;

namespace Erlang.Otp.Transport.Library.Tests;

internal static class TransportLibraryChecks
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
            using Stream input = typeof(Erlang.Otp.Transport.Library.Tests.TransportLibraryNames).Assembly.GetManifestResourceStream(Prefix + name)
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
        Check(OtpSsl.Modules.Count == 78);
        Check(OtpSsh.Modules.Count == 43);
        Inspect(OtpSsl.Modules, OtpSsl.ReadImage);
        Inspect(OtpSsh.Modules, OtpSsh.ReadImage);
        foreach (Func<byte[]> read in new Func<byte[]>[] { OtpSsl.ReadApplication, OtpSsl.ReadApplicationUpgrade, OtpSsh.ReadApplication, OtpSsh.ReadApplicationUpgrade })
        {
            byte[] bytes = read();
            Check(bytes.Length > 0);
            bytes[0] ^= byte.MaxValue;
            Check(bytes[0] != read()[0]);
        }
        Reject(() => OtpSsl.ReadImage(Traversal));
        Reject(() => OtpSsh.ReadImage(Unknown));
        Reject(() => OtpSsl.Beam(CipherFormat, null!));
        Reject(() => OtpSsh.Beam(SshBits, null!));
        RuntimeModuleSelection fragment = OtpSsl.Beam(CipherFormat, [Lists]);
        RuntimeModuleSelection application = OtpSsh.Beam(SshBits, [Lists]);
        Check(fragment.Module == CipherFormat && fragment.Backend == RuntimeModuleBackend.Beam && fragment.Dependencies.SequenceEqual([Lists]));
        Check(application.Module == SshBits && application.Backend == RuntimeModuleBackend.Beam && application.Dependencies.SequenceEqual([Lists]));
        Reject(() => OtpSsl.Beam(CipherFormat, [Lists, Lists]));
        Reject(() => OtpSsh.Beam(SshBits, [string.Empty]));
        var runtime = new ProcessRuntime();
        try
        {
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                (registry, scope) =>
            {
                string[] imports = OtpSsl.Modules.Concat(OtpSsh.Modules).Concat(OtpStdlib.Modules).Concat([Observer, Internal, ErlangModule, Crypto, Logger, Debugger]).ToArray();
                var dependencies = new BeamDependencySelection(imports);
                byte[][] bytes = [OtpStdlib.ReadImage(Lists), OtpStdlib.ReadImage(StringModule), OtpStdlib.ReadImage(Unicode), OtpStdlib.ReadImage(UnicodeUtilities), OtpSsl.ReadImage(CipherFormat), OtpSsh.ReadImage(SshBits), OtpSsh.ReadImage(SshMessage), OtpSsl.ReadImage(SslAlert), Resource(ObserverImage)];
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
        Console.WriteLine($"Original ssl/ssh packaging: {passed} checks passed; 1107 complete outcomes/stacks match OTP");
    }
}
