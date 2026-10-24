namespace Erlang.Tests;

internal static class BeamLoaderDependencies410Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(BeamLoaderDependencies410Names.Failure);
    }

    internal static ProcessRuntime Runtime()
    {
        var runtime = new ProcessRuntime(1);
        // Test-owned pure dependency, deliberately NOT a NativeBuiltinCatalog MFA.
        runtime.Modules.Register(
            BeamLoaderDependencies410Names.DependencyModule,
            BeamLoaderDependencies410Names.Answer,
            0,
            (_, _) => ValueTask.FromResult<Term>(Term.I(BeamLoaderDependencies410Names.Expected))
        );

        return runtime;
    }

    internal static PreparedBeamImage Caller(ProcessRuntime runtime, string module, bool declared)
    {
        PreparedBeamImage image = runtime.Modules.PrepareBeam(
            BeamLoaderDependencies410Bytes.Caller(module),
            new BeamDependencySelection(declared ? [BeamLoaderDependencies410Names.DependencyModule] : [])
        );
        Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);

        return image;
    }

    internal static ModuleCodeImage Code(ProcessRuntime runtime, PreparedBeamImage image)
        => runtime.Modules.CurrentCodeImage(image.Name)!;

    internal static int ResourceCount(ProcessRuntime runtime)
    {
        object resources = runtime.ReferenceResources;
        object gate = resources.GetType().GetField(BeamLoaderDependencies410Names.Gate,
            BeamLoaderDependencies410Names.Members)!.GetValue(resources)!;
        lock (gate)
            return ((System.Collections.IDictionary)resources.GetType().GetField(BeamLoaderDependencies410Names.Entries,
                BeamLoaderDependencies410Names.Members)!.GetValue(resources)!).Count;
    }

    internal static ValueTask<Term> Native(ProcessContext context, string function, params Term[] arguments)
        => context.Runtime.Modules.Call(
            context,
            BeamLoaderDependencies410Names.Erlang,
            function,
            arguments
        );

    internal static ValueTask<Term> Prepare(ProcessContext context)
        => Native(
            context,
            BeamLoaderDependencies410Names.Prepare,
            Term.A(BeamLoaderDependencies410Names.TargetModule),
            new BitString(BeamLoaderDependencies410Bytes.Target())
        );

    internal static async Task Refuses(ProcessContext context)
    {
        var runtime = context.Runtime;
        var exports = runtime.Modules.Exports.ToHashSet();
        int resources = ResourceCount(runtime);
        bool refused = false;
        try
        {
            _ = await Prepare(context);
        }
        catch (NotSupportedException)
        {
            refused = true;
        }
        Require(refused && exports.SetEquals(runtime.Modules.Exports) && ResourceCount(runtime) == resources
            && !runtime.Modules.HasExport(BeamLoaderDependencies410Names.TargetModule, BeamLoaderDependencies410Names.Run, 0));
    }
}
