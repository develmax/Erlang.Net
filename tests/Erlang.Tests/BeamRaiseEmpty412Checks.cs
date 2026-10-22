using Erlang;

namespace Erlang.Tests;

internal static class BeamRaiseEmpty412Checks
{
    internal static async Task ChangedBinding()
    {
        BeamModule module = BeamModule.Prepare(BeamRaiseEmpty412Bytes.Image(BeamRaiseEmpty412Shape.Valid));
        await using var runtime = new ProcessRuntime();
        bool invoked = false;
        _ = runtime.Modules.DeleteModule(ErlangModuleNames.Module);
        runtime.Modules.Register(
            ErlangModuleNames.Module,
            BeamRaiseEmpty412Names.RaiseFunction,
            3,
            (_, _) =>
        {
            invoked = true;

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        bool refused = false;
        try
        {
            await actor.InvokeAsync(context => module.Invoke(context, BeamOpcodeEdgeNames.Run).AsTask());
        }
        catch (NotSupportedException)
        {
            refused = true;
        }
        BeamBinaryHostAccess.Require(refused && !invoked);
    }

    internal static async Task Valid()
    {
        BeamModule module = BeamModule.Prepare(BeamRaiseEmpty412Bytes.Image(BeamRaiseEmpty412Shape.Valid));
        BeamBinaryHostAccess.Require(module.Supports(BeamOpcodeEdgeNames.Run, 0));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term result = await actor.InvokeAsync(context => module.Invoke(context, BeamOpcodeEdgeNames.Run).AsTask());
        BeamBinaryHostAccess.Require(result.Equals(Term.A(BeamOpcodeEdgeNames.Marker)));
    }

    internal static Task Negatives()
    {
        foreach (BeamRaiseEmpty412Shape shape in new[] { BeamRaiseEmpty412Shape.InvalidClass, BeamRaiseEmpty412Shape.UnknownClass,
            BeamRaiseEmpty412Shape.NonNil, BeamRaiseEmpty412Shape.Clobber, BeamRaiseEmpty412Shape.Bypass })
        {
            bool refused;
            try
            {
                refused = !BeamModule.Prepare(BeamRaiseEmpty412Bytes.Image(shape)).Supports(BeamOpcodeEdgeNames.Run,
                               shape == BeamRaiseEmpty412Shape.UnknownClass ? 1 : 0);
            }
            catch (Exception error) when (error is InvalidDataException or NotSupportedException)
            {
                refused = true;
            }
            BeamBinaryHostAccess.Require(refused);
        }

        return Task.CompletedTask;
    }

    internal static async Task OrdinaryReturn()
    {
        BeamModule module = BeamModule.Prepare(BeamRaiseEmpty412Bytes.Image(BeamRaiseEmpty412Shape.OrdinaryBadarg));
        BeamBinaryHostAccess.Require(module.Supports(BeamOpcodeEdgeNames.Run, 0));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term result = await actor.InvokeAsync(context => module.Invoke(context, BeamOpcodeEdgeNames.Run).AsTask());
        BeamBinaryHostAccess.Require(result.Equals(Term.A(ErlangErrorReasons.BadArgument)));
    }
}
