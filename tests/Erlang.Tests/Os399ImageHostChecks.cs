using Erlang;
using System.Security.Cryptography;
using static Erlang.Tests.Os399HostAccess;

namespace Erlang.Tests;

internal static class Os399ImageHostChecks
{
    private static readonly string[] Imports = ["application", "binary", "erlang", "file", "filename", "lists", "os", "persistent_term", "string", "unicode"];

    internal static async Task Run()
    {
        if (!OperatingSystem.IsWindows()) return;
        using Stream resource = typeof(Os399ImageHostChecks).Assembly.GetManifestResourceStream(Os399HostNames.Resource)!;
        using var buffer = new MemoryStream();
        await resource.CopyToAsync(buffer);
        byte[] bytes = buffer.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == Os399HostNames.ImageSha);
        await using var runtime = new ProcessRuntime();
        var before = LibrarySelectionPolicyNegativeAccess.Bodies(runtime.Modules).ToDictionary(x => x.Key, x => x.Value);
        var moduleNames = runtime.Modules.LoadedModules.Order(StringComparer.Ordinal).ToArray();
        // Import declaration failure must not expose a prefix of the native image.
        try
        {
            runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection([]));
            throw new InvalidOperationException(Os399HostNames.Invariant);
        }
        catch (NotSupportedException) { }
        Require(before.Count == LibrarySelectionPolicyNegativeAccess.Bodies(runtime.Modules).Count);
        Require(moduleNames.SequenceEqual(runtime.Modules.LoadedModules.Order(StringComparer.Ordinal)));
        foreach (var entry in before) Require(ReferenceEquals(LibrarySelectionPolicyNegativeAccess.Bodies(runtime.Modules)[entry.Key], entry.Value));
        PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection(Imports));
        Require(image.NativePrimitiveExports.Count == 10 && !runtime.Modules.HasExport("os", "env", 0));
        Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        foreach (var entry in image.NativePrimitiveExports) Require(ReferenceEquals(runtime.Modules.SourceOwner("os", entry.Name, entry.Arity), image));
        Require(ReferenceEquals(runtime.Modules.SourceOwner("os", "module_info", 1), image));
        Require(LibrarySelectionPolicyNegativeAccess.Captured(image).Count == 10);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Require((await actor.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            "os",
            "module_info",
            Term.A("md5")
        ))).Equals(image.NativeMd5));
        await using var blocked = new ProcessRuntime();
        ErlangFunction foreign = (_, _) => ValueTask.FromResult<Term>(Nil.Value);
        blocked.Modules.Register(
            "os",
            "getenv",
            1,
            foreign
        );
        before = LibrarySelectionPolicyNegativeAccess.Bodies(blocked.Modules).ToDictionary(x => x.Key, x => x.Value);
        moduleNames = blocked.Modules.LoadedModules.Order(StringComparer.Ordinal).ToArray();
        object? oldImage = blocked.Modules.CurrentCodeImage("os");
        try
        {
            blocked.Modules.PrepareBeam(bytes, new BeamDependencySelection(Imports));
            throw new InvalidOperationException(Os399HostNames.Invariant);
        }
        catch (NotSupportedException) { }
        Require(before.Count == LibrarySelectionPolicyNegativeAccess.Bodies(blocked.Modules).Count);
        foreach (var entry in before) Require(ReferenceEquals(LibrarySelectionPolicyNegativeAccess.Bodies(blocked.Modules)[entry.Key], entry.Value));
        Require(moduleNames.SequenceEqual(blocked.Modules.LoadedModules.Order(StringComparer.Ordinal)));
        Require(ReferenceEquals(oldImage, blocked.Modules.CurrentCodeImage("os")) && !blocked.Modules.HasExport("os", "env", 0));
    }
}
