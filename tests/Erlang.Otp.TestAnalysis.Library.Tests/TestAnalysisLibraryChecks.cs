using Erlang;
using Erlang.Otp;
using static Erlang.Otp.TestAnalysis.Library.Tests.TestAnalysisLibraryNames;

namespace Erlang.Otp.TestAnalysis.Library.Tests;

internal static class TestAnalysisLibraryChecks
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
            using Stream input = typeof(Erlang.Otp.TestAnalysis.Library.Tests.TestAnalysisLibraryNames).Assembly.GetManifestResourceStream(Prefix + name)
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
        Check(OtpCommonTest.Modules.Count == 49);
        Check(OtpDialyzer.Modules.Count == 29);
        Inspect(OtpCommonTest.Modules, OtpCommonTest.ReadImage);
        Inspect(OtpDialyzer.Modules, OtpDialyzer.ReadImage);
        foreach (Func<byte[]> read in new Func<byte[]>[] { OtpCommonTest.ReadApplication, OtpCommonTest.ReadApplicationUpgrade, OtpDialyzer.ReadApplication, OtpDialyzer.ReadApplicationUpgrade })
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
        Check(OtpCommonTest.SupportFiles.Count == 5);
        Check(OtpDialyzer.SupportFiles.Count == 0);
        Support(OtpCommonTest.SupportFiles, OtpCommonTest.ReadSupportFile);
        Support(OtpDialyzer.SupportFiles, OtpDialyzer.ReadSupportFile);
        Reject(() => OtpCommonTest.ReadImage(Traversal));
        Reject(() => OtpDialyzer.ReadImage(Unknown));
        Reject(() => OtpCommonTest.Beam(CommonTestGroups, null!));
        Reject(() => OtpDialyzer.Beam(DialyzerOptions, null!));
        foreach (RuntimeModuleSelection selection in new[] { OtpCommonTest.Beam(CommonTestGroups, [Lists]), OtpDialyzer.Beam(DialyzerOptions, [Lists]) })
            Check(selection.Backend == RuntimeModuleBackend.Beam && selection.Dependencies.SequenceEqual([Lists]));
        Reject(() => OtpCommonTest.Beam(CommonTestGroups, [Lists, Lists]));
        Reject(() => OtpDialyzer.Beam(DialyzerOptions, [string.Empty]));
        var runtime = new ProcessRuntime();
        try
        {
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                async (registry, scope) =>
            {
                await runtime.InitializeModulesAsync(_ =>
                {
                    {
                        RuntimeNativePrimitiveProviders.Register(runtime, registry, RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.PersistentTermCore);
                    }
                    {
                        RuntimeBootstrapFileSystem.Register(runtime, registry);
                    }
                });
                string[] imports = OtpCommonTest.Modules.Concat(OtpDialyzer.Modules).Concat(OtpStdlib.Modules).Concat(OtpKernel.Modules).Concat(OtpCompiler.Modules).Concat(OtpErts.Modules).Concat([Observer, Internal, Initialization, ErlangModule, Crypto, PrettyPrinter]).ToArray();
                var dependencies = new BeamDependencySelection(imports);
                byte[][] bytes = [OtpStdlib.ReadImage(Lists), OtpStdlib.ReadImage(OrderedSets), OtpStdlib.ReadImage(Proplists), OtpStdlib.ReadImage(IoLibrary), OtpStdlib.ReadImage(IoFormatter), OtpStdlib.ReadImage(IoPrettyPrinter), OtpStdlib.ReadImage(StringModule), OtpStdlib.ReadImage(Unicode), OtpStdlib.ReadImage(UnicodeUtilities), OtpStdlib.ReadImage(Filename), OtpStdlib.ReadImage(Preprocessor), OtpStdlib.ReadImage(Scanner), OtpStdlib.ReadImage(InputOutput), OtpStdlib.ReadImage(Sets), OtpStdlib.ReadImage(SetFamilies), OtpCompiler.ReadImage(Core), OtpCommonTest.ReadImage(CommonTestConfig), OtpCommonTest.ReadImage(CommonTestGroups), OtpDialyzer.ReadImage(DialyzerOptions), OtpDialyzer.ReadImage(DialyzerUtilities), Resource(ObserverImage)];
                bytes = bytes.Concat(TestAnalysisLibraryBoot.Images()).ToArray();
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

                return _ =>
                {
                    Check(registry.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
                    foreach (PreparedBeamImage image in images)
                    {
                        var exports = registry.Exports.Where(export => export.Module == image.Name).ToArray();
                        Check(exports.Length > 0 && exports.All(export => ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image)));
                    }
                };
            }
            );
            await TestAnalysisLibraryBoot.Start(runtime, Check);
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
        Console.WriteLine($"Original common_test/dialyzer packaging: {passed} checks passed; 2463 complete outcomes/stacks match OTP");
    }
}
