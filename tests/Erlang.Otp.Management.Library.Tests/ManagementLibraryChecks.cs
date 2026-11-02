using Erlang;
using Erlang.Otp;
using static Erlang.Otp.Management.Library.Tests.ManagementLibraryNames;

namespace Erlang.Otp.Management.Library.Tests;

internal static class ManagementLibraryChecks
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
            using Stream input = typeof(Erlang.Otp.Management.Library.Tests.ManagementLibraryNames).Assembly.GetManifestResourceStream(Prefix + name)
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
        Check(OtpDiameter.Modules.Count == 47);
        Check(OtpSnmp.Modules.Count == 90);
        Inspect(OtpDiameter.Modules, OtpDiameter.ReadImage);
        Inspect(OtpSnmp.Modules, OtpSnmp.ReadImage);
        foreach (Func<byte[]> read in new Func<byte[]>[] { OtpDiameter.ReadApplication, OtpDiameter.ReadApplicationUpgrade, OtpSnmp.ReadApplication, OtpSnmp.ReadApplicationUpgrade })
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
        Check(OtpDiameter.SupportFiles.Count == 1);
        Check(OtpSnmp.SupportFiles.Count == 60);
        Support(OtpDiameter.SupportFiles, OtpDiameter.ReadSupportFile);
        Support(OtpSnmp.SupportFiles, OtpSnmp.ReadSupportFile);
        Reject(() => OtpDiameter.ReadImage(Traversal));
        Reject(() => OtpSnmp.ReadImage(Unknown));
        Reject(() => OtpDiameter.Beam(DiameterTypes, null!));
        Reject(() => OtpSnmp.Beam(SnmpPdus, null!));
        foreach (RuntimeModuleSelection selection in new[] { OtpDiameter.Beam(DiameterTypes, [Lists]), OtpSnmp.Beam(SnmpPdus, [Lists]) })
            Check(selection.Backend == RuntimeModuleBackend.Beam && selection.Dependencies.SequenceEqual([Lists]));
        Reject(() => OtpDiameter.Beam(DiameterTypes, [Lists, Lists]));
        Reject(() => OtpSnmp.Beam(SnmpPdus, [string.Empty]));
        var runtime = new ProcessRuntime();
        try
        {
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                (registry, scope) =>
            {
                string[] imports = OtpDiameter.Modules.Concat(OtpSnmp.Modules).Concat(OtpStdlib.Modules).Concat(OtpKernel.Modules).Concat([Observer, Internal, ErlangModule, Crypto, Debugger, Asn1Runtime, PrimitiveSocket, PersistentTerm, PrimitiveInternet]).ToArray();
                var dependencies = new BeamDependencySelection(imports);
                byte[][] bytes = [OtpStdlib.ReadImage(Lists), OtpStdlib.ReadImage(Calendar), OtpStdlib.ReadImage(IoLibrary), OtpStdlib.ReadImage(IoFormatter), OtpStdlib.ReadImage(StringModule), OtpStdlib.ReadImage(Unicode), OtpStdlib.ReadImage(UnicodeUtilities), OtpStdlib.ReadImage(Proplists), OtpKernel.ReadImage(InternetParser), OtpKernel.ReadImage(Internet), OtpDiameter.ReadImage(DiameterTypes), OtpDiameter.ReadImage(DiameterLibrary), OtpSnmp.ReadImage(SnmpPdus), OtpSnmp.ReadImage(SnmpBasics), OtpSnmp.ReadImage(SnmpMisc), Resource(ObserverImage)];
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
        Console.WriteLine($"Original diameter/snmp packaging: {passed} checks passed; 768 complete outcomes/stacks match OTP");
    }
}
