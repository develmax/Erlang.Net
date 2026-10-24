using Erlang;
using System.Reflection;
using static Erlang.Tests.FileNameEncoding396TestNames;

namespace Erlang.Tests;

internal static class FileNameEncoding395HostChecks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }

    internal static async Task Run(ReadOnlyMemory<byte> originalFileImage, IEnumerable<string> actualImportModules)
    {
        Type type = typeof(ProcessRuntime).Assembly.GetType(RuntimeType, true)!;
        MethodInfo capture = type.GetMethod(Capture, BindingFlags.Static | BindingFlags.NonPublic)!;
        foreach (var candidate in new[] { (FileModule, Function, 1), (FileModule, Other, 0), (Other, Function, 0) })
        {
            object?[] arguments = [candidate.Item1, candidate.Item2, candidate.Item3, null];
            Require(!(bool)capture.Invoke(null, arguments)! && arguments[3] is null);
        }
        await using var runtime = new ProcessRuntime();
        Require(!runtime.Modules.HasExport(FileModule, Function, 0));
        PreparedBeamImage image = runtime.Modules.PrepareBeam(originalFileImage, new BeamDependencySelection(actualImportModules));
        Require(image.Name == FileModule && image.NativePrimitiveExports.Count == 1);
        Require(image.NativePrimitiveExports[0] == new BeamFunctionSignature(Function, 0));
        // Preparing captures privately but does not publish a hand-written module.
        Require(!runtime.Modules.HasExport(FileModule, Function, 0));
        Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        Require(ReferenceEquals(runtime.Modules.SourceOwner(FileModule, Function, 0), image));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(context, FileModule, Function));
        Require(value.Equals(Term.A(Utf8)));
        Term again = await actor.InvokeAsync(async context => await runtime.Modules.Call(context, FileModule, Function));
        Require(ReferenceEquals(value, again));
        Term exports = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            FileModule,
            ModuleInfo,
            Term.A(Exports)
        ));
        Require(Cons.Items(exports).Count() == OriginalExports);
        Require(ReferenceEquals(runtime.Modules.SourceOwner(FileModule, ModuleInfo, 1), image));
    }
}
