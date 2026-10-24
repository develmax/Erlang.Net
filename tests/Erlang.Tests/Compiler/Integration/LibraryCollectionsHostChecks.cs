using System.Security.Cryptography;
using Erlang;
using static Erlang.Tests.LibraryCollectionsNames;

namespace Erlang.Tests;

internal static class LibraryCollectionsHostChecks
{
    internal static async Task Run()
    {
        var selections = Modules.Select(module => RuntimeModuleSelection.Beam(module, Read(module + BeamExtension), Dependencies[module]));
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection(selections));
        foreach (RuntimeLibraryModuleSnapshot selected in runtime.LibrarySelection.Modules)
        {
            Require(selected.Backend == RuntimeModuleBackend.Beam && selected.InputSha256 == Digests[selected.Module + BeamExtension]);
            Require(ReferenceEquals(selected.Image, runtime.Modules.CurrentCodeImage(selected.Module)) && !runtime.Modules.HasOldCode(selected.Module));
            foreach (BeamFunctionSignature export in selected.Exports)
                Require(runtime.Modules.SourceOwner(selected.Module, export.Name, export.Arity) is PreparedBeamImage image && image.Name == selected.Module);
        }
        PreparedBeamImage observer = runtime.Modules.PrepareBeam(Read(Observer + BeamExtension),
            new BeamDependencySelection(Modules.Concat([ErlangModuleNames.Module, MathModule])));
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

    internal static async Task InvalidSelections()
    {
        await using var runtime = new ProcessRuntime();
        foreach (string module in BeamOnly)
        {
            Require(!runtime.Modules.IsModuleLoaded(module));
            bool refused = false;
            try
            {
                _ = RuntimeModuleSelection.Managed(module);
            }
            catch (ArgumentException)
            {
                refused = true;
            }
            Require(refused);
        }
        bool missing = false;
        try
        {
            _ = new RuntimeLibrarySelection([RuntimeModuleSelection.Beam(OrderedDictionary,
                Read(OrderedDictionary + BeamExtension), [ListBeamHostNames.Module])]);
        }
        catch (ArgumentException)
        {
            missing = true;
        }
        Require(missing && !runtime.Modules.IsModuleLoaded(OrderedDictionary));
    }

    internal static byte[] Read(string name)
    {
        using Stream stream = typeof(LibraryCollectionsHostChecks).Assembly.GetManifestResourceStream(Prefix + name)!;
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
