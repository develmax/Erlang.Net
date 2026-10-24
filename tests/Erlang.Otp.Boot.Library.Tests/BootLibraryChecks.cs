using Erlang;
using Erlang.Otp;
using static Erlang.Otp.Boot.Library.Tests.BootLibraryNames;

namespace Erlang.Otp.Boot.Library.Tests;

internal static class BootLibraryChecks
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
            using Stream input = typeof(Erlang.Otp.Boot.Library.Tests.BootLibraryNames).Assembly.GetManifestResourceStream(Prefix + name)
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
        Check(OtpErts.Modules.Count == 22);
        Check(OtpSasl.Modules.Count == 17);
        Inspect(OtpErts.Modules, OtpErts.ReadImage);
        Inspect(OtpSasl.Modules, OtpSasl.ReadImage);
        foreach (Func<byte[]> read in new Func<byte[]>[] { OtpErts.ReadApplication, OtpSasl.ReadApplication, OtpSasl.ReadApplicationUpgrade })
        {
            byte[] bytes = read();
            Check(bytes.Length > 0);
            bytes[0] ^= byte.MaxValue;
            Check(bytes[0] != read()[0]);
        }
        Reject(() => OtpErts.ReadImage("../erlang"));
        Reject(() => OtpSasl.ReadImage("missing"));
        Reject(() => OtpErts.Beam("counters", null!));
        Reject(() => OtpSasl.Beam("misc_supp", null!));
        var runtime = new ProcessRuntime();
        try
        {
            await runtime.InitializeModulesAsync(registry =>
            {
                RuntimeBootstrapAtomics.Register(runtime, registry);
                RuntimeBootstrapFileSystem.Register(runtime, registry);
                RuntimeBootstrapScheduler.Register(runtime, registry);
                RuntimeBootstrapInternalPorts416.Register(runtime, registry);
                RuntimeBootstrapEtsInspection420.Register(runtime, registry);
                string[] imports = OtpErts.Modules.Concat(OtpSasl.Modules).Concat(OtpStdlib.Modules).Concat(OtpKernel.Modules).Append(Observer).ToArray();
                var dependencies = new BeamDependencySelection(imports);
                byte[][] bytes = [OtpStdlib.ReadImage("lists"), OtpErts.ReadImage("atomics"), OtpErts.ReadImage("counters"), OtpErts.ReadImage("erts_internal"), OtpSasl.ReadImage("misc_supp"), OtpSasl.ReadImage("systools_lib"), Resource(ObserverImage)];
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
                File.WriteAllText(Path.Combine(args[0], "managed-values.txt"), outcome.ToString());
            }
            Check(outcome.Equals(expected));
            Check(ExternalTermFormat.Encode(outcome).AsSpan().SequenceEqual(original));
            Check(runtime.AtomicsStorage413.Created > 0);
            Check(runtime.CountersStorage421.Created > 0);
        }
        finally
        {
            await runtime.DisposeAsync();
        }
        Check(runtime.AtomicsStorage413.Closed && runtime.AtomicsStorage413.LiveArrays == 0 && runtime.AtomicsStorage413.OwnedNativeBytes == 0 && runtime.AtomicsStorage413.ActiveInvocations == 0);
        Check(runtime.CountersStorage421.Closed && runtime.CountersStorage421.LiveArrays == 0 && runtime.CountersStorage421.OwnedNativeBytes == 0 && runtime.CountersStorage421.ActiveInvocations == 0);
        Console.WriteLine($"Original ERTS/SASL packaging: {passed} checks passed; 19 complete outcomes/stacks match OTP; cleanup passed");
    }
}
