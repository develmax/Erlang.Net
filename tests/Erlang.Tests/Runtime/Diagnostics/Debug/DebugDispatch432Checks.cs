using System.Reflection;
namespace Erlang.Tests;

internal static class DebugDispatch432Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DebugDispatch432Names.Failure);
    }

    private static ValueTask<Term> Toggle(ProcessContext context, bool enable)
        => context.Runtime.Modules.Call(
            context,
            DebugDispatch432Names.Debug,
            DebugDispatch432Names.Breakpoint,
            Term.Tuple(Term.A(BeamOpcodeEdgeNames.Module), Term.A(BeamOpcodeEdgeNames.Run), Term.I(0)),
            Term.A(enable ? DebugDispatch432Names.True : DebugDispatch432Names.False)
        );

    internal static async Task Run()
    {
        var runtime = new ProcessRuntime(1);
        try
        {
            await Body(runtime);
        }
        finally
        {
            await runtime.DisposeAsync();
        }
    }

    private static async Task Body(ProcessRuntime runtime)
    {
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugDispatch432.Register(runtime, registry));
        byte[] bytes = BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(Term.I(432)));
        PreparedBeamImage first = runtime.Modules.PrepareBeam(bytes);
        Require(runtime.Modules.PublishPreparedBeam(first).Status == BeamPublicationStatus.Published);
        var control = new RuntimeNodeFoundationHostActor(runtime);
        Term instructions = await control.InvokeAsync(async context => await runtime.Modules.Call(context, DebugDispatch432Names.Debug, DebugDispatch432Names.Instructions));
        Type interpreter = typeof(ProcessRuntime).Assembly.GetType("Erlang.BeamRegisterInterpreter", true)!;
        var handler = (Delegate)interpreter.GetField("executionHandler432", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        Require(instructions.Equals(Cons.From(new[] { Term.String(handler.Method.Name) })));
        Require((await control.InvokeAsync(async context => await Toggle(context, true))).Equals(Term.I(1)));
        var target = new RuntimeNodeFoundationHostActor(runtime);
        RuntimeDebugController432 controller = runtime.DebugController432;
        Task<Term> call = target.InvokeAsync(async context => await runtime.Modules.Call(context, BeamOpcodeEdgeNames.Module, BeamOpcodeEdgeNames.Run));
        RuntimeDebugStop432 stop = await controller.WaitForStopAsync().AsTask().WaitAsync(DebugDispatch432Names.Deadline);
        Require(!call.IsCompleted && stop.Process.Equals(target.Process.Pid) && stop.Function == BeamOpcodeEdgeNames.Run
            && stop.Arity == 0 && stop.Arguments.Count == 0 && !stop.IsSettled && ReferenceEquals(stop.Image, runtime.Modules.CurrentCodeImage(first.Name)));
        // Real code lease must still identify a deleted/old image while stopped.
        ModuleCodeImage old = stop.Image!;
        Require(runtime.Modules.DeleteModule(first.Name).Equals(Term.A(DebugDispatch432Names.True)));
        Term code = await control.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            "erlang",
            "check_process_code",
            target.Process.Pid,
            Term.A(first.Name)
        ));
        Require(code.Equals(Term.A(DebugDispatch432Names.True)) && !call.IsCompleted);
        PreparedBeamImage second = runtime.Modules.PrepareBeam(bytes);
        Require(runtime.Modules.PublishPreparedBeam(second).Status == BeamPublicationStatus.Published);
        Require(!ReferenceEquals(old, runtime.Modules.CurrentCodeImage(second.Name)));
        // Newly published generation is not accidentally armed by MFA alone.
        Term fresh = await control.InvokeAsync(async context => await runtime.Modules.Call(context, BeamOpcodeEdgeNames.Module, BeamOpcodeEdgeNames.Run));
        Require(fresh.Equals(Term.I(432)) && !call.IsCompleted);
        stop.Resume();
        Require((await call).Equals(Term.I(432)) && stop.IsSettled && stop.Image is null && stop.Arguments.Count == 0);
        Require((await control.InvokeAsync(async context => await Toggle(context, true))).Equals(Term.I(1)));
        var victim = new RuntimeNodeFoundationHostActor(runtime);
        Task<Term> blocked = victim.InvokeAsync(async context => await runtime.Modules.Call(context, BeamOpcodeEdgeNames.Module, BeamOpcodeEdgeNames.Run));
        RuntimeDebugStop432 killed = await controller.WaitForStopAsync().AsTask().WaitAsync(DebugDispatch432Names.Deadline);
        Require(killed.Process.Equals(victim.Process.Pid) && !blocked.IsCompleted);
        await control.Invoke(context =>
 {
     runtime.Exit(context, victim.Process.Pid, Term.A("kill"));

     return Nil.Value;
 });
        try
        {
            await blocked;
            throw new InvalidOperationException(DebugDispatch432Names.Failure);
        }
        catch (OperationCanceledException) { }
        await victim.Process.Completion.WaitAsync(DebugDispatch432Names.Deadline);
        Require(killed.IsSettled && killed.Arguments.Count == 0 && killed.Image is null);
        await victim.DisposeAsync();
        Require((await control.InvokeAsync(async context => await Toggle(context, false))).Equals(Term.I(1)));
        Require((await target.InvokeAsync(async context => await runtime.Modules.Call(context, BeamOpcodeEdgeNames.Module, BeamOpcodeEdgeNames.Run))).Equals(Term.I(432)));
    }
}
