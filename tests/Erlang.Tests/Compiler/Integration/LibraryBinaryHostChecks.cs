using System.Security.Cryptography;
using Erlang;
using static Erlang.Tests.LibraryBinaryNames;

namespace Erlang.Tests;

internal static class LibraryBinaryHostChecks
{
    internal static async Task Run()
    {
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([
            RuntimeModuleSelection.Beam(Module, Read(Module + BeamExtension), [Lists]),
            RuntimeModuleSelection.Beam(Lists, Read(Lists + BeamExtension))
        ]));
        foreach (RuntimeLibraryModuleSnapshot selected in runtime.LibrarySelection.Modules)
        {
            Require(selected.Backend == RuntimeModuleBackend.Beam && selected.InputSha256 == Digests[selected.Module + BeamExtension]);
            Require(ReferenceEquals(selected.Image, runtime.Modules.CurrentCodeImage(selected.Module)) && !runtime.Modules.HasOldCode(selected.Module));
            foreach (BeamFunctionSignature export in selected.Exports)
                Require(runtime.Modules.SourceOwner(selected.Module, export.Name, export.Arity) is PreparedBeamImage image && image.Name == selected.Module);
        }
        Require(runtime.LibrarySelection.Modules.Single(module => module.Module == Module).NativePrimitives.Count == NativePrimitiveCount);
        PreparedBeamImage observer = runtime.Modules.PrepareBeam(Read(Observer + BeamExtension), new BeamDependencySelection([Module, Lists, ErlangModuleNames.Module]));
        Require(runtime.Modules.PublishPreparedBeam(observer).Status == BeamPublicationStatus.Published);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach ((string entry, string golden) in new[] { (RunEntry, ValuesFile), (Stacks, StacksFile) })
            {
                Term actual = await runtime.Modules.Call(context, Observer, entry);
                byte[] original = Read(golden);
                byte[] encoded = ExternalTermFormat.Encode(actual, new EtfEncodingOptions { MinorVersion = 2, FunctionOwner = runtime.InitializationPid });
                Require(encoded.AsSpan().SequenceEqual(original) && actual.Equals(runtime.ImportExternalTerm(original).Term));
            }

            return true;
        });
    }

    private static byte[] Read(string name)
    {
        using Stream stream = typeof(LibraryBinaryHostChecks).Assembly.GetManifestResourceStream(Prefix + name)!;
        using var output = new MemoryStream();
        stream.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == Digests[name]);

        return bytes;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
