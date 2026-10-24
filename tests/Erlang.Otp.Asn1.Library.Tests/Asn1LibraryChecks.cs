using Erlang;
using Erlang.Otp;
using static Erlang.Otp.Asn1.Library.Tests.Asn1LibraryNames;

namespace Erlang.Otp.Asn1.Library.Tests;

internal static class Asn1LibraryChecks
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
            using Stream input = typeof(Erlang.Otp.Asn1.Library.Tests.Asn1LibraryNames).Assembly.GetManifestResourceStream(Prefix + name)
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
        Check(OtpAsn1.Modules.Count == 22);
        Inspect(OtpAsn1.Modules, OtpAsn1.ReadImage);
        foreach (Func<byte[]> read in new Func<byte[]>[] { OtpAsn1.ReadApplication, OtpAsn1.ReadApplicationUpgrade })
        {
            byte[] bytes = read();
            Check(bytes.Length > 0);
            bytes[0] ^= byte.MaxValue;
            Check(bytes[0] != read()[0]);
        }
        Reject(() => OtpAsn1.ReadImage(Traversal));
        Reject(() => OtpAsn1.ReadImage(Unknown));
        Reject(() => OtpAsn1.Beam(ExternalTransform, null!));
        Reject(() => OtpAsn1.Beam(CompilerTable, null!));
        RuntimeModuleSelection fragment = OtpAsn1.Beam(ExternalTransform, [Lists]);
        RuntimeModuleSelection application = OtpAsn1.Beam(CompilerTable, [Lists]);
        Check(fragment.Module == ExternalTransform && fragment.Backend == RuntimeModuleBackend.Beam && fragment.Dependencies.SequenceEqual([Lists]));
        Check(application.Module == CompilerTable && application.Backend == RuntimeModuleBackend.Beam && application.Dependencies.SequenceEqual([Lists]));
        Reject(() => OtpAsn1.Beam(ExternalTransform, [Lists, Lists]));
        Reject(() => OtpAsn1.Beam(CompilerTable, [string.Empty]));
        var runtime = new ProcessRuntime();
        try
        {
            await runtime.InitializeModulesAsync(registry =>
            {
                RuntimeNativePrimitiveProviders.Register(runtime, registry, RuntimeNativePrimitives.EtsStorageCore);
                RuntimeBootstrapEtsDeletion.Register(runtime, registry);
                RuntimeBootstrapEtsQueries414.Register(runtime, registry);
                RuntimeBootstrapEtsMutations414.Register(runtime, registry);
                RuntimeBootstrapEtsTraversal416.Register(runtime, registry);
                RuntimeBootstrapEtsCounter417.Register(runtime, registry);
                RuntimeBootstrapEtsRename418.Register(runtime, registry);
                RuntimeBootstrapEtsHeir419.Register(runtime, registry);
                RuntimeBootstrapEtsCompiled419.Register(runtime, registry);
                RuntimeBootstrapEtsAll420.Register(runtime, registry);
                RuntimeBootstrapEtsFixation420.Register(runtime, registry);
                RuntimeBootstrapEtsCursor420.Register(runtime, registry);
                RuntimeBootstrapEtsReplace420.Register(runtime, registry);
                RuntimeBootstrapEtsInspection420.Register(runtime, registry);
                RuntimeBootstrapEtsInfo420.Register(runtime, registry);
                string[] imports = OtpAsn1.Modules.Concat(OtpStdlib.Modules).Concat([Observer, Internal, ErlangModule, FileModule, DiskLog]).ToArray();
                var dependencies = new BeamDependencySelection(imports);
                byte[][] bytes = [OtpStdlib.ReadImage(Lists), OtpStdlib.ReadImage(Ets), OtpAsn1.ReadImage(ExternalTransform), OtpAsn1.ReadImage(CompilerTable), Resource(ObserverImage)];
                PreparedBeamImage[] images = bytes.Select(image =>
                {
                    try
                    {
                        return registry.PrepareBeam(image, dependencies);
                    }
                    catch (Exception error)
                    {
                        throw new InvalidOperationException(BeamArtifactInspector.Inspect(image).Name, error);
                    }
                }).ToArray();
                Check(registry.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
                foreach (PreparedBeamImage image in images)
                {
                    var exports = registry.Exports.Where(export => export.Module == image.Name).ToArray();
                    Check(exports.Length > 0 && exports.All(export => ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image)));
                }
            });
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
        Console.WriteLine($"Original Asn1 packaging: {passed} checks passed; 26 complete outcomes/stacks match OTP");
    }
}
