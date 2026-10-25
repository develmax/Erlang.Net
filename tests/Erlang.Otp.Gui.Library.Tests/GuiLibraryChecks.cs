using Erlang;
using Erlang.Otp;
using static Erlang.Otp.Gui.Library.Tests.GuiLibraryNames;

namespace Erlang.Otp.Gui.Library.Tests;

internal static class GuiLibraryChecks
{
    internal static async Task Run(string[] args)
    {
        await RunCorpus(args);
        await GuiFloatStore479Checks.Run(args);
        await GuiClosedWait480Checks.Run(args);
    }

    private static async Task RunCorpus(string[] args)
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
            using Stream input = typeof(Erlang.Otp.Gui.Library.Tests.GuiLibraryNames).Assembly.GetManifestResourceStream(Prefix + name)
                ?? throw new InvalidDataException(MissingResource);
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }

        GuiLibraryAsset[] libraries =
        [
            new(
                OtpDebugger.Modules,
                OtpDebugger.ReadImage,
                OtpDebugger.ReadApplication,
                OtpDebugger.ReadApplicationUpgrade,
                OtpDebugger.SupportFiles,
                OtpDebugger.ReadSupportFile,
                OtpDebugger.Beam,
                24,
                1
            ),
            new(
                OtpEt.Modules,
                OtpEt.ReadImage,
                OtpEt.ReadApplication,
                OtpEt.ReadApplicationUpgrade,
                OtpEt.SupportFiles,
                OtpEt.ReadSupportFile,
                OtpEt.Beam,
                6,
                0
            ),
            new(
                OtpObserver.Modules,
                OtpObserver.ReadImage,
                OtpObserver.ReadApplication,
                OtpObserver.ReadApplicationUpgrade,
                OtpObserver.SupportFiles,
                OtpObserver.ReadSupportFile,
                OtpObserver.Beam,
                44,
                5
            ),
            new(
                OtpReltool.Modules,
                OtpReltool.ReadImage,
                OtpReltool.ReadApplication,
                OtpReltool.ReadApplicationUpgrade,
                OtpReltool.SupportFiles,
                OtpReltool.ReadSupportFile,
                OtpReltool.Beam,
                9,
                0
            ),
            new(
                OtpWx.Modules,
                OtpWx.ReadImage,
                OtpWx.ReadApplication,
                OtpWx.ReadApplicationUpgrade,
                OtpWx.SupportFiles,
                OtpWx.ReadSupportFile,
                OtpWx.Beam,
                241,
                3
            )
        ];
        foreach (GuiLibraryAsset library in libraries)
        {
            Check(library.Modules.Count == library.ExpectedModules);
            foreach (string module in library.Modules)
            {
                byte[] image = library.ReadImage(module);
                BeamArtifactInfo info = BeamArtifactInspector.Inspect(image);
                Check(info.Name == module);
                image[0] ^= byte.MaxValue;
                Check(BeamArtifactInspector.Inspect(library.ReadImage(module)) == info);
            }
            foreach (Func<byte[]> read in new[] { library.ReadApplication, library.ReadUpgrade })
            {
                byte[] bytes = read();
                Check(bytes.Length > 0);
                bytes[0] ^= byte.MaxValue;
                Check(bytes[0] != read()[0]);
            }
            Check(library.SupportFiles.Count == library.ExpectedSupport);
            foreach (string name in library.SupportFiles)
            {
                byte[] bytes = library.ReadSupport(name);
                Check(bytes.AsSpan().SequenceEqual(library.ReadSupport(name)));
                if (bytes.Length > 0)
                    bytes[0] ^= byte.MaxValue;
                Check(bytes.Length == 0 || bytes[0] != library.ReadSupport(name)[0]);
            }
            Reject(() => library.ReadSupport(Traversal));
            Reject(() => library.ReadSupport(Unknown));
            Reject(() => library.ReadImage(Traversal));
            Reject(() => library.ReadImage(Unknown));
            Reject(() => library.Beam(library.Modules[0], null!));
            Reject(() => library.Beam(library.Modules[0], [Lists, Lists]));
            RuntimeModuleSelection selected = library.Beam(library.Modules[0], [Lists]);
            Check(selected.Backend == RuntimeModuleBackend.Beam && selected.Dependencies.SequenceEqual([Lists]));
        }
        var runtime = new ProcessRuntime();
        try
        {
            await runtime.InitializeModulesAsync(registry =>
            {
                string[] imports = libraries.SelectMany(library => library.Modules).Concat(OtpStdlib.Modules).Concat(GuiLibraryNames.OriginalImports).Append(Observer).Distinct().ToArray();
                var dependencies = new BeamDependencySelection(imports);
                byte[][] bytes = GuiLibraryNames.WxClasses.Select(module => OtpWx.ReadImage(module))
                    .Concat([OtpDebugger.ReadImage(GuiLibraryNames.Helpers[0]), OtpEt.ReadImage(GuiLibraryNames.Helpers[1]), OtpObserver.ReadImage(GuiLibraryNames.Helpers[2]), OtpReltool.ReadImage(GuiLibraryNames.Helpers[3])])
                    .Concat(GuiLibraryNames.StdlibImages.Select(module => OtpStdlib.ReadImage(module))).Append(Resource(ObserverImage)).ToArray();
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
                string output = args[0];
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
        Console.WriteLine($"Original debugger/et/observer/reltool/wx packaging: {passed} checks passed; 1415 complete outcomes/stacks match OTP");
    }
}
