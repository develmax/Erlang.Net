using Erlang;
using Erlang.Otp;
using static Erlang.Otp.Compiler.Library.Tests.CompilerLibraryNames;

namespace Erlang.Otp.Compiler.Library.Tests;

internal static class CompilerLibraryChecks
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
            using Stream input = typeof(Erlang.Otp.Compiler.Library.Tests.CompilerLibraryNames).Assembly.GetManifestResourceStream(Prefix + name)
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
        Check(OtpCompiler.Modules.Count == 59);
        Inspect(OtpCompiler.Modules, OtpCompiler.ReadImage);
        foreach (Func<byte[]> read in new Func<byte[]>[] { OtpCompiler.ReadApplication, OtpCompiler.ReadApplicationUpgrade })
        {
            byte[] bytes = read();
            Check(bytes.Length > 0);
            bytes[0] ^= byte.MaxValue;
            Check(bytes[0] != read()[0]);
        }
        Reject(() => OtpCompiler.ReadImage(Traversal));
        Reject(() => OtpCompiler.ReadImage(Unknown));
        Reject(() => OtpCompiler.Beam(SyntaxModule, null!));
        Reject(() => OtpCompiler.Beam(Parser, null!));
        RuntimeModuleSelection syntax = OtpCompiler.Beam(SyntaxModule, [Lists]);
        RuntimeModuleSelection parser = OtpCompiler.Beam(Parser, [Lists]);
        Check(syntax.Module == SyntaxModule && syntax.Backend == RuntimeModuleBackend.Beam && syntax.Dependencies.SequenceEqual([Lists]));
        Check(parser.Module == Parser && parser.Backend == RuntimeModuleBackend.Beam && parser.Dependencies.SequenceEqual([Lists]));
        Reject(() => OtpCompiler.Beam(SyntaxModule, [Lists, Lists]));
        Reject(() => OtpCompiler.Beam(Parser, [string.Empty]));
        var runtime = new ProcessRuntime();
        try
        {
            await runtime.InitializeModulesAsync(registry =>
            {
                string[] imports = OtpCompiler.Modules.Concat(OtpStdlib.Modules).Append(Observer).ToArray();
                var dependencies = new BeamDependencySelection(imports);
                byte[][] bytes = [OtpStdlib.ReadImage(Lists), OtpCompiler.ReadImage(SyntaxModule), OtpCompiler.ReadImage(Parser), Resource(ObserverImage)];
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
        Console.WriteLine($"Original compiler packaging: {passed} checks passed; 20 complete outcomes/stacks match OTP");
    }
}
