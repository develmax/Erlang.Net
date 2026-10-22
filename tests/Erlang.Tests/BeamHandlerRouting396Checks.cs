using static Erlang.Tests.LibrarySelectionHostData;

namespace Erlang.Tests;

internal static class BeamHandlerRouting396Checks
{
    internal static Task Admission()
    {
        Accept(BeamHandlerRouting396Bytes.Image());
        Accept(BeamHandlerRouting396Bytes.Image(tuplePrefix: true));
        Reject(BeamHandlerRouting396Bytes.Image(returning: true));
        Reject(BeamHandlerRouting396Bytes.Image(oneReturningArm: true));
        Reject(BeamHandlerRouting396Bytes.Image(badRoot: true));
        Reject(BeamHandlerRouting396Bytes.Image(directJump: true));
        Reject(BeamHandlerRouting396Bytes.Image(wrongSlot: true));

        return Task.CompletedTask;
    }

    internal static async Task CaughtRuntime()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (bool tuple in new[] { false, true })
        {
            BeamModule module = BeamModule.Prepare(BeamHandlerRouting396Bytes.Image(tuplePrefix: tuple));
            Term result = await actor.InvokeAsync(context => module.Invoke(context, BeamOpcodeEdgeNames.Run, Term.I(3)).AsTask());
            Require(result.Equals(Term.A(BeamOpcodeEdgeNames.Marker)));
        }
    }

    internal static async Task ReturningImportCannotForgeHandler()
    {
        foreach (bool markNative in new[] { false, true })
        {
            await using var runtime = new ProcessRuntime();
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            int calls = 0;
            runtime.Modules.Apply(registry =>
            {
                registry.DeleteModule(ErlangModuleNames.Module);

                void Override(ModuleRegistry target) => target.Register(
                    ErlangModuleNames.Module,
                    BeamHandlerRouting396Names.Error,
                    1,
                    (_, _) =>
                    {
                        Interlocked.Increment(ref calls);

                        return ValueTask.FromResult<Term>(Term.I(23));
                    }
                );
                if (markNative)
                    registry.InstallPreloaded(Override);
                else
                    Override(registry);
            });
            BeamModule module = BeamModule.Prepare(BeamHandlerRouting396Bytes.Image());
            bool refused = false;
            try
            {
                _ = await actor.InvokeAsync(context => module.Invoke(context, BeamOpcodeEdgeNames.Run, Term.I(3)).AsTask());
            }
            catch (InvalidDataException)
            {
                refused = true;
            }
            Require(refused && calls == 1);
        }
    }

    private static void Accept(byte[] image)
        => Require(BeamModule.Prepare(image).Supports(BeamOpcodeEdgeNames.Run, 1));

    private static void Reject(byte[] image)
    {
        bool refused;
        try
        {
            refused = !BeamModule.Prepare(image).Supports(BeamOpcodeEdgeNames.Run, 1);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            refused = true;
        }
        Require(refused);
    }
}
