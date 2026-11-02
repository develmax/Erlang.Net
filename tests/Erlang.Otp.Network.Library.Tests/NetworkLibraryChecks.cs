using Erlang;
using Erlang.Otp;
using static Erlang.Otp.Network.Library.Tests.NetworkLibraryNames;

namespace Erlang.Otp.Network.Library.Tests;

internal static class NetworkLibraryChecks
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
            using Stream input = typeof(Erlang.Otp.Network.Library.Tests.NetworkLibraryNames).Assembly.GetManifestResourceStream(Prefix + name)
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
        Check(OtpInets.Modules.Count == 63);
        Check(OtpFtp.Modules.Count == 6);
        Check(OtpTftp.Modules.Count == 8);
        Check(OtpEldap.Modules.Count == 2);
        Inspect(OtpInets.Modules, OtpInets.ReadImage);
        Inspect(OtpFtp.Modules, OtpFtp.ReadImage);
        Inspect(OtpTftp.Modules, OtpTftp.ReadImage);
        Inspect(OtpEldap.Modules, OtpEldap.ReadImage);
        foreach (Func<byte[]> read in new Func<byte[]>[] { OtpInets.ReadApplication, OtpInets.ReadApplicationUpgrade, OtpFtp.ReadApplication, OtpFtp.ReadApplicationUpgrade, OtpTftp.ReadApplication, OtpTftp.ReadApplicationUpgrade, OtpEldap.ReadApplication, OtpEldap.ReadApplicationUpgrade })
        {
            byte[] bytes = read();
            Check(bytes.Length > 0);
            bytes[0] ^= byte.MaxValue;
            Check(bytes[0] != read()[0]);
        }
        Reject(() => OtpInets.ReadImage(Traversal));
        Reject(() => OtpFtp.ReadImage(Unknown));
        Reject(() => OtpTftp.Beam(TftpLibrary, null!));
        Reject(() => OtpEldap.Beam(Eldap, null!));
        foreach (RuntimeModuleSelection selection in new[] { OtpInets.Beam(HttpUtility, [Lists]), OtpFtp.Beam(FtpResponse, [Lists]), OtpTftp.Beam(TftpLibrary, [Lists]), OtpEldap.Beam(Eldap, [Lists]) })
            Check(selection.Backend == RuntimeModuleBackend.Beam && selection.Dependencies.SequenceEqual([Lists]));
        Reject(() => OtpInets.Beam(HttpUtility, [Lists, Lists]));
        Reject(() => OtpFtp.Beam(FtpResponse, [string.Empty]));
        var runtime = new ProcessRuntime();
        try
        {
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                (registry, scope) =>
            {
                string[] imports = OtpInets.Modules.Concat(OtpFtp.Modules).Concat(OtpTftp.Modules).Concat(OtpEldap.Modules).Concat(OtpStdlib.Modules).Concat(OtpKernel.Modules).Concat([Observer, Internal, ErlangModule, Ssl, Asn1Runtime, Debugger]).ToArray();
                var dependencies = new BeamDependencySelection(imports);
                byte[][] bytes = [OtpStdlib.ReadImage(Lists), OtpStdlib.ReadImage(Calendar), OtpStdlib.ReadImage(IoLibrary), OtpStdlib.ReadImage(IoFormatter), OtpKernel.ReadImage(InternetParser), OtpStdlib.ReadImage(StringModule), OtpStdlib.ReadImage(Unicode), OtpStdlib.ReadImage(UnicodeUtilities), OtpStdlib.ReadImage(Sets), OtpStdlib.ReadImage(Proplists), OtpInets.ReadImage(HttpUtility), OtpInets.ReadImage(HttpServerUtility), OtpFtp.ReadImage(FtpResponse), OtpTftp.ReadImage(TftpLibrary), OtpEldap.ReadImage(Eldap), Resource(ObserverImage)];
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
        Console.WriteLine($"Original inets/ftp/tftp/eldap packaging: {passed} checks passed; 1337 complete outcomes/stacks match OTP");
    }
}
