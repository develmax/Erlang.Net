using Erlang;
using Erlang.Otp;
using static Erlang.Otp.Documentation.Library.Tests.DocumentationLibraryNames;

namespace Erlang.Otp.Documentation.Library.Tests;

internal static class DocumentationLibraryChecks
{
    internal static async Task Run(string[] args)
    {
        int passed = 0;

        void Check(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException($"{Failure}: {passed + 1}");
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
            using Stream input = typeof(Erlang.Otp.Documentation.Library.Tests.DocumentationLibraryNames).Assembly.GetManifestResourceStream(Prefix + name)
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
        Check(OtpEunit.Modules.Count == 13);
        Check(OtpEdoc.Modules.Count == 21);
        Check(OtpXmerl.Modules.Count == 35);
        Inspect(OtpEunit.Modules, OtpEunit.ReadImage);
        Inspect(OtpEdoc.Modules, OtpEdoc.ReadImage);
        Inspect(OtpXmerl.Modules, OtpXmerl.ReadImage);
        foreach (Func<byte[]> read in new Func<byte[]>[] { OtpEunit.ReadApplication, OtpEdoc.ReadApplication, OtpEdoc.ReadApplicationUpgrade, OtpEunit.ReadApplicationUpgrade, OtpXmerl.ReadApplication, OtpXmerl.ReadApplicationUpgrade })
        {
            byte[] bytes = read();
            Check(bytes.Length > 0);
            bytes[0] ^= byte.MaxValue;
            Check(bytes[0] != read()[0]);
        }
        Reject(() => OtpEunit.ReadImage(Traversal));
        Reject(() => OtpEdoc.ReadImage(Unknown));
        Reject(() => OtpEunit.Beam(EunitUtilities, null!));
        Reject(() => OtpEdoc.Beam(EdocUtilities, null!));
        RuntimeModuleSelection eunit = OtpEunit.Beam(EunitUtilities, [Lists]);
        RuntimeModuleSelection edoc = OtpEdoc.Beam(EdocUtilities, [Trees, Lists]);
        Check(eunit.Module == EunitUtilities && eunit.Backend == RuntimeModuleBackend.Beam && eunit.Dependencies.SequenceEqual([Lists]));
        Check(edoc.Module == EdocUtilities && edoc.Backend == RuntimeModuleBackend.Beam && edoc.Dependencies.SequenceEqual([Trees, Lists]));
        Reject(() => OtpEunit.Beam(EunitUtilities, [Lists, Lists]));
        Reject(() => OtpEdoc.Beam(EdocUtilities, [string.Empty]));
        Check(OtpEdoc.SupportFiles.Count == 4);
        foreach (string name in OtpEdoc.SupportFiles)
        {
            byte[] bytes = OtpEdoc.ReadSupportFile(name);
            Check(bytes.Length > 0);
            bytes[0] ^= byte.MaxValue;
            Check(bytes[0] != OtpEdoc.ReadSupportFile(name)[0]);
        }
        Reject(() => OtpEdoc.ReadSupportFile(Traversal));
        Reject(() => OtpXmerl.Beam(XmlUtilities, null!));
        RuntimeModuleSelection xml = OtpXmerl.Beam(XmlUtilities, [Lists]);
        Check(xml.Module == XmlUtilities && xml.Backend == RuntimeModuleBackend.Beam && xml.Dependencies.SequenceEqual([Lists]));
        var runtime = new ProcessRuntime();
        try
        {
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                (registry, scope) =>
            {
                string[] imports = OtpEunit.Modules.Concat(OtpEdoc.Modules).Concat(OtpXmerl.Modules).Concat(OtpStdlib.Modules).Concat(OtpKernel.Modules).Concat(OtpErts.Modules).Append(Observer).ToArray();
                var dependencies = new BeamDependencySelection(imports);
                byte[][] bytes = [OtpStdlib.ReadImage(Lists), OtpStdlib.ReadImage(Trees), OtpEunit.ReadImage(EunitUtilities), OtpEdoc.ReadImage(EdocUtilities), OtpXmerl.ReadImage(XmlUtilities), Resource(ObserverImage)];
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
        Console.WriteLine($"Original eunit/edoc/xmerl packaging: {passed} checks passed; 30 complete outcomes/stacks match OTP");
    }
}
