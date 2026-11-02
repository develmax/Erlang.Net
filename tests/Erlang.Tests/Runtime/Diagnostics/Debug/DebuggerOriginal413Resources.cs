using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class DebuggerOriginal413Resources
{
    private static byte[] Raw(string name)
    {
        using Stream stream = typeof(DebuggerOriginal413Resources).Assembly.GetManifestResourceStream(
            DebuggerOriginal413Names.ResourcePrefix + name) ?? throw new InvalidDataException(DebuggerOriginal413Names.Failure);
        using var result = new MemoryStream();
        stream.CopyTo(result);

        return result.ToArray();
    }

    internal static byte[] Read(string name)
    {
        var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(Raw(DebuggerOriginal413Names.Manifest))!;
        byte[] bytes = Raw(name);
        if (!manifest.TryGetValue(name, out string? expected) || Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(DebuggerOriginal413Names.Failure);

        return bytes;
    }

    internal static async Task<ProcessRuntime> Start(LibraryDependencyOriginalSuite suite)
    {
        var runtime = new ProcessRuntime();
        try
        {
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                async (registry, scope) =>
            {
                await runtime.InitializeModulesAsync(_ =>
                {
                    Erlang.Otp.OtpModules.Register(registry);
                });
                string[] modules = [DebuggerOriginal413Names.Module, DebuggerOriginal413Names.Observer, DebuggerOriginal413Names.Subject];
                var dependencies = new BeamDependencySelection(modules.Concat([ErlangModuleNames.Module]));
                PreparedBeamImage[] images = modules.Select(name => scope.Prepare(() => registry.PrepareBeam(Read(name + ".beam"), dependencies))).ToArray();

                return _ =>
                {
                    if (images[0].NativePrimitiveExports.Count != DebuggerOriginal413Names.NativeCount
                    || images.Skip(1).Any(image => image.NativePrimitiveExports.Count != 0)
                    || registry.PublishPreparedBeam(images).Status != BeamPublicationStatus.Published)
                        throw new InvalidOperationException(DebuggerOriginal413Names.Failure);
                    foreach (var image in images)
                        if (registry.Exports.Where(export => export.Module == image.Name).Any(export =>
                        !ReferenceEquals(registry.SourceOwner(image.Name, export.Function, export.Arity), image)))
                            throw new InvalidOperationException(DebuggerOriginal413Names.Failure);
                };
            }
            );

            return runtime;
        }
        catch
        {
            await runtime.DisposeAsync();
            throw;
        }
    }
}
