using System.Runtime.CompilerServices;

namespace Erlang.Tests;

internal static class PersistentNative417AmbientChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(1);
        PreparedBeamImage image = runtime.Modules.PrepareBeam(
            NativeProvider408Access.Resource(PersistentNative417Names.Image),
            new BeamDependencySelection([NativeProvider408Names.RuntimeModule])
        );
        PersistentTerm408HostAccess.Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        runtime.Modules.Register(
            PersistentNative417Names.Handler,
            ProcessErrorHandlerHostNames.Undefined,
            3,
            static async (context, _) =>
            {
                await context.Runtime.Modules.Call(context, PersistentNative417Names.Module, PersistentNative417Names.Info);

                return Term.A(PersistentNative417Names.Ok);
            }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var weak = await FirstCall(actor, runtime);
        _ = await actor.Invoke(_ => true);
        // Real undefined-function dispatch installed selectedErrorHandler arguments
        // and host execution-frame AsyncLocals, not a fabricated test ambient slot.
        GuestStoredFunction407Access.Collected(weak);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<WeakReference<object>> FirstCall(RuntimeNodeFoundationHostActor actor, ProcessRuntime runtime)
        => actor.InvokeAsync(async context =>
        {
            await runtime.Modules.Call(
                context,
                NativeProvider408Names.RuntimeModule,
                ProcessErrorHandlerHostNames.ProcessFlag,
                Term.A(ProcessErrorHandlerHostNames.Flag),
                Term.A(PersistentNative417Names.Handler)
            );
            var argument = Term.Tuple(new BitString(new byte[8192]), Term.List(Term.I(4), Term.I(1), Term.I(7)));
            Term result = await runtime.Modules.Call(
                context,
                ProcessErrorHandlerHostNames.MissingModule,
                ProcessErrorHandlerHostNames.MissingFunction,
                argument
            );
            PersistentTerm408HostAccess.Require(result.Equals(Term.A(PersistentNative417Names.Ok)));

            return new WeakReference<object>(argument);
        });
}
