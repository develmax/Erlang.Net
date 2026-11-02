using Erlang;
using Erlang.Otp;
using static Erlang.Otp.Security.Library.Tests.SecurityLibraryNames;

namespace Erlang.Otp.Security.Library.Tests;

internal static class SecurityLibraryChecks
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
            using Stream input = typeof(Erlang.Otp.Security.Library.Tests.SecurityLibraryNames).Assembly.GetManifestResourceStream(Prefix + name)
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
                BeamArtifactInfo info = BeamArtifactInspector.Inspect(image);
                Check(info.Name == module);
                image[0] ^= byte.MaxValue;
                Check(BeamArtifactInspector.Inspect(read(module)) == info);
            }
        }
        Check(OtpCrypto.Modules.Count == 2);
        Check(OtpPublicKey.Modules.Count == 42);
        Inspect(OtpCrypto.Modules, OtpCrypto.ReadImage);
        Inspect(OtpPublicKey.Modules, OtpPublicKey.ReadImage);
        foreach (Func<byte[]> read in new Func<byte[]>[] { OtpCrypto.ReadApplication, OtpCrypto.ReadApplicationUpgrade, OtpPublicKey.ReadApplication, OtpPublicKey.ReadApplicationUpgrade })
        {
            byte[] bytes = read();
            Check(bytes.Length > 0);
            bytes[0] ^= byte.MaxValue;
            Check(bytes[0] != read()[0]);
        }
        Reject(() => OtpCrypto.ReadImage(Traversal));
        Reject(() => OtpPublicKey.ReadImage(Unknown));
        Reject(() => OtpCrypto.Beam(Curves, null!));
        Reject(() => OtpPublicKey.Beam(Translation, null!));
        RuntimeModuleSelection fragment = OtpCrypto.Beam(Curves, [Lists]);
        RuntimeModuleSelection application = OtpPublicKey.Beam(Translation, [Lists]);
        Check(fragment.Module == Curves && fragment.Backend == RuntimeModuleBackend.Beam && fragment.Dependencies.SequenceEqual([Lists]));
        Check(application.Module == Translation && application.Backend == RuntimeModuleBackend.Beam && application.Dependencies.SequenceEqual([Lists]));
        Reject(() => OtpCrypto.Beam(Curves, [Lists, Lists]));
        Reject(() => OtpPublicKey.Beam(Translation, [string.Empty]));
        var runtime = new ProcessRuntime();
        try
        {
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                (registry, scope) =>
            {
                string[] imports = OtpCrypto.Modules.Concat(OtpPublicKey.Modules).Concat(OtpStdlib.Modules).Concat([Observer, Asn1Runtime, ErlangModule]).ToArray();
                var dependencies = new BeamDependencySelection(imports);
                byte[][] bytes = [OtpStdlib.ReadImage(Lists), OtpCrypto.ReadImage(Curves), OtpPublicKey.ReadImage(Translation), OtpPublicKey.ReadImage(PolicyTree), OtpPublicKey.ReadImage(Pkcs3), Resource(ObserverImage)];
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
        Console.WriteLine($"Original crypto/public_key packaging: {passed} checks passed; 213 complete outcomes/stacks match OTP");
    }
}
