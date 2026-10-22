namespace Erlang.Tests;

internal static class DebugDispatch432LifecycleChecks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DebugDispatch432Names.Failure);
    }

    internal static async Task Run()
    {
        for (int mode = 0; mode < 3; mode++) await One(mode);
    }

    private static async Task One(int mode)
    {
        var runtime = new ProcessRuntime(1);
        Task<Term>? call = null;
        try
        {
            await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugDispatch432.Register(runtime, registry));
            PreparedBeamImage image = runtime.Modules.PrepareBeam(BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(Term.I(432))));
            Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
            var control = new RuntimeNodeFoundationHostActor(runtime);
            var target = new RuntimeNodeFoundationHostActor(runtime);
            await control.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                DebugDispatch432Names.Debug,
                DebugDispatch432Names.Breakpoint,
                Term.Tuple(Term.A(BeamOpcodeEdgeNames.Module), Term.A(BeamOpcodeEdgeNames.Run), Term.I(0)),
                Term.A(DebugDispatch432Names.True)
            ));
            RuntimeDebugController432 controller = runtime.DebugController432;
            call = target.InvokeAsync(async context => await runtime.Modules.Call(context, BeamOpcodeEdgeNames.Module, BeamOpcodeEdgeNames.Run));
            RuntimeDebugStop432 stop = await controller.WaitForStopAsync().AsTask().WaitAsync(DebugDispatch432Names.Deadline);
            Require(!call.IsCompleted && !stop.IsSettled && stop.Image is not null);
            if (mode == 0) await runtime.DisposeAsync().AsTask().WaitAsync(DebugDispatch432Names.Deadline);
            else if (mode == 1)
            {
                await runtime.RequestImmediateHalt(0).WaitAsync(DebugDispatch432Names.Deadline);
                await runtime.HaltQuiescence.WaitAsync(DebugDispatch432Names.Deadline);
            }
            else await control.Invoke(context =>
 {
     runtime.Exit(context, runtime.InitializationPid, Term.A("kill"));

     return Nil.Value;
 });
            bool refused = false;
            try
            {
                await call;
            }
            catch (OperationCanceledException)
            {
                refused = true;
            }
            catch (ObjectDisposedException)
            {
                refused = true;
            }
            catch (Exception error) when (error.GetType().FullName == DebugDispatch432Names.HaltTransfer)
            {
                refused = true;
            }
            Require(refused && stop.IsSettled && stop.Image is null && stop.Arguments.Count == 0);
            if (mode == 2) Require(!runtime.IsAlive(runtime.InitializationPid));
        }
        finally
        {
            await runtime.DisposeAsync();
            if (call is not null) try
                {
                    await call;
                }
                catch (OperationCanceledException) { }
                catch (ObjectDisposedException) { }
                catch (Exception error) when (error.GetType().FullName == DebugDispatch432Names.HaltTransfer) { }
        }
    }
}
